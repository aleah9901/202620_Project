using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;

namespace QTI_Editor.WWW
{
    public partial class QuestionEditor : System.Web.UI.Page
    {
        // Supported QTI namespaces — spec requires v2.1 support alongside v2.2
        private static readonly XNamespace QtiNs22 = "http://www.imsglobal.org/xsd/imsqti_v2p2";
        private static readonly XNamespace QtiNs21 = "http://www.imsglobal.org/xsd/imsqti_v2p1";

        // The namespace detected from the current document (set during load)
        private XNamespace ActiveQtiNs
        {
            get { return (XNamespace)((string)ViewState["ActiveQtiNs"] ?? QtiNs22.NamespaceName); }
            set { ViewState["ActiveQtiNs"] = value.NamespaceName; }
        }

        // Enumeration of question types
        private enum QuestionType
        {
            Unknown, MultipleChoice, MultiSelect, LongFormEssay, ShortAnswer, FileUpload, NumericalRange
        }

        // In-memory list of answer choices for the current question
        private List<ChoiceItem> Choices
        {
            get { return (List<ChoiceItem>)ViewState["Choices"] ?? new List<ChoiceItem>(); }
            set { ViewState["Choices"] = value; }
        }

        // In-memory list of acceptable short answers
        private List<string> ShortAnswers
        {
            get { return (List<string>)ViewState["ShortAnswers"] ?? new List<string>(); }
            set { ViewState["ShortAnswers"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadQuestion();
            }
        }

        // Navigates back to the QuizOverview page.
        // Referenced by btnBack (OnClick="Back_Click") and btnDiscard.
        protected void Back_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/QuizOverview.aspx");
        }

        // Fires when the question type dropdown changes.
        // Shows or hides the appropriate panels.
        protected void Type_Changed(object sender, EventArgs e)
        {
            ShowPanelsForType(ddlType.SelectedValue);
        }

        // Adds a new blank answer choice to the list and rebinds.
        protected void AddAnswer_Click(object sender, EventArgs e)
        {
            CollectChoicesFromRepeater();
            var choices = Choices;
            choices.Add(new ChoiceItem
            {
                Identifier = "choice_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                Text = "",
                IsCorrect = false
            });
            Choices = choices;
            BindChoices();
        }

