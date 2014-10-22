Imports StaffRmb

Partial Class DesktopModules_AgapeConnect_StaffRmb_Mobile
    Inherits System.Web.UI.Page

    Dim d As New StaffRmbDataContext
    Private base As DotNetNuke.Entities.Modules.PortalModuleBase = New DotNetNuke.Entities.Modules.PortalModuleBase()
    'TODO:replace hardcoded userid
    Dim UserId As Integer = 3
    Protected PortalId As Integer = base.PortalId
    Dim UserInfo As DotNetNuke.Entities.Users.UserInfo = base.UserInfo
    Protected Settings As Hashtable = base.Settings

#Region "CopiedFromStaffRmb"
    Protected Function IsAccounts() As Boolean
        ' Does the logged in user have the "Accounts" role?
        Try
            For Each role In CStr(Settings("AccountsRoles")).Split(";")
                If (UserInfo.Roles().Contains(role)) Then
                    Return True
                End If
            Next
        Catch ex As Exception
        End Try
        Return False
    End Function

    Protected Function CanEdit(ByVal status As Integer) As Boolean
        ' Can a reimbursement in the provided status be edited by the logged in user?
        Return status <> RmbStatus.Paid And status <> RmbStatus.Processing And status <> RmbStatus.PendingDownload And status <> RmbStatus.DownloadFailed And (status <> RmbStatus.Approved Or IsAccounts())
    End Function

    Public Function IsDifferentExchangeRate(xRate1 As Double, xRate2 As Double) As Boolean
        'determine whether the 2 exchange rates differ by more than the fudge factor
        Dim fudge_factor = 0.001
        Dim max_difference As Double = 0.0
        If (xRate1 >= xRate2) Then
            max_difference = xRate1 * fudge_factor
        Else
            max_difference = xRate2 * fudge_factor
        End If
        Return Math.Abs(xRate1 - xRate2) > max_difference
    End Function



    Protected Function GetLocalTypeName(ByVal LineTypeId As Integer) As String
        ' Look up the Name for a TypeID
        Dim d As New StaffRmbDataContext
        Dim q = From c In d.AP_StaffRmb_PortalLineTypes Where c.LineTypeId = LineTypeId And c.PortalId = PortalId Select c.LocalName
        If q.Count > 0 Then
            Return q.First
        Else
            Dim r = From c In d.AP_Staff_RmbLineTypes Where c.LineTypeId = LineTypeId Select c.TypeName
            If r.Count > 0 Then
                Return r.First
            Else
                Return "?"
            End If
        End If
    End Function

    Protected Function GetMileageString(ByVal mileage As Integer, unitIndex As String) As String
        ' Mileage string in standard format
        Dim result As String
        If mileage = 0 Then
            Return ""
        End If
        result = "(" & mileage
        If unitIndex IsNot Nothing Then
            result &= Settings("MRate" & (unitIndex + 1) & "Name")
        End If
        Return result & ")"
    End Function

    Public Function GetLineComment(ByVal comment As String, ByVal Currency As String, ByVal CurrencyValue As Double, ByVal ShortComment As String, Optional ByVal includeInitials As Boolean = True, Optional ByVal explicitStaffInitals As String = Nothing, Optional ByVal Mileage As String = "") As String
        ' Line Comment in standard format
        ' Prefix initials  // suffix Currency   // Trim to 30 char
        Dim initials As String = ""
        If includeInitials Then
            If Not String.IsNullOrEmpty(explicitStaffInitals) Then
                initials = UnidecodeSharpFork.Unidecoder.Unidecode(explicitStaffInitals & "-").Substring(0, 3)
            Else
                initials = UnidecodeSharpFork.Unidecoder.Unidecode(hfStaffInitials.Value & "-").Substring(0, 3)
            End If
        End If
        If Not String.IsNullOrEmpty(ShortComment) Then
            Return initials & UnidecodeSharpFork.Unidecoder.Unidecode(ShortComment)
        End If
        Dim CurString = ""
        If Mileage <> "" Then
            'this is a mileage expense item, so don't show currency - show milage instead.
            CurString = "-" & Mileage & Left(Settings("DistanceUnit").ToString(), 2)
        Else
            If Not String.IsNullOrEmpty(Currency) Then
                If Currency <> StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId) Then
                    CurString = Currency & CurrencyValue.ToString("f2")
                    CurString = CurString.Replace(".00", "")
                End If
            End If
        End If
        Dim c = UnidecodeSharpFork.Unidecoder.Unidecode(comment)
        Return initials & c.Substring(0, Math.Min(c.Length, 27 - CurString.Length)) & CurString
    End Function

    Public Function GetTotal(ByVal theRmbNo As Integer) As Double
        ' Return the total value of the reimbursement
        Dim result As Double
        If theRmbNo = -1 Then
            theRmbNo = hfRmbNo.Value
        End If
        Try
            result = CType((From c In d.AP_Staff_RmbLines Where c.RmbNo = theRmbNo Select c.GrossAmount).Sum, Double?).GetValueOrDefault(0)
        Catch
            result = 0
        End Try
        Return result
    End Function

    Protected Function ElectronicReceiptTags(lineId As Integer) As String
        ' Format electronic receipt images
        Dim ERR = "<span color='red'>!</span>"
        Dim result = ""
        Dim receipts = From e In d.AP_Staff_RmbLine_Files Where e.RmbLineNo = lineId Order By e.RecNum Select e.URL, e.FileId
        If (receipts.Count < 1) Then
            result = ERR
        Else
            For Each receipt In receipts
                Dim extension = "NONE"
                Dim file = DotNetNuke.Services.FileSystem.FileManager.Instance.GetFile(receipt.FileId)
                If (file Is Nothing) Then
                    result += ERR
                Else
                    extension = file.Extension.ToLower()
                    If extension = "pdf" Then
                        result += "<a target='_Blank' href='" + receipt.URL + "' title = 'click to download'><img class='" & "' src='/Icons/Sigma/ExtPdf_32X32_Standard.png' width=20 alt='pdf' /></a>"
                    ElseIf {"jpg", "jpeg", "png", "gif", "bmp"}.Contains(extension) Then
                        result += "<a target='_Blank' href=" + receipt.URL + "><img id='" + receipt.URL + "' class='viewReceipt" & "' src='/Icons/Sigma/ExtPng_32x32_Standard.png' width=20 alt='img' /></a>"
                    Else
                        result += "<img src='/Icons/Sigma/ErrorWarning_16X16_Standard.png' width=20 alt='missing' title='" & extension & "'/>"
                    End If
                End If
            Next
        End If
        Return result
    End Function
