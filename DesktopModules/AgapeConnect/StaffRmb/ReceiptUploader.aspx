<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ReceiptUploader.aspx.cs" Inherits="PowerToChange.Modules.StaffRmb.Views.ReceiptUploader" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <link rel="stylesheet" type="text/css" href="/DesktopModules/AgapeConnect/StaffRmb/css/ReceiptUploader.css" />
    <title></title>
</head>
<body onload="document.getElementById('fuCamera').click();">
    <form id="form1" runat="server" enctype="multipart/form-data">
        <asp:ScriptManager ID="ScriptManager" runat="server" />
        <div class="fullwidth fullheight">
            <div class="header row">
                <asp:FileUpload  ID="fuCamera" ClientIDMode="Static" runat="server" capture="camera" accept="image/*" CssClass="hidden" OnChange="file_chosen(this);"/>
                <asp:UpdatePanel runat="server">
                    <ContentTemplate>
                        <h2><asp:Label ID="lblTitle" runat="server" CssClass="center title" Text="<b>Rmb #[RID]</b><br/>Take a picture of<br/>your receipt" /></h2>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
            <div class="body row">
                <asp:UpdatePanel ID="upBody" runat="server" >
                    <ContentTemplate>
                        <asp:HiddenField ID="hfMessage" ClientIDMode="Static" runat="server" />
                        <asp:HiddenField ID="hfShutterState" ClientIDMode="Static" runat="server" />
                        <asp:Label ID="lblMessage" ClientIDMode="static" runat="server" CssClass="middle center message" style="width:50%; display:none"/>
                        <asp:panel ID="pnlShutter" ClientIDMode="Static" runat="server" CssClass="middle center">
                            <asp:ImageButton ID="btnShutter" runat="server" ImageUrl="../StaffRmb/images/shutter.png" AlternateText="Take Picture" OnClientClick="shutter_click(); return false;" style="margin-bottom:-30px"/>
                            <h4><asp:Label ID="lblShutter" runat="server" text="Launch Camera"  /></h4>
                        </asp:panel>
                        <asp:Panel ID="divOverlay" runat="server" CssClass="overlay" style="display:none"/>
                        <asp:image id="imgPreview" ClientIDMode="Static" runat="server" class="center image" alt="" style="max-height:100%; max-width:100%" />
                        <asp:Button class="hidden" id="btnSubmit" ClientIDMode="Static" runat="server" OnClick="Upload" />
                        <asp:HiddenField ID="image_data" ClientIDMode="static" runat="server" />
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
            <div class="footer row">
                <asp:UpdatePanel runat="server">
                    <ContentTemplate>
                        <asp:HiddenField ID="hfTimer" ClientIDMode="Static" runat="server"/>
                        <asp:Label ID="lblTimer" ClientIDMode="Static" runat="server" CssClass="center"/>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>
    </form>
    <script type="text/javascript">
        var timer;
        function shutter_click() {
            document.getElementById('fuCamera').click();
        }
        function file_chosen(input) {
            if (input.files && input.files[0]) {
                document.getElementById('pnlShutter').style.display = 'none';
                document.getElementById('hfShutterState').value = 'hidden';
                document.getElementById('lblMessage').innerHTML = "Please wait...";
                document.getElementById('hfMessage').value = "Please wait...";
                document.getElementById('lblMessage').style.display = '';
                var files = input.files
                var reader = new FileReader();
                reader.onload = function (e) {
                    document.getElementById('imgPreview').src = e.target.result;
                    document.getElementById('image_data').value = e.target.result;
                    document.getElementById('imgPreview').style.display = 'block';
                    document.getElementById('hfMessage').value = '';
                    document.getElementById('btnSubmit').click();
                };
                reader.readAsDataURL(input.files[0]);
            }
        }
        function expire() {
            clearInterval(timer);
            document.getElementById('lblMessage').innerHTML = "Expired";
            document.getElementById('lblTimer').innerHTML = "This link has expired.<br/>Re-open the popup to generate a fresh QR Code.";
            document.getElementById('pnlShutter').style.display = 'none';
            document.getElementById('hfShutterState').value = "hidden";
        }
        function initializeTimer() {
            var totalSec = Number(document.getElementById('hfTimer').value);
            if (totalSec < 0) expire();
            else {
                timer = setInterval(function () {
                    var hours = parseInt(totalSec / 3600);
                    var minutes = parseInt(totalSec / 60) % 60;
                    var seconds = totalSec % 60;

                    document.getElementById('lblTimer').innerHTML = "Remaining time: " + ('00' + hours).slice(-2) + ":" + ('00' + minutes).slice(-2) + ":" + ('00' + seconds).slice(-2);

                    if (--totalSec < 0) {
                        expire();
                    }
                }, 1000);
            }
        }
        //on load
        initializeTimer();
    </script>
</body>
</html>
