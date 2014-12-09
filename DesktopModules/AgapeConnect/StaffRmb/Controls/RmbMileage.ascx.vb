Imports System.Linq
Partial Class controls_Mileage
    Inherits Entities.Modules.PortalModuleBase
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

    Public Function Translate(ByVal ResourceString As String) As String
        Return DotNetNuke.Services.Localization.Localization.GetString(ResourceString, LocalResourceFile)

    End Function


    Public Property Supplier() As String
        Get
            Return ""
        End Get
        Set(value As String)
        End Set
    End Property
    Public Property Comment() As String
        Get
            Return tbDesc.Text
        End Get
        Set(ByVal value As String)
            tbDesc.Text = value
        End Set
    End Property
    Public Property theDate() As Date
        Get
            Return CDate(dtDate.Text)
        End Get
        Set(ByVal value As Date)
            If value = Nothing Then
                dtDate.Text = Today.ToShortDateString
            Else
                dtDate.Text = value
            End If
        End Set
    End Property

    Public Property Amount() As Double
        Get
            If tbAmount.Text <> "" Then
                Try
                    Return Math.Round((CInt(tbAmount.Text) * CDbl(ddlDistUnits.SelectedValue)), 2)
                Catch
                    Return 0
                End Try
            Else
                Return 0
            End If

        End Get
        Set(ByVal value As Double)
            'tbAmount.Text = CInt(value / ((ddlDistUnits.SelectedValue + (5 * CInt(ddlStaff.SelectedValue))) / 100))
        End Set
    End Property
    Public Property Spare1() As String
        Get
            Return ddlProvince.SelectedValue
        End Get
        Set(ByVal value As String)
            Try
                ddlProvince.SelectedValue = value
            Catch
                ddlProvince = Nothing
            End Try
        End Set
    End Property
    Public Property Spare2() As String
         Get
            Return Nothing
        End Get
        Set(ByVal value As String)
        End Set
    End Property
    Public Property Spare3() As String
        Get
            Return ddlDistUnits.SelectedIndex
        End Get
        Set(ByVal value As String)
            Try
                ddlDistUnits.ClearSelection()
                ddlDistUnits.SelectedIndex = CInt(value)
            Catch ex As Exception
                ddlDistUnits.SelectedIndex = 0

            End Try
        End Set
    End Property
    Public Property Spare4() As String
        Get
            Return tbOrigin.Text
        End Get
        Set(ByVal value As String)
            tbOrigin.Text = value
        End Set
    End Property
    Public Property Spare5() As String
        Get
            Return tbDestination.Text
        End Get
        Set(ByVal value As String)
            tbDestination.Text = value
        End Set
    End Property
    Public Property Mileage As Integer
        Get
            Return CInt(tbAmount.Text)
        End Get
        Set(ByVal value As Integer)
            Try
                tbAmount.Text = CInt(value)
            Catch ex As Exception
                tbAmount.Text = 0
            End Try
        End Set
    End Property

    Public ReadOnly Property MileageRate() As Decimal
        Get
            Return CDec(ddlDistUnits.SelectedValue)
        End Get
    End Property
    Public Property Receipt() As Boolean
        Get
            Return False ' ddlReceipt.SelectedValue = "Yes"
        End Get
        Set(ByVal value As Boolean)
        End Set
    End Property
    Public Property ReceiptType() As Integer
        Get
            Return 0 'no receipt required for mileage expenses
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
    Public Property VAT() As Boolean
        Get
            Return False
        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property
    Public Property Taxable() As Boolean
        Get
            Return False
        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property
    Public Property CADValue() As Double
        Get
            Return CDbl(hfCADValue.Value)
        End Get
        Set(value As Double)
            hfCADValue.Value = value
        End Set
    End Property
    Public Function ValidateForm(ByVal userId As Integer) As Boolean
        If (tbDesc.Text.Length < 5) Then
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Description.Error", LocalResourceFile)
            Return False
        End If
        If (tbOrigin.Text.Length < 3) Then
            ErrorLbl.Text = Translate("Origin.Error")
            Return False
        End If
        If (tbDestination.Text.Length < 3) Then
            ErrorLbl.Text = Translate("Destination.Error")
            Return False
        End If
        Try
            Dim theDate As Date = dtDate.Text
            If theDate > Today Then
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("OldDate.Error", LocalResourceFile)
                Return False
            End If
        Catch ex As Exception
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Date.Error", LocalResourceFile)
            Return False
        End Try

        Try
            Dim theMiles As Double = tbAmount.Text
            If theMiles <= 0 Then
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Reverse.Error", LocalResourceFile)
                Return False
            ElseIf theMiles <= 1 Or theMiles > 9999 Then
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Miles.Error", LocalResourceFile)
                Return False
            End If
        Catch ex As Exception
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Miles.Error", LocalResourceFile)
            Return False
        End Try

        Dim staff As New ArrayList
        Dim staff2 As New ArrayList


        ErrorLbl.Text = ""
        Return True
    End Function


    Public Property ErrorText() As String
        Get
            Return ""
        End Get
        Set(ByVal value As String)
            ErrorLbl.Text = value
        End Set
    End Property

    Public Sub Initialize(ByVal Settings As System.Collections.Hashtable)
        Dim PS = CType(HttpContext.Current.Items("PortalSettings"), PortalSettings)
        If (ddlDistUnits.Items.Count() = 0) Then
            For I As Integer = 1 To 4
                Dim value As String = Settings("MRate" & I)
                If value <> "" Then
                    ddlDistUnits.Items.Add(New ListItem(Settings("MRate" & I & "Name") & " (" & StaffBrokerFunctions.GetSetting("Currency", PS.PortalId) & CDbl(value).ToString("0.00") & ")", CDbl(value)))
                End If
            Next I
        End If
        tbOrigin.Attributes.Add("placeholder", Translate("Origin.Hint"))
        tbDestination.Attributes.Add("placeholder", Translate("Destination.Hint"))
        tbDesc.Attributes.Add("placeholder", DotNetNuke.Services.Localization.Localization.GetString("DescriptionHint.Text", "/DesktopModules/AgapeConnect/StaffRmb/App_LocalResources/StaffRmb.ascx.resx"))
        Session("RmbSettings") = Settings
    End Sub

    Protected Sub UpdatePanel1_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles UpdatePanel1.PreRender
        If Not Session("RmbSettings") Is Nothing Then
            Initialize(Session("RmbSettings"))
        End If

    End Sub


    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

    End Sub
End Class


