using System;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using QTI_Editor.WWW.Save;

namespace QTI_Editor.WWW
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
        }

        // Fires when a session expires or is abandoned
        // Deletes the session's cache directory so no QTI data lingers past the session lifetime
        // Note: only fires automatically when sessionState mode="InProc" (the project default)
        protected void Session_End(object sender, EventArgs e)
        {
            string sessionId = Session["QtiSessionId"] as string;

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                var cleanup = new SessionCleanup();
                cleanup.CleanSession(sessionId, Server);
            }
        }
    }
}