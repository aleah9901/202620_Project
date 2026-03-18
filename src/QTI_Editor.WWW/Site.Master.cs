using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QTI_Editor.WWW
{
    public partial class Site : System.Web.UI.MasterPage
    {
        
        protected void Page_Load(object sender, EventArgs e)
        {
            // had to create a directory. if this works how I think it works it should be created when the site first loads

            //I'm gonna do some writelines to see if it actually works
            Console.WriteLine("Hello World!");
            string direc = @"C:\direc";
            Directory.CreateDirectory(direc);

           
        }
    }
}