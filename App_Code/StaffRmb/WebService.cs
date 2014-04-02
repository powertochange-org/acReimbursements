using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Newtonsoft.Json;
using StaffRmb;

/// <summary>
/// Provides a sublist of account numbers
/// </summary>

[WebService(Namespace = "powertochange.org")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[System.Web.Script.Services.ScriptService]
public class WebService : System.Web.Services.WebService {

    public WebService () {

        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }

    [WebMethod]
    public void GetAccountNumbers(string term) {
        var result = new StaffRmbDataContext().AP_StaffBroker_CostCenters
            .Select(s => new { label = s.CostCentreCode + ":" + s.CostCentreName, value = s.CostCentreCode })
            .Where(w => w.label.Contains(term))
            .OrderBy(o => o.value);
        string json = JsonConvert.SerializeObject(result);
        HttpContext.Current.Response.ContentType = "application/json";
        HttpContext.Current.Response.Write(json);
    }
    
}
