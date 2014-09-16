Imports DotNetNuke.Services.FileSystem
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
            If (Page.IsPostBack) Then
                Dim script As String = "$('.hfCurOpen').val('true');"
                script = script + "if ($('.ddlCur').val() == '" & ac & "') {$('.curDetails').hide();}"
                ScriptManager.RegisterStartupScript(Page, Me.GetType(), "cur", script, True)
            End If
        End If
    End Sub

    ' Update the exchange rate text box
    Private Sub updateExchangeRate()
        ' Get the exchange rate based on the current country and our account currency
        '        tbExchangeRate.Text = StaffBrokerFunctions.GetExchangeRate(ddlCurrencies.SelectedValue, ac)
    End Sub

End Class
