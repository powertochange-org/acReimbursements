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
        ListItem noReceiptItem = ddlReceipt.Items.FindByValue(RmbReceiptType.No_Receipt.ToString());
        if (noReceiptItem != null) noReceiptItem.Enabled = false; //require a receipt
    }

    new public string Spare2
    {
        get { return (cbExtra.Checked ? "true" : "false"); }
        set {
            if (value == null)
            {
                cbExtra.Checked = false;
            }
            else
            {
                cbExtra.Checked = value.Equals("true");
            }
        }
    }
  

}

