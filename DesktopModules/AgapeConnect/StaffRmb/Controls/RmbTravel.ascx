<%@ Control Language="VB" AutoEventWireup="false" CodeFile="RmbTravel.ascx.vb" Inherits="controls_RmbTravel" ClassName="controls_RmbTravel"  %>
<%@ Register assembly="DotNetNuke" namespace="DotNetNuke.UI.WebControls" tagprefix="cc1" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<%@ Register src="Currency.ascx" tagname="Currency" tagprefix="uc1" %>

<div class="Agape_SubTitle"> 
    <asp:HiddenField ID="hfNoReceiptLimit" runat="server" Value="0" />
    <asp:HiddenField ID="hfCADValue" runat="server" Value="" />
    <asp:Label ID="Label6" runat="server" Font-Italic="true" ForeColor="Gray" CssClass="explanation" resourcekey="Explanation"></asp:Label>
  </div><br />

<table   style="font-size:9pt; ">
    <tr>
        <td width="150px;"><b><dnn:Label runat="server" ControlName="tbSupplier" ResourceKey="lblSupplier" /></b></td>
        <td><asp:TextBox ID="tbSupplier" runat="server" Width="278px"></asp:TextBox></td>
    </tr>
    <tr>
        <td width="200px;"><b><dnn:label id="Label4"  runat="server" controlname="tbDesc" ResourceKey="lblDesc"  /></b></td>
        <td><asp:TextBox ID="tbDesc" runat="server" maxlength="27" CSSStyle="width:15em" ></asp:TextBox></td>
    </tr>
    <tr>
     <td><b><dnn:label id="Label1"  runat="server" controlname="dtDate"  ResourceKey="lblDate"  /></b></td>
    <td  colspan="2">
        <asp:TextBox ID="dtDate" runat="server" Width="90px" class="datepicker" onChange="check_expense_date();"></asp:TextBox>
        <span id="olddatetext"></span>       
       <%-- <cc2:CalendarExtender ID="CalendarExtender1" runat="server" TargetControlID="dtDate"  Format="dd/MM/yyyy" >
        </cc2:CalendarExtender>
        <cc2:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" runat="server" TargetControlID="dtDate" ValidChars="0123456789/">
        </cc2:FilteredTextBoxExtender>--%>
    </td>
</tr>

<%--<tr>
    <td><b><dnn:label id="Label5"  runat="server" controlname="tbDesc" ResourceKey="lblType"  text="Type:" HelpText="Please enter the type of travel that best describes this expense" /></b></td>
  
    <td>
    <asp:UpdatePanel ID="UpdatePanel1" runat="server" >
        <ContentTemplate>
       <asp:DropDownList ID="DropDownList1" runat="server" AutoPostBack="true">
         <asp:ListItem Value="0" ResourceKey="Parking">Parking</asp:ListItem>
         <asp:ListItem Value="1" ResourceKey="Fuel">Fuel</asp:ListItem>
         <asp:ListItem Value="2" ResourceKey="Train">Train or bus ticket</asp:ListItem>
         <asp:ListItem Value="3" ResourceKey="Flight">Flight</asp:ListItem>
         <asp:ListItem Value="4" ResourceKey="Taxi">Taxi</asp:ListItem>
         <asp:ListItem Value="5" ResourceKey="Travelcard">Travelcard/Season Ticket</asp:ListItem>
         <asp:ListItem Value="6" ResourceKey="HireCar">Rental Car</asp:ListItem>
         <asp:ListItem Value="7" ResourceKey="Visa">Foreign Visa</asp:ListItem>
         <asp:ListItem Value="8" ResourceKey="TravelAgent">Travel Agent Fees</asp:ListItem>

         <asp:ListItem Value="9" ResourceKey="Other">Other</asp:ListItem>
           
   </asp:DropDownList>
        <asp:Panel ID="pnlTravelcard" runat="server">
     
            <asp:Label ID="Label7" runat="server" ResourceKey="lblTravelcard"></asp:Label>
     
         <asp:DropDownList ID="DropDownList3" runat="server">
         <asp:ListItem Value="Yes" ResourceKey="Yes">Yes</asp:ListItem>
         <asp:ListItem Value="No" ResourceKey="No">No</asp:ListItem>
     </asp:DropDownList>
     <br />

     </asp:Panel>
     <br />
            <asp:Panel ID="pnlWorlPlace" runat="server" Visible="false">
            
            <asp:Label ID="Label8" runat="server" ResourceKey="lblNormal"></asp:Label>
     
         <asp:DropDownList ID="ddlWorkplace" runat="server">
         <asp:ListItem Value="Yes"  ResourceKey="Yes">Yes</asp:ListItem>
         <asp:ListItem Value="No"  ResourceKey="No" Selected="True">No</asp:ListItem>
     </asp:DropDownList></asp:Panel>
        </ContentTemplate>
        


        </asp:UpdatePanel>
        
   
    </td>
</tr>--%>

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
                    <asp:ListItem Text="Quebec" Value="PQ">
                    </asp:ListItem>
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
<tr id="ReceiptLine" runat="server">
    <td><b><dnn:label id="ttlReceipt"  runat="server" controlname="ddlVATReceipt" /></b></td>
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

