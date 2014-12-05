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
        //hide the controls we won't be using
        tbSupplier.Visible = false;
        lbSupplier.Visible = false;
        lblSupplier.Visible = false;
        ddlCurrencies.Visible = false;
        ddlVATReceipt.Visible = false;
        lblReceipt.Visible = false;
        lbReceipt.Visible = false;
        lblAmount.Visible = false;
        tbAmount.Visible = false;
        lbAmount.Visible = false;
        currencyUpdatePanel.Visible = false;
    }

    new public void Initialize(Hashtable settings)
    {
        // Set up view
        try
        {
            hfBreakfast.Value = double.Parse(settings["PDBreakfast"].ToString()).ToString();
            hfLunch.Value = double.Parse(settings["PDLunch"].ToString()).ToString();
            hfSupper.Value = double.Parse(settings["PDSupper"].ToString()).ToString();
            lblMaxBreakfast.Text = String.Format("{0:C}", double.Parse(hfBreakfast.Value));
            lblMaxLunch.Text = String.Format("{0:C}",double.Parse(hfLunch.Value));
            lblMaxSupper.Text = String.Format("{0:C}",double.Parse(hfSupper.Value));
        }
        catch
        {
            hfBreakfast.Value = "-1";
            hfLunch.Value = "-1";
            hfSupper.Value = "-1";
        }

        tbBreakfast.Enabled = cbBreakfast.Checked;
        tbLunch.Enabled = cbLunch.Checked;
        tbSupper.Enabled = cbSupper.Checked;
        tbRepeat.Visible = false;
        lblRepeat.Visible = false;
        tbRepeat.Text = "1";

        try
        {
            if (tbBreakfast.Text == "0") tbBreakfast.Text = String.Format("{0:f2}", double.Parse(settings["PDBreakfast"].ToString()));
        }
        catch
        {
            tbBreakfast.Text = "0.00";
        }
        try
        {
            if (tbLunch.Text == "0") tbLunch.Text = String.Format("{0:f2}", double.Parse(settings["PDLunch"].ToString()));
        }
        catch
        {
            tbLunch.Text = "0.00";
        }
        try
        {
            if (tbSupper.Text == "0") tbSupper.Text = String.Format("{0:f2}", double.Parse(settings["PDSupper"].ToString()));
        }
        catch
        {
            tbSupper.Text = "0.00";
        }
        ScriptManager.RegisterClientScriptBlock(cbBreakfast, typeof(CheckBox), "calculate", "updatePerDiem($('.pdbreakfast'),$('.pdbreakfast').is(':enabled'));", true);
        // Set up help strings
        hlpDesc.Text = DotNetNuke.Services.Localization.Localization.GetString("lblDesc.Help", LocalResourceFile);
        hlpDate.Text = DotNetNuke.Services.Localization.Localization.GetString("lblDate.Help", LocalResourceFile);
        hlpAmount.Text = DotNetNuke.Services.Localization.Localization.GetString("lblAmount.Help", LocalResourceFile);
        hlpProvince.Text = DotNetNuke.Services.Localization.Localization.GetString("lblProvince.Help", LocalResourceFile);
        // Hint strings
        tbDesc.Attributes.Add("Placeholder", DotNetNuke.Services.Localization.Localization.GetString("lblDesc.Hint", LocalResourceFile));
    }

    new public string Supplier
    {
        get { return ""; }
        set { }
    }
    new public bool VAT
    {
        get { return false; }
        set { }
    }
    new public bool Taxable
    {
        get { return false; }
        set { }
    }
    new public double Amount
    {
        get
        {
            double result = 0;
            try
            {
                result += (cbBreakfast.Checked ? double.Parse(tbBreakfast.Text) : 0);
                result += (cbLunch.Checked ? double.Parse(tbLunch.Text) : 0);
                result += (cbSupper.Checked ? double.Parse(tbSupper.Text) : 0);
            }
            catch
            {
                result = 0;
            }
            return result;
        }
        set { tbAmount.Text = value.ToString(); }
    }
    new public string Spare2
    {
        get
        {
            if (cbBreakfast.Checked)
            {
                try
                {
                    return double.Parse(tbBreakfast.Text).ToString();
                }
                catch
                {
                    return "0";
                }
            }
            return "0";
        }
        set
        {
            try
            {
                double amount = double.Parse(value);
                cbBreakfast.Checked = (amount > 0);
                tbBreakfast.Text = value;
            }
            catch
            {
                cbBreakfast.Checked = false;
                tbBreakfast.Text = "0";
            }
        }
    }
    new public string Spare3
    {
        get
        {
            if (cbLunch.Checked)
            {
                try
                {
                    return double.Parse(tbLunch.Text).ToString();
                }
                catch
                {
                    return "0";
                }
            }
            return "0";
        }
        set
        {
            try
            {
                double amount = double.Parse(value);
                cbLunch.Checked = (amount > 0);
                tbLunch.Text = value;
            }
            catch
            {
                cbLunch.Checked = false;
                tbLunch.Text = "0";
            }
        }
    }
    new public string Spare4
    {
        get
        {
            if (cbSupper.Checked)
            {
                try
                {
                    return double.Parse(tbSupper.Text).ToString();
                }
                catch
                {
                    return "0";
                }
            }
            return "0";
        }
        set
        {
            try
            {
                double amount = double.Parse(value);
                cbSupper.Checked = (amount > 0);
                tbSupper.Text = value;
            }
            catch
            {
                cbSupper.Checked = false;
                tbSupper.Text = "0";
            }
        }
    }
    new public string Spare5
    {
        get
        {
            string result = (cbBreakfast.Checked ? "B)" + String.Format("{0:f2}", tbBreakfast.Text) + " " : "");
            result += (cbLunch.Checked ? "L)" + String.Format("{0:f2}", tbLunch.Text) + " " : "");
            result += (cbSupper.Checked ? "S)" + String.Format("{0:f2}", tbSupper.Text) : "");
            return result;
        }
        set { }
    }
    new public bool Receipt
    {
        get { return false; }
        set { }
    }
    new public int ReceiptType
    {
        get { return 0; } //No receipts required for PerDiem exppenses
        set { }
    }
    new public int Repeat
    {
        get
        {
            try
            {
                return int.Parse(tbRepeat.Text);
            }
            catch
            {
                return 1;
            }
        }
        set
        {
            tbRepeat.Text = value.ToString();
            tbRepeat.Visible = true;
            lblRepeat.Visible = true;
        }
    }

    new public bool ValidateForm(int Userid)
    {
        bool result = true;
        if (!validate_required_fields()) { ErrorLbl2.Visible = true; return false; }
        if (!validate_description()) { result = false; }
        else if (!validate_date()) { result = false; }
        ErrorLbl2.Text = ErrorLbl.Text;
        ErrorLbl.Text = "";
        ErrorLbl.Visible = false;
        if (!result)
        {
            ErrorLbl2.Visible = true;
            return false;
        }
        if (!validate_selections()) return false;
        if (!validate_amounts()) return false;
        if (!validate_repeat()) return false;
        ErrorLbl2.Text = "";
        ErrorLbl2.Visible = false;
        return true;
    }

    new public bool validate_required_fields()
    {
        tbDesc.CssClass = tbDesc.CssClass.Replace("missing", "");
        if (tbDesc.Text.Length == 0)
        {
            ErrorLbl2.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.RequiredField", LocalResourceFile);
            tbDesc.CssClass = tbDesc.CssClass + " missing";
            return false;
        }
        return true;
    }
    new private bool validate_selections()
    {
        if (!(cbBreakfast.Checked || cbLunch.Checked || cbSupper.Checked))
        {
            ErrorLbl2.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.Selections", LocalResourceFile);
            ErrorLbl2.Visible = true;
            return false;
        }
        return true;
    }

    private bool validate_amounts()
    {
        try
        {
            if ((cbBreakfast.Checked && double.Parse(tbBreakfast.Text) <= 0) || (cbLunch.Checked && double.Parse(tbLunch.Text) <= 0) || (cbSupper.Checked && double.Parse(tbSupper.Text) <= 0))
            {
                ErrorLbl2.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.Amount", LocalResourceFile);
                ErrorLbl2.Visible = true;
                return false;
            }
            if (double.Parse(tbBreakfast.Text) > double.Parse(hfBreakfast.Value) || double.Parse(tbLunch.Text) > double.Parse(hfLunch.Value) || double.Parse(tbSupper.Text) > double.Parse(hfSupper.Value))
            {
                ErrorLbl2.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.MaxAmount", LocalResourceFile);
                ErrorLbl2.Visible = true;
                return false;
            }
        }
        catch
        {
            ErrorLbl2.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.Amount", LocalResourceFile);
            ErrorLbl2.Visible = true;
            return false;
        }

        return true;
    }

    private bool validate_repeat()
    {
        try
        {
            int repeat = int.Parse(tbRepeat.Text);
            if (repeat < 1 || repeat > 14)
            {
                ErrorLbl2.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.Repeat", LocalResourceFile);
                ErrorLbl2.Visible = true;
                return false;
            }
            if (theDate.AddDays(repeat - 1) > DateTime.Today)
            {
                ErrorLbl2.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.RepeatDate", LocalResourceFile);
                ErrorLbl2.Visible = true;
                return false;
            }
        }
        catch
        {
            ErrorLbl2.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.Repeat", LocalResourceFile);
            ErrorLbl2.Visible = true;
            return false;
        }
        return true;
    }
}

