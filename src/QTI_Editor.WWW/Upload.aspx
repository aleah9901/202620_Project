<!-- This is a control where we utilize the HTMLInputFile Control
        From this this .aspx we can create have the button for a user to select a file.
        Then we can send the file path to Upload.aspx.cs for backend logic.
       -->
<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Upload.aspx.cs" Inherits="QTI_Editor.WWW.Upload" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Upload Zip</title>
</head>
<body>
    <form id="form1" runat="server">
        <asp:FileUpload 
            ID="FileUpload1" 
            runat="server"  />

        <asp:Button
            ID="btnUpload"
            runat="server"
            Text="Upload ZIP File"
            OnClick="Process_ZIP"/>
    </form>
</body>
</html>
