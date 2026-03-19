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
            
            //Test if there is a file
            if (FileUpload1.FileName.Length == 0)
            {
                lblmessage.Text = "Please choose a ZIP file.";
                return;
            }

            //Test if file is a zip file
            string originalFile = Path.GetFileName(FileUpload1.FileName);
            if (!originalFile.EndsWith(".zip"))
            {
                lblmessage.Text = "Only .zip files are allowed";
                return ;
            }

            //Generate Session ID
            string sessionId = QTI_Editor.WWW.Services.SessionService.GenerateSession();

           
            //Create Cache Directory
            string cacheDirectory = Server.MapPath(CACHECONST + sessionId);
            Directory.CreateDirectory(cacheDirectory);

            //Copies contents from original zip to cache session zip
            string zipPath = Path.Combine(cacheDirectory, sessionId + ".zip");
            FileUpload1.SaveAs(zipPath);
            
            //Extracted zipPath method goes here

            //QTI verification boolean
            
        } 
    }
}