﻿Imports System
Imports System.Collections
Imports System.Configuration
Imports System.Data
Imports System.Data.OleDb
Imports System.Linq
Imports System.Web
Imports System.Web.Security
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Web.UI.WebControls
Imports System.Web.UI.WebControls.WebParts
Imports System.IO
Imports System.Threading.Tasks
Imports System.Net
Imports System.Net.Http
Imports DotNetNuke
Imports DotNetNuke.Security
Imports StaffRmb
Imports StaffBroker
Imports DotNetNuke.Services.FileSystem
Namespace DotNetNuke.Modules.StaffRmbMod
    Partial Class ViewStaffRmb
        Inherits Entities.Modules.PortalModuleBase
        Implements Entities.Modules.IActionable
        Dim ENABLE_ADVANCE_FUNCTIONALITY As Boolean = False

#Region "Properties"
        Dim d As New StaffRmbDataContext
        Dim ds As New StaffBrokerDataContext
        Dim theControl As Object
        Dim objEventLog As New DotNetNuke.Services.Log.EventLog.EventLogController
        'Dim SpouseList As IQueryable(Of StaffBroker.User)  '= AgapeStaffFunctions.SpouseIsLeader()
        Dim VAT3ist As String() = {"111X", "112X", "113", "116", "514X"}
#End Region

#Region "Page Events"
        Protected Sub Page_Load1(sender As Object, e As System.EventArgs) Handles Me.Load
            hfPortalId.Value = PortalId
            lblMovedMenu.Visible = IsEditable

            For i As Integer = 2 To hfRows.Value
                Dim insert As New TableRow()
                Dim insertDesc As New TableCell()
                Dim insertAmt As New TableCell()
                Dim tbDesc As New TextBox()
                ' tbDesc.ID = "tbDesc" & i
                tbDesc.Width = Unit.Percentage(100)
                tbDesc.CssClass = "Description"
                Dim tbAmt As New TextBox()
                tbAmt.Width = Unit.Pixel(100)
                '  tbAmt.ID = "tbAmt" & i
                tbAmt.CssClass = "Amount"
                tbAmt.Attributes.Add("onblur", "calculateTotal();")
                insertDesc.Controls.Add(tbDesc)
                insertAmt.Controls.Add(tbAmt)

                insert.Cells.Add(insertDesc)
                insert.Cells.Add(insertAmt)
                tblSplit.Rows.Add(insert)
            Next
        End Sub

        Private Async Function Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) As Task Handles Me.Init

            lblAdvanceRequest.Visible = ENABLE_ADVANCE_FUNCTIONALITY
            lblError.Visible = False
            If Not String.IsNullOrEmpty(Settings("NoReceipt")) Then
                hfNoReceiptLimit.Value = Settings("NoReceipt")
            End If

            If Not Page.IsPostBack Then
                If Request.QueryString("RmbNo") <> "" Then
                    hfRmbNo.Value = CInt(Request.QueryString("RmbNo"))
                End If
                If Settings("isLoaded") = "" Then
                    LoadDefaultSettings()
                End If
                Try
                    If Not ddlBankAccount.Items.FindByValue(Settings("BankAccount")) Is Nothing Then
                        ddlBankAccount.SelectedValue = CStr(Settings("BankAccount"))
                    End If
                Catch ex As Exception

                End Try

                If StaffBrokerFunctions.GetSetting("ZA-Mode", PortalId) = "True" Then
                    cbExpenses.Enabled = False
                    cbExpenses.Checked = True
                    cbSalaries.Checked = True
                    cbSalaries.Enabled = False

                End If
                hfAccountingCurrency.Value = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)
                If hfAccountingCurrency.Value = "" Then
                    hfAccountingCurrency.Value = "USD"
                    StaffBrokerFunctions.SetSetting("AccountingCurrency", "USD", PortalId)
                End If

                Dim staff = StaffBrokerFunctions.GetStaffMember(UserId)
                Dim PayOnly As Boolean = False
                Dim PAC = StaffBrokerFunctions.GetStaffProfileProperty(staff.StaffId, "PersonalAccountCode")
                Dim CC = ""
                If staff Is Nothing Then
                    'Cannot Use
                    lblError.Text = "Access Denied. You have not been setup as a staff member on this website. Please ask your accounts team or administrator to setup your staff profile."

                    lblError.Visible = True
                    pnlEverything.Visible = False
                    Return
                ElseIf staff.CostCenter = Nothing And PAC = "" Then

                    'cannot use
                    lblError.Text = "Access Denied. Your account has not been setup with a valid Responsibility Center. Please ask your accounts team or administrator to setup your staff profile."
                    lblError.Visible = True
                    pnlEverything.Visible = False
                    Return


                Else
                    CC = staff.CostCenter
                    PayOnly = StaffBrokerFunctions.GetStaffProfileProperty(staff.StaffId, "PayOnly")
                    'PAC = StaffBrokerFunctions.GetStaffProfileProperty(staff.StaffId, "PersonalAccountCode")
                    If CC = "" And PAC = "" Then
                        'cannot use
                        lblError.Text = "Access Denied. Your account has not been setup with a valid Responsibility Center. Please ask your accounts team or administrator to setup your staff profile."
                        lblError.Visible = True
                        pnlEverything.Visible = False
                        Return
                    End If


                End If



                Dim q = From c In ds.AP_StaffBroker_Staffs Where (c.UserId1 = UserId Or c.UserId2 = UserId) And (Not PayOnly) And c.CostCenter.Trim().Length > 0 And c.PortalId = PortalId Select DisplayName = (c.DisplayName & "(" & c.CostCenter & ")"), c.CostCenter, ViewOrder = 1
                q = q.Union(From c In ds.AP_StaffBroker_Departments Where c.CanRmb = True And c.CostCentre.Length > 0 And c.PortalId = PortalId Select DisplayName = (c.Name & "(" & c.CostCentre & ")"), CostCenter = c.CostCentre, ViewOrder = 2)

                'ddlNewChargeTo.DataSource = From c In q Order By c.ViewOrder, c.DisplayName
                'ddlNewChargeTo.DataTextField = "DisplayName"
                'ddlNewChargeTo.DataValueField = "CostCenter"
                'ddlNewChargeTo.DataBind()

                'ddlChargeTo.DataSource = From c In q Order By c.ViewOrder, c.DisplayName
                'ddlChargeTo.DataTextField = "DisplayName"
                'ddlChargeTo.DataValueField = "CostCenter"
                'ddlChargeTo.DataBind()

                'Dim myArrayValue = "var allAccounts = " & StaffRmbFunctions.getAccounts(StaffRmbFunctions.logonFromId(PortalId, UserId))
                'Page.ClientScript.RegisterArrayDeclaration("allAccounts", myArrayValue)
                'Page.ClientScript.RegisterClientScriptBlock(GetType(Page), "allAccounts", myArrayValue)

                'Dim allAccounts = From a In ds.AP_StaffBroker_AccountCodes Where (a.AccountCodeType = AccountType.Exspnse)
                '                Select label = a.AccountCodeName, value = a.AccountCode
                '                Order By value

                GridView1.Columns(0).HeaderText = Translate("TransDate")
                GridView1.Columns(1).HeaderText = Translate("LineType")
                GridView1.Columns(2).HeaderText = Translate("Comment")
                GridView1.Columns(3).HeaderText = Translate("Amount")
                GridView1.Columns(4).HeaderText = Translate("ReceiptNo")



                Dim acc As Boolean = IsAccounts()
                ' btnDownloadBatch.Visible = acc
                btnAdvDownload.Visible = acc
                ' btnShowSuggestedPayments.Visible = acc
                tbCostcenter.Enabled = acc
                ddlAccountCode.Enabled = acc
                pnlAccountsOptions.Visible = acc
                pnlVAT.Visible = Settings("VatAttrib")

                lblHighlight.Visible = acc '--this is the label to indicate that you are on the finance team
                If acc Then
                    Dim errors = From c In d.AP_Staff_Rmbs Where c.PortalId = PortalId And c.Error = True And (c.Status = RmbStatus.PendingDownload Or c.Status = RmbStatus.DownloadFailed Or c.Status = RmbStatus.Approved)

                    If errors.Count > 0 Then
                        Dim s As String = ""
                        For Each rmb In errors
                            s &= "<a href='" & NavigateURL() & "?RmbNo=" & rmb.RMBNo & "'>R" & rmb.RID & "</a>, "
                        Next
                        lblErrors.Text = Translate("Errors") & Left(s, s.Length - 2)
                        lblErrors.Visible = True
                    Else

                        lblErrors.Visible = False
                    End If
                End If



                lblDefatulSettings.Visible = (Settings("isLoaded") <> "Yes")

                hfPortalId.Value = PortalId

                pnlMain.Visible = False
                pnlSplash.Visible = True

                '  btnSettings.Visible = IsEditable

                If hfRmbNo.Value <> "" Then
                    If CInt(hfRmbNo.Value) < 0 Then
                        Await LoadAdv(-hfRmbNo.Value)
                    Else
                        Await LoadRmb(hfRmbNo.Value)
                    End If
                Else
                    ltSplash.Text = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("RmbSplash", PortalId))
                    Await ResetMenuAsync()
                End If
            End If
        End Function

        Protected Sub UpdatePanel2_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles UpdatePanel2.Load
            Try
                Dim lt = From c In d.AP_Staff_RmbLineTypes Where c.LineTypeId = ddlLineTypes.SelectedValue

                If lt.Count > 0 Then

                    phLineDetail.Controls.Clear()
                    theControl = LoadControl(lt.First.ControlPath)
                    theControl.ID = "theControl"
                    phLineDetail.Controls.Add(theControl)

                End If
            Catch ex As Exception

            End Try
        End Sub
#End Region

