using System;
using System.IO;
using System.IO.Compression;
using System.Web;

namespace QTI_Editor.WWW.Save
{
    // Returned by ExportService so the code-behind knows whether the export ZIP is ready
    public class ExportResult
    {
        public bool   Success  { get; set; }
        public string ZipPath  { get; set; }
        public string FileName { get; set; }
        public string Message  { get; set; }
    }

    // Handles "Export from cache": re-packages the edited extracted content
    // under ~/cache/<sessionId>/extracted/ back into a downloadable ZIP file
    // stored at ~/cache/<sessionId>/<sessionId>_export.zip
    public class ExportService
    {
        private const string CacheVirtualRoot = "~/cache/";

        // Re-zips the edited extracted content for a given session
        // Returns an ExportResult with the physical path of the ready-to-stream ZIP
        public ExportResult ExportToZip(string sessionId, HttpServerUtility server)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Fail("No active session. Please upload a QTI file first.");

            string cacheDirectory = server.MapPath(CacheVirtualRoot + sessionId);
            string extractedPath  = Path.Combine(cacheDirectory, "extracted");

            // Guard: extracted directory must exist
            if (!Directory.Exists(extractedPath))
                return Fail("Extracted content not found for session: " + sessionId);

            string exportFileName = sessionId + "_export.zip";
            string exportZipPath  = Path.Combine(cacheDirectory, exportFileName);

            // Remove any previous export before recreating
            try
            {
                if (File.Exists(exportZipPath))
                    File.Delete(exportZipPath);

                ZipFile.CreateFromDirectory(
                    extractedPath,
                    exportZipPath,
                    CompressionLevel.Fastest,
                    includeBaseDirectory: false);
            }
            catch (Exception ex)
            {
                return Fail("Could not create export ZIP. " + ex.Message);
            }

            return new ExportResult
            {
                Success  = true,
                ZipPath  = exportZipPath,
                FileName = exportFileName,
                Message  = "Export ready: " + exportFileName
            };
        }

        // Returns a failed ExportResult with the given message
        private static ExportResult Fail(string message)
        {
            return new ExportResult { Success = false, Message = message };
        }
    }
}
