using System;
using System.IO;
using System.Web;

namespace QTI_Editor.WWW.Save
{
    // Deletes the session cache directory when a user session ends
    // Called from Global.asax.cs Session_End so no QTI data lingers past the session lifetime
    public class SessionCleanup
    {
        private const string CacheVirtualRoot = "~/cache/";

        // Deletes ~/cache/<sessionId>/ and all of its contents
        public void CleanSession(string sessionId, HttpServerUtility server)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            string cacheDirectory = server.MapPath(CacheVirtualRoot + sessionId);

            try
            {
                if (Directory.Exists(cacheDirectory))
                    Directory.Delete(cacheDirectory, recursive: true);
            }
            catch (Exception ex)
            {
                // Log but do not surface; session cleanup must never throw and interrupt application shutdown
                System.Diagnostics.Debug.WriteLine(
                    "[SessionCleanup] Failed to delete cache for session "
                    + sessionId + ": " + ex.Message);
            }
        }
    }
}
