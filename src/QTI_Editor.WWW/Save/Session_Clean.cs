using System;
using System.IO;

namespace QTI_Editor.WWW
{
    //This class will delete cache session
    private class SessionClean
    {
        public class SessionClean
        {
            public static void DeleteSession(string sessionFolder)
            {
                //Deletes session folder if it is found.
                if (!string.IsNullOrEmpty(sessionFolder) && Directory.Exists(sessionFolder))
                {
                    Directory.Delete(sessionFolder, true);
                }

            }
        }
    }
}