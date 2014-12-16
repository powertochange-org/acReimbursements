<%@ Control Language="VB" AutoEventWireup="false" CodeFile="Advance.ascx.vb" Inherits="RmbAdvance" %>
<%@ Register assembly="DotNetNuke" namespace="DotNetNuke.UI.WebControls" tagprefix="cc1" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<div class="Agape_SubTitle">
    <div id="uncleared"> 
        <asp:HiddenField ID="hfUnclearedAmount" runat="server" Value="" />
    </div>
    <asp:Label ID="Label5" runat="server" Font-Italic="true" ForeColor="Gray" CssClass="explanation" resourcekey="Explanation"></asp:Label>
</div><br />
<table   style="font-size:9pt; ">
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
                            <asp:TextBox ID="tbAmount" runat="server" Width="90px" class="numeric rmbAmount" onkeyup="$('#uncleared>input').val($(this).val());"></asp:TextBox>
                        </td>

                    </tr>
                </table></td>
    </tr>

</table>
 <asp:Label ID="ErrorLbl" runat="server" Font-Size="9pt" ForeColor="Red" />
