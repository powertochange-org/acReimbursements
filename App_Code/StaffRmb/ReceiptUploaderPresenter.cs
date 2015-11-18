using DotNetNuke.Web.Mvp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.FileSystem;
using System.IO;
using System.Text.RegularExpressions;
using StaffRmb;

using PowerToChange.Modules.StaffRmb.Views;
using PowerToChange.Modules.StaffRmb.Helpers;

namespace PowerToChange.Modules.StaffRmb.Presenters
{
    public class ReceiptUploaderPresenter
    {
        public const int EXPIRE_MINUTES = 10;
        bool _testing = false;
        IReceiptUploader _view;
        IEnumerable<AP_Staff_Rmb> _rmbs;
        IEnumerable<AP_Staff_RmbLine> _lines;
        IEnumerable<AP_Staff_RmbLine_File> _images;
        IEnumerable<AP_Staff_Rmb_Log> _log;

        //Constructor and Properties for testing only
        public ReceiptUploaderPresenter(IReceiptUploader view, bool testing)
        {
            _view = view;
            _testing = testing;
        }
        public IEnumerable<AP_Staff_Rmb> Rmbs { set { _rmbs = value; } }
        public IEnumerable<AP_Staff_RmbLine> Lines { set { _lines = value; } }
        public IEnumerable<AP_Staff_RmbLine_File> Images { set { _images = value; } }
        public IEnumerable<AP_Staff_Rmb_Log> Log { set { _log = value; } }

        public ReceiptUploaderPresenter(IReceiptUploader view)
        {
            _view = view;
            _view.InitializeEvent += InitializeEvent;
            _view.UploadEvent += UploadEvent;
            StaffRmbDataContext d = new StaffRmbDataContext();
            _rmbs = d.AP_Staff_Rmbs;
            _lines = d.AP_Staff_RmbLines;
            _images = d.AP_Staff_RmbLine_Files;
            _log = d.AP_Staff_Rmb_Logs;
        }

        public void InitializeEvent(object sender, MobileEventArgs args)
        {
            AP_Staff_Rmb rmb = null;
            try { 
                rmb = _rmbs.Where(a => a.SpareField4 == args.token).Single();
                _view.RID = rmb.RID.ToString("D5");
            }
            catch
            {
                _view.RID = "unknown";
                _view.Message = "The correct Reimbursement could not be found.";
                return;
            }
        }

