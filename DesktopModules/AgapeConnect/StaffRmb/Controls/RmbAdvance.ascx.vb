Imports System
Imports System.Collections
Imports System.Configuration
Imports System.Data
Imports System.Linq
Imports System.Web
Imports System.Web.Security
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Web.UI.WebControls
Imports System.Web.UI.WebControls.WebParts
Imports System.IO
Imports System.Net
Imports DotNetNuke
Imports DotNetNuke.Security

'Imports AgapeStaff
Imports StaffBroker
Imports StaffBrokerFunctions

Partial Class RmbAdvance
    Inherits Entities.Modules.PortalModuleBase

    Dim MAX_DAYS = 365 'Maximum number of days before clearing an advance

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
    Public Sub Initialize(ByVal settings As Hashtable)
        tbDesc.Attributes.Add("placeholder", DotNetNuke.Services.Localization.Localization.GetString("DescriptionHint.Text", "/DesktopModules/AgapeConnect/StaffRmb/App_LocalResources/StaffRmb.ascx.resx"))
    End Sub
    Public Property ReceiptType() As Integer
        Get
            Return 0
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
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
    Public Property VAT() As Boolean
        Get
            Return False
        End Get
        Set(ByVal value As Boolean)
        End Set
    End Property

    Public Property Amount() As Double
        Get
            Try
                Return Double.Parse(tbAmount.Text, New CultureInfo("en-US").NumberFormat)
            Catch
                Return 0
            End Try
        End Get
        Set(ByVal value As Double)
            tbAmount.Text = value.ToString("n2", New CultureInfo("en-US"))
        End Set
    End Property

    Public Property Taxable() As Boolean
        Get
            Return False
        End Get
        Set(ByVal value As Boolean)
        End Set
    End Property
    Public Property Spare1() As String
        Get
            Return Nothing
        End Get
        Set(ByVal value As String)
        End Set
    End Property
    Public Property Spare2() As String
        Get
            Return hfUnclearedAmount.value
        End Get
        Set(ByVal value As String)
            hfUnclearedAmount.Value = value
        End Set
    End Property
    Public Property Spare3() As String
        Get
            Return Nothing
        End Get
        Set(ByVal value As String)

        End Set
    End Property
    Public Property Spare4() As String
        Get
            Return Nothing
        End Get
        Set(ByVal value As String)

        End Set
    End Property
    Public Property Spare5() As String
        Get
            Return Nothing
        End Get
        Set(ByVal value As String)

        End Set
    End Property
    Public Property ErrorText() As String
        Get
            Return ""
        End Get
        Set(ByVal value As String)
            ErrorLbl.Text = value
        End Set
    End Property
    Public Property Receipt() As Boolean
        Get
            Return False
        End Get
        Set(ByVal value As Boolean)
           
        End Set
    End Property
    Public Property CADValue() As Double
        Get
            Return Amount
        End Get
        Set(value As Double)
        End Set
    End Property

    Public Function ValidateForm(ByVal userId As Integer) As Boolean

        If (tbDesc.Text.Length < 5) Then
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Description.Error", LocalResourceFile)
            Return False
        End If
        Try
            Dim theDate As Date = dtDate.Text
            If theDate > Today.AddDays(MAX_DAYS) Then
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("FutureDate.Error", LocalResourceFile)
                Return False
            End If
            If theDate < Today Then
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("OldDate.Error", LocalResourceFile)
                Return False
            End If
        Catch ex As Exception
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Date.Error", LocalResourceFile)
            Return False
        End Try

        Try
            Dim theAmount As Double = Double.Parse(tbAmount.Text, New CultureInfo("en-US").NumberFormat)
            If Amount <= 0.01 Then
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Amount.Error", LocalResourceFile)
                Return False
            End If
        Catch ex As Exception
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Amount.Error", LocalResourceFile)
            Return False
        End Try
        ErrorLbl.Text = ""
        Return True
    End Function


End Class

