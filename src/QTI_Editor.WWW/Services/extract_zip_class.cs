using System;

public class extract_zip_class
{
    //the class takes a string, that is a path in the file system leading to a zipfile
    //extracts the zipfile
    //saves the extracted zip file
    //returns a the path for the extracted zip file, also as a string
    // @"C:\direc" is the directory I created. I don't know if a different place already created one. 


    public static string extract_zip_class(string: zipPath) -> string;
	{
        ZipFile.ExtractToDirectory(zipPath, @"C:\direc");
        return @"C:\direc"



}
}