#Region "Loading Functions"
        Public Async Function ResetMenuAsync() As Task
            Try
                Dim basicTask = LoadBasicMenuAsync()
                Dim supervisorTask = LoadSupervisorMenuAsync()
                Dim financeTask = LoadFinanceMenuAsync()

                Await basicTask
                Await supervisorTask
                Await financeTask

            Catch ex As Exception
                lblError.Text = "Error loading Menu: " & ex.Message
                lblError.Visible = True
            End Try
        End Function

        Private Async Function LoadBasicMenuAsync() As Task
            Try
                '*** EVERYONE SEES THIS STUFF ***
                Dim MenuSize = Settings("MenuSize")
                '--Highlight reimbursements that need more information in a bar at the top
                Dim MoreInfo = From c In d.AP_Staff_Rmbs
                                    Where c.MoreInfoRequested = True And c.Status <> RmbStatus.Processed And c.UserId = UserId And c.PortalId = PortalId
                                    Select c.UserRef, c.RID, c.RMBNo
                For Each row In MoreInfo
                    Dim hyp As New HyperLink()
                    hyp.CssClass = "ui-state-highlight ui-corner-all AgapeWarning"
                    hyp.Font.Size = FontUnit.Small
                    hyp.Font.Bold = True
                    hyp.Text = Translate("MoreInfo").Replace("[RMBNO]", row.RID).Replace("[USERREF]", row.UserRef)
                    hyp.NavigateUrl = NavigateURL() & "?RmbNo=" & row.RMBNo
                    PlaceHolder1.Controls.Add(hyp)
                Next

                '--Drafts
                Dim Pending = (From c In d.AP_Staff_Rmbs
                               Where c.Status = RmbStatus.Draft And c.PortalId = PortalId And (c.UserId = UserId)
                               Order By c.RID Descending
                               Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(MenuSize)
                dlPending.DataSource = Pending
                dlPending.DataBind()

                '--Submitted
                Dim Submitted = (From c In d.AP_Staff_Rmbs
                                 Where c.Status = RmbStatus.Submitted And c.UserId = UserId And c.PortalId = PortalId
                                 Order By c.RID Descending
                                 Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(MenuSize)
                dlSubmitted.DataSource = Submitted
                dlSubmitted.DataBind()

                Dim AdvSubmitted = (From c In d.AP_Staff_AdvanceRequests
                                    Where c.RequestStatus = RmbStatus.Submitted And c.UserId = UserId And c.PortalId = PortalId
                                    Order By c.LocalAdvanceId Descending
                                    Select c.AdvanceId, c.RequestDate, c.LocalAdvanceId, c.UserId).Take(MenuSize)
                dlAdvSubmitted.DataSource = AdvSubmitted
                dlAdvSubmitted.DataBind()
                dlAdvSubmitted.AlternatingItemStyle.CssClass = IIf(dlSubmitted.Items.Count Mod 2 = 1, "dnnGridItem", "dnnGridAltItem")
                dlAdvSubmitted.ItemStyle.CssClass = IIf(dlSubmitted.Items.Count Mod 2 = 1, "dnnGridAltItem", "dnnGridItem")

                Dim submitted_count = Submitted.Count + AdvSubmitted.Count

                '--Approved
                Dim Approved = (From c In d.AP_Staff_Rmbs
                                Where (c.Status = RmbStatus.Approved Or c.Status = RmbStatus.PendingDownload Or c.Status = RmbStatus.DownloadFailed) _
                                    And c.UserId = UserId And c.PortalId = PortalId
                                Order By c.RID Descending
                                Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(MenuSize)
                dlApproved.DataSource = Approved
                dlApproved.DataBind()

                Dim AdvApproved = (From c In d.AP_Staff_AdvanceRequests
                                   Where (c.RequestStatus = RmbStatus.Approved Or c.RequestStatus = RmbStatus.PendingDownload Or c.RequestStatus = RmbStatus.DownloadFailed) _
                                        And c.UserId = UserId And c.PortalId = PortalId
                                   Order By c.LocalAdvanceId Descending
                                   Select c.AdvanceId, c.RequestDate, c.LocalAdvanceId, c.UserId).Take(MenuSize)
                dlAdvApproved.DataSource = AdvApproved
                dlAdvApproved.DataBind()
                dlAdvApproved.AlternatingItemStyle.CssClass = IIf(dlApproved.Items.Count Mod 2 = 1, "dnnGridItem", "dnnGridAltItem")
                dlAdvApproved.ItemStyle.CssClass = IIf(dlApproved.Items.Count Mod 2 = 1, "dnnGridAltItem", "dnnGridItem")

                '--Processed 
                Dim Complete = (From c In d.AP_Staff_Rmbs
                                Where c.Status = RmbStatus.Processed And c.UserId = UserId And c.PortalId = PortalId
                                Order By c.RID Descending
                                Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(MenuSize)
                dlProcessed.DataSource = Complete
                dlProcessed.DataBind()

                Dim CompleteAdv = (From c In d.AP_Staff_AdvanceRequests
                                   Where c.RequestStatus = RmbStatus.Processed And c.UserId = UserId And c.PortalId = PortalId
                                   Order By c.LocalAdvanceId Descending
                                   Select c.AdvanceId, c.RequestDate, c.LocalAdvanceId, c.UserId).Take(MenuSize)
                dlAdvProcessed.DataSource = CompleteAdv
                dlAdvProcessed.DataBind()


                '--Cancelled
                Dim Cancelled = (From c In d.AP_Staff_Rmbs
                                 Where c.Status = RmbStatus.Cancelled And c.UserId = UserId And c.PortalId = PortalId
                                 Order By c.RID Descending
                                 Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(MenuSize)
                dlCancelled.DataSource = Cancelled
                dlCancelled.DataBind()

                '***APPROVERS***

                '--list any unapproved reimbursements submitted to this user for approval
                Dim ApprovableRmbs = (From c In d.AP_Staff_Rmbs
                            Where c.Status = RmbStatus.Submitted And c.ApprUserId = UserId And c.ApprDate Is Nothing And c.PortalId = PortalId
                            Order By c.RMBNo Descending
                            Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId)
                dlToApprove.DataSource = ApprovableRmbs
                dlToApprove.DataBind()

                Dim ApprovableAdvs = (From c In d.AP_Staff_AdvanceRequests
                             Where c.RequestStatus = RmbStatus.Submitted And c.ApproverId = UserId And c.ApprovedDate Is Nothing And c.PortalId = PortalId
                             Order By c.LocalAdvanceId Descending
                             Select c.AdvanceId, c.RequestDate, c.LocalAdvanceId, c.UserId)
                dlAdvToApprove.DataSource = ApprovableAdvs
                dlAdvToApprove.DataBind()
                dlAdvToApprove.AlternatingItemStyle.CssClass = IIf(dlToApprove.Items.Count Mod 2 = 1, "dnnGridItem", "dnnGridAltItem")
                dlAdvToApprove.ItemStyle.CssClass = IIf(dlToApprove.Items.Count Mod 2 = 1, "dnnGridAltItem", "dnnGridItem")

                Dim approvable_count = ApprovableRmbs.Count + ApprovableAdvs.Count
                Dim isApprover = (approvable_count > 0)

                '-- Add a count of items to the 'Submitted' heading
                submitted_count += approvable_count
                If submitted_count > 0 Then
                    lblSubmittedCount.Text = "(" & submitted_count & ")"
                    pnlSubmitted.CssClass = "ui-state-highlight ui-corner-all"
                Else
                    lblSubmittedCount.Text = ""
                    pnlSubmitted.CssClass = ""
                End If

                If isApprover Then
                    lblApproveHeading.Visible = True
                    divApproveHeading.Visible = True
                Else
                    lblApproveHeading.Visible = False
                    divApproveHeading.Visible = False
                End If

            Catch ex As Exception
                Throw New Exception("Error loading basic menu: " + ex.Message)
            End Try
        End Function

        Private Async Function LoadSupervisorMenuAsync() As Task
            Try
                '***SUPERVISORS***

                Dim Team = StaffBrokerFunctions.GetTeam(UserId)
                Dim isSupervisor = (Team.Count > 0)
                If isSupervisor Then
                    Dim TeamIds = From c In Team Select c.UserID

                    '--Team Approved (build a tree)
                    Dim TeamApprovedNode As New TreeNode("Your Team")
                    TeamApprovedNode.SelectAction = TreeNodeSelectAction.Expand
                    TeamApprovedNode.Expanded = False

                    For Each team_member In Team
                        Dim TeamMemberApprovedNode As New TreeNode(team_member.DisplayName)
                        TeamMemberApprovedNode.SelectAction = TreeNodeSelectAction.Expand
                        TeamMemberApprovedNode.Expanded = False

                        Dim TeamApproved = From c In d.AP_Staff_Rmbs
                                           Where c.UserId = team_member.UserID _
                                                And (c.Status = RmbStatus.Approved Or c.Status = RmbStatus.PendingDownload Or c.Status = RmbStatus.DownloadFailed) _
                                                And c.UserId <> UserId And c.PortalId = PortalId
                                           Select c.RMBNo, c.RmbDate, c.UserRef, c.UserId, c.RID
                        For Each rmb In TeamApproved
                            Dim rmb_node As New TreeNode()
                            Dim rmbUser = UserController.GetUserById(PortalId, rmb.UserId).DisplayName
                            If (rmb.RmbDate Is Nothing) Then
                                rmb_node.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(rmb.RID, New Date(), rmbUser) & "</span>"
                            Else
                                rmb_node.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(rmb.RID, rmb.RmbDate, rmbUser) & "</span>"
                            End If
                            rmb_node.NavigateUrl = NavigateURL() & "?RmbNo=" & rmb.RMBNo
                            TeamMemberApprovedNode.ChildNodes.Add(rmb_node)
                            If IsSelected(rmb.RMBNo) Then
                                TeamMemberApprovedNode.Expanded = True
                                TeamApprovedNode.Expanded = True
                            End If
                        Next

                        Dim TeamAdvApproved = From c In d.AP_Staff_AdvanceRequests
                                              Where c.UserId = team_member.UserID _
                                                And (c.RequestStatus = RmbStatus.Approved Or c.RequestStatus = RmbStatus.PendingDownload Or c.RequestStatus = RmbStatus.DownloadFailed) _
                                                And c.UserId <> UserId And c.PortalId = PortalId
                                              Select c.AdvanceId, c.RequestDate, c.UserId, c.LocalAdvanceId
                        For Each adv In TeamAdvApproved
                            Dim adv_node As New TreeNode()
                            Dim advUser = UserController.GetUserById(PortalId, adv.UserId).DisplayName
                            If (adv.RequestDate Is Nothing) Then
                                adv_node.Text = "<span onClick='show_loading_spinner()'>" & GetAdvTitleTeamShort(adv.LocalAdvanceId, New Date(), advUser) & "</span>"
                            Else
                                adv_node.Text = "<span onClick='show_loading_spinner()'>" & GetAdvTitleTeamShort(adv.LocalAdvanceId, adv.RequestDate, advUser) & "</span>"
                            End If
                            adv_node.NavigateUrl = NavigateURL() & "?RmbNo=" & -adv.AdvanceId
                            TeamMemberApprovedNode.ChildNodes.Add(adv_node)
                            If IsSelected(-adv.AdvanceId) Then
                                TeamMemberApprovedNode.Expanded = True
                                TeamApprovedNode.Expanded = True
                            End If
                        Next
                        TeamApprovedNode.ChildNodes.Add(TeamMemberApprovedNode)
                    Next
                    tvTeamApproved.Nodes.Clear()
                    tvTeamApproved.Nodes.Add(TeamApprovedNode)

                    '--Team Processed (build a tree)
                    Dim TeamProcessedNode As New TreeNode("Your Team")
                    TeamProcessedNode.SelectAction = TreeNodeSelectAction.Expand
                    TeamProcessedNode.Expanded = False

                    For Each team_member In Team
                        Dim TeamMemberProcessedNode As New TreeNode(team_member.DisplayName)
                        TeamMemberProcessedNode.Expanded = False
                        TeamMemberProcessedNode.SelectAction = TreeNodeSelectAction.Expand

                        Dim TeamProcessed = From c In d.AP_Staff_Rmbs
                                            Join b In d.AP_StaffBroker_CostCenters
                                                On c.CostCenter Equals b.CostCentreCode _
                                                    And c.PortalId Equals b.PortalId
                                            Where c.UserId = team_member.UserID And c.Status = RmbStatus.Processed And b.Type = CostCentreType.Staff And c.PortalId = PortalId
                                            Select c.RMBNo, c.RmbDate, c.UserRef, c.UserId, c.RID
                        For Each rmb In TeamProcessed
                            Dim rmb_node As New TreeNode()
                            Dim rmbUser = UserController.GetUserById(PortalId, rmb.UserId).DisplayName
                            If (rmb.RmbDate Is Nothing) Then
                                rmb_node.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(rmb.RID, New Date(), rmbUser) & "</span>"
                            Else
                                rmb_node.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(rmb.RID, rmb.RmbDate, rmbUser) & "</span>"
                            End If
                            rmb_node.NavigateUrl = NavigateURL() & "?RmbNo=" & rmb.RMBNo
                            TeamMemberProcessedNode.ChildNodes.Add(rmb_node)
                            If IsSelected(rmb.RMBNo) Then
                                TeamMemberProcessedNode.Expanded = True
                                TeamProcessedNode.Expanded = True
                            End If
                        Next

                        Dim TeamAdvProcessed = From c In d.AP_Staff_AdvanceRequests
                                               Where c.RequestStatus = RmbStatus.Processed And c.UserId = team_member.UserID And c.PortalId = PortalId
                                               Select c.AdvanceId, c.RequestDate, c.UserId, c.LocalAdvanceId
                        For Each adv In TeamAdvProcessed
                            Dim adv_node As New TreeNode()
                            Dim advUser = UserController.GetUserById(PortalId, adv.UserId).DisplayName
                            If (adv.RequestDate Is Nothing) Then
                                adv_node.Text = "<span onClick='show_loading_spinner()'>" & GetAdvTitleTeamShort(adv.LocalAdvanceId, New Date(), advUser) & "</span>"
                            Else
                                adv_node.Text = "<span onClick='show_loading_spinner()'>" & GetAdvTitleTeamShort(adv.LocalAdvanceId, adv.RequestDate, advUser) & "</span>"
                            End If
                            adv_node.NavigateUrl = NavigateURL() & "?RmbNo=" & -adv.AdvanceId
                            TeamMemberProcessedNode.ChildNodes.Add(adv_node)
                            If IsSelected(-adv.AdvanceId) Then
                                TeamMemberProcessedNode.Expanded = True
                                TeamProcessedNode.Expanded = True
                            End If
                        Next
                        TeamProcessedNode.ChildNodes.Add(TeamMemberProcessedNode)

                    Next
                    tvTeamProcessed.Nodes.Clear()
                    tvTeamProcessed.Nodes.Add(TeamProcessedNode)

                    tvTeamApproved.Visible = True
                    tvTeamProcessed.Visible = True

                Else '--They are not a supervisor
                    tvTeamApproved.Visible = False
                    tvTeamProcessed.Visible = False
                End If

            Catch ex As Exception
                Throw New Exception("Error loading supervisor menu: " + ex.Message)
            End Try
        End Function

        Private Async Function LoadFinanceMenuAsync() As Task
            Try
                '***FINANCE DEPARTMENT***
                Dim MenuSize = Settings("MenuSize")
                Dim isFinance = IsAccounts()
                If isFinance Then
                    Dim allStaff = StaffBrokerFunctions.GetStaff()

                    '--Submitted / Processed (build trees)
                    Dim AllStaffSubmittedNode As New TreeNode("All Staff")
                    Dim AllStaffProcessedNode As New TreeNode("All Staff")
                    AllStaffSubmittedNode.SelectAction = TreeNodeSelectAction.Expand
                    AllStaffProcessedNode.SelectAction = TreeNodeSelectAction.Expand
                    AllStaffSubmittedNode.Expanded = False
                    AllStaffProcessedNode.Expanded = False

                    For Each person In allStaff

                        '-- sort by first letter of last name
                        Dim letter = person.LastName.Substring(0, 1)

                        '--here are the submitted reimbursements & advances
                        Dim SubmittedRmb = (From c In d.AP_Staff_Rmbs Where c.Status = RmbStatus.Submitted And c.PortalId = PortalId And (c.UserId = person.UserID) Order By c.RID Descending Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(MenuSize)
                        Dim SubmittedAdv = (From c In d.AP_Staff_AdvanceRequests Where c.RequestStatus = RmbStatus.Submitted And c.PortalId = PortalId And c.UserId = person.UserID Order By c.LocalAdvanceId Descending Select c.AdvanceId, c.RequestDate, c.LocalAdvanceId).Take(MenuSize)
                        If SubmittedRmb.Count() > 0 Or SubmittedAdv.Count() > 0 Then
                            Dim submittedNode As New TreeNode(person.DisplayName)
                            If SubmittedRmb.Count() > 0 Then
                                addItemsToTree(AllStaffSubmittedNode, submittedNode, letter, SubmittedRmb, "rmb")
                            End If
                            If SubmittedAdv.Count() > 0 Then
                                addItemsToTree(AllStaffSubmittedNode, submittedNode, letter, SubmittedAdv, "adv")
                            End If
                        End If
                        '--here are the processed reimbursements & advances
                        Dim ProcessedRmb = (From c In d.AP_Staff_Rmbs Where c.Status = RmbStatus.Processed And c.PortalId = PortalId And (c.UserId = person.UserID) Order By c.RID Descending Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(MenuSize)
                        Dim ProcessedAdv = (From c In d.AP_Staff_AdvanceRequests Where c.RequestStatus = RmbStatus.Processed And c.PortalId = PortalId And c.UserId = person.UserID Order By c.LocalAdvanceId Descending Select c.AdvanceId, c.RequestDate, c.LocalAdvanceId).Take(MenuSize)
                        If (ProcessedRmb.Count() > 0 Or ProcessedAdv.Count() > 0) Then
                            Dim processedNode As New TreeNode(person.DisplayName)
                            If ProcessedRmb.Count() > 0 Then
                                addItemsToTree(AllStaffProcessedNode, processedNode, letter, ProcessedRmb, "rmb")
                            End If
                            If ProcessedAdv.Count() > 0 Then
                                addItemsToTree(AllStaffProcessedNode, processedNode, letter, ProcessedAdv, "adv")
                            End If
                        End If

                    Next
                    tvAllSubmitted.Nodes.Clear()
                    tvAllSubmitted.Nodes.Add(AllStaffSubmittedNode)
                    tvAllProcessed.Nodes.Clear()
                    tvAllProcessed.Nodes.Add(AllStaffProcessedNode)

                    '--This is the key part for the FINANCE team (approved, but not processed requests)
                    '--lookup all approved reimbursements & advances
                    Dim finance_node As New TreeNode("Finance")
                    finance_node.SelectAction = TreeNodeSelectAction.Expand
                    finance_node.Expanded = False

                    Dim AllApproved = (From c In d.AP_Staff_Rmbs
                                       Where (c.Status = RmbStatus.Approved Or c.Status >= RmbStatus.PendingDownload) And c.PortalId = PortalId
                                       Order By c.RID Descending
                                       Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId, c.Status, _
                                           Receipts = ((c.AP_Staff_RmbLines.Where(Function(x) x.Receipt And (x.ReceiptImageId Is Nothing))).Count > 0)).Take(MenuSize)
                    Dim AllApprovedAdv = (From c In d.AP_Staff_AdvanceRequests
                                          Where (c.RequestStatus = RmbStatus.Approved Or c.RequestStatus >= RmbStatus.PendingDownload) And c.PortalId = PortalId
                                          Order By c.LocalAdvanceId Descending).Take(MenuSize)

                    Dim rec = From c In AllApproved Where c.Status = RmbStatus.Approved And c.Receipts
                    Dim nonRec = From c In AllApproved Where c.Status = RmbStatus.Approved And Not c.Receipts
                    Dim nonRecAdv = From c In AllApprovedAdv Where c.RequestStatus = RmbStatus.Approved
                    Dim PendingDownload = From c In AllApproved Where c.Status >= RmbStatus.PendingDownload
                    Dim PendingDownloadAdv = From c In AllApprovedAdv Where c.RequestStatus >= RmbStatus.PendingDownload
                    Dim receipts_node As New TreeNode("Receipts (" & rec.Count & ")")
                    receipts_node.SelectAction = TreeNodeSelectAction.Expand
                    receipts_node.Expanded = False
                    For Each rmb In rec
                        Dim rmb_node As New TreeNode()
                        Dim rmbUser = UserController.GetUserById(PortalId, rmb.UserId).DisplayName
                        If (rmb.RmbDate Is Nothing) Then
                            rmb_node.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(rmb.RID, New Date(), rmbUser) & "</span>"
                        Else
                            rmb_node.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(rmb.RID, rmb.RmbDate, rmbUser) & "</span>"
                        End If
                        rmb_node.NavigateUrl = NavigateURL() & "?RmbNo=" & rmb.RMBNo
                        receipts_node.ChildNodes.Add(rmb_node)
                        If IsSelected(rmb.RMBNo) Then
                            receipts_node.Expanded = True
                            finance_node.Expanded = True
                        End If
                    Next

                    Dim no_receipts_node As New TreeNode("No Receipts (" & nonRec.Count + nonRecAdv.Count & ")")
                    no_receipts_node.SelectAction = TreeNodeSelectAction.Expand
                    no_receipts_node.Expanded = False
                    For Each rmb In nonRec
                        Dim rmb_node As New TreeNode()
                        Dim rmbUser = UserController.GetUserById(PortalId, rmb.UserId).DisplayName
                        If (rmb.RmbDate Is Nothing) Then
                            rmb_node.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(rmb.RID, New Date(), rmbUser) & "</span>"
                        Else
                            rmb_node.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(rmb.RID, rmb.RmbDate, rmbUser) & "</span>"
                        End If
                        rmb_node.NavigateUrl = NavigateURL() & "?RmbNo=" & rmb.RMBNo
                        no_receipts_node.ChildNodes.Add(rmb_node)
                        If IsSelected(rmb.RMBNo) Then
                            no_receipts_node.Expanded = True
                            finance_node.Expanded = True
                        End If
                    Next
                    For Each adv In nonRecAdv
                        Dim adv_node As New TreeNode()
                        Dim advUser = UserController.GetUserById(PortalId, adv.UserId).DisplayName
                        If (adv.RequestDate Is Nothing) Then
                            adv_node.Text = "<span onClick='show_loading_spinner()'>" & GetAdvTitleTeamShort(adv.LocalAdvanceId, New Date(), advUser) & "</span>"
                        Else
                            adv_node.Text = "<span onClick='show_loading_spinner()'>" & GetAdvTitleTeamShort(adv.LocalAdvanceId, adv.RequestDate, advUser) & "</span>"
                        End If
                        adv_node.NavigateUrl = NavigateURL() & "?RmbNo=" & -adv.AdvanceId
                        no_receipts_node.ChildNodes.Add(adv_node)
                        If IsSelected(-adv.AdvanceId) Then
                            no_receipts_node.Expanded = True
                            finance_node.Expanded = True
                        End If
                    Next

                    Dim pending_download_node As New TreeNode("Pending Download (" & PendingDownload.Count + PendingDownloadAdv.Count & ")")
                    pending_download_node.SelectAction = TreeNodeSelectAction.Expand
                    pending_download_node.Expanded = False
                    For Each rmb In PendingDownload
                        Dim rmb_node As New TreeNode()
                        Dim rmbUser = UserController.GetUserById(PortalId, rmb.UserId).DisplayName
                        If (rmb.RmbDate Is Nothing) Then
                            rmb_node.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(rmb.RID, New Date(), rmbUser) & "</span>"
                        Else
                            rmb_node.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(rmb.RID, rmb.RmbDate, rmbUser) & "</span>"
                        End If
                        rmb_node.NavigateUrl = NavigateURL() & "?RmbNo=" & rmb.RMBNo
                        pending_download_node.ChildNodes.Add(rmb_node)
                        If IsSelected(rmb.RMBNo) Then
                            pending_download_node.Expanded = True
                            finance_node.Expanded = True
                        End If
                    Next
                    For Each adv In PendingDownloadAdv
                        Dim adv_node As New TreeNode()
                        Dim advUser = UserController.GetUserById(PortalId, adv.UserId).DisplayName
                        If (adv.RequestDate Is Nothing) Then
                            adv_node.Text = "<span onClick='show_loading_spinner()'>" & GetAdvTitleTeamShort(adv.LocalAdvanceId, New Date(), advUser) & "</span>"
                        Else
                            adv_node.Text = "<span onClick='show_loading_spinner()'>" & GetAdvTitleTeamShort(adv.LocalAdvanceId, adv.RequestDate, advUser) & "</span>"
                        End If
                        adv_node.NavigateUrl = NavigateURL() & "?RmbNo=" & -adv.AdvanceId
                        pending_download_node.ChildNodes.Add(adv_node)
                        If IsSelected(-adv.AdvanceId) Then
                            pending_download_node.Expanded = True
                            finance_node.Expanded = True
                        End If
                    Next

                    finance_node.ChildNodes.Add(receipts_node)
                    finance_node.ChildNodes.Add(no_receipts_node)
                    finance_node.ChildNodes.Add(pending_download_node)

                    tvFinance.Nodes.Clear()
                    tvFinance.Nodes.Add(finance_node)


                    '-- Add a count of items to the 'Approved' heading
                    If AllApproved.Count + AllApprovedAdv.Count > 0 Then
                        lblToProcess.Text = "(" & AllApproved.Count + AllApprovedAdv.Count & ")"
                        pnlToProcess.CssClass = "ui-state-highlight ui-corner-all"
                    Else
                        lblToProcess.Text = ""
                        pnlToProcess.CssClass = ""
                    End If

                    tvFinance.Visible = True
                    tvAllSubmitted.Visible = True
                    tvAllProcessed.Visible = True
                Else
                    tvFinance.Visible = False
                    tvAllSubmitted.Visible = False
                    tvAllProcessed.Visible = False
                End If
                lblApprovedDivider.Visible = (tvTeamApproved.Visible)
                lblProcessedDivider.Visible = (tvFinance.Visible Or tvAllProcessed.Visible Or tvTeamProcessed.Visible)
                lblYourProcessed.Visible = lblProcessedDivider.Visible
            Catch ex As Exception
                Throw New Exception("Error loading finance menu: " + ex.Message)
            End Try
        End Function

        Public Async Function LoadAdv(ByVal AdvanceId As Integer) As Task
            Try
                pnlMain.Visible = False
                pnlMainAdvance.Visible = True
                pnlSplash.Visible = False
                hfRmbNo.Value = -AdvanceId

                Dim q = From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = AdvanceId And c.PortalId = PortalId

                If q.Count > 0 Then
                    Dim advRel As Integer = StaffRmbFunctions.AuthenticateAdv(UserId, q.First.AdvanceId, PortalId)
                    If advRel = RmbAccess.Denied And Not IsAccounts() And Not (UserId = Settings("AuthUser") Or UserId = Settings("AuthAuthUser")) Then
                        pnlMain.Visible = False
                        'Need an access denied warning
                        pnlSplash.Visible = True
                        Return
                    End If


                    lblAdvanceId.Text = ZeroFill(q.First.LocalAdvanceId, 5)
                    imgAdvAvatar.ImageUrl = GetProfileImage(q.First.UserId)
                    lblAdvStatus.Text = Translate(RmbStatus.StatusName(q.First.RequestStatus))
                    Dim StaffMember = UserController.GetUserById(PortalId, q.First.UserId)



                    lblAdvCur.Text = "" 'StaffBrokerFunctions.GetSetting("Currency", PortalId)
                    Dim ac = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)
                    hfAccountingCurrency.Value = ac
                    hfOrigCurrency.Value = ac
                    hfOrigCurrencyValue.Value = q.First.RequestAmount
                    If Not String.IsNullOrEmpty(q.First.OrigCurrency) Then
                        If q.First.OrigCurrency.ToUpper <> ac.ToUpper Then
                            lblAdvCur.Text = q.First.OrigCurrencyAmount.Value.ToString("0.00") & " " & q.First.OrigCurrency.ToUpper

                            hfOrigCurrency.Value = q.First.OrigCurrency
                            hfOrigCurrencyValue.Value = q.First.OrigCurrencyAmount.Value.ToString("0.00")


                            Dim jscript As String = ""

                            hfOrigCurrencyValue.Value = q.First.OrigCurrencyAmount
                            jscript &= " $('.currency').attr('value'," & q.First.OrigCurrencyAmount & ");"

                            hfOrigCurrency.Value = q.First.OrigCurrency

                            jscript &= " $('.ddlCur').val('" & q.First.OrigCurrency & "'); checkCur();"

                            hfExchangeRate.Value = q.First.RequestAmount / q.First.OrigCurrencyAmount
                            Dim t As Type = AdvAmount.GetType()
                            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                            sb.Append("<script language='javascript'>")
                            sb.Append(jscript)
                            sb.Append("</script>")
                            ScriptManager.RegisterStartupScript(AdvAmount, t, "loadEditAdvCur", sb.ToString, False)
                        End If
                    End If


                    AdvAmount.Text = q.First.RequestAmount.Value.ToString("0.00")
                    AdvReason.Text = q.First.RequestText
                    AdvDate.Text = Translate("AdvDate").Replace("[DATE]", q.First.RequestDate.Value.ToShortDateString)
                    If q.First.ApprovedDate Is Nothing Then
                        Await updateApproversListAsync(q.First)
                    Else
                        Dim Approver = UserController.GetUserById(PortalId, q.First.ApproverId).DisplayName
                        AdvDate.Text &= "<br />" & Translate("AdvApproved").Replace("[DATE]", q.First.ApprovedDate.Value.ToShortDateString).Replace("[STAFFNAME]", Approver)
                    End If
                    If Not q.First.ProcessedDate Is Nothing Then
                        AdvDate.Text &= "<br />" & Translate("AdvProcessed").Replace("[DATE]", q.First.ProcessedDate.Value.ToShortDateString)
                    End If

                    Dim theStaff = StaffBrokerFunctions.GetStaffMember(q.First.UserId)


                    AccBal.Text = "Unknown"
                    AdvBal.Text = "Unknown"
                    Dim AdvPay = From c In ds.AP_Staff_SuggestedPayments Where c.PortalId = PortalId And c.CostCenter.StartsWith(theStaff.CostCenter)

                    If AdvPay.Count > 0 Then
                        If Not AdvPay.First.AdvanceBalance Is Nothing Then
                            AdvBal.Text = StaffBrokerFunctions.GetFormattedCurrency(PortalId, AdvPay.First.AdvanceBalance.Value.ToString("0.00"))
                        End If
                        If Not AdvPay.First.AccountBalance Is Nothing Then
                            AccBal.Text = StaffBrokerFunctions.GetFormattedCurrency(PortalId, AdvPay.First.AccountBalance.Value.ToString("0.00"))
                        End If
                    End If




                    '   AccBal.Text = StaffBrokerFunctions.GetSetting("Currency", PortalId) & "3000"
                    '   AdvBal.Text = StaffBrokerFunctions.GetSetting("Currency", PortalId) & "150"

                    Select Case q.First.RequestStatus

                        Case RmbStatus.Submitted
                            btnAdvApprove.Visible = (q.First.UserId <> UserId)
                            btnAdvReject.Visible = (q.First.UserId <> UserId)
                            btnAdvSave.Visible = (q.First.UserId = UserId)
                            btnAdvCancel.Visible = (q.First.UserId = UserId)
                            btnAdvProcess.Visible = False
                            btnAdvUnProcess.Visible = False
                            If q.First.UserId = UserId Then
                                lblAdv1.Visible = False
                            Else
                                lblAdv1.Visible = True
                                lblAdv1.Text = Translate("AdvIntro").Replace("[STAFFNAME]", StaffMember.DisplayName)

                            End If

                        Case RmbStatus.Approved
                            btnAdvApprove.Visible = False
                            btnAdvCancel.Visible = False
                            btnAdvUnProcess.Visible = False
                            If IsAccounts() Then
                                btnAdvReject.Visible = True
                                btnAdvSave.Visible = True
                                btnAdvProcess.Visible = True

                            Else
                                btnAdvSave.Visible = False
                                btnAdvReject.Visible = False
                                btnAdvProcess.Visible = False

                            End If

                        Case RmbStatus.PendingDownload, RmbStatus.DownloadFailed
                            btnAdvApprove.Visible = False
                            btnAdvCancel.Visible = False
                            btnAdvProcess.Visible = False
                            btnAdvReject.Visible = False
                            btnAdvSave.Visible = False
                            If IsAccounts() Then

                                btnAdvUnProcess.Visible = True
                            Else

                                btnAdvUnProcess.Visible = False
                            End If

                        Case RmbStatus.Processed
                            btnAdvApprove.Visible = False
                            btnAdvCancel.Visible = False
                            btnAdvProcess.Visible = False
                            btnAdvReject.Visible = False
                            btnAdvSave.Visible = False
                            If IsAccounts() Then

                                btnAdvUnProcess.Visible = True
                            Else

                                btnAdvUnProcess.Visible = False
                            End If

                        Case RmbStatus.Cancelled
                            btnAdvApprove.Visible = False
                            btnAdvReject.Visible = False
                            btnAdvSave.Visible = False
                            btnAdvCancel.Visible = False
                            btnAdvProcess.Visible = False
                            btnAdvUnProcess.Visible = False
                            btnAdvApprove.Visible = False
                            btnAdvCancel.Visible = False
                            btnAdvProcess.Visible = False
                            btnAdvReject.Visible = False
                            btnAdvSave.Visible = False


                        Case Else
                            btnAdvApprove.Visible = False
                            btnAdvReject.Visible = False
                            btnAdvSave.Visible = False
                            btnAdvCancel.Visible = False
                            btnAdvProcess.Visible = False
                            btnAdvUnProcess.Visible = False
                    End Select
                    lblAdvDownloadError.Visible = False
                    If IsAccounts() Then
                        pnlAdvPeriodYear.Visible = True
                        SetYear(ddlAdvYear, q.First.Year)

                        If q.First.Error And Not String.IsNullOrEmpty(q.First.ErrorMessage) Then
                            lblAdvDownloadError.Text = q.First.ErrorMessage
                            lblAdvDownloadError.Visible = True
                        ElseIf Not String.IsNullOrEmpty(q.First.ErrorMessage) Then
                            AdvDate.Text &= "<br /> " & q.First.ErrorMessage
                        End If


                        If q.First.Period Is Nothing Then
                            ddlAdvPeriod.SelectedIndex = 0
                        Else
                            ddlAdvPeriod.SelectedValue = q.First.Period
                        End If
                    Else
                        pnlAdvPeriodYear.Visible = False
                    End If
                    AdvAmount.Enabled = btnAdvSave.Visible
                End If
                Await ResetMenuAsync()
            Catch ex As Exception
                lblError.Text = "Error loading Advance: " & ex.Message
                lblError.Visible = True
            End Try
        End Function

        Public Sub SetYear(ByRef ddlYearIn As DropDownList, ByVal selectedYear As Integer?)
            ddlYearIn.Items.Clear()
            ddlYearIn.Items.Add(New ListItem("Default", ""))
            ddlYearIn.Items.Add(Year(Today) - 1)
            ddlYearIn.Items.Add(Year(Today))
            ddlYearIn.Items.Add(Year(Today) + 1)
            If selectedYear Is Nothing Then
                ddlYearIn.SelectedIndex = 0
            Else
                If selectedYear < Year(Today) - 1 Then
                    ddlYearIn.Items.Insert(0, selectedYear)
                ElseIf selectedYear > Year(Today) + 1 Then
                    ddlYearIn.Items.Add(selectedYear)
                End If
                ddlYearIn.SelectedValue = selectedYear
            End If
        End Sub

        Public Async Function LoadRmb(ByVal RmbNo As Integer) As Task

            pnlMain.Visible = True
            pnlMainAdvance.Visible = False
            pnlSplash.Visible = False
            '--Set visibility and enabled attributes for different parts of the form
            '--Based on form state and user privileges
            Try
                hfRmbNo.Value = RmbNo
                Dim q = From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo
                If q.Count > 0 Then
                    Dim Rmb = q.First
                    Dim updateApproversTask = updateApproversListAsync(Rmb)
                    Dim updateAccountBalanceTask = refreshAccountBalanceAsync(Rmb.CostCenter, StaffRmbFunctions.logonFromId(PortalId, UserId))

                    '--hidden fields
                    hfChargeToValue.Value = If(Rmb.CostCenter Is Nothing, "", Rmb.CostCenter)
                    hfAccountBalance.Value = 0

                    Dim DRAFT = Rmb.Status = RmbStatus.Draft
                    Dim MORE_INFO = (Rmb.MoreInfoRequested IsNot Nothing AndAlso Rmb.MoreInfoRequested = True)
                    Dim SUBMITTED = Rmb.Status = RmbStatus.Submitted
                    Dim APPROVED = Rmb.Status = RmbStatus.Approved
                    Dim PROCESSING = Rmb.Status = RmbStatus.PendingDownload Or Rmb.Status = RmbStatus.DownloadFailed
                    Dim PROCESSED = Rmb.Status = RmbStatus.Processed
                    Dim CANCELLED = Rmb.Status = RmbStatus.Cancelled
                    Dim FORM_HAS_ITEMS = Rmb.AP_Staff_RmbLines.Count > 0

                    Dim user = UserController.GetUserById(PortalId, Rmb.UserId)
                    Dim staff_member = StaffBrokerFunctions.GetStaffMember(Rmb.UserId)
                    Dim PACMode = (String.IsNullOrEmpty(staff_member.CostCenter) And StaffBrokerFunctions.GetStaffProfileProperty(staff_member.StaffId, "PersonalAccountCode") <> "")

                    Dim isOwner = (UserId = Rmb.UserId)
                    Dim isSpouse = (StaffBrokerFunctions.GetSpouseId(UserId) = Rmb.UserId)
                    Dim isApprover = (UserId = Rmb.ApprUserId) And Not isOwner
                    Dim isSupervisor = (Not isOwner) And StaffBrokerFunctions.isLeaderOf(UserId, Rmb.UserId)
                    Dim isFinance = IsAccounts() And Not isOwner

                    '--Ensure the user is authorized to view this reimbursement
                    Dim RmbRel As Integer
                    RmbRel = StaffRmbFunctions.Authenticate(UserId, RmbNo, PortalId)
                    If RmbRel = RmbAccess.Denied And Not (isApprover Or isFinance) Then
                        'Need an access denied warning
                        pnlMain.Visible = False
                        pnlSplash.Visible = True
                        Return
                    End If

                    SetYear(ddlYear, Rmb.Year)

                    '*** ERRORS ***
                    lblWrongType.Visible = False
                    pnlError.Visible = isFinance And Rmb.Error
                    lblErrorMessage.Text = Rmb.ErrorMessage & "<br /><i>" & Translate("ErrorHelp") & "</i>"

                    '*** TITLE ***
                    lblRmbNo.Text = ZeroFill(Rmb.RID, 5)
                    imgAvatar.ImageUrl = GetProfileImage(Rmb.UserId)
                    tbChargeTo.Text = If(Rmb.CostCenter Is Nothing, "", Rmb.CostCenter)
                    tbChargeTo.Enabled = DRAFT Or MORE_INFO Or CANCELLED Or (SUBMITTED And (isOwner Or isSpouse))
                    lblStatus.Text = Translate(RmbStatus.StatusName(Rmb.Status))
                    If (Rmb.MoreInfoRequested) Then
                        lblStatus.Text = lblStatus.Text & " - " & Translate("StatusMoreInfo")
                    End If
                    lblAccountBalance.Text = "Unknown"
                    lblAdvanceBalance.Text = "Unknown"

                    '*** FORM HEADER ***
                    '--dates
                    lblSubmittedDate.Text = If(Rmb.RmbDate Is Nothing, "", Rmb.RmbDate.Value.ToShortDateString)
                    lblSubBy.Text = user.DisplayName

                    lblApprovedDate.Text = If(Rmb.ApprDate Is Nothing, "", Rmb.ApprDate.Value.ToShortDateString)
                    ttlWaitingApp.Visible = Rmb.ApprDate Is Nothing
                    ttlApprovedBy.Visible = Not Rmb.ApprDate Is Nothing
                    ttlApprovedBy.Text = If(Rmb.ApprUserId Is Nothing Or Rmb.ApprUserId = -1, "", UserController.GetUserById(PortalId, Rmb.ApprUserId).DisplayName)
                    ddlApprovedBy.Visible = DRAFT Or MORE_INFO Or CANCELLED Or ((isApprover Or isOwner Or isSpouse) And SUBMITTED)
                    ddlApprovedBy.Enabled = DRAFT Or MORE_INFO Or CANCELLED Or ((isApprover Or isOwner Or isSpouse) And SUBMITTED)
                    lblApprovedBy.Visible = Not ddlApprovedBy.Visible

                    lblProcessedDate.Text = If(Rmb.ProcDate Is Nothing, "", Rmb.ProcDate.Value.ToShortDateString)
                    lblProcessedBy.Text = If(Rmb.ProcUserId Is Nothing, "", UserController.GetUserById(PortalId, Rmb.ProcUserId).DisplayName)

                    '--reference / period / year
                    tbYouRef.Enabled = Rmb.Status = RmbStatus.Draft
                    tbYouRef.Text = If(Rmb.UserRef Is Nothing, "", Rmb.UserRef)
                    pnlPeriodYear.Visible = isFinance And (APPROVED Or PROCESSING Or PROCESSED)
                    ddlPeriod.SelectedIndex = 0
                    If Not Rmb.Period Is Nothing Then
                        ddlPeriod.SelectedValue = Rmb.Period
                    End If

                    '--comments
                    ttlYourComments.Visible = (isOwner Or isSpouse)
                    tbComments.Visible = (isOwner Or isSpouse)
                    tbComments.Enabled = isOwner And Not (Rmb.Locked Or PROCESSING Or PROCESSED)
                    tbComments.Text = Rmb.UserComment
                    ttlUserComments.Visible = Not (isOwner Or isSpouse)
                    lblComments.Visible = Not (isOwner Or isSpouse)
                    lblComments.Text = Rmb.UserComment

                    lblApprComments.Visible = Not isApprover
                    lblApprComments.Text = If(Rmb.ApprComment Is Nothing, "", Rmb.ApprComment)
                    tbApprComments.Visible = isApprover
                    tbApprComments.Enabled = isApprover And Not (PROCESSING Or PROCESSED)
                    tbApprComments.Text = If(Rmb.ApprComment Is Nothing, "", Rmb.ApprComment)
                    cbApprMoreInfo.Visible = (isApprover And SUBMITTED)
                    cbApprMoreInfo.Checked = If(Rmb.MoreInfoRequested, Rmb.MoreInfoRequested, False)

                    lblAccComments.Visible = Not isFinance
                    lblAccComments.Text = If(Rmb.AcctComment Is Nothing, "", Rmb.AcctComment)
                    tbAccComments.Visible = isFinance
                    tbAccComments.Enabled = isFinance And Not (PROCESSING Or PROCESSED)
                    tbAccComments.Text = If(Rmb.AcctComment Is Nothing, "", Rmb.AcctComment)

                    cbMoreInfo.Visible = (isFinance And APPROVED)
                    cbMoreInfo.Checked = If(Rmb.MoreInfoRequested, Rmb.MoreInfoRequested, False)

                    '--buttons
                    btnSave.Text = Translate("btnSaved")
                    btnSave.Style.Add(HtmlTextWriterStyle.Display, "none") '--hide, but still generate the button
                    btnSaveAdv.Visible = Not (PROCESSING Or PROCESSED)
                    btnDelete.Visible = Not (PROCESSING Or PROCESSED Or CANCELLED)


                    '*** REIMBURSEMENT DETAILS ***
                    pnlTaxable.Visible = (From c In Rmb.AP_Staff_RmbLines Where c.Taxable = True).Count > 0

                    '--grid
                    staffInitials.Value = user.FirstName.Substring(0, 1) & user.LastName.Substring(0, 1)
                    GridView1.DataSource = Rmb.AP_Staff_RmbLines
                    GridView1.DataBind()

                    '--buttons
                    btnAddLine.Visible = (isOwner Or isSpouse) And Not (PROCESSING Or PROCESSED Or APPROVED)
                    addLinebtn2.Visible = (isOwner Or isSpouse) And Not (PROCESSING Or PROCESSED Or APPROVED)

                    btnPrint.Visible = FORM_HAS_ITEMS
                    btnPrint.OnClientClick = "window.open('/DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & RmbNo & "&UID=" & Rmb.UserId & "', '_blank'); "
                    btnSubmit.Visible = (isOwner Or isSpouse) And (DRAFT Or MORE_INFO Or CANCELLED) And FORM_HAS_ITEMS
                    btnSubmit.Text = If(DRAFT, Translate("btnSubmit"), Translate("btnResubmit"))
                    btnSubmit.Enabled = btnSubmit.Visible And Rmb.CostCenter IsNot Nothing And Rmb.ApprUserId IsNot Nothing AndAlso (Rmb.CostCenter.Length = 6) And (Rmb.ApprUserId >= 0)
                    btnSubmit.ToolTip = If(btnSubmit.Enabled, "", "Please select an account and an approver in order to submit")
                    btnSubmit.OnClientClick = "window.open('/DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & RmbNo & "&UID=" & Rmb.UserId & "&mode=1', '_blank'); "
                    btnApprove.Visible = isApprover And SUBMITTED
                    btnApprove.Enabled = btnApprove.Visible
                    btnProcess.Visible = isFinance And APPROVED
                    btnProcess.Enabled = btnProcess.Visible
                    btnUnProcess.Visible = isFinance And (PROCESSING Or PROCESSED)
                    btnUnProcess.Enabled = btnUnProcess.Visible
                    btnDownload.Visible = (isFinance Or isOwner Or isSpouse) And FORM_HAS_ITEMS


                    '*** ADVANCES ***

                    pnlAdvance.Visible = (Rmb.AP_Staff_RmbLines.Count > 0) And (Not PACMode) And ENABLE_ADVANCE_FUNCTIONALITY
                    If (ENABLE_ADVANCE_FUNCTIONALITY) Then
                        tbAdvanceAmount.Enabled = DRAFT Or MORE_INFO Or SUBMITTED Or APPROVED
                        tbAdvanceAmount.Text = If(Rmb.AdvanceRequest = Nothing, "", Rmb.AdvanceRequest.ToString("0.00", New CultureInfo("en-US").NumberFormat))

                        Dim qAdvPayments = From c In ds.AP_Staff_SuggestedPayments
                                     Where c.CostCenter.StartsWith(staff_member.CostCenter) And c.PortalId = PortalId
                        If (qAdvPayments.Count > 0) Then
                            If (qAdvPayments.First.AdvanceBalance IsNot Nothing) Then
                                lblAdvanceBalance.Text = StaffBrokerFunctions.GetFormattedCurrency(PortalId, qAdvPayments.First.AdvanceBalance.Value.ToString("0.00"))
                            End If
                        End If

                        Dim qAccPayments = From c In ds.AP_Staff_SuggestedPayments
                                     Where c.CostCenter.StartsWith(Rmb.CostCenter) And c.PortalId = PortalId
                        If (qAccPayments.Count > 0) Then
                            If (qAccPayments.First.AccountBalance IsNot Nothing) Then
                                lblAccountBalance.Text = StaffBrokerFunctions.GetFormattedCurrency(PortalId, qAccPayments.First.AccountBalance.Value.ToString("0.00"))
                                hfAccountBalance.Value = qAccPayments.First.AccountBalance.Value
                            End If
                        End If
                    End If
                    Await updateApproversTask
                    Await updateAccountBalanceTask
                Else
                    pnlMain.Visible = False
                    pnlSplash.Visible = True
                End If

            Catch ex As Exception
                lblError.Text = "Error loading Rmb: " & ex.Message & ex.StackTrace
                lblError.Visible = True
            End Try

        End Function

        Private Async Function updateApproversListAsync(ByVal obj As Object) As Task
            Dim approvers As Object
            Dim approverId = -1
            If (obj.GetType() Is GetType(AP_Staff_Rmb)) Then
                approvers = Await StaffRmbFunctions.getApproversAsync(obj, Nothing, Nothing)
                approverId = obj.ApprUserId
            Else
                approvers = Await StaffRmbFunctions.getAdvApproversAsync(obj, 0D, Nothing, Nothing)
                approverId = obj.ApproverId
            End If

            ddlApprovedBy.Items.Clear()
            Dim blank As ListItem
            blank = New ListItem("", "-1")
            blank.Attributes.Add("disabled", "disabled")
            ddlApprovedBy.Items.Add(blank)
            For Each row In approvers.UserIds
                If Not row Is Nothing Then
                    ddlApprovedBy.Items.Add(New ListItem(row.DisplayName, row.UserID))
                End If
            Next
            Try
                ddlApprovedBy.SelectedValue = approverId
                lblApprovedBy.Text = ddlApprovedBy.SelectedItem.ToString
            Catch ex As Exception
                ddlApprovedBy.SelectedValue = -1
                lblApprovedBy.Text = "[NOBODY]"
            End Try
        End Function
#End Region

#Region "Button Events"
        Protected Async Sub btnAddLine_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnAddLine.Click

            Dim ucType As Type = theControl.GetType()

            If btnAddLine.CommandName = "Save" Then
                Dim theUserId = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.UserId).First
                If ucType.GetMethod("ValidateForm").Invoke(theControl, New Object() {theUserId}) = True Then

                    Dim q = From c In d.AP_Staff_RmbLines Where c.RmbNo = hfRmbNo.Value And c.Receipt Select c.ReceiptNo

                    'Dim AccType = Right(ddlChargeTo.SelectedValue, 1)


                    Dim insert As New AP_Staff_RmbLine
                    insert.Comment = CStr(ucType.GetProperty("Comment").GetValue(theControl, Nothing))

                    insert.GrossAmount = CDbl(ucType.GetProperty("Amount").GetValue(theControl, Nothing))

                    'Look for currency conversion

                    If hfCurOpen.Value = "false" Or String.IsNullOrEmpty(hfOrigCurrency.Value) Or hfOrigCurrency.Value = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId) Then
                        insert.OrigCurrency = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)
                        insert.OrigCurrencyAmount = insert.GrossAmount
                    Else
                        insert.OrigCurrency = hfOrigCurrency.Value
                        insert.OrigCurrencyAmount = hfOrigCurrencyValue.Value
                    End If
                    Dim LineTypeName = d.AP_Staff_RmbLineTypes.Where(Function(c) c.LineTypeId = CInt(ddlLineTypes.SelectedValue)).First.TypeName.ToString()


                    insert.ShortComment = GetLineComment(insert.Comment, insert.OrigCurrency, insert.OrigCurrencyAmount, tbShortComment.Text, False, Nothing, IIf(LineTypeName = "Mileage", CStr(ucType.GetProperty("Spare2").GetValue(theControl, Nothing)), ""))


                    If insert.GrossAmount >= Settings("TeamLeaderLimit") Then
                        insert.LargeTransaction = True
                    Else
                        insert.LargeTransaction = False
                    End If
                    insert.LineType = CInt(ddlLineTypes.SelectedValue)
                    insert.TransDate = CDate(ucType.GetProperty("theDate").GetValue(theControl, Nothing))

                    Dim age = DateDiff(DateInterval.Month, insert.TransDate, Today)
                    If ddlOverideTax.SelectedIndex > 0 Then
                        insert.Taxable = (ddlOverideTax.SelectedValue = 1)
                        If (age > Settings("Expire")) Then
                            insert.OutOfDate = True
                            insert.ForceTaxOrig = True
                        Else
                            insert.OutOfDate = False
                            insert.ForceTaxOrig = CBool(ucType.GetProperty("Taxable").GetValue(theControl, Nothing))
                        End If
                    Else
                        insert.ForceTaxOrig = Nothing
                        Dim theCC = From c In d.AP_StaffBroker_CostCenters Where c.CostCentreCode = tbCostcenter.Text And c.PortalId = PortalId And c.Type = CostCentreType.Department

                        If age > Settings("Expire") Then
                            Dim msg As String = ""
                            If theCC.Count > 0 Then

                                Dim staffMember = StaffBrokerFunctions.GetStaffMember(theUserId)

                                If Not String.IsNullOrEmpty(staffMember.CostCenter) Then
                                    insert.CostCenter = (staffMember.CostCenter)
                                    msg = Translate("ExpireDept").Replace("[EXPIRE]", Settings("Expire"))
                                Else
                                    msg = Translate("ExpireImpossible").Replace("[EXPIRE]", Settings("Expire"))
                                    SendMessage(msg)
                                    Return
                                End If

                            Else
                                msg = Translate("Expire").Replace("[EXPIRE]", Settings("Expire"))
                            End If

                            insert.OutOfDate = True
                            insert.Taxable = True

                            Dim t1 As Type = Me.GetType()
                            Dim sb1 As System.Text.StringBuilder = New System.Text.StringBuilder()
                            sb1.Append("<script language='javascript'>")
                            sb1.Append("alert(""" & msg & """);")
                            sb1.Append("</script>")
                            ScriptManager.RegisterClientScriptBlock(Page, t1, "popup", sb1.ToString, False)
                        Else
                            insert.OutOfDate = False
                            If theCC.Count > 0 Then
                                insert.Taxable = False
                            Else
                                insert.Taxable = CBool(ucType.GetProperty("Taxable").GetValue(theControl, Nothing))
                            End If

                        End If
                    End If

                    insert.VATReceipt = CBool(ucType.GetProperty("VAT").GetValue(theControl, Nothing))

                    Try
                        If cbRecoverVat.Checked And CDbl(tbVatRate.Text) > 0 Then
                            insert.VATRate = CDbl(tbVatRate.Text)
                        Else
                            insert.VATRate = Nothing
                        End If
                    Catch ex As Exception
                        insert.VATRate = Nothing
                    End Try

                    insert.Receipt = CBool(ucType.GetProperty("Receipt").GetValue(theControl, Nothing))
                    insert.RmbNo = hfRmbNo.Value

                    Dim theFile As IFileInfo
                    Dim ElectronicReceipt As Boolean = False
                    Try
                        If (CInt(ucType.GetProperty("ReceiptType").GetValue(theControl, Nothing) = 2)) Then

                            ElectronicReceipt = True

                            Dim theFolder As IFolderInfo = FolderManager.Instance.GetFolder(PortalId, "_RmbReceipts/" & theUserId)
                            theFile = FileManager.Instance.GetFile(theFolder, "R" & hfRmbNo.Value & "LNew.jpg")

                            If Not theFile Is Nothing Then
                                'FileManager.Instance.RenameFile(theFile, "R" & hfRmbNo.Value & "L" & line.First.RmbLineNo & ".jpg")

                                insert.ReceiptImageId = theFile.FileId
                            Else
                                theFile = FileManager.Instance.GetFile(theFolder, "R" & hfRmbNo.Value & "LNew.pdf")
                                If Not theFile Is Nothing Then
                                    insert.ReceiptImageId = theFile.FileId
                                End If
                            End If
                        End If
                    Catch ex As Exception
                        StaffBrokerFunctions.EventLog("Rmb" & hfRmbNo.Value, "Failed to Add Electronic Receipt: " & ex.ToString, UserId)

                    End Try

                    insert.Spare1 = CStr(ucType.GetProperty("Spare1").GetValue(theControl, Nothing))
                    insert.Spare2 = CStr(ucType.GetProperty("Spare2").GetValue(theControl, Nothing))
                    insert.Spare3 = CStr(ucType.GetProperty("Spare3").GetValue(theControl, Nothing))
                    insert.Spare4 = CStr(ucType.GetProperty("Spare4").GetValue(theControl, Nothing))
                    insert.Spare5 = CStr(ucType.GetProperty("Spare5").GetValue(theControl, Nothing))
                    insert.Split = False


                    ' insert.AnalysisCode = GetAnalysisCode(insert.LineType)

                    'insert.AccountCode = GetAccountCode(insert.LineType, insert.AP_Staff_Rmb.CostCenter, insert.AP_Staff_Rmb.UserId)
                    'insert.CostCenter = insert.AP_Staff_Rmb.CostCenter
                    insert.AccountCode = ddlAccountCode.SelectedValue
                    insert.CostCenter = tbCostcenter.Text

                    If insert.Receipt Then
                        If q.Count = 0 Then
                            insert.ReceiptNo = 1
                        ElseIf q.Max Is Nothing Then
                            insert.ReceiptNo = 1
                        Else
                            insert.ReceiptNo = q.Max + 1
                        End If

                    End If

                    d.AP_Staff_RmbLines.InsertOnSubmit(insert)

                    If btnApprove.Visible Then
                        'If this the Aprrover makes a change, the staff member must be notified upon submit
                        Dim theRmb = (From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value)).First
                        theRmb.Changed = True
                    End If
                    d.SubmitChanges()
                    If ElectronicReceipt And Not theFile Is Nothing Then
                        FileManager.Instance.RenameFile(theFile, "R" & hfRmbNo.Value & "L" & insert.RmbLineNo & "." & theFile.Extension)
                    End If

                    Dim t As Type = Me.GetType()
                    Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                    sb.Append("<script language='javascript'>")
                    sb.Append("closePopup();")
                    sb.Append("</script>")
                    Await LoadRmb(hfRmbNo.Value)
                    ScriptManager.RegisterClientScriptBlock(Page, t, "", sb.ToString, False)

                End If
            ElseIf btnAddLine.CommandName = "Edit" Then
                If ucType.GetMethod("ValidateForm").Invoke(theControl, New Object() {UserId}) = True Then

                    Dim line = From c In d.AP_Staff_RmbLines Where c.RmbLineNo = CInt(btnAddLine.CommandArgument)
                    If line.Count > 0 Then

                        Dim LineTypeName = d.AP_Staff_RmbLineTypes.Where(Function(c) c.LineTypeId = CInt(ddlLineTypes.SelectedValue)).First.TypeName.ToString()
                        Dim GrossAmount = CDbl(ucType.GetProperty("Amount").GetValue(theControl, Nothing))

                        If String.IsNullOrEmpty(hfOrigCurrency.Value) Then
                            line.First.OrigCurrency = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)
                            line.First.OrigCurrencyAmount = line.First.GrossAmount
                        Else
                            line.First.OrigCurrency = hfOrigCurrency.Value
                            line.First.OrigCurrencyAmount = hfOrigCurrencyValue.Value
                        End If

                        Dim comment As String = CStr(ucType.GetProperty("Comment").GetValue(theControl, Nothing))
                        Dim sc = tbShortComment.Text
                        If (sc <> line.First.ShortComment) Then
                            'the short comment was manully changed, so this should take precidence over anything else.
                            line.First.ShortComment = GetLineComment(comment, line.First.OrigCurrency, line.First.OrigCurrencyAmount, tbShortComment.Text, False, Nothing, IIf(LineTypeName = "Mileage", CStr(ucType.GetProperty("Spare2").GetValue(theControl, Nothing)), ""))
                        Else
                            line.First.ShortComment = GetLineComment(comment, line.First.OrigCurrency, line.First.OrigCurrencyAmount, "", False, Nothing, IIf(LineTypeName = "Mileage", CStr(ucType.GetProperty("Spare2").GetValue(theControl, Nothing)), ""))
                        End If
                        'If line.First.ShortComment <> comment Then
                        '    line.First.Comment = comment
                        '    If line.First.ShortComment = tbShortComment.Text Then
                        '        line.First.ShortComment = GetLineComment(comment, line.First.OrigCurrency, line.First.OrigCurrencyAmount, "", False, Nothing, IIf(LineTypeName = "Mileage", CStr(ucType.GetProperty("Spare2").GetValue(theControl, Nothing)), ""))
                        '    Else
                        '        line.First.ShortComment = GetLineComment(comment, line.First.OrigCurrency, line.First.OrigCurrencyAmount, tbShortComment.Text, False, Nothing, IIf(LineTypeName = "Mileage", CStr(ucType.GetProperty("Spare2").GetValue(theControl, Nothing)), ""))

                        '    End If


                        'Else
                        '    line.First.ShortComment = GetLineComment(comment, line.First.OrigCurrency, line.First.OrigCurrencyAmount, tbShortComment.Text, False, Nothing, IIf(LineTypeName = "Mileage", CStr(ucType.GetProperty("Spare2").GetValue(theControl, Nothing)), ""))

                        'End If



                        line.First.GrossAmount = GrossAmount

                        If line.First.GrossAmount >= Settings("TeamLeaderLimit") Then
                            line.First.LargeTransaction = True
                        Else
                            line.First.LargeTransaction = False
                        End If


                        'look for electronic receipt


                        Try
                            If (CInt(ucType.GetProperty("ReceiptType").GetValue(theControl, Nothing) = 2)) Then

                                Dim theFolder As IFolderInfo = FolderManager.Instance.GetFolder(PortalId, "_RmbReceipts/" & line.First.AP_Staff_Rmb.UserId)
                                Dim theFile = FileManager.Instance.GetFile(theFolder, "R" & line.First.RmbNo & "L" & line.First.RmbLineNo & ".jpg")
                                If Not theFile Is Nothing Then
                                    line.First.ReceiptImageId = theFile.FileId
                                Else
                                    theFile = FileManager.Instance.GetFile(theFolder, "R" & line.First.RmbNo & "L" & line.First.RmbLineNo & ".pdf")
                                    If Not theFile Is Nothing Then
                                        line.First.ReceiptImageId = theFile.FileId
                                    End If
                                End If
                            End If
                        Catch ex As Exception

                        End Try

                        line.First.AccountCode = ddlAccountCode.SelectedValue
                        line.First.CostCenter = tbCostcenter.Text
                        line.First.LineType = CInt(ddlLineTypes.SelectedValue)
                        line.First.TransDate = CDate(ucType.GetProperty("theDate").GetValue(theControl, Nothing))
                        Dim age = DateDiff(DateInterval.Month, line.First.TransDate, Today)
                        Dim theCC = From c In d.AP_StaffBroker_CostCenters Where c.CostCentreCode = tbCostcenter.Text And c.PortalId = PortalId And c.Type = CostCentreType.Department
                        If ddlOverideTax.SelectedIndex > 0 And Not (theCC.Count > 0 And ddlOverideTax.SelectedValue = 1) Then
                            line.First.Taxable = (ddlOverideTax.SelectedValue = 1)
                            If (age > Settings("Expire")) Then
                                line.First.OutOfDate = True
                                line.First.ForceTaxOrig = True
                            Else
                                line.First.OutOfDate = False
                                line.First.ForceTaxOrig = CBool(ucType.GetProperty("Taxable").GetValue(theControl, Nothing))
                            End If

                        Else
                            line.First.ForceTaxOrig = Nothing
                            If age > Settings("Expire") Then
                                Dim msg As String = ""
                                If theCC.Count > 0 Then

                                    Dim staffMember = StaffBrokerFunctions.GetStaffMember(line.First.AP_Staff_Rmb.UserId)

                                    If Not String.IsNullOrEmpty(staffMember.CostCenter) Then
                                        line.First.CostCenter = (staffMember.CostCenter)
                                        msg = Translate("ExpireDept").Replace("[EXPIRE]", Settings("Expire"))
                                    Else
                                        msg = Translate("ExpireImpossible").Replace("[EXPIRE]", Settings("Expire"))
                                        SendMessage(msg)
                                        Return
                                    End If



                                Else
                                    msg = Translate("Expire").Replace("[EXPIRE]", Settings("Expire"))
                                End If

                                line.First.OutOfDate = True
                                line.First.Taxable = True

                                Dim t1 As Type = Me.GetType()
                                Dim sb1 As System.Text.StringBuilder = New System.Text.StringBuilder()
                                sb1.Append("<script language='javascript'>")
                                sb1.Append("alert(""" & msg & """);")
                                sb1.Append("</script>")
                                ScriptManager.RegisterClientScriptBlock(Page, t1, "popup", sb1.ToString, False)
                            Else
                                line.First.OutOfDate = False

                                If theCC.Count > 0 Then
                                    line.First.Taxable = False
                                Else
                                    line.First.Taxable = CBool(ucType.GetProperty("Taxable").GetValue(theControl, Nothing))
                                End If

                            End If
                        End If

                        line.First.VATReceipt = CBool(ucType.GetProperty("VAT").GetValue(theControl, Nothing))
                        'Dim AccType = Right(ddlChargeTo.SelectedValue, 1)

                        line.First.Receipt = CBool(ucType.GetProperty("Receipt").GetValue(theControl, Nothing))
                        Try
                            If cbRecoverVat.Checked And CDbl(tbVatRate.Text) > 0 Then
                                line.First.VATRate = CDbl(tbVatRate.Text)
                            Else
                                line.First.VATRate = Nothing
                            End If
                        Catch ex As Exception
                            line.First.VATRate = Nothing
                        End Try

                        'If line.First.LineType <> 9 And line.First.LineType <> 14 And line.First.LineType <> 7 And line.First.Receipt = False And (line.First.GrossAmount > Settings("NoReceipt")) Then
                        '    ucType.GetProperty("ErrorText").SetValue(theControl, "*For transactions over " & Settings("NoReceipt") & ", a receipt must be supplied.", Nothing)
                        '    Return
                        'End If

                        line.First.Spare1 = CStr(ucType.GetProperty("Spare1").GetValue(theControl, Nothing))
                        line.First.Spare2 = CStr(ucType.GetProperty("Spare2").GetValue(theControl, Nothing))
                        line.First.Spare3 = CStr(ucType.GetProperty("Spare3").GetValue(theControl, Nothing))
                        line.First.Spare4 = CStr(ucType.GetProperty("Spare4").GetValue(theControl, Nothing))
                        line.First.Spare5 = CStr(ucType.GetProperty("Spare5").GetValue(theControl, Nothing))

                        'line.First.Split = False
                        'If line.First.LineType = 16 Then
                        '    If line.First.Spare1 = True Then
                        '        'Staff Meeting that is split
                        '        line.First.Split = True
                        '    End If
                        'End If

                        ' line.First.AnalysisCode = GetAnalysisCode(line.First.LineType)

                        If line.First.Receipt Then
                            If line.First.ReceiptNo Is Nothing Then
                                Dim q = From c In d.AP_Staff_RmbLines Where c.RmbNo = hfRmbNo.Value And c.Receipt Select c.ReceiptNo
                                If q.Max Is Nothing Then
                                    line.First.ReceiptNo = 1
                                Else
                                    line.First.ReceiptNo = q.Max + 1
                                End If


                            End If
                        Else
                            line.First.ReceiptNo = Nothing
                        End If


                        If btnApprove.Visible Then
                            'If this the Aprrover makes a change, the staff member must be notified upon submit
                            Dim theRmb = (From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value)).First
                            theRmb.Changed = True
                        End If

                        d.SubmitChanges()
                        'If ddlLineTypes.SelectedItem.Text = "Mileage" Then
                        '    'Mileage
                        '    ucType.GetMethod("AddStaff").Invoke(theControl, New Object() {line.First.RmbLineNo})
                        '    If line.First.Spare3 <> Settings("Motorcycle") And line.First.Spare3 <> Settings("Bicycle") Then

                        '        GetMilesForYear(line.First.RmbLineNo, line.First.AP_Staff_Rmb.UserId)

                        '    End If

                        'End If

                        btnAddLine.CommandName = "Save"

                    End If
                    Dim t As Type = Me.GetType()
                    Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                    sb.Append("<script language='javascript'>")
                    sb.Append("closePopup();")
                    sb.Append("</script>")
                    Await LoadRmb(hfRmbNo.Value)
                    ScriptManager.RegisterClientScriptBlock(Page, t, "", sb.ToString, False)
                End If
            End If
        End Sub

        Protected Async Sub btnCreate_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCreate.Click
            Dim insert As New AP_Staff_Rmb
            If tbNewYourRef.Text = "" Then
                insert.UserRef = Translate("Expenses")
            Else

                insert.UserRef = tbNewYourRef.Text
            End If
            insert.RID = StaffRmbFunctions.GetNewRID(PortalId)
            insert.CostCenter = tbNewChargeTo.Text
            insert.UserComment = tbNewComments.Text
            insert.UserId = UserId
            ' insert.PersonalCC = ddlNewChargeTo.Items(0).Value
            insert.AdvanceRequest = 0.0

            insert.PortalId = PortalId

            insert.Status = RmbStatus.Draft

            insert.Locked = False

            Dim CC = From c In ds.AP_StaffBroker_Staffs Where (c.UserId1 = UserId Or c.UserId2 = UserId) And Not c.CostCenter.EndsWith("X") Select c.CostCenter

            If CC.Count > 0 Then
                insert.SupplierCode = "P-" & Left(CC.First, 3) & "0"
                'Else
                '    Dim PCC = From c In ds.AP_StaffBroker_Staffs Where (c.UserId1 = UserId Or c.UserId2 = UserId) And c.PersonalCostCentre <> "" Select c.PersonalCostCentre
                '    If PCC.Count > 0 Then
                '        insert.SupplierCode = "P-" & PCC.First & "0"
                '    Else
                '        insert.SupplierCode = ""
                '    End If
            End If

            insert.Department = StaffBrokerFunctions.IsDept(PortalId, CC.First)

            btnApprove.Visible = False
            btnSubmit.Visible = True

            d.AP_Staff_Rmbs.InsertOnSubmit(insert)
            d.SubmitChanges()

            Dim t As Type = tbNewChargeTo.GetType()
            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            sb.Append("<script language='javascript'>")
            sb.Append("closePopup2();")
            sb.Append("</script>")
            Await LoadRmb(insert.RMBNo)
            ScriptManager.RegisterClientScriptBlock(tbNewChargeTo, t, "", sb.ToString, False)

        End Sub

        Protected Async Function btnSubmit_Click(ByVal sender As Object, ByVal e As System.EventArgs) As Task Handles btnSubmit.Click
            Await saveIfNecessaryAsync()
            Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value
            If rmb.Count > 0 Then
                Dim NewStatus As Integer = rmb.First.Status
                Dim message As String = Translate("Printout")

                If (rmb.First.MoreInfoRequested) Then
                    rmb.First.MoreInfoRequested = False
                    rmb.First.Locked = True
                Else
                    NewStatus = RmbStatus.Submitted
                    rmb.First.Locked = False
                End If

                rmb.First.Status = NewStatus
                rmb.First.RmbDate = Now
                rmb.First.Period = Nothing
                rmb.First.Year = Nothing

                Await SubmitChangesAsync()
                'dlPending.DataBind()
                'dlSubmitted.DataBind()

                'Send Email to Staff Member
                If NewStatus = RmbStatus.Submitted Then
                    SendApprovalEmail(rmb.First)
                End If
                Log(rmb.First.RMBNo, "SUBMITTED")

                'use an alert to switch back to the main window from the printout window
                Dim t As Type = Me.GetType()
                Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                sb.Append("<script language='javascript'>")
                sb.Append("alert(""" & message & """);")
                sb.Append("</script>")
                Await LoadRmb(hfRmbNo.Value)
                ScriptManager.RegisterStartupScript(Page, t, "popup", sb.ToString, False)

            End If

        End Function

        Protected Async Sub btnDelete_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnDelete.Click

            Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value
            If rmb.Count > 0 Then
                rmb.First.Status = RmbStatus.Cancelled
                Dim submitChangesTask = SubmitChangesAsync()
                lblStatus.Text = Translate(RmbStatus.StatusName(RmbStatus.Cancelled))
                btnApprove.Visible = False
                btnDelete.Visible = False

                If rmb.First.UserId = UserId Then
                    Log(rmb.First.RMBNo, "DELETED by owner")

                    Await submitChangesTask
                    Await ResetMenuAsync()
                    ScriptManager.RegisterStartupScript(btnDelete, btnDelete.GetType(), "select4", "selectIndex(4)", True)
                Else
                    'Send an email to the end user
                    Dim Message = ""
                    Dim dr As New TemplatesDataContext
                    '  Dim ConfTemplate = From c In dr.AP_StaffBroker_Templates Where c.TemplateName = "RmbCancelled" And c.PortalId = PortalId Select c.TemplateHTML

                    Message = StaffBrokerFunctions.GetTemplate("RmbCancelled", PortalId)

                    '  If ConfTemplate.Count > 0 Then
                    'Message = Server.HtmlDecode(ConfTemplate.First)
                    ' End If

                    Dim StaffMbr = UserController.GetUserById(PortalId, rmb.First.UserId)

                    Message = Message.Replace("[STAFFNAME]", StaffMbr.FirstName)
                    Message = Message.Replace("[APPRNAME]", UserInfo.FirstName & " " & UserInfo.LastName)
                    Message = Message.Replace("[APPRFIRSTNAME]", UserInfo.FirstName)

                    Dim comments As String = ""
                    If tbApprComments.Text.Trim().Length > 0 Then
                        comments = Translate("CommentLeft").Replace("[FIRSTNAME]", UserInfo.FirstName).Replace("[COMMENT]", tbApprComments.Text)

                    End If

                    Message = Message.Replace("[COMMENTS]", comments)


                    'DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", theUser.Email, "donotreply@agape.org.uk", "Rmb#: " & hfRmbNo.Value & "-" & rmb.First.UserRef & " has been cancelled", Message, "", "HTML", "", "", "", "")
                    DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", StaffMbr.Email, "", Translate("EmailCancelledSubject").Replace("[RMBNO]", rmb.First.RID).Replace("[USERREF]", rmb.First.UserRef), Message, "", "HTML", "", "", "", "")

                    'ltSplash.Text = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("RmbSplash", PortalId))

                    pnlMain.Visible = False
                    pnlSplash.Visible = True

                    Log(rmb.First.RMBNo, "DELETED")

                    Await submitChangesTask
                    Await ResetMenuAsync()
                    ScriptManager.RegisterStartupScript(btnDelete, btnDelete.GetType(), "select0", "selectIndex(0)", True)

                End If


            End If

        End Sub

        Protected Async Sub btnApprove_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnApprove.Click
            Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value
            If rmb.Count > 0 Then

                rmb.First.Status = RmbStatus.Approved
                rmb.First.Locked = True
                rmb.First.ApprDate = Now
                rmb.First.ApprUserId = UserId
                rmb.First.Period = Nothing
                rmb.First.Year = Nothing

                'SEND EMAIL TO OTHER APPROVERS
                Dim Auth = UserController.GetUserById(PortalId, Settings("AuthUser"))
                Dim AuthAuth = UserController.GetUserById(PortalId, Settings("AuthAuthUser"))
                Dim myApprovers = Await StaffRmbFunctions.getApproversAsync(rmb.First, Auth, AuthAuth)
                Dim SpouseId As Integer = StaffBrokerFunctions.GetSpouseId(rmb.First.UserId)

                Dim ObjAppr As UserInfo = UserController.GetUserById(PortalId, Me.UserId)
                Dim theUser As UserInfo = UserController.GetUserById(PortalId, rmb.First.UserId)
                Dim ApprMessage = ""
                '   Dim dr As New TemplatesDataContext
                '  Dim ConfTemplate = From c In dr.AP_StaffBroker_Templates Where c.TemplateName = "RmbApprovedEmail-ApproversVersion" And c.PortalId = PortalId Select c.TemplateHTML

                '  If ConfTemplate.Count > 0 Then
                'ApprMessage = Server.HtmlDecode(ConfTemplate.First)
                ' End If
                ApprMessage = StaffBrokerFunctions.GetTemplate("RmbApprovedEmail-ApproversVersion", PortalId)

                ApprMessage = ApprMessage.Replace("[APPRNAME]", ObjAppr.DisplayName).Replace("[RMBNO]", rmb.First.RMBNo).Replace("[STAFFNAME]", theUser.DisplayName)


                For Each row In (From c In myApprovers.UserIds Where c.UserID <> rmb.First.UserId And c.UserID <> SpouseId)
                    ApprMessage = ApprMessage.Replace("[THISAPPRNAME]", row.DisplayName)
                    'DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", row.Email, "donotreply@agape.org.uk", "Rmb#:" & hfRmbNo.Value & " has been approved by " & ObjAppr.DisplayName, ApprMessage, "", "HTML", "", "", "", "")
                    DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", row.Email, "", Translate("EmailApprovedSubjectA").Replace("[RMBNO]", rmb.First.RID).Replace("[APPROVER]", ObjAppr.DisplayName), ApprMessage, "", "HTML", "", "", "", "")

                Next

                'SEND APRROVE EMAIL
                Dim Emessage = ""
                ' Dim ApprovedTemp = From c In dr.AP_StaffBroker_Templates Where c.TemplateName = "RmbApprovedEmail" And PortalId = c.PortalId Select c.TemplateHTML
                Emessage = StaffBrokerFunctions.GetTemplate("RmbApprovedEmail", PortalId)
                'If ApprovedTemp.Count > 0 Then
                'Emessage = Server.HtmlDecode(ApprovedTemp.First)
                ' End If
                Emessage = Emessage.Replace("[STAFFNAME]", theUser.DisplayName).Replace("[RMBNO]", rmb.First.RID).Replace("[USERREF]", IIf(rmb.First.UserRef <> "", rmb.First.UserRef, "None"))
                Emessage = Emessage.Replace("[APPROVER]", ObjAppr.DisplayName)
                If rmb.First.Changed = True Then
                    Emessage = Emessage.Replace("[CHANGES]", ". " & Translate("EmailApproverChanged"))
                    rmb.First.Changed = False
                Else
                    Emessage = Emessage.Replace("[CHANGES]", "")
                End If
                d.SubmitChanges()

                ' DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agape.org.uk", theUser.Email, "donotreply@agape.org.uk", "Rmb#: " & hfRmbNo.Value & "-" & rmb.First.UserRef & " has been approved", Emessage, "", "HTML", "", "", "", "")
                DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", theUser.Email, "", Translate("EmailApprovedSubjectP").Replace("[RMBNO]", rmb.First.RID).Replace("[USERREF]", rmb.First.UserRef), Emessage, "", "HTML", "", "", "", "")

                btnApprove.Visible = False

                Await ResetMenuAsync()

                Log(rmb.First.RMBNo, "Approved")
                Dim message As String = Translate("RmbApproved").Replace("[RMBNO]", rmb.First.RID)
                Dim t As Type = btnApprove.GetType()
                Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                sb.Append("<script language='javascript'>")

                sb.Append("selectIndex(2);")
                sb.Append("alert(""" & message & """);")
                sb.Append("</script>")
                ScriptManager.RegisterStartupScript(btnApprove, t, "select2", sb.ToString, False)
                btnApprove.Visible = False
                pnlMain.Visible = False
                pnlSplash.Visible = True

            End If
        End Sub

        Protected Async Function btnSave_Click(ByVal sender As Object, ByVal e As System.EventArgs) As Task Handles btnSave.Click, btnSaveAdv.Click
            Dim RmbNo As Integer
            Dim PortalId As Integer
            Try
                RmbNo = CInt(hfRmbNo.Value)
                PortalId = CInt(hfPortalId.Value)
            Catch
                Return
            End Try

            Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo And c.PortalId = PortalId
            lblAdvError.Text = ""
            If rmb.Count > 0 Then
                '--Dim rmbLoadTask = Await LoadRmb(RmbNo)
                rmb.First.UserComment = tbComments.Text
                rmb.First.UserRef = tbYouRef.Text
                rmb.First.ApprComment = tbApprComments.Text
                rmb.First.MoreInfoRequested = cbMoreInfo.Checked Or cbApprMoreInfo.Checked
                If (cbMoreInfo.Checked Or cbApprMoreInfo.Checked) Then
                    rmb.First.Locked = False
                End If
                rmb.First.ApprUserId = ddlApprovedBy.SelectedValue
                rmb.First.AcctComment = tbAccComments.Text
                'If ddlPeriod.SelectedIndex > 0 Then
                '    rmb.First.Period = ddlPeriod.SelectedValue
                'End If
                'If ddlYear.SelectedIndex > 0 Then
                '    rmb.First.Year = ddlYear.SelectedValue
                'End If

                For Each row In (From c In rmb.First.AP_Staff_RmbLines Where c.CostCenter = rmb.First.CostCenter)
                    row.CostCenter = hfChargeToValue.Value
                Next
                rmb.First.CostCenter = hfChargeToValue.Value
                If tbAdvanceAmount.Text = "" Then
                    tbAdvanceAmount.Text = 0
                End If

                Try
                    rmb.First.AdvanceRequest = Double.Parse(tbAdvanceAmount.Text, New CultureInfo("en-US"))
                    If rmb.First.AdvanceRequest > rmb.First.AP_Staff_RmbLines.Sum(Function(x) x.GrossAmount) Then
                        rmb.First.AdvanceRequest = rmb.First.AP_Staff_RmbLines.Sum(Function(x) x.GrossAmount)
                        tbAdvanceAmount.Text = rmb.First.AdvanceRequest.ToString("0.00")
                    End If
                Catch
                    lblAdvError.Text = Translate("AdvanceError")
                    Return
                End Try

                Await SubmitChangesAsync()
                '--Await rmbLoadTask  'Don't need to reload on a save.
            End If

        End Function

        Protected Async Sub addLinebtn2_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles addLinebtn2.Click

            'ddlLineTypes_SelectedIndexChanged(Me, Nothing)
            'ddlCostcenter.SelectedValue = ddlChargeTo.SelectedValue
            Await saveIfNecessaryAsync()
            tbCostcenter.Text = hfChargeToValue.Value

            ddlLineTypes.Items.Clear()
            Dim lineTypes = From c In d.AP_StaffRmb_PortalLineTypes Where c.PortalId = PortalId Order By c.ViewOrder Select c.AP_Staff_RmbLineType.LineTypeId, c.LocalName, c.PCode, c.DCode

            '            If StaffBrokerFunctions.IsDept(PortalId, ddlChargeTo.SelectedValue) Then
            If StaffBrokerFunctions.IsDept(PortalId, hfChargeToValue.Value) Then
                lineTypes = lineTypes.Where(Function(x) x.DCode <> "")

            Else
                lineTypes = lineTypes.Where(Function(x) x.PCode <> "")
            End If
            ddlLineTypes.DataSource = lineTypes
            ddlLineTypes.DataBind()

            ResetNewExpensePopup(True)
            cbRecoverVat.Checked = False
            tbVatRate.Text = ""
            tbShortComment.Text = ""

            'PopupTitle.Text = "Add New Reimbursement Expense"
            btnAddLine.CommandName = "Save"

            hfOrigCurrency.Value = ""
            hfOrigCurrencyValue.Value = ""

            ifReceipt.Attributes("src") = "http://" & PortalSettings.PortalAlias.HTTPAlias & "/DesktopModules/AgapeConnect/StaffRmb/ReceiptEditor.aspx?RmbNo=" & hfRmbNo.Value & "&RmbLine=New"
            pnlElecReceipts.Attributes("style") = "display: none;"
            Dim jscript As String = ""
            jscript &= " $('#" & hfOrigCurrency.ClientID & "').attr('value', '');"
            jscript &= " $('#" & hfOrigCurrencyValue.ClientID & "').attr('value', '');"

            Dim t As Type = addLinebtn2.GetType()
            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            sb.Append("<script language='javascript'>")

            sb.Append(jscript & "showPopup();")
            sb.Append("</script>")
            ScriptManager.RegisterStartupScript(addLinebtn2, t, "popupAdd", sb.ToString, False)
        End Sub

        'Protected Sub btnSettings_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSettings.Click
        '    Response.Redirect(EditUrl("RmbSettings"))
        'End Sub

        Protected Sub btnSplitAdd_Click(sender As Object, e As System.EventArgs) Handles btnSplitAdd.Click
            hfRows.Value += 1
            'tblSplit.Rows.Clear()

            'For i As Integer = 2 To hfRows.Value
            Dim insert As New TableRow()
            Dim insertDesc As New TableCell()
            Dim insertAmt As New TableCell()
            Dim tbDesc As New TextBox()
            ' tbDesc.ID = "tbDesc" & hfRows.Value
            tbDesc.Width = Unit.Percentage(100)
            tbDesc.CssClass = "Description"
            tbDesc.Text = lblOriginalDesc.Text
            Dim tbAmt As New TextBox()
            tbAmt.Width = Unit.Pixel(100)
            ' tbAmt.ID = "tbAmt" & hfRows.Value
            tbAmt.CssClass = "Amount"
            tbAmt.Attributes.Add("onblur", "calculateTotal();")
            insertDesc.Controls.Add(tbDesc)
            insertAmt.Controls.Add(tbAmt)

            insert.Cells.Add(insertDesc)
            insert.Cells.Add(insertAmt)
            tblSplit.Rows.Add(insert)
            '  Next
        End Sub

        Protected Async Sub btnOK_Click(sender As Object, e As System.EventArgs) Handles btnOK.Click
            Dim theLine = From c In d.AP_Staff_RmbLines Where c.RmbLineNo = CInt(hfSplitLineId.Value)
            If theLine.Count > 0 Then
                For Each row As TableRow In tblSplit.Rows
                    Dim RowAmount = CType(row.Cells(1).Controls(0), TextBox).Text
                    Dim RowDesc = CType(row.Cells(0).Controls(0), TextBox).Text
                    If RowAmount = "" Or RowDesc = "" Then
                        lblSplitError.Visible = True
                        Return
                    ElseIf CDbl(RowAmount) = 0 Then
                        lblSplitError.Visible = True
                        Return
                    End If
                    Dim insert As New AP_Staff_RmbLine()
                    insert.AnalysisCode = theLine.First.AnalysisCode
                    insert.Comment = RowDesc
                    insert.GrossAmount = CDbl(RowAmount)
                    insert.LargeTransaction = RowAmount > CDbl(Settings("TeamLeaderLimit"))
                    insert.LineType = theLine.First.LineType
                    insert.Mileage = theLine.First.Mileage
                    insert.MileageRate = theLine.First.MileageRate
                    insert.OutOfDate = theLine.First.OutOfDate
                    insert.Receipt = theLine.First.Receipt
                    insert.ReceiptNo = theLine.First.ReceiptNo
                    insert.RmbNo = theLine.First.RmbNo
                    insert.Spare1 = theLine.First.Spare1
                    insert.Spare2 = theLine.First.Spare2
                    insert.Spare3 = theLine.First.Spare3
                    insert.Spare4 = theLine.First.Spare4
                    insert.Spare5 = theLine.First.Spare5
                    insert.Split = True
                    insert.Taxable = theLine.First.Taxable
                    insert.TransDate = theLine.First.TransDate
                    insert.VATReceipt = theLine.First.VATReceipt
                    insert.CostCenter = theLine.First.CostCenter
                    insert.AccountCode = theLine.First.AccountCode
                    d.AP_Staff_RmbLines.InsertOnSubmit(insert)
                Next
            End If
            d.AP_Staff_RmbLines.DeleteAllOnSubmit(theLine)
            d.SubmitChanges()
            lblSplitError.Visible = False
            Await LoadRmb(hfRmbNo.Value)

            Dim t As Type = btnOK.GetType()
            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            sb.Append("<script language='javascript'>")
            sb.Append("closePopupSplit();")
            sb.Append("</script>")
            ScriptManager.RegisterStartupScript(btnOK, t, "clostSplit", sb.ToString, False)

        End Sub

        Protected Async Sub btnProcess_Click(sender As Object, e As System.EventArgs) Handles btnProcess.Click, btnAccountWarningYes.Click

            'Mark as Pending Download in next batch.
            Dim theRmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value)

            'Check Balance
            If CType(sender, Button).ID = "btnProcess" And Settings("WarnIfNegative") Then
                Dim pendingBalance = GetNumericRemainingBalance(2)

                Dim RmbBalance = theRmb.First.AP_Staff_RmbLines.Where(Function(x) x.CostCenter = x.AP_Staff_Rmb.CostCenter).Sum(Function(x) x.GrossAmount)
                If RmbBalance > pendingBalance Then
                    Dim message2 = Translate("NextBatch")
                    Dim t2 As Type = Me.GetType()
                    Dim sb2 As System.Text.StringBuilder = New System.Text.StringBuilder()
                    sb2.Append("<script language='javascript'>")
                    sb2.Append("showAccountWarning();")
                    sb2.Append("</script>")
                    ScriptManager.RegisterStartupScript(Page, t2, "popupWarning", sb2.ToString, False)
                    Return
                End If
            End If

            theRmb.First.Status = RmbStatus.PendingDownload
            theRmb.First.ProcDate = Today
            theRmb.First.MoreInfoRequested = False
            theRmb.First.ProcUserId = UserId
            d.SubmitChanges()
            Await LoadRmb(hfRmbNo.Value)
            Log(theRmb.First.RMBNo, "Processed - this reimbursement will be added to the next download batch")
            Dim message = Translate("NextBatch")
            Dim t As Type = Me.GetType()
            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            sb.Append("<script language='javascript'>")
            sb.Append("alert(""" & message & """);")
            sb.Append("</script>")
            ScriptManager.RegisterStartupScript(Page, t, "popup", sb.ToString, False)

        End Sub

        Protected Sub btnDownload_Click(sender As Object, e As System.EventArgs) Handles btnDownload.Click
            Dim RmbNo As Integer = CInt(hfRmbNo.Value)
            Dim thisRID = (From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo And c.PortalId = PortalId Select c.RID).First
            Dim export As String = "Account,Subaccount,Ref,Date," & GetOrderedString("Description", "Debit", "Credit", "Company")
            export &= DownloadRmbSingle(CInt(hfRmbNo.Value))
            Dim attachment As String = "attachment; filename=Rmb" & ZeroFill(thisRID, 5) & ".csv"

            HttpContext.Current.Response.Clear()
            HttpContext.Current.Response.ClearHeaders()
            HttpContext.Current.Response.ClearContent()
            HttpContext.Current.Response.AddHeader("content-disposition", attachment)
            HttpContext.Current.Response.ContentType = "text/csv"
            HttpContext.Current.Response.AddHeader("Pragma", "public")
            HttpContext.Current.Response.Write(export)
        End Sub

        Protected Sub btnMarkProcessed_Click(sender As Object, e As System.EventArgs) Handles btnMarkProcessed.Click
            DownloadBatch(True)

            'Dim RmbList As List(Of Integer) = Session("RmbList")
            'If Not RmbList Is Nothing Then
            '    Dim q = From c In d.AP_Staff_Rmbs Where RmbList.Contains(c.RMBNo) And c.PortalId = PortalId

            '    For Each row In q
            '        row.Status = RmbStatus.Processed
            '        row.ProcDate = Now
            '        Log(row.RMBNo, "Marked as Processed - after a manual download")
            '    Next
            'End If
            'Dim AdvList As List(Of Integer) = Session("AdvList")
            'If Not AdvList Is Nothing Then

            '    Dim r = From c In d.AP_Staff_AdvanceRequests Where AdvList.Contains(c.AdvanceId) And c.PortalId = PortalId

            '    For Each row In r
            '        row.RequestStatus = RmbStatus.Processed
            '        row.ProcessedDate = Now
            '        Log(row.AdvanceId, "Advance Marked as Processed - after a manual download")
            '    Next

            'End If

            'd.SubmitChanges()




            'If hfRmbNo.Value <> "" Then
            '    If hfRmbNo.Value > 0 Then
            '        Await LoadRmb(CInt(hfRmbNo.Value))
            '    Else
            '        LoadAdv(-CInt(hfRmbNo.Value))
            '    End If


            'End If

            'Await ResetMenuAsync()


            'Dim t As Type = Me.GetType()
            'Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            'sb.Append("<script language='javascript'>")
            'sb.Append("closePopupDownload();")
            'sb.Append("</script>")
            'ScriptManager.RegisterClientScriptBlock(Page, t, "popupDownload", sb.ToString, False)
            'HttpContext.Current.Response.End()
        End Sub

        Protected Sub btnDontMarkProcessed_Click(sender As Object, e As System.EventArgs) Handles btnDontMarkProcessed.Click
            DownloadBatch()

            'Dim t As Type = Me.GetType()
            'Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            'sb.Append("<script language='javascript'>")
            'sb.Append("closePopupDownload();")
            'sb.Append("</script>")
            'ScriptManager.RegisterClientScriptBlock(Page, t, "popupDownload", sb.ToString, False)
            'HttpContext.Current.Response.End()
        End Sub

        'Protected Sub btnDownloadBatch_Click(sender As Object, e As System.EventArgs) Handles btnDownloadBatch.Click
        '    DownloadBatch()

        'End Sub

        Protected Sub btnPrint_Click(sender As Object, e As System.EventArgs) Handles btnPrint.Click
            Dim theRmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value)
            Dim t As Type = btnPrint.GetType()
            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()

            sb.Append("<script language='javascript'>")
            sb.Append("window.open('/DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & hfRmbNo.Value & "&UID=" & theRmb.First.UserId & "', '_blank'); ")
            sb.Append("</script>")
            ScriptManager.RegisterStartupScript(btnPrint, t, "printOut", sb.ToString, False)
        End Sub

        Protected Async Sub btnUnProcess_Click(sender As Object, e As System.EventArgs) Handles btnUnProcess.Click
            Dim theRmb = (From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value)).First
            If theRmb.Status = RmbStatus.Processed Then
                'If the reimbursement has already been downloaded, a warning should be displayed - but hte reimbursement can be simply unprocessed
                theRmb.Status = RmbStatus.Approved
                d.SubmitChanges()
                Log(theRmb.RMBNo, "UNPROCESSED, after it had been fully processed")
            Else
                'if it has not been downloaded, it will be downloaded very soon. We need to check if a download is already in progress.
                If StaffBrokerFunctions.GetSetting("Datapump", PortalId) = "locked" Then
                    'If a download is in progress, we need to display a "not at this time" message
                    Dim message = "This reimbursement cannot be unprocessed at this time, as it is currently being downloaded by the automatic datapump (transaction broker). You can try again in a few minutes, but be aware that it will already have been processed into your accounts program."
                    Dim t As Type = Me.GetType()
                    Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                    sb.Append("<script language='javascript'>")
                    sb.Append("alert(""" & message & """);")
                    sb.Append("</script>")
                    ScriptManager.RegisterStartupScript(Page, t, "popup", sb.ToString, False)
                    Log(theRmb.RMBNo, "Attempted unprocessed, but could not as it was in the process of being downloaded by the automatic transaction broker")
                    Return
                Else
                    'If not, we need to lock the download progress (to ensure that it is not downloaded whilsts we are playing with it
                    StaffBrokerFunctions.SetSetting("Datapump", "Locked", PortalId)
                    'Then we can unprocess it
                    theRmb.Status = RmbStatus.Approved
                    theRmb.Period = Nothing
                    theRmb.Year = Nothing
                    theRmb.ProcDate = Nothing

                    d.SubmitChanges()
                    'Then release the lock.
                    StaffBrokerFunctions.SetSetting("Datapump", "Unlocked", PortalId)
                    Log(theRmb.RMBNo, "UNPROCESSED - before it was downloaded")
                End If
            End If
            Await LoadRmb(hfRmbNo.Value)
        End Sub

        Protected Async Sub GridView1_RowCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles GridView1.RowCommand
            If e.CommandName = "myDelete" Then
                ' d.AP_Staff_RmbLineAddStaffs.DeleteAllOnSubmit(From c In d.AP_Staff_RmbLineAddStaffs Where c.RmbLineId = CInt(e.CommandArgument))
                d.AP_Staff_RmbLines.DeleteAllOnSubmit(From c In d.AP_Staff_RmbLines Where c.RmbLineNo = CInt(e.CommandArgument))
                d.SubmitChanges()
                Await LoadRmb(hfRmbNo.Value)

            ElseIf e.CommandName = "myEdit" Then
                Dim theLine = From c In d.AP_Staff_RmbLines Where c.RmbLineNo = CInt(e.CommandArgument)
                If theLine.Count > 0 Then
                    'PopupTitle.Text = "Edit Reimbursement Transaction"
                    'Dim lt = From c In d.AP_Staff_RmbLineTypes Where c.LineTypeId = theLine.First.LineType
                    'If lt.Count > 0 Then
                    '    phLineDetail.Controls.Clear()
                    '    theControl = LoadControl(lt.First.ControlPath)
                    '    theControl.ID = "theControl"
                    '    phLineDetail.Controls.Add(theControl)
                    'End If
                    'theControl = Nothing


                    ddlLineTypes.Items.Clear()
                    Dim lineTypes = From c In d.AP_StaffRmb_PortalLineTypes Where c.PortalId = PortalId Order By c.LocalName Select c.AP_Staff_RmbLineType.LineTypeId, c.LocalName, c.PCode, c.DCode

                    If StaffBrokerFunctions.IsDept(PortalId, theLine.First.CostCenter) Then
                        lineTypes = lineTypes.Where(Function(x) x.DCode <> "")

                    Else
                        lineTypes = lineTypes.Where(Function(x) x.PCode <> "")
                    End If
                    ddlLineTypes.DataSource = lineTypes
                    ddlLineTypes.DataBind()
                    lblIncType.Visible = False
                    btnAddLine.Enabled = True

                    If lineTypes.Where(Function(x) x.LineTypeId = theLine.First.LineType).Count = 0 Then
                        ddlLineTypes.Items.Add(New ListItem(theLine.First.AP_Staff_RmbLineType.AP_StaffRmb_PortalLineTypes.Where(Function(x) x.PortalId = PortalId).First.LocalName, theLine.First.LineType))
                        '  ddlLineTypes.Items.Add(New ListItem(theLine.First.LineType,"Wrong type"))

                        'Wrong Type... needs changing!
                        lblIncType.Visible = True
                        btnAddLine.Enabled = False
                    End If

                    ddlLineTypes.SelectedValue = theLine.First.LineType
                    ddlLineTypes_SelectedIndexChanged(Me, Nothing)

                    Dim ucType As Type = theControl.GetType()
                    ucType.GetProperty("Comment").SetValue(theControl, theLine.First.Comment, Nothing)
                    ucType.GetProperty("Amount").SetValue(theControl, CDbl(theLine.First.GrossAmount), Nothing)
                    Dim jscript As String = ""
                    If (Not theLine.First.OrigCurrencyAmount Is Nothing) Then
                        hfOrigCurrencyValue.Value = theLine.First.OrigCurrencyAmount
                        jscript &= " $('#" & hfOrigCurrencyValue.ClientID & "').attr('value', '" & theLine.First.OrigCurrencyAmount & "');"
                        'jscript &= " $('.currency').attr('value'," & theLine.First.OrigCurrencyAmount & ");"
                        hfExchangeRate.Value = (theLine.First.GrossAmount / theLine.First.OrigCurrencyAmount).Value.ToString(New CultureInfo(""))
                    End If
                    If (Not String.IsNullOrEmpty(theLine.First.OrigCurrency)) Then
                        jscript &= " $('#" & hfOrigCurrency.ClientID & "').attr('value', '" & theLine.First.OrigCurrency & "');"
                        hfOrigCurrency.Value = theLine.First.OrigCurrency
                        'jscript &= " $('.ddlCur').val('" & theLine.First.OrigCurrency & "');"

                    End If

                    ucType.GetProperty("theDate").SetValue(theControl, theLine.First.TransDate, Nothing)
                    ucType.GetProperty("VAT").SetValue(theControl, theLine.First.VATReceipt, Nothing)
                    ucType.GetProperty("Receipt").SetValue(theControl, theLine.First.Receipt, Nothing)
                    ucType.GetProperty("Spare1").SetValue(theControl, theLine.First.Spare1, Nothing)
                    ucType.GetProperty("Spare2").SetValue(theControl, theLine.First.Spare2, Nothing)
                    ucType.GetProperty("Spare3").SetValue(theControl, theLine.First.Spare3, Nothing)
                    ucType.GetProperty("Spare4").SetValue(theControl, theLine.First.Spare4, Nothing)
                    ucType.GetProperty("Spare5").SetValue(theControl, theLine.First.Spare5, Nothing)

                    Dim receiptMode = 2
                    If theLine.First.VATReceipt Then
                        receiptMode = 0
                    ElseIf Not theLine.First.Receipt Then
                        receiptMode = -1
                    ElseIf theLine.First.ReceiptImageId Is Nothing Then
                        receiptMode = 1
                    ElseIf theLine.First.ReceiptImageId < 0 Then
                        receiptMode = 1

                    End If
                    Try
                        ucType.GetProperty("ReceiptType").SetValue(theControl, receiptMode, Nothing)
                    Catch ex As Exception

                    End Try

                    ucType.GetMethod("Initialize").Invoke(theControl, New Object() {Settings})
                    cbRecoverVat.Checked = False
                    If theLine.First.ForceTaxOrig Is Nothing Then
                        ddlOverideTax.SelectedIndex = 0
                    Else
                        ddlOverideTax.SelectedValue = IIf(theLine.First.Taxable, 1, 2)

                    End If
                    tbVatRate.Text = ""
                    If theLine.First.VATRate > 0 Then
                        If theLine.First.VATRate > 0 Then
                            cbRecoverVat.Checked = True
                            tbVatRate.Text = theLine.First.VATRate
                        End If
                    End If

                    tbShortComment.Text = GetLineComment(theLine.First.Comment, theLine.First.OrigCurrency, theLine.First.OrigCurrencyAmount, theLine.First.ShortComment, False, Nothing, IIf(theLine.First.AP_Staff_RmbLineType.TypeName = "Mileage", theLine.First.Spare2, ""))

                    'If ddlLineTypes.SelectedValue = 7 Then

                    '    ucType.GetMethod("LoadStaff").Invoke(theControl, New Object() {theLine.First.RmbLineNo, Settings, CanAddPass()})
                    'End If

                    btnAddLine.CommandName = "Edit"
                    btnAddLine.CommandArgument = CInt(e.CommandArgument)
                    tbCostcenter.Text = theLine.First.CostCenter
                    ddlAccountCode.SelectedValue = theLine.First.AccountCode

                    ifReceipt.Attributes("src") = Request.Url.Scheme & "://" & Request.Url.Host & "/DesktopModules/AgapeConnect/StaffRmb/ReceiptEditor.aspx?RmbNo=" & theLine.First.RmbNo & "&RmbLine=" & theLine.First.RmbLineNo

                    If Not theLine.First.ReceiptImageId Is Nothing Then
                        pnlElecReceipts.Attributes("style") = ""
                    Else
                        pnlElecReceipts.Attributes("style") = "display: none;"
                    End If

                    Dim t As Type = GridView1.GetType()
                    Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                    sb.Append("<script language='javascript'>")
                    sb.Append(jscript & "showPopup();")
                    sb.Append("</script>")
                    ScriptManager.RegisterStartupScript(GridView1, t, "popupedit", sb.ToString, False)

                End If
            ElseIf e.CommandName = "mySplit" Then
                hfRows.Value = 1
                hfSplitLineId.Value = CInt(e.CommandArgument)
                lblSplitError.Visible = False
                Dim theLine = From c In d.AP_Staff_RmbLines Where c.RmbLineNo = CInt(e.CommandArgument)

                If theLine.Count > 0 Then
                    lblOriginalDesc.Text = theLine.First.Comment
                    lblOriginalAmt.Text = theLine.First.GrossAmount.ToString("0.00")
                    tbSplitDesc.Text = lblOriginalDesc.Text
                End If
                tbSplitAmt.Attributes.Add("onblur", "calculateTotal();")
                tbSplitAmt.Text = ""
                tbSplitDesc.Text = ""

                Dim t As Type = GridView1.GetType()
                Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                sb.Append("<script language='javascript'>")
                sb.Append("showPopupSplit();")
                sb.Append("</script>")
                ScriptManager.RegisterStartupScript(GridView1, t, "popupSplit", sb.ToString, False)

            ElseIf e.CommandName = "myDefer" Then
                'Try to find a deferred transactions pending reimbursement
                Dim theLine = From c In d.AP_Staff_RmbLines Where c.RmbLineNo = CInt(e.CommandArgument)
                If theLine.Count > 0 Then
                    theLine.First.Spare5 = theLine.First.RmbNo
                    Dim q = From c In d.AP_Staff_RmbLines Where c.Spare5 = theLine.First.RmbNo And c.AP_Staff_Rmb.Status = RmbStatus.Draft And c.AP_Staff_Rmb.UserId = theLine.First.AP_Staff_Rmb.UserId And c.AP_Staff_Rmb.PortalId = PortalId Select c.AP_Staff_Rmb
                    If q.Count = 0 Then

                        Dim insert As New AP_Staff_Rmb
                        insert.UserRef = "Deferred"
                        insert.AcctComment = "Contains transactions deferred from previous month"
                        insert.RID = StaffRmbFunctions.GetNewRID(PortalId)
                        insert.CostCenter = theLine.First.AP_Staff_Rmb.CostCenter

                        insert.UserComment = ""
                        insert.UserId = theLine.First.AP_Staff_Rmb.UserId
                        ' insert.PersonalCC = ddlNewChargeTo.Items(0).Value
                        insert.AdvanceRequest = 0.0

                        insert.PortalId = PortalId

                        insert.Status = RmbStatus.Draft

                        insert.Locked = False
                        insert.SupplierCode = theLine.First.AP_Staff_Rmb.SupplierCode

                        insert.Department = theLine.First.AP_Staff_Rmb.Department

                        d.AP_Staff_Rmbs.InsertOnSubmit(insert)
                        d.SubmitChanges()
                        theLine.First.AP_Staff_Rmb = insert

                    Else
                        theLine.First.AP_Staff_Rmb = q.First
                    End If
                    d.SubmitChanges()

                    Dim theUser = UserController.GetUserById(PortalId, theLine.First.AP_Staff_Rmb.UserId)
                    Dim t As Type = GridView1.GetType()
                    Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                    sb.Append("<script language='javascript'>")
                    sb.Append("window.open('mailto:" & theUser.Email & "?subject=Reimbursment " & theLine.First.AP_Staff_Rmb.RID & ": Deferred Transactions');")
                    sb.Append("</script>")
                    Await LoadRmb(hfRmbNo.Value)
                    ScriptManager.RegisterStartupScript(GridView1, t, "email", sb.ToString, False)

                End If
            End If

        End Sub

        Protected Async Sub dlApproved_ItemCommand(ByVal source As Object, ByVal e As System.Web.UI.WebControls.DataListCommandEventArgs) Handles dlProcessed.ItemCommand, dlAdvProcessed.ItemCommand, dlApproved.ItemCommand, dlCancelled.ItemCommand, dlToApprove.ItemCommand, dlSubmitted.ItemCommand, dlPending.ItemCommand, dlAdvApproved.ItemCommand, dlAdvSubmitted.ItemCommand, dlAdvToApprove.ItemCommand, dlAdvApproved.ItemCommand
            If e.CommandName = "Goto" Then
                Await LoadRmb(e.CommandArgument)
                UpdatePanel4.Update()
            ElseIf e.CommandName = "GotoAdvance" Then
                Await LoadAdv(e.CommandArgument)
            End If
        End Sub



#End Region
#Region "OnChange Events"
        Protected Sub ddlLineTypes_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlLineTypes.SelectedIndexChanged
            If lblIncType.Visible And ddlLineTypes.SelectedIndex <> ddlLineTypes.Items.Count - 1 Then
                Dim oldValue = ddlLineTypes.SelectedValue
                ddlLineTypes.Items.Clear()
                Dim lineTypes = From c In d.AP_StaffRmb_PortalLineTypes Where c.PortalId = PortalId Order By c.LocalName Select c.AP_Staff_RmbLineType.LineTypeId, c.LocalName, c.PCode, c.DCode

                If StaffBrokerFunctions.IsDept(PortalId, tbCostcenter.Text) Then
                    lineTypes = lineTypes.Where(Function(x) x.DCode <> "")

                Else
                    lineTypes = lineTypes.Where(Function(x) x.PCode <> "")
                End If
                ddlLineTypes.DataSource = lineTypes
                ddlLineTypes.DataBind()
                lblIncType.Visible = False
                btnAddLine.Enabled = True
            End If

            ResetNewExpensePopup(False)
        End Sub

        Protected Async Sub tbChargeTo_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles tbChargeTo.TextChanged
            'The User selected a new cost centre
            Try
                'Detect if Dept is now Personal or vica versa
                Dim PortalId = CInt(hfPortalId.Value)
                Dim RmbNo = CInt(hfRmbNo.Value)
                Dim Dept = StaffBrokerFunctions.IsDept(PortalId, hfChargeToValue.Value)
                Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo And c.PortalId = PortalId
                If (rmb.Count > 0) Then

                    If Dept <> StaffBrokerFunctions.IsDept(PortalId, rmb.First.CostCenter) Then
                        'We now need to redetermine the AccountCodes
                        rmb.First.CostCenter = hfChargeToValue.Value
                        rmb.First.Department = Dept

                        For Each row In rmb.First.AP_Staff_RmbLines
                            If rmb.First.CostCenter = row.CostCenter Then
                                row.Department = Dept
                                row.AccountCode = GetAccountCode(row.LineType, hfChargeToValue.Value)
                                row.CostCenter = hfChargeToValue.Value
                            End If
                        Next
                        Await SubmitChangesAsync()
                        Await updateApproversListAsync(rmb.First)
                    End If

                    If (rmb.First.Status <> RmbStatus.Draft) Then
                        rmb.First.Status = RmbStatus.Draft
                        Await ResetMenuAsync()
                    End If
                    Await SubmitChangesAsync()
                End If
            Catch ex As Exception
                lblError.Text = "Error in ChargeTo OnChange event: " & ex.Message
                lblError.Visible = True
            End Try

        End Sub

        Protected Async Sub ddlApprovedBy_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlApprovedBy.SelectedIndexChanged
            Dim RmbNo = CInt(hfRmbNo.Value)
            Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo And c.PortalId = PortalId
            Try
                rmb.First.ApprUserId = ddlApprovedBy.SelectedValue
            Catch
                rmb.First.ApprUserId = Nothing
            End Try
            rmb.First.Status = RmbStatus.Draft
            Await SubmitChangesAsync()
            If btnSubmit.Visible And rmb.First.CostCenter IsNot Nothing And rmb.First.ApprUserId IsNot Nothing AndAlso (rmb.First.CostCenter.Length = 6) And (rmb.First.ApprUserId >= 0) Then
                btnSubmit.Enabled = True
            End If
            ScriptManager.RegisterStartupScript(ddlApprovedBy, ddlApprovedBy.GetType(), "selectDrafts", "selectIndex(0)", True)
        End Sub


#End Region
#Region "Formatting and shortcuts"
        Public Function GetRmbTitle(ByVal UserRef As String, ByVal RID As Integer, ByVal RmbDate As Date) As String
            If String.IsNullOrEmpty(UserRef) Then
                UserRef = Translate("Expenses")
            End If

            Dim rtn = Left(UserRef, 20) & "<br />" & "<span style=""font-size: 6.5pt; color: #999999;"">#" & ZeroFill(RID.ToString, 5)
            If RmbDate > (New Date(2010, 1, 1)) Then
                rtn = rtn & ": " & RmbDate.ToShortDateString & "</span>"
            Else
                rtn = rtn & "</span>"
            End If
            Return rtn


        End Function

        Public Function GetLineComment(ByVal comment As String, ByVal Currency As String, ByVal CurrencyValue As Double, ByVal ShortComment As String, Optional ByVal includeInitials As Boolean = True, Optional ByVal explicitStaffInitals As String = Nothing, Optional ByVal Mileage As String = "") As String




            'Prefix initials  // suffix Currency   // Trim to 30 char

            Dim initials As String = ""
            If includeInitials Then
                If Not String.IsNullOrEmpty(explicitStaffInitals) Then
                    initials = UnidecodeSharpFork.Unidecoder.Unidecode(explicitStaffInitals & "-").Substring(0, 3)
                Else
                    initials = UnidecodeSharpFork.Unidecoder.Unidecode(staffInitials.Value & "-").Substring(0, 3)
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

        Private Function FormatNumber(ByVal num As Double) As String
            If (num >= 1000000) Then
                Return (num / 1000000).ToString("0.#") + "M"
            End If

            If (num >= 100000) Then
                Return (num / 1000).ToString("#,0") + "K"
            End If

            If (num >= 10000) Then
                Return (num / 1000D).ToString("0.#") + "K"
            End If

            Return num.ToString("#,0")


        End Function

        Public Function GetAdvTitle(ByVal LocalAdvanceId As Integer, ByVal RequestDate As Date) As String

            Dim rtn = "Advance:<br />" & "<span style=""font-size: 6.5pt; color: #999999;"">Adv#" & ZeroFill(LocalAdvanceId.ToString, 4)
            If RequestDate > (New Date(2010, 1, 1)) Then
                rtn = rtn & ": " & RequestDate.ToShortDateString & "</span>"
            Else
                rtn = rtn & "</span>"
            End If
            Return rtn

            ' Return Left("RMB#" & RmbNo & " " & UserController.GetUser(PortalId, UID, False).DisplayName, 24)
        End Function

        Public Function GetAdvTitleTeam(ByVal LocalAdvanceId As Integer, ByVal UID As Integer, ByVal RequestDate As Date) As String
            Dim Sm = UserController.GetUserById(PortalId, UID)

            Dim rtn = Left(Sm.FirstName & " " & Sm.LastName, 20) & "<br />" & "<span style=""font-size: 6.5pt; color: #999999;"">Adv#" & ZeroFill(LocalAdvanceId.ToString, 4)

            If RequestDate > (New Date(2010, 1, 1)) Then
                rtn = rtn & ": " & RequestDate.ToShortDateString & "</span>"
            Else
                rtn = rtn & "</span>"
            End If
            Return rtn

            ' Return Left("RMB#" & RmbNo & " " & UserController.GetUser(PortalId, UID, False).DisplayName, 24)
        End Function

        Public Function GetRmbTitleTeam(ByVal RID As Integer, ByVal UID As Integer, ByVal RmbDate As Date) As String
            Dim Sm = UserController.GetUserById(PortalId, UID)

            Dim rtn = Left(Sm.FirstName & " " & Sm.LastName, 20) & "<br />" & "<span style=""font-size: 6.5pt; color: #999999;"">#" & ZeroFill(RID.ToString, 5)
            If (RmbDate > (New Date(2010, 1, 1))) Then
                rtn = rtn & ": " & RmbDate.ToShortDateString & "</span>"
            Else
                rtn = rtn & "</span>"
            End If
            Return rtn

            ' Return Left("RMB#" & RmbNo & " " & UserController.GetUser(PortalId, UID, False).DisplayName, 24)
        End Function

        Protected Function GetRmbTitleTeamShort(ByVal RID As Integer, ByVal RmbDate As Date, ByVal name As String) As String

            Dim rtn As String = "<span style=""font-size: 6.5pt; color: #999999;"">#" & ZeroFill(RID.ToString, 5)

            If (RmbDate > (New Date(2010, 1, 1))) Then
                rtn = rtn & ": " & RmbDate.ToShortDateString
            End If
            If (name IsNot Nothing) Then
                rtn = rtn & " - " & name
            End If
            rtn = rtn & "</span>"
            Return rtn

            ' Return Left("RMB#" & RmbNo & " " & UserController.GetUser(PortalId, UID, False).DisplayName, 24)
        End Function

        Protected Function GetAdvTitleTeamShort(ByVal LocalAdvanceId As Integer, ByVal RequestDate As Date, ByVal name As String) As String

            Dim rtn As String = "<span style=""font-size: 6.5pt; color: #999999;"">Adv#" & ZeroFill(LocalAdvanceId.ToString, 4)

            If RequestDate > (New Date(2010, 1, 1)) Then
                rtn = rtn & ": " & RequestDate.ToShortDateString
            End If
            If (name IsNot Nothing) Then
                rtn = rtn & " - " & name
            End If
            rtn = rtn & "</span>"
            Return rtn

            ' Return Left("RMB#" & RmbNo & " " & UserController.GetUser(PortalId, UID, False).DisplayName, 24)
        End Function

        Public Function GetDateFormat() As String
            Dim sdp As String = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.ToLower
            If sdp.IndexOf("d") < sdp.IndexOf("m") Then
                Return "dd/mm/yy"
            Else
                Return "mm/dd/yy"
            End If
        End Function

#End Region

#Region "GetValues"
        Public Function getSelectedTab() As Integer
            If hfRmbNo.Value = "" Then
                Return 0
            End If

            Dim RmbNo As Integer = hfRmbNo.Value
            If RmbNo > 0 Then

                Try
                    Dim rmb_status = (From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo Select c.Status).First
                    Select Case rmb_status
                        Case 0 To 4
                            Return rmb_status
                        Case 10 To 20
                            Return 2
                        Case Else
                            Return 0
                    End Select
                Catch
                    Return 0
                End Try


            Else
                'Advance
                Dim adv = From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = -RmbNo And c.PortalId = PortalId

                If adv.Count > 0 Then
                    Select Case adv.First.RequestStatus
                        'Case RmbStatus.MoreInfo
                        '    Return 0
                        Case Is >= RmbStatus.PendingDownload
                            Return 2
                        Case Else
                            Return adv.First.RequestStatus
                    End Select
                End If

            End If
            Return 0
        End Function

        Protected Function IsAccounts() As Boolean
            'If Not ModuleContext.IsEditable Then
            '    Return False
            'End If
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
            Return status <> RmbStatus.Processed And status <> RmbStatus.PendingDownload And status <> RmbStatus.DownloadFailed And (status <> RmbStatus.Approved Or IsAccounts())


        End Function

        Protected Sub GetMilesForYear(ByVal RMBLineId As Integer, ByVal UID As Integer)
            Dim RMBLine = (From c In d.AP_Staff_RmbLines Where c.RmbLineNo = RMBLineId).First

            Dim TaxDate1 As New Date(2010, 4, 5)
            Dim TaxDate2 As New Date(2010, 4, 5)
            If RMBLine.TransDate.DayOfYear < TaxDate1.DayOfYear Then
                TaxDate1 = "05/04/" & (Year(RMBLine.TransDate) - 1)
                TaxDate2 = "05/04/" & (Year(RMBLine.TransDate))
            Else
                TaxDate1 = "05/04/" & (Year(RMBLine.TransDate))
                TaxDate2 = "05/04/" & (Year(RMBLine.TransDate) + 1)
            End If


            Dim q = (From c In d.AP_Staff_RmbLines Where c.LineType = 7 And c.AP_Staff_Rmb.Status <> RmbStatus.Cancelled And c.Spare3 <> CInt(Settings("Motorcycle")) And c.Spare3 <> CInt(Settings("Bicycle")) And c.AP_Staff_Rmb.UserId = UID And c.TransDate >= TaxDate1 And c.TransDate < TaxDate2 And c.Spare2 <> "" Select Miles = CInt(c.Spare2))

            Try

                If q.Sum > CInt(Settings("MThreshold")) Then

                    If q.Sum - CInt(Settings("MThreshold")) < CInt(RMBLine.Spare2) Then
                        'Need to split

                        Dim Diff As Integer = CInt(Settings("MThreshold")) + CInt(RMBLine.Spare2) - q.Sum


                        Dim insert As New AP_Staff_RmbLine
                        insert.AnalysisCode = RMBLine.AnalysisCode
                        insert.Comment = RMBLine.Comment & "(>" & CInt(Settings("MThreshold")) & " rate)"

                        insert.LineType = RMBLine.LineType
                        insert.Receipt = RMBLine.Receipt
                        insert.ReceiptNo = RMBLine.ReceiptNo
                        insert.RmbNo = RMBLine.RmbNo
                        insert.Spare1 = RMBLine.Spare1
                        insert.Spare2 = RMBLine.Spare2 - Diff
                        RMBLine.Spare2 = Diff
                        RMBLine.GrossAmount = RMBLine.Spare2 * ((CInt(RMBLine.Spare3) + (CInt(Settings("AddPass")) * RMBLine.Spare1)) / 100)

                        insert.Spare3 = RMBLine.Spare3 - (CInt(Settings("MRate1")) - CInt(Settings("MRate2")))
                        insert.GrossAmount = insert.Spare2 * ((CInt(insert.Spare3) + (CInt(Settings("AddPass")) * insert.Spare1)) / 100)
                        insert.Taxable = RMBLine.Taxable
                        insert.TransDate = RMBLine.TransDate
                        insert.VATReceipt = RMBLine.VATReceipt
                        'Dim AccType = Right(ddlChargeTo.SelectedValue, 1)

                        'If insert.VATReceipt Then
                        '    If AccType = "X" Then
                        '        If insert.TransDate >= "04/01/2011" Then
                        '            insert.VATCode = "3"
                        '            insert.VATRate = 20
                        '        Else
                        '            insert.VATCode = "3T"
                        '            insert.VATRate = 17.5
                        '        End If
                        '        insert.VATAmount = insert.GrossAmount / (1 + (100 / insert.VATRate))
                        '    Else
                        '        insert.VATCode = 8
                        '        insert.VATAmount = 0.0
                        '        insert.VATRate = 0.0
                        '    End If
                        'Else
                        '    If AccType = "X" Then
                        '        insert.VATCode = 6
                        '        insert.VATAmount = 0.0
                        '        insert.VATRate = 0.0
                        '    Else
                        '        insert.VATCode = 8
                        '        insert.VATAmount = 0.0
                        '        insert.VATRate = 0.0
                        '    End If
                        'End If


                        d.AP_Staff_RmbLines.InsertOnSubmit(insert)
                        d.SubmitChanges()

                        'Dim r = From c In d.AP_Staff_RmbLineAddStaffs Where c.RmbLineId = RMBLineId
                        'For Each row In r
                        '    Dim person = New AP_Staff_RmbLineAddStaff
                        '    person.UserId = row.UserId
                        '    person.Name = row.Name

                        '    person.RmbLineId = insert.RmbLineNo
                        '    d.AP_Staff_RmbLineAddStaffs.InsertOnSubmit(person)

                        'Next
                        'd.SubmitChanges()

                    Else
                        RMBLine.Spare3 -= (CInt(Settings("MRate1")) - CInt(Settings("MRate2")))
                        RMBLine.GrossAmount = CInt(RMBLine.Spare2) * ((CInt(RMBLine.Spare3) + (CInt(Settings("AddPass")) * RMBLine.Spare1)) / 100)
                        d.SubmitChanges()
                    End If





                End If



            Catch ex As Exception
                lblTest.Text = lblTest.Text & " " & ex.Message
            End Try


        End Sub

        Public Function GetTotal(ByVal theRmbNo As Integer) As Double

            Return (From c In d.AP_Staff_RmbLines Where c.RmbNo = hfRmbNo.Value Select c.GrossAmount).Sum


        End Function

        Public Function IsSelected(ByVal RmbNo As Integer) As Boolean
            If hfRmbNo.Value = "" Then
                Return False
            Else
                Return (CInt(hfRmbNo.Value) = RmbNo)
            End If
        End Function

        Public Function IsAdvSelected(ByVal AdvanceNo As Integer) As Boolean
            If hfRmbNo.Value = "" Then
                Return False
            Else
                Return (CInt(hfRmbNo.Value) = -AdvanceNo)
            End If
        End Function

        Public Function GetNumericRemainingBalance(ByVal mode As Integer) As Double

            Dim statusList = {RmbStatus.Approved, RmbStatus.PendingDownload, RmbStatus.DownloadFailed}
            If mode = 2 Then
                statusList = {RmbStatus.PendingDownload, RmbStatus.DownloadFailed}
            End If

            Dim AccountBalance As Double = 0
            If hfAccountBalance.Value <> "" Then
                AccountBalance = hfAccountBalance.Value
            End If

            '--Get totals from form
            Dim r = (From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value) And PortalId = PortalId).First
            Dim theStaff = StaffBrokerFunctions.GetStaffMember(r.UserId)

            Dim Advance As Double = 0
            If Not r.AdvanceRequest = Nothing Then
                Advance = r.AdvanceRequest
            End If

            Dim rTotal As Double = 0
            Dim rT = (From c In d.AP_Staff_RmbLines Where c.AP_Staff_Rmb.PortalId = PortalId And (c.AP_Staff_Rmb.UserId = theStaff.UserId1 Or c.AP_Staff_Rmb.UserId = theStaff.UserId2) And statusList.Contains(c.AP_Staff_Rmb.Status) Select c.GrossAmount)
            If rT.Count > 0 Then
                rTotal = rT.Sum()
            End If

            Dim a = (From c In d.AP_Staff_AdvanceRequests Where c.PortalId = PortalId And (c.UserId = theStaff.UserId1 Or c.UserId = theStaff.UserId2) And statusList.Contains(c.RequestStatus) Select c.RequestAmount)
            Dim aTotal As Double = 0
            If a.Count > 0 Then
                aTotal = a.Sum()
            End If

            Return AccountBalance + Advance - (rTotal + aTotal)

        End Function

        Public Function GetRemainingBalance() As String
            Return StaffBrokerFunctions.GetFormattedCurrency(PortalId, GetNumericRemainingBalance(1).ToString("0.00"))
        End Function

        Public Function IsWrongType(ByVal CostCenter As String, ByVal LineTypeId As Integer) As Boolean

            Dim isD = StaffBrokerFunctions.IsDept(PortalId, CostCenter)
            Dim rtn As Boolean
            If isD Then
                rtn = d.AP_StaffRmb_PortalLineTypes.Where(Function(x) x.PortalId = PortalId And x.LineTypeId = LineTypeId And x.DCode <> "").Count = 0
            Else
                rtn = d.AP_StaffRmb_PortalLineTypes.Where(Function(x) x.PortalId = PortalId And x.LineTypeId = LineTypeId And x.PCode <> "").Count = 0


            End If
            If rtn Then
                lblWrongType.Visible = True
                pnlError.Visible = True
                btnSubmit.Enabled = False
                btnProcess.Enabled = False
                btnApprove.Enabled = False
            End If
            Return rtn
        End Function

        Public Function GetJsonAccountString(ByVal Account As String) As String
            Dim rtn As String = "/MobileCAS/MobileCAS.svc/GetAccountBalance?CountryURL="
            rtn &= StaffBrokerFunctions.GetSetting("DataserverURL", PortalId)
            rtn &= "&GUID=" & UserInfo.Profile.GetPropertyValue("ssoGUID")
            rtn &= "&PGTIOU=" & UserInfo.Profile.GetPropertyValue("GCXPGTIOU") & "&Account=" & Account
            Return rtn

        End Function


#End Region

#Region "Utilities"
        Private Async Function SubmitChangesAsync() As Task
            d.SubmitChanges()
        End Function

        Private Async Function saveIfNecessaryAsync() As task
            If (btnSave.Text = Translate("btnSave")) Then
                Await btnSave_Click(Me, Nothing)
            End If
        End Function

        Protected Sub Log(ByVal RmbNo As Integer, ByVal Message As String)
            objEventLog.AddLog("Rmb" & RmbNo, Message, PortalSettings, UserId, Services.Log.EventLog.EventLogController.EventLogType.ADMIN_ALERT)
        End Sub

        Protected Function GetAccountCode(ByVal LineTypeId As Integer, ByVal CostCenter As String) As String

            Dim q = From c In d.AP_StaffRmb_PortalLineTypes Where c.LineTypeId = LineTypeId And c.PortalId = PortalId


            If q.Count > 0 Then
                If q.First.PCode.Length = 0 And q.First.DCode.Length > 0 Then
                    Return q.First.DCode
                ElseIf q.First.DCode.Length = 0 And q.First.PCode.Length > 0 Then
                    Return q.First.PCode
                End If

                If StaffBrokerFunctions.IsDept(PortalId, CostCenter) And q.First.DCode.Length > 0 Then
                    Return q.First.DCode
                Else
                    Return q.First.PCode
                End If
            End If
            Return ""

        End Function

        Public Sub LoadDefaultSettings()
            Dim tmc As New DotNetNuke.Entities.Modules.ModuleController
            If Settings("NoReceipt") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "NoReceipt", 5)
            End If
            If Settings("Expire") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "Expire", 3)
            End If
            If Settings("TeamLeaderLimit") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "TeamLeaderLimit", 10000)
            End If
            If Settings("MRate1") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "MRate1", 0.4)
            End If
            'If Settings("MRate2") = "" Then
            '    tmc.UpdateTabModuleSetting(TabModuleId, "MRate2", 25)
            'End If
            'If Settings("MThreshold") = "" Then
            '    tmc.UpdateTabModuleSetting(TabModuleId, "MThreshold", 100000)
            'End If
            'If Settings("AddPass") = "" Then
            '    tmc.UpdateTabModuleSetting(TabModuleId, "AddPass", 5)
            'End If
            If Settings("Motorcycle") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "Motorcycle", 0.25)
            End If
            If Settings("Bicycle") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "Bicycle", 0.05)
            End If
            If Settings("AccountsEmail") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "AccountsEmail", "accounts@your-domain.com")
            End If
            If Settings("SubBreakfast") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "SubBreakfast", 5)
            End If
            If Settings("SubLunch") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "SubLunch", 10)
            End If
            If Settings("SubDinner") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "SubDinner", 15)
            End If
            If Settings("SubDay") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "SubDay", 30)
            End If
            If Settings("EntBreakfast") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "EntBreakfast", 5)
            End If
            If Settings("EntLunch") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "EntLunch", 10)
            End If

            If Settings("EntDinner") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "EntDinner", 15)
            End If
            If Settings("EntOvernight") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "EntOvernight", 5)
            End If
            If Settings("EntDay") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "EntDay", 20)
            End If
            If Settings("MenuSize") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "MenuSize", 15)
            End If
            If Settings("VatAttrib") = "" Then
                tmc.UpdateTabModuleSetting(TabModuleId, "VatAttrib", False)
            End If
            tmc.UpdateTabModuleSetting(TabModuleId, "isLoaded", "No")

            SynchronizeModule()

        End Sub

        Protected Function ZeroFill(ByVal number As Integer, ByVal len As Integer) As String
            If number.ToString.Length > len Then
                Return Right(number.ToString, len)
            Else
                Dim Filler As String = ""
                For i As Integer = 1 To len - number.ToString.Length
                    Filler &= "0"
                Next
                Return Filler & number.ToString
            End If


        End Function

        Protected Sub ResetNewExpensePopup(ByVal blankValues As Boolean)
            Try


                Dim lt = From c In d.AP_Staff_RmbLineTypes Where c.LineTypeId = ddlLineTypes.SelectedValue
                If lt.Count > 0 Then
                    Dim Comment As String = ""
                    Dim Amount As Double = 0.0
                    Dim theDate As Date = Today
                    Dim VAT As Boolean = False
                    Dim Receipt As Boolean = True

                    If Not blankValues Then


                        Try

                            If Not (theControl Is Nothing) Then
                                Dim ucTypeOld As Type = theControl.GetType()
                                Comment = CStr(ucTypeOld.GetProperty("Comment").GetValue(theControl, Nothing))
                                theDate = CDate(ucTypeOld.GetProperty("theDate").GetValue(theControl, Nothing))
                                Amount = CDbl(ucTypeOld.GetProperty("Amount").GetValue(theControl, Nothing))
                                VAT = CStr(ucTypeOld.GetProperty("VAT").GetValue(theControl, Nothing))
                                Receipt = CStr(ucTypeOld.GetProperty("Receipt").GetValue(theControl, Nothing))



                            End If

                        Catch ex As Exception

                        End Try
                    End If
                    ' Save the standard values



                    phLineDetail.Controls.Clear()

                    ddlOverideTax.SelectedIndex = 0

                    theControl = LoadControl(lt.First.ControlPath)

                    theControl.ID = "theControl"
                    phLineDetail.Controls.Add(theControl)

                    Dim ucType As Type = theControl.GetType()

                    ucType.GetProperty("Comment").SetValue(theControl, Comment, Nothing)
                    ucType.GetProperty("Amount").SetValue(theControl, Amount, Nothing)
                    ucType.GetProperty("theDate").SetValue(theControl, theDate, Nothing)
                    ucType.GetProperty("VAT").SetValue(theControl, VAT, Nothing)
                    ucType.GetProperty("Receipt").SetValue(theControl, Receipt, Nothing)
                    ucType.GetProperty("Spare1").SetValue(theControl, "", Nothing)
                    ucType.GetProperty("Spare2").SetValue(theControl, "", Nothing)
                    ucType.GetProperty("Spare3").SetValue(theControl, "", Nothing)
                    ucType.GetProperty("Spare4").SetValue(theControl, "", Nothing)
                    ucType.GetProperty("Spare5").SetValue(theControl, "", Nothing)
                    ucType.GetMethod("Initialize").Invoke(theControl, New Object() {Settings})


                    'Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value)
                    'If rmb.Count > 0 Then
                    '    'Dim codes = From c In lt.First.AP_StaffRmb_PortalLineTypes Where c.PortalId = PortalId
                    '    'If codes.Count > 0 Then
                    '    '    If codes.First.DCode.Length = 0 Or rmb.First.CostCenter = StaffBrokerFunctions.GetStaffMember(rmb.First.UserId).CostCenter Then
                    '    '        ddlAccountCode.SelectedValue = codes.First.PCode
                    '    '    Else
                    '    '        ddlAccountCode.SelectedValue = codes.First.DCode
                    '    '    End If
                    '    'End If

                    'End If
                    ddlAccountCode.SelectedValue = GetAccountCode(lt.First.LineTypeId, tbCostcenter.Text)


                End If


            Catch ex As Exception
                StaffBrokerFunctions.EventLog("Error Resetting Expense Popup", ex.ToString, UserId)
            End Try
        End Sub

        Protected Sub SendApprovalEmail(ByVal theRmb As AP_Staff_Rmb)
            Try

                Dim SpouseId As Integer = StaffBrokerFunctions.GetSpouseId(theRmb.UserId)
                Dim ownerMessage As String = StaffBrokerFunctions.GetTemplate("RmbConfirmation", PortalId)
                Dim approverMessage As String = StaffBrokerFunctions.GetTemplate("RmbApproverEmail", PortalId)
                Dim approver = UserController.GetUserById(theRmb.PortalId, theRmb.ApprUserId)
                Dim toEmail = approver.Email
                Dim toName = approver.FirstName
                Dim Approvers = approver.DisplayName

                ownerMessage = ownerMessage.Replace("[APPROVER]", Approvers).Replace("[EXTRA]", "")
                If (From c In theRmb.AP_Staff_RmbLines Where c.Receipt = True And (Not c.ReceiptImageId Is Nothing)).Count > 0 Then
                    ownerMessage = ownerMessage.Replace("[STAFFACTION]", Translate("PostReceipts"))
                Else
                    ownerMessage = ownerMessage.Replace("[STAFFACTION]", Translate("NoPostReceipts"))
                End If
                ownerMessage = ownerMessage.Replace("[PRINTOUT]", "<a href='" & Request.Url.Scheme & "://" & Request.Url.Authority & Request.ApplicationPath & "DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & theRmb.RMBNo & "&UID=" & theRmb.UserId & "' target-'_blank' style='width: 134px; display:block;)'><div style='text-align: center; width: 122px; margin: 10px;'><img src='" _
                    & Request.Url.Scheme & "://" & Request.Url.Authority & Request.ApplicationPath & "DesktopModules/AgapeConnect/StaffRmb/Images/PrintoutIcon.jpg' /><br />Printout</div></a><style> a div:hover{border: solid 1px blue;}</style>")

                'Email to the submitter here
                DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", UserInfo.Email, "", Translate("EmailSubmittedSubject").Replace("[RMBNO]", theRmb.RID), ownerMessage, "", "HTML", "", "", "", "")

                'Send Approvers Instructions Here
                If toEmail.Length > 0 Then
                    If StaffRmbFunctions.isStaffAccount(theRmb.CostCenter) Then
                        'Personal Reimbursement
                        approverMessage = approverMessage.Replace("[STAFFNAME]", UserInfo.DisplayName).Replace("[RMBNO]", theRmb.RMBNo).Replace("[USERREF]", IIf(theRmb.UserRef <> "", theRmb.UserRef, "None"))
                        approverMessage = approverMessage.Replace("[APPRNAME]", toName)
                        approverMessage = approverMessage.Replace("[TEAMLEADERLIMIT]", StaffBrokerFunctions.GetSetting("Currency", PortalId) & Settings("TeamLeaderLimit"))
                        If theRmb.UserComment <> "" Then
                            approverMessage = approverMessage.Replace("[COMMENTS]", Translate("EmailComments") & "<br />" & theRmb.UserComment)
                        Else
                            approverMessage = approverMessage.Replace("[COMMENTS]", "")
                        End If
                        DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", toEmail, "", Translate("SubmittedApprEmailSubject").Replace("[STAFFNAME]", UserInfo.FirstName & " " & UserInfo.LastName), approverMessage, "", "HTML", "", "", "", "")
                    Else
                        approverMessage = approverMessage.Replace("[STAFFNAME]", UserInfo.DisplayName).Replace("[RMBNO]", theRmb.RMBNo).Replace("[USERREF]", IIf(theRmb.UserRef <> "", theRmb.UserRef, "None"))
                        approverMessage = approverMessage.Replace("[APPRNAME]", Left(toName, Math.Max(toName.Length - 2, 0)))
                        approverMessage = approverMessage.Replace("[TEAMLEADERLIMIT]", StaffBrokerFunctions.GetSetting("Currency", PortalId) & Settings("TeamLeaderLimit"))
                        If theRmb.UserComment <> "" Then
                            approverMessage = approverMessage.Replace("[COMMENTS]", Translate("EmailComments") & "<br />" & theRmb.UserComment)
                        Else
                            approverMessage = approverMessage.Replace("[COMMENTS]", "")
                        End If
                        DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", Left(toEmail, toEmail.Length - 1), "", Translate("SubmittedApprEmailSubject").Replace("[STAFFNAME]", UserInfo.FirstName & " " & UserInfo.LastName), approverMessage, "", "HTML", "", "", "", "")
                    End If
                End If
            Catch ex As Exception
                lblError.Text = "Error Sending Approval eMail: " & ex.Message & ex.StackTrace
                lblError.Visible = True
            End Try

        End Sub

        Public Function Translate(ByVal ResourceString As String) As String
            Return DotNetNuke.Services.Localization.Localization.GetString(ResourceString & ".Text", LocalResourceFile)

        End Function

        Public Function GetLocalTypeName(ByVal LineTypeId As Integer) As String
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
#End Region

#Region "Downloading"
        Protected Function DownloadRmbSingle(ByVal RmbNo As Integer) As String
            Dim rtn As String = ""
            Dim theRmb = From c In d.AP_Staff_RmbLines Where c.RmbNo = RmbNo

            If theRmb.Count > 0 Then
                Dim theUserId = (From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo Select c.UserId).First
                Dim theUser = UserController.GetUserById(PortalId, theUserId)



                Dim ref = "R" & ZeroFill(theRmb.First.AP_Staff_Rmb.RID, 5)
                Dim theDate As String = "=""" & Today.ToString("dd-MMM-yy") & """"

                For Each line In theRmb
                    theDate = "=""" & line.TransDate.ToString("dd-MMM-yy") & """"

                    If line.Taxable Then
                        rtn &= "=""" & Settings("TaxAccountsReceivable") & ""","
                    Else
                        If line.AccountCode Is Nothing Then

                            rtn &= "=""" & GetAccountCode(line.LineType, line.CostCenter) & ""","
                        Else
                            rtn &= "=""" & line.AccountCode & ""","
                        End If
                    End If

                    rtn &= "=""" & line.CostCenter & ""","
                    rtn &= ref & ","
                    rtn &= theDate & ","
                    Dim Debit As String = ""
                    Dim Credit As String = ""
                    If line.GrossAmount > 0 Then
                        Debit = (line.GrossAmount.ToString("0.00"))
                    Else
                        Credit = -line.GrossAmount.ToString("0.00")
                    End If
                    Dim shortComment = GetLineComment(line.Comment, line.OrigCurrency, line.OrigCurrencyAmount, line.ShortComment, True, Left(theUser.FirstName, 1) & Left(theUser.LastName, 1), IIf(line.AP_Staff_RmbLineType.TypeName = "Mileage", line.Spare2, ""))
                    rtn &= GetOrderedString(shortComment,
                                         Debit, Credit)






                    'If line.GrossAmount > 0 Then
                    '    rtn &= line.GrossAmount.ToString("0.00") & ",,"
                    'Else

                    '    rtn &= "," & -line.GrossAmount.ToString("0.00") & ","
                    'End If

                    'rtn &= "=""" & Left(theUser.FirstName, 1) & Left(theUser.LastName, 1) & "-" & line.Comment & """" & vbNewLine

                Next


                ' IF we opt to go like UK... take out the "IF Line.Taxable..." at the beginning of the above loop
                ' Then add two more transactions. One to back out on 7012 (we will need to get this from a setting)
                ' Then back in on Tax Accounts Payable

                theDate = "=""" & Today.ToString("dd-MMM-yy") & """"
                Dim theStaff = StaffBrokerFunctions.GetStaffMember(theUserId)
                Dim PACMode = (
                    theStaff.CostCenter = "" And StaffBrokerFunctions.GetStaffProfileProperty(theStaff.StaffId, "PersonalAccountCode") <> "")

                If theRmb.Count > 0 Then




                    Dim RmbTotal As Double = (From c In theRmb Select c.GrossAmount).Sum


                    If PACMode Then
                        If RmbTotal <> 0 Then

                            rtn &= "=""" & StaffBrokerFunctions.GetStaffProfileProperty(theStaff.StaffId, "PersonalAccountCode") & ""","
                            rtn &= "=""" & Settings("ControlAccount") & """" & ","
                            rtn &= ref & ","
                            rtn &= theDate & ","

                            Dim Debit As String = ""
                            Dim Credit As String = ""



                            If RmbTotal > 0 Then
                                Credit = RmbTotal.ToString("0.00")

                                'rtn &= "," & RmbTotal.ToString("0.00") & ","
                            Else
                                'rtn &= -RmbTotal.ToString("0.00") & ",,"
                                Debit = -RmbTotal.ToString("0.00")
                            End If

                            'rtn &= Left(theUser.FirstName, 1) & Left(theUser.LastName, 1) & "-Payment for " & ref & vbNewLine
                            rtn &= GetOrderedString(Left(theUser.FirstName, 1) & Left(theUser.LastName, 1) & "-Payment for " & ref,
                                                    Debit, Credit)


                        End If

                    Else
                        Dim rmbAdvance As Double = 0.0
                        Dim rmbAdvanceBalance As Double = 99999.99

                        If theRmb.Count > 0 Then

                            Dim Adv As Double = theRmb.First.AP_Staff_Rmb.AdvanceRequest
                            If Not Adv = Nothing Then


                                If Adv > 0 Then
                                    rmbAdvance = Math.Min(Math.Min(RmbTotal, Adv), rmbAdvanceBalance)
                                ElseIf Adv = -1 Then
                                    rmbAdvance = Math.Min(RmbTotal, rmbAdvanceBalance)
                                End If

                            End If


                        End If



                        RmbTotal -= rmbAdvance





                        If RmbTotal <> 0 Then



                            rtn &= "=""" & Settings("AccountsPayable") & ""","
                            rtn &= "=""" & theStaff.CostCenter & """" & ","
                            rtn &= ref & ","
                            rtn &= theDate & ","

                            Dim Debit As String = ""
                            Dim Credit As String = ""



                            If RmbTotal > 0 Then
                                Credit = RmbTotal.ToString("0.00")

                                'rtn &= "," & RmbTotal.ToString("0.00") & ","
                            Else
                                'rtn &= -RmbTotal.ToString("0.00") & ",,"
                                Debit = -RmbTotal.ToString("0.00")
                            End If

                            'rtn &= Left(theUser.FirstName, 1) & Left(theUser.LastName, 1) & "-Payment for " & ref & vbNewLine
                            rtn &= GetOrderedString(Left(theUser.FirstName, 1) & Left(theUser.LastName, 1) & "-Payment for " & ref,
                                                    Debit, Credit)


                        End If
                        If rmbAdvance <> 0 Then

                            rtn &= "=""" & Settings("AccountsReceivable") & ""","

                            rtn &= "=""" & theStaff.CostCenter & """" & ","

                            rtn &= ref & ","
                            rtn &= theDate & ","

                            Dim Debit As String = ""
                            Dim Credit As String = ""

                            If rmbAdvance > 0 Then
                                Credit = rmbAdvance.ToString("0.00")
                                'rtn &= "," & rmbAdvance.ToString("0.00") & ","
                            Else
                                Debit = -rmbAdvance.ToString("0.00")
                                ' rtn &= -rmbAdvance.ToString("0.00") & ",,"
                            End If

                            '  rtn &= Left(theUser.FirstName, 1) & Left(theUser.LastName, 1) & "-Pay off advance with " & ref & vbNewLine
                            rtn &= GetOrderedString(Left(theUser.FirstName, 1) & Left(theUser.LastName, 1) & "-Pay off advance with " & ref,
                                                    Debit, Credit)

                        End If



                    End If
                End If
            End If

            Return rtn
        End Function

        Protected Function DownloadAdvSingle(ByVal AdvanceNo As Integer) As String
            Dim rtn As String = ""
            Dim theAdv = From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = AdvanceNo And c.PortalId = PortalId
            If theAdv.Count > 0 Then
                Dim ac = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)
                Dim theUser = UserController.GetUserById(PortalId, theAdv.First.UserId)
                Dim StaffMember = StaffBrokerFunctions.GetStaffMember(theAdv.First.UserId)

                'First Debit 12xx
                Dim ref = "A" & ZeroFill(theAdv.First.LocalAdvanceId, 5)
                'Dim theDate As String = "=""" & Today.ToString("dd-MMM-yy") & """"
                Dim theDate As String = "=""" & theAdv.First.RequestDate.Value.ToString("dd-MMM-yy") & """"
                rtn &= "=""" & Settings("AccountsReceivable") & ""","
                rtn &= "=""" & StaffMember.CostCenter & ""","
                rtn &= ref & ","
                rtn &= theDate & ","

                Dim Debit As String = ""
                Dim Credit As String = ""
                If theAdv.First.RequestAmount > 0 Then
                    Debit = (theAdv.First.RequestAmount.Value.ToString("0.00"))
                Else
                    Credit = (-theAdv.First.RequestAmount.Value).ToString("0.00")
                End If
                Dim curSuffix = ""
                If Not String.IsNullOrEmpty(theAdv.First.OrigCurrency) Then

                    If theAdv.First.OrigCurrency <> ac Then
                        curSuffix = "-" & theAdv.First.OrigCurrency & theAdv.First.OrigCurrencyAmount.Value.ToString("0.00").Replace(".00", "")

                    End If
                End If
                rtn &= GetOrderedString(Left(theUser.FirstName, 1) & Left(theUser.LastName, 1) & "-Adv#" & theAdv.First.LocalAdvanceId & curSuffix,
                                        Debit, Credit)

                'Now Credit 23xx
                Dim PAC = StaffBrokerFunctions.GetStaffProfileProperty(StaffMember.StaffId, "PersonalAccountCode")
                Dim PACMode = (
                     StaffMember.CostCenter = "" And PAC <> "")

                If PACMode Then

                    rtn &= "=""" & PAC & ""","
                    rtn &= "=""" & Settings("ControlAccount") & ""","
                Else
                    rtn &= "=""" & Settings("AccountsPayable") & ""","
                    rtn &= "=""" & StaffMember.CostCenter & ""","
                End If

                rtn &= ref & ","
                rtn &= theDate & ","

                Debit = ""
                Credit = ""
                If theAdv.First.RequestAmount > 0 Then
                    Credit = (theAdv.First.RequestAmount.Value.ToString("0.00"))
                Else
                    Debit = (-theAdv.First.RequestAmount.Value).ToString("0.00")
                End If





                rtn &= GetOrderedString(Left(theUser.FirstName, 1) & Left(theUser.LastName, 1) & "-Adv#" & theAdv.First.LocalAdvanceId & curSuffix,
                                        Debit, Credit)

            End If
            Return rtn
        End Function

        Protected Function GetOrderedString(ByVal Desc As String, ByVal Debit As String, ByVal Credit As String, Optional Company As String = "") As String
            Dim format As String = "DDC"
            If CStr(Settings("DownloadFormat")) <> "" Then
                format = CStr(Settings("DownloadFormat"))
            End If


            Dim CompanyName = CStr(StaffBrokerFunctions.GetSetting("CompanyName", PortalId))
            If Company <> "" Then
                CompanyName = Company
            End If

            Select Case format
                Case "DCD"
                    Return "=""" & Debit & """,=""" & Credit & """,=""" & Desc & """" & vbNewLine
                Case "CDCD"
                    Return "=""" & CompanyName & """,=""" & Debit & """,=""" & Credit & """,=""" & Desc & """" & vbNewLine
                Case "CDDC"
                    Return "=""" & CompanyName & """,=""" & Desc & """,=""" & Debit & """,=""" & Credit & """" & vbNewLine
                Case Else 'including DDC
                    Return "=""" & Desc & """,=""" & Debit & """,=""" & Credit & """" & vbNewLine
            End Select
        End Function

        Protected Async Sub DownloadBatch(Optional ByVal MarkAsProcessed As Boolean = False)
            Dim downloadStatuses() As Integer = {RmbStatus.PendingDownload, RmbStatus.DownloadFailed}
            Log(0, "Downloading Batched Transactions")

            Dim pendDownload = From c In d.AP_Staff_Rmbs Where downloadStatuses.Contains(c.Status) And c.PortalId = PortalId

            Dim export As String = "Account,Subaccount,Ref,Date," & GetOrderedString("Description", "Debit", "Credit", "Company")
            Dim RmbList As New List(Of Integer)
            For Each Rmb In pendDownload
                Log(Rmb.RMBNo, "Downloading Rmb")
                export &= DownloadRmbSingle(Rmb.RMBNo)

                RmbList.Add(Rmb.RMBNo)

            Next
            Dim pendDownloadAdv = From c In d.AP_Staff_AdvanceRequests Where downloadStatuses.Contains(c.RequestStatus) And c.PortalId = PortalId

            Dim AdvList As New List(Of Integer)
            For Each adv In pendDownloadAdv
                Log(adv.AdvanceId, "Downloading Advance")
                export &= DownloadAdvSingle(adv.AdvanceId)

                AdvList.Add(adv.AdvanceId)

            Next



            If (MarkAsProcessed) Then

                If Not RmbList Is Nothing Then
                    Dim q = From c In d.AP_Staff_Rmbs Where RmbList.Contains(c.RMBNo) And c.PortalId = PortalId

                    For Each row In q
                        row.Status = RmbStatus.Processed
                        row.ProcDate = Now
                        Log(row.RMBNo, "Marked as Processed - after a manual download")
                    Next
                End If

                If Not AdvList Is Nothing Then

                    Dim r = From c In d.AP_Staff_AdvanceRequests Where AdvList.Contains(c.AdvanceId) And c.PortalId = PortalId

                    For Each row In r
                        row.RequestStatus = RmbStatus.Processed
                        row.ProcessedDate = Now
                        Log(row.AdvanceId, "Advance Marked as Processed - after a manual download")
                    Next

                End If

                d.SubmitChanges()




                If hfRmbNo.Value <> "" Then
                    If hfRmbNo.Value > 0 Then
                        Await LoadRmb(CInt(hfRmbNo.Value))
                    Else
                        Await LoadAdv(-CInt(hfRmbNo.Value))
                    End If


                End If

                Await ResetMenuAsync()



            End If


            Dim t As Type = Me.GetType()
            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            sb.Append("<script language='javascript'>")
            sb.Append("closePopupDownload();")
            sb.Append("</script>")
            ScriptManager.RegisterClientScriptBlock(Page, t, "popupDownload", sb.ToString, False)


            Session("RmbList") = RmbList
            Session("AdvList") = AdvList
            Dim attachment As String = "attachment; filename=RmbDownload " & Today.ToString("yy-MM-dd") & ".csv"




            HttpContext.Current.Response.Clear()
            HttpContext.Current.Response.ClearHeaders()
            HttpContext.Current.Response.ClearContent()
            HttpContext.Current.Response.AddHeader("content-disposition", attachment)
            HttpContext.Current.Response.ContentType = "text/csv"
            HttpContext.Current.Response.AddHeader("Pragma", "public")
            HttpContext.Current.Response.Write(export)
            HttpContext.Current.Response.End()

        End Sub




#End Region



#Region "UKOnly"
        'Protected Function GetAnalysisCode(ByVal LineType As Integer, Optional ByVal CCin As String = "-1") As String
        '    Dim Acc = From c In d.AP_StaffRmb_PortalLineTypes Where c.PortalId = PortalId And c.LineTypeId = LineType Select c.DCode, c.PCode
        '    Dim CC = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.CostCenter)

        '    If CCin = "-1" Then
        '        If CC.Count > 0 And Acc.Count > 0 Then
        '            If Right(CC.First, 1) = "X" Then
        '                Return "D-" & Left(CC.First, 3) & "X-" & Acc.First.DCode
        '            Else
        '                Return "P-" & Left(CC.First, 3) & "0-" & Acc.First.PCode
        '            End If

        '        Else
        '            Return ""
        '        End If
        '    Else
        '        If Acc.Count > 0 Then
        '            If Right(CCin, 1) = "X" Then
        '                Return "D-" & Left(CCin, 3) & "X-" & Acc.First.DCode
        '            Else
        '                Return "P-" & Left(CCin, 3) & "0-" & Acc.First.PCode
        '            End If

        '        Else
        '            Return ""
        '        End If
        '    End If



        ' End Function

#End Region

#Region "Advance Buttons"
        Protected Async Sub btnAdvanceRequest_Click(sender As Object, e As System.EventArgs) Handles btnAdvanceRequest.Click
            Dim insert As New AP_Staff_AdvanceRequest
            insert.Error = False
            insert.ErrorMessage = ""
            insert.LocalAdvanceId = StaffRmbFunctions.GetNewAdvId(PortalId)
            insert.RequestAmount = StaffAdvanceRmb1.Amount
            insert.RequestText = StaffAdvanceRmb1.ReqMessage
            insert.UserId = UserId
            insert.RequestDate = Today
            insert.PortalId = PortalId
            insert.RequestStatus = RmbStatus.Submitted
            insert.OrigCurrency = hfOrigCurrency.Value
            insert.OrigCurrencyAmount = Double.Parse(hfOrigCurrencyValue.Value, New CultureInfo(""))

            d.AP_Staff_AdvanceRequests.InsertOnSubmit(insert)
            d.SubmitChanges()
            'Send Confirmation
            Dim Auth = UserController.GetUserById(PortalId, Settings("AuthUser"))
            Dim AuthAuth = UserController.GetUserById(PortalId, Settings("AuthAuthUser"))

            'Dim ConfMessage As String = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("AdvConfirmation", PortalId))
            Dim ConfMessage As String = StaffBrokerFunctions.GetTemplate("AdvConfirmation", PortalId)
            ConfMessage = ConfMessage.Replace("[STAFFNAME]", UserInfo.DisplayName).Replace("[ADVNO]", insert.LocalAdvanceId)
            Dim myApprovers = Await StaffRmbFunctions.getAdvApproversAsync(insert, Settings("TeamLeaderLimit"), Nothing, Nothing)
            If myApprovers.SpouseSpecial And insert.UserId <> Auth.UserID Then
                ConfMessage = ConfMessage.Replace("[EXTRA]", Translate("AdvSpouseSpecial").Replace("[AUTHUSER]", Auth.DisplayName))
            ElseIf myApprovers.AmountSpecial And insert.UserId <> Auth.UserID Then
                ConfMessage = ConfMessage.Replace("[EXTRA]", Translate("AdvAmountSpecial").Replace("[TEAMLEADERLIMIT]", StaffBrokerFunctions.GetSetting("Currency", PortalId) & Settings("TeamLeaderLimit")).Replace("[AUTHUSER]", Auth.DisplayName) & " ")

            ElseIf insert.UserId = Auth.UserID And (myApprovers.SpouseSpecial Or myApprovers.AmountSpecial) Then
                ConfMessage = ConfMessage.Replace("[EXTRA]", Translate("AdvAuthSpecial").Replace("[AUTHAUTHUSER]", AuthAuth.DisplayName))
            Else
                ConfMessage = ConfMessage.Replace("[EXTRA]", "")
            End If

            Dim message2 As String = StaffBrokerFunctions.GetTemplate("AdvApproverEmail", PortalId)

            If myApprovers.AmountSpecial Then
                'message2 = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("AdvLargeTransaction", PortalId))
                message2 = StaffBrokerFunctions.GetTemplate("AdvLargeTransaction", PortalId)
            End If
            Dim approver = UserController.GetUserById(insert.PortalId, insert.ApproverId)
            Dim toEmail = approver.Email
            Dim toName = approver.FirstName
            Dim Approvers = approver.DisplayName

            message2 = message2.Replace("[STAFFNAME]", UserInfo.DisplayName).Replace("[ADVNO]", insert.AdvanceId)
            message2 = message2.Replace("[AMOUNT]", StaffBrokerFunctions.GetSetting("Currency", PortalId) & insert.RequestAmount)
            message2 = message2.Replace("[APPRNAME]", Left(toName, Math.Max(toName.Length - 2, 0)))
            message2 = message2.Replace("[TEAMLEADERLIMIT]", StaffBrokerFunctions.GetSetting("Currency", PortalId) & Settings("TeamLeaderLimit"))

            message2 = message2.Replace("[COMMENTS]", insert.RequestText)

            If toEmail.Length > 0 Then
                ' DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agape.org.uk", Left(toEmail, toEmail.Length - 1), "donotreply@agape.org.uk", "Reimbursement for " & UserInfo.FirstName & " " & UserInfo.LastName, message2, "", "HTML", "", "", "", "")
                DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", toEmail, "", Translate("AdvSubmittedApprEmailSubject").Replace("[STAFFNAME]", UserInfo.FirstName & " " & UserInfo.LastName), message2, "", "HTML", "", "", "", "")

            End If

            ConfMessage = ConfMessage.Replace("[APPROVER]", Approvers)
            '  DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agape.org.uk", UserInfo.Email, "donotreply@agape.org.uk", "Reimbursement #" & theRmb.RMBNo, message, Server.MapPath("/Portals/0/RmbForm" & theRmb.RMBNo & ".htm"), "HTML", "", "", "", "")
            DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", UserInfo.Email, "", Translate("AdvEmailSubmittedSubject").Replace("[ADVNO]", insert.LocalAdvanceId), ConfMessage, "", "HTML", "", "", "", "")


            'Need to load the Advance!
            Await ResetMenuAsync()

        End Sub

        Protected Async Sub btnAdvApprove_Click(sender As Object, e As System.EventArgs) Handles btnAdvApprove.Click
            btnAdvSave_Click(Me, Nothing)
            Dim q = From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = -CInt(hfRmbNo.Value) And c.PortalId = PortalId
            If q.Count > 0 Then
                If q.First.RequestStatus = RmbStatus.Submitted Then
                    q.First.RequestStatus = RmbStatus.Approved
                    q.First.ApproverId = UserId
                    q.First.ApprovedDate = Today
                    q.First.Period = Nothing
                    q.First.Year = Nothing

                    d.SubmitChanges()


                    Dim Auth = UserController.GetUserById(PortalId, Settings("AuthUser"))
                    Dim AuthAuth = UserController.GetUserById(PortalId, Settings("AuthAuthUser"))
                    Dim myApprovers = Await StaffRmbFunctions.getAdvApproversAsync(q.First, Settings("TeamLeaderLimit"), Auth, AuthAuth)
                    Dim SpouseId As Integer = StaffBrokerFunctions.GetSpouseId(q.First.UserId)

                    Dim ObjAppr As UserInfo = UserController.GetUserById(PortalId, Me.UserId)
                    Dim theUser As UserInfo = UserController.GetUserById(PortalId, q.First.UserId)

                    Dim dr As New TemplatesDataContext
                    'Dim ApprMessage As String = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("AdvApprovedEmail-ApproversVersion", PortalId))
                    Dim ApprMessage As String = StaffBrokerFunctions.GetTemplate("AdvApprovedEmail-ApproversVersion", PortalId)

                    ApprMessage = ApprMessage.Replace("[APPRNAME]", ObjAppr.DisplayName).Replace("[ADVNO]", q.First.LocalAdvanceId).Replace("[STAFFNAME]", theUser.DisplayName)


                    For Each row In (From c In myApprovers.UserIds Where c.UserID <> q.First.UserId And c.UserID <> SpouseId)
                        ApprMessage = ApprMessage.Replace("[THISAPPRNAME]", row.DisplayName)
                        'DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agape.org.uk", row.Email, "donotreply@agape.org.uk", "Rmb#:" & hfRmbNo.Value & " has been approved by " & ObjAppr.DisplayName, ApprMessage, "", "HTML", "", "", "", "")
                        DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", row.Email, "", Translate("AdvEmailApprovedSubject").Replace("[ADVNO]", q.First.LocalAdvanceId).Replace("[APPROVER]", ObjAppr.DisplayName), ApprMessage, "", "HTML", "", "", "", "")

                    Next

                    'SEND APRROVE EMAIL


                    ' Dim Emessage As String = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("AdvApprovedEmail", PortalId))
                    Dim Emessage As String = StaffBrokerFunctions.GetTemplate("AdvApprovedEmail", PortalId)

                    Emessage = Emessage.Replace("[STAFFNAME]", theUser.DisplayName).Replace("[ADVNO]", q.First.LocalAdvanceId)
                    Emessage = Emessage.Replace("[APPROVER]", ObjAppr.DisplayName)

                    d.SubmitChanges()

                    ' DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agape.org.uk", theUser.Email, "donotreply@agape.org.uk", "Rmb#: " & hfRmbNo.Value & "-" & rmb.First.UserRef & " has been approved", Emessage, "", "HTML", "", "", "", "")
                    DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", theUser.Email, "", Translate("AdvEmailApprovedSubject").Replace("[ADVNO]", q.First.LocalAdvanceId).Replace("[APPROVER]", ObjAppr.DisplayName), Emessage, "", "HTML", "", "", "", "")

                    Dim loadAdvTask = LoadAdv(-hfRmbNo.Value)

                    Log(q.First.AdvanceId, "Advance Approved")

                    SendMessage(Translate("AdvanceApproved").Replace("[ADVANCEID]", q.First.LocalAdvanceId), "selectIndex(2);")
                    Await loadAdvTask

                End If
            End If
        End Sub

        Protected Async Sub btnAdvReject_Click(sender As Object, e As System.EventArgs) Handles btnAdvReject.Click, btnAdvCancel.Click
            Dim q = From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = -CInt(hfRmbNo.Value) And c.PortalId = PortalId

            If q.Count > 0 Then
                Dim LockedList() = {RmbStatus.PendingDownload, RmbStatus.DownloadFailed, RmbStatus.Processed}
                If LockedList.Contains(q.First.RequestStatus) Then
                    SendMessage(Translate("AdvLocked") & "<br />")
                Else
                    q.First.RequestStatus = RmbStatus.Cancelled
                    Log(q.First.AdvanceId, "Advance Cancelled")
                    d.SubmitChanges()

                    Dim Message As String = StaffBrokerFunctions.GetTemplate("AdvCancelled", PortalId)

                    ' Dim dr As New TemplatesDataContext
                    '  Dim Message As String = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("AdvCancelled", PortalId))

                    Dim StaffMbr = UserController.GetUserById(PortalId, q.First.UserId)

                    Message = Message.Replace("[STAFFNAME]", StaffMbr.FirstName)
                    Message = Message.Replace("[APPRNAME]", UserInfo.FirstName & " " & UserInfo.LastName)
                    Message = Message.Replace("[APPRFIRSTNAME]", UserInfo.FirstName)

                    ' DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agape.org.uk", theUser.Email, "donotreply@agape.org.uk", "Rmb#: " & hfRmbNo.Value & "-" & rmb.First.UserRef & " has been cancelled", Message, "", "HTML", "", "", "", "")
                    DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agapeconnect.me", StaffMbr.Email, "", Translate("AdvEmailCancelledSubject").Replace("[ADVNO]", q.First.LocalAdvanceId), Message, "", "HTML", "", "", "", "")

                    Await LoadAdv(-hfRmbNo.Value)

                End If
            End If

        End Sub

        Protected Async Sub btnAdvSave_Click(sender As Object, e As System.EventArgs) Handles btnAdvSave.Click
            Dim q = From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = -CInt(hfRmbNo.Value) And c.PortalId = PortalId
            If q.Count > 0 Then
                Dim LockedList() = {RmbStatus.PendingDownload, RmbStatus.DownloadFailed, RmbStatus.Processed}
                If LockedList.Contains(q.First.RequestStatus) Then
                    lblAdvErr.Text = "* " & Translate("AdvLocked") & "<br />"
                Else
                    lblAdvErr.Text = ""
                    q.First.RequestAmount = CDbl(AdvAmount.Text)
                    q.First.OrigCurrencyAmount = CDbl(hfOrigCurrencyValue.Value)
                    q.First.OrigCurrency = hfOrigCurrency.Value


                    'If IsAccounts() Then
                    '    If ddlAdvPeriod.SelectedIndex > 0 Then
                    '        q.First.Period = ddlAdvPeriod.SelectedValue
                    '    End If
                    '    If ddlAdvYear.SelectedIndex > 0 Then
                    '        q.First.Year = ddlAdvYear.SelectedValue
                    '    End If
                    'End If

                    d.SubmitChanges()
                End If
            End If
            Await LoadAdv(-hfRmbNo.Value)
        End Sub

        Protected Sub SendMessage(ByVal Message As String, Optional ByVal AppendLines As String = "", Optional ByVal Startup As Boolean = True)

            Dim t As Type = Me.GetType()
            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            sb.Append("<script language='javascript'>")
            If Message <> "" Then
                sb.Append("alert(""" & Message & """);")
            End If
            If AppendLines <> "" Then
                sb.Append(AppendLines)
            End If
            sb.Append("</script>")
            If Startup Then
                ScriptManager.RegisterStartupScript(Page, t, "popup", sb.ToString, False)
            Else
                ScriptManager.RegisterClientScriptBlock(Page, t, "popup", sb.ToString, False)
            End If

        End Sub

        Protected Async Sub btnAdvProcess_Click(sender As Object, e As System.EventArgs) Handles btnAdvProcess.Click
            btnAdvSave_Click(Me, Nothing)
            Dim q = From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = -CInt(hfRmbNo.Value) And c.PortalId = PortalId
            If q.Count > 0 Then

                q.First.RequestStatus = RmbStatus.PendingDownload
                q.First.ProcessedDate = Today
                d.SubmitChanges()

                Await LoadAdv(-hfRmbNo.Value)
                Log(q.First.AdvanceId, "Advance Processed - this advance will be added to the next download batch")
                SendMessage(Translate("NextBatchAdvance"))

            End If
        End Sub

        Protected Async Sub btnAdvUnProcess_Click(sender As Object, e As System.EventArgs) Handles btnAdvUnProcess.Click
            Dim q = From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = -CInt(hfRmbNo.Value) And c.PortalId = PortalId
            If q.Count > 0 Then
                q.First.RequestStatus = RmbStatus.Approved
                q.First.Period = Nothing
                q.First.Year = Nothing
                q.First.ProcessedDate = Nothing

                d.SubmitChanges()

                Await LoadAdv(-hfRmbNo.Value)

                Log(q.First.AdvanceId, "Advance UnProcessed")

                SendMessage("", "selectIndex(2);")

            End If
        End Sub

        Protected Sub btnAdvDownload_Click(sender As Object, e As System.EventArgs) Handles btnAdvDownload.Click
            btnAdvSave_Click(Me, Nothing)
            Dim AdvanceNo As Integer = -CInt(hfRmbNo.Value)
            Dim LocalAdvId = (From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = AdvanceNo And c.PortalId = PortalId Select c.LocalAdvanceId).First
            Dim export As String = "Account,Subaccount,Ref,Date,Debit,Credit,Description" & vbNewLine
            export &= DownloadAdvSingle(AdvanceNo)
            Dim attachment As String = "attachment; filename=Adv" & ZeroFill(LocalAdvId, 5) & ".csv"

            HttpContext.Current.Response.Clear()
            HttpContext.Current.Response.ClearHeaders()
            HttpContext.Current.Response.ClearContent()
            HttpContext.Current.Response.AddHeader("content-disposition", attachment)
            HttpContext.Current.Response.ContentType = "text/csv"
            HttpContext.Current.Response.AddHeader("Pragma", "public")
            HttpContext.Current.Response.Write(export)
            HttpContext.Current.Response.End()
        End Sub
#End Region

        Public Function GetProfileImage(ByVal UserId As Integer) As String
            Dim username = UserController.GetUserById(PortalId, UserId).Username
            username = Left(username, Len(username) - 1)
            Return "https://staff.powertochange.org/custom-pages/webService.php?type=staff_photo&api_token=V7qVU7n59743KNVgPdDMr3T8&staff_username=" + username
        End Function

        Protected Async Sub cbMoreInfo_CheckedChanged(sender As Object, e As System.EventArgs) Handles cbMoreInfo.CheckedChanged
            cbApprMoreInfo.Checked = cbMoreInfo.Checked
            Dim theRmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value) And c.PortalId = PortalId
            If theRmb.Count > 0 Then
                If cbMoreInfo.Checked Then
                    Dim theUser = UserController.GetUserById(PortalId, theRmb.First.UserId)
                    SendMessage(Translate("MoreInfoMsg"), "window.open('mailto:" & theUser.Email & "?subject=Reimbursment " & theRmb.First.RID & ": More info requested');")
                End If
                Await btnSave_Click(Me, Nothing)
            End If
        End Sub

        Protected Async Sub cbApprMoreInfo_CheckedChanged(sender As Object, e As System.EventArgs) Handles cbApprMoreInfo.CheckedChanged
            cbMoreInfo.Checked = cbApprMoreInfo.Checked
            Dim theRmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value) And c.PortalId = PortalId
            If theRmb.Count > 0 Then
                If cbApprMoreInfo.Checked Then
                    Dim theUser = UserController.GetUserById(PortalId, theRmb.First.UserId)
                    SendMessage(Translate("MoreInfoMsg"), "window.open('mailto:" & theUser.Email & "?subject=Reimbursment " & theRmb.First.RID & ": More info requested');")
                End If
                Await btnSave_Click(Me, Nothing)
            End If

        End Sub

        Public Function GetLocalStaffProfileName(ByVal StaffProfileName As String) As String
            Dim s = Localization.GetString("ProfileProperties_" & StaffProfileName & ".Text", "/DesktopModules/Admin/Security/App_LocalResources/Profile.ascx.resx", System.Threading.Thread.CurrentThread.CurrentCulture.Name)
            If String.IsNullOrEmpty(s) Then
                Return StaffProfileName
            Else
                Return s
            End If
        End Function

        Private Sub GetRSADownload(ByRef myCommand As OleDbCommand)

            'Dim sql2 = "Update [Deductions$A2:J2000] Set F1='', F2='', F3='', F4='', F5='', F6='', F7='', F8='', F9='' ;" ', F10='', F11='', F12='', F13='', F14='', F15='' ;" ',F16='', F17='', F18='', F19='', F20='', F21='', F22='', F23='', F24='', F25='', F26='' ;"
            'myCommand.CommandText = sql2
            'myCommand.ExecuteNonQuery()

            'sql2 = "Update [Earnings$A4:H2000] Set F1='', F2='', F3='', F4='', F5='', F6='', F7='';"
            'myCommand.CommandText = sql2
            'myCommand.ExecuteNonQuery()

            'Maybe this would be better to do my working out the current period
            Log(0, "Step A")
            Dim currentRmbs = From c In d.AP_Staff_RmbLines Where c.AP_Staff_Rmb.PortalId = PortalId And c.AP_Staff_Rmb.Status = RmbStatus.Processed 'And c.AP_Staff_Rmb.ProcDate > Today.AddDays(-15) And c.Department = False

            Dim Deductions = DotNetNuke.Entities.Profile.ProfileController.GetPropertyDefinitionsByCategory(PortalId, "Payroll-Deductions")
            Dim Earnings = DotNetNuke.Entities.Profile.ProfileController.GetPropertyDefinitionsByCategory(PortalId, "Payroll-Earnings")


            Dim sql = "Update [Deductions$A2:Z2] Set F1='', F2='' "
            Dim j As Integer = 3
            For Each item As DotNetNuke.Entities.Profile.ProfilePropertyDefinition In Deductions
                sql &= ",F" & j & "='" & GetLocalStaffProfileName(item.PropertyName) & "' "
                j += 1
            Next

            myCommand.CommandText = sql
            myCommand.ExecuteNonQuery()

            Dim StaffTypes = {"National Staff", "National Staff, Overseas", "Centrally Funded"}
            Dim allStaff = StaffBrokerFunctions.GetStaff(-1)

            Log(0, "Step B")
            '.OrderBy(Function(x) x.LastName).ThenBy(Function(x) x.AP_StaffBroker_Staffs.StaffId)
            Dim i As Integer = 3
            For Each row In allStaff
                'Load Values
                Dim theUser = UserController.GetUserById(PortalId, row.UserID)
                Dim theStaff = StaffBrokerFunctions.GetStaffMember(theUser.UserID)
                If Not (theStaff Is Nothing Or theUser Is Nothing) Then


                    '  Dim CurrentPeriod = StaffBrokerFunctions.GetSetting("CurrentFiscalPeriod", PortalId)

                    Dim EmpCode As String = theUser.Profile.GetPropertyValue("EmployeeCode")
                    If EmpCode Is Nothing Or EmpCode = "0" Then
                        EmpCode = ""
                    End If


                    Dim CostCenter = theStaff.CostCenter

                    Dim salary As Double = 0
                    For Each item As DotNetNuke.Entities.Profile.ProfilePropertyDefinition In Earnings
                        If item.Deleted = False Then
                            salary += theUser.Profile.GetPropertyValue(item.PropertyName)
                        End If

                    Next

                    'Dim VehicleInsurance As Double = theUser.Profile.GetPropertyValue("VehicleInsurance")
                    'Dim RetirementPolicies As Double = theUser.Profile.GetPropertyValue("RetirementPolicies")
                    'Dim DependantParent As Double = theUser.Profile.GetPropertyValue("DependantParent")
                    'Dim HousingAllowance As Double = theUser.Profile.GetPropertyValue("HousingAllowance")

                    Dim NormalSalary As Double = theUser.Profile.GetPropertyValue("NormalSalary")

                    salary += NormalSalary





                    Dim AccountBalance As Double = 0
                    Dim AdvanceBalance As Double = 0
                    Try


                        Dim sugPay = From c In ds.AP_Staff_SuggestedPayments Where c.PortalId = PortalId And c.CostCenter = CostCenter


                        If sugPay.Count > 0 Then
                            If Not sugPay.First.AccountBalance Is Nothing Then
                                AccountBalance = sugPay.First.AccountBalance
                            End If

                            If Not sugPay.First.AdvanceBalance Is Nothing Then
                                AdvanceBalance = sugPay.First.AdvanceBalance
                            End If
                        End If
                    Catch ex As Exception

                    End Try
                    'lookup Expenses for current period, user
                    Dim myRmbs = From c In currentRmbs Where c.AP_Staff_Rmb.UserId = row.UserID

                    Dim Travel As Double = 0
                    Dim AllowancesNontax As Double = 0
                    Dim AllowancesTax As Double = 0
                    '######################## NEED TO SET THE TRAVEL TYPE ########################
                    Dim TravelExpenseTypes = {58}
                    '######################## NEED TO SET THE TRAVEL TYPE ########################
                    Try
                        Travel = myRmbs.Where(Function(x) TravelExpenseTypes.Contains(x.LineType)).Sum(Function(y) CType(y.GrossAmount, Decimal?)) ' This needs better definition

                    Catch ex As Exception

                    End Try


                    Try
                        AllowancesNontax = myRmbs.Where(Function(x) x.Taxable = False And Not TravelExpenseTypes.Contains(x.LineType)).Sum(Function(y) CType(y.GrossAmount, Decimal?))

                    Catch ex As Exception

                    End Try
                    Try
                        AllowancesTax = myRmbs.Where(Function(x) x.Taxable = True And Not TravelExpenseTypes.Contains(x.LineType)).Sum(Function(y) CType(y.GrossAmount, Decimal?))

                    Catch ex As Exception

                    End Try

                    'Check Balances
                    If AccountBalance - (salary + Travel + AllowancesNontax + AllowancesTax) < 0 Then
                        'We need to reduce Salary
                        salary = Math.Max(AccountBalance - (Travel + AllowancesNontax + AllowancesTax), 0)
                    End If


                    'Dim SanLamGroupLife As Double = theUser.Profile.GetPropertyValue("SanlamGroupLife")
                    'Dim LibertyLifeRAF As Double = theUser.Profile.GetPropertyValue("LibertyLifeRAF")
                    'Dim OldMutualRAF As Double = theUser.Profile.GetPropertyValue("OldMutualRAF")
                    'Dim SalaryAdvance As Double = theUser.Profile.GetPropertyValue("SalaryAdvance")
                    'Dim SavingsScheme As Double = theUser.Profile.GetPropertyValue("SavingsScheme")



                    'Complete the Deductions columns
                    sql = "Update [Deductions$A" & i & ":Z" & i & "] Set F1=@EmpCode, F2='CCCSA-FIELD STAFF'"
                    myCommand.Parameters.AddWithValue("@EmpCode", EmpCode.Trim(" "))
                    j = 3
                    For Each item As DotNetNuke.Entities.Profile.ProfilePropertyDefinition In Deductions
                        If item.Deleted = False Then
                            Dim Value = theUser.Profile.GetPropertyValue(item.PropertyName)
                            If Value <> 0 Then
                                sql &= ",F" & j & "=" & Value

                            End If
                            j += 1
                        End If
                    Next


                    sql &= ",F" & j & "=" & 999
                    j += 1

                    sql &= ",F" & j & "=" & 1

                    sql &= " ;"

                    myCommand.CommandText = sql


                    myCommand.ExecuteNonQuery()
                    myCommand.Parameters.Clear()

                    'Complete the Earnings Columns
                    sql = "Update [Earnings$A" & i & ":H" & i & "] Set F1=@EmpCode, F2='CCCSA-FIELD STAFF'"
                    myCommand.Parameters.AddWithValue("@EmpCode", EmpCode.Trim(" "))
                    If salary <> 0 Then
                        sql &= ",F3=@Salary"
                        myCommand.Parameters.AddWithValue("@Salary", salary)
                    End If
                    If Travel <> 0 Then
                        sql &= ",F4=@Travel"
                        myCommand.Parameters.AddWithValue("@Travel", Travel)
                    End If
                    If AllowancesTax <> 0 Then
                        sql &= ",F5=@AllowancesTax"
                        myCommand.Parameters.AddWithValue("@AllowancesTax", AllowancesTax)
                    End If
                    If AllowancesNontax <> 0 Then
                        sql &= ",F6=@AllowancesNonTax"
                        myCommand.Parameters.AddWithValue("@AllowancesNonTax", AllowancesNontax)
                    End If
                    If CostCenter <> "" Then
                        sql &= ",F7=@RC"
                        myCommand.Parameters.AddWithValue("@RC", CostCenter.Trim(" "))
                    End If
                    If CostCenter <> "" Then
                        sql &= ",F8=@AccountBalance"
                        myCommand.Parameters.AddWithValue("@AccountBalance", AccountBalance)
                    End If
                    sql &= " ;"

                    myCommand.CommandText = sql








                    myCommand.ExecuteNonQuery()
                    myCommand.Parameters.Clear()




                    i += 1
                End If
            Next







        End Sub

        Protected Sub btnSuggestedPayments_Click(sender As Object, e As System.EventArgs) Handles btnSuggestedPayments.Click


            Dim filename As String = "SuggestedPayments.xls"
            If StaffBrokerFunctions.GetSetting("NetSalaries", PortalId) = "True" Then
                filename = "SuggestedPayments-NETSalary.xls"
            End If
            If StaffBrokerFunctions.GetSetting("ZA-Mode", PortalId) = "True" Then
                filename = "SuggestedPayments-ZA.xls"
                'filename = filename
            End If

            File.Copy(Server.MapPath("/DesktopModules/AgapeConnect/StaffRmb/" & filename), PortalSettings.HomeDirectoryMapPath & filename, True)



            Dim connStr As String = "provider=Microsoft.Jet.OLEDB.4.0;Data Source='" & PortalSettings.HomeDirectoryMapPath & filename & "';Extended Properties='Excel 8.0;HDR=NO'"
            Dim MyConnection As OleDbConnection
            MyConnection = New OleDbConnection(connStr)

            MyConnection.Open()

            'Dim sql = ""
            Dim MyCommand As New OleDbCommand()
            MyCommand.Connection = MyConnection

            'Clear the form
            Try


                Dim sql2 = "Update [Suggested Payments$A4:J1000] Set F1='', F2='', F3='', F4='', F5='',F6='', F7='', F8='', F10='' ;"
                'MyCommand.CommandText = sql2
                'MyCommand.ExecuteNonQuery()




                Dim q = From c In ds.AP_Staff_SuggestedPayments Where c.PortalId = PortalId

                Dim i As Integer = 4


                For Each row In q
                    If d.AP_StaffBroker_CostCenters.Where(Function(x) x.PortalId = PortalId And x.CostCentreCode.Trim() = row.CostCenter.Trim() And x.Type = 1).Count > 0 Then


                        Dim salary1 = ""
                        Dim salary2 = ""
                        Dim expense = ""
                        Dim taxexpenses = ""
                        If cbSalaries.Checked And Not StaffBrokerFunctions.GetSetting("ZA-Mode", PortalId) = "True" Then
                            Dim staffMember = From c In ds.AP_StaffBroker_Staffs Where c.PortalId = PortalId And c.CostCenter = row.CostCenter.TrimEnd(" ")
                            If staffMember.Count > 0 Then
                                Dim s1 = From c In staffMember.First.AP_StaffBroker_StaffProfiles Where c.AP_StaffBroker_StaffPropertyDefinition.FixedFieldName = "Salary1" Select c.PropertyValue
                                If s1.Count > 0 Then
                                    salary1 = s1.First
                                End If
                                If staffMember.First.UserId2 > 0 Then
                                    Dim s2 = From c In staffMember.First.AP_StaffBroker_StaffProfiles Where c.AP_StaffBroker_StaffPropertyDefinition.FixedFieldName = "Salary2" Select c.PropertyValue
                                    If s2.Count > 0 Then
                                        salary2 = s2.First
                                    End If
                                End If


                            End If
                        End If

                        If cbExpenses.Checked Then

                            expense = row.ExpPayable
                            taxexpenses = row.ExpTaxable


                        End If

                        If Not (salary1 = "" And salary2 = "" And (expense = "" Or expense = "0") And (taxexpenses = "" Or taxexpenses = "0")) Then


                            Dim Name As String = (From c In d.AP_StaffBroker_CostCenters Where c.PortalId = PortalId And c.CostCentreCode = row.CostCenter.TrimEnd(" ")).First.CostCentreName
                            'Dim test = From c In d.AP_StaffBroker_CostCenters





                            Dim sql = "Update [Suggested Payments$A" & i & ":F" & i & "] Set F1=@RC , F2=@Name"
                            MyCommand.Parameters.AddWithValue("@RC", row.CostCenter.TrimEnd(" "))
                            MyCommand.Parameters.AddWithValue("@Name", Name)

                            If expense <> "" Then
                                sql &= ",F3=@Expense"
                                MyCommand.Parameters.AddWithValue("@Expense", expense)
                            End If
                            If taxexpenses <> "" Then
                                sql &= ",F4=@TaxExpense"
                                MyCommand.Parameters.AddWithValue("@TaxExpense", taxexpenses)
                            End If
                            If salary1 <> "" Then
                                sql &= ",F5=@Salary1"
                                MyCommand.Parameters.AddWithValue("@Salary1", salary1)
                            End If
                            If salary2 <> "" Then
                                sql &= ",F6=@Salary2"
                                MyCommand.Parameters.AddWithValue("@Salary2", salary2)
                            End If
                            sql &= " ;"

                            MyCommand.CommandText = sql






                            MyCommand.ExecuteNonQuery()
                            MyCommand.Parameters.Clear()
                            i += 1
                        End If
                    End If
                Next

                If cbSalaries.Checked Then
                    'Now insert the Salaries for staff who don't have a suggested payments entry
                    Dim otherStaff = From c In ds.AP_StaffBroker_Staffs Where c.PortalId = PortalId And c.Active = True

                    For Each member In otherStaff
                        If Not String.IsNullOrEmpty(member.CostCenter) Then
                            Dim search = From c In ds.AP_Staff_SuggestedPayments Where c.PortalId = PortalId And c.CostCenter.StartsWith(member.CostCenter)
                            If search.Count = 0 Then
                                'need to insert this one!
                                Dim salary1 = ""
                                Dim salary2 = ""
                                Dim s1 = From c In member.AP_StaffBroker_StaffProfiles Where c.AP_StaffBroker_StaffPropertyDefinition.FixedFieldName = "Salary1" Select c.PropertyValue
                                If s1.Count > 0 Then
                                    salary1 = s1.First
                                End If
                                If member.UserId2 > 0 Then
                                    Dim s2 = From c In member.AP_StaffBroker_StaffProfiles Where c.AP_StaffBroker_StaffPropertyDefinition.FixedFieldName = "Salary2" Select c.PropertyValue
                                    If s2.Count > 0 Then
                                        salary2 = s2.First
                                    End If
                                End If
                                If Not (salary1 = "" And salary2 = "") Then


                                    Dim Name As String = (From c In d.AP_StaffBroker_CostCenters Where c.PortalId = PortalId And c.CostCentreCode = member.CostCenter).First.CostCentreName

                                    Dim sql = "Update [Suggested Payments$A" & i & ":F" & i & "] Set F1=@RC, F2=@Name"
                                    MyCommand.Parameters.AddWithValue("@RC", member.CostCenter.TrimEnd(" "))
                                    MyCommand.Parameters.AddWithValue("@Name", Name)
                                    If salary1 <> "" Then
                                        sql &= ",F5=@Salary1"
                                        MyCommand.Parameters.AddWithValue("@Salary1", salary1)
                                    End If
                                    If salary2 <> "" Then
                                        sql &= ",F6=@Salary2"
                                        MyCommand.Parameters.AddWithValue("@Salary2", salary2)
                                    End If
                                    sql &= " ;"

                                    MyCommand.CommandText = sql





                                    MyCommand.ExecuteNonQuery()
                                    MyCommand.Parameters.Clear()
                                    i += 1
                                End If

                            End If

                        End If
                    Next


                End If


                'Now refresh the settings
                Dim period = StaffBrokerFunctions.GetSetting("CurrentFiscalPeriod", PortalId).Insert(4, "-")
                sql2 = "Update [Suggested Payments$P2:P2] Set F1='" & period & "' ;"
                MyCommand.CommandText = sql2
                MyCommand.ExecuteNonQuery()

                sql2 = "Update [Suggested Payments$P3:P3] Set F1='" & Settings("ControlAccount") & "' ;"
                MyCommand.CommandText = sql2
                MyCommand.ExecuteNonQuery()

                sql2 = "Update [Suggested Payments$P4:P4] Set F1='" & Settings("AccountsPayable") & "' ;"
                MyCommand.CommandText = sql2
                MyCommand.ExecuteNonQuery()

                sql2 = "Update [Suggested Payments$P5:P5] Set F1='" & Settings("PayrollPayable") & "' ;"
                MyCommand.CommandText = sql2
                MyCommand.ExecuteNonQuery()

                sql2 = "Update [Suggested Payments$P6:P6] Set F1='" & Settings("TaxAccountsReceivable") & "' ;"
                MyCommand.CommandText = sql2
                MyCommand.ExecuteNonQuery()

                sql2 = "Update [Suggested Payments$P7:P7] Set F1='" & Settings("SalaryAccount") & "' ;"
                MyCommand.CommandText = sql2
                MyCommand.ExecuteNonQuery()

                sql2 = "Update [Suggested Payments$P8:P8] Set F1='" & ddlBankAccount.SelectedValue & "' ;"
                MyCommand.CommandText = sql2
                MyCommand.ExecuteNonQuery()

                sql2 = "Update [Suggested Payments$P9:P9] Set F1='" & StaffBrokerFunctions.GetSetting("CompanyName", PortalId) & "' ;"
                MyCommand.CommandText = sql2
                MyCommand.ExecuteNonQuery()

                Dim DownloadFormat As Integer = 1
                Select Case Settings("DownloadFormat")
                    Case "DDC"
                        DownloadFormat = 1
                    Case "DCD"
                        DownloadFormat = 2
                    Case "CDDC"
                        DownloadFormat = 3
                    Case "CDCD"
                        DownloadFormat = 4

                End Select


                sql2 = "Update [Suggested Payments$P10:P10] Set F1=" & DownloadFormat & " ;"
                MyCommand.CommandText = sql2
                MyCommand.ExecuteNonQuery()


                If StaffBrokerFunctions.GetSetting("ZA-Mode", PortalId) = "True" Then
                    GetRSADownload(MyCommand)
                End If




                MyConnection.Close()
                Dim attachment As String = "attachment; filename=SuggestedPayments " & period & ".xls"

                HttpContext.Current.Response.Clear()
                HttpContext.Current.Response.ClearHeaders()
                HttpContext.Current.Response.ClearContent()
                HttpContext.Current.Response.AddHeader("content-disposition", attachment)
                HttpContext.Current.Response.ContentType = "application/vnd.ms-excel"
                HttpContext.Current.Response.AddHeader("Pragma", "public")
                HttpContext.Current.Response.WriteFile(PortalSettings.HomeDirectoryMapPath & filename)
                HttpContext.Current.Response.End()


            Catch ex As Exception
                lblError.Text = ex.Message
                lblError.Visible = True
                MyConnection.Close()
                ' File.Delete(PortalSettings.HomeDirectoryMapPath & filename)
            Finally


            End Try




        End Sub

#Region "Optional Interfaces"
        Private Sub AddClientAction(ByVal Title As String, ByVal theScript As String, ByRef root As DotNetNuke.Entities.Modules.Actions.ModuleActionCollection)
            Dim jsAction As New DotNetNuke.Entities.Modules.Actions.ModuleAction(ModuleContext.GetNextActionID)
            With jsAction
                .Title = Title
                .CommandName = DotNetNuke.Entities.Modules.Actions.ModuleActionType.AddContent
                .ClientScript = theScript
                .Secure = Security.SecurityAccessLevel.Edit
            End With
            root.Add(jsAction)
        End Sub

        Public ReadOnly Property ModuleActions() As Entities.Modules.Actions.ModuleActionCollection Implements Entities.Modules.IActionable.ModuleActions
            Get

                Dim Actions As New Entities.Modules.Actions.ModuleActionCollection

                Actions.Add(GetNextActionID, "Expense Settings", "RmbSettings", "", "action_settings.gif", EditUrl("RmbSettings"), False, SecurityAccessLevel.Edit, True, False)

                AddClientAction("Download Batched Transactions", "showDownload()", Actions)
                AddClientAction("Suggested Payments", "showSuggestedPayments()", Actions)

                For Each a As DotNetNuke.Entities.Modules.Actions.ModuleAction In Actions
                    If a.Title = "Download Batched Transactions" Or a.Title = "Suggested Payments" Then
                        a.Icon = "FileManager/Icons/xls.gif"
                    End If
                Next
                Return Actions
            End Get
        End Property

#End Region



        Private Sub addItemsToTree(AllStaffSubmittedNode As TreeNode, submittedNode As TreeNode, letter As String, queryResult As Object, type As String)
            submittedNode.Expanded = False
            submittedNode.SelectAction = TreeNodeSelectAction.Expand
            getAlphabeticNode(AllStaffSubmittedNode, letter).ChildNodes.Add(submittedNode)

            For Each row In queryResult '--get details of each reimbursement or advance
                Dim newNode As New TreeNode()
                If (type.Equals("rmb")) Then
                    Dim rmbUser = UserController.GetUserById(PortalId, row.UserId).DisplayName
                    newNode.Text = "<span onClick='show_loading_spinner()'>" & GetRmbTitleTeamShort(row.RID, row.RmbDate, rmbUser) & "</span>"
                    newNode.NavigateUrl = NavigateURL() & "?RmbNo=" & row.RMBNo
                Else
                    Dim advUser = UserController.GetUserById(PortalId, row.UserId).DisplayName
                    newNode.Text = "<span onClick='show_loading_spinner()'>" & GetAdvTitleTeamShort(row.LocalAdvanceId, row.RequestDate, advUser) & "</span>"
                    newNode.NavigateUrl = NavigateURL() & "?RmbNo=" & -row.AdvanceId
                End If
                submittedNode.ChildNodes.Add(newNode)

                If IsSelected(row.RMBNo) Then
                    submittedNode.Expanded = True
                    submittedNode.Parent.Expanded = True
                    submittedNode.Parent.Parent.Expanded = True
                End If
            Next
        End Sub

        Private Function getAlphabeticNode(mainTree As TreeNode, letter As String) As TreeNode
            For Each child As TreeNode In mainTree.ChildNodes
                If child.Text.Equals(letter) Then
                    Return child
                End If
            Next
            Dim newChild As New TreeNode(letter)
            newChild.Expanded = False
            newChild.SelectAction = TreeNodeSelectAction.Expand
            For index As Integer = 0 To mainTree.ChildNodes.Count() - 1
                If mainTree.ChildNodes.Item(index).Text > letter Then
                    mainTree.ChildNodes.AddAt(index, newChild)
                    Return newChild
                End If
            Next
            mainTree.ChildNodes.Add(newChild)
            Return newChild
        End Function

        Private Async Function refreshAccountBalanceAsync(account As String, logon As String) As Task
            Dim accountBalance = Await StaffRmbFunctions.getAccountBalanceAsync(account, logon)
            Try
                hfAccountBalance.Value = Double.Parse(accountBalance)
            Catch
                hfAccountBalance.Value = 0
            End Try
            lblAccountBalance.Text = accountBalance
        End Function

    End Class
End Namespace
