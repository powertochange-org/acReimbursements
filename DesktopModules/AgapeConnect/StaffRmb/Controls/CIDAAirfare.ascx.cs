using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class ControlBase : StaffRmb.StaffRmbControl {
    new protected void Page_Init(object sender, EventArgs e)
    {
        base.Page_Init(sender, e);
        show_mileage_fields();
    }

    private void show_mileage_fields()
    {
        lblOrigin.Visible = true;
        lbOrigin.Visible = true;
        tbOrigin.Visible = true;
        tbDestination.Visible = true;
    }

    new public string Spare4
    {
        get { return tbOrigin.Text; }
        set { tbOrigin.Text = value; }
    }
    new public string Spare5
    {
        get { return tbDestination.Text; }
        set { tbDestination.Text = value; }
    }

    new public bool ValidateForm(int UserId)
    {
        if (!validate_required_fields()) return false;
        if (!validate_description()) return false;
        if (!validate_date()) return false;
        if (!validate_amount()) return false;
        if (!validate_receipt()) return false;
        ErrorLbl.Text = "";
        return true;
    }
    new public bool validate_required_fields()
    {
        TextBox[] required_fields = new TextBox[] { tbSupplier, tbOrigin, tbDestination, tbDesc, tbAmount };
        bool result = true;
        foreach (TextBox control in required_fields)
        {
            control.CssClass = control.CssClass.Replace("missing", "");
            if (((TextBox)control).Text.Length == 0)
            {
                result = false;
                control.CssClass = control.CssClass + " missing";
            }
        }
        if (!result) ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.RequiredField", LocalResourceFile);
        return result;
    }

}

