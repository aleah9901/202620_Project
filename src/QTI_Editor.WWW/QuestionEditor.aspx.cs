using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using QTI_Editor.WWW.Save;

namespace QTI_Editor.WWW
{
    public partial class QuestionEditor : System.Web.UI.Page
    {
        // QTI 2.2 XML namespace
        private static readonly XNamespace QtiNs = "http://www.imsglobal.org/xsd/imsqti_v2p2";

        // Enumeration of question types
        private enum QuestionType
        {
            Unknown, MultipleChoice, MultiSelect, LongFormEssay, FileUpload, NumericalRange
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Redirect if no session or no question selected
            if (Session["QtiSessionId"] == null)
                Response.Redirect("~/Upload.aspx");

            if (Session["QtiCurrentItem"] == null)
                Response.Redirect("~/QuizOverview.aspx");

            if (!IsPostBack)
                LoadQuestion();
        }

        // Loads the selected question from the session cache and populates the form controls
        private void LoadQuestion()
        {
            string sessionId = Session["QtiSessionId"]  as string;
            string itemHref  = Session["QtiCurrentItem"] as string;

            var service = new QuestionEditService();
            QuestionModel model = service.LoadQuestion(sessionId, itemHref, Server);

            if (model == null)
            {
                lblError.Text    = "Question file could not be loaded.";
                lblError.Visible = true;
                return;
            }

            // Cache in Session so handlers can access the model on postback
            Session["QtiCurrentQuestion"] = model;

            // Populate form fields
            txtTitle.Text        = model.Title;
            txtQuestionText.Text = model.QuestionText;
            lblQuestionType.Text = model.QuestionType;

            // Set type dropdown
            if (ddlType.Items.FindByValue(model.QuestionType) != null)
                ddlType.SelectedValue = model.QuestionType;

            // Show the correct panel for this question type
            ShowPanelsForType(model.QuestionType);

            if (pnlChoices.Visible)
            {
                choiceRepeater.DataSource = model.Choices;
                choiceRepeater.DataBind();
            }

            if (pnlRange.Visible)
            {
                txtRangeMin.Text = model.RangeMin;
                txtRangeMax.Text = model.RangeMax;
            }

            // For ShortAnswer, populate the correct answer
            if (pnlShortAnswer.Visible)
            {
                var correctValues = model.Choices.Where(c => c.IsCorrect).Select(c => c.Text).ToList();
                txtShortAnswer.Text = correctValues.Count > 0 ? correctValues[0] : model.RangeMin ?? "";
            }
        }

        // Shows/hides the answer panels based on the selected question type
        private void ShowPanelsForType(string qType)
        {
            pnlChoices.Visible     = qType == "MultipleChoice" || qType == "MultiSelect";
            pnlRange.Visible       = qType == "NumericalRange";
            pnlShortAnswer.Visible = qType == "ShortAnswer";
        }

        // Fires when the Type dropdown changes — updates panels dynamically
        protected void Type_Changed(object sender, EventArgs e)
        {
            string newType = ddlType.SelectedValue;
            lblQuestionType.Text = newType;
            ShowPanelsForType(newType);

            var model = Session["QtiCurrentQuestion"] as QuestionModel;
            if (model != null)
            {
                model.QuestionType = newType;
                Session["QtiCurrentQuestion"] = model;
            }
        }

        // Collects all edited values from the form, saves them back to the XML, and returns to QuizOverview
        protected void Save_Question(object sender, EventArgs e)
        {
            var model = Session["QtiCurrentQuestion"] as QuestionModel;

            if (model == null)
            {
                lblError.Text    = "Session expired. Please re-select your question.";
                lblError.Visible = true;
                HideModal();
                return;
            }

            model.Title        = txtTitle.Text.Trim();
            model.QuestionText = txtQuestionText.Text.Trim();

            // Collect edited choices from the Repeater
            CollectChoicesFromRepeater(model);

            // Collect range values
            if (model.QuestionType == "NumericalRange")
            {
                model.RangeMin = txtRangeMin.Text.Trim();
                model.RangeMax = txtRangeMax.Text.Trim();
            }

            var service = new QuestionEditService();
            bool saved  = service.SaveQuestion(model);

            if (!saved)
            {
                lblError.Text    = "Save failed. The question file could not be written.";
                lblError.Visible = true;
                HideModal();
                return;
            }

            Response.Redirect("~/QuizOverview.aspx");
        }

        // Returns to QuizOverview without saving
        protected void Back_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/QuizOverview.aspx");
        }

