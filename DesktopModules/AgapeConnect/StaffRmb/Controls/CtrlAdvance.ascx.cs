using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class ControlBase : StaffRmb.StaffRmbControl {
    new protected void Page_Init(object sender, EventArgs e)
    {
        base.Page_Init(sender, e);
        hide_unwanted_fields();
    }

    new protected void Page_Load(object sender, EventArgs e) {}

    public void Initialize(Hashtable settings)
    {
        hlpDesc.Text = DotNetNuke.Services.Localization.Localization.GetString("lblDesc.Help", LocalResourceFile);
        hlpDate.Text = DotNetNuke.Services.Localization.Localization.GetString("lblDate.Help", LocalResourceFile);
        hlpAmount.Text = DotNetNuke.Services.Localization.Localization.GetString("lblAmount.Help", LocalResourceFile);
        // Hint strings
        tbDesc.Attributes.Add("Placeholder", DotNetNuke.Services.Localization.Localization.GetString("lblDesc.Hint", LocalResourceFile));
    }

    #region Properties
    public string Supplier
    {
        get { return ""; }
        set { }
    }
    public bool VAT
    {
        get { return false; }
        set {}
    }
    public int ReceiptType
    {
        get { return StaffRmb.RmbReceiptType.No_Receipt; }
        set {}
    }
    public bool Taxable
    {
        get { return false;  }
        set {}
    }
    public string Spare1
    {
        get { return ""; }
        set {}
    }
    public bool Receipt
    {
        get { return false; }
        set {}
    }
    #endregion

    #region Validation
    public bool ValidateForm(int UserId)
    {
        if (!validate_required_fields()) return false;
        if (!validate_description()) return false;
        if (!validate_date()) return false;
        if (!validate_amount()) return false;
        ErrorLbl.Text = "";
        return true;
    }
    public bool validate_required_fields()
    {
        TextBox[] required_fields = new TextBox[] { tbDesc, tbAmount };
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
    public bool validate_date()
    {
        try
        {
            DateTime date = DateTime.Parse(dtDate.Text);
            if (date <= DateTime.Today)
            {
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.FutureDate", LocalResourceFile);
                return false;
            }
            if (date > DateTime.Today.AddDays(365))
            {
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.DateTooFar", LocalResourceFile);
                return false;
            }
        }
        catch
        {
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.Date", LocalResourceFile);
            return false;
        }
        return true;
    }
    public bool validate_amount()
    {
        try
        {
            Double amount = Double.Parse(tbAmount.Text);
            if (tbCADAmount.Text.Equals(String.Empty))
            {
                tbCADAmount.Text = CADValue.ToString("n2", new System.Globalization.CultureInfo("en-US"));
            }
            //if (amount <= 0)
            //{
            //    ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Warn.NegativeAmount", LocalResourceFile);
            //    return false;
            //}
            if (CADValue > 10000)
            {
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.LargeAmount", LocalResourceFile);
                return false;
            }
        }
        catch
        {
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.Amount", LocalResourceFile);
            return false;
        }
        return true;
    }
    #endregion
    private void hide_unwanted_fields()
    {
        lblSupplier.Visible = false;
        tbSupplier.Visible = false;
        lbSupplier.Visible = false;
        currencyUpdatePanel.Attributes.Add("style", "display:none");
        lblProvince.Visible = false;
        lbProvince.Visible = false;
        ddlProvince.Visible = false;
        lblReceipt.Visible = false;
        ddlReceipt.Visible = false;
        lbReceipt.Visible = false;
    }

}

