using StaffRmb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PowerToChange.Modules.StaffRmb.Views
{

    public interface IReceiptUploader
    {
        IEnumerable<AP_Staff_RmbLine> Lines { set; }
        string RID { set; }
        string LineNo { set; }
        string Message { set; }
        byte[] ImageFile { get; }
        string ImageData { get; }

        void Expire();

        event System.EventHandler<MobileEventArgs> InitializeEvent;
        event System.EventHandler<MobileEventArgs> UploadEvent;
    }

    public class MobileEventArgs : System.EventArgs
    {
        public string token { set; get; }
    }
}