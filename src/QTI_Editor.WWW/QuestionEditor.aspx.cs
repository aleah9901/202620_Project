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
        // QTI 1.2 uses no XML namespace for item elements (elements are unqualified)

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

        // Saves the current question data back to the QTI 1.2 XML file.    
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

            // Parse compound href to get item ident
            string itemIdent = null;
            if (href.Contains("#"))
                itemIdent = href.Split(new[] { '#' }, 2)[1];

            try
            {
                XDocument doc = XDocument.Load(filePath);

                // Find the target <item> element
                XElement item = FindItemElement(doc, itemIdent);
                if (item == null)
                {
                    ShowError("Could not locate item element in QTI file.");
                    return;
                }

                // Update title attribute on <item>
                item.SetAttributeValue("title", txtTitle.Text.Trim());

                // Get or create <presentation><flow>
                XElement presentation = item.Elements()
                    .FirstOrDefault(el => el.Name.LocalName == "presentation");
                if (presentation == null)
                {
                    presentation = new XElement("presentation");
                    item.AddFirst(presentation);
                }

                XElement flow = presentation.Elements()
                    .FirstOrDefault(el => el.Name.LocalName == "flow");
                if (flow == null)
                {
                    flow = new XElement("flow");
                    presentation.Add(flow);
                }

                // Update question text in the first <material><mattext>
                UpdateQuestionText(flow, txtQuestionText.Text.Trim());

                // Save type-specific data
                string selectedType = ddlType.SelectedValue;

                if (selectedType == "MultipleChoice" || selectedType == "MultiSelect")
                {
                    CollectChoicesFromRepeater();
                    SaveChoiceData(item, flow, selectedType);
                }
                else if (selectedType == "ShortAnswer")
                {
                    CollectShortAnswersFromRepeater();
                    SaveShortAnswerData(item, flow);
                }
                else if (selectedType == "NumericalRange")
                {
                    SaveNumericalRangeData(item, flow);
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

        // ----- Save helpers for QTI 1.2 -----

        // Updates the first <material><mattext> within the flow element
        private void UpdateQuestionText(XElement flow, string text)
        {
            // Find the first <material> that is a direct child (the question text, not inside a response_label)
            XElement material = flow.Elements()
                .FirstOrDefault(el => el.Name.LocalName == "material");

            if (material == null)
            {
                material = new XElement("material",
                    new XElement("mattext", text));
                flow.AddFirst(material);
                return;
            }

            XElement mattext = material.Elements()
                .FirstOrDefault(el => el.Name.LocalName == "mattext");
            if (mattext == null)
            {
                material.Add(new XElement("mattext", text));
            }
            else
            {
                mattext.Value = text;
            }
        }

        // Saves MC/MS data: rebuilds <response_lid><render_choice> and <resprocessing>
        private void SaveChoiceData(XElement item, XElement flow, string selectedType)
        {
            string cardinality = selectedType == "MultiSelect" ? "Multiple" : "Single";

            // Remove existing response_lid
            flow.Elements().Where(el => el.Name.LocalName == "response_lid").Remove();

            // Build new response_lid with render_choice
            var responseLid = new XElement("response_lid",
                new XAttribute("ident", "RESPONSE"),
                new XAttribute("rcardinality", cardinality));

            var renderChoice = new XElement("render_choice",
                new XAttribute("shuffle", "No"));

            foreach (var choice in Choices)
            {
                renderChoice.Add(new XElement("response_label",
                    new XAttribute("ident", choice.Identifier),
                    new XElement("material",
                        new XElement("mattext", choice.Text))));
            }

            responseLid.Add(renderChoice);
            flow.Add(responseLid);

            // Rebuild resprocessing with correct answers
            RebuildResprocessing(item,
                Choices.Where(c => c.IsCorrect).Select(c => c.Identifier).ToList());
        }

        // Saves short answer data: rebuilds <response_str><render_fib> and <resprocessing>
        private void SaveShortAnswerData(XElement item, XElement flow)
        {
            // Remove existing response_str
            flow.Elements().Where(el => el.Name.LocalName == "response_str").Remove();

            // Build new response_str with render_fib
            flow.Add(new XElement("response_str",
                new XAttribute("ident", "RESPONSE"),
                new XAttribute("rcardinality", "Single"),
                new XElement("render_fib",
                    new XElement("response_label",
                        new XAttribute("ident", "answer")))));

            var answers = ShortAnswers.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
            RebuildResprocessing(item, answers);
        }

        // Saves numerical range: stores [min,max] as the correct value
        private void SaveNumericalRangeData(XElement item, XElement flow)
        {
            // Remove existing response_str
            flow.Elements().Where(el => el.Name.LocalName == "response_str").Remove();

            // Build render_fib for numeric input
            flow.Add(new XElement("response_str",
                new XAttribute("ident", "RESPONSE"),
                new XAttribute("rcardinality", "Single"),
                new XElement("render_fib",
                    new XElement("response_label",
                        new XAttribute("ident", "answer")))));

            string rangeValue = "[" + txtRangeMin.Text.Trim() + "," + txtRangeMax.Text.Trim() + "]";
            RebuildResprocessing(item, new List<string> { rangeValue });
        }

        // Rebuilds the <resprocessing> element with correct answer conditions
        private void RebuildResprocessing(XElement item, List<string> correctValues)
        {
            // Remove old resprocessing
            item.Elements().Where(el => el.Name.LocalName == "resprocessing").Remove();

            var resprocessing = new XElement("resprocessing",
                new XElement("outcomes",
                    new XElement("decvar",
                        new XAttribute("varname", "SCORE"),
                        new XAttribute("vartype", "Decimal"),
                        new XAttribute("defaultval", "0"))));

            foreach (string val in correctValues)
            {
                resprocessing.Add(new XElement("respcondition",
                    new XAttribute("title", "Correct"),
                    new XElement("conditionvar",
                        new XElement("varequal",
                            new XAttribute("respident", "RESPONSE"),
                            val)),
                    new XElement("setvar",
                        new XAttribute("varname", "SCORE"),
                        new XAttribute("action", "Set"),
                        "1")));
            }

            item.Add(resprocessing);
        }

        // ----- Load logic for QTI 1.2 -----

        // Loads the QTI 1.2 assessment item referenced by Session["QtiCurrentItem"]
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

            // Parse compound href for item ident
            string itemIdent = null;
            if (href.Contains("#"))
                itemIdent = href.Split(new[] { '#' }, 2)[1];

            try
            {
                XDocument doc = XDocument.Load(filePath);

                XElement item = FindItemElement(doc, itemIdent);
                if (item == null)
                {
                    ShowError("No <item> element found in QTI file.");
                    return;
                }

                // Title from <item title="...">
                txtTitle.Text = (string)item.Attribute("title") ?? "";

                // Find <presentation> and its <flow> wrapper (if present)
                XElement presentation = item.Elements()
                    .FirstOrDefault(el => el.Name.LocalName == "presentation");

                // The content container is either <flow> inside presentation, or presentation itself
                XElement contentContainer = null;
                if (presentation != null)
                {
                    XElement flow = presentation.Elements()
                        .FirstOrDefault(el => el.Name.LocalName == "flow");
                    contentContainer = flow ?? presentation;
                }

                // Extract question text from <material><mattext>
                string questionText = ExtractQuestionText(contentContainer);
                txtQuestionText.Text = questionText;

                // Detect question type from QTI 1.2 elements
                QuestionType qType = DetectQuestionType(contentContainer, item);
                lblQuestionType.Text = qType.ToString();

                // Select the type in the dropdown
                if (ddlType.Items.FindByValue(qType.ToString()) != null)
                    ddlType.SelectedValue = qType.ToString();
                else
                    ddlType.SelectedValue = "LongFormEssay";

                ShowPanelsForType(ddlType.SelectedValue);

                // Load type-specific data
                if (qType == QuestionType.MultipleChoice || qType == QuestionType.MultiSelect)
                {
                    LoadChoices(contentContainer, item);
                }
                else if (qType == QuestionType.ShortAnswer)
                {
                    LoadShortAnswer(item);
                }
                else if (qType == QuestionType.NumericalRange)
                {
                    LoadNumericalRange(item);
                }
            }
            catch (Exception ex)
            {
                ShowError("Could not load question: " + ex.Message);
            }
        }

        // Extracts the question prompt text from the first <material><mattext> in the container.
        // Skips <mattext> elements inside <response_label> (those are answer choices, not the question).
        private string ExtractQuestionText(XElement container)
        {
            if (container == null) return "";

            var textParts = new List<string>();

            // Get <material> elements that are direct children of the container
            // (not inside response_lid/response_label which hold answer text)
            foreach (var material in container.Elements()
                .Where(el => el.Name.LocalName == "material"))
            {
                var mattext = material.Elements()
                    .FirstOrDefault(el => el.Name.LocalName == "mattext");
                if (mattext != null && !string.IsNullOrWhiteSpace(mattext.Value))
                    textParts.Add(mattext.Value.Trim());
            }

            return string.Join("\n", textParts);
        }

        // Loads MC/MS choices from <response_lid><render_choice><response_label>
        // and marks correct answers from <resprocessing><respcondition><conditionvar><varequal>
        private void LoadChoices(XElement container, XElement item)
        {
            var choices = new List<ChoiceItem>();
            if (container == null) return;

            var renderChoice = container.Descendants()
                .FirstOrDefault(el => el.Name.LocalName == "render_choice");

            if (renderChoice != null)
            {
                foreach (var label in renderChoice.Elements()
                    .Where(el => el.Name.LocalName == "response_label"))
                {
                    string ident = (string)label.Attribute("ident") ?? "";

                    // Get choice text from <material><mattext>
                    string choiceText = "";
                    var mattext = label.Descendants()
                        .FirstOrDefault(el => el.Name.LocalName == "mattext");
                    if (mattext != null)
                        choiceText = mattext.Value.Trim();

                    choices.Add(new ChoiceItem
                    {
                        Identifier = ident,
                        Text = choiceText,
                        IsCorrect = false
                    });
                }
            }

            // Mark correct answers from resprocessing > respcondition > conditionvar > varequal
            var correctValues = item.Descendants()
                .Where(el => el.Name.LocalName == "varequal")
                .Select(v => v.Value.Trim())
                .ToList();

            foreach (var c in choices)
            {
                c.IsCorrect = correctValues.Contains(c.Identifier);
            }

            Choices = choices;
            BindChoices();
        }

        // Loads short-answer data from <resprocessing> <varequal> elements
        private void LoadShortAnswer(XElement item)
        {
            var answers = new List<string>();

            // Read correct answers from <respcondition><conditionvar><varequal>
            var varEquals = item.Descendants()
                .Where(el => el.Name.LocalName == "varequal")
                .Select(v => v.Value.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();

            if (varEquals.Count > 0)
                answers = varEquals;

            ShortAnswers = answers;
            BindShortAnswers();
        }

        // Loads numerical-range [min,max] from <varequal> in resprocessing
        private void LoadNumericalRange(XElement item)
        {
            string correctValue = item.Descendants()
                .Where(el => el.Name.LocalName == "varequal")
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

        // Finds the target <item> element within the document.
        // If itemIdent is provided, matches by ident attribute.
        // Otherwise returns the first <item> element found.
        private XElement FindItemElement(XDocument doc, string itemIdent)
        {
            if (!string.IsNullOrEmpty(itemIdent))
            {
                return doc.Descendants()
                    .FirstOrDefault(el => el.Name.LocalName == "item"
                        && (string)el.Attribute("ident") == itemIdent);
            }

            return doc.Descendants()
                .FirstOrDefault(el => el.Name.LocalName == "item");
        }

        // Resolves the physical file path for a given session + href.
        // Strips the "#IDENT" fragment from compound hrefs used for multi-item files.
        private string ResolveItemPath(string sessionId, string href)
        {
            // Strip item fragment if present (QTI 1.2 compound href: "file.xml#IDENT")
            string filePart = href.Contains("#") ? href.Split(new[] { '#' }, 2)[0] : href;

            string extractDir = Server.MapPath("~/cache/" + sessionId + "/extracted");
            string filePath = System.IO.Path.Combine(extractDir, filePart);

            if (System.IO.File.Exists(filePath))
                return filePath;

            // Search subdirectories
            string[] found = System.IO.Directory.GetFiles(
                extractDir, System.IO.Path.GetFileName(filePart), System.IO.SearchOption.AllDirectories);
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

        // Detects the question type from QTI 1.2 XML structure
        private QuestionType DetectQuestionType(XElement container, XElement item)
        {
            // Check for file upload by body text
            string fullText = item != null ? item.Value : "";
            if (IsFileUploadQuestion(fullText))
                return QuestionType.FileUpload;

            if (container == null)
                return QuestionType.LongFormEssay;

            // QTI 1.2 element detection
            XElement responseLid = container.Descendants()
                .FirstOrDefault(el => el.Name.LocalName == "response_lid");
            XElement responseStr = container.Descendants()
                .FirstOrDefault(el => el.Name.LocalName == "response_str");
            XElement renderChoice = container.Descendants()
                .FirstOrDefault(el => el.Name.LocalName == "render_choice");
            XElement renderFib = container.Descendants()
                .FirstOrDefault(el => el.Name.LocalName == "render_fib");

            bool hasAnyInteraction = responseLid != null || responseStr != null;

            if (!hasAnyInteraction)
                return QuestionType.LongFormEssay;

            // Multiple choice / multi-select: response_lid with render_choice
            if (responseLid != null && renderChoice != null)
            {
                string cardinality = ((string)responseLid.Attribute("rcardinality") ?? "Single").Trim();
                return cardinality.Equals("Multiple", StringComparison.OrdinalIgnoreCase)
                    ? QuestionType.MultiSelect
                    : QuestionType.MultipleChoice;
            }

            // Text input: response_str with render_fib
            if (responseStr != null && renderFib != null)
            {
                // Check for numerical range in resprocessing
                if (HasNumericalRangeResponse(item))
                    return QuestionType.NumericalRange;

                // Distinguish essay from short answer by rows attribute
                string rowsAttr = (string)renderFib.Attribute("rows");
                int rows = 0;
                if (!string.IsNullOrEmpty(rowsAttr))
                    int.TryParse(rowsAttr, out rows);

                return rows > 1 ? QuestionType.LongFormEssay : QuestionType.ShortAnswer;
            }

            return QuestionType.Unknown;
        }

        private bool IsFileUploadQuestion(string bodyText)
        {
            return !string.IsNullOrEmpty(bodyText) && bodyText.IndexOf("Upload a file", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool HasNumericalRangeResponse(XElement item)
        {
            if (item == null) return false;

            string correctValue = item.Descendants()
                .Where(el => el.Name.LocalName == "varequal")
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