﻿Imports System.Linq
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
                    Return (CDbl(tbAmount.Text) * CDbl(ddlDistUnits.SelectedValue))
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
            Return Nothing
        End Get
        Set(ByVal value As String)

        End Set
    End Property
    Public Property Spare2() As String
        Get

            Return tbAmount.Text
        End Get
        Set(ByVal value As String)
            Try
                tbAmount.Text = CInt(value)
            Catch ex As Exception
                tbAmount.Text = 0
            End Try


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
    Public Property Receipt() As Boolean
        Get
            Return False ' ddlVATReceipt.SelectedValue = "Yes"
        End Get
        Set(ByVal value As Boolean)
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
    Public Function ValidateForm(ByVal userId As Integer) As Boolean
        if (tbDesc.Text.Length < 5) Then
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Description.Error", LocalResourceFile)
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
            ElseIf theMiles <= 1 Then
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


