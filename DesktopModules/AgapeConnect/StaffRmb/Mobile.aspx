<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Mobile.aspx.vb" Inherits="DesktopModules_AgapeConnect_StaffRmb_Mobile"  Async="true" AsyncTimeout="60"%>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>P2C Reimbursements</title>
    <meta name="viewport" content="width=device-width, initial-scale=1"/> 
	<link rel="stylesheet" href="http://code.jquery.com/mobile/1.4.4/jquery.mobile-1.4.4.min.css" />
    <link rel="stylesheet" href="/Portals/_default/Skins/Carmel/Mobile.css" />
	<script type="text/javascript" src="http://code.jquery.com/jquery-2.1.1.min.js"></script>
	<script type="text/javascript" src="http://code.jquery.com/mobile/1.4.4/jquery.mobile-1.4.4.min.js"></script>
</head>
<body>
    <form runat="server">
        <asp:ScriptManager runat="server"></asp:ScriptManager>
        <div ID="home" data-role="page">
	        <div data-role="header">
		        <h1><asp:Label runat="server" Text="P2C Reimbursements" /></h1>
	        </div><!-- /header -->

	        <div data-role="content">	
		        <img class="feature_image" alt="P2C Reimbursements" src="/Portals/0/Images/reimbursement.jpg" />		
                <div>
                    <asp:Button ID="btnList" runat="server" Text="My Active Reimbursements" OnClientClick="location.href='#my_active_list';"/>
                </div>
	        </div><!-- /content -->
        </div><!-- /page -->



        <div ID="my_active_list" data-role="page">
	        <div data-role="header">
		        <h1><asp:Label runat="server" Text="Active Reimbursements" /></h1>
	        </div><!-- /header -->

	        <div data-role="content">	
                <div>
                    <asp:UpdatePanel id="upActiveList" runat="server">
                        <ContentTemplate>
                            <ul data-role="listview" data-inset="true" width="100%">
                            <asp:Repeater ID="dlActiveList" runat="server">
                                <ItemTemplate>
                                    <li><asp:Button runat="server" Text='<%# Eval("RMBNo")%>' CommandName='GoTo' CommandArgument='<%# Eval("RMBNo")%>' OnClientClick="location.href='#rmb_details';" /></li>
                                </ItemTemplate>
                            </asp:Repeater>
                            </ul>
                        </ContentTemplate>
                        <Triggers>
                            <asp:AsyncPostBackTrigger ControlID="btnList" />
                        </Triggers>
                    </asp:UpdatePanel>
                </div>
	        </div><!-- /content -->
        </div><!-- /page -->



        <div id="rmb_details" data-role="page">
	        <div data-role="header">
		        <h1><asp:Label runat="server" Text="Reimbursement" /></h1>
	        </div><!-- /header -->

	        <div data-role="content">	
                    <!--TODO: Remove Value here-->
                    <asp:HiddenField ID="hfRmbNo" runat="server" Value="" />
                    <asp:HiddenField ID="hfStaffInitials" runat="server" Value="KFC" />
                    <asp:UpdatePanel id="upDetails" runat="server">
                        <ContentTemplate>
                            <asp:Label id="lblLoadingDetails" runat="server" Text="Loading..."></asp:Label>
                            <asp:GridView ID="gvRmbLines" class="rmbDetails" runat="server" AutoGenerateColumns="False" DataKeyNames="RmbLineNo"
                                CellPadding="4" ForeColor="#333333" GridLines="None" Width="100%" ShowFooter="True">
                                <RowStyle CssClass="dnnGridItem" />
                                <AlternatingRowStyle CssClass="dnnGridAltItem" />
                                <Columns>
                                    <asp:TemplateField HeaderText="TransDate" SortExpression="TransDate">
                                        <ItemTemplate>
                                            <asp:Label ID="lblDate" runat="server" CssClass='<%# IIF(Eval("OutOfDate"), "ui-state-highlight ui-corner-all","") %>' ToolTip='<%# IIF(Eval("OutOfDate"),Translate("OutOfDate"),"") %>' Text='<%# Bind("TransDate", "{0:d}") %>'></asp:Label>
                                        </ItemTemplate>
                                        <ItemStyle HorizontalAlign="Left" Width="50px" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Extra" SortExpression="Spare1">
                                        <EditItemTemplate>
                                        </EditItemTemplate>
                                        <ItemTemplate>
                                            <asp:Label ID="lblExtra" runat="server" Text='<%# Eval("Spare1")%>'></asp:Label>
                                        </ItemTemplate>
                                        <ItemStyle HorizontalAlign="Center" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Line Type" SortExpression="LineType" ItemStyle-Width="110px">
                                        <EditItemTemplate>
                                            <asp:TextBox ID="TextBox1" runat="server" Text='<%# Bind("LineType") %>'></asp:TextBox>
                                        </EditItemTemplate>
                                        <ItemTemplate>
                                            <asp:Label ID="Label1" runat="server" Text='<%# GetLocalTypeName(Eval("AP_Staff_RmbLineType.LineTypeId") )  & If(Eval("LineType")=31, " " & GetMileageString(If(Eval("Mileage"), 0), If(Eval("Spare3"), "0")) ,"") %>'></asp:Label>
                                            <asp:Label ID="lblToFrom" runat="server" Font-Size="XX-Small" ForeColor="#AAAAAA" Font-Names="Courier" Visible=<%# If(Eval("LineType")=31,"True","False") %> Text=<%# If(Eval("Spare4") IsNot Nothing And Eval("Spare5") IsNot Nothing, Left(Eval("Spare4"),9) & " - " & Left(Eval("Spare5"),9),"") %>></asp:Label>
                                        </ItemTemplate>
                                        <ItemStyle HorizontalAlign="Left" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Comment" SortExpression="Comment">
                                        <EditItemTemplate>
                                        </EditItemTemplate>
                                        <ItemTemplate>
                                            <asp:Label ID="lblComment" runat="server" Text='<%#  Eval("Comment")  %>'></asp:Label>
                                            <asp:Panel ID="pnlRemBal1" runat="server" Visible='<%# CanEdit(Eval("AP_Staff_Rmb.Status")) and IsAccounts()  %>'>
                                                <asp:Label ID="lblTrimmedComment" runat="server" Font-Size="X-Small" ForeColor="#AAAAAA" Font-Names="Courier" Text='<%# GetLineComment(Eval("Comment"), Eval("OrigCurrency"), Eval("OrigCurrencyAmount"), Eval("ShortComment"))%>'></asp:Label>
                                            </asp:Panel>

                                        </ItemTemplate>
                                        <FooterTemplate>
                                            <asp:Label ID="lblTotalAmount" runat="server" Font-Bold="True" Text="Total:"></asp:Label>
                                            <asp:Panel ID="pnlRemBal1" runat="server" Visible='<%# Settings("ShowRemBal") = "True" %>'>
                                                <asp:Label ID="lblRemainingBalance" runat="server" Font-Size="XX-Small" ForeColor="#AAAAAA" Font-Italic="true" Text="Estimated Remaining Balance:"></asp:Label>
                                            </asp:Panel>
                                        </FooterTemplate>
                                        <ItemStyle HorizontalAlign="Left" />
                                        <FooterStyle HorizontalAlign="Right" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Amount" SortExpression="GrossAmount" ItemStyle-Width="75px">
                                        <EditItemTemplate>
                                        </EditItemTemplate>
                                        <ItemTemplate>
                                            <asp:Label ID="lblAmount" runat="server" CssClass='<%# IIF(Eval("LargeTransaction"), "ui-state-highlight ui-corner-all","") %>' ToolTip='<%# IIF(Eval("LargeTransaction"),Translate("LargeTransaction"),"") %>' Text='<%#  Eval("GrossAmount", "{0:F2}") & IIF(Eval("Taxable")=True, "*", "") %>'></asp:Label>

                                            <asp:Panel ID="pnlCur" runat="server" Visible='<%# Not String.IsNullOrEmpty(Eval("OrigCurrency")) And Eval("OrigCurrency") <> StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)%>'>
                                                <asp:Label ID="lblCur" runat="server" Font-Size="XX-Small" ForeColor="#AAAAAA" Text='<%# Eval("OrigCurrency") & Eval("OrigCurrencyAmount", "{0:F2}")%>' 
                                                    CssClass='<%# If(Eval("ExchangeRate") IsNot Nothing, If(IsDifferentExchangeRate(Eval("ExchangeRate"), Eval("OrigCurrencyAmount") / Eval("GrossAmount")), "highlight", ""), "")%>' 
                                                    ToolTip='<%# If(Eval("ExchangeRate") IsNot Nothing, If(IsDifferentExchangeRate(Eval("ExchangeRate"), Eval("OrigCurrencyAmount") / Eval("GrossAmount")), Translate("DifferentExchangeRate"), ""), "")%>'></asp:Label>
                                            </asp:Panel>
                                        </ItemTemplate>
                                        <FooterTemplate>
                                            <asp:Label ID="lblTotalAmount" runat="server" Text='<%# StaffBrokerFunctions.GetSetting("Currency", PortalId) & GetTotal(-1).ToString("F2") %>'></asp:Label>
                                            <asp:Panel ID="pnlRemBal2" runat="server" Visible='<%# Settings("ShowRemBal") = "True"%>'>
                                                <asp:Label ID="lblRemainingBalance" runat="server" Font-Size="xx-small" Text=''></asp:Label>
                                            </asp:Panel>
                                        </FooterTemplate>
                                        <ItemStyle HorizontalAlign="Right" />
                                        <FooterStyle HorizontalAlign="Right" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Receipt" ItemStyle-Width="20px">
                                        <ItemTemplate>
                                            <%# If(Not Eval("Receipt"), "<img src='/Icons/Sigma/no_receipt_32x32.png' width=20 alt='none' title='no receipt' />",
                                                    If(Eval("ReceiptImageId") Is Nothing, "<img src='/Icons/Sigma/BulkMail_32X32_Standard.png' width=20 alt='mail' title='receipt will be sent by mail'/>",
                                                    ElectronicReceiptTags(Eval("RmbLineNo"))))
                                            %>
                                        </ItemTemplate>
                                        <ItemStyle HorizontalAlign="Center" />
                                    </asp:TemplateField>
                                                
                                    <asp:TemplateField HeaderText="" ItemStyle-Width="10px" ItemStyle-Wrap="false">
                                        <EditItemTemplate>
                                        </EditItemTemplate>
                                        <ItemTemplate>
                                            <asp:LinkButton ID="LinkButton5" runat="server" CommandName="myEdit" Visible='<%# CanEdit(Eval("AP_Staff_Rmb.Status"))%>'
                                                CommandArgument='<%# Eval("RmbLineNo") %>' resourcekey="Edit"></asp:LinkButton>
                                            <asp:LinkButton ID="LinkButton4" runat="server" CommandName="myDelete" Visible='<%# CanEdit(Eval("AP_Staff_Rmb.Status")) %>' CssClass="confirm"
                                                CommandArgument='<%# Eval("RmbLineNo") %>' resourcekey="Delete"></asp:LinkButton>
                                            <asp:Panel ID="Accounts" runat="server" Visible='<%# (CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.RmbStatus.Paid and CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.rmbStatus.Processing and CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.rmbStatus.DownloadFailed and CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.rmbStatus.PendingDownload)  and IsAccounts()  %>'>
                                                <asp:LinkButton ID="LinkButton6" runat="server" CommandName="mySplit"
                                                    CommandArgument='<%# Eval("RmbLineNo") %>' resourcekey="Split"></asp:LinkButton>
                                                <asp:LinkButton ID="LinkButton7" runat="server" CommandName="myDefer" ToolTip="Moves this transaction to a new 'Pending' Reimbursement."
                                                    CommandArgument='<%# Eval("RmbLineNo") %>' resourcekey="Defer" Text="Defer"></asp:LinkButton>

                                            </asp:Panel>


                                        </ItemTemplate>
                                        <ItemStyle HorizontalAlign="Left" />
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="" ItemStyle-Width="10px" ItemStyle-Wrap="false">
                                        <EditItemTemplate>
                                        </EditItemTemplate>
                                        <ItemTemplate>
                                        </ItemTemplate>
                                        <ItemStyle HorizontalAlign="Left" />
                                    </asp:TemplateField>
                                </Columns>
                                <FooterStyle CssClass="ui-widget-header dnnGridFooter acGridHeader" />
                                <HeaderStyle CssClass="ui-widget-header dnnGridHeader acGridHeader" />
                                <PagerStyle CssClass="dnnGridPager" />
                                <SelectedRowStyle CssClass="dnnFormError" />
                            </asp:GridView>
                        </ContentTemplate>
                        <Triggers>
                            <asp:AsyncPostBackTrigger ControlID="dlActiveList" />
                        </Triggers>
                    </asp:UpdatePanel>
	        </div><!-- /content -->
        </div><!-- /page -->
    </form>
</body>
</html>
