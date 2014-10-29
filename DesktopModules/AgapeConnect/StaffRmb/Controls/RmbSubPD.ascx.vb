
Partial Class controls_RmbSubPD
    Inherits Entities.Modules.PortalModuleBase

    Protected breakfastValue As Double
    Protected lunchValue As Double
    Protected supperValue As Double

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


    Public Property Supplier() As String
        Get
            Return ""
        End Get
        Set(value As String)
        End Set
    End Property
    Public Property Comment() As String
        Get
            Dim result = If(cbBreakfast.Checked, Translate("lblBreakfast") & " ", "") & If(cbLunch.Checked, Translate("lblLunch") & " ", "") & If(cbSupper.Checked, Translate("lblSupper") & " ", "")
            Return result
        End Get
        Set(ByVal value As String)
            cbBreakfast.Checked = value.Contains(Translate("lblBreakfast") & " ")
            cbLunch.Checked = value.Contains(Translate("lblLunch") & " ")
            cbSupper.Checked = value.Contains(Translate("lblSupper") & " ")
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
    Public Property Taxable() As Boolean
        Get
            Return False
        End Get
        Set(ByVal value As Boolean)
        End Set
    End Property
    Public Property Amount() As Double
        Get
            Dim result As Double
            Try
                result = CDbl(tbAmount.Text)
            Catch
                Return 0
            End Try
            Return result
        End Get
        Set(ByVal value As Double)
            'tbAmount.Text = value.ToString("n2", New CultureInfo("en-US"))
            ScriptManager.RegisterClientScriptBlock(tbAmount, GetType(CheckBox), "calculate", "updatePerDiemTotal();", True)
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
            Return ""
        End Get
        Set(ByVal value As String)
        End Set
    End Property
    Public Property Spare3() As String
        Get
            Return ""
        End Get
        Set(ByVal value As String)
        End Set
    End Property
    Public Property Spare4() As String
        Get
            Return ""
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
            Return False
        End Get
        Set(ByVal value As Boolean)

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
    Public Property CADValue() As Double
        Get
            Return CDbl(hfCADValue.Value)
        End Get
        Set(value As Double)
            hfCADValue.Value = value
        End Set
    End Property
    Public Property Repeat() As Integer
        Get
            Try
                Return CInt(tbRepeat.Text)
            Catch
                Return 1
            End Try
        End Get
        Set(value As Integer)
            tbRepeat.Text = CStr(value)
            tbRepeat.Visible = True
            lblRepeat.Visible = True
        End Set
    End Property

    Protected Function Translate(Key As String) As String
        Return DotNetNuke.Services.Localization.Localization.GetString(Key & ".Text", LocalResourceFile)
    End Function

    Public Function ValidateForm(ByVal userId As Integer) As Boolean
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
        Dim selections As Integer = 0
        selections = If(cbBreakfast.Checked, 1, 0) + If(cbLunch.Checked, 1, 0) + If(cbSupper.Checked, 1, 0)
        If (selections = 0) Then
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Selection.Error", LocalResourceFile)
            Return False
        End If
        Try
            Dim repeat As Integer = CInt(tbRepeat.Text)
            If repeat < 1 Or repeat > 14 Then
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Repeat.Error", LocalResourceFile)
                Return False
            End If
            If theDate.AddDays(repeat - 1) > Today Then
                ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("RepeatDate.Error", LocalResourceFile)
                Return False
            End If
        Catch ex As Exception
            ErrorLbl.Text = DotNetNuke.Services.Localization.Localization.GetString("Repeat.Error", LocalResourceFile)
            Return False
        End Try
        ErrorLbl.Text = ""
        Return True
    End Function

    Public Sub Initialize(ByVal Settings As Hashtable)
        Try
            breakfastValue = CDbl(Settings("PDBreakfast"))
            cbBreakfast.Attributes.Add("title", Settings("PDBreakfast")) 'Store value in title attribute, for javascript to access
            lunchValue = CDbl(Settings("PDLunch"))
            cbLunch.Attributes.Add("title", Settings("PDLunch"))
            supperValue = CDbl(Settings("PDSupper"))
            cbSupper.Attributes.Add("title", Settings("PDSupper"))
        Catch
            breakfastValue = -1
            lunchValue = -1
            supperValue = -1
        End Try
        tbRepeat.Text = "1"
        tbRepeat.Visible = False
        lblRepeat.Visible = False
    End Sub


End Class