#End Region

#Region "Utilities"
    Public Function Translate(ByVal ResourceString As String) As String
        ' Look up a resource string
        Return DotNetNuke.Services.Localization.Localization.GetString(ResourceString & ".Text", base.LocalResourceFile)

    End Function
#End Region

#Region "Events"
    Protected Sub btnList_click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnList.Click
        Try
            Dim Rmbs = From c In d.AP_Staff_Rmbs
                           Where c.UserId = UserId
                           Order By c.RID Descending
                           Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId
            Dim count As Integer = Rmbs.Count()
            dlActiveList.DataSource = Rmbs
            dlActiveList.DataBind()
            lblLoadingDetails.Visible = True
        Catch ex As Exception
            'TODO:error message
        End Try
    End Sub

    Protected Sub btnDetails_click(ByVal sender As Object, ByVal e As RepeaterCommandEventArgs) Handles dlActiveList.ItemCommand
        If (e.CommandName = "GoTo") Then
            Dim rmbNo As String = e.CommandArgument
            If (rmbNo.Length = 0) Then Return
            hfRmbNo.Value = rmbNo
            Dim q = From c In d.AP_Staff_Rmbs Where c.RMBNo = rmbNo
            If q IsNot Nothing And q.Count > 0 Then
                Dim Rmb = q.First
                gvRmbLines.DataSource = Rmb.AP_Staff_RmbLines
                gvRmbLines.DataBind()
                lblLoadingDetails.Visible = False
            End If
        End If
    End Sub
#End Region

End Class
