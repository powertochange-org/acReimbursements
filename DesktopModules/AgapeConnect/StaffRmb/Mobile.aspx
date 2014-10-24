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
<div data-role="content">	
		        <img class="feature_image" alt="P2C Reimbursements" src="/Portals/0/Images/reimbursement.jpg" />		
                <div style="position:fixed; top:50%;left:30%;">
                    <H2>Swipe Left <---</H2>
                    <div class="hidden">
                        <asp:Button ID="btnList" runat="server" OnClick="loadRmbList" OnClientClick="showListPage();"  />
                    </div>
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
                            <asp:Repeater ID="dlActiveList" runat="server">
                                <ItemTemplate>
                                    <asp:Button runat="server" Text='<%# Eval("UserRef") + Environment.NewLine + Eval("Total")%>' CommandName="LoadRMB" CommandArgument='<%# Eval("RMBNo")%>' CssClass='<%# "round_button " & StaffRmb.RmbStatus.StatusName(Eval("Status")).ToLower()%>' OnClientClick="showExpenses();"/>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ContentTemplate>
                        <Triggers>
                            <asp:AsyncPostBackTrigger ControlId="btnList" />
                        </Triggers>
                    </asp:UpdatePanel>
                </div>
	        </div><!-- /content -->
        </div><!-- /page -->


        <div id="expenses" data-role="page">
	        <div ID="expenses_header" data-role="header">
		        <h1><asp:Label runat="server" Text="Expenses" /></h1>
	        </div><!-- /header -->

	        <div data-role="content">	
                    <!--TODO: Remove Value here-->
                    <asp:HiddenField ID="hfRmbNo" runat="server" Value="" />
                    <asp:HiddenField ID="hfStaffInitials" runat="server" Value="KFC" />
                    <asp:UpdatePanel id="upDetails" runat="server">
                        <ContentTemplate>
                            <asp:panel id="pnlLoadingDetails" runat="server" CssClass="loading_spinner">
                                <img alt="..."src="/Portals/_default/Skins/carmel/images/ui-anim_basic_16x16.gif" /><br />loading
                            </asp:panel>
                            <asp:GridView ID="gvRmbLines" class="rmbDetails" runat="server" AutoGenerateColumns="False" DataKeyNames="RmbLineNo"
                                CellPadding="4" ForeColor="#333333" GridLines="None" Width="100%" ShowFooter="True">
                                <RowStyle CssClass="dnnGridItem" />
                                <AlternatingRowStyle CssClass="dnnGridAltItem" />
                                <Columns>
                                    <asp:TemplateField HeaderText="Date" SortExpression="TransDate">
                                        <ItemTemplate>
                                            <div class="calendar">
                                                <asp:Label ID="lblDate" runat="server" CssClass='<%# If(Eval("OutOfDate"), "highlight", "")%>' Text='<%# Bind("TransDate", "{0: d }")%>' />
                                            </div>
                                        </ItemTemplate>
                                        <ItemStyle HorizontalAlign="Left" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Type" SortExpression="LineType" ItemStyle-Width="48px" ItemStyle-VerticalAlign="middle">
                                        <ItemTemplate>
                                            <asp:Panel runat="server" CssClass='<%# GetLocalTypeName(Eval("AP_Staff_RmbLineType.LineTypeId")).ToLower()  & " icon expense_type"%>' />
                                        </ItemTemplate>
                                        <ItemStyle HorizontalAlign="Left" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Description" SortExpression="Comment">
                                        <EditItemTemplate>
                                        </EditItemTemplate>
                                        <ItemTemplate>
                                            <asp:Label ID="lblComment" runat="server" Text='<%# Eval("Comment")%>' CssClass="comment"></asp:Label>
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
                                            <asp:Label ID="lblAmount" runat="server" CssClass='amount' Text='<%#  Eval("GrossAmount", "{0:F2}") & IIF(Eval("Taxable")=True, "*", "") %>'></asp:Label>
                                            <asp:Panel ID="pnlCur" runat="server" Visible='<%# Not String.IsNullOrEmpty(Eval("OrigCurrency")) And Eval("OrigCurrency") <> StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)%>'>
                                                <asp:Label ID="lblCur" runat="server" Font-Size="XX-Small" ForeColor="#AAAAAA" Text='<%# Eval("OrigCurrency") & Eval("OrigCurrencyAmount", "{0:F2}")%>' 
                                                    CssClass='<%# If(Eval("ExchangeRate") IsNot Nothing, If(IsDifferentExchangeRate(Eval("ExchangeRate"), Eval("OrigCurrencyAmount") / Eval("GrossAmount")), "highlight", ""), "")%>' ></asp:Label>
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

                                    <asp:TemplateField HeaderText="Receipt" ItemStyle-Width="48px">
                                        <ItemTemplate>
                                            <%# If(Not Eval("Receipt"), "<img class='icon' src='/Icons/Sigma/no_receipt_32x32.png' width=20 alt='none' title='no receipt' />",
                                                    If(Eval("ReceiptImageId") Is Nothing, "<img class='icon' src='/Icons/Sigma/BulkMail_32X32_Standard.png' width=20 alt='mail' title='receipt will be sent by mail'/>",
                                                    ElectronicReceiptTags(Eval("RmbLineNo"))))%>
                                        </ItemTemplate>
                                        <ItemStyle HorizontalAlign="Center" />
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

        <div id="reimbursement_details" data-role="popup" data-corners="false" data-theme="none" data-shadow="false" data-tolerance="0,0">
	        <div data-role="header">
		        <h1><asp:Label runat="server" Text="Reimbursement" /></h1>
	        </div><!-- /header -->

	        <div data-role="content">	

	        </div><!-- /content -->
        </div><!-- /page -->


    </form>

    <script type="text/javascript">
        function showExpenses() {
            $('#gvRmbLines tr').remove();
            $('#pnlLoadingDetails').show();
            $.mobile.changePage("#expenses", { transition: "flow" });
        }
        function showListPage() {
            $.mobile.changePage("#my_active_list", { transition: "slide" });
        }
        function showReimbursementHeader() {
            $('#reimbursement_details').popup("open", {transition: "slidedown", position:"window"});
        }

        $(document).ready(function () {
            //HOME
            $('#home').on("swipeleft", function (event) {
                __doPostBack('btnList', '');
                showListPage();
            })

            //ACTIVE LIST
            $('#my_active_list').on("swiperight", function (event) {
                $.mobile.back();
            })

            //EXPENSE DETAILS
            $("#expenses_header").click(function () {
                showReimbursementHeader();
            })
            $('#expenses_header').on("swipedown", function (event) {
                showReimbursementHeader();
            })
            $('#expenses').on("swiperight", function (event) {
                $.mobile.back();
            })

            //REIMBURSEMENT
            $('#reimbursement_details').popup();
            $('#reimbursement_details').on("swipeup", function (event) {
                $.mobile.back();
            })
        })
    </script>
</body>
</html>
