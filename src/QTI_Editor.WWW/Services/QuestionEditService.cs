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

    // Service for reading and modifying QTI 1.2 manifest and assessment items.
    // QTI 1.2 uses <questestinterop> as the root element with <item> children
    // nested under optional <assessment><section> wrappers.
    // Used by QuizOverview.aspx.cs for listing, adding, and removing questions.
    public class QuestionEditService
    {
        // Returns the quiz title. Checks:
        // 1. organization > title in manifest
        // 2. LOM metadata > general > title > string in manifest
        // 3. The assessment title attribute inside the QTI XML file
        // 4. Manifest identifier attribute
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

            // 2. Look for LOM metadata > general > title > string
            //    QTI 1.2 manifests often store the title in <imsmd:lom><imsmd:general><imsmd:title><imsmd:string>
            XElement metadataTitle = manifest.Root
                .Descendants()
                .Where(el => el.Name.LocalName == "general")
                .SelectMany(g => g.Elements().Where(el => el.Name.LocalName == "title"))
                .SelectMany(t => t.Elements().Where(el => el.Name.LocalName == "string"))
                .FirstOrDefault();

            if (metadataTitle != null && !string.IsNullOrWhiteSpace(metadataTitle.Value))
                return metadataTitle.Value.Trim();

            // 3. Look for the assessment element inside a QTI XML file and read its title
            string manifestPath = GetManifestPath(sessionId, server);
            if (manifestPath != null)
            {
                string manifestDir = Path.GetDirectoryName(manifestPath);
                var qtiResource = manifest.Root
                    .Descendants()
                    .FirstOrDefault(el => el.Name.LocalName == "resource"
                        && ((string)el.Attribute("type") ?? "").IndexOf("qti", StringComparison.OrdinalIgnoreCase) >= 0);

                if (qtiResource != null)
                {
                    string qtiHref = (string)qtiResource.Attribute("href");
                    if (!string.IsNullOrEmpty(qtiHref))
                    {
                        string qtiPath = Path.Combine(manifestDir, qtiHref);
                        if (File.Exists(qtiPath))
                        {
                            try
                            {
                                XDocument qtiDoc = XDocument.Load(qtiPath);
                                // Look for <assessment title="...">
                                XElement assessment = qtiDoc.Descendants()
                                    .FirstOrDefault(el => el.Name.LocalName == "assessment");
                                if (assessment != null)
                                {
                                    string assessTitle = (string)assessment.Attribute("title");
                                    if (!string.IsNullOrWhiteSpace(assessTitle))
                                        return assessTitle.Trim();
                                }
                            }
                            catch { /* ignore unreadable files */ }
                        }
                    }
                }
            }

            // 4. Fallback: manifest identifier
            string manifestId = (string)manifest.Root.Attribute("identifier");
            if (!string.IsNullOrEmpty(manifestId) && manifestId != "MANIFEST-QTI-TEST-TITLE")
                return manifestId.Trim();

            return "Untitled Quiz";
        }

        // Sets the quiz title. Writes to:
        // 1. LOM metadata > general > title > string in manifest
        // 2. The <assessment title="..."> attribute in the QTI XML file
        // 3. Creates organization > title in manifest if no other location exists
        public void SetQuizTitle(string sessionId, string title, HttpServerUtility server)
        {
            string manifestPath = GetManifestPath(sessionId, server);
            if (manifestPath == null) return;

            string manifestDir = Path.GetDirectoryName(manifestPath);
            XDocument manifest = XDocument.Load(manifestPath);

            // 1. Try to write to LOM metadata > general > title > string
            XElement metadataTitle = manifest.Root
                .Descendants()
                .Where(el => el.Name.LocalName == "general")
                .SelectMany(g => g.Elements().Where(el => el.Name.LocalName == "title"))
                .SelectMany(t => t.Elements().Where(el => el.Name.LocalName == "string"))
                .FirstOrDefault();

            if (metadataTitle != null)
            {
                metadataTitle.Value = title;
                manifest.Save(manifestPath);
            }

            // 2. Try to write to the assessment element in the QTI XML file
            var qtiResource = manifest.Root
                .Descendants()
                .FirstOrDefault(el => el.Name.LocalName == "resource"
                    && ((string)el.Attribute("type") ?? "").IndexOf("qti", StringComparison.OrdinalIgnoreCase) >= 0);

            if (qtiResource != null)
            {
                string qtiHref = (string)qtiResource.Attribute("href");
                if (!string.IsNullOrEmpty(qtiHref))
                {
                    string qtiPath = Path.Combine(manifestDir, qtiHref);
                    if (File.Exists(qtiPath))
                    {
                        try
                        {
                            XDocument qtiDoc = XDocument.Load(qtiPath);
                            XElement assessment = qtiDoc.Descendants()
                                .FirstOrDefault(el => el.Name.LocalName == "assessment");
                            if (assessment != null)
                            {
                                assessment.SetAttributeValue("title", title);
                                qtiDoc.Save(qtiPath);
                                return;
                            }
                        }
                        catch { /* fall through to manifest approach */ }
                    }
                }
            }

            // 3. Fallback: write to organization > title in manifest
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

        // Returns all question item elements from the QTI 1.2 XML files.
        // QTI 1.2 often stores multiple items in a single XML file under
        // <questestinterop><assessment><section><item>... or directly as
        // <questestinterop><item>...
        // Each <item> becomes a ManifestItem with Href = "filename.xml#ITEM_IDENT"
        public List<ManifestItem> GetManifestItems(string sessionId, HttpServerUtility server)
        {
            var items = new List<ManifestItem>();

            XDocument manifest = LoadManifest(sessionId, server);
            if (manifest == null) return items;

            string manifestPath = GetManifestPath(sessionId, server);
            string manifestDir = manifestPath != null ? Path.GetDirectoryName(manifestPath) : null;

            // Find all <resource> elements with QTI type (broad "qti" substring match)
            var resources = manifest.Root
                .Descendants()
                .Where(el => el.Name.LocalName == "resource"
                    && ((string)el.Attribute("type") ?? "").IndexOf("qti", StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (XElement res in resources)
            {
                string resHref = (string)res.Attribute("href") ?? string.Empty;
                string type = (string)res.Attribute("type") ?? string.Empty;

                if (manifestDir == null || string.IsNullOrEmpty(resHref))
                    continue;

                string xmlPath = Path.Combine(manifestDir, resHref);
                if (!File.Exists(xmlPath))
                    continue;

                try
                {
                    XDocument qtiDoc = XDocument.Load(xmlPath);

                    // Find all <item> elements at any depth (handles assessment/section nesting)
                    var itemElements = qtiDoc.Descendants()
                        .Where(el => el.Name.LocalName == "item")
                        .ToList();

                    foreach (XElement itemEl in itemElements)
                    {
                        string ident = (string)itemEl.Attribute("ident") ?? "";
                        string itemTitle = (string)itemEl.Attribute("title") ?? ident;

                        // Try to get question text from <presentation><material><mattext>
                        string questionText = null;
                        XElement presentation = itemEl.Elements()
                            .FirstOrDefault(el => el.Name.LocalName == "presentation");
                        if (presentation != null)
                        {
                            XElement mattext = presentation.Descendants()
                                .FirstOrDefault(el => el.Name.LocalName == "mattext");
                            if (mattext != null && !string.IsNullOrWhiteSpace(mattext.Value))
                                questionText = mattext.Value.Trim();
                        }

                        // Use question text for display if available, else item title
                        string displayTitle = !string.IsNullOrEmpty(questionText)
                            ? questionText
                            : itemTitle;

                        // Href uses compound key: filename#ident for multi-item files
                        string compoundHref = resHref + "#" + ident;

                        items.Add(new ManifestItem
                        {
                            Identifier = ident,
                            Title = displayTitle,
                            Href = compoundHref,
                            Type = type
                        });
                    }
                }
                catch { /* skip unreadable files */ }
            }

            return items;
        }

        // Removes a question item from the QTI XML file and cleans up.
        // QTI 1.2 may have multiple items in one file, so we remove just the
        // matching <item> element. If the file then has no items, we remove
        // the resource from the manifest and delete the file.
        public void DeleteQuestion(string sessionId, string href, HttpServerUtility server)
        {
            string manifestPath = GetManifestPath(sessionId, server);
            if (manifestPath == null) return;

            string manifestDir = Path.GetDirectoryName(manifestPath);

            // Parse compound href: "filename.xml#ITEM_IDENT"
            string filePart = href;
            string itemIdent = null;
            if (href.Contains("#"))
            {
                string[] parts = href.Split(new[] { '#' }, 2);
                filePart = parts[0];
                itemIdent = parts[1];
            }

            string xmlPath = Path.Combine(manifestDir, filePart);
            if (!File.Exists(xmlPath)) return;

            try
            {
                XDocument qtiDoc = XDocument.Load(xmlPath);

                // Find and remove the specific <item>
                XElement targetItem = null;
                if (!string.IsNullOrEmpty(itemIdent))
                {
                    targetItem = qtiDoc.Descendants()
                        .FirstOrDefault(el => el.Name.LocalName == "item"
                            && (string)el.Attribute("ident") == itemIdent);
                }
                else
                {
                    targetItem = qtiDoc.Descendants()
                        .FirstOrDefault(el => el.Name.LocalName == "item");
                }

                if (targetItem != null)
                    targetItem.Remove();

                // Check if any items remain in the file
                bool hasRemainingItems = qtiDoc.Descendants()
                    .Any(el => el.Name.LocalName == "item");

                if (hasRemainingItems)
                {
                    // Save the file with the item removed
                    qtiDoc.Save(xmlPath);
                }
                else
                {
                    // No more items: delete the file and remove from manifest
                    File.Delete(xmlPath);

                    XDocument manifest = XDocument.Load(manifestPath);
                    var resource = manifest.Root
                        .Descendants()
                        .FirstOrDefault(el => el.Name.LocalName == "resource"
                            && (string)el.Attribute("href") == filePart);

                    if (resource != null)
                    {
                        resource.Remove();
                        manifest.Save(manifestPath);
                    }
                }
            }
            catch { /* best-effort removal */ }
        }

        // Creates a new QTI 1.2 assessment item XML file and registers it in the manifest.
        // Generates valid QTI 1.2 with the correct interaction elements based on questionType.
        // Returns the compound href (filename#ident) of the new item, or null on failure.
        public string CreateNewQuestion(string sessionId, string title, string questionType, HttpServerUtility server)
        {
            string manifestPath = GetManifestPath(sessionId, server);
            if (manifestPath == null) return null;

            string manifestDir = Path.GetDirectoryName(manifestPath);

            string identifier = "item_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string fileName = identifier + ".xml";
            string relativeHref = fileName;
            string filePath = Path.Combine(manifestDir, relativeHref);

            // Build QTI 1.2 <item> element
            var item = new XElement("item",
                new XAttribute("ident", identifier),
                new XAttribute("title", title));

            // Build <presentation> with question text and appropriate interaction
            var presentation = new XElement("presentation");
            var flow = new XElement("flow");

            // Question text
            flow.Add(new XElement("material",
                new XElement("mattext", "Enter question text here.")));

            switch (questionType)
            {
                case "MultipleChoice":
                    flow.Add(BuildResponseLid(identifier, "Single",
                        new[] { "CHOICE-A", "CHOICE-B" },
                        new[] { "Option A", "Option B" }));
                    break;

                case "MultiSelect":
                    flow.Add(BuildResponseLid(identifier, "Multiple",
                        new[] { "CHOICE-A", "CHOICE-B" },
                        new[] { "Option A", "Option B" }));
                    break;

                case "ShortAnswer":
                    flow.Add(new XElement("response_str",
                        new XAttribute("ident", "RESPONSE"),
                        new XAttribute("rcardinality", "Single"),
                        new XElement("render_fib",
                            new XElement("response_label",
                                new XAttribute("ident", "answer")))));
                    break;

                case "LongFormEssay":
                    flow.Add(new XElement("response_str",
                        new XAttribute("ident", "RESPONSE"),
                        new XAttribute("rcardinality", "Single"),
                        new XElement("render_fib",
                            new XAttribute("rows", "10"),
                            new XAttribute("columns", "60"),
                            new XElement("response_label",
                                new XAttribute("ident", "answer")))));
                    break;

                case "FileUpload":
                    // QTI 1.2 has no native file upload; use essay with instruction
                    flow.Add(new XElement("material",
                        new XElement("mattext", "Upload a file with your response.")));
                    flow.Add(new XElement("response_str",
                        new XAttribute("ident", "RESPONSE"),
                        new XAttribute("rcardinality", "Single"),
                        new XElement("render_fib",
                            new XAttribute("rows", "5"),
                            new XAttribute("columns", "60"),
                            new XElement("response_label",
                                new XAttribute("ident", "answer")))));
                    break;

                case "NumericalRange":
                    flow.Add(new XElement("response_str",
                        new XAttribute("ident", "RESPONSE"),
                        new XAttribute("rcardinality", "Single"),
                        new XElement("render_fib",
                            new XElement("response_label",
                                new XAttribute("ident", "answer")))));
                    break;
            }

            presentation.Add(flow);
            item.Add(presentation);

            // Build <resprocessing> for scoring
            var resprocessing = new XElement("resprocessing",
                new XElement("outcomes",
                    new XElement("decvar",
                        new XAttribute("varname", "SCORE"),
                        new XAttribute("vartype", "Decimal"),
                        new XAttribute("defaultval", "0"))));

            // Add a default correct-answer condition for MC/MS
            if (questionType == "MultipleChoice" || questionType == "MultiSelect")
            {
                resprocessing.Add(new XElement("respcondition",
                    new XAttribute("title", "Correct"),
                    new XElement("conditionvar",
                        new XElement("varequal",
                            new XAttribute("respident", "RESPONSE"),
                            "CHOICE-A")),
                    new XElement("setvar",
                        new XAttribute("varname", "SCORE"),
                        new XAttribute("action", "Set"),
                        "1")));
            }

            item.Add(resprocessing);

            // Wrap in <questestinterop>
            var root = new XElement("questestinterop", item);
            var itemDoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                root);
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
                    new XAttribute("type", "imsqti_item_xmlv1p2"),
                    new XAttribute("href", relativeHref),
                    new XElement(resources.Name.Namespace + "file",
                        new XAttribute("href", relativeHref)));
                resources.Add(newResource);
                manifest.Save(manifestPath);
            }

            // Return compound href
            return relativeHref + "#" + identifier;
        }

        // Builds a <response_lid> element with <render_choice> for MC/MS questions
        private XElement BuildResponseLid(string itemIdent, string cardinality,
            string[] choiceIds, string[] choiceTexts)
        {
            var responseLid = new XElement("response_lid",
                new XAttribute("ident", "RESPONSE"),
                new XAttribute("rcardinality", cardinality));

            var renderChoice = new XElement("render_choice",
                new XAttribute("shuffle", "No"));

            for (int i = 0; i < choiceIds.Length; i++)
            {
                renderChoice.Add(new XElement("response_label",
                    new XAttribute("ident", choiceIds[i]),
                    new XElement("material",
                        new XElement("mattext", choiceTexts[i]))));
            }

            responseLid.Add(renderChoice);
            return responseLid;
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
