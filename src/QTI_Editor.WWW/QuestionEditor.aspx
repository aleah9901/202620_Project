<%-- This is the Question Editor page, which appears after selecting a question from the Quiz Overview. It is the main
    workspace for editing a question's text, answers, and general feedback. You can switch between question types
    automatically. Leaving the answer field blank creates a long-form essay question, while including "Upload a file" in
    the question text triggers a File Upload question type. For multiple-choice or multi-select questions, you can add
    answer options and mark them as correct or incorrect. An error detection system warns if no correct answer is set or
    if more options are needed. --%>

    <%@ Page Title="QuestionEditor" MasterPageFile="~/Site.Master" Language="C#" AutoEventWireup="true"
        CodeBehind="QuestionEditor.aspx.cs" Inherits="QTI_Editor.WWW.QuestionEditor" %>

        <asp:Content ID="HeaderContent" ContentPlaceHolderID="HeaderControls" runat="server">
            <asp:Button ID="btnBack" runat="server" Text="Back to Overview" CssClass="btn" OnClick="Back_Click" />
        </asp:Content>

        <asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

            <h2 class="page-title">Edit Question</h2>
            <p class="page-subtitle">
                <span class="type-badge">
                    <asp:Label ID="lblQuestionType" runat="server" />
                </span>
            </p>

            <asp:Label ID="lblError" runat="server" CssClass="error-label" Visible="false" />

            <%-- Title --%>
                <div class="field-group">
                    <label class="field-label">Question Title</label>
                    <asp:TextBox ID="txtTitle" runat="server" CssClass="field-input" />
                </div>

                <%-- Type Dropdown --%>
                    <div class="field-group">
                        <label class="field-label">Type</label>
                        <asp:DropDownList ID="ddlType" runat="server" CssClass="field-input" AutoPostBack="true"
                            OnSelectedIndexChanged="Type_Changed">
                            <asp:ListItem Text="MultipleChoice" Value="MultipleChoice" />
                            <asp:ListItem Text="MultiSelect" Value="MultiSelect" />
                            <asp:ListItem Text="LongFormEssay" Value="LongFormEssay" />
                            <asp:ListItem Text="ShortAnswer" Value="ShortAnswer" />
                            <asp:ListItem Text="FileUpload" Value="FileUpload" />
                            <asp:ListItem Text="NumericalRange" Value="NumericalRange" />
                        </asp:DropDownList>
                    </div>

                    <%-- Question Text --%>
                        <div class="field-group">
                            <label class="field-label">Question Text</label>
                            <asp:TextBox ID="txtQuestionText" runat="server" TextMode="MultiLine" Rows="5"
                                CssClass="field-input" />
                        </div>

                        <%-- Choice panel: shown for MultipleChoice and MultiSelect --%>
                            <asp:Panel ID="pnlChoices" runat="server" Visible="false">
                                <div class="field-group">
                                    <label class="field-label">Answer Choices <span
                                            style="color:#999; font-weight:normal;">(check = correct)</span></label>
                                    <asp:Repeater ID="choiceRepeater" runat="server"
                                        OnItemCommand="choiceRepeater_ItemCommand">
                                        <ItemTemplate>
                                            <div class="choice-row">
                                                <asp:HiddenField ID="hidIdentifier" runat="server"
                                                    Value='<%# Eval("Identifier") %>' />
                                                <asp:CheckBox ID="chkCorrect" runat="server"
                                                    Checked='<%# (bool)Eval("IsCorrect") %>' />
                                                <asp:TextBox ID="txtChoice" runat="server" Text='<%# Eval("Text") %>'
                                                    CssClass="field-input" style="flex:1;" />
                                                <asp:Button ID="btnRemoveChoice" runat="server" Text="Delete"
                                                    CssClass="btn btn-sm btn-danger" CommandName="RemoveChoice"
                                                    CommandArgument='<%# Eval("Identifier") %>' />
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                    <asp:Button ID="btnAddAnswer" runat="server" Text="+ add answer choice"
                                        CssClass="btn-link" OnClick="AddAnswer_Click" />
                                </div>
                            </asp:Panel>

                            <%-- Short answer panel --%>
                                <asp:Panel ID="pnlShortAnswer" runat="server" Visible="false">
                                    <div class="field-group">
                                        <label class="field-label">Correct Answer</label>
                                        <asp:TextBox ID="txtShortAnswer" runat="server" CssClass="field-input"
                                            placeholder="expected answer..." />
                                    </div>
                                </asp:Panel>

                                <%-- Range panel: shown for NumericalRange --%>
                                    <asp:Panel ID="pnlRange" runat="server" Visible="false">
                                        <div class="field-group">
                                            <label class="field-label">Numerical Range</label>
                                            <div style="display:flex; gap:10px; align-items:center;">
                                                <asp:TextBox ID="txtRangeMin" runat="server" CssClass="field-input"
                                                    placeholder="min" style="width:120px;" />
                                                <span>to</span>
                                                <asp:TextBox ID="txtRangeMax" runat="server" CssClass="field-input"
                                                    placeholder="max" style="width:120px;" />
                                            </div>
                                        </div>
                                    </asp:Panel>

                                    <%-- Action bar --%>
                                        <div class="action-bar">
                                            <asp:Button ID="btnDeleteQuestion" runat="server" Text="Delete Question"
                                                CssClass="btn btn-danger" OnClick="DeleteQuestion_Click"
                                                OnClientClick="return confirm('Delete this question permanently?');"
                                                style="margin-right:auto;" />
                                            <asp:Button ID="btnDiscard" runat="server" Text="Discard" CssClass="btn"
                                                OnClick="Back_Click" />
                                            <asp:Button ID="btnSave" runat="server" Text="Save Changes"
                                                CssClass="btn btn-primary" OnClick="Save_Question"
                                                OnClientClick="showModal('Saving changes...'); return true;" />
                                        </div>

        </asp:Content>