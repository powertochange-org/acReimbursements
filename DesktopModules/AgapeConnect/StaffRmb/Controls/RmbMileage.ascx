<%@ Control Language="VB" AutoEventWireup="false" CodeFile="RmbMileage.ascx.vb" Inherits="controls_Mileage" ClassName="controls_Mileage"  %>
<%@ Register assembly="DotNetNuke" namespace="DotNetNuke.UI.WebControls" tagprefix="cc1" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>


<asp:HiddenField ID="hfAddStaffRate" runat="server" />

<div class="Agape_SubTitle"> <asp:HiddenField ID="hfNoReceiptLimit" runat="server" Value="0" />
    <asp:Label ID="Label2" runat="server" Font-Italic="true" ForeColor="Gray" resourcekey="Explanation"></asp:Label>
</div>
<br />

<asp:UpdatePanel ID="UpdatePanel1" runat="server">
<ContentTemplate>
    <table style="font-size:9pt; ">
        <tr>
            <td ><b><dnn:label id="lblDesc"  runat="server" controlname="tbDesc" ResourceKey="lblDesc"  /></b></td>
            <td colspan="2"><asp:TextBox ID="tbDesc" runat="server" Width="450px"></asp:TextBox></td>
        </tr>
        <tr>
            <td><b><dnn:label id="lblDate" runat="server" controlname="dtDate"  ResourceKey="lblDate" /></b></td>
            <td colspan="2">
                <asp:TextBox ID="dtDate" runat="server" Width="90px" class="datepicker"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td>
                <b><dnn:Label ID="lblDistance" runat="server" ControlName="tbDistance" ResourceKey="lblDistance"   /></b>
            </td>
            <td>
                <asp:TextBox ID="tbDistance" runat="server" Width="90px" class="numeric"></asp:TextBox>
            </td>
        </tr>

        <tr id="pnlDistUnits" runat="server">
            <td><b><dnn:label id="lblDistUnits" runat="server" controlname="ddlDistUnits"  resourcekey="lblDistUnits" /></b></td>
            <td>
                <asp:DropDownList ID="ddlDistUnits" runat="server" AutoPostBack="true" ></asp:DropDownList>
            </td>
        </tr>
    </table>
    <asp:Label ID="ErrorLbl" runat="server" Font-Size="9pt" ForeColor="Red" />
</ContentTemplate>
<Triggers>
    <asp:PostBackTrigger ControlID="dtDate" />
    <asp:PostBackTrigger ControlID="lblDistance" />
    <asp:PostBackTrigger ControlID="ddlDistUnits" />
</Triggers>

</asp:UpdatePanel>
