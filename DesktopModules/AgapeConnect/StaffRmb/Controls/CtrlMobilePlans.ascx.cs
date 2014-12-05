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
        cbExtra.Visible = true;
        cbExtraText.Visible = true;
    }
    new public void Initialize(Hashtable settings)
    {
        base.Initialize(settings);
        ddlVATReceipt.Items[3].Enabled = false; //require a receipt
        ddlVATReceipt.SelectedValue = "2";
        ScriptManager.RegisterClientScriptBlock(this, typeof(WebControl), "open_receipts", "$('.electronic_receipts_panel').show();", true);
    }

    new public string Spare2
    {
        get { return (cbExtra.Checked ? "true" : "false"); }
        set { cbExtra.Checked = value.Equals("true"); }
    }
    new public bool VAT
    {
        get { return ddlVATReceipt.SelectedValue == "0"; }
        set
        {
            if (value == true) ddlVATReceipt.SelectedValue = "0";
            else ddlVATReceipt.SelectedValue = "1";
        }
    }
  

}

