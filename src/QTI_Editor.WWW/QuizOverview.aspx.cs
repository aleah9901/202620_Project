using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using QTI_Editor.WWW.Save;

namespace QTI_Editor.WWW
{
    // Code-behind for QuizOverview.aspx
    // Loads the question list from the manifest and handles Export, quiz title, add/remove questions
    public partial class QuizOverview : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Redirect back to upload if there is no active session
            if (Session["QtiSessionId"] == null)
                Response.Redirect("~/Upload.aspx");

            if (!IsPostBack)
            {
                BindQuestionList();
                LoadQuizTitle();
            }
        }

        // Reads the quiz title from the manifest and populates the title input
        private void LoadQuizTitle()
        {
            string sessionId = Session["QtiSessionId"] as string;
            var service = new QuestionEditService();
            txtQuizTitle.Text = service.GetQuizTitle(sessionId, Server);
        }

        // Reads all question items from the manifest and binds them to the Repeater
        private void BindQuestionList()
        {
            string sessionId = Session["QtiSessionId"] as string;

            var service = new QuestionEditService();
            var items   = service.GetManifestItems(sessionId, Server);

            if (items.Count == 0)
            {
                lblOverviewError.Text    = "No question items were found in the manifest.";
                lblOverviewError.Visible = true;
            }

            questionList.DataSource = items;
            questionList.DataBind();
        }

        // Saves the quiz title to the manifest when the input loses focus
        protected void QuizTitle_Changed(object sender, EventArgs e)
        {
            string sessionId = Session["QtiSessionId"] as string;
            var service = new QuestionEditService();
            service.SetQuizTitle(sessionId, txtQuizTitle.Text.Trim(), Server);
        }

        // Fires when an Edit or Remove button is clicked in the question list
        protected void questionList_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            string href = e.CommandArgument.ToString();

            if (e.CommandName == "Select")
            {
                Session["QtiCurrentItem"] = href;
                Response.Redirect("~/QuestionEditor.aspx");
            }

            if (e.CommandName == "Remove")
            {
                string sessionId = Session["QtiSessionId"] as string;
                var service = new QuestionEditService();
                service.DeleteQuestion(sessionId, href, Server);
                BindQuestionList();
            }
        }

        // Creates a new question item, registers it in the manifest, and opens it for editing
        protected void AddQuestion_Click(object sender, EventArgs e)
        {
            string sessionId = Session["QtiSessionId"] as string;
            string title     = txtNewQuestionTitle.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                lblAddError.Text    = "Please enter a question title.";
                lblAddError.Visible = true;
                return;
            }

            var service = new QuestionEditService();
            string href = service.CreateNewQuestion(sessionId, title, Server);

            if (string.IsNullOrEmpty(href))
            {
                lblAddError.Text    = "Could not create question. Manifest not found.";
                lblAddError.Visible = true;
                return;
            }

            // Navigate directly to the new question in the editor
            Session["QtiCurrentItem"] = href;
            Response.Redirect("~/QuestionEditor.aspx");
        }

        // Re-packages the edited extracted content and streams it as a file download
        protected void Export_ZIP(object sender, EventArgs e)
        {
            string sessionId = Session["QtiSessionId"] as string;

            var service = new ExportService();
            var result  = service.ExportToZip(sessionId, Server);

            if (!result.Success)
            {
                ScriptManager.RegisterStartupScript(
                    this, GetType(), "HideModal",
                    $"hideModal(); alert('{EscapeJs(result.Message)}');",
                    addScriptTags: true);
                return;
            }

            Response.Clear();
            Response.ContentType = "application/zip";
            Response.AddHeader(
                "Content-Disposition",
                "attachment; filename=\"" + result.FileName + "\"");
            Response.TransmitFile(result.ZipPath);
            Response.Flush();
            Response.End();
        }

        // Cleans up text so it doesn't break the browser's popup message box
        private static string EscapeJs(string s)
        {
            return s?.Replace("\\", "\\\\")
                     .Replace("'", "\\'")
                     .Replace("\r", "")
                     .Replace("\n", "") ?? string.Empty;
        }
    }
}