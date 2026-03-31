using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QTI_Editor.WWW
{
    public partial class QuizOverview : System.Web.UI.Page
    {
        // This will send the correct cache folder to use that should have the qti file in it.
        private string SessionId => Request.QueryString["id"];
        private string SessionFolder => Server.MapPath("~/cache/" + SessionId);
        private string ExtractFolder => System.IO.Path.Combine(SessionFolder, "extract");

        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}