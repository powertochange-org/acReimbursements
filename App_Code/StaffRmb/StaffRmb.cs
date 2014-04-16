using Newtonsoft.Json;
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
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Summary description for StaffRmb
/// </summary>
/// 
namespace StaffRmb
{
    public static class RmbReceiptMode
    {
        public const int Disabled = 0;
        public const int NoVAT = 1;
        public const int VAT = 2;
        static public string ModeName(int ModeNo)
        {
            switch (ModeNo)
            {
                case 0: return "Disabled";
                case 1: return "NoVAT";
                case 2: return "VAT";
                default: return "Unknown";
            }

        }
    }
    public static class RmbStatus
    {
        public const int Draft = 0;
        public const int Submitted = 1;
        public const int Approved = 2;
        public const int Processed = 3;
        public const int Cancelled = 4;
        public const int MoreInfo = 5; 
        public const int PendingDownload = 10;
        public const int DownloadFailed = 20;
        static public string StatusName(int StatusNo)
        {
            switch (StatusNo)
            {
                case 0: return "Draft";
                case 1: return "Submitted";
                case 2: return "Approved";
                case 3: return "Processed";
                case 4: return "Cancelled";
                case 5: return "MoreInfo";
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
        public struct Approvers
        {
            public List<DotNetNuke.Entities.Users.UserInfo> UserIds;
            public Boolean CCMSpecial, SpouseSpecial, AmountSpecial, isDept;
            public string Name;
        }
        public StaffRmbFunctions()
        {
        }

        static public String getCostCentres()
        {
            var result = new StaffRmbDataContext().AP_StaffBroker_CostCenters.Select(s => new { label = s.CostCentreCode + ":" + s.CostCentreName, value = s.CostCentreCode}).OrderBy(o => o.value);
            return JsonConvert.SerializeObject(result).Replace('\'', ' ').Replace("\"label\"","label").Replace("\"value\"","value");
            
        }


        static public async Task<Approvers> getApproversAsync(AP_Staff_Rmb rmb, DotNetNuke.Entities.Users.UserInfo authUser, DotNetNuke.Entities.Users.UserInfo authAuthUser)
        {
            String staff_logon = logonFromId(rmb.PortalId, rmb.UserId);
            String spouse_logon = logonFromId(rmb.PortalId, StaffBrokerFunctions.GetSpouseId(rmb.UserId));
            // initialize the response
            Approvers result = new Approvers();
            result.CCMSpecial = false;
            result.SpouseSpecial = false;
            result.AmountSpecial = false;
            result.isDept = false;
            result.UserIds = new List<DotNetNuke.Entities.Users.UserInfo>();

            if (rmb.CostCenter == null || rmb.CostCenter.Length == 0) return result;

            string[] potential_approvers = null;
            if (isStaffAccount(rmb.CostCenter))
            {
                Task<String[]> getStaffManagersTask = managersInDepartmentAsync(staff_logon);
                Task<String[]> getSpouseManagersTask = managersInDepartmentAsync(spouse_logon);
                potential_approvers = combineArrays(await getStaffManagersTask, await getSpouseManagersTask);
            }
            else //ministry account
            {
                result.isDept = true;
                Decimal amount = (from line in rmb.AP_Staff_RmbLines select line.GrossAmount).Sum();
                amount += (Decimal) 0.00; //exclude staff with "view only" signing authority ($0)
                potential_approvers = await staffWithSigningAuthorityAsync(rmb.CostCenter, amount);
            }

            foreach (String potential_approver in potential_approvers) {
                if (! (potential_approver.Equals(staff_logon) || potential_approver.Equals(spouse_logon))) { //exclude rmb creator and spouse
                    result.UserIds.Add(UserController.GetUserByName(rmb.PortalId, potential_approver+rmb.PortalId.ToString()));
                }
            }
            return result;
        }

        static public async Task<Approvers> getAdvApproversAsync(AP_Staff_AdvanceRequest adv, Double largeTransaction, DotNetNuke.Entities.Users.UserInfo authUser, DotNetNuke.Entities.Users.UserInfo authAuthUser)
        {
            String staff_logon = logonFromId(adv.PortalId , (int)adv.UserId);
            String spouse_logon = logonFromId(adv.PortalId, StaffBrokerFunctions.GetSpouseId((int)adv.UserId));
            // initialize the response
            Approvers result = new Approvers();
            result.CCMSpecial = false;
            result.SpouseSpecial = false;
            result.AmountSpecial = false;
            result.isDept = false;
            result.UserIds = new List<DotNetNuke.Entities.Users.UserInfo>();

            string[] potential_approvers = null;
            Decimal amount = (Decimal)adv.RequestAmount;
            potential_approvers = await staffWithSigningAuthorityAsync(personalCostCenter((int)adv.UserId), amount);
            foreach (String potential_approver in potential_approvers)
            {
                if (!(potential_approver.Equals(staff_logon) || potential_approver.Equals(spouse_logon)))
                { //exclude staff and spouse
                    result.UserIds.Add(UserController.GetUserByName(adv.PortalId, potential_approver + adv.PortalId.ToString()));
                }
            }
            return result;
        }

        static private string[] combineArrays(string[] a1, string[] a2)
        {
            string[] result = a1.Concat(a2).Distinct().ToArray();
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

        static private async Task<string[]> managersInDepartmentAsync(string logon)
        // Returns a list of staff who supervise other staff in the same department.
        {
            if (logon.Equals("")) return new string[0];
            string postData = string.Format("logon={0}", logon);
            string url = "https://staffapps.powertochange.org/AuthManager/webservice/get_department_supervisors";
            string result = await getResultFromWebServiceAsync(url, postData);
            return JsonConvert.DeserializeObject<string[]>(result);
        }

        static private async Task<string[]> staffWithSigningAuthorityAsync(string account, Decimal amount)
        // Returns a list of staff with signing authority for a certain amount or greater on a given account
        {
            string postData = string.Format("account={0}&amount={1}", account, amount);
            string url = "https://staffapps.powertochange.org/AuthManager/webservice/get_signatories";
            string result = await getResultFromWebServiceAsync(url, postData);
            return JsonConvert.DeserializeObject<string[]>(result);
        }

        static public async Task<string[]> staffWhoReportToAsync(string logon, Boolean directly)
        // Returns a list of staff who report (either directly or indirectly) to the given user
        {
            string postData = string.Format("logon={0}&directly{1}", logon, directly);
            string url = "https://staffapps.powertochange.org/Authmanager/webservice/get_subordinates";
            string result = await getResultFromWebServiceAsync(url, postData);
            return JsonConvert.DeserializeObject<string[]>(result);
        }

        static public async Task<string> getAccountsAsync(string logon)
        // Returns a list of all accounts, provided the given logon is valid (using checkAuthorizations where user==logon)
        {
            string postData = string.Format("logon={0}", logon);
            string url = "https://staffapps.powertochange.org/Authmanager/webservice/get_accounts";
            string result = await getResultFromWebServiceAsync(url, postData);
            return result;
        }

        static public async Task<string> getAccountBalanceAsync(string account, string user_logon)
        // Returns the balance of the account, or "" if the specified user does not have View Finanicals access to the account
        {
            string postData = string.Format("_reportPath=/General/Account%20Balance&_renderFormat=CSV&_apiToken={0}&ProjectCodeSearch={1}&ExecuteAsUser={2}", Constants.getApiToken(), account, user_logon);
            string url = "https://1chronicles/CallRptServicesTest/CallRpt.aspx";
            string response = await getResultFromWebServiceAsync(url, postData);
            string result = "";
            Match match = Regex.Match(response, @"\""([0-9,\-\.]+)\""\r?\n?$");  //Look at digits, comma, period and minus in quotes at the end of the string.
            if (match.Success) {
                result = match.Groups[1].Value;
            }
            return result;
        }

        static public async Task<string> getBudgetBalanceAsync(string account, string user_logon)
        // Returns the current balance of the budget for this account, or "" if the specified user does not have View Financials access to the account
        {
            return "200.00";
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
                    return response_string;
                }
                return "NO-DATA";
            }
            catch (WebException e)
            {
                return "ERROR";
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
            string NextRID = StaffBrokerFunctions.GetSetting("NextRID", PortalId);
            if (NextRID == "")
            {
                StaffRmbDataContext d = new StaffRmbDataContext();
                var MaxRID = (from c in d.AP_Staff_Rmbs where c.PortalId == PortalId select c.RID);
                if (MaxRID.Count() == 0)
                {
                    StaffBrokerFunctions.SetSetting("NextRID", "2", PortalId);
                    return 1;

                }
                else
                {
                    StaffBrokerFunctions.SetSetting("NextRID", (MaxRID.Max() + 1).ToString(), PortalId);
                    return MaxRID.First();
                }
            }
            else
            {

                StaffBrokerFunctions.SetSetting("NextRID",  (Convert.ToInt32(NextRID) + 1).ToString(), PortalId);
                return Convert.ToInt32( NextRID);
            }        
        }

        static public int GetNewAdvId(int PortalId)
        {
            string NextAdvID = StaffBrokerFunctions.GetSetting("NextAdvID", PortalId);
            if (NextAdvID == "")
            {
                StaffRmbDataContext d = new StaffRmbDataContext();
                var MaxAdvID = (from c in d.AP_Staff_AdvanceRequests  where c.PortalId == PortalId select c.LocalAdvanceId);
                if (MaxAdvID.Count() == 0)
                {
                    StaffBrokerFunctions.SetSetting( "NextAdvID",  "2", PortalId);
                    return 1;

                }
                else
                {
                    StaffBrokerFunctions.SetSetting("NextAdvID", (MaxAdvID.Max() + 1).ToString(), PortalId);
                    return (int) MaxAdvID.First();
                }
            }
            else
            {

                StaffBrokerFunctions.SetSetting("NextAdvID", (Convert.ToInt32(NextAdvID) + 1).ToString(), PortalId);
                return Convert.ToInt32(NextAdvID);
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

    }
}