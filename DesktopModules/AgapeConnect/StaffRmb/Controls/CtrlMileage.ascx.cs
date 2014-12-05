using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StaffRmb;

public partial class ControlBase : StaffRmbControl {
    new protected void Page_Init(object sender, EventArgs e)
    {
        base.Page_Init(sender, e);
        show_mileage_fields();
        hide_unwanted_fields();
    }

    new public void Initialize(Hashtable settings)
    {
        base.Initialize(settings);
        if (ddlDistUnits.Items.Count == 0) {
            for (int i=1; i<=4; i++) {
                string valuestring = settings["MRate" + i.ToString()].ToString();
                double value = 0;
                try {
                    value = double.Parse(valuestring);
                } catch {}
                if (value>0) {
                    ddlDistUnits.Items.Add(new ListItem(settings["MRate" + i.ToString() + "Name"].ToString() + " (" +  String.Format("{0:C}",value) + ")", valuestring));
                }
            }
        }
    }

    new public string Supplier
    {
        get { return ""; }
        set { }
    }
    new public double Amount
    {
        get
        {
            double value = 0;
            try
            {
                value = Math.Round(double.Parse(tbAmount.Text) * double.Parse(ddlDistUnits.SelectedValue));
            }
            catch { }
            return value;
        }
        set { }
    }
    new public string Spare3
    {
        get { return ddlDistUnits.SelectedIndex.ToString(); }
        set
        {
            ddlDistUnits.ClearSelection();
            try
            {
                ddlDistUnits.SelectedIndex = int.Parse(value);
            }
            catch { }
        }
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
    public int Mileage
    {
        get {
            try
            {
                return int.Parse(tbAmount.Text);
            }
            catch { }
            return 0;
        }
        set { tbAmount.Text = value.ToString(); }
    }
    public decimal MileageRate
    {
        get
        {
            try { return decimal.Parse(ddlDistUnits.SelectedValue); }
            catch { }
        return -1;}
        set {}
    }
    new public bool Receipt
    {
        get { return false; }
        set { }
    }
    new public int ReceiptType
    {
        get { return 0; } //no receipt required for mileage expenses
        set { }
    }
    new public bool VAT
    {
        get { return false; }
        set { }
    }

    public bool ValidateForm(int UserId)
    {
        if (!validate_required_fields()) return false;
        if (!validate_description()) return false;
        if (!validate_date()) return false;
        if (!validate_amount()) return false;
        ErrorLbl.Text = "";
        return true;
    }
    new public bool validate_required_fields()
    {
        TextBox[] required_fields = new TextBox[] { tbOrigin, tbDestination, tbDesc, tbAmount };
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

    private void show_mileage_fields()
    {
        lblOrigin.Visible = true;
        lbOrigin.Visible = true;
        tbOrigin.Visible = true;
        tbDestination.Visible = true;
        ddlDistUnits.Visible = true;
    }

    private void hide_unwanted_fields()
    {
        lblSupplier.Visible = false;
        tbSupplier.Visible = false;
        lbSupplier.Visible = false;
        currencyUpdatePanel.Visible = false;
        lblReceipt.Visible = false;
        ddlVATReceipt.Visible = false;
        lbReceipt.Visible = false;
    }

}

