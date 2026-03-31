﻿<%-- This will be the page shown after Zip Upload. You will have the ability to choose a question to edit, export the
  quiz, edit the quiz title, edit points per question, and edit quiz description. There will also be a button with quiz
  settings, like shuffle answer order for questions, show correct answers after submission, only show one question at a
  time, and allowing/disallowing going back to the previous question. --%>

  <%@ Page Title="Quiz Overview" MasterPageFile="~/Site.Master" Language="C#" AutoEventWireup="true"
    CodeBehind="QuizOverview.aspx.cs" Inherits="QTI_Editor.WWW.QuizOverview" %>

    <asp:Content ID="HeaderContent" ContentPlaceHolderID="HeaderControls" runat="server">
      <%-- Content for the Quiz Overview goes here --%>
        <asp:Button ID="exZipButton" runat="server" Text="Export ZIP file" CssClass="modalButton"
          OnClientClick="showModal('Exporting file...');" />


    </asp:Content>