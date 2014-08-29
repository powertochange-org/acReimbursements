Imports DotNetNuke.Services.FileSystem
Imports System.IO
Imports StaffBroker
Imports System.Drawing.Imaging

Partial Class DesktopModules_AgapeConnect_StaffRmb_Controls_Currency
    Inherits Entities.Modules.PortalModuleBase

    Dim theFolder As IFolderInfo
    Dim PS As PortalSettings = CType(HttpContext.Current.Items("PortalSettings"), PortalSettings)
    Dim ac As String = StaffBrokerFunctions.GetSetting("AccountingCurrency", PS.PortalId)
    Private _advMode As Boolean
    Public Property AdvMode() As Boolean
        Get
            Return _advMode
        End Get
        Set(ByVal value As Boolean)
            _advMode = value
        End Set
    End Property
    Private _advPayoffMode As Boolean
    Public Property AdvPayOffMode() As Boolean
        Get
            Return _advPayoffMode
        End Get
        Set(ByVal value As Boolean)
            _advPayoffMode = value
        End Set
    End Property

    Protected Sub Page_Init(sender As Object, e As System.EventArgs) Handles Me.Init
        Dim FileName As String = System.IO.Path.GetFileNameWithoutExtension(Me.AppRelativeVirtualPath)
        If Not (Me.ID Is Nothing) Then
            'this will fix it when its placed as a ChildUserControl 
            Me.LocalResourceFile = Me.LocalResourceFile.Replace(Me.ID, FileName)
        Else
            ' this will fix it when its dynamically loaded using LoadControl method 
            Me.LocalResourceFile = Me.LocalResourceFile & FileName & ".ascx.resx"
            Dim Locale = System.Threading.Thread.CurrentThread.CurrentCulture.Name
            Dim AppLocRes As New System.IO.DirectoryInfo(Me.LocalResourceFile.Replace(FileName & ".ascx.resx", ""))
            If Locale = PortalSettings.CultureCode Then
                'look for portal varient
                If AppLocRes.GetFiles(FileName & ".ascx.Portal-" & PortalId & ".resx").Count > 0 Then
                    Me.LocalResourceFile = Me.LocalResourceFile.Replace("resx", "Portal-" & PortalId & ".resx")
                End If
            Else

                If AppLocRes.GetFiles(FileName & ".ascx." & Locale & ".Portal-" & PortalId & ".resx").Count > 0 Then
                    'lookFor a CulturePortalVarient
                    Me.LocalResourceFile = Me.LocalResourceFile.Replace("resx", Locale & ".Portal-" & PortalId & ".resx")
                ElseIf AppLocRes.GetFiles(FileName & ".ascx." & Locale & ".resx").Count > 0 Then
                    'look for a CultureVarient
                    Me.LocalResourceFile = Me.LocalResourceFile.Replace("resx", Locale & ".resx")
                ElseIf AppLocRes.GetFiles(FileName & ".ascx.Portal-" & PortalId & ".resx").Count > 0 Then
                    'lookFor a PortalVarient
                    Me.LocalResourceFile = Me.LocalResourceFile.Replace("resx", "Portal-" & PortalId & ".resx")
                End If
            End If
        End If
    End Sub
    Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
        ' Set the proper visibility
        updateView()
        If Not ((AdvMode Or AdvPayOffMode) And Page.IsPostBack) Then


            'Dim suffix = ""
            'Dim amount = "rmbAmount"
            'If AdvMode And Not btnCurrency.CssClass.EndsWith("Adv") Then
            '    btnCurrency.CssClass &= "Adv"
            '    tbCurrency.Attributes("class") &= "Adv"
            '    ddlCurrencies.Attributes("class") &= "Adv"
            '    dCurrency.Attributes("class") &= "Adv"
            '    suffix = "Adv"
            '    amount = "advAmount"
            'ElseIf AdvPayOffMode And Not btnCurrency.CssClass.EndsWith("AdvPO") Then
            '    btnCurrency.CssClass &= "AdvPO"
            '    tbCurrency.Attributes("class") &= "AdvPO"
            '    ddlCurrencies.Attributes("class") &= "AdvPO"
            '    dCurrency.Attributes("class") &= "AdvPO"
            '    suffix = "AdvPO"
            '    amount = "advPOAmount"
            'End If

            ' Update helper text
            tbExchangeRateLbl.Text = "Exchange Rate: 1 " & ddlCurrencies.SelectedValue.ToString() & " = "
            tbExchangeRatePostLbl.Text = ac




            'sb.Append(" var jsonCall= ""/MobileCAS/MobileCAS.svc/ConvertCurrency?FromCur=" + lc + "&ToCur=" + ac + """;")
            ' sb.Append("$.getJSON( jsonCall ,function(x) { setXRate(x);});")



            ' sb.Append(" $('.ddlCur" & suffix & "').change();")
            'sb.Append("setXRate(" & xrate & ");")



            'If StaffBrokerFunctions.GetSetting("CurConverter", PortalId) = "True" Then

            '    Dim t As Type = Me.GetType()
            '    Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            '    sb.Append("<script language='javascript'>")
            '    'If suffix = "" Then
            '    '    sb.Append(" var tempValue=$('." & amount & "').val();   $('.ddlCur" & suffix & "').change(); $('." & amount & "').val(tempValue);   $('.currency" & suffix & "').val((parseFloat(tempValue) / parseFloat(" & xrate.ToString("n8", New CultureInfo("en-US")) & ")).toFixed(2));  $('.divCur" & suffix & "').show(); $('#" & btnCurrency.ClientID & "').hide();")
            '    'Else
            '    '    sb.Append("$('.divCur" & suffix & "').show(); $('#" & btnCurrency.ClientID & "').hide();")

            '    'End If

            '    sb.Append("$('.hfCurOpen').val('true');")
            '    sb.Append("</script>")
            '    ScriptManager.RegisterStartupScript(Page, t, "cur" & suffix, sb.ToString, False)
            'End If


        End If
    End Sub

    Protected Sub ddlCurrencies_Change(sender As Object, e As System.EventArgs) Handles ddlCurrencies.SelectedIndexChanged
        updateExchangeRate()
        ' Update helper text
        tbExchangeRateLbl.Text = "Exchange Rate: 1 " & ddlCurrencies.SelectedValue.ToString() & " = "
        tbExchangeRatePostLbl.Text = ac
    End Sub

    Protected Sub selection_Change(sender As Object, e As System.EventArgs) Handles rbManualExchange.CheckedChanged, rbAutomaticExchange.CheckedChanged, cbForeignCurrency.CheckedChanged
        updateView()
    End Sub

    ' Function set the visibility of various parts of the control
    Private Sub updateView()
            ' Set visibility of the currency div based on the status of the checkbox
            If cbForeignCurrency.Checked Then
                dCurrency.Visible = True
                ' Set the visibility of the photo upload button
                If rbManualExchange.Checked Then
                    ' We're using manual
                    uploadProof.Visible = True
                    ' Enable the exchange rate entry
                    tbExchangeRate.Attributes.Remove("disabled")
                Else
                    ' Hide the photo uploader
                    uploadProof.Visible = False
                    ' Disable the exchange rate entry and calculate it automatically
                    ' We disable it using the attributes so that we can still access the element using javascript
                    tbExchangeRate.Attributes("disabled") = "disabled"
                    updateExchangeRate()
                End If
            Else
                dCurrency.Visible = False
            End If
     End Sub

    ' Update the exchange rate text box
    Private Sub updateExchangeRate()
        ' Get the exchange rate based on the current country and our account currency
        tbExchangeRate.Text = StaffBrokerFunctions.GetExchangeRate(ddlCurrencies.SelectedValue, ac)
    End Sub

End Class
