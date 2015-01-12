<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CtrlAdvance.ascx.cs" Inherits="ControlBase" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>


<table style="font-size:9pt; ">
    <tr>
        <td style="width:200px;"><b><dnn:Label runat="server" ControlName="cbClearExternal" ResourceKey="lblClearExternal" style="text-align:left" /></b></td>
        <td colspan="2">
            <asp:CheckBox ID="cbClearExternal" runat="server" onclick="$('.rmbAmount').val(-$('.rmbAmount').val()); $('.equivalentCAD').val($('rmbAmount').val()); $('.rmbAmount').prop('disabled', $(this).is(':checked'));"/>
            <asp:label runat="server" resourceKey="cbClearExternal" /> 
        </td>
    </tr>
</table>