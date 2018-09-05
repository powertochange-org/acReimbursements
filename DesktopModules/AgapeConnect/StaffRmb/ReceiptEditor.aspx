<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ReceiptEditor.aspx.vb" Inherits="DesktopModules_AgapeConnect_StaffRmb_ReceiptEditor" Async="true" AsyncTimeout="60"%>



<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Expires" content="0"/>
    <meta http-equiv="Pragma" content="no-cache"/>
    <meta http-equiv="Cache-Control" content="no-cache"/>

    <title></title>
    <script src="/Resources/Shared/Scripts/jquery/jquery.min.js?cdv=34" type="text/javascript"></script>
    <script src="/Resources/Shared/Scripts/jquery/jquery-ui.min.js?cdv=34" type="text/javascript"></script>
    <link href="/Portals/_default/Skins/AgapeBlue/skinPopup.css?cdv=34" type="text/css" rel="stylesheet"/>

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
        
        #rotate_instructions {
            color:lightblue;
            opacity: 0.5;
            text-shadow: -2px -2px 1px #fff;
            font-family:sans-serif;
            font-size:20pt;
            font-weight:bold;
            position:absolute;
            z-index:-1;
            top:43px;
            left:45px;
        }

        #qrcode {
            display:inline-block; 
            position:fixed;
            right:5px;
            padding:5px; 
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager runat="server" />
        <span id="rotate_instructions">Please rotate images<br />right-side-up.</span>
        <div>
            <div style="width: 100%; text-align: left;">
                <input type="button" class="aButton" onclick="fuReceipt.click()" value="Add attachment..." style="font-size:small" />
                <asp:FileUpload ID="fuReceipt" runat="server" style="display:none" OnChange="$('#btnUploadReceipt').click();" />
                <asp:Button ID="btnUploadReceipt" runat="server" Text="Upload Selected File" CssClass="aButton" Style="display:none" Font-Size="small" />
                <asp:UpdatePanel ID="receipts" runat="server" style="position:absolute">
                    <ContentTemplate>
                        <div id="currentReceipts" runat="server"></div>
                        <asp:Label ID="lblError" runat="server" ForeColor="Red"></asp:Label>
                        <asp:Timer ID="refresh_timer" runat="server" interval="5000" />
                    </ContentTemplate>
                </asp:UpdatePanel>
                
                <img id="qrcode" runat="server" src="#" width="128" height="128" title="Scan this code to connect your smartphone to this reimbursement" alt="Save this line-item and re-open it to generate a QR code"/>
            </div>
        </div>
    </form>
    <script type="text/javascript">
        var qrcode;
        var isMobile = navigator.userAgent.match(/Android|iPhone|iPad|iPod/i);
        $(document).ready(function () {
            if (isMobile) {
                $('#qrcode').remove();
            }
        })
        function pageLoad() { $('.aButton').button();}
    </script>
</body>
</html>
