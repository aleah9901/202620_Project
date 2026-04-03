using System;
using System.IO;
using System.Xml.Linq;

namespace QTI_Editor.WWW.Services
{
    // Holds the result of a QTI 2.2 validation pass
    public class QtiValidationResult
    {
        public bool   IsValid      { get; set; }
        public string Message      { get; set; }
        public string ManifestPath { get; set; }
    }

    // Validates that an extracted QTI 2.2 ZIP package is structurally sound
    // All checks are holistic; the first failure short-circuits and returns immediately
    public class QtiValidationService
    {
        public QtiValidationResult Validate_QTI(string extractedFolderPath)
        {
            var result = new QtiValidationResult
            {
                IsValid = true,
                Message = "QTI validation passed: imsmanifest.xml found and successfully read."
            };

            // Guard: path must be non-empty
            if (string.IsNullOrWhiteSpace(extractedFolderPath))
            {
                result.IsValid = false;
                result.Message = "Validation failed: extracted folder path is empty.";
                return result;
            }

            // Guard: directory must exist
            if (!Directory.Exists(extractedFolderPath))
            {
                result.IsValid = false;
                result.Message = "Validation failed: extracted folder does not exist.";
                return result;
            }

            // Locate imsmanifest.xml (required by QTI 2.2 spec)
            string[] manifestFiles = Directory.GetFiles(
                extractedFolderPath, "imsmanifest.xml", SearchOption.AllDirectories);

            if (manifestFiles.Length == 0)
            {
                result.IsValid = false;
                result.Message = "Validation failed: imsmanifest.xml was not found.";
                return result;
            }

            string manifestPath   = manifestFiles[0];
            result.ManifestPath   = manifestPath;

            // Parse and inspect the manifest
            try
            {
                XDocument manifestDoc = XDocument.Load(manifestPath);

                if (manifestDoc.Root == null)
                {
                    result.IsValid = false;
                    result.Message = "Validation failed: imsmanifest.xml is empty.";
                    return result;
                }

                // QTI 2.2 spec §4.1: root element MUST be <manifest>
                if (!manifestDoc.Root.Name.LocalName.Equals(
                        "manifest", StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Message = "Validation failed: root element is not <manifest>.";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = "Validation failed: imsmanifest.xml could not be read. " + ex.Message;
                return result;
            }

            return result;
        }
    }
}
