<%-- This will be the page shown after Zip Upload. You will have the ability to choose a question to edit, export the
    quiz, edit the quiz title, edit points per question, and edit quiz description. There will also be a button with
    quiz settings, like shuffle answer order for questions, show correct answers after submission, only show one
    question at a time, and allowing/disallowing going back to the previous question. --%>

    <%@ Page Title="Quiz Overview" MasterPageFile="~/Site.Master" Language="C#" AutoEventWireup="true"
        CodeBehind="QuizOverview.aspx.cs" Inherits="QTI_Editor.WWW.QuizOverview" %>

        <asp:Content ID="HeaderContent" ContentPlaceHolderID="HeaderControls" runat="server">
            <asp:Button ID="exZipButton" runat="server" Text="Export ZIP file" CssClass="modalButton"
                OnClick="Export_ZIP" OnClientClick="showModal('Exporting file...');" />
        </asp:Content>

        <asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

            <div class="quiz-title-row">
                <span class="quiz-title-label">Quiz Title:</span>
                <asp:TextBox ID="txtQuizTitle" runat="server" CssClass="quiz-title-input" AutoPostBack="true"
                    OnTextChanged="QuizTitle_Changed" />
            </div>

            <asp:Label ID="lblOverviewError" runat="server" CssClass="error-label" Visible="false" />

            <asp:Repeater ID="questionList" runat="server" OnItemCommand="questionList_ItemCommand">
                <HeaderTemplate>
                    <table class="q-table">
                        <tr>
                            <th>Question</th>
                            <th>Type</th>
                            <th></th>
                        </tr>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td>
                            <%# Eval("Title") %>
                        </td>
                        <td>
                            <%# Eval("Type") %>
                        </td>
                        <td>
                            <asp:Button ID="btnSelect" runat="server" Text="Edit" CssClass="btn btn-sm"
                                CommandName="Select" CommandArgument='<%# Eval("Href") %>' />
                            <asp:Button ID="btnRemove" runat="server" Text="Remove" CssClass="btn btn-sm btn-danger"
                                CommandName="Remove" CommandArgument='<%# Eval("Href") %>'
                                OnClientClick="return confirm('Remove this question?');" />
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </table>
                </FooterTemplate>
            </asp:Repeater>

            <div class="add-row">
                <asp:TextBox ID="txtNewQuestionTitle" runat="server" placeholder="New question title..." />
                <asp:DropDownList ID="ddlNewQuestionType" runat="server">
                    <asp:ListItem Text="Multiple Choice" Value="MultipleChoice" />
                    <asp:ListItem Text="Multi Select" Value="MultiSelect" />
                    <asp:ListItem Text="Essay" Value="LongFormEssay" />
                    <asp:ListItem Text="Short Answer" Value="ShortAnswer" />
                    <asp:ListItem Text="File Upload" Value="FileUpload" />
                    <asp:ListItem Text="Numerical Range" Value="NumericalRange" />
                </asp:DropDownList>
                <asp:Button ID="btnAddQuestion" runat="server" Text="Add Question" CssClass="btn btn-primary"
                    OnClick="AddQuestion_Click" />
            </div>

            <asp:Label ID="lblAddError" runat="server" CssClass="error-label" Visible="false" />

        </asp:Content>