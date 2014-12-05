<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CtrlEquipment.ascx.cs" Inherits="ControlBase" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<table style="font-size:9pt; ">
    <tr>
        <td style="width:200px">
            <b>
                <dnn:Label ID="lblMoreInfo" runat="server" ControlName="ddlVATReceipt" ResourceKey="lblMoreInfo" />
            </b>
        </td>
        <td>
            <asp:DropDownList ID="ddlType" runat="server">
                <asp:ListItem Value="0" ResourceKey="itmComputer">Computer</asp:ListItem>
                <asp:ListItem Value="1" ResourceKey="itmMobile">Software</asp:ListItem>
                <asp:ListItem Value="2" ResourceKey="itmPeripheral">Computer Peripheral</asp:ListItem>
                <asp:ListItem Value="3" Selected="True" ResourceKey="itmOther">Other</asp:ListItem>
            </asp:DropDownList>
        </td>
    </tr>
</table>
<asp:Label ID="ErrorLbl1" runat="server" Font-Size="9pt" ForeColor="Red" />
