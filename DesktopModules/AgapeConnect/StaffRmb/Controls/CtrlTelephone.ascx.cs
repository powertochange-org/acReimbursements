using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StaffRmb;

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
        ddlReceipt.Items[3].Enabled = false; //require a receipt
        ddlReceipt.SelectedValue = RmbReceiptType.Electronic.ToString();
        ScriptManager.RegisterClientScriptBlock(this, typeof(WebControl), "open_receipts", "$('.electronic_receipts_panel').show();", true);
    }

    new public string Spare2
    {
        get { return (cbExtra.Checked ? "true" : "false"); }
        set { cbExtra.Checked = value.Equals("true"); }
    }
    new public bool VAT
    {
        get { return ddlReceipt.SelectedValue.Equals(RmbReceiptType.VAT.ToString()); }
        set
        {
            if (value == true) ddlReceipt.SelectedValue = RmbReceiptType.VAT.ToString();
            else ddlReceipt.SelectedValue = RmbReceiptType.Standard.ToString() ;
        }
    }
  

}