        public void UploadEvent(object sender, MobileEventArgs args)
        {
            DateTime tokenTime = getTimeFromToken(args.token);
            if (tokenTime.AddMinutes(EXPIRE_MINUTES) <= DateTime.Now)
            {
                _view.Expire();
                return;
            }
            int rmbNo = getRmbNoFromToken(args.token);
            int lineNo = getLineNoFromToken(args.token);

            AP_Staff_Rmb rmb = null;
            AP_Staff_RmbLine line = null;
            try {
                rmb = _rmbs.Where(a => a.SpareField4==args.token).Single();
                if (rmb.RMBNo != rmbNo) throw new Exception();
                line = _lines.Where(a => a.RmbLineNo == lineNo && a.RmbNo == rmbNo).Single(); 
            } 
            catch { }
            try // Upload
            {
                // initialize folder/permissions
                int recnum;
                try { recnum = _images.Where(a => a.RmbLineNo==lineNo && a.RMBNo == rmbNo).Select(a => a.RecNum).Max() + 1; }
                catch { recnum = 1; }
                IFileInfo file;
                string strUrl = "";
                if (_testing)
                {
                    file = new DotNetNuke.Services.FileSystem.FileInfo() { FileId = -1 };
                }
                else
                {
                    PortalSettings PS = (PortalSettings)HttpContext.Current.Items["PortalSettings"];
                    Filesystem.ensureFolderExists(PS.PortalId);
                    IFolderInfo imageFolder = Filesystem.getImageFolder(rmb.UserId, PS.PortalId);
                    Filesystem.checkFolderPermissions(PS.PortalId, imageFolder, rmb.UserId, null);  //no approvers list sent because it is an async function
                    string filename = "R" + rmbNo.ToString()+"L"+(lineNo<0?"New":lineNo.ToString()) + "Rec" + recnum.ToString() + ".png";
                    // save file to DNN database
                    string base64Data = Regex.Match(_view.ImageData, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
                    byte[] image_data = Convert.FromBase64String(base64Data);
                    //byte[] image_data = _view.ImageFile;
                    //_view.ImageUrl= "data:image/png;base64," + Convert.ToBase64String(image_data);
                    if (image_data == null || image_data.Length == 0) return;
                    MemoryStream image_stream = resizeImage(image_data);
                    file = FileManager.Instance.AddFile(imageFolder, filename, image_stream, false); //true is for overwrite
                    string URL = FileManager.Instance.GetUrl(file);
                    string strPathAndQuery = HttpContext.Current.Request.Url.PathAndQuery;
                    strUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, URL);
                }
                // link file to image 
                AP_Staff_RmbLine_File image = new AP_Staff_RmbLine_File() { RMBNo = rmbNo, RecNum = recnum, FileId = file.FileId };
                if (lineNo >= 0) image.RmbLineNo = lineNo; //A null RmbLineNo indicates that the line hasn't been saved yet
                image.URL = strUrl;
                StaffRmbDataContext d = new StaffRmbDataContext();
                d.AP_Staff_RmbLine_Files.InsertOnSubmit(image);
                d.AP_Staff_Rmb_Logs.InsertOnSubmit(new AP_Staff_Rmb_Log() { Timestamp = DateTime.Now, LogType = 2, RID = rmb.RID, Message = "Receipt image uploaded via mobile page" });
                d.SubmitChanges();
                _view.Message = "Image uploaded.";
            }
            catch (Exception ex)
            {
                StaffRmbDataContext d = new StaffRmbDataContext();
                d.AP_Staff_Rmb_Logs.InsertOnSubmit(new AP_Staff_Rmb_Log() { Timestamp = DateTime.Now, LogType = 4, RID = rmb.RID, Message = "Error saving receipt image via mobile page" });
                d.SubmitChanges();
                _view.Message = "Image Upload Failed";
            }
        }

        static public DateTime getTimeFromToken(string token)
        {
            try
            {
                if (token == null) throw new Exception();
                byte[] bytes = Convert.FromBase64String(token);
                long ticks = BitConverter.ToInt64(bytes, 0);
                return new DateTime(ticks, DateTimeKind.Utc);
            }
            catch
            {
                return DateTime.Now.AddMinutes(-(EXPIRE_MINUTES + 1)); //return an  expired time
            }
        }
        static public int getRmbNoFromToken(string token)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(token);
                int id = BitConverter.ToInt32(bytes, 8);
                return id;
            }
            catch
            {
                return -1;
            }
        }
        static public int getLineNoFromToken(string token)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(token);
                int id = BitConverter.ToInt32(bytes, 12);
                return id;
            }
            catch
            {
                return -1;
            }
        }
        static public string encodeToken(DateTime timestamp, int RmbNo, int LineNo)
        {
            byte[] bytes = new byte[16];
            long ticks = timestamp.Ticks;
            Buffer.BlockCopy(BitConverter.GetBytes(ticks), 0, bytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(RmbNo), 0, bytes, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(LineNo), 0, bytes, 12, 4);
            string result = Convert.ToBase64String(bytes);
            return result;
        }
        public static MemoryStream resizeImage(byte[] image_data)
        {
            // Resize given byte array image to a width of 1000px, and return result as a memory stream
            MemoryStream ms = new MemoryStream(image_data);
            System.Drawing.Image fullsizeImage = System.Drawing.Image.FromStream(ms);
            float ratio = (float)fullsizeImage.Height / (float)fullsizeImage.Width;
            int width = 1000;
            int height = (int)Math.Round(width * ratio);
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(fullsizeImage, width, height);
            MemoryStream result = new MemoryStream();
            bitmap.Save(result, System.Drawing.Imaging.ImageFormat.Png);
            return result;
        }
    }
}