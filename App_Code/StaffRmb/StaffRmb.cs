﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;
using System.Web.Services;
using System.Web.Services.Protocols;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Log.EventLog;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Web.Caching;
using DotNetNuke.Entities.Modules;
using System.Configuration;

/// <summary>
/// Summary description for StaffRmb
/// </summary>
/// 
namespace StaffRmb
{

    //public static class RmbReceiptMode
    //{
    //    public const int Disabled = 0;
    //    public const int NoVAT = 1;
    //    public const int VAT = 2;
    //    static public string ModeName(int ModeNo)
    //    {
    //        switch (ModeNo)
    //        {
    //            case 0: return "Disabled";
    //            case 1: return "NoVAT";
    //            case 2: return "VAT";
    //            default: return "Unknown";
    //        }

    //    }
    //}

    public static class RmbReceiptType
    {
        public const int UNSELECTED = -1;
        public const int No_Receipt = 0;
        public const int Standard = 1;
        public const int Electronic = 2;
        public const int VAT = 3;

        static public string Name(int type)
        {
            switch (type)
            {
                case -1: return "UNSELECTED";
                case 0: return "No_Receipt";
                case 1: return "Standard";
                case 2: return "Electronic";
                case 3: return "VAT";
                default: return "Unknown";
            }
        }
    }

    public static class RmbStatus
    {
        public const int Draft = 0;
        public const int Submitted = 1;
        public const int PendingDirectorApproval = 2;
        public const int PendingEDMSApproval = 3;
        public const int Approved = 4;
        public const int Processing = 5;
        public const int Cancelled = 6;
        public const int MoreInfo = 7;
        public const int Paid = 8;
        public const int PendingDownload = 10;
        public const int DownloadFailed = 20;

        static public string StatusName(int StatusNo)
        {
            switch (StatusNo)
            {
                case 0: return "Draft";
                case 1: return "Submitted";
                case 2: return "PendingDirectorApproval";
                case 3: return "PendingEDMSApproval";
                case 4: return "Approved";
                case 5: return "Processing";
                case 6: return "Cancelled";
                case 7: return "MoreInfo";
                case 8: return "Paid";
                case 10: return "PendingDownload";
                case 20: return "DownloadFailed";
                

                default: return "Unknown";
            }

        }
    }
    public static class RmbAccess
    {
        public const int Denied = 0;
        public const int Owner = 1;
        public const int Spouse = 2;
        public const int Approver = 3;
        public const int Leader = 4;
        public const int Accounts = 5;


        static public string StatusName(int StatusNo)
        {
            switch (StatusNo)
            {
                case 0: return "Denied";
                case 1: return "Owner";
                case 2: return "Spouse";
                case 3: return "Approver";
                case 4: return "Leader";
                case 5: return "Accounts";

                default: return "Denied";
            }

        }

    }

    public class StaffRmbFunctions
    {
        public const String WEB_SERVICE_ERROR = "ERROR in web service";
        public const String PERMISSION_DENIED_ERROR = "ERROR permission denied";
        private static EventLogController eventLog = new EventLogController();
        private static DotNetNuke.Entities.Portals.PortalSettings portalSettings = new DotNetNuke.Framework.UserControlBase().PortalSettings;
        private static int userId = new DotNetNuke.Entities.Modules.PortalModuleBase().UserId;
        private static int PRESIDENTID = -1;
        private static TimeSpan CACHE_FOR = new TimeSpan(0, 20, 0); //cache for 20 minutes

        public StaffRmbFunctions()
        {
        }

        public class UserInfoComparer : IEqualityComparer<UserInfo>
        {
            public bool Equals(UserInfo one, UserInfo two) {
                return one.UserID == two.UserID;
            }
            public int GetHashCode(UserInfo item) {
                return item.UserID.GetHashCode();
            }
        }
        public struct Approvers
        {
            public HashSet<UserInfo> UserIds;
            public Boolean CCMSpecial, SpouseSpecial, AmountSpecial, isDept;
            public string Name;
        }
        static public String getCostCentres()
        {
            var result = new StaffRmbDataContext().AP_StaffBroker_CostCenters.Select(s => new { label = s.CostCentreCode + ":" + s.CostCentreName, value = s.CostCentreCode}).OrderBy(o => o.value);
            return JsonConvert.SerializeObject(result).Replace('\'', ' ').Replace("\"label\"","label").Replace("\"value\"","value");
            
        }

