<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CtrlPerDiem.ascx.cs" Inherits="ControlBase" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>


<table style="font-size:9pt; ">
    <tr>
        <td style="width:200px;"><b><dnn:Label runat="server" ControlName="phMeals" ResourceKey="lblMeals" /></b></td>
        <td colspan="2">
            <table style="font-size:9pt">
                <tr>
                    <td >
                        <asp:CheckBox ID="cbBreakfast" runat="server" CssClass="perdiem" OnClick="updatePerDiem($('.pdbreakfast'),$(this).is(':checked'));"/>
                        <b><asp:label runat="server" resourceKey="lblBreakfast" /></b> 
                    </td>
                    <td>
                        <asp:TextBox ID="tbBreakfast" runat="server" CssClass="number pdbreakfast" Width="50" Enabled="false" OnKeyup="calculatePerDiemTotal();" />
                        (max: <asp:label ID="lblMaxBreakfast" runat="server" />)
                    </td>
                </tr>
                <tr>
                    <td>
                        <asp:CheckBox ID="cbLunch" runat="server" CssClass="perdiem"  OnClick="updatePerDiem($('.pdlunch'),$(this).is(':checked'));"/>
                        <b><asp:label runat="server" resourcekey="lblLunch"/></b>
                    </td>
                    <td>
                        <asp:TextBox ID="tbLunch" runat="server" CssClass="number pdlunch" Width="50" enabled="false" OnKeyup="calculatePerDiemTotal();"/>
                            (max: <asp:Label ID="lblMaxLunch" runat="server" />)
                    </td>
                </tr>
                <tr>
                    <td>
                        <asp:CheckBox ID="cbSupper" runat="server" CssClass="perdiem" OnClick="updatePerDiem($('.pdsupper'),$(this).is(':checked'));"/>
                        <b><asp:label runat="server" resourcekey="lblSupper" /></b>
                    </td>
                    <td>
                        <asp:TextBox ID="tbSupper" runat="server" CssClass="number pdsupper" Width="50" enabled="false" OnKeyup="calculatePerDiemTotal();"/>
                            (max: <asp:Label ID=lblMaxSupper runat=server />)
                    </td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td></td>
        <td>
            <b><dnn:label id="lbl"  runat="server"  ResourceKey="lblTotal" />:</b>
            $<span id="tbPDTotal">0.00</span>
        </td>
    </tr>
    <tr>
        <td><b><dnn:Label id="lblRepeat" runat="server" ControlName="tbRepeat" ResourceKey="lblRepeat" Visible="False" /></b></td>
        <td colspan="2" style="text-align:left"><asp:TextBox id="tbRepeat" runat="server" Width="25px" Visible="false" /></td>
    </tr>
</table>
<asp:HiddenField ID="hfBreakfast" runat="server" />
<asp:HiddenField ID="hfLunch" runat="server" />
<asp:HiddenField ID="hfSupper" runat="server" />
<asp:Label ID="ErrorLbl2" runat="server" Font-Size="9pt" ForeColor="Red" />