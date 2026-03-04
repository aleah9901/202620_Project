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
             * The .Path.GetFileName() will store the file name as string, from the System.IO namespace and Path class.
             * ref:https://learn.microsoft.com/en-us/dotnet/api/system.io.path?view=net-10.0
             * Then .EndsWith() will test if the file ends in .zip, from the System namespace and String class.
             * ref:https://learn.microsoft.com/en-us/dotnet/api/system.string?view=net-10.0
             * If the file does not end in .zip, the program will return with an error message.
             * The error message displays with a label controller on Upload.aspx.
             */
            string orinialFile = Path.GetFileName(FileUpload1.FileName);
            if (!orinialFile.EndsWith(".zip"))
            {
                lblmessage.Text = "Only .zip files are allowed";
                return ;
            }
        }
    }
}