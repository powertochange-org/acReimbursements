﻿Imports DotNetNuke.Services.FileSystem
Imports System.IO
Imports StaffBroker
Imports System.Drawing.Imaging

Partial Class DesktopModules_AgapeConnect_StaffRmb_Controls_Currency
    Inherits Entities.Modules.PortalModuleBase

    Dim theFolder As IFolderInfo
    Dim PS As PortalSettings = CType(HttpContext.Current.Items("PortalSettings"), PortalSettings)
    Dim ac As String = StaffBrokerFunctions.GetSetting("AccountingCurrency", PS.PortalId)

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
        If StaffBrokerFunctions.GetSetting("CurConverter", PortalId) = "True" Then
            ddlCurrencies.Attributes.Add("onchange", "currencyChange(this.value);")
            If (Page.IsPostBack) Then
                display_currency_details()
            End If
        End If
    End Sub

    Public Sub Currency_Change(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlCurrencies.SelectedIndexChanged
        Dim accounting_currency = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)
        Dim foreign_currency = ddlCurrencies.SelectedValue
        Dim exchangeRate = StaffBrokerFunctions.GetExchangeRate(accounting_currency, foreign_currency)
        Dim script = "setXRate(" & exchangeRate & "); calculateEquivalentCAD();"
        ScriptManager.RegisterStartupScript(Page, Me.GetType(), "xrate", script, True)
    End Sub

    'Public Sub setCurrency(currency As String)
    '    ddlCurrencies.SelectedValue = currency
    '    display_currency_details()
    'End Sub

    Private Sub display_currency_details()
        Dim script As String = "$('.hfCurOpen').val('true');"
        script = script + "if ($('.ddlCur').val() == '" & ac & "') {$('.curDetails').hide();} else {$('.curDetails').show();}"
        ScriptManager.RegisterStartupScript(Page, Me.GetType(), "cur", script, True)
    End Sub

End Class
