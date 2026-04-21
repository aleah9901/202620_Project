using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace QTI_Editor.WWW
{
    // Holds the outcome of a QTI 1.2 validation pass.
    public class QTI_validation_result
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string ManifestPath { get; set; }
    }

    // Validates an extracted QTI 1.2 package by locating and parsing imsmanifest.xml.
    // Also verifies that the manifest contains QTI resource entries and that the
    // referenced item files exist on disk.
    public class QTI_verification
    {
        // Public entry point called by UploadService and Upload.aspx.cs.
        public QTI_validation_result Validate_QTI(string extractedFolderPath)
        {
            return Validate(extractedFolderPath);
        }

        // Validates the extracted folder for QTI 1.2 compliance:
        //   1. Folder path is not empty
        //   2. Folder exists on disk
        //   3. imsmanifest.xml is present
        //   4. imsmanifest.xml is readable XML
        //   5. Root element is "manifest"
        //   6. At least one resource element with a QTI type exists
        //   7. Referenced item files exist on disk
        // Returns a single QTI_validation_result with the outcome.
        public QTI_validation_result Validate(string extractedFolderPath)
        {
            var result = new QTI_validation_result();

            // 1. Check that the path is not empty
            if (string.IsNullOrWhiteSpace(extractedFolderPath))
            {
                result.IsValid = false;
                result.Message = "Validation failed: extracted folder path is empty.";
                return result;
            }

            // 2. Check that the folder exists
            if (!Directory.Exists(extractedFolderPath))
            {
                result.IsValid = false;
                result.Message = "Validation failed: extracted folder does not exist.";
                return result;
            }

            // 3. Locate imsmanifest.xml
            string[] manifestFiles = Directory.GetFiles(
                extractedFolderPath, "imsmanifest.xml", SearchOption.AllDirectories);

            if (manifestFiles.Length == 0)
            {
                // Fallback: look for bare QTI XML files (no manifest) and generate one.
                // A bare QTI XML has <questestinterop> as its root element.
                string generatedManifest = TryGenerateManifestFromBareXml(extractedFolderPath);
                if (generatedManifest == null)
                {
                    result.IsValid = false;
                    result.Message = "Validation failed: imsmanifest.xml was not found and no bare QTI XML files were detected.";
                    return result;
                }
                manifestFiles = new[] { generatedManifest };
            }

            // Simplified: assign directly to result.ManifestPath (#22)
            result.ManifestPath = manifestFiles[0];

            // 4. Attempt to read and parse the manifest
            XDocument manifestDoc;
            try
            {
                manifestDoc = XDocument.Load(result.ManifestPath);

                if (manifestDoc.Root == null)
                {
                    result.IsValid = false;
                    result.Message = "Validation failed: imsmanifest.xml is empty.";
                    return result;
                }

                // 5. Root element must be "manifest"
                if (!manifestDoc.Root.Name.LocalName.Equals("manifest", StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Message = "Validation failed: root element is not 'manifest'.";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = "Validation failed: imsmanifest.xml unable to read. " + ex.Message;
                return result;
            }

            // 6. Check that at least one <resource> with a QTI type exists.
            //    QTI 1.2 resource types vary by exporter. Common values include:
            //      "ims_qtiasiv1p2"  (Canvas, Respondus)
            //      "imsqti_xmlv1p2"
            //      "imsqti_item_xmlv1p2"
            //      "imsqti_test_xmlv1p2"
            //    We match on the substring "qti" to catch all variants.
            string manifestDir = Path.GetDirectoryName(result.ManifestPath);

            var resources = manifestDoc.Root
                .Descendants()
                .Where(el => el.Name.LocalName == "resource")
                .ToList();

            if (resources.Count == 0)
            {
                result.IsValid = false;
                result.Message = "Validation failed: no resource elements found in manifest.";
                return result;
            }

            // Filter to QTI-specific resources using broad "qti" substring match
            var qtiResources = resources
                .Where(r =>
                {
                    string type = (string)r.Attribute("type") ?? "";
                    return type.IndexOf("qti", StringComparison.OrdinalIgnoreCase) >= 0;
                })
                .ToList();

            if (qtiResources.Count == 0)
            {
                result.IsValid = false;
                result.Message = "Validation failed: no QTI resource entries found in manifest.";
                return result;
            }

            // 7. Verify that referenced item files exist on disk.
            //    QTI 1.2 packages may specify the file via the resource href attribute
            //    OR via child <file href="..."/> elements (or both).
            foreach (var resource in qtiResources)
            {
                // Check resource-level href
                string href = (string)resource.Attribute("href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    string itemPath = Path.Combine(manifestDir, href);
                    if (!File.Exists(itemPath))
                    {
                        result.IsValid = false;
                        result.Message = "Validation failed: referenced file '" + href + "' not found on disk.";
                        return result;
                    }
                }

                // Also check child <file> elements
                foreach (var fileEl in resource.Elements().Where(el => el.Name.LocalName == "file"))
                {
                    string fileHref = (string)fileEl.Attribute("href");
                    if (!string.IsNullOrWhiteSpace(fileHref))
                    {
                        string filePath = Path.Combine(manifestDir, fileHref);
                        if (!File.Exists(filePath))
                        {
                            result.IsValid = false;
                            result.Message = "Validation failed: referenced file '" + fileHref + "' not found on disk.";
                            return result;
                        }
                    }
                }
            }

            // All checks passed
            result.IsValid = true;
            result.Message = "QTI validation passed: manifest valid with "
                + qtiResources.Count + " QTI resource(s) verified.";
            return result;
        }

        // Scans the extracted folder for bare QTI XML files (root element = questestinterop)
        // and generates a synthetic imsmanifest.xml referencing each one.
        // Returns the path to the generated manifest, or null if no QTI XML files were found.
        private string TryGenerateManifestFromBareXml(string extractedFolderPath)
        {
            // Find all .xml files in the extracted folder
            string[] xmlFiles = Directory.GetFiles(extractedFolderPath, "*.xml", SearchOption.AllDirectories);
            var qtiFiles = new System.Collections.Generic.List<string>();

            foreach (string xmlFile in xmlFiles)
            {
                try
                {
                    XDocument doc = XDocument.Load(xmlFile);
                    if (doc.Root != null &&
                        doc.Root.Name.LocalName.Equals("questestinterop", StringComparison.OrdinalIgnoreCase))
                    {
                        qtiFiles.Add(xmlFile);
                    }
                }
                catch
                {
                    // Skip files that can't be parsed as XML
                }
            }

            if (qtiFiles.Count == 0)
                return null;

            // Build a synthetic imsmanifest.xml
            XNamespace cpNs = "http://www.imsglobal.org/xsd/imscp_v1p1";
            var resources = new XElement(cpNs + "resources");

            for (int i = 0; i < qtiFiles.Count; i++)
            {
                // Use a path relative to the extracted folder
                string relativePath = qtiFiles[i].Substring(extractedFolderPath.TrimEnd('\\').Length + 1)
                    .Replace("\\", "/");

                string resId = "RES_" + (i + 1);
                resources.Add(new XElement(cpNs + "resource",
                    new XAttribute("identifier", resId),
                    new XAttribute("type", "imsqti_xmlv1p2"),
                    new XAttribute("href", relativePath),
                    new XElement(cpNs + "file",
                        new XAttribute("href", relativePath))));
            }

            var manifest = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(cpNs + "manifest",
                    new XAttribute("identifier", "AUTO_GENERATED_MANIFEST"),
                    new XElement(cpNs + "organizations"),
                    resources));

            string manifestPath = Path.Combine(extractedFolderPath, "imsmanifest.xml");
            manifest.Save(manifestPath);

            return manifestPath;
        }
    }
}
