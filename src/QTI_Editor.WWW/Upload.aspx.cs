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
            const string CACHECONST = "~/cache/";
            string sessionId = null;
            string cacheDirectory = null;

            //Test if there is a file
            if (FileUpload1.FileName.Length == 0)
            {
                lblmessage.Text = "Please choose a ZIP file.";
            }

            //Test if file is a zip file
            else if (!Path.GetFileName(FileUpload1.FileName).EndsWith(".zip"))
            {
                lblmessage.Text = "Only .zip files are allowed.";
            }

            else
            {
                try
                {
                    //Generate Session ID
                    sessionId = QTI_Editor.WWW.Services.SessionService.GenerateSession();


                    //Create Cache Directory
                    cacheDirectory = Server.MapPath(CACHECONST + sessionId);
                    Directory.CreateDirectory(cacheDirectory);

                    //Copies contents from original zip to cache session zip
                    string zipPath = Path.Combine(cacheDirectory, sessionId + ".zip");
                    FileUpload1.SaveAs(zipPath);

                    //Extracted zipPath method goes here

                    //QTI verification
                    QTI_verification verifier = new QTI_verification();
                    QTI_validation_result validationrResult = verifier.Validate_QTI(extractPath);

                    //Delete directory if validation fails
                    if (!validationrResult.IsValid)
                    {
                        lblmessage.Text = validationrResult.Message;
                        //SessionClean class will exist when merge with Team 2
                        SessionClean.DeleteSession(cacheDirectory);
                    }

                    //Redirects to QuestionOverview
                    else
                    {
                        Response.Redirect("QuestionOverview.aspx?id=" + sessionId);
                    }
                }

                //Will display error message and clean created sessions
                catch (Exception ex)
                {
                    lblmessage.Text = "Processing failed:" + ex.Message;

                    //If session exist, it will be deleted.
                    //SessionClean class will exist when merge with Team 2
                    SessionClean.DeleteSession(cacheDirectory)
                }
            }
        }
    }
}
