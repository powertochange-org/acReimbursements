<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AdvanceView.ascx.cs" Inherits="PowerToChange.Modules.StaffRmb.Views.AdvanceView" %>


<h2>ADVANCE ADJUSTMENT GRID</h2>
<form>
    <asp:UpdatePanel runat="server">
        <ContentTemplate>
            <asp:GridView ID="gvGrid" runat="server" AutoGenerateColumns="False" OnRowDataBound="SetupGridLine" OnRowEditing="LineChanged" >
                <RowStyle CssClass="dnnGridItem" />
                <AlternatingRowStyle CssClass="dnnGridAltItem" />
                <Columns>
                    <asp:TemplateField HeaderText="Rmb ID" >
                        <ItemTemplate>
                            <asp:Label runat="server" Text='<%# Eval("RID").ToString().PadLeft(5, (char)48) %>'></asp:Label>
                        </ItemTemplate>
                        <ItemStyle HorizontalAlign="Right" Width="50px" />
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Staff">
                        <ItemTemplate>
                            <asp:Label id="lblWho" runat="server"></asp:Label>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="To be cleared by" >
                        <ItemTemplate>
                            <asp:Label runat="server" Text='<%# ((DateTime)Eval("date")).ToShortDateString() %>'></asp:Label>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Original Amt">
                        <ItemTemplate>
                            <asp:Label id="lblOriginalAmount" CssClass="original" runat="server" Text='<%# string.Format("{0:0.00}", Eval("originalAmount")) %>'></asp:Label>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Cleared Amt" >
                        <ItemTemplate>
                            <asp:HiddenField ID="hfLineNo" runat="server" Value='<%# Eval("LineNo") %>' />
                            <asp:TextBox id="tbCleared" type="number" step=".01"  runat="server" style="text-align:right; width:100px" OnFocus="$(this).select();"
                                OnChange="if (confirmIfClearing(this)) { $(this).next().click() }" />
                            <asp:Button ID="btnCommand" runat="server" CommandName="Edit" CommandArgument='<%# ((GridViewRow) Container).RowIndex %>' style="display:none;" />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Outstanding Amt">
                        <ItemTemplate>
                            <asp:Label ID="lblOutstanding" CssClass="outstanding" runat="server" Text='<%# Eval("outstandingAmount") %>'></asp:Label>
                        </ItemTemplate>
                        <ItemStyle HorizontalAlign="Right" Width="50px" />
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Comment" SortExpression="comment">
                        <ItemTemplate>
                            <asp:Label runat="server" Text='<%# Eval("comment") %>'></asp:Label>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
            <asp:Label ID="lblWarning" runat="server" CssClass="AgapeWarning" />
            <asp:HiddenField ID="hfLog" runat="server" />
        </ContentTemplate>
    </asp:UpdatePanel>
</form>
<script type="text/javascript">
    function confirmIfClearing(text) {
        console.warn("HERE");
        var clearing = $(text).val();
        var total = $(text).parent().prev().children('.original').text();
        console.log("clearing: " + clearing);
        console.log("total: " + Number(total));
        if (Number(clearing) != Number(total)) return true; //no need to confirm
        return confirm("Are you sure you want to clear this item entirely?");
    }
</script>