        // Handles RemoveChoice commands from within the answer choice repeater.
        protected void choiceRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "RemoveChoice")
            {
                CollectChoicesFromRepeater();
                string idToRemove = e.CommandArgument.ToString();
                var choices = Choices;
                choices.RemoveAll(c => c.Identifier == idToRemove);
                Choices = choices;
                BindChoices();
            }
        }

        // Adds a new blank short answer to the list and rebinds.
        protected void AddShortAnswer_Click(object sender, EventArgs e)
        {
            CollectShortAnswersFromRepeater();
            var answers = ShortAnswers;
            answers.Add("");
            ShortAnswers = answers;
            BindShortAnswers();
        }

        // Handles RemoveAnswer commands from the short answer repeater.
        protected void shortAnswerRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "RemoveAnswer")
            {
                CollectShortAnswersFromRepeater();
                int index = int.Parse(e.CommandArgument.ToString());
                var answers = ShortAnswers;
                if (index >= 0 && index < answers.Count)
                    answers.RemoveAt(index);
                ShortAnswers = answers;
                BindShortAnswers();
            }
        }

        // Saves the current question data back to the QTI XML file.    
        protected void Save_Question(object sender, EventArgs e)
        {
            string sessionId = (string)Session["QtiSessionId"];
            string href = (string)Session["QtiCurrentItem"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(href))
            {
                ShowError("No active question to save.");
                return;
            }

            string filePath = ResolveItemPath(sessionId, href);
            if (filePath == null)
            {
                ShowError("Question file not found.");
                return;
            }

            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement root = doc.Root;
                XNamespace ns = ActiveQtiNs;

                // Update title attribute
                root.SetAttributeValue("title", txtTitle.Text.Trim());

                // Get or create itemBody
                XElement itemBody = root.Elements(ns + "itemBody").FirstOrDefault();
                if (itemBody == null)
                {
                    itemBody = new XElement(ns + "itemBody");
                    root.Add(itemBody);
                }

                // Update question prompt text — write to <p> element within itemBody
                XElement pElement = itemBody.Elements(ns + "p").FirstOrDefault();
                if (pElement == null)
                {
                    pElement = new XElement(ns + "p");
                    itemBody.AddFirst(pElement);
                }
                pElement.Value = txtQuestionText.Text.Trim();

                // Also update <prompt> inside any interaction if one existed
                XElement prompt = itemBody.Descendants(ns + "prompt").FirstOrDefault();
                if (prompt != null)
                {
                    prompt.Value = txtQuestionText.Text.Trim();
                }

                // Save type-specific data
                string selectedType = ddlType.SelectedValue;

                if (selectedType == "MultipleChoice" || selectedType == "MultiSelect")
                {
                    CollectChoicesFromRepeater();
                    SaveChoiceInteraction(root, itemBody, ns, selectedType);
                }
                else if (selectedType == "ShortAnswer")
                {
                    SaveShortAnswer(root, itemBody, ns);
                }
                else if (selectedType == "NumericalRange")
                {
                    SaveNumericalRange(root, itemBody, ns);
                }

                doc.Save(filePath);
                ScriptManager.RegisterStartupScript(this, GetType(), "HideModal", "hideModal();", true);
            }
            catch (Exception ex)
            {
                ShowError("Save failed: " + ex.Message);
            }
        }

        // Deletes the current question and navigates back to Overview.
        protected void DeleteQuestion_Click(object sender, EventArgs e)
        {
            string sessionId = (string)Session["QtiSessionId"];
            string href = (string)Session["QtiCurrentItem"];

            if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(href))
            {
                var service = new QuestionEditService();
                service.DeleteQuestion(sessionId, href, Server);
            }

            Response.Redirect("~/QuizOverview.aspx");
        }

        // ----- Save helpers -----

        // Saves multiple-choice / multi-select answer data to the QTI XML
        private void SaveChoiceInteraction(XElement root, XElement itemBody, XNamespace ns, string selectedType)
        {
            XElement choiceInteraction = itemBody.Descendants(ns + "choiceInteraction").FirstOrDefault();
            if (choiceInteraction == null)
            {
                choiceInteraction = new XElement(ns + "choiceInteraction",
                    new XAttribute("responseIdentifier", "RESPONSE"),
                    new XAttribute("shuffle", "false"),
                    new XAttribute("maxChoices", selectedType == "MultiSelect" ? "0" : "1"));
                itemBody.Add(choiceInteraction);
            }
            else
            {
                choiceInteraction.SetAttributeValue("maxChoices", selectedType == "MultiSelect" ? "0" : "1");
            }

            // Replace all simpleChoice elements
            choiceInteraction.Elements(ns + "simpleChoice").Remove();
            foreach (var choice in Choices)
            {
                choiceInteraction.Add(new XElement(ns + "simpleChoice",
                    new XAttribute("identifier", choice.Identifier),
                    choice.Text));
            }

            // Update responseDeclaration with correct answers
            UpdateCorrectResponse(root, ns, Choices.Where(c => c.IsCorrect).Select(c => c.Identifier).ToList());
        }

        // Saves short-answer data to the QTI XML, including mapping for multiple correct answers
        private void SaveShortAnswer(XElement root, XElement itemBody, XNamespace ns)
        {
            XElement textEntry = itemBody.Descendants(ns + "textEntryInteraction").FirstOrDefault();
            if (textEntry == null)
            {
                textEntry = new XElement(ns + "textEntryInteraction",
                    new XAttribute("responseIdentifier", "RESPONSE"));
                itemBody.Add(textEntry);
            }

            CollectShortAnswersFromRepeater();
            var answers = ShortAnswers.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();

            // Set the first answer as the primary correct response
            if (answers.Count > 0)
                UpdateCorrectResponse(root, ns, new List<string> { answers[0] });

            // Write ALL answers to <mapping> with <mapEntry> per QTI 2.2 spec
            XElement responseDecl = root.Elements(ns + "responseDeclaration")
                .FirstOrDefault(rd => (string)rd.Attribute("identifier") == "RESPONSE");

            if (responseDecl != null && answers.Count > 0)
            {
                // Remove old mapping, rebuild
                responseDecl.Elements(ns + "mapping").Remove();

                var mapping = new XElement(ns + "mapping",
                    new XAttribute("defaultValue", "0"));
                foreach (string answer in answers)
                {
                    mapping.Add(new XElement(ns + "mapEntry",
                        new XAttribute("mapKey", answer.Trim()),
                        new XAttribute("mappedValue", "2")));
                }
                responseDecl.Add(mapping);
            }
        }

        // Saves numerical-range data to the QTI XML
        private void SaveNumericalRange(XElement root, XElement itemBody, XNamespace ns)
        {
            XElement textEntry = itemBody.Descendants(ns + "textEntryInteraction").FirstOrDefault();
            if (textEntry == null)
            {
                textEntry = new XElement(ns + "textEntryInteraction",
                    new XAttribute("responseIdentifier", "RESPONSE"));
                itemBody.Add(textEntry);
            }

            string rangeValue = "[" + txtRangeMin.Text.Trim() + "," + txtRangeMax.Text.Trim() + "]";
            UpdateCorrectResponse(root, ns, new List<string> { rangeValue });
        }

        // Updates the responseDeclaration > correctResponse > value elements
        private void UpdateCorrectResponse(XElement root, XNamespace ns, List<string> correctValues)
        {
            XElement responseDecl = root.Elements(ns + "responseDeclaration")
                .FirstOrDefault(rd => (string)rd.Attribute("identifier") == "RESPONSE");

            if (responseDecl == null)
            {
                responseDecl = new XElement(ns + "responseDeclaration",
                    new XAttribute("identifier", "RESPONSE"),
                    new XAttribute("cardinality", correctValues.Count > 1 ? "multiple" : "single"),
                    new XAttribute("baseType", "identifier"));
                XElement itemBody = root.Elements(ns + "itemBody").FirstOrDefault();
                if (itemBody != null)
                    itemBody.AddBeforeSelf(responseDecl);
                else
                    root.AddFirst(responseDecl);
            }

            responseDecl.Elements(ns + "correctResponse").Remove();

            var correctResponse = new XElement(ns + "correctResponse");
            foreach (string val in correctValues)
            {
                correctResponse.Add(new XElement(ns + "value", val));
            }
            responseDecl.Add(correctResponse);
        }

        // ----- Load logic -----

        // Loads the assessment item referenced by Session["QtiCurrentItem"]
        private void LoadQuestion()
        {
            string sessionId = (string)Session["QtiSessionId"];
            string href = (string)Session["QtiCurrentItem"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(href))
            {
                ShowError("No question selected.");
                return;
            }

            string filePath = ResolveItemPath(sessionId, href);
            if (filePath == null)
            {
                ShowError("Question file not found: " + href);
                return;
            }

            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement root = doc.Root;

                // Detect the namespace from the actual document (v2.1 or v2.2)
                XNamespace ns = DetectQtiNamespace(root);
                ActiveQtiNs = ns;

                txtTitle.Text = (string)root.Attribute("title") ?? "";

                XElement itemBody = root.Elements(ns + "itemBody").FirstOrDefault();

                // Extract question text from <p> elements AND <prompt> elements
                string questionText = ExtractQuestionText(itemBody, ns);
                txtQuestionText.Text = questionText;

                // Detect question type — pass the full itemBody.Value for file-upload detection
                string fullBodyText = itemBody != null ? itemBody.Value : "";
                QuestionType qType = DetectQuestionType(itemBody, ns, fullBodyText);
                lblQuestionType.Text = qType.ToString();

                // Select the type in the dropdown (handle Unknown by defaulting to LongFormEssay)
                if (ddlType.Items.FindByValue(qType.ToString()) != null)
                    ddlType.SelectedValue = qType.ToString();
                else
                    ddlType.SelectedValue = "LongFormEssay";

                ShowPanelsForType(ddlType.SelectedValue);

                // Load type-specific data
                if (qType == QuestionType.MultipleChoice || qType == QuestionType.MultiSelect)
                {
                    LoadChoices(root, itemBody, ns);
                }
                else if (qType == QuestionType.ShortAnswer)
                {
                    LoadShortAnswer(root, ns);
                }
                else if (qType == QuestionType.NumericalRange)
                {
                    LoadNumericalRange(root, ns);
                }
            }
            catch (Exception ex)
            {
                ShowError("Could not load question: " + ex.Message);
            }
        }

        // Extracts question text from <p> elements in the itemBody and <prompt> elements
        // inside interactions. Per QTI 2.2 spec, the question text may live in either location.
        private string ExtractQuestionText(XElement itemBody, XNamespace ns)
        {
            if (itemBody == null) return "";

            var textParts = new List<string>();

            // 1. Collect text from direct <p> children of itemBody
            foreach (var p in itemBody.Elements(ns + "p"))
            {
                string pText = p.Value.Trim();
                if (!string.IsNullOrEmpty(pText))
                    textParts.Add(pText);
            }

            // 2. Collect text from <prompt> elements inside any interaction
            //    Per QTI spec, <prompt> is a child of choiceInteraction, orderInteraction, etc.
            foreach (var prompt in itemBody.Descendants(ns + "prompt"))
            {
                string promptText = prompt.Value.Trim();
                if (!string.IsNullOrEmpty(promptText))
                    textParts.Add(promptText);
            }

            // If both exist, prefer <prompt> text if <p> is empty/generic
            // If only one exists, use that
            return string.Join("\n", textParts.Distinct());
        }

        // Loads multiple-choice / multi-select answer data from the QTI XML
        private void LoadChoices(XElement root, XElement itemBody, XNamespace ns)
        {
            var choices = new List<ChoiceItem>();
            if (itemBody == null) return;

            var choiceInteraction = itemBody.Descendants(ns + "choiceInteraction").FirstOrDefault();
            if (choiceInteraction != null)
            {
                foreach (var sc in choiceInteraction.Elements(ns + "simpleChoice"))
                {
                    choices.Add(new ChoiceItem
                    {
                        Identifier = (string)sc.Attribute("identifier") ?? "",
                        Text = sc.Value.Trim(),
                        IsCorrect = false
                    });
                }
            }

            // Mark correct answers from responseDeclaration
            var correctValues = root.Elements(ns + "responseDeclaration")
                .SelectMany(rd => rd.Elements(ns + "correctResponse"))
                .SelectMany(cr => cr.Elements(ns + "value"))
                .Select(v => v.Value.Trim())
                .ToList();

            foreach (var c in choices)
            {
                c.IsCorrect = correctValues.Contains(c.Identifier);
            }

            Choices = choices;
            BindChoices();
        }

        // Loads short-answer data from the QTI XML
        // Reads from <mapping> <mapEntry> elements to get all acceptable answers
        private void LoadShortAnswer(XElement root, XNamespace ns)
        {
            var answers = new List<string>();

            // Primary source: <mapping><mapEntry> which holds all acceptable answers
            var mapEntries = root.Elements(ns + "responseDeclaration")
                .Where(rd => (string)rd.Attribute("identifier") == "RESPONSE")
                .SelectMany(rd => rd.Elements(ns + "mapping"))
                .SelectMany(m => m.Elements(ns + "mapEntry"))
                .Select(me => ((string)me.Attribute("mapKey") ?? "").Trim())
                .Where(k => !string.IsNullOrEmpty(k))
                .ToList();

            if (mapEntries.Count > 0)
            {
                answers = mapEntries;
            }
            else
            {
                // Fallback: read from <correctResponse><value>
                string correctValue = root.Elements(ns + "responseDeclaration")
                    .SelectMany(rd => rd.Elements(ns + "correctResponse"))
                    .SelectMany(cr => cr.Elements(ns + "value"))
                    .Select(v => v.Value.Trim())
                    .FirstOrDefault() ?? "";
                if (!string.IsNullOrEmpty(correctValue))
                    answers.Add(correctValue);
            }

            ShortAnswers = answers;
            BindShortAnswers();
        }

        // Loads numerical-range [min,max] from the QTI XML
        private void LoadNumericalRange(XElement root, XNamespace ns)
        {
            string correctValue = root.Elements(ns + "responseDeclaration")
                .SelectMany(rd => rd.Elements(ns + "correctResponse"))
                .SelectMany(cr => cr.Elements(ns + "value"))
                .Select(v => v.Value.Trim())
                .FirstOrDefault() ?? "";

            var match = Regex.Match(correctValue, @"\[\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*\]");
            if (match.Success)
            {
                txtRangeMin.Text = match.Groups[1].Value;
                txtRangeMax.Text = match.Groups[2].Value;
            }
        }

        // ----- Shared utilities -----

        // Detects whether the document uses QTI v2.2 or v2.1 namespace.
        // Per spec: "Documents with a namespace of ...v2p1 must still be supported."
        private XNamespace DetectQtiNamespace(XElement root)
        {
            if (root == null) return QtiNs22;

            string rootNs = root.Name.NamespaceName;

            if (rootNs == QtiNs21.NamespaceName)
                return QtiNs21;

            // Default to v2.2 (also handles no-namespace documents)
            return QtiNs22;
        }

        // Resolves the physical file path for a given session + href
        private string ResolveItemPath(string sessionId, string href)
        {
            string extractDir = Server.MapPath("~/cache/" + sessionId + "/extracted");
            string filePath = System.IO.Path.Combine(extractDir, href);

            if (System.IO.File.Exists(filePath))
                return filePath;

            // Search subdirectories
            string[] found = System.IO.Directory.GetFiles(
                extractDir, System.IO.Path.GetFileName(href), System.IO.SearchOption.AllDirectories);
            return found.Length > 0 ? found[0] : null;
        }

        // Shows/hides panels based on the selected question type
        private void ShowPanelsForType(string type)
        {
            pnlChoices.Visible = (type == "MultipleChoice" || type == "MultiSelect");
            pnlShortAnswer.Visible = (type == "ShortAnswer");
            pnlRange.Visible = (type == "NumericalRange");
        }

        // Collects current choice text and check states from the repeater controls
        private void CollectChoicesFromRepeater()
        {
            var choices = new List<ChoiceItem>();
            foreach (RepeaterItem item in choiceRepeater.Items)
            {
                HiddenField hid = (HiddenField)item.FindControl("hidIdentifier");
                TextBox txt = (TextBox)item.FindControl("txtChoice");
                CheckBox chk = (CheckBox)item.FindControl("chkCorrect");

                if (hid != null && txt != null && chk != null)
                {
                    choices.Add(new ChoiceItem
                    {
                        Identifier = hid.Value,
                        Text = txt.Text,
                        IsCorrect = chk.Checked
                    });
                }
            }
            Choices = choices;
        }

        // Binds the choices list to the repeater
        private void BindChoices()
        {
            choiceRepeater.DataSource = Choices;
            choiceRepeater.DataBind();
        }

        // Collects current short answer values from the repeater controls
        private void CollectShortAnswersFromRepeater()
        {
            var answers = new List<string>();
            foreach (RepeaterItem item in shortAnswerRepeater.Items)
            {
                TextBox txt = (TextBox)item.FindControl("txtShortAnswerItem");
                if (txt != null)
                    answers.Add(txt.Text);
            }
            ShortAnswers = answers;
        }

        // Binds the short answers list to the repeater
        private void BindShortAnswers()
        {
            shortAnswerRepeater.DataSource = ShortAnswers;
            shortAnswerRepeater.DataBind();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }

        // Detects the question type from the QTI XML structure
        private QuestionType DetectQuestionType(XElement itemBody, XNamespace ns, string bodyText)
        {
            if (IsFileUploadQuestion(bodyText))
                return QuestionType.FileUpload;

            if (itemBody == null)
                return QuestionType.LongFormEssay;

            XElement choiceInteraction = itemBody.Descendants(ns + "choiceInteraction").FirstOrDefault();
            XElement textEntry = itemBody.Descendants(ns + "textEntryInteraction").FirstOrDefault();
            XElement extendedText = itemBody.Descendants(ns + "extendedTextInteraction").FirstOrDefault();
            XElement uploadInteraction = itemBody.Descendants(ns + "uploadInteraction").FirstOrDefault();

            if (extendedText != null)
                return QuestionType.LongFormEssay;

            bool hasAnyInteraction = choiceInteraction != null || textEntry != null || uploadInteraction != null;

            if (!hasAnyInteraction)
                return QuestionType.LongFormEssay;

            if (uploadInteraction != null)
                return QuestionType.FileUpload;

            if (textEntry != null && HasNumericalRangeResponse(itemBody.Document?.Root, ns))
                return QuestionType.NumericalRange;

            if (textEntry != null)
                return QuestionType.ShortAnswer;

            if (choiceInteraction != null)
            {
                string maxChoicesAttr = (string)choiceInteraction.Attribute("maxChoices");
                int maxChoices = 1;
                int.TryParse(maxChoicesAttr, out maxChoices);

                return maxChoices > 1 ? QuestionType.MultiSelect : QuestionType.MultipleChoice;
            }

            return QuestionType.Unknown;
        }

        private bool IsFileUploadQuestion(string bodyText)
        {
            return !string.IsNullOrEmpty(bodyText) && bodyText.IndexOf("Upload a file", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool HasNumericalRangeResponse(XElement assessmentItem, XNamespace ns)
        {
            if (assessmentItem == null) return false;

            string correctValue = assessmentItem
                .Elements(ns + "responseDeclaration")
                .SelectMany(rd => rd.Elements(ns + "correctResponse"))
                .SelectMany(cr => cr.Elements(ns + "value"))
                .Select(v => v.Value.Trim())
                .FirstOrDefault();

            return IsNumericalRangeFormat(correctValue);
        }

        private bool IsNumericalRangeFormat(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return Regex.IsMatch(value.Trim(), @"^\[\s*-?\d+(\.\d+)?\s*,\s*-?\d+(\.\d+)?\s*\]$");
        }
    }

    // Serializable data object for answer choices, stored in ViewState.
    [Serializable]
    public class ChoiceItem
    {
        public string Identifier { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}