        static public String GetDefaultProvince(int StaffId) {
            //Get the last province used by this person,
            //otherwise return their home province.
            StaffRmbDataContext d = new StaffRmbDataContext();
            var lines = from c in d.AP_Staff_RmbLines where c.AP_Staff_Rmb.UserId == StaffId orderby c.RmbLineNo descending select c.Spare1;
            if ((lines.Count() > 0) && (lines.First() != null)) {
                return lines.First();
            }
            string Province = StaffBrokerFunctions.GetStaffProfileProperty(StaffId, "Province");
            if (Province != null) {
                return Province;
            }
            return "--";
        }

        static public async Task<Approvers> getApproversAsync(AP_Staff_Rmb rmb)
        {
            int presidentId = getPresidentId();
            int spouseId;
            try {
                spouseId = StaffBrokerFunctions.GetSpouseId(rmb.UserId);
            } catch {
                spouseId = -2;
            }
            int delegateId = -1;
            String delegate_logon = "";
            if (rmb.SpareField3 != null) {
                try {
                    delegateId = int.Parse(rmb.SpareField3);
                    delegate_logon = logonFromId(rmb.PortalId, delegateId);
                } catch {
                    delegateId=-1;
                }
            }
            String staff_logon = logonFromId(rmb.PortalId, rmb.UserId);
            String spouse_logon = logonFromId(rmb.PortalId, spouseId);
            int levels = 2; //the number of supervisor upline to include
            // initialize the response
            Approvers result = new Approvers();
            result.CCMSpecial = false;
            result.SpouseSpecial = false;
            result.AmountSpecial = false;
            result.isDept = false;
            result.UserIds = new HashSet<UserInfo>(new UserInfoComparer());

            if (rmb.CostCenter == null || rmb.CostCenter.Length == 0) return result; //empty result

            Decimal amount = (from line in rmb.AP_Staff_RmbLines select line.GrossAmount).Sum(); 
            Task<String[]> signingAuthorityTask = staffWithSigningAuthorityAsync(rmb.CostCenter, amount, rmb.RID);
            if (isStaffAccount(rmb.CostCenter))
            {
                if (!accountBelongsToStaffMember(rmb.CostCenter, rmb.UserId))
                {
                    return result; //empty result
                }
                Task<int[]>  userSupervisorsTask = getSupervisors(rmb.UserId, levels);
                Task<int[]> spouseSupervisorTask = getSupervisors(spouseId, levels);
                Task<int[]> getELTTask = ELT();
                // Special case where staff member or spouse reports directly to the president, and thus would have only a single approver
                int[] userSupervisors = await userSupervisorsTask;
                if (userSupervisors.Count() == 1 && userSupervisors.Single() == presidentId)
                {
                    userSupervisors = combineArrays(userSupervisors, await getELTTask);
                }
                int[] spouseSupervisors = await spouseSupervisorTask;
                if (spouseSupervisors.Count() == 1 && spouseSupervisors.Single() == presidentId)
                {
                    spouseSupervisors = combineArrays(spouseSupervisors, await getELTTask);
                }
                foreach (int uid in combineArrays(userSupervisors, spouseSupervisors))
                {
                    //exclude user and spouse and delegate
                    if (!((uid == rmb.UserId) || (uid == spouseId) || (uid==delegateId)))
                    {
                        result.UserIds.Add(UserController.GetUserById(rmb.PortalId, uid));
                    }
                }
            }
            else //ministry account
            {
                result.isDept = true;
            }
            String[] potential_approvers = await signingAuthorityTask;
            foreach (String potential_approver in potential_approvers)
            {
                if (!(potential_approver.Equals(staff_logon) || potential_approver.Equals(spouse_logon) || potential_approver.Equals(delegate_logon)))
                { //exclude rmb creator and spouse and delegate
                    UserInfo user = UserController.GetUserByName(rmb.PortalId, potential_approver + rmb.PortalId.ToString());
                    if (user != null)
                    {
                        result.UserIds.Add(user);
                    }
                }
            }
            return result;
        }

