<!-- This is a control where we utilize the HTMLInputFile Control
        From this this .aspx we can create have the button for a user to select a file.
        Then we can send the file path to Upload.aspx.cs for backend logic.
       -->
<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Upload.aspx.cs" Inherits="QTI_Editor.WWW.Upload" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Upload Zip</title>
    <!-- Style rules made by team 4 -->
    <style>
        body {
            font-family: Arial, sans-serif;
            text-align: center;
            margin-top: 100px;
        }

        button {
            padding: 10px 20px;
            margin: 10px;
            font-size: 16px;
            cursor: pointer;
        }

        /* Modal Background */
        .modal {
            display: none;
            position: fixed;
            z-index: 1000;
            left: 0;
            top: 0;
            width: 100%;
            height: 100%;
            background-color: rgba(0, 0, 0, 0.5);
            justify-content: center;
            align-items: center;
        }

        .modalButton {
            padding: 10px 18px;
            text-align: center;
            font-size: 16px;
            border-radius: 6px;
            cursor: pointer;
        }

        .modalButton:hover {
            background-color: #45a049;
        }

        /* Modal Content */
        .modal-content {
            background: white;
            padding: 40px;
            border-radius: 10px;
            text-align: center;
            width: 300px;
        }

        label {
            text-align: center;
            font-family:Arial, sans-serif;
        }

        /* Spinner */
        .spinner {
            border: 6px solid #f3f3f3;
            border-top: 6px solid #3498db;
            border-radius: 50%;
            width: 50px;
            height: 50px;
            animation: spin 1s linear infinite;
            margin: 0 auto 20px auto;
        }

        @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }

        .status-text {
            font-size: 16px;
            font-weight: bold;
        }
    </style>
</head>
<body>

    <h2>Upload a QTI 2.2 ZIP File</h2>

    <form id="form1" runat="server">
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

        <!-- Modal from team 4 -->
        <div id="statusModal" class="modal">
             <div class="modal-content">
                <div class="spinner"></div>
                <div id="statusText" class="status-text">Processing...</div>
             </div>
        </div>
    </form>
    <!-- Modal script from team 4 -->
    <script>
        const modal = document.getElementById("statusModal");
        const statusText = document.getElementById("statusText");

        function showModal(message) {
            statusText.innerText = message;
            modal.style.display = "flex";
        }

        function hideModal() {
            modal.style.display = "none";
        }
</script>
</body>
</html>
