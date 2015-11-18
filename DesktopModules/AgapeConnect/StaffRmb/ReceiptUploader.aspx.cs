using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebFormsMvp;
using DotNetNuke.Web.Mvp;
using DotNetNuke;
using StaffRmb;

using PowerToChange.Modules.StaffRmb.Presenters;

namespace PowerToChange.Modules.StaffRmb.Views
{
    [PresenterBinding(typeof(ReceiptUploaderPresenter))]
    public partial class ReceiptUploader : Page, IReceiptUploader
    {
        private string EXPIRED = "This link has expired.<br/>Save and re-open the line-item on your computer to generate a fresh QR Code.";

        private ReceiptUploaderPresenter _presenter;
        private IEnumerable<AP_Staff_Rmb> _rmbs;
        private IEnumerable<AP_Staff_RmbLine> _lines;

        //Testing
        public IEnumerable<AP_Staff_RmbLine> Lines { set { _lines = value; } }

        public string RID { set { lblTitle.Text = lblTitle.Text.Replace("[RID]", value); } }
        public string LineNo { set { } }
        public string Message
        {
            set
            {
                lblMessage.Text = value;
                hfMessage.Value = value;
                if (string.IsNullOrEmpty(value))
                {
                    lblMessage.Style.Add("display", "none");
                    divOverlay.Style.Add("display", "none");
                    pnlShutter.Visible = true;
                }
                else
                {
                    lblMessage.Style.Remove("display");
                    divOverlay.Style.Remove("display");
                    pnlShutter.Visible = false;
                }
            }
        }
        public byte[] ImageFile { get { if (fuCamera.HasFile) return fuCamera.FileBytes; else return null; } }
        public string ImageData { get { return image_data.Value; } }

        public void Expire()
        {
            Message = "Expired";
            lblTimer.Text = EXPIRED;
            tmTimer.Enabled = false;
            pnlShutter.Visible = false;
            hfShutterState.Value = "hidden";
        }

        public event System.EventHandler<MobileEventArgs> InitializeEvent;
        public event System.EventHandler<MobileEventArgs> UploadEvent;

        //Constructor
        public ReceiptUploader()
        {
            _presenter = new ReceiptUploaderPresenter(this);
            StaffRmbDataContext d = new StaffRmbDataContext();
            _rmbs = d.AP_Staff_Rmbs;
            _lines = d.AP_Staff_RmbLines;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!this.Page.IsPostBack)
            {
                MobileEventArgs args = new MobileEventArgs() { token = this.Page.Request.QueryString["id"].Replace(" ", "+") };
                try { if (InitializeEvent != null) InitializeEvent(this, args); }
                catch { Message = "Error displaying mobile page"; }
            }
            else
            {
                Message = hfMessage.Value;
                if (!string.IsNullOrEmpty(hfMessage.Value)) divOverlay.Style.Remove("display");
                pnlShutter.Visible = (hfShutterState.Value != "hidden");
            }
        }

        protected void Upload(object sender, EventArgs e)
        {
            fuCamera.Visible = false; //prevent autolaunching
            pnlShutter.Visible = false; //prevent re-clicking
            imgPreview.ImageUrl = image_data.Value;
            MobileEventArgs args = new MobileEventArgs() { token = this.Page.Request.QueryString["id"] };
            try { if (UploadEvent != null) UploadEvent(this, args); }
            catch { Message = "Upload Failed with token:" + this.Page.Request.QueryString["id"]; }
        }

        protected void TimerTick(object sender, EventArgs e)
        {
            DateTime expireTime = ReceiptUploaderPresenter.getTimeFromToken(this.Page.Request.QueryString["id"]).AddMinutes(ReceiptUploaderPresenter.EXPIRE_MINUTES);
            TimeSpan remainingTime = (expireTime - DateTime.Now);
            if (remainingTime > TimeSpan.Zero)
            {
                lblTimer.Text = "Remaining time: " + remainingTime.Hours.ToString("D2") + ":" + remainingTime.Minutes.ToString("D2") + ":" + remainingTime.Seconds.ToString("D2");
            }
            else Expire();
        }
    }


}