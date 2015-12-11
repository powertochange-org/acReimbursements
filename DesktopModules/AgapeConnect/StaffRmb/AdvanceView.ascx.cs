using DotNetNuke.Web.Mvp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebFormsMvp;

using PowerToChange.Modules.StaffRmb.Presenters;
using StaffRmb;

namespace DotNetNuke.Modules.StaffRmbMod.Views
{
    [PresenterBinding(typeof(AdvancePresenter))]
    public partial class AdvanceView : ModuleView, IAdvancePresenter
    {
        public IEnumerable<AP_Staff_Rmb> OutstandingAdvances
        {
            set { gvGrid }
        }
    }

    interface IAdvancePresenter
    {
        IEnumerable<AP_Staff_Rmb> OutstandingAdvances { set; }
    }
}