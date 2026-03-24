using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QTI_Editor.WWW
{
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
                        Directory.Delete(cacheDirectory, true);
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
                    if (cacheDirectory != null && Directory.Exists(cacheDirectory))
                    {
                        Directory.Delete(cacheDirectory, true);
                    }
                }
            }
        }
    }
}
