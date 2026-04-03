<%-- This will be the page shown after Zip Upload. You will have the ability to choose a question to edit, export the
    quiz, edit the quiz title, edit points per question, and edit quiz description. There will also be a button with
    quiz settings, like shuffle answer order for questions, show correct answers after submission, only show one
    question at a time, and allowing/disallowing going back to the previous question. --%>

    <%@ Page Title="Quiz Overview" MasterPageFile="~/Site.Master" Language="C#" AutoEventWireup="true"
        CodeBehind="QuizOverview.aspx.cs" Inherits="QTI_Editor.WWW.QuizOverview" %>

        <asp:Content ID="HeaderContent" ContentPlaceHolderID="HeaderControls" runat="server">
            <asp:Button ID="exZipButton" runat="server" Text="Export ZIP" CssClass="btn" OnClick="Export_ZIP"
                OnClientClick="showModal('Exporting file...'); return true;" />
        </asp:Content>

        <asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

            <h2 class="page-title">Questions</h2>

            <%-- Quiz Title --%>
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
                                <th>Question Title</th>
                                <th>Type</th>
                                <th style="text-align:right;">Actions</th>
                            </tr>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td>
                                <%# Eval("Title") %>
                            </td>
                            <td><span class="type-badge">
                                    <%# Eval("Type") %>
                                </span></td>
                            <td style="text-align:right;">
                                <asp:Button ID="btnEditQuestion" runat="server" Text="Edit" CssClass="btn btn-sm"
                                    CommandName="Select" CommandArgument='<%# Eval("Href") %>' />
                                <asp:Button ID="btnRemoveQuestion" runat="server" Text="Remove"
                                    CssClass="btn btn-sm btn-danger" CommandName="Remove"
                                    CommandArgument='<%# Eval("Href") %>'
                                    OnClientClick="return confirm('Remove this question?');" />
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>

                <%-- Add Question --%>
                    <div class="add-row">
                        <asp:TextBox ID="txtNewQuestionTitle" runat="server" placeholder="question title..." />
                        <asp:DropDownList ID="ddlNewQuestionType" runat="server" CssClass="btn btn-sm">
                            <asp:ListItem Text="MultipleChoice" Value="MultipleChoice" />
                            <asp:ListItem Text="LongFormEssay" Value="LongFormEssay" />
                            <asp:ListItem Text="ShortAnswer" Value="ShortAnswer" />
                            <asp:ListItem Text="FileUpload" Value="FileUpload" />
                        </asp:DropDownList>
                        <asp:Button ID="btnAddQuestion" runat="server" Text="+ Add" CssClass="btn"
                            OnClick="AddQuestion_Click" />
                    </div>
                    <asp:Label ID="lblAddError" runat="server" CssClass="error-label" Visible="false" />

        </asp:Content>