        static public bool accountBelongsToStaffMember(String costcenter, int userId)
        {
            StaffBroker.AP_StaffBroker_Staff staff_member = StaffBrokerFunctions.GetStaffMember(userId);
            bool result = (staff_member.CostCenter != null && staff_member.CostCenter.Equals(costcenter));
            return result;
        }

        static private string[] combineArrays(string[] a1, string[] a2)
        {
            string[] result = a1.Concat(a2).Distinct().ToArray();
            return result;
        }

        static private int[] combineArrays(int[] a1, int[] a2)
        {
            int[] result = a1.Union(a2).ToArray();
            return result;
        }

        static public string logonFromId(int portalId, int userId)
        {
            if (userId < 0) return "";
            Regex stripPortal = new Regex(portalId.ToString() + "$");
            return stripPortal.Replace(UserController.GetUserById(portalId, userId).Username, "");
        }

        static private string personalCostCenter(int userId) {
            return StaffBrokerFunctions.GetStaffMember(userId).CostCenter;
        }

        static public bool isStaffAccount(string account)
        // Returns true if account# starts with an 8 or a 9
        {
            if (account.Length != 6) return false; //must be 6 characters long
            if (Regex.Replace(account, @"[\d-]", string.Empty).Length != 0) return false; //must all be digits
            if (account.Substring(0, 1).Equals("8") || account.Substring(0, 1).Equals("9")) return true; //must begin with 8 or 9
            return false;
        }

