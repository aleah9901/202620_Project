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
            /*This if statement will test if the user selected a file.
             *If the user didn't select a file, the program will return with an error message.
             *The error message displays with a label controller on Upload.aspx.
             */
            if (FileUpload1.FileName.Length == 0)
            {
                lblmessage.Text = "Please choose a ZIP file.";
                return;
            }

            /*This if statement test if the user uploaded a ZIP file.
             * The Path.GetFileName() will store the name and path as string, from the System.IO namespace and Path class.
             * ref:https://learn.microsoft.com/en-us/dotnet/api/system.io.path?view=net-10.0
             * Then .EndsWith() will test if the file ends in .zip, from the System namespace and String class.
             * ref:https://learn.microsoft.com/en-us/dotnet/api/system.string?view=net-10.0
             * If the file does not end in .zip, the program will return with an error message.
             * The error message displays with a label controller on Upload.aspx.
             */
            string originalFile = Path.GetFileName(FileUpload1.FileName);
            if (!originalFile.EndsWith(".zip"))
            {
                lblmessage.Text = "Only .zip files are allowed";
                return ;
            }

            string sessionId = QTI_Editor.WWW.Services.SessionService.GenerateSession();

            /*
             * The previous method did not work.
             * Attempted to create a cache folder with generated session
             * Recieved Server Error
             * System.UnauthorizedAccessException: Access to the path 'cache\XmOghjDfs20260305_142544' is denied.
             * Would require user to grant ASP.NET access to a file.
             * 
             * The Directory.CreateDirectory did not work initially becuase it attempted to create a directory from the relative path
             * The path would look like C:\cache\sessionId
             * The OS would not let file be created on a protected system directory
             * 
             * Getting the full path is neccessary in order for the cache folder to be saved in the right place
             * Example: C:\Users\(USER)\source\repos\202620_Project\src\QTI_Editor.WWW\cache\(sessionId)
             * Path.GetFullPath does not for Server/web paths
             * Server.MapPath is needed to get the full path
             * ref: https://learn.microsoft.com/en-us/previous-versions/iis/6.0-sdk/ms524632(v=vs.90)
             */


            /*
             * This method creates a cache folder in the project directory.
             * In the cache folder, a folder named with the generate session name is made also.
             * We will use the folder as a temporary storage.
             * When the program is closed, We will have a method to delete this session.
             */
            string cacheDirectory = Server.MapPath(CACHECONST + sessionId);
            Directory.CreateDirectory(cacheDirectory);

            /*
             * This method combines the cacheDirectory with a .zip file named with the generated sessionId.
             * We will save the contents of the original ZIP file to the new one we made.
             * This will allow us to make changes to a temporary file and save to the original when complete.
             */
            string zipPath = Path.Combine(cacheDirectory, sessionId + ".zip");
            FileUpload1.SaveAs(zipPath);
            
            /*
             * From here, we need a extractZIP class and QTI-validator class.
             * If test are good, will redirect for load/edit questions page.
             */
        }
    }
}