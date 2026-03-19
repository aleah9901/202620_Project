<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="QuestionEditor.aspx.cs" Inherits="QTI_Editor.WWW.WebForm1" %>

<!DOCTYPE html>
<!--
    
This is the Question Editor page, which appears after selecting a question
from the Quiz Overview. It is the main workspace for editing a question’s text, answers,
 and general feedback. You can switch between question types automatically. Leaving
 the answer field blank creates a long-form essay question, while including “Upload a file” in the 
question text triggers a File Upload question type.

For multiple-choice or multi-select questions, you can add answer options and 
mark them as correct or incorrect. An error detection system warns if no correct 
answer is set or if more options are needed.

-->
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
        </div>
    </form>
</body>
</html>
