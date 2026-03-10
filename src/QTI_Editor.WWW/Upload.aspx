<%-- This is a control where we utilize the HTMLInputFile Control
        From this this .aspx we can create have the button for a user to select a file.
        Then we can send the file path to Upload.aspx.cs for backend logic.
       --%>

<%@ Page Title="Upload"
    MasterPageFile="~/Site.Master"
    Language="C#"
    AutoEventWireup="true"
    CodeBehind="~/Upload.aspx.cs"
    Inherits="QTI_Editor.WWW.Upload" %>


<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
   
    <h2>Upload a QTI 2.2 ZIP File</h2>

        <asp:FileUpload 
            ID="FileUpload1" 
            runat="server"/>

        <asp:Button
            ID="btnUpload"
            runat="server"
            Text="Upload ZIP File"
            CssClass="modalButton"
            OnClick="Process_ZIP"
            OnClientClick="showModal('Uploading file...');" />

        <asp:Label 
            ID="lblmessage" 
            runat="server" 
            style="display:block; margin-top:10px;
            "/>

</asp:Content>
