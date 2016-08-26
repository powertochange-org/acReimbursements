using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using Newtonsoft.Json;
using StaffRmb;
using System.Threading.Tasks;
using DotNetNuke.Web.Api;

/// <summary>
/// Provides a sublist of account numbers
/// </summary>

[WebService(Namespace = "powertochange.org")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[System.Web.Script.Services.ScriptService]
public class WebService : System.Web.Services.WebService {

    public WebService () {

        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }

    [WebMethod]
    public void GetAccountNumbers(string term) {
        var result = new StaffRmbDataContext().AP_StaffBroker_CostCenters
            .Select(s => new { label = s.CostCentreCode + ":" + s.CostCentreName, value = s.CostCentreCode })
            .Where(w => w.label.Contains(term))
            .OrderBy(o => o.value);
        string json = JsonConvert.SerializeObject(result);
        HttpContext.Current.Response.ContentType = "application/json";
        HttpContext.Current.Response.Write(json);
    }

    [WebMethod]
    public void GetStaffNames(int portalid, string term)
    {
        term = term.ToLower();
        var result = new DotNetNuke.Security.Roles.RoleController().GetUsersByRole(portalid, "Staff")
            .Where(w => w.DisplayName.ToLower().Contains(term) && w.IsDeleted==false)
            .Select(s => new { label = s.DisplayName, value = s.UserID })
            .OrderBy(o => o.label);            
        string json = JsonConvert.SerializeObject(result);
        HttpContext.Current.Response.ContentType = "application/json";
        HttpContext.Current.Response.Write(json);
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public string[] GetVendorIds(string company)
    {
        List<string> result = new List<string>();
        byte[] postData = System.Text.Encoding.UTF8.GetBytes("company="+company);
        //send request
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://gpapp/gpimport/webservice/GetVendors");
        request.KeepAlive = false; //This and the following line are required to prevent "connection closed" problems
        request.ProtocolVersion = HttpVersion.Version10;
        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = postData.Length;
        try
        {
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(postData, 0, postData.Length);
            }
            var response = (HttpWebResponse)request.GetResponse();
            if (response != null)
            {
                var reader = new System.IO.StreamReader(response.GetResponseStream());
                dynamic vendors = JsonConvert.DeserializeObject(reader.ReadToEnd());
                foreach (var vendor in vendors)
                {
                    result.Add("[" + vendor.VendorID + "] " + vendor.VendorName);
                }
            }
        }
        catch (WebException e)
        {
        }
        return result.ToArray();
    }

    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json, UseHttpGet=true)]
    public void AllRmbs(int portalid, int tabmoduleid, int status)
    {
        if (isFinance(tabmoduleid))
        {
            Item tree = new Item("All Staff");
            if (status == RmbStatus.Submitted || status == RmbStatus.Processing || status == RmbStatus.Paid)
            {
                StaffRmbDataContext d = new StaffRmbDataContext();
                foreach (AP_Staff_Rmb rmb in from c in d.AP_Staff_Rmbs
                                             where c.Status == status && c.PortalId == portalid
                                             select c)
                {
                    DotNetNuke.Entities.Users.UserInfo staffMember = DotNetNuke.Entities.Users.UserController.GetUserById(portalid, rmb.UserId);
                    string firstLetter = (staffMember.LastName!=null?staffMember.LastName.ToUpper().Substring(0, 1):"-");
                    Item letter = tree.Needs(firstLetter);
                    Item staff = letter.Needs(staffMember.LastName!=null?(staffMember.LastName + ", " + staffMember.FirstName):staffMember.DisplayName);
                    string id = rmb.RID.ToString().PadLeft(5, '0');
                    Item reimbursement = staff.Needs(id + " : " + (rmb.RmbDate == null ? "" : rmb.RmbDate.Value.ToShortDateString()) + " : " + rmb.SpareField1, false); //false means reverse sort
                    if (rmb.PrivComment != null) { reimbursement.label += " *"; }
                    reimbursement.setRmbNo(rmb.RMBNo.ToString());
                }
            }
            var result = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new Item[] { tree });
            HttpContext.Current.Response.ContentType = "application/json";
            HttpContext.Current.Response.Write(result);
        }

    }

    //[WebMethod]
    //[ScriptMethod(ResponseFormat = ResponseFormat.Json, UseHttpGet=true)]
    //public void getStaffAppsButton()
    //{
    //    String AUTHORIZATION_MGR = "<a href='https://staffapps.powertochange.org/authmanager'> <img src='https://staff.powertochange.org/wp-content/images/AuthorizationMgr-Icon.png' alt='Authorization Manager'></a>";
    //    String REPORTS = "<a href='https://staff.powertochange.org/reports/'> <img src='https://staff.powertochange.org/wp-content/images/Reports-Icon.png' alt='Reports'></a>";
    //    String STAFF_DIRECTORY = "<a href='https://staff.powertochange.org/staff-directory/'> <img src='https://staff.powertochange.org/wp-content/images/Staff-Directory-Icon.png' alt='Staff Directory'></a>";
    //    String REIMBURSEMENTS = "<a href='https://apps.powertochange.org/Reimbursement-form'><img src='https://staff.powertochange.org/wp-content/images/Reimbursements-Icon.png' alt='Reimbursements' /></a>";
    //    String HELPDESK = "<a href='mailto:helpdesk@powertochange.org'> <img src='https://staff.powertochange.org/wp-content/images/HelpDesk-Icon.png' alt='Help Desk'></a>";
    //    String LINK_SHORTENER = "<a href='https://staff.powertochange.org/sh'><img src='https://staff.powertochange.org/wp-content/images/Link-Shortener-Icon.png' alt='Link Shortener'></a>";
    //    String WIKI = "<a href='https://wiki.powertochange.org/help'><img src='https://staff.powertochange.org/wp-content/images/Self-Help-Wiki-Icon.png' alt='Self-Help Wiki'></a>";
    //    String EGENCIA = "<a href='https://staff.powertochange.org/egencia-login/'><img src='https://staff.powertochange.org/wp-content/images/Egencia-Icon.png' alt='Egencia'></a>";
    //    String DAYFORCE = "<a href='https://sso.dayforcehcm.com/p2c'><img src='https://staff.powertochange.org/wp-content/images/Dayforce-Icon.png' alt='Dayforce'></a>";
    //    String LEARNING = "<a href='https://learning.powertochange.org'><img src='https://staff.powertochange.org/wp-content/images/Learning-Icon.png' alt='Learning'></a>";
    //    String SETTINGS = "<a href='https://staff.powertochange.org/my-settings'><img src='https://staff.powertochange.org/wp-content/images/My-Settings-Icon.png' alt='Settings' /></a>";

    //    String STYLE = "<style type='text/css' scoped>#staffAppsButton:hover, #staffAppsButton:active, #staffAppsButton:focus {color: #000000!important;text-decoration: none;}#staffAppsMenu table td{background:transparent;}</style>";
    //    String SCRIPT = "<script type='text/javascript'>function staffAppsMenuShow() {$('#staffAppsMenu').show();var e = document.getElementById('staffAppsButton');e.style.background = '#f4f4f4';e.style.border = '1px solid #d6d7d4';e.style.borderBottom = '1px solid #f4f4f4';}" +
    //           "function staffAppsMenuHide() {$('#staffAppsMenu').hide(); var e = document.getElementById('staffAppsButton');e.style.background = '#f58220';e.style.border = '1px solid #eb8528';}</script>";

    //    String code = "<div id='staff-app-container' style='display:inline-block; position:relative; float:right; min-width:302px;'>" +
    //           "<a id='staffAppsButton' class='button related' onmouseout='staffAppsMenuHide();' onmouseover='staffAppsMenuShow();' style='background:#f58220; border:1px solid #eb8528; cursor:default;  z-index:910; position:relative; float:right; width:103px; height:21px; color:#000000; " +
    //           "text-align:center; font-family:sans-serif; font-weight:300; font-size:13px;  padding-top:5px; padding-left:0; padding-bottom:5px; padding-right:0px; margin:10px 10px; border-radius:5px; border:1px solid rgb(235, 133, 40); background:rgb(245, 130, 32);'>Staff Apps</a>" +
    //           "<div id='staffAppsMenu' onmouseout='staffAppsMenuHide();' onmouseover='staffAppsMenuShow();' style='position:absolute; display:none; border:1px solid rgb(214, 215, 212); padding: 10px 40px; right:10px; top:37px; z-index:900; background-color: rgb(244, 244, 244);' >" +
    //           "<center><ul class='staffAppsPopupMenu' style='margin:15px 0; padding:15px 0; color:#adafb2; font-size:15px;'><table><tbody>" +
    //           "<tr><td style='border:0;'>" + AUTHORIZATION_MGR + "</td>" +
    //           "<td style='border:0;'>" + REPORTS + "</td>" +
    //           "<td style='border:0;'>" + STAFF_DIRECTORY + "</td>" +
    //           "<td style='border:0;'>" + REIMBURSEMENTS + "</td>" +
    //           "</tr><tr>" +
    //           "<td style='border:0;'>" + HELPDESK + "</td>" +
    //           "<td style='border:0;'>" + DAYFORCE + "</td>" +
    //           "<td style='border:0;'>" + EGENCIA + "</td>" +
    //           "<td style='border:0;'>" + LINK_SHORTENER + "</td>" +
    //           "</tr><tr>" +
    //           "<td style='border:0;'>" + WIKI + "</td>" +
    //           "<td style='border:0;'>" + LEARNING + "</td>" +
    //           "<td style='border:0;'>" + SETTINGS + "</td>" +
    //           "</tr></tbody></table></ul></center></div>" +
    //            STYLE + SCRIPT + "</div>";
    //    HttpContext.Current.Response.ContentType = "application/json";
    //    HttpContext.Current.Response.Headers.Add("Access-Control-Allow-Origin", "*");
    //    HttpContext.Current.Response.Write(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(code));
    //}

    //[WebMethod]
    //[ScriptMethod(ResponseFormat = ResponseFormat.Json, UseHttpGet=true)]
    //public void getStaffAppsButtonResponsive()
    //{
    //    //String ABSENCE_TRACKER = "<a href='https://absences.powertochange.org' onclick='trackStaffAppsMenuClick(\"AbsenceTracker\")'> <img src='https://staff.powertochange.org/wp-content/images/Absence-Tracker-Icon.png' alt='Absence Tracker'></a>";
    //    String AUTHORIZATION_MGR = "<a href='https://staffapps.powertochange.org/authmanager' onclick='trackStaffAppsMenuClick(\"Authorization Manager\")'> <img src='https://staff.powertochange.org/wp-content/images/AuthorizationMgr-Icon.png' alt='Authorization Manager'></a>";
    //    String REPORTS = "<a href='https://staff.powertochange.org/reports/' onclick='trackStaffAppsMenuClick(\"Reports\")'> <img src='https://staff.powertochange.org/wp-content/images/Reports-Icon.png' alt='Reports'></a>";
    //    String STAFF_DIRECTORY = "<a href='https://staff.powertochange.org/staff-directory/' onclick='trackStaffAppsMenuClick(\"Staff Directory\")'> <img src='https://staff.powertochange.org/wp-content/images/Staff-Directory-Icon.png' alt='Staff Directory'></a>";
    //    String REIMBURSEMENTS = "<a href='https://apps.powertochange.org/Reimbursement-form' onclick='trackStaffAppsMenuClick(\"Reimbursements\")'><img src='https://staff.powertochange.org/wp-content/images/Reimbursements-Icon.png' alt='Reimbursements' /></a>";
    //    String HELPDESK = "<a href='mailto:helpdesk@powertochange.org' onclick='trackStaffAppsMenuClick(\"Helpdesk\")'> <img src='https://staff.powertochange.org/wp-content/images/HelpDesk-Icon.png' alt='Help Desk'></a>";
    //    String LINK_SHORTENER = "<a href='https://staff.powertochange.org/sh' onclick='trackStaffAppsMenuClick(\"Link Shortener\")'><img src='https://staff.powertochange.org/wp-content/images/Link-Shortener-Icon.png' alt='Link Shortener'></a>";
    //    String WIKI = "<a href='https://staff.powertochange.org/kb/' onclick='trackStaffAppsMenuClick(\"Knowledge Base\")'><img src='https://staff.powertochange.org/wp-content/images/Self-Help-Wiki-Icon.png' alt='Self-Help Wiki'></a>";
    //    String EGENCIA = "<a href='https://staff.powertochange.org/egencia-login/' onclick='trackStaffAppsMenuClick(\"Egencia\")'><img src='https://staff.powertochange.org/wp-content/images/Egencia-Icon.png' alt='Egencia'></a>";
    //    String DAYFORCE = "<a href='https://sso.dayforcehcm.com/p2c' onclick='trackStaffAppsMenuClick(\"Dayforce\")'><img src='https://staff.powertochange.org/wp-content/images/Dayforce-Icon.png' alt='Dayforce'></a>";
    //    String DAYFORCE_CALENDAR = "<a href='https://staffapps.powertochange.org/DayforceCalendar/Relationship/ManageTeam' onclick='trackStaffAppsMenuClick(\"Dayforce Calendar\")'> <img src='https://staff.powertochange.org/wp-content/images/Dayforce-Calendar-Icon.png' alt='Dayforce Calendar'></a>";
    //    String LEARNING = "<a href='https://learning.powertochange.org' onclick='trackStaffAppsMenuClick(\"Learning\")'><img src='https://staff.powertochange.org/wp-content/images/Learning-Icon.png' alt='Learning'></a>";
    //    String SETTINGS = "<a href='https://staff.powertochange.org/my-settings' onclick='trackStaffAppsMenuClick(\"Settings\")'><img src='https://staff.powertochange.org/wp-content/images/My-Settings-Icon.png' alt='Settings' /></a>";
    //    String ONLINE_AP = "<a href='https://apps.powertochange.org/AP' onclick='trackStaffAppsMenuClick(\"Online AP\")'><img src='https://staff.powertochange.org/wp-content/images/AP-Icon.png' alt='Online AP' /></a>";
    //    String MPDX = "<a href='https://mpdx.org'' onclick='trackStaffAppsMenuClick(\"mpdx\")'><img src='https://staff.powertochange.org/wp-content/images/mpdx-Icon.png' alt='mpdx' /></a>";
    //    String PLACEHOLDER = "<div style='width:63px'></div>";

    //    String STYLE = "<style type='text/css' scoped>#staffAppsButton:hover, #staffAppsButton:active, #staffAppsButton:focus {color: #000000!important;text-decoration: none;}#staffAppsMenu table td{background:transparent;}</style>";
    //    String SCRIPT = "<script type='text/javascript'>function staffAppsMenuShow() {$('#staffAppsMenu').show();var e = document.getElementById('staffAppsButton');e.style.background = '#f4f4f4';e.style.border = '1px solid #d6d7d4';e.style.borderBottom = '1px solid #f4f4f4';}" +
    //           "function staffAppsMenuHide() {$('#staffAppsMenu').hide(); var e = document.getElementById('staffAppsButton');e.style.background = '#f58220';e.style.border = '1px solid #eb8528';}" +
    //           "function trackStaffAppsMenuClick(Label) {'use strict'; console.debug('tracking label: '+Label); if (typeof (_gaq) !== 'undefined') { console.debug('_gaq'); _gaq.push(['_trackEvent', 'Staff Apps Menu', 'click', Label]); } else if (typeof (ga) !== 'undefined') { console.debug('ga'); ga('send', 'event', 'Staff Apps Menu', 'click', Label);}}</script>";

    //    String code = "<link href='https://apps.powertochange.org/portals/_default/skins/carmel/staff-apps-button.css' type='text/css' rel='stylesheet'><div id='staff-app-container'>" +
    //           "<a id='staffAppsButton' class='button related' onmouseout='staffAppsMenuHide();' onmouseover='staffAppsMenuShow();' style='background:#f58220; border:1px solid #eb8528; cursor:default; position:relative; float:right; width:103px; height:21px; color:#000000; " +
    //           "text-align:center; font-family:sans-serif; font-weight:300; font-size:13px;  padding-top:6px; padding-left:0; padding-bottom:4px; padding-right:0px; margin:10px 10px; border-radius:5px; border:1px solid rgb(235, 133, 40); background:rgb(245, 130, 32);'>Staff Apps</a>" +
    //           "<div id='staffAppsMenu' onmouseout='staffAppsMenuHide();' onmouseover='staffAppsMenuShow();' class='staff-apps-button' >" +
    //           "<center><ul class='staffAppsPopupMenu' id='staff-apps-popup-menu'><div><div>" +
    //           "<div><div class='staff-app-button-styling'>" + AUTHORIZATION_MGR + "</div>" +
    //           "<div class='staff-app-button-styling'>" + REPORTS + "</div>" +
    //           "<div class='staff-app-button-styling'>" + STAFF_DIRECTORY + "</div>" +
    //           "<div class='staff-app-button-styling'>" + REIMBURSEMENTS + "</div>" +
    //           "</div><div>" +
    //           "<div class='staff-app-button-styling'>" + HELPDESK + "</div>" +
    //           "<div class='staff-app-button-styling'>" + DAYFORCE + "</div>" +
    //           "<div class='staff-app-button-styling'>" + DAYFORCE_CALENDAR + "</div>" +
    //           "<div class='staff-app-button-styling'>" + EGENCIA + "</div>" +
    //           "</div><div>" +
    //           "<div class='staff-app-button-styling'>" + LINK_SHORTENER + "</div>" +
    //           "<div class='staff-app-button-styling'>" + WIKI + "</div>" +
    //           "<div class='staff-app-button-styling'>" + LEARNING + "</div>" +
    //           "<div class='staff-app-button-styling'>" + MPDX + "</div>" +
    //           "</div><div>" +
    //           "<div class='staff-app-button-styling'>" + PLACEHOLDER + "</div>" +
    //           "<div class='staff-app-button-styling'>" + PLACEHOLDER + "</div>" +
    //           "<div class='staff-app-button-styling'>" + PLACEHOLDER + "</div>" +
    //           "<div class='staff-app-button-styling'>" + SETTINGS + "</div>" +
    //           "</div></div></div></ul></center></div>" +
    //            STYLE + SCRIPT + "</div>";
    //    //"<div class='staff-app-button-styling'>" + PLACEHOLDER + "</div>" +
    //    HttpContext.Current.Response.ClearHeaders();
    //    HttpContext.Current.Response.ContentType = "application/json";
    //    HttpContext.Current.Response.Headers.Add("Access-Control-Allow-Origin", "*");
    //    HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.Public);
    //    HttpContext.Current.Response.Cache.SetExpires(DateTime.Now.AddDays(1));
    //    HttpContext.Current.Response.Cache.SetMaxAge(TimeSpan.FromDays(1));
    //    HttpContext.Current.Response.Cache.SetSlidingExpiration(true);
    //    HttpContext.Current.Response.Cache.SetValidUntilExpires(true);
    //    HttpContext.Current.Response.Write(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(code));
    //}
	
    public class Item
    {
        public Item() 
        {
            this.label = "";
            children = new List<Item>();
        }
        public Item(string label)
        {
            this.label = label;
            children = new List<Item>();
        }

        public string label {get;  set;}
        public string rmbno { get; set; }
        public string style { get; set; }
        public List<Item> children {get; set;}

        public void setRmbNo(string rmbno) {
            this.rmbno = rmbno;
        }

        public void setStyle(string style)
        {
            this.style = style;
        }

        public bool Contains(string match) {
            foreach (Item item in children) {
                if (item.label.Equals(match)) return true;
            }
            return false;
        }

        public Item Needs(string match, bool forward=true)
        // Returns item if it exists, otherwise creates it (in alphabetical order) and returns it
        {
            int position=-1;
            Item item;
            for (int index=0; index<children.Count; index++)
            {
                item = children.ElementAt(index);
                if (item.label.Equals(match)) return item;
                if (forward) {
                    if (String.Compare(item.label, match, true) < 0) position = index;
                } else {
                    if (String.Compare(item.label, match, true) > 0) position = index;
                }
            }
            item = new Item(match);
            children.Insert(position+1, item); //insert after the last item that preceeds it
            return item;
        }
    }

    private Boolean isFinance(int tabmoduleid)
    // Is the currently logged in user a member of the finance team (AccountsRoles in settings)?
    {
        Boolean result = false;
        System.Collections.Hashtable  settings = new DotNetNuke.Entities.Modules.ModuleController().GetTabModuleSettings(tabmoduleid);
        if (!settings.Contains("AccountsRoles")) return false;
        string[] accountRoles = settings["AccountsRoles"].ToString().Split(';');
        string username = Context.User.Identity.Name;
        DotNetNuke.Entities.Users.UserInfo user = DotNetNuke.Entities.Users.UserController.GetUserByName(username);
        if (user == null || user.Roles == null) return false;
        foreach (string role in accountRoles) {
            if (user.Roles.Contains(role)) result = true;
        }
        return result;
    }
    
}
