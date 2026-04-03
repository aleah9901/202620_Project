using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace QTI_Editor.WWW.Save
{
    // Listed in the manifest question picker on QuizOverview
    public class ManifestItem
    {
        public string Identifier { get; set; }
        public string Title      { get; set; }
        public string Type       { get; set; }
        public string Href       { get; set; }
    }

    // A single answer choice within a MultipleChoice or MultiSelect question
    public class ChoiceModel
    {
        public string Identifier { get; set; }
        public string Text       { get; set; }
        public bool   IsCorrect  { get; set; }
    }

    // Full editable state of one assessmentItem; passed between the service and QuestionEditor
    public class QuestionModel
    {
        public string            PhysicalPath { get; set; }
        public string            Identifier   { get; set; }
        public string            Title        { get; set; }
        public string            QuestionText { get; set; }
        public string            QuestionType { get; set; }
        public List<ChoiceModel> Choices      { get; set; } = new List<ChoiceModel>();
        public string            RangeMin     { get; set; }
        public string            RangeMax     { get; set; }
    }

    // Reads the manifest and item XML files from the session cache,
    // and writes edited question data back to those item files
    public class QuestionEditService
    {
        private const string CacheVirtualRoot = "~/cache/";

        // Parses imsmanifest.xml and returns an ordered list of question items
        public List<ManifestItem> GetManifestItems(string sessionId, HttpServerUtility server)
        {
            var items = new List<ManifestItem>();
            string extractedPath = GetExtractedPath(sessionId, server);
            string manifestPath  = FindManifest(extractedPath);
            if (manifestPath == null) return items;

            // All href values in the manifest are relative to the manifest file's directory
            string manifestDir = Path.GetDirectoryName(manifestPath);

            XDocument doc = XDocument.Load(manifestPath);

            // Use LocalName comparison to avoid namespace mismatches across QTI packages
            var resources = doc.Descendants()
                .Where(e => e.Name.LocalName == "resource")
                .Where(e => {
                    string type = (string)e.Attribute("type") ?? "";
                    return type.Contains("imsqti_item") || type.Contains("imsqti_xmlv2");
                });

            foreach (var resource in resources)
            {
                string href = (string)resource.Attribute("href");
                if (string.IsNullOrWhiteSpace(href)) continue;

                // Resolve href relative to the manifest directory, not the extracted root
                string itemPath = Path.Combine(manifestDir,
                    href.Replace('/', Path.DirectorySeparatorChar));

                // Read the actual title from the assessmentItem XML
                string title = ReadItemTitle(itemPath)
                            ?? (string)resource.Attribute("identifier")
                            ?? href;

                // Detect question type from the item XML
                string qType = ReadItemType(itemPath);

                items.Add(new ManifestItem
                {
                    Identifier = (string)resource.Attribute("identifier"),
                    Title      = title,
                    Type       = qType,
                    Href       = href
                });
            }

            return items;
        }

        // Loads a single assessmentItem XML and returns a populated QuestionModel
        public QuestionModel LoadQuestion(string sessionId, string itemHref,
                                          HttpServerUtility server)
        {
            string extractedPath = GetExtractedPath(sessionId, server);
            string manifestPath  = FindManifest(extractedPath);
            if (manifestPath == null) return null;

            // Resolve href relative to the manifest directory
            string manifestDir = Path.GetDirectoryName(manifestPath);
            string itemPath    = Path.Combine(manifestDir,
                itemHref.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(itemPath)) return null;

            XDocument doc  = XDocument.Load(itemPath);
            XElement  root = doc.Root;
            if (root == null) return null;

            // Find itemBody using namespace-agnostic search
            XElement itemBody = FindByLocalName(root, "itemBody");

            var model = new QuestionModel
            {
                PhysicalPath = itemPath,
                Identifier   = (string)root.Attribute("identifier"),
                Title        = (string)root.Attribute("title") ?? "",
                QuestionText = ReadQuestionText(itemBody),
                QuestionType = DetectType(itemBody, root)
            };

            // Populate choices for MultipleChoice and MultiSelect
            if (model.QuestionType == "MultipleChoice" || model.QuestionType == "MultiSelect")
            {
                var correctValues     = ReadCorrectValues(root);
                var choiceInteraction  = FindByLocalName(itemBody, "choiceInteraction");

                if (choiceInteraction != null)
                {
                    model.Choices = choiceInteraction.Elements()
                        .Where(c => c.Name.LocalName == "simpleChoice")
                        .Select(c => new ChoiceModel
                        {
                            Identifier = (string)c.Attribute("identifier"),
                            Text       = c.Value.Trim(),
                            IsCorrect  = correctValues.Contains((string)c.Attribute("identifier"))
                        })
                        .ToList();
                }
            }

            // Parse [min,max] for NumericalRange
            if (model.QuestionType == "NumericalRange")
            {
                string raw   = ReadCorrectValues(root).FirstOrDefault() ?? "";
                var    match = Regex.Match(raw.Trim(),
                    @"^\[\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*\]$");
                if (match.Success)
                {
                    model.RangeMin = match.Groups[1].Value;
                    model.RangeMax = match.Groups[2].Value;
                }
            }

            return model;
        }

        // Writes an edited QuestionModel back to its item XML file in the session cache
        public bool SaveQuestion(QuestionModel model)
        {
            if (!File.Exists(model.PhysicalPath)) return false;

            XDocument doc  = XDocument.Load(model.PhysicalPath);
            XElement  root = doc.Root;
            if (root == null) return false;

            XElement itemBody = FindByLocalName(root, "itemBody");

            // Title
            root.SetAttributeValue("title", model.Title);

            // Question prompt text
            WriteQuestionText(itemBody, model.QuestionText);

            // Choice text and correct response
            if (model.Choices != null && model.Choices.Count > 0)
            {
                var choiceInteraction = FindByLocalName(itemBody, "choiceInteraction");

                if (choiceInteraction != null)
                {
                    var existing = choiceInteraction.Elements()
                        .Where(e => e.Name.LocalName == "simpleChoice").ToList();
                    for (int i = 0; i < existing.Count && i < model.Choices.Count; i++)
                        existing[i].Value = model.Choices[i].Text;
                }

                var responseDecl = root.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "responseDeclaration");
                if (responseDecl != null)
                {
                    var correctResp = responseDecl.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "correctResponse");
                    if (correctResp != null)
                    {
                        XNamespace ns = correctResp.Name.Namespace;
                        correctResp.RemoveNodes();
                        foreach (var c in model.Choices.Where(c => c.IsCorrect))
                            correctResp.Add(new XElement(ns + "value", c.Identifier));
                    }
                }
            }

            // Numerical range value
            if (!string.IsNullOrWhiteSpace(model.RangeMin) &&
                !string.IsNullOrWhiteSpace(model.RangeMax))
            {
                string rangeValue = $"[{model.RangeMin},{model.RangeMax}]";
                var valueEl = root.Descendants()
                    .Where(e => e.Name.LocalName == "correctResponse")
                    .SelectMany(cr => cr.Elements().Where(v => v.Name.LocalName == "value"))
                    .FirstOrDefault();
                if (valueEl != null) valueEl.Value = rangeValue;
            }

            doc.Save(model.PhysicalPath);
            return true;
        }

        // ── Private helpers ────────────────────────────────────────────────

        private string GetExtractedPath(string sessionId, HttpServerUtility server)
        {
            return Path.Combine(server.MapPath(CacheVirtualRoot + sessionId), "extracted");
        }

        // Searches for imsmanifest.xml anywhere under the extracted folder
        private string FindManifest(string extractedPath)
        {
            if (!Directory.Exists(extractedPath)) return null;
            string[] files = Directory.GetFiles(
                extractedPath, "imsmanifest.xml", SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : null;
        }

        // Finds the first descendant element matching the given localName regardless of namespace
        private XElement FindByLocalName(XElement parent, string localName)
        {
            if (parent == null) return null;
            return parent.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);
        }

        // Reads the title attribute from an assessmentItem XML file
        private string ReadItemTitle(string itemPath)
        {
            try
            {
                if (!File.Exists(itemPath)) return null;
                return (string)XDocument.Load(itemPath).Root?.Attribute("title");
            }
            catch { return null; }
        }

        // Reads the detected question type from an item XML file without full loading
        private string ReadItemType(string itemPath)
        {
            try
            {
                if (!File.Exists(itemPath)) return "Unknown";
                XDocument doc  = XDocument.Load(itemPath);
                XElement  root = doc.Root;
                XElement  body = FindByLocalName(root, "itemBody");
                return DetectType(body, root);
            }
            catch { return "Unknown"; }
        }

        // Gets question prompt text: tries <prompt> first, then first <p>, then raw body text
        // Uses namespace-agnostic lookup so it works regardless of whether QTI 2.2 default namespace is declared
        private string ReadQuestionText(XElement itemBody)
        {
            if (itemBody == null) return "";

            var prompt = FindByLocalName(itemBody, "prompt");
            if (prompt != null) return prompt.Value.Trim();

            // Try namespace-qualified <p> and plain <p>
            var p = itemBody.Descendants().FirstOrDefault(e => e.Name.LocalName == "p");
            if (p != null) return p.Value.Trim();

            return itemBody.Value.Trim();
        }

        // Writes updated question text back to the same location it was read from
        private void WriteQuestionText(XElement itemBody, string text)
        {
            if (itemBody == null) return;

            var prompt = FindByLocalName(itemBody, "prompt");
            if (prompt != null) { prompt.Value = text; return; }

            var p = itemBody.Descendants().FirstOrDefault(e => e.Name.LocalName == "p");
            if (p != null) { p.Value = text; return; }

            itemBody.Value = text;
        }

        // Returns all correct response value strings for an assessmentItem
        // Uses namespace-agnostic lookup
        private List<string> ReadCorrectValues(XElement assessmentItem)
        {
            return assessmentItem.Descendants()
                .Where(e => e.Name.LocalName == "responseDeclaration")
                .SelectMany(rd => rd.Descendants().Where(cr => cr.Name.LocalName == "correctResponse"))
                .SelectMany(cr => cr.Elements().Where(v => v.Name.LocalName == "value"))
                .Select(v => v.Value.Trim())
                .ToList();
        }

        // Detects the question type string from the item XML structure
        // Uses namespace-agnostic lookups to handle all QTI 2.2 packaging variants
        private string DetectType(XElement itemBody, XElement assessmentItem)
        {
            if (itemBody == null) return "LongFormEssay";

            string bodyText = itemBody.Value;
            if (!string.IsNullOrEmpty(bodyText) &&
                bodyText.IndexOf("Upload a file", StringComparison.OrdinalIgnoreCase) >= 0)
                return "FileUpload";

            var choiceInteraction = FindByLocalName(itemBody, "choiceInteraction");
            var textEntry         = FindByLocalName(itemBody, "textEntryInteraction");
            var extendedText      = FindByLocalName(itemBody, "extendedTextInteraction");
            var uploadInteraction = FindByLocalName(itemBody, "uploadInteraction");

            if (extendedText      != null) return "LongFormEssay";
            if (uploadInteraction != null) return "FileUpload";

            if (textEntry != null)
            {
                string correctVal = ReadCorrectValues(assessmentItem).FirstOrDefault() ?? "";
                if (Regex.IsMatch(correctVal.Trim(),
                    @"^\[\s*-?\d+(\.\d+)?\s*,\s*-?\d+(\.\d+)?\s*\]$"))
                    return "NumericalRange";
                return "ShortAnswer";
            }

            if (choiceInteraction != null)
            {
                int.TryParse((string)choiceInteraction.Attribute("maxChoices"), out int max);
                return max > 1 ? "MultiSelect" : "MultipleChoice";
            }

            return "LongFormEssay";
        }

        // Adds a new blank simpleChoice to an existing item's choiceInteraction in the XML
        // Returns the updated ChoiceModel list so the UI can rebind
        public List<ChoiceModel> AddChoiceToXml(QuestionModel model)
        {
            if (!File.Exists(model.PhysicalPath)) return model.Choices;

            XDocument doc  = XDocument.Load(model.PhysicalPath);
            XElement  root = doc.Root;
            if (root == null) return model.Choices;

            XElement itemBody         = FindByLocalName(root, "itemBody");
            XElement choiceInteraction = FindByLocalName(itemBody, "choiceInteraction");
            if (choiceInteraction == null) return model.Choices;

            // Determine namespace from existing choices
            var existingChoices = choiceInteraction.Elements()
                .Where(e => e.Name.LocalName == "simpleChoice").ToList();
            XNamespace ns = existingChoices.Count > 0
                ? existingChoices[0].Name.Namespace
                : choiceInteraction.Name.Namespace;

            // Generate a unique identifier for the new choice
            string newId = "CHOICE_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            // Append the new choice element
            choiceInteraction.Add(new XElement(ns + "simpleChoice",
                new XAttribute("identifier", newId),
                "New Answer"));

            doc.Save(model.PhysicalPath);

            // Return updated choices list including the new one
            var correctValues = ReadCorrectValues(root);
            return choiceInteraction.Elements()
                .Where(e => e.Name.LocalName == "simpleChoice")
                .Select(c => new ChoiceModel
                {
                    Identifier = (string)c.Attribute("identifier"),
                    Text       = c.Value.Trim(),
                    IsCorrect  = correctValues.Contains((string)c.Attribute("identifier"))
                })
                .ToList();
        }

        // Creates a new blank QTI 2.2 multiple-choice question XML file
        // and registers it in imsmanifest.xml
        // Returns the href string so the UI can navigate to it
        public string CreateNewQuestion(string sessionId, string questionTitle,
                                        HttpServerUtility server)
        {
            string extractedPath = GetExtractedPath(sessionId, server);
            string manifestPath  = FindManifest(extractedPath);
            if (manifestPath == null) return null;

            string manifestDir = Path.GetDirectoryName(manifestPath);

            // Generate a unique item identifier
            string itemId   = "ITEM_" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
            string fileName = itemId + ".xml";

            // Place items in an "items" subfolder relative to the manifest
            string itemsDir = Path.Combine(manifestDir, "items");
            Directory.CreateDirectory(itemsDir);

            string itemFilePath = Path.Combine(itemsDir, fileName);
            string href         = "items/" + fileName;

            // Build a minimal QTI 2.2 assessmentItem
            XNamespace qti = "http://www.imsglobal.org/xsd/imsqti_v2p2";

            var itemDoc = new XDocument(
                new XElement(qti + "assessmentItem",
                    new XAttribute("identifier", itemId),
                    new XAttribute("title", questionTitle ?? "New Question"),
                    new XAttribute("adaptive", "false"),
                    new XAttribute("timeDependent", "false"),
                    new XElement(qti + "responseDeclaration",
                        new XAttribute("identifier", "RESPONSE"),
                        new XAttribute("cardinality", "single"),
                        new XAttribute("baseType", "identifier"),
                        new XElement(qti + "correctResponse",
                            new XElement(qti + "value", "A"))),
                    new XElement(qti + "outcomeDeclaration",
                        new XAttribute("identifier", "SCORE"),
                        new XAttribute("cardinality", "single"),
                        new XAttribute("baseType", "float"),
                        new XElement(qti + "defaultValue",
                            new XElement(qti + "value", "0"))),
                    new XElement(qti + "itemBody",
                        new XElement(qti + "p", questionTitle ?? "New Question"),
                        new XElement(qti + "choiceInteraction",
                            new XAttribute("responseIdentifier", "RESPONSE"),
                            new XAttribute("shuffle", "false"),
                            new XAttribute("maxChoices", "1"),
                            new XElement(qti + "simpleChoice",
                                new XAttribute("identifier", "A"), "Answer A"),
                            new XElement(qti + "simpleChoice",
                                new XAttribute("identifier", "B"), "Answer B")))));

            itemDoc.Save(itemFilePath);

            // Register the new item in imsmanifest.xml
            XDocument manifestDoc = XDocument.Load(manifestPath);
            XElement  manifestRoot = manifestDoc.Root;
            if (manifestRoot != null)
            {
                // Find or create <resources> container
                var resources = manifestRoot.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "resources");

                if (resources != null)
                {
                    XNamespace mns = resources.Name.Namespace;

                    resources.Add(new XElement(mns + "resource",
                        new XAttribute("identifier", itemId),
                        new XAttribute("type", "imsqti_item_xmlv2p2"),
                        new XAttribute("href", href),
                        new XElement(mns + "file",
                            new XAttribute("href", href))));

                    manifestDoc.Save(manifestPath);
                }
            }

            return href;
        }

        // Deletes an item XML file and removes its resource entry from the manifest
        public bool DeleteQuestion(string sessionId, string itemHref, HttpServerUtility server)
        {
            string extractedPath = GetExtractedPath(sessionId, server);
            string manifestPath  = FindManifest(extractedPath);
            if (manifestPath == null) return false;

            string manifestDir = Path.GetDirectoryName(manifestPath);
            string itemPath    = Path.Combine(manifestDir,
                itemHref.Replace('/', Path.DirectorySeparatorChar));

            // Delete the XML file
            try { if (File.Exists(itemPath)) File.Delete(itemPath); }
            catch {}

            // Remove the resource entry from the manifest
            XDocument doc = XDocument.Load(manifestPath);
            var resource = doc.Descendants()
                .Where(e => e.Name.LocalName == "resource")
                .FirstOrDefault(e => (string)e.Attribute("href") == itemHref);

            if (resource != null)
            {
                resource.Remove();
                doc.Save(manifestPath);
            }

            return true;
        }

        // Removes a single simpleChoice by identifier from the item XML
        // Returns the updated choices list
        public List<ChoiceModel> RemoveChoiceFromXml(QuestionModel model, string choiceIdentifier)
        {
            if (!File.Exists(model.PhysicalPath)) return model.Choices;

            XDocument doc  = XDocument.Load(model.PhysicalPath);
            XElement  root = doc.Root;
            if (root == null) return model.Choices;

            XElement itemBody         = FindByLocalName(root, "itemBody");
            XElement choiceInteraction = FindByLocalName(itemBody, "choiceInteraction");
            if (choiceInteraction == null) return model.Choices;

            var target = choiceInteraction.Elements()
                .Where(e => e.Name.LocalName == "simpleChoice")
                .FirstOrDefault(e => (string)e.Attribute("identifier") == choiceIdentifier);

            if (target != null)
            {
                target.Remove();
                doc.Save(model.PhysicalPath);
            }

            var correctValues = ReadCorrectValues(root);
            return choiceInteraction.Elements()
                .Where(e => e.Name.LocalName == "simpleChoice")
                .Select(c => new ChoiceModel
                {
                    Identifier = (string)c.Attribute("identifier"),
                    Text       = c.Value.Trim(),
                    IsCorrect  = correctValues.Contains((string)c.Attribute("identifier"))
                })
                .ToList();
        }

        // Reads the quiz title from the manifest's root title attribute
        public string GetQuizTitle(string sessionId, HttpServerUtility server)
        {
            string extractedPath = GetExtractedPath(sessionId, server);
            string manifestPath  = FindManifest(extractedPath);
            if (manifestPath == null) return "";

            XDocument doc = XDocument.Load(manifestPath);
            // Try the manifest root title, then first organization title
            string title = (string)doc.Root?.Attribute("title");
            if (!string.IsNullOrWhiteSpace(title)) return title;

            var orgTitle = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "title");
            return orgTitle?.Value?.Trim() ?? "My Quiz";
        }

        // Writes the quiz title to the manifest's root title attribute
        public void SetQuizTitle(string sessionId, string title, HttpServerUtility server)
        {
            string extractedPath = GetExtractedPath(sessionId, server);
            string manifestPath  = FindManifest(extractedPath);
            if (manifestPath == null) return;

            XDocument doc = XDocument.Load(manifestPath);
            doc.Root?.SetAttributeValue("title", title);
            doc.Save(manifestPath);
        }
    }
}
