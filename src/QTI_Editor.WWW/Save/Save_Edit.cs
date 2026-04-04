using System;
using System.IO;

namespace QTI_Editor.WWW
{
    //This class will allow us to save individual question edits.
    public static class SaveEditService
    {
        public static void SaveEdit(string edits )
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "edits.txt");

            File.WriteAllText(filePath, edits);
            Console.WriteLine("Edits Saved!");














        }
    }
}