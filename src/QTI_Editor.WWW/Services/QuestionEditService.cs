using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace QTI_Editor.WWW
{
    // Data object representing a single question item from the manifest.
    public class ManifestItem
    {
        public string Identifier { get; set; }
        public string Title { get; set; }
        public string Href { get; set; }
        public string Type { get; set; }
    }

    // Service for reading and modifying QTI 2.2 manifest and assessment items.
    // Used by QuizOverview.aspx.cs for listing, adding, and removing questions.
    public class QuestionEditService
    {
        // Returns the quiz title. Checks:
        // 1. organization > title in manifest
        // 2. The assessmentTest file's title attribute (referenced by the test resource)
        // 3. Manifest identifier attribute
        // Falls back to "Untitled Quiz" if none found.
        public string GetQuizTitle(string sessionId, HttpServerUtility server)
        {
            XDocument manifest = LoadManifest(sessionId, server);
            if (manifest == null) return string.Empty;

            // 1. Look for <organizations><organization><title>
            XElement orgTitle = manifest.Root
                .Descendants()
                .Where(el => el.Name.LocalName == "organization")
                .SelectMany(org => org.Elements())
                .FirstOrDefault(el => el.Name.LocalName == "title");

            if (orgTitle != null && !string.IsNullOrWhiteSpace(orgTitle.Value))
                return orgTitle.Value.Trim();

            // 2. Look for the assessmentTest file and read its title attribute
            string manifestPath = GetManifestPath(sessionId, server);
            if (manifestPath != null)
            {
                string manifestDir = Path.GetDirectoryName(manifestPath);
                var testResource = manifest.Root
                    .Descendants()
                    .FirstOrDefault(el => el.Name.LocalName == "resource"
                        && ((string)el.Attribute("type") ?? "").IndexOf("imsqti_test", StringComparison.OrdinalIgnoreCase) >= 0);

                if (testResource != null)
                {
                    string testHref = (string)testResource.Attribute("href");
                    if (!string.IsNullOrEmpty(testHref))
                    {
                        string testPath = Path.Combine(manifestDir, testHref);
                        if (File.Exists(testPath))
                        {
                            try
                            {
                                XDocument testDoc = XDocument.Load(testPath);
                                string testTitle = (string)testDoc.Root?.Attribute("title");
                                if (!string.IsNullOrWhiteSpace(testTitle))
                                    return testTitle.Trim();
                            }
                            catch { /* ignore unreadable test files */ }
                        }
                    }
                }
            }

            // 3. Fallback: manifest identifier
            string manifestId = (string)manifest.Root.Attribute("identifier");
            if (!string.IsNullOrEmpty(manifestId) && manifestId != "MANIFEST-QTI-TEST-TITLE")
                return manifestId.Trim();

            return "Untitled Quiz";
        }

        // Sets the quiz title. Writes to:
        // 1. The assessmentTest file's title attribute (if it exists)
        // 2. Creates organization > title in manifest if no test file exists
        public void SetQuizTitle(string sessionId, string title, HttpServerUtility server)
        {
            string manifestPath = GetManifestPath(sessionId, server);
            if (manifestPath == null) return;

            string manifestDir = Path.GetDirectoryName(manifestPath);
            XDocument manifest = XDocument.Load(manifestPath);

            // 1. Try to write to the assessmentTest file
            var testResource = manifest.Root
                .Descendants()
                .FirstOrDefault(el => el.Name.LocalName == "resource"
                    && ((string)el.Attribute("type") ?? "").IndexOf("imsqti_test", StringComparison.OrdinalIgnoreCase) >= 0);

            if (testResource != null)
            {
                string testHref = (string)testResource.Attribute("href");
                if (!string.IsNullOrEmpty(testHref))
                {
                    string testPath = Path.Combine(manifestDir, testHref);
                    if (File.Exists(testPath))
                    {
                        try
                        {
                            XDocument testDoc = XDocument.Load(testPath);
                            testDoc.Root.SetAttributeValue("title", title);
                            testDoc.Save(testPath);
                            return;
                        }
                        catch { /* fall through to manifest approach */ }
                    }
                }
            }

            // 2. Fallback: write to organization > title in manifest
            XElement orgTitle = manifest.Root
                .Descendants()
                .Where(el => el.Name.LocalName == "organization")
                .SelectMany(org => org.Elements())
                .FirstOrDefault(el => el.Name.LocalName == "title");

            if (orgTitle != null)
            {
                orgTitle.Value = title;
            }
            else
            {
                // Create organization structure if empty <organizations/>
                XElement orgs = manifest.Root.Descendants()
                    .FirstOrDefault(el => el.Name.LocalName == "organizations");
                if (orgs != null)
                {
                    var orgElement = new XElement(orgs.Name.Namespace + "organization",
                        new XAttribute("identifier", "ORG-1"),
                        new XElement(orgs.Name.Namespace + "title", title));
                    orgs.Add(orgElement);
                }
            }
            manifest.Save(manifestPath);
        }

        // Returns only item resources from the manifest (filters out test resources).
        // Per QTI 2.2, items have type="imsqti_item_xmlv2p2" or "imsqti_item_xmlv2p1".
        public List<ManifestItem> GetManifestItems(string sessionId, HttpServerUtility server)
        {
            var items = new List<ManifestItem>();

            XDocument manifest = LoadManifest(sessionId, server);
            if (manifest == null) return items;

            string manifestPath = GetManifestPath(sessionId, server);
            string manifestDir = manifestPath != null ? Path.GetDirectoryName(manifestPath) : null;

            // Look for <resource> elements with item type only (not test type)
            var resources = manifest.Root
                .Descendants()
                .Where(el => el.Name.LocalName == "resource"
                    && ((string)el.Attribute("type") ?? "").IndexOf("imsqti_item", StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (XElement res in resources)
            {
                string href = (string)res.Attribute("href") ?? string.Empty;
                string type = (string)res.Attribute("type") ?? string.Empty;
                string identifier = (string)res.Attribute("identifier") ?? string.Empty;

                // Get display title: prefer question text from <p> in itemBody for usability
                string title = identifier;

                if (manifestDir != null && !string.IsNullOrEmpty(href))
                {
                    string itemPath = Path.Combine(manifestDir, href);
                    if (File.Exists(itemPath))
                    {
                        try
                        {
                            XDocument itemDoc = XDocument.Load(itemPath);
                            XElement itemRoot = itemDoc.Root;

                            // Try to get question text from the first <p> in itemBody
                            string questionText = null;
                            XElement body = itemRoot.Elements()
                                .FirstOrDefault(el => el.Name.LocalName == "itemBody");
                            if (body != null)
                            {
                                XElement firstP = body.Elements()
                                    .FirstOrDefault(el => el.Name.LocalName == "p");
                                if (firstP != null && !string.IsNullOrWhiteSpace(firstP.Value))
                                    questionText = firstP.Value.Trim();
                            }

                            // Use question text if available, otherwise fall back to title attribute
                            if (!string.IsNullOrEmpty(questionText))
                                title = questionText;
                            else
                            {
                                string itemTitle = (string)itemRoot.Attribute("title");
                                if (!string.IsNullOrEmpty(itemTitle))
                                    title = itemTitle.Trim();
                            }
                        }
                        catch { /* use identifier as fallback */ }
                    }
                }

                items.Add(new ManifestItem
                {
                    Identifier = identifier,
                    Title = title,
                    Href = href,
                    Type = type
                });
            }

            return items;
        }

        // Removes a question resource from the manifest and deletes the item XML file.
        public void DeleteQuestion(string sessionId, string href, HttpServerUtility server)
        {
            string manifestPath = GetManifestPath(sessionId, server);
            if (manifestPath == null) return;

            XDocument manifest = XDocument.Load(manifestPath);
            string manifestDir = Path.GetDirectoryName(manifestPath);

            var resource = manifest.Root
                .Descendants()
                .FirstOrDefault(el => el.Name.LocalName == "resource"
                    && (string)el.Attribute("href") == href);

            if (resource != null)
            {
                // Also remove any <dependency> referencing this resource
                string resId = (string)resource.Attribute("identifier");
                if (!string.IsNullOrEmpty(resId))
                {
                    var deps = manifest.Root
                        .Descendants()
                        .Where(el => el.Name.LocalName == "dependency"
                            && (string)el.Attribute("identifierref") == resId)
                        .ToList();
                    foreach (var dep in deps)
                        dep.Remove();
                }

                resource.Remove();
                manifest.Save(manifestPath);
            }

            // Delete the actual file
            string filePath = Path.Combine(manifestDir, href);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        // Creates a new assessment item XML file and registers it in the manifest.
        // Generates valid QTI 2.2 with the correct interaction element based on questionType.
        // Returns the href of the new item, or null on failure.
        public string CreateNewQuestion(string sessionId, string title, string questionType, HttpServerUtility server)
        {
            string manifestPath = GetManifestPath(sessionId, server);
            if (manifestPath == null) return null;

            string manifestDir = Path.GetDirectoryName(manifestPath);

            // Check if an items/ subdirectory exists (common QTI package pattern)
            string itemsDir = Path.Combine(manifestDir, "items");
            string subDir = Directory.Exists(itemsDir) ? "items" : "";

            string identifier = "item_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string fileName = identifier + ".xml";
            string relativeHref = string.IsNullOrEmpty(subDir) ? fileName : subDir + "/" + fileName;
            string filePath = Path.Combine(manifestDir, relativeHref);

            // Ensure directory exists
            string fileDir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(fileDir))
                Directory.CreateDirectory(fileDir);

            // Build the QTI 2.2 assessment item
            XNamespace qtiNs = "http://www.imsglobal.org/xsd/imsqti_v2p2";

            // Determine responseDeclaration baseType and cardinality based on question type
            string baseType = "identifier";
            string cardinality = "single";
            string rpTemplate = "http://www.imsglobal.org/question/qti_v2p2/rptemplates/match_correct";

            if (questionType == "MultiSelect")
            {
                cardinality = "multiple";
                rpTemplate = "http://www.imsglobal.org/question/qti_v2p2/rptemplates/map_response";
            }
            else if (questionType == "ShortAnswer")
            {
                baseType = "string";
                rpTemplate = "http://www.imsglobal.org/question/qti_v2p2/rptemplates/map_response";
            }
            else if (questionType == "LongFormEssay" || questionType == "FileUpload")
            {
                baseType = questionType == "FileUpload" ? "file" : "string";
                rpTemplate = null; // No automated scoring
            }

            // Build the root assessmentItem
            var root = new XElement(qtiNs + "assessmentItem",
                new XAttribute("identifier", identifier),
                new XAttribute("title", title),
                new XAttribute("adaptive", "false"),
                new XAttribute("timeDependent", "false"));

            // responseDeclaration
            root.Add(new XElement(qtiNs + "responseDeclaration",
                new XAttribute("identifier", "RESPONSE"),
                new XAttribute("cardinality", cardinality),
                new XAttribute("baseType", baseType)));

            // outcomeDeclaration
            root.Add(new XElement(qtiNs + "outcomeDeclaration",
                new XAttribute("identifier", "SCORE"),
                new XAttribute("cardinality", "single"),
                new XAttribute("baseType", "float"),
                new XElement(qtiNs + "defaultValue",
                    new XElement(qtiNs + "value", "0"))));

            // itemBody with the correct interaction per QTI 2.2 spec
            var itemBody = new XElement(qtiNs + "itemBody",
                new XElement(qtiNs + "p", "Enter question text here."));

            switch (questionType)
            {
                case "MultipleChoice":
                    itemBody.Add(new XElement(qtiNs + "choiceInteraction",
                        new XAttribute("responseIdentifier", "RESPONSE"),
                        new XAttribute("shuffle", "false"),
                        new XAttribute("maxChoices", "1"),
                        new XElement(qtiNs + "simpleChoice",
                            new XAttribute("identifier", "CHOICE-A"), "Option A"),
                        new XElement(qtiNs + "simpleChoice",
                            new XAttribute("identifier", "CHOICE-B"), "Option B")));
                    break;

                case "MultiSelect":
                    itemBody.Add(new XElement(qtiNs + "choiceInteraction",
                        new XAttribute("responseIdentifier", "RESPONSE"),
                        new XAttribute("shuffle", "false"),
                        new XAttribute("maxChoices", "0"),
                        new XElement(qtiNs + "simpleChoice",
                            new XAttribute("identifier", "CHOICE-A"), "Option A"),
                        new XElement(qtiNs + "simpleChoice",
                            new XAttribute("identifier", "CHOICE-B"), "Option B")));
                    break;

                case "ShortAnswer":
                    itemBody.Add(new XElement(qtiNs + "textEntryInteraction",
                        new XAttribute("responseIdentifier", "RESPONSE"),
                        new XAttribute("expectedLength", "100")));
                    break;

                case "LongFormEssay":
                    itemBody.Add(new XElement(qtiNs + "extendedTextInteraction",
                        new XAttribute("responseIdentifier", "RESPONSE"),
                        new XAttribute("expectedLines", "10")));
                    break;

                case "FileUpload":
                    itemBody.Add(new XElement(qtiNs + "uploadInteraction",
                        new XAttribute("responseIdentifier", "RESPONSE")));
                    break;

                case "NumericalRange":
                    itemBody.Add(new XElement(qtiNs + "textEntryInteraction",
                        new XAttribute("responseIdentifier", "RESPONSE"),
                        new XAttribute("expectedLength", "20")));
                    break;
            }

            root.Add(itemBody);

            // responseProcessing (per spec: template URI for auto-scored, empty for manual)
            if (rpTemplate != null)
                root.Add(new XElement(qtiNs + "responseProcessing",
                    new XAttribute("template", rpTemplate)));
            else
                root.Add(new XElement(qtiNs + "responseProcessing"));

            var itemDoc = new XDocument(root);
            itemDoc.Save(filePath);

            // Register in the manifest
            XDocument manifest = XDocument.Load(manifestPath);
            XElement resources = manifest.Root
                .Descendants()
                .FirstOrDefault(el => el.Name.LocalName == "resources");

            if (resources != null)
            {
                XElement newResource = new XElement(
                    resources.Name.Namespace + "resource",
                    new XAttribute("identifier", identifier),
                    new XAttribute("type", "imsqti_item_xmlv2p2"),
                    new XAttribute("href", relativeHref),
                    new XElement(resources.Name.Namespace + "file",
                        new XAttribute("href", relativeHref)));
                resources.Add(newResource);

                // Add dependency to the test resource if one exists
                var testResource = resources.Elements()
                    .FirstOrDefault(el => el.Name.LocalName == "resource"
                        && ((string)el.Attribute("type") ?? "").IndexOf("imsqti_test", StringComparison.OrdinalIgnoreCase) >= 0);
                if (testResource != null)
                {
                    testResource.Add(new XElement(
                        resources.Name.Namespace + "dependency",
                        new XAttribute("identifierref", identifier)));
                }

                manifest.Save(manifestPath);
            }

            return relativeHref;
        }


        // Locates the imsmanifest.xml inside the extracted folder for the session
        private string GetManifestPath(string sessionId, HttpServerUtility server)
        {
            if (string.IsNullOrEmpty(sessionId)) return null;

            string extractDir = server.MapPath("~/cache/" + sessionId + "/extracted");
            if (!Directory.Exists(extractDir)) return null;

            string[] files = Directory.GetFiles(extractDir, "imsmanifest.xml", SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : null;
        }

        private XDocument LoadManifest(string sessionId, HttpServerUtility server)
        {
            string path = GetManifestPath(sessionId, server);
            if (path == null) return null;

            try { return XDocument.Load(path); }
            catch { return null; }
        }
    }
}
