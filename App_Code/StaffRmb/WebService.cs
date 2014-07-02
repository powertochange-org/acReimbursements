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

    //[WebMethod]
    //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    //public void AllRmbs2(int portalid, int tabmoduleid, int status)
    //{
    //    string result = "";
    //    if (isFinance(tabmoduleid))
    //    {
    //        result += "[{label:'All Staff'";
    //        if (status == RmbStatus.Submitted || status == RmbStatus.Processing || status == RmbStatus.Paid)
    //        {
    //            StaffRmbDataContext d = new StaffRmbDataContext();
    //            IQueryable<AP_Staff_Rmb> rmbs = from c in d.AP_Staff_Rmbs
    //                                            where c.Status == status && c.PortalId == portalid
    //                                            orderby c.RID descending
    //                                            select c;
    //            string letters = "";
    //            for (char letter = 'A'; letter <= 'Z'; letter++)
    //            {
    //                IQueryable<StaffBroker.User> staffmembers = StaffBrokerFunctions.GetStaff().Where(w => w.LastName.ToUpper()[0] == letter);
    //                if (staffmembers.Count() > 0)
    //                {
    //                    string people = "";
    //                    foreach (StaffBroker.User staffmember in staffmembers)
    //                    {
    //                        string nodes = "";
    //                        string name = staffmember.DisplayName;
    //                        IQueryable<AP_Staff_Rmb> staffrmbs = rmbs.Where(w => w.UserId == staffmember.UserID);
    //                        if (staffrmbs.Count() > 0)
    //                        {
    //                            foreach (AP_Staff_Rmb rmb in staffrmbs)
    //                            {
    //                                if (nodes.Length > 0) nodes += ",";
    //                                nodes += "{label:'" + rmb.RID + " : " + (rmb.RmbDate == null ? "" : rmb.RmbDate.Value.ToShortDateString()) + " : " + rmb.SpareField1 + "'}";
    //                            }
    //                            if (nodes.Length > 0)
    //                            {
    //                                people += "{label:'" + staffmember.DisplayName + "',children:[" + nodes + "]}";
    //                            }
    //                        }
    //                    }
    //                    if (people.Length > 0)
    //                    {
    //                        letters += "{label:'" + letter + "',children:[" + people + "]}";
    //                    }
    //                }
    //            }
    //            if (letters.Length > 0)
    //            {
    //                result += ",children:[" + letters + "]";
    //            }
    //            result += "}]";
    //        }
    //    }
    //    HttpContext.Current.Response.ContentType = "text/json";
    //    HttpContext.Current.Response.Write(result);
    //}

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
                                             orderby c.RID descending
                                             select c)
                {
                    DotNetNuke.Entities.Users.UserInfo staffMember = DotNetNuke.Entities.Users.UserController.GetUserById(portalid, rmb.UserId);
                    string firstLetter = staffMember.LastName.ToUpper().Substring(0, 1);
                    Item letter = tree.Needs(firstLetter);
                    Item staff = letter.Needs(staffMember.DisplayName);
                    string id = rmb.RID.ToString().PadLeft(5, '0'); 
                    Item reimbursement = staff.Needs(id + " : " + (rmb.RmbDate == null ? "" : rmb.RmbDate.Value.ToShortDateString()) + " : " + rmb.SpareField1);
                    reimbursement.setRmbNo(rmb.RMBNo.ToString());
                    reimbursement.setStyle("font-size: 6.5pt; color: #999999;");
                }
            }
            var result = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new Item[] { tree });
            HttpContext.Current.Response.ContentType = "application/json";
            HttpContext.Current.Response.Write(result);
        }

    }

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

        public Item Needs(string match)
        {
            foreach (Item item in children)
            {
                if (item.label.Equals(match)) return item;
            }
            Item newItem = new Item(match);
            children.Add(newItem);
            return newItem;
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
