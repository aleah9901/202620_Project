using System;
using System.IO;
using System.IO.Compression;
using System.Web;

namespace QTI_Editor.WWW.Services
{
    // Returned by UploadService so the code-behind can display status without knowing file-system details
    public class UploadResult
    {
        public bool   Success   { get; set; }
        public string SessionId { get; set; }
        public string Message   { get; set; }
    }

    // QTI ZIP import process:
    // 1. Extension guard (.zip only)
    // 2. 24-char session ID generation via SessionService
    // 3. Cache directory creation ~/cache/<sessionId>/
    // 4. Save raw ZIP ~/cache/<sessionId>/<sessionId>.zip
    // 5. Extract ZIP ~/cache/<sessionId>/extracted/
    // 6. QTI 1.2 validation via QtiValidationService
    // 7. On failure: delete session cache, return error result
    public class UploadService
    {
        private const string CacheVirtualRoot = "~/cache/";

        // Processes an uploaded file stream end-to-end
        // Returns an UploadResult indicating success or a failure reason
        public UploadResult ProcessUpload(Stream fileStream, string originalFileName,
                                          HttpServerUtility server)
        {
            // Extension guard
            if (!originalFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return Fail("Only .zip files are allowed.");
            }

            // Generate 24-char session ID
            string sessionId = SessionService.GenerateSession();

            // Create cache directory
            string cacheDirectory = server.MapPath(CacheVirtualRoot + sessionId);

            try
            {
                Directory.CreateDirectory(cacheDirectory);
            }
            catch (Exception ex)
            {
                return Fail("Could not create session cache directory. " + ex.Message);
            }

            // Save raw ZIP to cache
            string zipPath = Path.Combine(cacheDirectory, sessionId + ".zip");

            try
            {
                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                {
                    fileStream.CopyTo(fs);
                }
            }
            catch (Exception ex)
            {
                CleanupSession(cacheDirectory);
                return Fail("Could not save uploaded file. " + ex.Message);
            }

            // Extract ZIP to extracted/ subfolder
            string extractedPath = Path.Combine(cacheDirectory, "extracted");

            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractedPath);
            }
            catch (Exception ex)
            {
                CleanupSession(cacheDirectory);
                return Fail("Could not extract ZIP file. The file may be corrupt or password-protected. " + ex.Message);
            }

            // Run QTI 1.2 validation against the extracted content
            var validator = new QTI_verification();
            QTI_validation_result validation = validator.Validate_QTI(extractedPath);

            if (!validation.IsValid)
            {
                CleanupSession(cacheDirectory);
                return Fail(validation.Message);
            }

            return new UploadResult
            {
                Success   = true,
                SessionId = sessionId,
                Message   = "Upload successful. Session: " + sessionId
            };
        }

        // Returns a failed UploadResult with the given message
        private static UploadResult Fail(string message)
        {
            return new UploadResult { Success = false, Message = message };
        }

        // Deletes the entire session cache directory on any import process failure
        // so no partial data stays after the failed import
        private static void CleanupSession(string cacheDirectory)
        {
            try
            {
                if (Directory.Exists(cacheDirectory))
                    Directory.Delete(cacheDirectory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup: silently swallowed because the primary
                // operation already failed and we don't want to mask that error
            }
        }
    }
}
