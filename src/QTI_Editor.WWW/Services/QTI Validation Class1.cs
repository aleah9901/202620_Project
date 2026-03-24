using System;
using System.IO;
using System.Xml.Linq;

namespace QTI_Editor.WWW
{
	public class QTI_validation_result
	{
		public bool IsValid { get; set; }
		public string Message { get; set; }
		public string ManifestPath { get; set; }
	}
	public class QTI_verification
	{
		public QTI_validation_result Validate_QTI(string extractedFolderPath)
		{
			QTI_validation_result result = new QTI_validation_result();
			{
				result.IsValid = true;
				result.Message = "QTI validation passed: imsmanifest.xml found and successfully read.";
			}
			
			try
			{

				if (string.IsNullOrWhiteSpace(extractedFolderPath))
				{
					result.IsValid = false;
					result.Message = "Validation Failed: extracted folder path is empty.";
				}
				
				else if (!Directory.Exists(extractedFolderPath))
				{
					result.IsValid = false;
					result.Message = "Validation Failed: extracted folder does not exist.";

				}
				
				else
				{
					string[] manifestFiles = Directory.GetFiles(
							extractedFolderPath,
							"imsmanifest.xml",
							SearchOption.AllDirectories);

					if (manifestFiles.Length == 0)
					{
						result.IsValid = false;
						result.Message = "Validation failed: imsmanifest.xml was not found.";

					}
					
					else
					{
						string manifestPath = manifestFiles[0];
						result.ManifestPath = manifestPath;

						XDocument manifestDoc = XDocument.Load(manifestPath);

						if (manifestDoc.Root == null)
						{
							result.IsValid = false;
							result.Message = "Validation failed: imsmanifest.xml is empty.";
						}
						
						else if (manifestDoc.Root.Name.LocalName.ToLower() != "manifest")
						{
							result.IsValid = false;
							result.Message = "Validation failed: root element is not manifest.";
						}
					}
				}
			}
			
			catch (Exception ex)
			{
				result.IsValid = false;
				result.Message = ex.Message;
			}

			return result;
		}
	}
}

