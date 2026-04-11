using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;

namespace QTI_Editor.WWW
{
    public partial class QuestionEditor : System.Web.UI.Page
    {
        // QTI 2.2 XML namespace
        private static readonly XNamespace QtiNs = "http://www.imsglobal.org/xsd/imsqti_v2p2";

        // Enumeration of question types
        private enum QuestionType
        {
            Unknown, MultipleChoice, MultiSelect, LongFormEssay, FileUpload, NumericalRange
        }

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        // Detects the question type.
        // Priority order: 1. File Upload, 2. Essay, 3. Numerical Range, 4. Choice based
        //private QuestionType DetectQuestionType(XElement itemBody, string bodyText)
        //{
        //    // 1. File upload trigger
        //    if (IsFileUploadQuestion(bodyText))
        //        return QuestionType.FileUpload;

        //    if (itemBody == null)
        //        return QuestionType.LongFormEssay;

        //    // Locate QTI 2.2 interaction elements
        //    XElement choiceInteraction = itemBody.Descendants(QtiNs + "choiceInteraction").FirstOrDefault();
        //    XElement textEntry = itemBody.Descendants(QtiNs + "textEntryInteraction").FirstOrDefault();
        //    XElement extendedText = itemBody.Descendants(QtiNs + "extendedTextInteraction").FirstOrDefault();
        //    XElement uploadInteraction = itemBody.Descendants(QtiNs + "uploadInteraction").FirstOrDefault();

        //    // 2. Essay
        //    if (extendedText != null)
        //        return QuestionType.LongFormEssay;
            
        //    bool hasAnyInteraction = choiceInteraction != null || textEntry != null || uploadInteraction != null;
           
        //    if (!hasAnyInteraction)
        //        return QuestionType.LongFormEssay;

        //    // If "Upload a file" text wasn't found, check if the QTI XML structure defines this as a file upload
        //    if (uploadInteraction != null)
        //        return QuestionType.FileUpload;

        //    // 3. Numerical range [x,y] in the correct response
        //    if (textEntry != null && HasNumericalRangeResponse(itemBody.Document?.Root))
        //        return QuestionType.NumericalRange;

        //    // 4. Choice based questions
        //    if (choiceInteraction != null)
        //    {
        //        string maxChoicesAttr = (string)choiceInteraction.Attribute("maxChoices");
        //        int maxChoices = 1;
        //        int.TryParse(maxChoicesAttr, out maxChoices);

        //        return maxChoices > 1 ? QuestionType.MultiSelect : QuestionType.MultipleChoice;
        //    }

        //    return QuestionType.Unknown;
        //}

        // Returns true when the item body contains the trigger phrase "Upload a file".
        private bool IsFileUploadQuestion(string bodyText)
        {
            bool stringNotEmpty = !string.IsNullOrEmpty(bodyText);
            bool containsTriggerPhrase = bodyText.IndexOf("Upload a file", StringComparison.OrdinalIgnoreCase) >= 0;



            return (stringNotEmpty && containsTriggerPhrase);
                
        }

        // Returns true when the correct response for a text-entry interaction follows the [x,y] notation.
        private bool HasNumericalRangeResponse(XElement assessmentItem)
        {
            if (assessmentItem == null) return false;

            string correctValue = assessmentItem
                .Elements(QtiNs + "responseDeclaration")
                .SelectMany(rd => rd.Elements(QtiNs + "correctResponse"))
                .SelectMany(cr => cr.Elements(QtiNs + "value"))
                .Select(v => v.Value.Trim())
                .FirstOrDefault();

            return IsNumericalRangeFormat(correctValue);
        }

        // Checks whether a string matches the [x,y] range.
        private bool IsNumericalRangeFormat(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return Regex.IsMatch(value.Trim(),@"^\[\s*-?\d+(\.\d+)?\s*,\s*-?\d+(\.\d+)?\s*\]$");
        }
    }
}