<%@ Control Language="VB" AutoEventWireup="false" CodeFile="RmbSubPD.ascx.vb" Inherits="controls_RmbSubPD" ClassName="controls_RmbSubPD"  %>

<%@ Register assembly="DotNetNuke" namespace="DotNetNuke.UI.WebControls" tagprefix="cc1" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<div class="Agape_SubTitle"> 
    <asp:HiddenField ID="hfNoReceiptLimit" runat="server" Value="0" />
    <asp:HiddenField ID="hfCADValue" runat="server" Value="" />
    <asp:Label ID="Label3" runat="server" Font-Italic="true" ForeColor="Gray" CssClass="explanation" resourcekey="Explanation"></asp:Label>
</div><br />

<table   style="font-size:9pt; ">

    <tr>
         <td><b><dnn:label id="Label1"  runat="server" controlname="dtDate" ResourceKey="lblDate"  /></b></td>
        <td  colspan="2">
            <asp:TextBox ID="dtDate" runat="server" Width="90px" class="datepicker" onChange="check_expense_date();"></asp:TextBox>
            <span id="olddatetext"></span>       
        
        </td>
    </tr>
    <tr>
        <td><b><dnn:Label runat="server" ControlName="phMeals" ResourceKey="lblMeals" /></b></td>
        <td colspan="2">
            <div>
                <asp:CheckBox ID="cbBreakfast" runat="server" CssClass="perdiem" OnClick="updatePerDiemTotal();"/>
                <b><%=Translate("lblBreakfast")%></b> (<%= FormatCurrency(breakfastValue)%>)
            </div>
            <div>
                <asp:CheckBox ID="cbLunch" runat="server" CssClass="perdiem"  OnClick="updatePerDiemTotal();"/>
                <b><%=Translate("lblLunch")%></b> (<%= FormatCurrency(lunchValue)%>)
            </div>
            <div>
                <asp:CheckBox ID="cbSupper" runat="server" CssClass="perdiem"  OnClick="updatePerDiemTotal();"/>
                <b><%=Translate("lblSupper")%></b> (<%= FormatCurrency(supperValue)%>)
            </div>
        </td>
    </tr>
    <tr>
        <td>
            <b><dnn:label id="lbl"  runat="server" controlname="tbAmount" ResourceKey="lblTotal" /></b>
         </td>
        <td colspan="2">
            <asp:TextBox id="tbAmount" runat="server" cssclass="PDAmount" Enabled="False" Width="40px" />
        </td>
    </tr>
    <tr><td><b><dnn:Label ID="lblProvince" runat="server" controlname="ddlProvince" ResourceKey="lblProvince" /></b></td>
        <td ><asp:DropDownList ID="ddlProvince" CssClass="ddlProvince" runat="server">
                <asp:ListItem Text="British Columbia" Value="BC" />
                <asp:ListItem Text="Alberta" Value="AB" />
                <asp:ListItem Text="Saskatchewan" Value="SK" />
                <asp:ListItem Text="Manitoba" Value="MB" />
                <asp:ListItem Text="Ontario" Value="ON" />
                <asp:ListItem Text="Quebec" Value="PQ" />
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
    <tr>
        <td><b><dnn:Label id="lblRepeat" runat="server" ControlName="tbRepeat" ResourceKey="lblRepeat" Visible="False" /></b></td>
        <td colspan="2" style="text-align:left"><asp:TextBox id="tbRepeat" runat="server" Width="25px" Visible="false" /></td>
    </tr>
</table>
<asp:Label ID="ErrorLbl" runat="server" Font-Size="9pt" ForeColor="Red" />


