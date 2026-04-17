using System;
using System.Web.UI;
using QTI_Editor.WWW.Services;

namespace QTI_Editor.WWW
{
    // Code-behind for Upload.aspx
    // Delegates all file-system and QTI logic to Services/UploadService.cs
    public partial class Upload : System.Web.UI.Page
    {
        public void Process_ZIP(object sender, EventArgs e)
        {
            // Test if there is a file
            if (FileUpload1.FileName.Length == 0)
            {
                lblmessage.Text = "Please choose a ZIP file.";
                return;
            }

            // Test if file is a zip file
            if (!FileUpload1.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                lblmessage.Text = "Only .zip files are allowed.";
                return;
            }

            var service = new UploadService();
            UploadResult result = service.ProcessUpload(
                FileUpload1.PostedFile.InputStream,
                FileUpload1.FileName,
                Server);

            if (!result.Success)
            {
                lblmessage.Text = result.Message;
                return;
            }

            Session["QtiSessionId"] = result.SessionId;
            Response.Redirect("QuizOverview.aspx?id=" + result.SessionId);
        }
    }
}
