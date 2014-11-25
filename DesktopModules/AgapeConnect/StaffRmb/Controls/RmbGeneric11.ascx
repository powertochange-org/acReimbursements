<%@ Control Language="VB" AutoEventWireup="false" CodeFile="RmbGeneric11.ascx.vb" Inherits="controls_RmbGeneric" ClassName="controls_RmbGeneric"  %>
<%@ Register assembly="DotNetNuke" namespace="DotNetNuke.UI.WebControls" tagprefix="cc1" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<%@ Register src="Currency.ascx" tagname="Currency" tagprefix="uc1" %>

<div class="Agape_SubTitle"> 
    <asp:HiddenField ID="hfNoReceiptLimit" runat="server" Value="0" />
    <asp:HiddenField ID="hfCADValue" runat="server" Value="" />
    <asp:Label ID="Label5" runat="server" Font-Italic="true" ForeColor="Gray" CssClass="explanation" resourcekey="Explanation"></asp:Label>
</div><br />
<table   style="font-size:9pt; ">
<tr>
    <td width="150px;"><b><dnn:Label runat="server" ControlName="tbSupplier" ResourceKey="lblSupplier" /></b></td>
    <td><asp:TextBox ID="tbSupplier" runat="server" Width="278px"></asp:TextBox></td>
</tr>
<tr>
    <td width="200px;"><b><dnn:label id="Label4"  runat="server" controlname="tbDesc" ResourceKey="lblDesc"  /></b></td>
    <td><asp:TextBox ID="tbDesc" runat="server" maxlength="27" CSSStyle="width:15em"> </asp:TextBox></td>
</tr>
<tr>
  <td><b><dnn:label id="Label1"  runat="server" controlname="dtDate" ResourceKey="lblDate"  /></b></td>
    <td  colspan="2">
        <asp:TextBox ID="dtDate" runat="server" Width="90px" class="datepicker" onChange="check_expense_date();"></asp:TextBox>
        <span id="olddatetext"></span>       
       
    </td>
</tr>
<tr>
     <td><b><dnn:label id="Label2"  runat="server" controlname="tbAmount" ResourceKey="lblAmount"  /></b></td>
   <td><table>
                <tr>
                    <td>
                        <asp:TextBox ID="tbAmount" runat="server" Width="90px" class="numeric rmbAmount"></asp:TextBox>
                    </td>
                    <td>
                        <uc1:Currency ID="Currency1" runat="server" />
                    </td>
                </tr>
            </table></td>
</tr>
<tr><td><b><dnn:Label ID="lblProvince" runat="server" controlname="ddlProvince" ResourceKey="lblProvince" /></b></td>
    <td ><asp:DropDownList ID="ddlProvince" CssClass="ddlProvince" runat="server">
            <asp:ListItem Text="British Columbia" Value="BC" />
            <asp:ListItem Text="Alberta" Value="AB" />
            <asp:ListItem Text="Saskatchewan" Value="SK" />
            <asp:ListItem Text="Manitoba" Value="MB" />
            <asp:ListItem Text="Ontario" Value="ON" />
            <asp:ListItem Text="Quebec" Value="QC" />
            <asp:ListItem Text="Newfoundland" Value="NL" />
            <asp:ListItem Text="Nova Scotia" Value="NS" />
            <asp:ListItem Text="New Brunswick" Value="NB" />
            <asp:ListItem Text="Prince Edward Is." Value="PE" />
            <asp:ListItem Text="Yukon" Value="YT" />
            <asp:ListItem Text="Nunavut" Value="NV" />
            <asp:ListItem Text="Northwest Terr." Value="NT" />
            <asp:ListItem Text="Outside Canada" Value="--" />
        </asp:DropDownList></td>
</tr>
<tr  id="ReceiptLine" runat="server">
    <td><b><dnn:label id="ttlReceipt"  runat="server" controlname="ddlVATReceipt"   /></b></td>
    <td>
        <asp:DropDownList ID="ddlVATReceipt" runat="server"  CssClass="ddlReceipt">
            <asp:ListItem ResourceKey="VAT" Value="0">VAT</asp:ListItem>
            <asp:ListItem ResourceKey="Standard" Value="1">Standard</asp:ListItem>
            <asp:ListItem  Value="2" ResourceKey="Electronic">Electronic Receipt</asp:ListItem>
            <asp:ListItem  Value="-1">No Receipt (under [LIMIT])</asp:ListItem>
        </asp:DropDownList>
    </td>
</tr>
</table>
 <asp:Label ID="ErrorLbl" runat="server" Font-Size="9pt" ForeColor="Red" />

