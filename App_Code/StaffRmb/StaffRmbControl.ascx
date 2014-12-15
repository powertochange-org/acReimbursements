<%@ Control Language="C#" ClassName="StaffRmb.StaffRmbControl" Inherits="DotNetNuke.Entities.Modules.PortalModuleBase"%>

<asp:HiddenField ID="hfNoReceiptLimit" runat="server" Value="0" />
<asp:HiddenField ID="hfCADValue" runat="server" Value="" />
<asp:HiddenField ID="hfElecReceiptAttached" runat="server" value="false"/>
    
<div class="Agape_SubTitle"> 
    <asp:Label id="lblExplanation" runat="server" Font-Italic="true" ForeColor="Gray" CssClass="explanation" resourcekey="Explanation"></asp:Label>
</div>
<div id="prefix" >
    <br />
</div>
<table   style="font-size:9pt; ">
    <tr>
        <td><b><asp:label ID="lblOrigin" runat="server" ControlName="tbOrigin" ResourceKey="lblOrigin" visible="false"/></b>
            <asp:LinkButton id="lbOrigin" TabIndex="-1" runat="server" CausesValidation="False" EnableViewState="False" CssClass="dnnFormHelp"  style="position:relative" Visible="false"/>
            <asp:Panel runat="server" CssClass="dnnTooltip">
                <div class="dnnFormHelpContent dnnClear">
                    <asp:Label ID="hlpOrigin" runat="server" EnableViewState="False" class="dnnHelpText" />
                    <a href="#" class="pinHelp"></a>
               </div>   
            </asp:Panel>
        </td>
        <td><asp:TextBox ID="tbOrigin" runat="server" CssClass="required" AutoCompleteType="None" Visible="false"/>
            <asp:TextBox ID="tbDestination" runat="server" CssClass="required" AutoCompleteType="None" visible="false" style="border-left:10px;"/></td>
    </tr>
    <tr>
        <td ><b><asp:label ID="lblSupplier" runat="server" ControlName="tbSupplier" ResourceKey="lblSupplier" /></b>
            <asp:LinkButton id="lbSupplier" TabIndex="-1" runat="server" CausesValidation="False" EnableViewState="False" CssClass="dnnFormHelp"  style="position:relative"/>
            <asp:Panel runat="server" CssClass="dnnTooltip">
                <div class="dnnFormHelpContent dnnClear">
                    <asp:Label ID="hlpSupplier" runat="server" EnableViewState="False" class="dnnHelpText" />
                    <a href="#" class="pinHelp"></a>
               </div>   
            </asp:Panel>
        </td>
        <td colspan="2"><asp:TextBox ID="tbSupplier" runat="server" Width="278px" CssClass="required" AutoCompleteType="None" ></asp:TextBox></td>
    </tr>
    <tr>
        <td style="width:200px"><b><asp:label id="lblDesc" runat="server" controlname="tbDesc" ResourceKey="lblDesc"  /></b>
            <asp:LinkButton id="lbDesc" TabIndex="-1" runat="server" CausesValidation="False" EnableViewState="False" CssClass="dnnFormHelp"  style="position:relative"/>
            <asp:Panel runat="server" CssClass="dnnTooltip">
                <div class="dnnFormHelpContent dnnClear">
                    <asp:Label ID="hlpDesc" runat="server" EnableViewState="False" class="dnnHelpText" />
                    <a href="#" class="pinHelp"></a>
               </div>   
            </asp:Panel>
        </td>
        <td colspan="2"><asp:TextBox ID="tbDesc" runat="server" maxlength="27" Width="15em" CssClass="required" AutoCompleteType="None" ></asp:TextBox></td>
    </tr>
    <tr>
        <td><b><asp:label id="lblForWhom" runat="server" controlname="tbForWhom" ResourceKey="lblForWhom"  Visible="false" /></b>
            <asp:LinkButton id="lbForWhom" TabIndex="-1" runat="server" CausesValidation="False" EnableViewState="False" CssClass="dnnFormHelp"  style="position:relative" Visible="false"/>
            <asp:Panel runat="server" CssClass="dnnTooltip">
                <div class="dnnFormHelpContent dnnClear">
                    <asp:Label ID="hlpForWhom" runat="server" EnableViewState="False" class="dnnHelpText" />
                    <a href="#" class="pinHelp"></a>
               </div>   
            </asp:Panel>
        </td>
        <td colspan="2"><asp:TextBox ID="tbForWhom" runat="server" CssClass="required" AutoCompleteType="None" Visible="false"></asp:TextBox></td>
    </tr>
    <tr>
        <td><b><asp:label runat="server" id="lblDate" controlname="dtDate" ResourceKey="lblDate"  /></b>
            <asp:LinkButton id="lbDate" TabIndex="-1" runat="server" CausesValidation="False" EnableViewState="False" CssClass="dnnFormHelp"  style="position:relative"/>
            <asp:Panel runat="server" CssClass="dnnTooltip">
                <div class="dnnFormHelpContent dnnClear">
                    <asp:Label ID="hlpDate" runat="server" EnableViewState="False" class="dnnHelpText" />
                    <a href="#" class="pinHelp"></a>
               </div>   
            </asp:Panel>
        </td>
        <td  colspan="2"><asp:TextBox ID="dtDate" runat="server" Width="90px" class="datepicker" onChange="check_expense_date();"></asp:TextBox><span id="olddatetext"></span></td>
    </tr>
    <tr>
        <td><b><asp:label runat="server" id="lblAmount" controlname="tbAmount" ResourceKey="lblAmount"  /></b>
            <asp:LinkButton ID="lbAmount" TabIndex="-1" runat="server" CausesValidation="False" EnableViewState="False" CssClass="dnnFormHelp" style="position:relative"/>
            <asp:Panel runat="server" CssClass="dnnTooltip">
                <div class="dnnFormHelpContent dnnClear">
                    <asp:Label ID="hlpAmount" runat="server" EnableViewState="False" class="dnnHelpText" />
                    <a href="#" class="pinHelp"></a>
               </div>   
            </asp:Panel>
        </td>
        <td>
            <table style="font-size:9pt">
                <tr>
                    <td><asp:TextBox ID="tbAmount" runat="server" Width="90px" class="required numeric rmbAmount" onFocus="select();"></asp:TextBox></td>
                    <td colspan="2">
                        <asp:UpdatePanel ID="currencyUpdatePanel" runat="server">
                            <ContentTemplate>
                                <div id="dCurrency" class="divCur" >
                                    <table style="font-size:9pt"><tr>
                                    <td style="margin-left:30px">
                                        <asp:DropDownList ID="ddlCurrencies" runat="server" CssClass="ddlCur" AutoPostBack="true" OnSelectedIndexChanged="Currency_Change" OnChange="currencyChange(this.value);">
                                            <asp:ListItem Value="ALL">Albanian Lek</asp:ListItem>
                                            <asp:ListItem Value="DZD">Algerian Dinar</asp:ListItem>
                                            <asp:ListItem Value="ARS">Argentine Peso</asp:ListItem>
                                            <asp:ListItem Value="AWG">Aruba Florin</asp:ListItem>
                                            <asp:ListItem Value="AUD">Australian Dollar</asp:ListItem>
                                            <asp:ListItem Value="BSD">Bahamian Dollar</asp:ListItem>
                                            <asp:ListItem Value="BHD">Bahraini Dinar</asp:ListItem>
                                            <asp:ListItem Value="BDT">Bangladesh Taka</asp:ListItem>
                                            <asp:ListItem Value="BBD">Barbados Dollar</asp:ListItem>
                                            <asp:ListItem Value="BYR">Belarus Ruble</asp:ListItem>
                                            <asp:ListItem Value="BZD">Belize Dollar</asp:ListItem>
                                            <asp:ListItem Value="BMD">Bermuda Dollar</asp:ListItem>
                                            <asp:ListItem Value="BTN">Bhutan Ngultrum</asp:ListItem>
                                            <asp:ListItem Value="BOB">Bolivian Boliviano</asp:ListItem>
                                            <asp:ListItem Value="BWP">Botswana Pula</asp:ListItem>
                                            <asp:ListItem Value="BRL">Brazilian Real</asp:ListItem>
                                            <asp:ListItem Value="GBP">British Pound</asp:ListItem>
                                            <asp:ListItem Value="BND">Brunei Dollar</asp:ListItem>
                                            <asp:ListItem Value="BGN">Bulgarian Lev</asp:ListItem>
                                            <asp:ListItem Value="BIF">Burundi Franc</asp:ListItem>
                                            <asp:ListItem Value="KHR">Cambodia Riel</asp:ListItem>
                                            <asp:ListItem Value="CAD" Selected="True">Canadian Dollar</asp:ListItem>
                                            <asp:ListItem Value="CVE">Cape Verde Escudo</asp:ListItem>
                                            <asp:ListItem Value="KYD">Cayman Islands Dollar</asp:ListItem>
                                            <asp:ListItem Value="XOF">CFA Franc (BCEAO)</asp:ListItem>
                                            <asp:ListItem Value="XAF">CFA Franc (BEAC)</asp:ListItem>
                                            <asp:ListItem Value="CLP">Chilean Peso</asp:ListItem>
                                            <asp:ListItem Value="CNY">Chinese Yuan</asp:ListItem>
                                            <asp:ListItem Value="COP">Colombian Peso</asp:ListItem>
                                            <asp:ListItem Value="KMF">Comoros Franc</asp:ListItem>
                                            <asp:ListItem Value="CRC">Costa Rica Colon</asp:ListItem>
                                            <asp:ListItem Value="HRK">Croatian Kuna</asp:ListItem>
                                            <asp:ListItem Value="CUP">Cuban Peso</asp:ListItem>
                                            <asp:ListItem Value="CZK">Czech Koruna</asp:ListItem>
                                            <asp:ListItem Value="DKK">Danish Krone</asp:ListItem>
                                            <asp:ListItem Value="DJF">Dijibouti Franc</asp:ListItem>
                                            <asp:ListItem Value="DOP">Dominican Peso</asp:ListItem>
                                            <asp:ListItem Value="XCD">East Caribbean Dollar</asp:ListItem>
                                            <asp:ListItem Value="ECS">Ecuador Sucre</asp:ListItem>
                                            <asp:ListItem Value="EGP">Egyptian Pound</asp:ListItem>
                                            <asp:ListItem Value="SVC">El Salvador Colon</asp:ListItem>
                                            <asp:ListItem Value="ERN">Eritrea Nakfa</asp:ListItem>
                                            <asp:ListItem Value="EEK">Estonian Kroon</asp:ListItem>
                                            <asp:ListItem Value="ETB">Ethiopian Birr</asp:ListItem>
                                            <asp:ListItem Value="EUR">Euro</asp:ListItem>
                                            <asp:ListItem Value="FKP">Falkland Islands Pound</asp:ListItem>
                                            <asp:ListItem Value="FJD">Fiji Dollar</asp:ListItem>
                                            <asp:ListItem Value="GMD">Gambian Dalasi</asp:ListItem>
                                            <asp:ListItem Value="GHC">Ghanian Cedi</asp:ListItem>
                                            <asp:ListItem Value="GIP">Gibraltar Pound</asp:ListItem>
                                            <asp:ListItem Value="GTQ">Guatemala Quetzal</asp:ListItem>
                                            <asp:ListItem Value="GNF">Guinea Franc</asp:ListItem>
                                            <asp:ListItem Value="GYD">Guyana Dollar</asp:ListItem>
                                            <asp:ListItem Value="HTG">Haiti Gourde</asp:ListItem>
                                            <asp:ListItem Value="HNL">Honduras Lempira</asp:ListItem>
                                            <asp:ListItem Value="HKD">Hong Kong Dollar</asp:ListItem>
                                            <asp:ListItem Value="HUF">Hungarian Forint</asp:ListItem>
                                            <asp:ListItem Value="ISK">Iceland Krona</asp:ListItem>
                                            <asp:ListItem Value="INR">Indian Rupee</asp:ListItem>
                                            <asp:ListItem Value="IDR">Indonesian Rupiah</asp:ListItem>
                                            <asp:ListItem Value="IRR">Iran Rial</asp:ListItem>
                                            <asp:ListItem Value="IQD">Iraqi Dinar</asp:ListItem>
                                            <asp:ListItem Value="ILS">Israeli Shekel</asp:ListItem>
                                            <asp:ListItem Value="JMD">Jamaican Dollar</asp:ListItem>
                                            <asp:ListItem Value="JPY">Japanese Yen</asp:ListItem>
                                            <asp:ListItem Value="JOD">Jordanian Dinar</asp:ListItem>
                                            <asp:ListItem Value="KZT">Kazakhstan Tenge</asp:ListItem>
                                            <asp:ListItem Value="KES">Kenyan Shilling</asp:ListItem>
                                            <asp:ListItem Value="KWD">Kuwaiti Dinar</asp:ListItem>
                                            <asp:ListItem Value="LAK">Lao Kip</asp:ListItem>
                                            <asp:ListItem Value="LVL">Latvian Lat</asp:ListItem>
                                            <asp:ListItem Value="LBP">Lebanese Pound</asp:ListItem>
                                            <asp:ListItem Value="LSL">Lesotho Loti</asp:ListItem>
                                            <asp:ListItem Value="LRD">Liberian Dollar</asp:ListItem>
                                            <asp:ListItem Value="LYD">Libyan Dinar</asp:ListItem>
                                            <asp:ListItem Value="LTL">Lithuanian Lita</asp:ListItem>
                                            <asp:ListItem Value="MOP">Macau Pataca</asp:ListItem>
                                            <asp:ListItem Value="MKD">Macedonian Denar</asp:ListItem>
                                            <asp:ListItem Value="MWK">Malawi Kwacha</asp:ListItem>
                                            <asp:ListItem Value="MYR">Malaysian Ringgit</asp:ListItem>
                                            <asp:ListItem Value="MVR">Maldives Rufiyaa</asp:ListItem>
                                            <asp:ListItem Value="MTL">Maltese Lira</asp:ListItem>
                                            <asp:ListItem Value="MRO">Mauritania Ougulya</asp:ListItem>
                                            <asp:ListItem Value="MUR">Mauritius Rupee</asp:ListItem>
                                            <asp:ListItem Value="MXN">Mexican Peso</asp:ListItem>
                                            <asp:ListItem Value="MDL">Moldovan Leu</asp:ListItem>
                                            <asp:ListItem Value="MNT">Mongolian Tugrik</asp:ListItem>
                                            <asp:ListItem Value="MAD">Moroccan Dirham</asp:ListItem>
                                            <asp:ListItem Value="MMK">Myanmar Kyat</asp:ListItem>
                                            <asp:ListItem Value="NAD">Namibian Dollar</asp:ListItem>
                                            <asp:ListItem Value="NPR">Nepalese Rupee</asp:ListItem>
                                            <asp:ListItem Value="ANG">Neth Antilles Guilder</asp:ListItem>
                                            <asp:ListItem Value="NZD">New Zealand Dollar</asp:ListItem>
                                            <asp:ListItem Value="NIO">Nicaragua Cordoba</asp:ListItem>
                                            <asp:ListItem Value="NGN">Nigerian Naira</asp:ListItem>
                                            <asp:ListItem Value="KPW">North Korean Won</asp:ListItem>
                                            <asp:ListItem Value="NOK">Norwegian Krone</asp:ListItem>
                                            <asp:ListItem Value="OMR">Omani Rial</asp:ListItem>
                                            <asp:ListItem Value="PKR">Pakistani Rupee</asp:ListItem>
                                            <asp:ListItem Value="PAB">Panama Balboa</asp:ListItem>
                                            <asp:ListItem Value="PGK">Papua New Guinea Kina</asp:ListItem>
                                            <asp:ListItem Value="PYG">Paraguayan Guarani</asp:ListItem>
                                            <asp:ListItem Value="PEN">Peruvian Nuevo Sol</asp:ListItem>
                                            <asp:ListItem Value="PHP">Philippine Peso</asp:ListItem>
                                            <asp:ListItem Value="PLN">Polish Zloty</asp:ListItem>
                                            <asp:ListItem Value="QAR">Qatar Rial</asp:ListItem>
                                            <asp:ListItem Value="RON">Romanian New Leu</asp:ListItem>
                                            <asp:ListItem Value="RUB">Russian Rouble</asp:ListItem>
                                            <asp:ListItem Value="RWF">Rwanda Franc</asp:ListItem>
                                            <asp:ListItem Value="WST">Samoa Tala</asp:ListItem>
                                            <asp:ListItem Value="STD">Sao Tome Dobra</asp:ListItem>
                                            <asp:ListItem Value="SAR">Saudi Arabian Riyal</asp:ListItem>
                                            <asp:ListItem Value="SCR">Seychelles Rupee</asp:ListItem>
                                            <asp:ListItem Value="SLL">Sierra Leone Leone</asp:ListItem>
                                            <asp:ListItem Value="SGD">Singapore Dollar</asp:ListItem>
                                            <asp:ListItem Value="SKK">Slovak Koruna</asp:ListItem>
                                            <asp:ListItem Value="SIT">Slovenian Tolar</asp:ListItem>
                                            <asp:ListItem Value="SBD">Solomon Islands Dollar</asp:ListItem>
                                            <asp:ListItem Value="SOS">Somali Shilling</asp:ListItem>
                                            <asp:ListItem Value="ZAR">South African Rand</asp:ListItem>
                                            <asp:ListItem Value="KRW">South Korean Won</asp:ListItem>
                                            <asp:ListItem Value="LKR">Sri Lanka Rupee</asp:ListItem>
                                            <asp:ListItem Value="SHP">St Helena Pound</asp:ListItem>
                                            <asp:ListItem Value="SDG">Sudanese Pound</asp:ListItem>
                                            <asp:ListItem Value="SZL">Swaziland Lilageni</asp:ListItem>
                                            <asp:ListItem Value="SEK">Swedish Krona</asp:ListItem>
                                            <asp:ListItem Value="CHF">Swiss Franc</asp:ListItem>
                                            <asp:ListItem Value="SYP">Syrian Pound</asp:ListItem>
                                            <asp:ListItem Value="TWD">Taiwan Dollar</asp:ListItem>
                                            <asp:ListItem Value="TZS">Tanzanian Shilling</asp:ListItem>
                                            <asp:ListItem Value="THB">Thai Baht</asp:ListItem>
                                            <asp:ListItem Value="TOP">Tonga Pa'ang</asp:ListItem>
                                            <asp:ListItem Value="TTD">Trinidad Tobago Dollar</asp:ListItem>
                                            <asp:ListItem Value="TND">Tunisian Dinar</asp:ListItem>
                                            <asp:ListItem Value="TRY">Turkish Lira</asp:ListItem>
                                            <asp:ListItem Value="AED">UAE Dirham</asp:ListItem>
                                            <asp:ListItem Value="UGX">Ugandan Shilling</asp:ListItem>
                                            <asp:ListItem Value="UAH">Ukraine Hryvnia</asp:ListItem>
                                            <asp:ListItem Value="USD">United States Dollar</asp:ListItem>
                                            <asp:ListItem Value="UYU">Uruguayan New Peso</asp:ListItem>
                                            <asp:ListItem Value="VUV">Vanuatu Vatu</asp:ListItem>
                                            <asp:ListItem Value="VEF">Venezuelan Bolivar Fuerte</asp:ListItem>
                                            <asp:ListItem Value="VND">Vietnam Dong</asp:ListItem>
                                            <asp:ListItem Value="YER">Yemen Riyal</asp:ListItem>
                                            <asp:ListItem Value="ZMK">Zambian Kwacha</asp:ListItem>
                                            <asp:ListItem Value="ZWD">Zimbabwe Dollar</asp:ListItem>
                                        </asp:DropDownList> <br />
                                    </td>
                                    <td style="text-align:right;">
                                        <table class="curDetails" style="display:inline-table; margin-left:30px;" ><tr>
                                            <th colspan="2"><asp:Label runat="server" ResourceKey="lblExchangeHeader" /></th></tr><tr>
                                            <td style="text-align:center">
                                                <b><label for="tbExchangeRate"><%=DotNetNuke.Services.Localization.Localization.GetString("lblExchangeRate.Text", LocalResourceFile)%></label></b><br />
                                                <asp:textbox id="tbExchangeRate" runat="server" cssClass="exchangeRate" style="width:80px" onfocus="select();" Text="1.0000"/>
                                            </td>
                                            <td style="text-align:center;margin-left:20px">
                                                <b><asp:Label runat="server" ResourceKey="lblEquivalentCAD"/></b><br />
                                                <asp:TextBox ID="tbCADAmount" runat="server" cssclass="equivalentCAD" style="width:80px;" onfocus="select();"/>
                                            </td>
                                            <tr><td colspan="2" class="footer"><asp:label runat="server" ResourceKey="lblExchangeFooter"></asp:label></td></tr>
                                        </tr></table>
                                    </td>
                                    </tr></table>
                                </div>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                        <asp:DropDownList ID="ddlDistUnits" runat="server" Visible="false"></asp:DropDownList>
                    </td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td><b><asp:Label ID="lblExtra" runat="server" controlName="cbExtra" resourceKey="lblExtra" visible="false"/></b></td>
        <td colspan="2"><asp:CheckBox ID="cbExtra" runat="server" Visible="false" /><asp:Label id="cbExtraText" runat="server" resourceKey="cbExtra" visible="false"/></td>
    </tr>
    <tr><td><b><asp:Label ID="lblProvince" runat="server" controlname="ddlProvince" ResourceKey="lblProvince" /></b>
            <asp:LinkButton ID="lbProvince" TabIndex="-1" runat="server" CausesValidation="False" EnableViewState="False" CssClass="dnnFormHelp"  style="position:relative"/>
            <asp:Panel runat="server" CssClass="dnnTooltip">
                <div class="dnnFormHelpContent dnnClear">
                    <asp:Label ID="hlpProvince" runat="server" EnableViewState="False" class="dnnHelpText" />
                    <a href="#" class="pinHelp"></a>
               </div>   
            </asp:Panel>
        </td>
        <td colspan="2"><asp:DropDownList ID="ddlProvince" CssClass="ddlProvince" runat="server">
                <asp:ListItem Text="British Columbia" Value="BC" />
                <asp:ListItem Text="Alberta" Value="AB" />
                <asp:ListItem Text="Saskatchewan" Value="SK" />
                <asp:ListItem Text="Manitoba" Value="MB" />
                <asp:ListItem Text="Ontario" Value="ON" />
                <asp:ListItem Text="Quebec" Value="QC" />
                <asp:ListItem Text="Newfoundland" Value="NL" />
                <asp:ListItem Text="Nova Scotia" Value="NS" />
                <asp:ListItem Text="New Brunswick" Value="NB" />
                <asp:ListItem Text="Prince Edward Is." Value="PE" />
                <asp:ListItem Text="Yukon" Value="YT" />
                <asp:ListItem Text="Nunavut" Value="NV" />
                <asp:ListItem Text="Northwest Terr." Value="NT" />
                <asp:ListItem Text="Outside Canada" Value="--" />
             </asp:DropDownList></td>
    </tr>
    <tr  id="ReceiptLine" runat="server">
        <td><b><asp:label id="lblReceipt"  runat="server" controlname="ddlReceipt"  ResourceKey="lblReceipt" /></b>
            <asp:LinkButton id="lbReceipt" TabIndex="-1" runat="server" CausesValidation="False" EnableViewState="False" CssClass="dnnFormHelp"  style="position:relative"/>
            <asp:Panel runat="server" CssClass="dnnTooltip">
                <div class="dnnFormHelpContent dnnClear">
                    <asp:Label ID="hlpReceipt" runat="server" EnableViewState="False" class="dnnHelpText" />
                    <a href="#" class="pinHelp"></a>
               </div>   
            </asp:Panel>
        </td>
        <td colspan="2">
            <asp:DropDownList ID="ddlReceipt" runat="server"  CssClass="ddlReceipt required">
            </asp:DropDownList>
        </td>
    </tr>
