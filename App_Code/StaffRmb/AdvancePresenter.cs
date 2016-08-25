using DotNetNuke.Web.Mvp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using StaffRmb;

using PowerToChange.Modules.StaffRmb.Views;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Users;

namespace PowerToChange.Modules.StaffRmb.Presenters
{
    public class AdvancePresenter : ModulePresenter<IAdvanceView, AP_Staff_RmbLine>
    {
        private StaffRmbDataContext d;
        private int _advanceLineType;
        private IAdvanceView _view;

        public AdvancePresenter(IAdvanceView view)
            : base(view)
        {
            _view = view;
        }

        public void Initialize()
        {
            d = this.View.DataContext;
            this.View.ModuleLoad += this.ModuleLoad;
        }

        public void ModuleLoad(object sender, ModuleLoadEventArgs args)
        {
            bool isFinance = isAccounts();
            if (!isFinance) {
                _view.Warning = "This page can only be viewed by the Finance department";
            } else {
                if (!args.isPostBack)
                {
                    _advanceLineType = int.Parse((string)ModuleContext.Settings["AdvanceLineType"]);
                    _view.OutstandingAdvances = d.AP_Staff_RmbLines.Where(a =>
                        a.LineType == _advanceLineType
                        && a.GrossAmount > 0
                        && a.Spare2.Length > 0
                        && !a.Spare2.Equals("0")
                        && !a.Spare2.Equals("CLEARED")
                        && a.AP_Staff_Rmb.Status >= RmbStatus.Approved
                        && a.AP_Staff_Rmb.Status != RmbStatus.Cancelled)
                        .Select(b => new OutstandingAdvance()
                        {
                            LineNo = b.RmbLineNo,
                            RID = b.AP_Staff_Rmb.RID,
                            userId = b.AP_Staff_Rmb.UserId,
                            status = b.AP_Staff_Rmb.Status,
                            date = b.TransDate,
                            comment = b.Comment,
                            originalAmount = b.GrossAmount,
                            account = b.AP_Staff_Rmb.CostCenter,
                            outstandingAmount = b.Spare2
                        });
                }
            }
        }

        private bool isAccounts()
        {
            try
            {
                _view.Log = "Finance";
                UserInfo currentUser = DotNetNuke.Entities.Users.UserController.Instance.GetCurrentUserInfo();
                string accountRoleSetting = ((string)ModuleContext.Settings["AccountsRoles"]);
                string[] accountRoles = accountRoleSetting.Split(';');
                foreach (string role in accountRoles){
                    if (currentUser.Roles.Contains(role)) return true;
                }
                _view.Log = "fail";
            } catch (Exception ex){
                _view.Log=ex.Message; 
            }
            return false;
        }

    }
}