using System;
using System.IO;
using System.Xml.Linq;

namespace QTI_Editor.WWW
{	
	public class QTI_validation_result
		//---STRUCT NOT OBJECT---
	{
		public bool IsValid { get; set; }
		public string Message { get; set; }
		public string ManifestPath { get; set; }
		//---STRUCTURE NOT PERFORMING ACTIONS, RESULTS ONLY---
	}
	public class QTI_verification
	{
		public QTI_validation_result Validate(string extractedFolderPath)
		{
			QTI_validation_result result = new QTI_validation_result();

			result.IsValid = true;
			result.Message = "QTI validation passed: imsmanifest.xml found and successfully read.";
            //Validation rules passed


            if (string.IsNullOrWhiteSpace(extractedFolderPath))
			{
				result.IsValid = false;
				result.Message = "Validation Failed: extracted folder path is empty.";
				return result;
			}

			if (!Directory.Exists(extractedFolderPath))
			{
				result.IsValid = false;
				result.Message = "Validation Failed: extracted folder does not exist.";

			}

			else
			{
				string[] manifestFiles = Directory.GetFiles(extractedFolderPath, "imsmanifest.xml,

				SearchOption.AllDirectories);
			}

			if (manifestFiles.Length == 0)
			{
				result.IsValid = false;
				result.Message = "Validation failed: imsmanifest.xml was not found.";

			}

			else {
				string manifestPath = manifestFiles[0];
				result.ManifestPath = manifestPath;
			}

			//Is imsmanifest.xml readable
			if { 
			
				//---NEED FILL CLASS NAME---
				XDocument manifestDoc = XDocument.Load(manifestPath);

				if else 
				{
					manifestDoc.Root == null)
				
					result.IsValid = false;
					result.Message = "Validation failed: imsmanifest.xml is empty.";
					
				}

				//Root check
				if (manifestDoc.Root.Name.LocalName.ToLower() != "manifest")
				{
					result.IsValid = false;
					result.Message = "Validation failed: root element is not in manifest.";
					return result;
				}
			}
			if else 
			{
				//---(Exception ex)--???
			
				result.IsValid = false;
				result.Message = "Validation failed: imsmanifest.xml unable to read. " + ex.Message;
				return result;
			}

			
			
			
		}
	}
}






                }
