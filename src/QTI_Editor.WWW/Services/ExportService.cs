using System;
using System.IO;
using System.IO.Compression;
using System.Web;

namespace QTI_Editor.WWW
{
    // Result returned by ExportService.ExportToZip.
    public class ExportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
        public string ZipPath { get; set; }
    }

    // Packages the edited QTI content back into a downloadable ZIP file.
    // Used by QuizOverview.aspx.cs Export_ZIP handler.
    public class ExportService
    {
        // Re-packages the extracted (and potentially edited) content into a ZIP.
        // Returns an ExportResult with the path to the new ZIP file.
        public ExportResult ExportToZip(string sessionId, HttpServerUtility server)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return new ExportResult { Success = false, Message = "No active session." };
            }

            string cacheDir = server.MapPath("~/cache/" + sessionId);
            string extractDir = Path.Combine(cacheDir, "extracted");

            if (!Directory.Exists(extractDir))
            {
                return new ExportResult { Success = false, Message = "Extracted content not found." };
            }

            string exportFileName = sessionId + "_export.zip";
            string exportPath = Path.Combine(cacheDir, exportFileName);

            try
            {
                // Remove any previous export
                if (File.Exists(exportPath))
                    File.Delete(exportPath);

                ZipFile.CreateFromDirectory(extractDir, exportPath);

                return new ExportResult
                {
                    Success  = true,
                    Message  = "Export successful.",
                    FileName = exportFileName,
                    ZipPath  = exportPath
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Success = false,
                    Message = "Export failed: " + ex.Message
                };
            }
        }
    }
}
