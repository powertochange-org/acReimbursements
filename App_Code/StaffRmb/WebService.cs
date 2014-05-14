﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using Newtonsoft.Json;
using StaffRmb;
using System.Threading.Tasks;

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
    
    [WebMethod]
    [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
    public string[] GetVendorIds(string company)
    {
        List<string> result = new List<string>();
        byte[] postData = System.Text.Encoding.UTF8.GetBytes("company="+company);
        //send request
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://gpapp/gpimport/webservice/GetVendors");
        request.KeepAlive = false; //This and the following line are required to prevent "connection closed" problems
        request.ProtocolVersion = HttpVersion.Version10;
        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = postData.Length;
        try
        {
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(postData, 0, postData.Length);
            }
            var response = (HttpWebResponse)request.GetResponse();
            if (response != null)
            {
                var reader = new System.IO.StreamReader(response.GetResponseStream());
                dynamic vendors = JsonConvert.DeserializeObject(reader.ReadToEnd());
                foreach (var vendor in vendors)
                {
                    result.Add("[" + vendor.VendorID + "] " + vendor.VendorName);
                }
            }
        }
        catch (WebException e)
        {
        }
        return result.ToArray();
    }
    
}
