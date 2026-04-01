using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QTI_Editor.WWW
{
    public partial class Upload : System.Web.UI.Page
    {
        public void Process_ZIP(object sender, EventArgs e)
        {
            const string CACHECONST = "~/cache/";
            
            //Test if there is a file
            if (FileUpload1.FileName.Length == 0)
            {
                lblmessage.Text = "Please choose a ZIP file.";
                return;
            }

            //Test if file is a zip file
            string originalFile = Path.GetFileName(FileUpload1.FileName);
            if (!originalFile.EndsWith(".zip"))
            {
                lblmessage.Text = "Only .zip files are allowed";
                return ;
            }

            //Generate Session ID
            string sessionId = QTI_Editor.WWW.Services.SessionService.GenerateSession();

           
            //Create Cache Directory
            string cacheDirectory = Server.MapPath(CACHECONST + sessionId);
            Directory.CreateDirectory(cacheDirectory);

            //Copies contents from original zip to cache session zip
            string zipPath = Path.Combine(cacheDirectory, sessionId + ".zip");
            FileUpload1.SaveAs(zipPath);
            
            //Extracted zipPath method goes here

            //QTI verification boolean
            
        } 
    }
}
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Directories
var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), "cache");
var feedbackDir = Path.Combine(Directory.GetCurrentDirectory(), "feedback");
var exportDir = Path.Combine(Directory.GetCurrentDirectory(), "exports");

// Ensure directories exist
Directory.CreateDirectory(cacheDir);
Directory.CreateDirectory(feedbackDir);
Directory.CreateDirectory(exportDir);


// ---- Helper: Zip Directory ----
string? ZipDirectory(string sourceDir, string outputName)
{
    if (!Directory.Exists(sourceDir))
        return null;

    var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
    if (files.Length == 0)
        return null;

    var zipPath = Path.Combine(exportDir, outputName);

    if (File.Exists(zipPath))
        File.Delete(zipPath);

    ZipFile.CreateFromDirectory(sourceDir, zipPath, CompressionLevel.Fastest, true);

    return zipPath;
}


// ---- Frontend Page ----
app.MapGet("/", () =>
{
    var html = @"
    <!DOCTYPE html>
    <html>
    <head>
        <title>Export System</title>
        <style>
            body { font-family: Arial; padding: 40px; }
            button {
                padding: 10px 20px;
                margin: 10px;
                font-size: 16px;
                cursor: pointer;
            }
        </style>
    </head>
    <body>
        <h2>Export System</h2>

        <div>
            <h3>Export Feedback</h3>
            <button onclick='exportFeedback()'>Download Feedback</button>
        </div>

        <div>
            <h3>Export Cache</h3>
            <button onclick='exportCache()'>Download Cache</button>
        </div>

        <script>
            function exportFeedback() {
                window.location.href = '/export/feedback';
            }

            function exportCache() {
                window.location.href = '/export/cache';
            }
        </script>
    </body>
    </html>";

    return Results.Content(html, "text/html");
});


// ---- Export Feedback ----
app.MapGet("/export/feedback", () =>
{
    var zipPath = ZipDirectory(feedbackDir, "feedback_export.zip");

    if (zipPath == null)
        return Results.NotFound(new { error = "No feedback files found" });

    return Results.File(zipPath, "application/zip", "feedback_export.zip");
});


// ---- Export Cache ----
app.MapGet("/export/cache", () =>
{
    var zipPath = ZipDirectory(cacheDir, "cache_export.zip");

    if (zipPath == null)
        return Results.NotFound(new { error = "No cache files found" });

    return Results.File(zipPath, "application/zip", "cache_export.zip");
});


app.Run();