        // Adds a new blank answer choice to the current question
        protected void AddAnswer_Click(object sender, EventArgs e)
        {
            var model = Session["QtiCurrentQuestion"] as QuestionModel;
            if (model == null) return;

            model.Title        = txtTitle.Text.Trim();
            model.QuestionText = txtQuestionText.Text.Trim();
            CollectChoicesFromRepeater(model);

            var service = new QuestionEditService();
            service.SaveQuestion(model);
            model.Choices = service.AddChoiceToXml(model);

            Session["QtiCurrentQuestion"] = model;
            choiceRepeater.DataSource = model.Choices;
            choiceRepeater.DataBind();
        }

        // Fires when a Remove button is clicked on an individual answer choice
        protected void choiceRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "RemoveChoice") return;

            var model = Session["QtiCurrentQuestion"] as QuestionModel;
            if (model == null) return;

            // Save current form state first
            model.Title        = txtTitle.Text.Trim();
            model.QuestionText = txtQuestionText.Text.Trim();
            CollectChoicesFromRepeater(model);

            var service = new QuestionEditService();
            service.SaveQuestion(model);
            model.Choices = service.RemoveChoiceFromXml(model, e.CommandArgument.ToString());

            Session["QtiCurrentQuestion"] = model;
            choiceRepeater.DataSource = model.Choices;
            choiceRepeater.DataBind();
        }

        // Deletes the current question entirely and returns to QuizOverview
        protected void DeleteQuestion_Click(object sender, EventArgs e)
        {
            string sessionId = Session["QtiSessionId"]  as string;
            string itemHref  = Session["QtiCurrentItem"] as string;

            var service = new QuestionEditService();
            service.DeleteQuestion(sessionId, itemHref, Server);

            Session["QtiCurrentItem"]     = null;
            Session["QtiCurrentQuestion"] = null;
            Response.Redirect("~/QuizOverview.aspx");
        }

        // Reads choice text and correct checkboxes from the Repeater into the model
        private void CollectChoicesFromRepeater(QuestionModel model)
        {
            if (model.QuestionType != "MultipleChoice" && model.QuestionType != "MultiSelect")
                return;

            for (int i = 0; i < choiceRepeater.Items.Count && i < model.Choices.Count; i++)
            {
                var item       = choiceRepeater.Items[i];
                var txtChoice  = item.FindControl("txtChoice")  as TextBox;
                var chkCorrect = item.FindControl("chkCorrect") as CheckBox;

                model.Choices[i].Text      = txtChoice?.Text.Trim() ?? model.Choices[i].Text;
                model.Choices[i].IsCorrect = chkCorrect?.Checked ?? false;
            }
        }

        // Injects a client-side call to hide the spinner modal on error
        private void HideModal()
        {
            ScriptManager.RegisterStartupScript(
                this, GetType(), "HideModal", "hideModal();", addScriptTags: true);
        }

        // Detects the question type.
        // Priority order: 1. File Upload, 2. Essay, 3. Numerical Range, 4. Choice based
        // Uses namespace-agnostic lookups (LocalName) to handle all QTI 2.2 packaging variants
        private QuestionType DetectQuestionType(XElement itemBody, string bodyText)
        {
            if (IsFileUploadQuestion(bodyText))
                return QuestionType.FileUpload;

            if (itemBody == null)
                return QuestionType.LongFormEssay;

            XElement choiceInteraction = itemBody.Descendants().FirstOrDefault(el => el.Name.LocalName == "choiceInteraction");
            XElement textEntry = itemBody.Descendants().FirstOrDefault(el => el.Name.LocalName == "textEntryInteraction");
            XElement extendedText = itemBody.Descendants().FirstOrDefault(el => el.Name.LocalName == "extendedTextInteraction");
            XElement uploadInteraction = itemBody.Descendants().FirstOrDefault(el => el.Name.LocalName == "uploadInteraction");

            if (extendedText != null) return QuestionType.LongFormEssay;
            if (uploadInteraction != null) return QuestionType.FileUpload;
            if (textEntry != null && HasNumericalRangeResponse(itemBody.Document?.Root))
                return QuestionType.NumericalRange;

            if (choiceInteraction != null)
            {
                int.TryParse((string)choiceInteraction.Attribute("maxChoices"), out int maxChoices);
                return maxChoices > 1 ? QuestionType.MultiSelect : QuestionType.MultipleChoice;
            }

            return QuestionType.Unknown;
        }

        private bool IsFileUploadQuestion(string bodyText)
        {
            return !string.IsNullOrEmpty(bodyText) && bodyText.IndexOf("Upload a file", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool HasNumericalRangeResponse(XElement assessmentItem)
        {
            if (assessmentItem == null) return false;

            string correctValue = assessmentItem.Descendants()
                .Where(el => el.Name.LocalName == "responseDeclaration")
                .SelectMany(rd => rd.Descendants().Where(cr => cr.Name.LocalName == "correctResponse"))
                .SelectMany(cr => cr.Elements().Where(v => v.Name.LocalName == "value"))
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
}