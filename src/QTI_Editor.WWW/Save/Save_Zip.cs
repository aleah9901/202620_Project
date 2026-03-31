using System;
using System.IO;

//This class will build a ZIP file that saves the user edits
namespace QTI_Editor.WWW
{
    public static class SaveZipService
    {
        public static string BuildZip( string sessionFolder, string sessionID)
        {
            string extractPath = Path.Combine(sessionFolder, sessionID);
            string editedPath = Path.Combine(sessionFolder, sessionID, "edited");

            string zipPath = Path.Combine(sessionFolder, $"{sessionID}.zip");

        }
    }
}