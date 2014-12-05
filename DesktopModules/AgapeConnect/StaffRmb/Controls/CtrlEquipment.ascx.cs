using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class ControlBase : StaffRmb.StaffRmbControl
{

    public string Spare2 {
        get { return ddlType.SelectedValue; }
        set { ddlType.SelectedValue = value;  }
    }

    public bool ValidateForm(int UserId)
    {
        bool result = base.ValidateForm(UserId);
        ErrorLbl1.Text = ErrorLbl.Text;
        ErrorLbl.Visible = false;
        ErrorLbl1.Visible = ErrorLbl.Text.Length > 0;
        return result;
    }
}

