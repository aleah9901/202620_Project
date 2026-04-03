using System;
using System.IO;
using System.Web;
using System.Web.UI;
using QTI_Editor.WWW.Services;

namespace QTI_Editor.WWW
{
    // Code-behind for Upload.aspx
    // Responsibility: UI orchestration only
    // All file-system and QTI logic lives in Services/UploadService.cs
    public partial class Upload : System.Web.UI.Page
    {
        public void Process_ZIP(object sender, EventArgs e)
        {
            // UI guard: file must be selected
            if (FileUpload1.FileName.Length == 0)
            {
                lblmessage.Text = "Please choose a ZIP file.";
                HideModal();
                return;
            }

            // Delegate full pipeline to the service layer
            var service = new UploadService();
            var result  = service.ProcessUpload(
                FileUpload1.PostedFile.InputStream,
                Path.GetFileName(FileUpload1.FileName),
                Server);

            if (!result.Success)
            {
                lblmessage.Text = result.Message;
                HideModal();
                return;
            }

            // Store session ID for downstream pages
            Session["QtiSessionId"] = result.SessionId;

            // Redirect to the Quiz Overview editor
            Response.Redirect("~/QuizOverview.aspx");
        }

        // Injects a client-side call to hide the spinner modal
        // Used when a validation error keeps the user on this page
        private void HideModal()
        {
            ScriptManager.RegisterStartupScript(
                this, GetType(), "HideModal", "hideModal();", addScriptTags: true);
        }
    }
}