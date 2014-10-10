<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ReceiptEditor.aspx.vb" Inherits="DesktopModules_AgapeConnect_StaffRmb_ReceiptEditor" Async="true" AsyncTimeout="60"%>



<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script src="/Resources/Shared/Scripts/jquery/jquery.min.js?cdv=34" type="text/javascript"></script>
    <script src="/Resources/Shared/Scripts/jquery/jquery-ui.min.js?cdv=34" type="text/javascript"></script>
    <link href="/Portals/_default/Skins/AgapeBlue/skinPopup.css?cdv=34" type="text/css" rel="stylesheet">
    <script>
        $(function () {



            $(".aButton")
              .button()
              ;
        });
    </script>
    <style>
        .aButton {
            padding: .4em !important;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <div style="width: 100%; text-align: left;">
                <input type="button" class="aButton" onclick="fuReceipt.click()" value="Add receipt..." style="font-size:small" />
                <asp:FileUpload ID="fuReceipt" runat="server" style="display:none" OnChange="$('#btnUploadReceipt').click();" />
                <asp:Button ID="btnUploadReceipt" runat="server" Text="Upload Selected File" CssClass="aButton" Style="display:none" Font-Size="small" />
                <div id="currentReceipts" runat="server">
		</div>
                <asp:Label ID="lblError" runat="server" ForeColor="Red"></asp:Label>
            </div>
        </div>
    </form>
</body>
</html>
