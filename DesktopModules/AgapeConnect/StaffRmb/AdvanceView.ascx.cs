using DotNetNuke.Web.Mvp;
using DotNetNuke.Services.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebFormsMvp;
using StaffBroker;

using PowerToChange.Modules.StaffRmb.Presenters;
using StaffRmb;

namespace PowerToChange.Modules.StaffRmb.Views
{
    [PresenterBinding(typeof(AdvancePresenter))] //This doesn't work with presenter in app_code so manually binding in the constructor hooks events
    public partial class AdvanceView : ModuleView<AP_Staff_RmbLine>, IAdvanceView
    {
        private AdvancePresenter _presenter;
        private StaffBrokerDataContext sbdc;
        private StaffRmbDataContext d;
        public AdvanceView() {
            _presenter = new AdvancePresenter(this);  // manually effect PresenterBinding
            sbdc = new StaffBrokerDataContext();
            d = new StaffRmbDataContext();
            _presenter.Initialize();
        }

        public IEnumerable<OutstandingAdvance> OutstandingAdvances
        {
            set { 
                gvGrid.DataSource = value;
                gvGrid.DataBind();
            }
        }
        public StaffRmbDataContext DataContext { get { return d; } }
        public string Warning { set { lblWarning.Text = value; } }
        public string Log { set { hfLog.Value = value; } }

        public event EventHandler<ModuleLoadEventArgs> ModuleLoad;

        protected void Page_Load(object sender, EventArgs e) {
            ModuleLoadEventArgs args = new ModuleLoadEventArgs() { isPostBack = this.Page.IsPostBack };
            try { if (ModuleLoad != null) { ModuleLoad(this, args); } }
            catch (Exception ex) { Exceptions.ProcessModuleLoadException(this, ex); }
        }

        protected void SetupGridLine(Object sender, GridViewRowEventArgs args)
        {
            if (args.Row.RowType == DataControlRowType.DataRow)
            {
                OutstandingAdvance adv = (OutstandingAdvance) args.Row.DataItem;
                Label lblWho = (Label)args.Row.FindControl("lblWho");
                lblWho.Text = sbdc.Users.Where(a => a.UserID == adv.userId).Single().DisplayName;
                decimal outstanding;
                decimal.TryParse(adv.outstandingAmount, out outstanding);
                decimal cleared = adv.originalAmount - outstanding;
                TextBox tbCleared = (TextBox)args.Row.FindControl("tbCleared");
                tbCleared.Text = string.Format("{0:0.00}", cleared);
            }
        }

        protected void LineChanged(Object sender, GridViewEditEventArgs args)
        {
            int index = args.NewEditIndex;
            GridViewRow row = gvGrid.Rows[index];
            decimal cleared = Convert.ToDecimal(((TextBox)row.FindControl("tbCleared")).Text);
            long lineNo = Convert.ToInt64(((HiddenField)row.FindControl("hfLineNo")).Value);
            AP_Staff_RmbLine line = d.AP_Staff_RmbLines.Where(a => a.RmbLineNo == lineNo).Single();
            String outstanding = (line.GrossAmount - cleared).ToString("0.##");
            if (outstanding.Equals("0")) outstanding = "CLEARED";
            line.Spare2 = outstanding;
            ((Label)row.FindControl("lblOutstanding")).Text = outstanding;
            d.SubmitChanges();
            ModuleLoad(this, new ModuleLoadEventArgs() {isPostBack=false});
        }
    }

}