</table>
 <asp:Label ID="ErrorLbl" runat="server" Font-Size="9pt" ForeColor="Red" />

<script runat="server">

    string accounting_currency;

    protected void Page_Init(object sender, EventArgs e)
    {
        // Get the correct LocalResourceFile
        string FileName = System.IO.Path.GetFileNameWithoutExtension(this.AppRelativeVirtualPath);
        if (this.ID != null) {
            //this will fix it when its placed as a ChildUserControl 
            this.LocalResourceFile = this.LocalResourceFile.Replace(this.ID, FileName);
        }
        else
        {
            this.LocalResourceFile = this.LocalResourceFile + FileName + ".ascx.resx";
            string Locale = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
            System.IO.DirectoryInfo AppLocRes = new System.IO.DirectoryInfo(this.LocalResourceFile.Replace(FileName + ".ascx.resx", ""));
            if (Locale == PortalSettings.CultureCode) {
                //look for portal varient
                if (AppLocRes.GetFiles(FileName + ".ascx.Portal-" + PortalId + ".resx").Count() > 0) {
                    this.LocalResourceFile = this.LocalResourceFile.Replace("resx", "Portal-" + PortalId + ".resx");
                }
            } else {
                if (AppLocRes.GetFiles(FileName + ".ascx." + Locale + ".Portal-" + PortalId + ".resx").Count() > 0) {
                    //lookFor a CulturePortalVarient
                    this.LocalResourceFile = this.LocalResourceFile.Replace("resx", Locale + ".Portal-" + PortalId + ".resx");
                } else if (AppLocRes.GetFiles(FileName + ".ascx." + Locale + ".resx").Count() > 0) {
                    //look for a CultureVarient
                    this.LocalResourceFile = this.LocalResourceFile.Replace("resx", Locale + ".resx");
                } else if (AppLocRes.GetFiles(FileName + ".ascx.Portal-" + PortalId + ".resx").Count() > 0) {
                    //lookFor a PortalVarient
                    this.LocalResourceFile = this.LocalResourceFile.Replace("resx", "Portal-" + PortalId + ".resx");
                }
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        accounting_currency = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId);
        if (StaffBrokerFunctions.GetSetting("CurConverter", PortalId) == "True")
        {
            if (Page.IsPostBack)
            {
                display_currency_details();
            }
        }
        else
        {
            currencyUpdatePanel.Visible = false;
        }
    }

    public void Initialize(Hashtable settings)
    {
        double LIMIT = double.Parse(settings["NoReceipt"].ToString());
        hfNoReceiptLimit.Value = LIMIT.ToString();
        //Add items to receipt dropdown
        string text = DotNetNuke.Services.Localization.Localization.GetString(RmbReceiptType.Name(RmbReceiptType.Standard) + ".Text", LocalResourceFile);
        ListItem StandardItem = new ListItem(text, RmbReceiptType.Standard.ToString());
        text = DotNetNuke.Services.Localization.Localization.GetString(RmbReceiptType.Name(RmbReceiptType.No_Receipt) + ".Text", LocalResourceFile);
        ListItem NoReceiptItem = new ListItem(text, RmbReceiptType.No_Receipt.ToString(), true);
        text = DotNetNuke.Services.Localization.Localization.GetString(RmbReceiptType.Name(RmbReceiptType.Electronic) + ".Text", LocalResourceFile);
        ListItem ElectronicItem = new ListItem(text,RmbReceiptType.Electronic.ToString(), settings["ElectronicReceipts"].ToString().Equals("True"));
        text = DotNetNuke.Services.Localization.Localization.GetString(RmbReceiptType.Name(RmbReceiptType.VAT) + ".Text", LocalResourceFile);
        ListItem VATItem = new ListItem(text, RmbReceiptType.VAT.ToString(), settings["VatAttrib"].ToString().Equals("True"));

        ddlReceipt.Items.Clear();
        ddlReceipt.Items.Add(new ListItem("", "-1", true));
        if (NoReceiptItem.Enabled)
        {
            NoReceiptItem.Text = DotNetNuke.Services.Localization.Localization.GetString("NoReceipt", LocalResourceFile).Replace("[LIMIT]", LIMIT.ToString());
            NoReceiptItem.Attributes.Add("disabled", (CADValue > LIMIT)?"disabled":"");
            ddlReceipt.Items.Add(NoReceiptItem);
        }
        ddlReceipt.Items.Add(StandardItem);
        if (ElectronicItem.Enabled) ddlReceipt.Items.Add(ElectronicItem);
        if (VATItem.Enabled) ddlReceipt.Items.Add(VATItem);
        ReceiptLine.Visible = true;
        if (!(NoReceiptItem.Enabled || ElectronicItem.Enabled || VATItem.Enabled))
        {
            // If there are no other options, hide the receipts item and assume paper receipts will be sent.
            ReceiptLine.Visible = false;
            ddlReceipt.SelectedValue = StandardItem.Value;
        }
        
        // Help strings
        hlpSupplier.Text = DotNetNuke.Services.Localization.Localization.GetString("lblSupplier.Help", LocalResourceFile);
        hlpDesc.Text = DotNetNuke.Services.Localization.Localization.GetString("lblDesc.Help", LocalResourceFile);
        hlpForWhom.Text = DotNetNuke.Services.Localization.Localization.GetString("lblForWhom.Help", LocalResourceFile);
        hlpDate.Text = DotNetNuke.Services.Localization.Localization.GetString("lblDate.Help", LocalResourceFile);
        hlpAmount.Text = DotNetNuke.Services.Localization.Localization.GetString("lblAmount.Help", LocalResourceFile);
        hlpProvince.Text = DotNetNuke.Services.Localization.Localization.GetString("lblProvince.Help", LocalResourceFile);
        hlpReceipt.Text = DotNetNuke.Services.Localization.Localization.GetString("lblReceipt.Help", LocalResourceFile).Replace("[LIMIT]", LIMIT.ToString());
        hlpOrigin.Text = DotNetNuke.Services.Localization.Localization.GetString("hlpOrigin.Help", LocalResourceFile);
        // Hint strings
        tbDesc.Attributes.Add("Placeholder", DotNetNuke.Services.Localization.Localization.GetString("lblDesc.Hint", LocalResourceFile));
        tbOrigin.Attributes.Add("Placeholder", DotNetNuke.Services.Localization.Localization.GetString("tbOrigin.Hint", LocalResourceFile));
        tbDestination.Attributes.Add("Placeholder", DotNetNuke.Services.Localization.Localization.GetString("tbDestination.Hint", LocalResourceFile));
        tbForWhom.Attributes.Add("Placeholder", DotNetNuke.Services.Localization.Localization.GetString("tbForWhom.Hint", LocalResourceFile));
    }

    #region Properties 
        public string Supplier {
            get { return tbSupplier.Text; }
            set { tbSupplier.Text = value; }
        }
        public string Comment {
            get { return tbDesc.Text; }
            set { tbDesc.Text = value; }
        }
        public DateTime theDate  {
            get {
                try {
                    return DateTime.Parse(dtDate.Text);
                } catch {
                    return DateTime.Today;
                }
            }
            set { dtDate.Text = value.ToShortDateString(); }
        }
        public double Amount {
            get {
                try {
                    return Double.Parse(tbAmount.Text, new CultureInfo("en-US").NumberFormat);
                } catch {
                    return 0;
                }
            }
            set { 
                tbAmount.Text = value.ToString("n2", new CultureInfo("en-US"));
                double exchange_rate;
                try { exchange_rate = double.Parse(tbExchangeRate.Text); }
                catch { exchange_rate = 1; }
                CADValue = value * exchange_rate;
            }
        }
        public bool VAT {
            get { return ddlReceipt.SelectedValue.Equals(RmbReceiptType.VAT.ToString()); }
            set {
                if (value == true) ddlReceipt.SelectedValue = RmbReceiptType.VAT.ToString();
                else ddlReceipt.SelectedValue = RmbReceiptType.Standard.ToString();
            }
        }
        public int ReceiptType {
            get { return int.Parse(ddlReceipt.SelectedValue); }
            set { ddlReceipt.SelectedValue = value.ToString(); }
        }
        public bool Taxable {
            get { return ddlProvince.SelectedValue == "--"; }
            set { }
        }
        public string Spare1 {
            get { return ddlProvince.SelectedValue; }
            set { ddlProvince.SelectedValue = value; }
        }
        public string Spare2 {
            get { return ""; }
            set { }
        }
        public string Spare3 {
            get { return ""; }
            set { }
        }
        public string Spare4 {
            get { return ""; }
            set { }
        }
        public string Spare5 {
            get { return ""; }
            set { }
        }
        public string ErrorText {
            get { return ""; }
            set { ErrorLbl.Text = value; }
        }
        public bool Receipt {
            get { return ddlReceipt.SelectedValue == RmbReceiptType.Standard.ToString() || ddlReceipt.SelectedValue==RmbReceiptType.Electronic.ToString() || ddlReceipt.SelectedValue==RmbReceiptType.VAT.ToString();  }
            set { if (!value) ddlReceipt.SelectedValue = RmbReceiptType.Standard.ToString(); }
        }
        public bool ReceiptsAttached
        {
            get { return hfElecReceiptAttached.Value.ToLower().Equals("true"); }
            set { hfElecReceiptAttached.Value = value.ToString(); }
        }
        public double CADValue
        {
            get {
                try
                {
                    return double.Parse(hfCADValue.Value);
                } catch {
                    return 0;
                }
            }
            set { 
                hfCADValue.Value = value.ToString(); //this is used by javascript for some reason TODO:get rid of this
                // disable "No Receipt" option, if above limit
                ddlReceipt.Items.FindByValue(RmbReceiptType.No_Receipt.ToString()).Attributes.Add("disabled", (value <= double.Parse(hfNoReceiptLimit.Value))?"disabled":"");
                if (value <= 0) return;
                double xRate = (Amount / value);
                tbExchangeRate.Text = string.Format("{0:f4}", xRate);
            }
        }
    #endregion

    #region Currency functions
    public void Currency_Change(object sender, System.EventArgs e)
    {
        string foreign_currency = ddlCurrencies.SelectedValue;
        decimal exchangeRate = StaffBrokerFunctions.GetExchangeRate(PortalId, accounting_currency, foreign_currency);
        string script = "setXRate(" + exchangeRate + "); calculateEquivalentCAD();";
        ScriptManager.RegisterStartupScript(Page, this.GetType(), "xrate", script, true);
    }

    private void display_currency_details()
    {
        string script = "$('.hfCurOpen').val('true');";
        script = script + "if ($('.ddlCur').val() == '" + accounting_currency + "') {$('.curDetails').hide();} else {$('.curDetails').show();}";
        ScriptManager.RegisterStartupScript(Page, this.GetType(), "cur", script, true);
    }
    #endregion

    #region Validation
        public bool ValidateForm(int UserId)
        {
            if (!validate_required_fields()) return false;
            if (!validate_description()) return false;
            if (!validate_date()) return false;
            if (!validate_amount()) return false;
            if (!validate_receipt()) return false;
            ErrorLbl.Text = "";
            return true;
        }
        public bool validate_required_fields()
        {
            TextBox[] required_fields = new TextBox[] {tbSupplier, tbDesc, tbAmount};
            bool result = true;
            foreach (TextBox control in required_fields) {
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
        public bool validate_description()
        {
            tbDesc.CssClass = tbDesc.CssClass.Replace("missing", "");
            if (tbDesc.Text.Length < 5)
            {
                tbDesc.CssClass += " missing";
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.Description", LocalResourceFile);
                return false;
            }
            return true;
        }
        public bool validate_date()
        {
            try
            {
                DateTime date = DateTime.Parse(dtDate.Text);
                if (date > DateTime.Today)
                {
                    ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.FutureDate", LocalResourceFile);
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
                if (amount <= 0)
                {
                    ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.NegativeAmount", LocalResourceFile);
                    return false;
                }
                if (amount > 10000)
                {
                    ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.LargeAmount", LocalResourceFile);
                    return false;
                }
            }
            catch {
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.Amount", LocalResourceFile);
                return false;
            }
            return true;
        }
        public bool validate_receipt()
            // Ensure if no receipt is selected, that the value is below the no receipt limit
            // and ensure that if electronic receipt has been selected, that something has been attached
        {
            ddlReceipt.CssClass = ddlReceipt.CssClass.Replace("missing", "");
            if (ddlReceipt.SelectedValue.Equals(RmbReceiptType.UNSELECTED.ToString()))
            {
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.RequiredField", LocalResourceFile).Replace("[LIMIT]", hfNoReceiptLimit.Value);
                ddlReceipt.CssClass = ddlReceipt.CssClass + " missing";
                return false;
            }
            double limit = 0;
            try
            {
                limit = Double.Parse(hfNoReceiptLimit.Value);
            }
            catch { }
            if ((ddlReceipt.SelectedValue.Equals(RmbReceiptType.No_Receipt.ToString())) && (Double.Parse(tbAmount.Text) > limit))
            {
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.NoReceipt", LocalResourceFile).Replace("[LIMIT]", hfNoReceiptLimit.Value);
                return false;
            }
            if (ddlReceipt.SelectedValue.Equals(RmbReceiptType.Electronic.ToString())) {
                if (! hfElecReceiptAttached.Value.ToLower().Equals("true"))
                {
                    ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Error.NoElecReceipt", LocalResourceFile);
                    return false;
                }
            }
            return true;
        }
    #endregion
 
</script>

