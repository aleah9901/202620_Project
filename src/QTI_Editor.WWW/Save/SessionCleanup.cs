using System;
using System.IO;
using System.Web;

namespace QTI_Editor.WWW
{
    // Cleans up session cache directories.
    // Used by Global.asax.cs Session_End to remove QTI data when a session expires.
    public class SessionCleanup
    {
        // Deletes the cache directory for the given session ID.
        public void CleanSession(string sessionId, HttpServerUtility server)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return;

            string cacheDir = server.MapPath("~/cache/" + sessionId);

            if (Directory.Exists(cacheDir))
            {
                try
                {
                    Directory.Delete(cacheDir, true);
                }
                catch
                {
                    // Best-effort cleanup on session expiry
                }
            }
        }
    }
}
