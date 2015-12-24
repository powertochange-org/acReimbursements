using DotNetNuke.Web.Mvp;
using StaffRmb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PowerToChange.Modules.StaffRmb.Presenters
{
    public interface IAdvanceView : IModuleView<AP_Staff_RmbLine>
    {
        IEnumerable<OutstandingAdvance> OutstandingAdvances { set; }
        StaffRmbDataContext DataContext { get; }
        string Warning { set; }
        string Log { set; }

        event EventHandler<ModuleLoadEventArgs> ModuleLoad;

    }


    public class ModuleLoadEventArgs : EventArgs
    {
        public bool isPostBack { get; set; }
    }

    public class OutstandingAdvance
    {
        public long LineNo { get; set; }
        public int RID {get; set;}
        public int userId { get; set; }
        public int status { get; set; }
        public DateTime? date { get; set; }
        public decimal originalAmount { get; set; }
        public string outstandingAmount { get; set; }
        public string comment { get; set; }
    }

}