        static public async Task<string[]> managersInDepartmentAsync(string logon, int RID)
        // Returns a list of staff who supervise other staff in the same department.
        {
            string cacheKey = "managersFor"+logon;
            if (logon.Equals("")) return new string[0];
            string result = (string)HttpContext.Current.Cache.Get(cacheKey);
            if (result == null) {
                string postData = string.Format("logon={0}&client={1}&details={2}", logon, "Reimbursements", "#"+RID.ToString());
                string url = "https://staffapps.powertochange.org/AuthManager/webservice/get_department_supervisors";
                result = await getResultFromWebServiceAsync(url, postData);
                if (result.Length == 0 || result.Equals(WEB_SERVICE_ERROR))
                    result = "[\"ERR\"]"; //this will not produce a visible error, just an empty dropdown
                else
                {
                    HttpContext.Current.Cache.Add(cacheKey, result, null, DateTime.Now.Add(CACHE_FOR), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
                    //Log(RID, "Added to cache " + cacheKey + ": " + result);
                }
            } else {
                //Log(RID, "Retrieved Managers from cache: " + logon + ": " + result);
            }
            return JsonConvert.DeserializeObject<string[]>(result);
        }

        static public bool userSupervisesApprover(int userId, int approverId) {
            int presidentId = getPresidentId();
            if (approverId == presidentId) return false;
            StaffBroker.StaffBrokerDataContext d = new StaffBroker.StaffBrokerDataContext();
            var leaderIds = from l in d.AP_StaffBroker_LeaderMetas where l.UserId == approverId select l.LeaderId;
            foreach (int leaderId in leaderIds) {
                if (leaderId == userId) return true;
                if (userSupervisesApprover(userId, leaderId)) return true;
            }
            return false;
        }

        static public async Task<int[]> getSupervisors(int id, int levels)
        // Returns the <levels># of upline supervisors ids for a staff member
        {
            HashSet<int> result = new HashSet<int>();
            try
            {
                if (id < 0 || levels <= 0) return new int[0];
                int presidentId = getPresidentId();
                if (id == presidentId) return new int[1] { presidentId };
                StaffBroker.StaffBrokerDataContext d = new StaffBroker.StaffBrokerDataContext();
                var leaderIds = from l in d.AP_StaffBroker_LeaderMetas where l.UserId == id select l.LeaderId;
                foreach (int leaderId in leaderIds) {
                    result.Add(leaderId);
                    foreach (int supervisor in await getSupervisors(leaderId, (levels-1))) {
                        result.Add(supervisor);
                    }
                }
            }
            catch { }
            return result.ToArray<int>();
        }

        static public async Task<int[]> ELT()
        // Returns a list of the members of the ELT (IDs), which are those who report directly to the president, and have themselves, people reporting to them
        // It also includes the president himself
        {
            List<int> eltIds = new List<int>();
            foreach (UserInfo user in DotNetNuke.Entities.Users.UserController.GetUsers(0)) {
                if (user.IsInRole("ELT")) {
                    eltIds.Add(user.UserID);
                }
            }
            int presidentId = getPresidentId();
            if (eltIds.Contains(presidentId) == false) {
                eltIds.Add(presidentId);
            }
            return eltIds.ToArray<int>();
        }

        static public int getPresidentId()
        {
            if (PRESIDENTID >= 0) return PRESIDENTID;
            PRESIDENTID = discoverPresidentId();
            return PRESIDENTID;
        }

        static public void setPresidentId(int presidentId)
        {
            PRESIDENTID = presidentId;
        }

        static public int discoverPresidentId()
        // Determines the most likely person to be president based on supervisor information
        {
            int presidentId = 0;
            try {
                ModuleController mc = new ModuleController();
                presidentId = (int)(mc.GetTabModule(265).TabModuleSettings["PresidentId"]);
                if (presidentId <= 0) throw new Exception();
            } catch {
                try {
                    presidentId = int.Parse(ConfigurationManager.AppSettings["PresidentId"]);
                    if (presidentId <= 0) throw new Exception();
                } catch {
                    presidentId = 114; //Rod Bergen
                }
            }
            return presidentId;
        }

        static private async Task<string[]> staffWithSigningAuthorityAsync(string account, Decimal amount, int RID)
        // Returns a list of staff with signing authority for a certain amount or greater on a given account
        {
            string cacheKey = "signatoriesFor" + account + amount.ToString();
            string result = (string)HttpContext.Current.Cache.Get(cacheKey);
            if (result == null)
            {
                string postData = string.Format("account={0}&amount={1}&exclude_administrators={2}&client={3}&details={4}", account, amount, "true", "Reimbursements", "#"+RID.ToString());
                string url = "https://staffapps.powertochange.org/AuthManager/webservice/get_signatories";
                result = await getResultFromWebServiceAsync(url, postData);
                if (result.Length == 0 || result.Equals(WEB_SERVICE_ERROR))
                    result = "[\"ERR\"]"; //this will not produce a visible error, just an empty dropdown
                else {
                    HttpContext.Current.Cache.Add(cacheKey, result, null, DateTime.Now.Add(CACHE_FOR), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
                    //Log( RID, "Added signatories to Cache: " + account + "/"+amount.ToString()+": " + result );
                }
            } else {
                //Log(RID, "Retrieved signatories from cache: " + account+"/"+amount.ToString() + ": " + result );
            }
            return JsonConvert.DeserializeObject<string[]>(result);
        }

        //** Unused?
        //static public async Task<string[]> staffWhoReportToAsync(string logon, Boolean directly)
        //// Returns a list of staff who report (either directly or indirectly) to the given user
        //{
        //    string postData = string.Format("logon={0}&directly{1}", logon, directly);
        //    string url = "https://staffapps.powertochange.org/Authmanager/webservice/get_subordinates";
        //    string result = await getResultFromWebServiceAsync(url, postData);
        //    return JsonConvert.DeserializeObject<string[]>(result);
        //}

        static public async Task<object> getCompanies()
        // Returns a list of companies
        {
            string cacheKey = "RmbCompanies";
            string result = (string)HttpContext.Current.Cache.Get(cacheKey);
            if (result == null)
            {
                string postData = "";
                string url = "http://gpapp/gpimport/webservice/GetCompanies";
                result = await getResultFromWebServiceAsync(url, postData);
                if (result.Length == 0 || result.Equals(WEB_SERVICE_ERROR)) 
                    result = "[{\"CompanyID\":\"ERR\",\"CompanyName\":\"Oops, No companies!  Press F5 to reload the page.\"}]";
                else
                {
                    HttpContext.Current.Cache.Add(cacheKey, result, null, DateTime.Now.Add(CACHE_FOR), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
                    //Log(-1, "Added companies to Cache: " + result );
                }
            } else {
                //Log(-1, "Retrieved companies from cache: " + result );
            }
            return JsonConvert.DeserializeObject(result);
        }

        static public async Task<object> getRemitToAddresses(String company, String vendorId)
            // Returns a list of addresses for a given company and vendor
        {
            string cacheKey = "addressesFor" + company + "/" + vendorId;
            string result = (string)HttpContext.Current.Cache.Get(cacheKey);
            if (result == null)
            {
                string postData = string.Format("company={0}&vendorId={1}", company, vendorId);
                string url = "http://gpapp/gpimport/webservice/GetRemitToAddresses";
                result = await getResultFromWebServiceAsync(url, postData);
                if (result.Length == 0 || result.Equals(WEB_SERVICE_ERROR)) 
                    result = "[{\"AddressID\":\"ERR\",\"DefaultRemitToAddress\":\"N\",\"Address1\":\"Oops, No addresses!  Re-select the vendor to try again.\"}]";
                else {
                    HttpContext.Current.Cache.Add(cacheKey, result, null, DateTime.Now.Add(CACHE_FOR), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
                    //Log(-1, "Added addresses to Cache "+ cacheKey + ": " + result );
                }
            } else {
                //Log(-1, "Retrieved addresses from cache "+cacheKey +": " + result );
            }
            return JsonConvert.DeserializeObject(result);
        }

        //** Unused??
        //static public async Task<string> getAccountsAsync(string logon)
        //// Returns a list of all accounts, provided the given logon is valid (using checkAuthorizations where user==logon)
        //{
        //    string postData = string.Format("logon={0}", logon);
        //    string url = "https://staffapps.powertochange.org/Authmanager/webservice/get_accounts";
        //    string result = await getResultFromWebServiceAsync(url, postData);
        //    return result;
        //}

        static public async Task<string> getAccountBalanceAsync(string account, string user_logon)
        // Returns the balance of the account for staff accounts, or the budget:actual amounts for ministry accounts
        //, or PERMISSION_DENIED_ERROR if the specified user does not have View Finanicals access to the account
        {
            if (account.Equals(string.Empty) || user_logon.Equals(string.Empty)) return WEB_SERVICE_ERROR;
            string postData = string.Format("_reportPath=/General/Account%20Balance&_renderFormat=XML&_apiToken={0}&ProjectCodeSearch={1}&ExecuteAsUser={2}", Constants.getApiToken(), account, user_logon);
            string url = "http://SQL2012/CallRptServices/CallRpt.aspx";
            string response = await getResultFromWebServiceAsync(url, postData);
            if (response.Length == 0) return "";
            string result = "";
            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(response);
                if (xDoc.GetElementsByTagName("Detail")[0].Attributes["AccountDescription"].Value.Contains("YOU DON'T HAVE ACCESS TO THIS ACCOUNT")) {
                    return PERMISSION_DENIED_ERROR;
                }
                System.Globalization.NumberFormatInfo currencyFormat = new System.Globalization.CultureInfo(System.Globalization.CultureInfo.CurrentCulture.ToString()).NumberFormat;
                currencyFormat.CurrencyNegativePattern = 1;
                if (isStaffAccount(account))
                {
                    Double balance = Double.Parse(xDoc.GetElementsByTagName("Detail")[0].Attributes["Balance"].Value);
                    result = Math.Round(balance, 2).ToString("C", currencyFormat);
                }
                else
                {
                    Double budget = Double.Parse(xDoc.GetElementsByTagName("Detail")[0].Attributes["Budget"].Value);
                    Double actual = Double.Parse(xDoc.GetElementsByTagName("Detail")[0].Attributes["Actual"].Value);
                    result = Math.Round(budget, 2).ToString("C", currencyFormat) + ":" + Math.Round(actual, 2).ToString("C", currencyFormat);
                }
            }
            catch (Exception e)
            {
                eventLog.AddLog("getAccountBalanceAsync()", e.Message, portalSettings, userId, EventLogController.EventLogType.ADMIN_ALERT);
                return WEB_SERVICE_ERROR;
            }
            return result;
        }

        static public async Task<string> getBudgetBalanceAsync(string account, string user_logon)
        // Returns the current balance of the budget for this account, or "" if the specified user does not have View Financials access to the account
        {
            return "Not Yet Implemented";
        }

        static private async Task<string> getResultFromWebServiceAsync(string url, string postString)
        {

            byte[] postData = Encoding.UTF8.GetBytes(postString);
            //send request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
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
                var response = (HttpWebResponse) request.GetResponse();
                if (response != null)
                {
                    var reader = new StreamReader(response.GetResponseStream());
                    String response_string = await reader.ReadToEndAsync();
                    if (response_string.Contains("ERROR")) {
                        Log(-1, "WEB SERVICE ERROR: " + response_string);
                        return WEB_SERVICE_ERROR;
                    }
                    return response_string;
                }
                eventLog.AddLog("getResultFromWebServiceAsync()", "No data returned", portalSettings, userId, EventLogController.EventLogType.ADMIN_ALERT);
                return "";
            }
            catch (WebException e)
            {
                eventLog.AddLog("getResultFromWebServiceAsync()", e.Message, portalSettings, userId, EventLogController.EventLogType.ADMIN_ALERT);
                return WEB_SERVICE_ERROR;
            }
        }


        //static public Approvers getApprovers(AP_Staff_Rmb rmb, DotNetNuke.Entities.Users.UserInfo authUser, DotNetNuke.Entities.Users.UserInfo authAuthUser)
        //{
        //    StaffBroker.StaffBrokerDataContext dStaff = new StaffBroker.StaffBrokerDataContext();
        //    Approvers rtn = new Approvers();

        //    var st = StaffBrokerFunctions.GetStaffMember(rmb.UserId);
        //    rtn.Name = st.DisplayName;
        //    int SpouseId = StaffBrokerFunctions.GetSpouseId(rmb.UserId);
        //    rtn.AmountSpecial = (from c in rmb.AP_Staff_RmbLines where c.LargeTransaction == true select c).Count() > 0;
        //    rtn.isDept = (rmb.CostCenter != st.CostCenter);
        //    rtn.SpouseSpecial = false;
        //    rtn.UserIds = new List<DotNetNuke.Entities.Users.UserInfo>();
        //    if (rtn.isDept)
        //    {
        //        var cc = from c in dStaff.AP_StaffBroker_Departments where (c.CostCentre == rmb.CostCenter) && c.PortalId == rmb.PortalId select c;
        //        rtn.CCMSpecial = (from c in cc
        //                          where ((c.CostCentreManager == null && c.CostCentreDelegate == null) == false) &&
        //                              (
        //                              ((c.CostCentreManager != rmb.UserId) && (c.CostCentreManager != SpouseId)) ||
        //                              ((c.CostCentreDelegate != rmb.UserId) && (c.CostCentreDelegate != SpouseId))

        //                              )
        //                          select c.CostCenterId).Count() == 0;

        //        if (rtn.CCMSpecial || rtn.AmountSpecial || rtn.SpouseSpecial)
        //        {
        //            rtn.UserIds.Add(authUser.UserID == rmb.UserId ? authAuthUser : authUser);

        //            if (cc.First().CostCentreManager == rtn.UserIds.First().UserID || cc.First().CostCentreDelegate == rtn.UserIds.First().UserID)
        //            {
        //                rtn.AmountSpecial = false;
        //                rtn.CCMSpecial = false;
        //            }

                   
        //        }
        //        else
        //        {

        //            if (cc.First().CostCentreManager != rmb.UserId && cc.First().CostCentreManager != SpouseId && cc.First().CostCentreManager != null)
        //                rtn.UserIds.Add(DotNetNuke.Entities.Users.UserController.GetUserById(rmb.PortalId, (int)cc.First().CostCentreManager));
        //            if (cc.First().CostCentreDelegate != rmb.UserId && cc.First().CostCentreDelegate != SpouseId && cc.First().CostCentreDelegate != null)
        //                rtn.UserIds.Add(DotNetNuke.Entities.Users.UserController.GetUserById(rmb.PortalId, (int)cc.First().CostCentreDelegate));

        //        }
        //        if (cc.Count() > 0)
        //            rtn.Name = cc.First().Name;

        //    }
        //    else
        //    {
        //        rtn.CCMSpecial = false;
        //        var app2 = StaffBrokerFunctions.GetLeaders(rmb.UserId, true);
        //        rtn.SpouseSpecial = (app2.Count() == 1 && ((app2.First() == SpouseId) || (app2.First() == rmb.UserId)));
        //        if (rtn.AmountSpecial || rtn.SpouseSpecial || app2.Count() == 0)
        //        {
        //            rtn.UserIds.Add(authUser.UserID == rmb.UserId ? authAuthUser : authUser);
                   
        //            if (app2.Contains(rtn.UserIds.First().UserID))
        //            {
        //                rtn.AmountSpecial = false;
        //            }
        //        }
        //        else
        //        {
        //            foreach (int i in (from c in app2 where c != rmb.UserId && c != SpouseId select c))
        //                rtn.UserIds.Add(DotNetNuke.Entities.Users.UserController.GetUserById(rmb.PortalId, i));
        //        }

        //    }

        //    if(rtn.UserIds.Count()==0)
        //        rtn.UserIds.Add(authUser.UserID == rmb.UserId ? authAuthUser : authUser);



        //    return rtn;
        //}

        //static public Approvers getAdvApprovers(AP_Staff_AdvanceRequest  adv, double LargeTransaction, DotNetNuke.Entities.Users.UserInfo authUser, DotNetNuke.Entities.Users.UserInfo authAuthUser)
        //{
        //    StaffBroker.StaffBrokerDataContext dStaff = new StaffBroker.StaffBrokerDataContext();
        //    Approvers rtn = new Approvers();

        //    var st = StaffBrokerFunctions.GetStaffMember((int)adv.UserId );
        //    rtn.Name = st.DisplayName;
        //    int SpouseId = StaffBrokerFunctions.GetSpouseId((int)adv.UserId);
        //    rtn.AmountSpecial = ((double)adv.RequestAmount)>LargeTransaction ;
           
        //    rtn.SpouseSpecial = false;
        //    rtn.UserIds = new List<DotNetNuke.Entities.Users.UserInfo>();
            
        //    var app2 = StaffBrokerFunctions.GetLeaders((int)adv.UserId, true);
        //    rtn.SpouseSpecial = (app2.Count() == 1 && ((app2.First() == SpouseId) || (app2.First() == (int)adv.UserId)));
        //    if (rtn.AmountSpecial || rtn.SpouseSpecial || app2.Count() == 0)
        //    {
        //        rtn.UserIds.Add(authUser.UserID == adv.UserId ? authAuthUser : authUser);

        //        if (app2.Contains((authUser.UserID == adv.UserId ? (authAuthUser.UserID) : authUser.UserID)))
        //        {
        //            rtn.AmountSpecial = false;
        //        }
        //    }
        //    else
        //    {
        //        foreach (int i in (from c in app2 where c != adv.UserId && c != SpouseId select c))
        //            rtn.UserIds.Add(DotNetNuke.Entities.Users.UserController.GetUserById(adv.PortalId, i));
        //    }
        //    return rtn;
        //}


        static public int GetNewRID(int PortalId)
        {
            StaffRmbDataContext d = new StaffRmbDataContext();
            var MaxRID = (from c in d.AP_Staff_Rmbs where c.PortalId == PortalId orderby c.RID descending select c.RID);
            if (MaxRID.Count() == 0) {
                return 1;
            } else {
                return MaxRID.First() + 1;
            }
        }

        static public int GetNewAdvId(int PortalId)
        {
            StaffRmbDataContext d = new StaffRmbDataContext();
            var MaxAdvID = (from c in d.AP_Staff_AdvanceRequests where c.PortalId == PortalId orderby c.LocalAdvanceId descending select c.LocalAdvanceId);
            if (MaxAdvID.Count() == 0 || MaxAdvID.First() == null) {
                return 1;
            } else {
                return Convert.ToInt32(MaxAdvID.First()) + 1;
            }
        }
        static public int Authenticate(int UserId, int RmbNo, int PortalId )
        {
            StaffRmbDataContext d = new StaffRmbDataContext();
            var rmb = from c in d.AP_Staff_Rmbs where c.RMBNo == RmbNo  && c.PortalId == PortalId  select c;

            if (rmb.Count() > 0)
            {
                if (rmb.First().UserId == UserId)
                    return RmbAccess.Owner;
                else if (rmb.First().ApprUserId == UserId)
                    return RmbAccess.Approver;
                else
                {
                    var spouseId = StaffBrokerFunctions.GetSpouseId(UserId);
                    if (rmb.First().UserId == spouseId)
                        return RmbAccess.Spouse;
                    else
                    {
                        var team = from c in StaffBrokerFunctions.GetTeam(UserId) select c.UserID;
                        string pcc = StaffBrokerFunctions.GetStaffMember(UserId).CostCenter;
                        if (rmb.First().CostCenter == pcc)
                        {
                            if (team.Contains(rmb.First().UserId))
                                return RmbAccess.Approver;

                        }
                        else
                        {
                            var depts = from c in StaffBrokerFunctions.GetDepartments(UserId) select c.CostCentre;

                            if (depts.Contains(rmb.First().CostCenter))
                                return RmbAccess.Approver;
                            else if (team.Contains(rmb.First().UserId))
                                return RmbAccess.Leader;
                        }





                    }

                }

            }
            return RmbAccess.Denied;
        }


        static public int AuthenticateAdv(int UserId, int AdvanceId, int PortalId)
        {
            StaffRmbDataContext d = new StaffRmbDataContext();
            var adv = from c in d.AP_Staff_AdvanceRequests where c.AdvanceId == AdvanceId && c.PortalId == PortalId  select c;

            if (adv.Count() > 0)
            {
                if (adv.First().UserId == UserId)
                    return RmbAccess.Owner;
                else if (adv.First().ApproverId  == UserId)
                    return RmbAccess.Approver;
                else
                {
                    var spouseId = StaffBrokerFunctions.GetSpouseId(UserId);
                    if (adv.First().UserId == spouseId)
                        return RmbAccess.Spouse;
                    else
                    {
                        var team = from c in StaffBrokerFunctions.GetTeam(UserId) select c.UserID;
                        
                            if (team.Contains((int)(adv.First().UserId)) )
                                return RmbAccess.Approver;

                    }

                }

            }
            return RmbAccess.Denied;
        }

        static public string firstNameFromDisplayName(string displayName) {
            //Returns all except last word of displayName
            if (displayName == null) return "";
            string trimmedName = displayName.Trim();
            if (!trimmedName.Contains(' ')) return "";
            return trimmedName.Substring(0, trimmedName.LastIndexOf(' '));
        }

        static public string lastNameFromDisplayName(string displayName) {
            // Returns last word of displayName
            if (displayName == null) return "";
            string trimmedName = displayName.Trim();
            if (!trimmedName.Contains(' ')) return trimmedName;
            return trimmedName.Substring(trimmedName.LastIndexOf(' ') + 1);
        }

        static private void Log(int RID, string Message)
        {
            short verbose = 0;
            StaffRmbDataContext d = new StaffRmbDataContext();
            string username = "";
            try { username = UserController.Instance.GetCurrentUserInfo().DisplayName; }
            catch { }
            d.AP_Staff_Rmb_Logs.InsertOnSubmit(new AP_Staff_Rmb_Log() { Timestamp = DateTime.Now, LogType = verbose, RID = RID, Username=username, Message = Message });
            d.SubmitChanges();
        }

        static public String urlEncode(String text)
        {
            return text.Replace("+", "-").Replace("/", "_").Replace("=", ".");
        }
        static public String urlDecode(String text)
        {
            return text.Replace("-", "+").Replace("_", "/").Replace(".", "=");
        }
    }
}
