﻿<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ReceiptEditor.aspx.vb" Inherits="DesktopModules_AgapeConnect_StaffRmb_ReceiptEditor" Async="true" AsyncTimeout="60"%>



<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script src="/Resources/Shared/Scripts/jquery/jquery.min.js?cdv=34" type="text/javascript"></script>
    <script src="/Resources/Shared/Scripts/jquery/jquery-ui.min.js?cdv=34" type="text/javascript"></script>
    <script src="/DesktopModules/AgapeConnect/StaffRmb/js/qrcode.min.js" type="text/javascript" ></script>
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
        <span id="rotate_instructions">Please rotate images right-side-up.</span>
        <div>
            <div style="width: 100%; text-align: left;">
                <input type="button" class="aButton" onclick="fuReceipt.click()" value="Add receipt..." style="font-size:small" />
                <asp:FileUpload ID="fuReceipt" runat="server" style="display:none" OnChange="$('#btnUploadReceipt').click();" />
                <asp:Button ID="btnUploadReceipt" runat="server" Text="Upload Selected File" CssClass="aButton" Style="display:none" Font-Size="small" />
                <asp:UpdatePanel ID="receipts" runat="server" style="position:absolute">
                    <ContentTemplate>
                        <div id="currentReceipts" runat="server"></div>
                        <asp:Label ID="lblError" runat="server" ForeColor="Red"></asp:Label>
                        <asp:Timer ID="refresh_timer" runat="server" interval="7000" />
                    </ContentTemplate>
                </asp:UpdatePanel>
                <div id="qrcode" ></div>
            </div>
        </div>
        <asp:UpdatePanel runat="server" >
            <ContentTemplate>
                <asp:HiddenField ID="hfQR" ClientIDMode="Static" runat="server" />
            </ContentTemplate>
        </asp:UpdatePanel>
    </form>
    <script type="text/javascript">
        var qrcode;
        var isMobile = navigator.userAgent.match(/Android|iPhone|iPad|iPod/i);
        function generateQRCode() {
            var link = $('#hfQR').val();
            if (link == "") return;
            if (qrcode) {
                qrcode.clear();
                qrcode.makeCode(link);
            } else {
                $('#qrcode').empty();
                qrcode = new QRCode('qrcode', { width:128, height:128 }).makeCode(link);
            }
        }
        $(document).ready(function () {
            if (isMobile) {
                $('#qrcode').empty();
            } else {
                generateQRCode();
            }
        })
    </script>
</body>
</html>
