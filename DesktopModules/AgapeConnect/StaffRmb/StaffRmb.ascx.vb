Imports System
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
        Dim BALANCE_INCONCLUSIVE As String = "unknown"
        Dim BALANCE_PERMISSION_DENIED As String = "**hidden**"

#Region "Properties"
        Dim d As New StaffRmbDataContext
        Dim ds As New StaffBrokerDataContext
        Dim theControl As Object
        Dim objEventLog As New DotNetNuke.Services.Log.EventLog.EventLogController
        'Dim SpouseList As IQueryable(Of StaffBroker.User)  '= AgapeStaffFunctions.SpouseIsLeader()
        Dim VAT3ist As String() = {"111X", "112X", "113", "116", "514X"}
#End Region

#Region "Page Events"
        Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
            hfPortalId.Value = PortalId
            hfTabModuleId.Value = TabModuleId
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

        Private Async Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Me.Init

            lblAdvanceRequest.Visible = ENABLE_ADVANCE_FUNCTIONALITY
            lblError.Visible = False

            Dim TaskList As New List(Of Task)

            If Not String.IsNullOrEmpty(Settings("NoReceipt")) Then
                hfNoReceiptLimit.Value = Settings("NoReceipt")
            End If

            If Page.IsPostBack Then

            Else
                TaskList.Add(LoadCompaniesAsync())
                TaskList.Add(LoadMenuAsync())
                TaskList.Add(LoadAddressAsync())
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
                    lblError.Text = "Access Denied. You have not been setup as a staff member on this website. Please ask your accounts team or administrator to set up your staff profile."

                    lblError.Visible = True
                    pnlEverything.Visible = False
                    Return
                    '-- Disabled because we do not use PAC
                    'ElseIf staff.CostCenter = Nothing And PAC = "" Then
                    '    'cannot use
                    '    lblError.Text = "Access Denied. Your account has not been setup with a valid Responsibility Center. Please ask your accounts team or administrator to setup your staff profile"
                    '    lblError.Visible = True
                    '    pnlEverything.Visible = False
                    '    Return


                Else
                    CC = staff.CostCenter
                    PayOnly = StaffBrokerFunctions.GetStaffProfileProperty(staff.StaffId, "PayOnly")
                    'PAC = StaffBrokerFunctions.GetStaffProfileProperty(staff.StaffId, "PersonalAccountCode")
                    '-- Disabled because we do not use PAC
                    'If CC = "" And PAC = "" Then
                    '    'cannot use
                    '    lblError.Text = "Access Denied. Your account has not been setup with a valid Responsibility Center. Please ask your accounts team or administrator to setup your staff profile."
                    '    lblError.Visible = True
                    '    pnlEverything.Visible = False
                    '    Return
                    'End If


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

                pnlVAT.Visible = Settings("VatAttrib")

                If IsAccounts() Then
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
                        TaskList.Add(LoadAdvAsync(-hfRmbNo.Value))
                    Else
                        TaskList.Add(LoadRmbAsync(hfRmbNo.Value))
                    End If
                Else
                    ltSplash.Text = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("RmbSplash", PortalId))
                End If
                tbNewChargeTo.Attributes.Add("onkeypress", "return disableSubmitOnEnter();")
            End If
            Await Task.WhenAll(TaskList)
        End Sub

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
        Public Async Function LoadMenuAsync() As Task
            Try
                showDividers(False)
                lblSubmitted.Visible = False
                Dim basicTask = LoadBasicMenuAsync()
                Dim supervisorTask = LoadSupervisorMenuAsync()
                Dim financeTask = LoadFinanceMenuAsync()

                Await Task.WhenAll(basicTask, supervisorTask, financeTask)

            Catch ex As Exception
                lblError.Text = "Error loading Menu: " & ex.Message
                lblError.Visible = True
            End Try
        End Function

        Private Sub showDividers(onoff As Boolean)
            lblApproved.Visible = onoff
            lblYourProcessing.Visible = onoff
            lblYourPaid.Visible = onoff
        End Sub

        Private Async Function LoadBasicMenuAsync() As Task
            Try
                '*** EVERYONE SEES THIS STUFF ***
                Dim ReloadMenuTasks As New List(Of Task)
                ReloadMenuTasks.Add(loadBasicMoreInfoAsync())
                ReloadMenuTasks.Add(loadBasicDraftPaneAsync())
                ReloadMenuTasks.Add(loadBasicSubmittedPaneAsync())
                ReloadMenuTasks.Add(loadBasicApprovablePaneAsync())
                ReloadMenuTasks.Add(loadBasicApprovedPaneAsync())
                ReloadMenuTasks.Add(loadBasicProcessingPaneAsync())
                ReloadMenuTasks.Add(loadBasicPaidPaneAsync())
                ReloadMenuTasks.Add(loadBasicCancelledTaskAsync())

                Await Task.WhenAll(ReloadMenuTasks)

            Catch ex As Exception
                Throw New Exception("Error loading basic menu: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicMoreInfoAsync() As Task
            '--Highlight reimbursements that need more information in a bar at the top
            Try
                Dim MoreInfo As System.Linq.IQueryable
                MoreInfo = From c In d.AP_Staff_Rmbs
                                    Where c.MoreInfoRequested = True And c.Status <> RmbStatus.Processing And c.UserId = UserId And c.PortalId = PortalId
                                    Select c.UserRef, c.RID, c.RMBNo
                For Each row In MoreInfo
                    Dim hyp As New HyperLink()
                    hyp.CssClass = "ui-state-highlight ui-corner-all AgapeWarning"
                    hyp.Font.Size = FontUnit.Small
                    hyp.Font.Bold = True
                    hyp.Text = Translate("MoreInfo").Replace("[RMBNO]", row.RID).Replace("[USERREF]", row.UserRef)
                    hyp.NavigateUrl = NavigateURL() & "?RmbNo=" & row.RMBNo
                    MoreInfoPlaceholder.Controls.Add(hyp)
                Next
            Catch ex As Exception
                Throw New Exception("Error loading MoreInfo rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicDraftPaneAsync() As Task
            Try
                Dim Pending = (From c In d.AP_Staff_Rmbs
                               Where c.Status = RmbStatus.Draft And c.PortalId = PortalId And (c.UserId = UserId)
                               Order By c.RID Descending
                               Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(Settings("MenuSize"))
                dlPending.DataSource = Pending
                dlPending.DataBind()
                DraftsUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error loading draft rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicSubmittedPaneAsync() As Task
            Try
                Dim Submitted = (From c In d.AP_Staff_Rmbs
                                 Where c.Status = RmbStatus.Submitted And c.UserId = UserId And c.PortalId = PortalId
                                 Order By c.RID Descending
                                 Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(Settings("MenuSize"))
                dlSubmitted.DataSource = Submitted
                dlSubmitted.DataBind()

                Dim AdvSubmitted = (From c In d.AP_Staff_AdvanceRequests
                                    Where c.RequestStatus = RmbStatus.Submitted And c.UserId = UserId And c.PortalId = PortalId
                                    Order By c.LocalAdvanceId Descending
                                    Select c.AdvanceId, c.RequestDate, c.LocalAdvanceId, c.UserId).Take(Settings("MenuSize"))
                dlAdvSubmitted.DataSource = AdvSubmitted
                dlAdvSubmitted.DataBind()
                dlAdvSubmitted.AlternatingItemStyle.CssClass = IIf(dlSubmitted.Items.Count Mod 2 = 1, "dnnGridItem", "dnnGridAltItem")
                dlAdvSubmitted.ItemStyle.CssClass = IIf(dlSubmitted.Items.Count Mod 2 = 1, "dnnGridAltItem", "dnnGridItem")
                Dim submitted_count = Submitted.Count + AdvSubmitted.Count
                SubmittedUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error loading submitted rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicApprovablePaneAsync() As Task
            '--list any unapproved reimbursements submitted to this user for approval
            Try
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

                '-- Add a count of approvable items to the 'Submitted' heading
                If approvable_count > 0 Then
                    lblSubmitted.Visible = True
                    lblSubmittedCount.Text = "(" & approvable_count & ")"
                    pnlSubmitted.CssClass = "ui-state-highlight ui-corner-all"
                Else
                    lblSubmittedCount.Text = ""
                    pnlSubmitted.CssClass = ""
                End If

                lblApproveHeading.Visible = isApprover
                SubmittedUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error loading approvable rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicApprovedPaneAsync() As Task
            Try
                Dim Approved = (From c In d.AP_Staff_Rmbs
                                Where (c.Status = RmbStatus.Approved Or c.Status = RmbStatus.PendingDownload Or c.Status = RmbStatus.DownloadFailed) _
                                    And c.UserId = UserId And c.PortalId = PortalId
                                Order By c.RID Descending
                                Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(Settings("MenuSize"))
                dlApproved.DataSource = Approved
                dlApproved.DataBind()
                Dim AdvApproved = (From c In d.AP_Staff_AdvanceRequests
                                   Where (c.RequestStatus = RmbStatus.Approved Or c.RequestStatus = RmbStatus.PendingDownload Or c.RequestStatus = RmbStatus.DownloadFailed) _
                                        And c.UserId = UserId And c.PortalId = PortalId
                                   Order By c.LocalAdvanceId Descending
                                   Select c.AdvanceId, c.RequestDate, c.LocalAdvanceId, c.UserId).Take(Settings("MenuSize"))
                dlAdvApproved.DataSource = AdvApproved
                dlAdvApproved.DataBind()
                dlAdvApproved.AlternatingItemStyle.CssClass = IIf(dlApproved.Items.Count Mod 2 = 1, "dnnGridItem", "dnnGridAltItem")
                dlAdvApproved.ItemStyle.CssClass = IIf(dlApproved.Items.Count Mod 2 = 1, "dnnGridAltItem", "dnnGridItem")
                ApprovedUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error loading approved rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicProcessingPaneAsync() As Task
            Try
                Dim Complete = (From c In d.AP_Staff_Rmbs
                                Where c.Status = RmbStatus.Processing And c.UserId = UserId And c.PortalId = PortalId
                                Order By c.RID Descending
                                Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(Settings("MenuSize"))
                dlProcessing.DataSource = Complete
                dlProcessing.DataBind()
                Dim CompleteAdv = (From c In d.AP_Staff_AdvanceRequests
                                   Where c.RequestStatus = RmbStatus.Processing And c.UserId = UserId And c.PortalId = PortalId
                                   Order By c.LocalAdvanceId Descending
                                   Select c.AdvanceId, c.RequestDate, c.LocalAdvanceId, c.UserId).Take(Settings("MenuSize"))
                dlAdvProcessing.DataSource = CompleteAdv
                dlAdvProcessing.DataBind()
                ProcessingUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error loading processing rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicPaidPaneAsync() As Task
            Try
                Dim Complete = (From c In d.AP_Staff_Rmbs
                                Where c.Status = RmbStatus.Paid And c.UserId = UserId And c.PortalId = PortalId
                                Order By c.RID Descending
                                Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(Settings("MenuSize"))
                dlPaid.DataSource = Complete
                dlPaid.DataBind()
                Dim PaidAdv = (From c In d.AP_Staff_AdvanceRequests
                                   Where c.RequestStatus = RmbStatus.Paid And c.UserId = UserId And c.PortalId = PortalId
                                   Order By c.LocalAdvanceId Descending
                                   Select c.AdvanceId, c.RequestDate, c.LocalAdvanceId, c.UserId).Take(Settings("MenuSize"))
                dlAdvPaid.DataSource = PaidAdv
                dlAdvPaid.DataBind()
                PaidUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error loading paid rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicCancelledTaskAsync() As Task
            Try
                Dim Cancelled = (From c In d.AP_Staff_Rmbs
                                 Where c.Status = RmbStatus.Cancelled And c.UserId = UserId And c.PortalId = PortalId
                                 Order By c.RID Descending
                                 Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(Settings("MenuSize"))
                dlCancelled.DataSource = Cancelled
                dlCancelled.DataBind()
                CancelledUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error loading cancelled rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function LoadSupervisorMenuAsync() As Task
            Try
                Dim Team As List(Of User) = StaffBrokerFunctions.GetTeam(UserId)
                Dim isSupervisor = (Team.Count > 0)
                If isSupervisor Then
                    lblTeamLeader.Visible = True
                    showDividers(True)
                    Dim TeamIds = From c In Team Select c.UserID
                    Dim ReloadMenuTasks As New List(Of Task)
                    ReloadMenuTasks.Add(buildTeamApprovedTreeAsync(Team))
                    ReloadMenuTasks.Add(buildTeamProcessingTreeAsync(Team))
                    ReloadMenuTasks.Add(buildTeamPaidTreeAsync(Team))
                    Await Task.WhenAll(ReloadMenuTasks)

                Else '--They are not a supervisor
                    tvTeamApproved.Visible = False
                    tvTeamProcessing.Visible = False
                    tvTeamPaid.Visible = False
                End If

            Catch ex As Exception
                Throw New Exception("Error loading supervisor menu: " + ex.Message)
            End Try
        End Function

        Private Async Function buildTeamApprovedTreeAsync(Team As List(Of User)) As Task
            Try
                Dim TeamApprovedNode As New TreeNode("Your Team")
                TeamApprovedNode.Expanded = False
                TeamApprovedNode.SelectAction = TreeNodeSelectAction.Expand

                For Each team_member In Team
                    Dim TeamMemberApprovedNode As New TreeNode(team_member.DisplayName)
                    TeamMemberApprovedNode.SelectAction = TreeNodeSelectAction.Expand
                    TeamMemberApprovedNode.Expanded = False
                    Dim TeamApproved = From c In d.AP_Staff_Rmbs
                                       Where c.UserId = team_member.UserID _
                                            And (c.Status = RmbStatus.Approved Or c.Status = RmbStatus.PendingDownload Or c.Status = RmbStatus.DownloadFailed) _
                                            And c.UserId <> UserId And c.PortalId = PortalId
                                       Select c.RMBNo, c.RmbDate, c.RID, c.SpareField1
                    For Each rmb In TeamApproved
                        Dim rmb_node As New TreeNode()
                        Dim rmbTotal = If(rmb.SpareField1 Is Nothing, "", rmb.SpareField1)
                        If (rmb.RmbDate Is Nothing) Then
                            rmb_node.Text = GetRmbTitleTeamShort(rmb.RID, New Date(1970, 1, 1), rmbTotal)
                        Else
                            rmb_node.Text = GetRmbTitleTeamShort(rmb.RID, rmb.RmbDate, rmbTotal)
                        End If
                        rmb_node.Value = rmb.RMBNo
                        rmb_node.SelectAction = TreeNodeSelectAction.Select
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
                            adv_node.Text = GetAdvTitleTeamShort(adv.LocalAdvanceId, New Date(1970, 1, 1), advUser)
                        Else
                            adv_node.Text = GetAdvTitleTeamShort(adv.LocalAdvanceId, adv.RequestDate, advUser)
                        End If
                        adv_node.Value = -adv.AdvanceId
                        adv_node.SelectAction = TreeNodeSelectAction.Select
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
                tvTeamApproved.Visible = True
                ApprovedUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error building team approved tree: " + ex.Message)
            End Try
        End Function

        Private Async Function buildTeamProcessingTreeAsync(Team As List(Of User)) As Task
            Try
                Dim TeamProcessingNode As New TreeNode("Your Team")
                TeamProcessingNode.SelectAction = TreeNodeSelectAction.Expand
                TeamProcessingNode.Expanded = False

                For Each team_member In Team
                    Dim TeamMemberProcessingNode As New TreeNode(team_member.DisplayName)
                    TeamMemberProcessingNode.Expanded = False
                    TeamMemberProcessingNode.SelectAction = TreeNodeSelectAction.Expand

                    Dim TeamProcessing = From c In d.AP_Staff_Rmbs
                                        Join b In d.AP_StaffBroker_CostCenters
                                            On c.CostCenter Equals b.CostCentreCode _
                                                And c.PortalId Equals b.PortalId
                                        Where c.UserId = team_member.UserID And c.Status = RmbStatus.Processing And b.Type = CostCentreType.Staff And c.PortalId = PortalId
                                        Select c.RMBNo, c.RmbDate, c.RID, c.SpareField1
                    For Each rmb In TeamProcessing
                        Dim rmb_node As New TreeNode()
                        Dim rmbTotal = If(rmb.SpareField1 Is Nothing, "", rmb.SpareField1)
                        If (rmb.RmbDate Is Nothing) Then
                            rmb_node.Text = GetRmbTitleTeamShort(rmb.RID, New Date(1970, 1, 1), rmbTotal)
                        Else
                            rmb_node.Text = GetRmbTitleTeamShort(rmb.RID, rmb.RmbDate, rmbTotal)
                        End If
                        rmb_node.Value = rmb.RMBNo
                        rmb_node.SelectAction = TreeNodeSelectAction.Select
                        TeamMemberProcessingNode.ChildNodes.Add(rmb_node)
                        If IsSelected(rmb.RMBNo) Then
                            TeamMemberProcessingNode.Expanded = True
                            TeamProcessingNode.Expanded = True
                        End If
                    Next

                    Dim TeamAdvProcessing = From c In d.AP_Staff_AdvanceRequests
                                           Where c.RequestStatus = RmbStatus.Processing And c.UserId = team_member.UserID And c.PortalId = PortalId
                                           Select c.AdvanceId, c.RequestDate, c.UserId, c.LocalAdvanceId
                    For Each adv In TeamAdvProcessing
                        Dim adv_node As New TreeNode()
                        Dim advUser = UserController.GetUserById(PortalId, adv.UserId).DisplayName
                        If (adv.RequestDate Is Nothing) Then
                            adv_node.Text = GetAdvTitleTeamShort(adv.LocalAdvanceId, New Date(1970, 1, 1), advUser)
                        Else
                            adv_node.Text = GetAdvTitleTeamShort(adv.LocalAdvanceId, adv.RequestDate, advUser)
                        End If
                        adv_node.Value = -adv.AdvanceId
                        adv_node.SelectAction = TreeNodeSelectAction.Select
                        TeamMemberProcessingNode.ChildNodes.Add(adv_node)
                        If IsSelected(-adv.AdvanceId) Then
                            TeamMemberProcessingNode.Expanded = True
                            TeamProcessingNode.Expanded = True
                        End If
                    Next
                    TeamProcessingNode.ChildNodes.Add(TeamMemberProcessingNode)
                Next
                tvTeamProcessing.Nodes.Clear()
                tvTeamProcessing.Nodes.Add(TeamProcessingNode)
                tvTeamProcessing.Visible = True
                ProcessingUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error building team processing tree: " + ex.Message)
            End Try
        End Function

        Private Async Function buildTeamPaidTreeAsync(Team As List(Of User)) As Task
            Try
                Dim TeamPaidNode As New TreeNode("Your Team")
                TeamPaidNode.SelectAction = TreeNodeSelectAction.Expand
                TeamPaidNode.Expanded = False

                For Each team_member In Team
                    Dim TeamMemberPaidNode As New TreeNode(team_member.DisplayName)
                    TeamMemberPaidNode.Expanded = False
                    TeamMemberPaidNode.SelectAction = TreeNodeSelectAction.Expand

                    Dim TeamPaid = From c In d.AP_Staff_Rmbs
                                        Join b In d.AP_StaffBroker_CostCenters
                                            On c.CostCenter Equals b.CostCentreCode _
                                                And c.PortalId Equals b.PortalId
                                        Where c.UserId = team_member.UserID And c.Status = RmbStatus.Paid And b.Type = CostCentreType.Staff And c.PortalId = PortalId
                                        Select c.RMBNo, c.RmbDate, c.RID, c.SpareField1
                    For Each rmb In TeamPaid
                        Dim rmb_node As New TreeNode()
                        Dim rmbTotal = If(rmb.SpareField1 Is Nothing, "", rmb.SpareField1)
                        If (rmb.RmbDate Is Nothing) Then
                            rmb_node.Text = GetRmbTitleTeamShort(rmb.RID, New Date(1970, 1, 1), rmbTotal)
                        Else
                            rmb_node.Text = GetRmbTitleTeamShort(rmb.RID, rmb.RmbDate, rmbTotal)
                        End If
                        rmb_node.Value = rmb.RMBNo
                        rmb_node.SelectAction = TreeNodeSelectAction.Select
                        TeamMemberPaidNode.ChildNodes.Add(rmb_node)
                        If IsSelected(rmb.RMBNo) Then
                            TeamMemberPaidNode.Expanded = True
                            TeamPaidNode.Expanded = True
                        End If
                    Next

                    Dim TeamAdvPaid = From c In d.AP_Staff_AdvanceRequests
                                           Where c.RequestStatus = RmbStatus.Paid And c.UserId = team_member.UserID And c.PortalId = PortalId
                                           Select c.AdvanceId, c.RequestDate, c.UserId, c.LocalAdvanceId
                    For Each adv In TeamAdvPaid
                        Dim adv_node As New TreeNode()
                        Dim advUser = UserController.GetUserById(PortalId, adv.UserId).DisplayName
                        If (adv.RequestDate Is Nothing) Then
                            adv_node.Text = GetAdvTitleTeamShort(adv.LocalAdvanceId, New Date(1970, 1, 1), advUser)
                        Else
                            adv_node.Text = GetAdvTitleTeamShort(adv.LocalAdvanceId, adv.RequestDate, advUser)
                        End If
                        adv_node.Value = -adv.AdvanceId
                        adv_node.SelectAction = TreeNodeSelectAction.Select
                        TeamMemberPaidNode.ChildNodes.Add(adv_node)
                        If IsSelected(-adv.AdvanceId) Then
                            TeamMemberPaidNode.Expanded = True
                            TeamPaidNode.Expanded = True
                        End If
                    Next
                    TeamPaidNode.ChildNodes.Add(TeamMemberPaidNode)
                Next
                tvTeamPaid.Nodes.Clear()
                tvTeamPaid.Nodes.Add(TeamPaidNode)
                tvTeamPaid.Visible = True
                PaidUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error building team paid tree: " + ex.Message)
            End Try
        End Function

        Private Async Function LoadFinanceMenuAsync() As Task
            Try
                If IsAccounts() Then
                    showDividers(True)
                    Dim allStaff = StaffBrokerFunctions.GetStaff()
                    Dim ReloadMenuTasks = New List(Of Task)
                    submittedPlaceholder.Controls.AddAt(0, GenerateTreeControl("treeSubmitted"))
                    processingPlaceholder.Controls.AddAt(0, GenerateTreeControl("treeProcessing"))
                    paidPlaceholder.Controls.AddAt(0, GenerateTreeControl("treePaid"))
                    ReloadMenuTasks.Add(buildAllApprovedTreeAsync(allStaff)) '--This is the key part for the FINANCE team
                    Await Task.WhenAll(ReloadMenuTasks)
                End If

                lblAccountsTeam.Visible = IsAccounts()
            Catch ex As Exception
                Throw New Exception("Error loading finance menu: " + ex.Message)
            End Try
        End Function

        Private Function GenerateTreeControl(ByVal id As String) As Control
            Dim control = New HtmlGenericControl("div")
            control.Attributes.Add("class", "accounts_team")
            control.Attributes.Add("id", ID)
            Return control
        End Function

        Private Async Function buildAllApprovedTreeAsync(allStaff As IQueryable(Of StaffBroker.User)) As Task
            Try
                Dim finance_node As New TreeNode("Finance")
                finance_node.SelectAction = TreeNodeSelectAction.Expand
                finance_node.Expanded = False

                Dim AllApproved = (From c In d.AP_Staff_Rmbs
                                   Where (c.Status = RmbStatus.Approved Or c.Status >= RmbStatus.PendingDownload) And c.PortalId = PortalId
                                   Order By c.ApprDate Ascending
                                   Select c.RMBNo, c.RmbDate, c.ApprDate, c.UserRef, c.RID, c.UserId, c.Status, c.SpareField1, _
                                       Receipts = ((c.AP_Staff_RmbLines.Where(Function(x) x.Receipt And (x.ReceiptImageId Is Nothing))).Count > 0)).Take(Settings("MenuSize"))
                Dim total = AllApproved.Count

                Dim receiptsTask = buildRmbTreeAsync(Translate("Receipts"), finance_node, From c In AllApproved Where c.Status = RmbStatus.Approved And c.Receipts)
                Dim no_receiptsTask = buildRmbTreeAsync(Translate("NoReceipts"), finance_node, From c In AllApproved Where c.Status = RmbStatus.Approved And Not c.Receipts)
                Dim pendingImportTask = buildRmbTreeAsync(Translate("PendingImport"), finance_node, From c In AllApproved Where c.Status >= RmbStatus.PendingDownload)

                Dim no_receipts_node = Await no_receiptsTask
                Dim pending_import_node = Await pendingImportTask

                If (ENABLE_ADVANCE_FUNCTIONALITY) Then
                    Dim AllApprovedAdv = (From c In d.AP_Staff_AdvanceRequests
                          Where (c.RequestStatus = RmbStatus.Approved Or c.RequestStatus >= RmbStatus.PendingDownload) And c.PortalId = PortalId
                          Order By c.LocalAdvanceId Descending).Take(Settings("MenuSize"))
                    total += AllApprovedAdv.Count
                    Dim approvedAdvanceTask = addAdvsToTreeAsync(finance_node, no_receipts_node, From c In AllApprovedAdv Where c.RequestStatus = RmbStatus.Approved)
                    Dim pendingDownloadAdvanceTask = addAdvsToTreeAsync(finance_node, pending_import_node, From c In AllApprovedAdv Where c.RequestStatus >= RmbStatus.PendingDownload)
                    Await Task.WhenAll(approvedAdvanceTask, pendingDownloadAdvanceTask)
                End If

                finance_node.ChildNodes.Add(Await receiptsTask)
                finance_node.ChildNodes.Add(no_receipts_node)
                finance_node.ChildNodes.Add(pending_import_node)

                tvFinance.Nodes.Clear()
                tvFinance.Nodes.Add(finance_node)
                tvFinance.Visible = IsAccounts()

                '-- Add a count of items to the 'Approved' heading
                If total > 0 Then
                    lblToProcess.Text = "(" & total & ")"
                    pnlToProcess.CssClass = "ui-state-highlight ui-corner-all"
                Else
                    lblToProcess.Text = ""
                    pnlToProcess.CssClass = ""
                End If
                ApprovedUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error building approved tree: " + ex.Message)
            End Try
        End Function

        Private Async Function buildRmbTreeAsync(title As String, finance_node As TreeNode, rmbs As Object) As Task(Of TreeNode)
            Try
                Dim node As New TreeNode(title)
                node.SelectAction = TreeNodeSelectAction.Expand
                node.Expanded = False

                Dim count = 0
                For Each rmb In rmbs
                    Dim rmb_node As New TreeNode()
                    Dim rmbTotal = If(rmb.SpareField1 Is Nothing, "unknown", rmb.SpareField1)
                    rmb_node.Text = GetRmbTitleFinance(rmb.RID, rmb.ApprDate, rmbTotal)
                    rmb_node.SelectAction = TreeNodeSelectAction.Select
                    rmb_node.Value = rmb.RMBNo
                    node.ChildNodes.Add(rmb_node)
                    If IsSelected(rmb.RMBNo) Then
                        node.Expanded = True
                        finance_node.Expanded = True
                    End If
                    count += 1
                Next
                node.Text = node.Text & " (" & count & ")"
                Return node
            Catch ex As Exception
                Throw New Exception("Error building " & title & " tree: " + ex.Message)
            End Try
        End Function

        Private Async Function addAdvsToTreeAsync(finance_node As TreeNode, parent_node As TreeNode, advs As Object) As Task
            Try
                parent_node.Text = Left(parent_node.Text, Len(parent_node.Text) - 1) & " + " & advs.Count & ")"
                For Each adv In advs
                    Dim adv_node As New TreeNode()
                    Dim advUser = UserController.GetUserById(PortalId, adv.UserId).DisplayName
                    If (adv.RequestDate Is Nothing) Then
                        adv_node.Text = GetAdvTitleTeamShort(adv.LocalAdvanceId, New Date(), advUser)
                    Else
                        adv_node.Text = GetAdvTitleTeamShort(adv.LocalAdvanceId, adv.RequestDate, advUser)
                    End If
                    adv_node.SelectAction = TreeNodeSelectAction.Select
                    adv_node.Value = -adv.AdvanceId
                    parent_node.ChildNodes.Add(adv_node)
                    If IsSelected(-adv.AdvanceId) Then
                        parent_node.Expanded = True
                        finance_node.Expanded = True
                    End If
                Next
            Catch ex As Exception
                Throw New Exception("Error adding Advances to " & parent_node.Text & " tree: " + ex.Message)
            End Try

        End Function


        Public Async Function LoadAdvAsync(ByVal AdvanceId As Integer) As Task
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

                        Case RmbStatus.Processing
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
                Await LoadMenuAsync()
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

        Private Async Function ResetPostingDataAsync() As task
            Dim ExistingData = From c In d.AP_Staff_Rmb_Post_Extras Where c.RMBNo = CInt(hfRmbNo.Value)
            If (ExistingData.Count > 0) Then
                ddlCompany.SelectedValue = ExistingData.First.Company
                dtPostingDate.Text = ExistingData.First.PostingDate
                tbBatchId.Text = ExistingData.First.BatchId
                tbPostingReference.Text = ExistingData.First.Reference
                tbInvoiceNumber.Text = ExistingData.First.InvoiceNo
                If (ddlCompany.SelectedIndex > 0) Then
                    'Await LoadVendorsAsync()
                    'tbVendorId.Enabled = True
                    ScriptManager.RegisterClientScriptBlock(ddlCompany, ddlCompany.GetType(), "loadVendors", "loadVendorIds();", True)
                    tbVendorId.Text = ExistingData.First.VendorId
                    If (tbVendorId.Text.Length > 0) Then
                        Await LoadRemitToAsync()
                        ddlRemitTo.Enabled = True
                        ddlRemitTo.SelectedValue = ExistingData.First.RemitToAddress
                        btnSubmitPostingData.Enabled = (ddlRemitTo.SelectedIndex > 0)
                    Else
                        ddlRemitTo.Enabled = False
                        ddlRemitTo.Items.Clear()
                        btnSubmitPostingData.Enabled = False
                    End If
                Else
                    tbVendorId.Enabled = False
                    ddlRemitTo.Enabled = False
                    btnSubmitPostingData.Enabled = False
                End If
            Else
                Dim user = UserController.GetUserById(PortalId, UserId)
                Dim initials = Left(user.FirstName, 1) + Left(user.LastName, 1)
                dtPostingDate.Text = Today.ToString("MM/dd/yyyy")
                tbBatchId.Text = Today.ToString("yyMMdd") & initials
                tbPostingReference.Text = ""
                tbInvoiceNumber.Text = "REIMB" & lblRmbNo.Text
                tbVendorId.Enabled = ddlCompany.SelectedIndex > 0
                If (tbVendorId.Enabled) Then
                    'Await LoadVendorsAsync()
                    ScriptManager.RegisterClientScriptBlock(ddlCompany, ddlCompany.GetType(), "loadVendors", "loadVendorIds();", True)
                End If
                ddlRemitTo.Enabled = False
                ddlRemitTo.Items.Clear()
                btnSubmitPostingData.Enabled = False

            End If
        End Function

        Public Async Function LoadRmbAsync(ByVal RmbNo As Integer) As Task
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

                    '--hidden fields
                    hfChargeToValue.Value = If(Rmb.CostCenter Is Nothing, "", Rmb.CostCenter)
                    hfAccountBalance.Value = ""
                    lblAccountBalance.Text = "not loaded"
                    lblBudgetBalance.Text = "not loaded"

                    Dim getAccountBalanceTask = getAccountBalanceAsync(Rmb.CostCenter, StaffRmbFunctions.logonFromId(PortalId, UserId))
                    Dim getBudgetBalanceTask = getBudgetBalanceAsync(Rmb.CostCenter, StaffRmbFunctions.logonFromId(PortalId, UserId))

                    Dim DRAFT = Rmb.Status = RmbStatus.Draft
                    Dim MORE_INFO = (Rmb.MoreInfoRequested IsNot Nothing AndAlso Rmb.MoreInfoRequested = True)
                    Dim SUBMITTED = Rmb.Status = RmbStatus.Submitted
                    Dim APPROVED = Rmb.Status = RmbStatus.Approved
                    Dim PROCESSING = Rmb.Status = RmbStatus.PendingDownload Or Rmb.Status = RmbStatus.DownloadFailed Or Rmb.Status = RmbStatus.Processing
                    Dim PAID = Rmb.Status = RmbStatus.Paid
                    Dim CANCELLED = Rmb.Status = RmbStatus.Cancelled
                    Dim FORM_HAS_ITEMS = Rmb.AP_Staff_RmbLines.Count > 0

                    Dim user = UserController.GetUserById(PortalId, Rmb.UserId)
                    Dim staff_member = StaffBrokerFunctions.GetStaffMember(Rmb.UserId)
                    Dim PACMode = (String.IsNullOrEmpty(staff_member.CostCenter) And StaffBrokerFunctions.GetStaffProfileProperty(staff_member.StaffId, "PersonalAccountCode") <> "")

                    Dim isOwner = (UserId = Rmb.UserId)
                    Dim isSpouse = (StaffBrokerFunctions.GetSpouseId(UserId) = Rmb.UserId)
                    Dim isApprover = (UserId = Rmb.ApprUserId) And Not (isOwner Or isSpouse)
                    Dim isSupervisor = (Not isOwner) And StaffBrokerFunctions.isLeaderOf(UserId, Rmb.UserId)
                    Dim isFinance = IsAccounts() And Not (isOwner Or isSpouse) And Not DRAFT

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
                    Dim resetPostingDataTask = ResetPostingDataAsync()
                    imgAvatar.ImageUrl = GetProfileImage(Rmb.UserId)
                    staffInitials.Value = user.FirstName.Substring(0, 1) & user.LastName.Substring(0, 1)
                    tbChargeTo.Text = If(Rmb.CostCenter Is Nothing, "", Rmb.CostCenter)
                    tbChargeTo.Enabled = DRAFT Or MORE_INFO Or CANCELLED Or (SUBMITTED And (isOwner Or isSpouse))
                    lblStatus.Text = Translate(RmbStatus.StatusName(Rmb.Status))
                    If (Rmb.MoreInfoRequested) Then
                        lblStatus.Text = lblStatus.Text & " - " & Translate("StatusMoreInfo")
                    End If

                    '*** FORM HEADER ***
                    '--dates
                    lblSubmittedDate.Text = If(Rmb.RmbDate Is Nothing, "", Rmb.RmbDate.Value.ToShortDateString)
                    lblSubBy.Text = user.DisplayName

                    lblApprovedDate.Text = If(Rmb.ApprDate Is Nothing, "", Rmb.ApprDate.Value.ToShortDateString)
                    ttlWaitingApp.Visible = Rmb.ApprDate Is Nothing
                    ttlApprovedBy.Visible = Not Rmb.ApprDate Is Nothing
                    ddlApprovedBy.Visible = DRAFT Or MORE_INFO Or CANCELLED Or ((isApprover Or isOwner Or isSpouse) And SUBMITTED)
                    ddlApprovedBy.Enabled = DRAFT Or MORE_INFO Or CANCELLED Or ((isApprover Or isOwner Or isSpouse) And SUBMITTED)
                    lblApprovedBy.Visible = Not ddlApprovedBy.Visible
                    Dim approverName As String = If(Rmb.ApprUserId Is Nothing Or Rmb.ApprUserId = -1, "", UserController.GetUserById(PortalId, Rmb.ApprUserId).DisplayName)
                    Dim updateApproverListTask As New Task(Sub()
                                                               lblApprovedBy.Text = approverName
                                                           End Sub)
                    If (ddlApprovedBy.Visible) Then
                        updateApproverListTask = updateApproversListAsync(Rmb)
                    Else
                        updateApproverListTask.Start()
                    End If

                    lblProcessedDate.Text = If(Rmb.ProcDate Is Nothing, "", Rmb.ProcDate.Value.ToShortDateString)
                    lblProcessedBy.Text = If(Rmb.ProcUserId Is Nothing, "", UserController.GetUserById(PortalId, Rmb.ProcUserId).DisplayName)

                    '--reference / period / year
                    tbYouRef.Enabled = Rmb.Status = RmbStatus.Draft
                    tbYouRef.Text = If(Rmb.UserRef Is Nothing, "", Rmb.UserRef)

                    pnlPeriodYear.Visible = isFinance And (APPROVED Or PROCESSING Or PAID)
                    ddlPeriod.SelectedIndex = 0
                    If Not Rmb.Period Is Nothing Then
                        ddlPeriod.SelectedValue = Rmb.Period
                    End If

                    '--comments
                    ttlYourComments.Visible = (isOwner Or isSpouse)
                    tbComments.Visible = (isOwner Or isSpouse)
                    tbComments.Enabled = isOwner And Not (Rmb.Locked Or PROCESSING Or PAID)
                    tbComments.Text = Rmb.UserComment
                    ttlUserComments.Visible = Not (isOwner Or isSpouse)
                    lblComments.Visible = Not (isOwner Or isSpouse)
                    lblComments.Text = Rmb.UserComment

                    lblApprComments.Visible = Not isApprover
                    lblApprComments.Text = If(Rmb.ApprComment Is Nothing, "", Rmb.ApprComment)
                    tbApprComments.Visible = isApprover
                    tbApprComments.Enabled = isApprover And Not (PROCESSING Or PAID)
                    tbApprComments.Text = If(Rmb.ApprComment Is Nothing, "", Rmb.ApprComment)
                    cbApprMoreInfo.Visible = (isApprover And SUBMITTED)
                    cbApprMoreInfo.Checked = If(Rmb.MoreInfoRequested, Rmb.MoreInfoRequested, False)

                    lblAccComments.Visible = Not isFinance
                    lblAccComments.Text = If(Rmb.AcctComment Is Nothing, "", Rmb.AcctComment)
                    tbAccComments.Visible = isFinance
                    tbAccComments.Enabled = isFinance And Not (PROCESSING Or PAID)
                    tbAccComments.Text = If(Rmb.AcctComment Is Nothing, "", Rmb.AcctComment)

                    cbMoreInfo.Visible = (isFinance And APPROVED)
                    cbMoreInfo.Checked = If(Rmb.MoreInfoRequested, Rmb.MoreInfoRequested, False)

                    '--buttons
                    btnSave.Text = Translate("btnSaved")
                    btnSave.Style.Add(HtmlTextWriterStyle.Display, "none") '--hide, but still generate the button
                    btnSaveAdv.Visible = Not (PROCESSING Or PAID)
                    btnDelete.Visible = Not (PROCESSING Or PAID Or CANCELLED)


                    '*** REIMBURSEMENT DETAILS ***
                    pnlTaxable.Visible = (From c In Rmb.AP_Staff_RmbLines Where c.Taxable = True).Count > 0

                    '--grid
                    GridView1.DataSource = Rmb.AP_Staff_RmbLines
                    GridView1.DataBind()

                    '--buttons
                    btnSaveLine.Visible = ((isOwner Or isSpouse) And Not (PROCESSING Or PAID Or APPROVED)) Or (APPROVED And isFinance)
                    addLinebtn2.Visible = (isOwner Or isSpouse) And Not (PROCESSING Or PAID Or APPROVED)

                    btnPrint.Visible = FORM_HAS_ITEMS
                    btnPrint.OnClientClick = "window.open('/DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & RmbNo & "&UID=" & Rmb.UserId & "', '_blank'); "
                    btnSubmit.Visible = (isOwner Or isSpouse) And (DRAFT Or MORE_INFO Or CANCELLED) And FORM_HAS_ITEMS
                    btnSubmit.Text = If(DRAFT, Translate("btnSubmit"), Translate("btnResubmit"))
                    btnSubmit.Enabled = btnSubmit.Visible And Rmb.CostCenter IsNot Nothing And Rmb.ApprUserId IsNot Nothing AndAlso (Rmb.CostCenter.Length = 6) And (Rmb.ApprUserId >= 0)
                    btnSubmit.ToolTip = If(btnSubmit.Enabled, "", Translate("btnSubmitHelp"))
                    btnAddressOk.OnClientClick = ""
                    btnTempAddressChange.OnClientClick = ""
                    btnPermAddressChange.OnClientClick = ""
                    btnApprove.Visible = isApprover And SUBMITTED
                    btnApprove.Enabled = btnApprove.Visible
                    btnProcess.Visible = isFinance And APPROVED
                    btnProcess.Enabled = btnProcess.Visible
                    btnUnProcess.Visible = isFinance And (PROCESSING Or PAID)
                    btnUnProcess.Enabled = btnUnProcess.Visible
                    btnDownload.Visible = (isFinance Or isOwner Or isSpouse) And FORM_HAS_ITEMS

                    btnAdvDownload.Visible = isFinance
                    tbCostcenter.Enabled = isFinance
                    ddlAccountCode.Enabled = isFinance
                    pnlAccountsOptions.Visible = isFinance


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
                    Dim aBal = Await getAccountBalanceTask
                    Dim bBal = Await getBudgetBalanceTask
                    updateBalanceLabels(aBal, bBal)
                    If (isApprover) Then
                        checkLowBalance()
                    End If
                    ScriptManager.RegisterClientScriptBlock(Page, Page.GetType(), "calculate_remaining_balance", "calculate_remaining_balance()", True)
                    Await Task.WhenAll(updateApproverListTask, resetPostingDataTask)
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

        Protected Async Sub btnSaveLine_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSaveLine.Click

            Dim ucType As Type = theControl.GetType()

            If btnSaveLine.CommandName = "Save" Then
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

                    Dim age = DateDiff(DateInterval.Day, insert.TransDate, Today)
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

                            '-- Disabled, as we are already notifying of old transactions
                            'Dim t1 As Type = Me.GetType()
                            'Dim sb1 As System.Text.StringBuilder = New System.Text.StringBuilder()
                            'sb1.Append("<script language='javascript'>")
                            'sb1.Append("alert(""" & msg & """);")
                            'sb1.Append("</script>")
                            'ScriptManager.RegisterClientScriptBlock(Page, t1, "popup", sb1.ToString, False)
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
                    ' Get each of the files from the line - file table
                    Dim theFiles = (From lf In d.AP_Staff_RmbLine_Files Where lf.RmbLineNo Is Nothing And lf.RMBNo = insert.RmbNo)
                    Try
                        If (CInt(ucType.GetProperty("ReceiptType").GetValue(theControl, Nothing) = 2) And theFiles.Count > 0) Then

                            ElectronicReceipt = True

                            ' Set the receiptImageId to the first fileid that we get; this way, we know that it's at least got 
                            ' something, even if it doesn't have all of the receipts assocaited with this line
                            insert.ReceiptImageId = theFiles.First.FileId

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
                    If ElectronicReceipt Then
                        For Each file In theFiles
                            ' Get the actual file item
                            Dim thisFile = FileManager.Instance.GetFile(file.FileId)
                            ' Rename it
                            FileManager.Instance.RenameFile(thisFile, "R" & hfRmbNo.Value & "L" & insert.RmbLineNo & "Rec" & file.RecNum & "." & thisFile.Extension)
                            ' Update the line number to match
                            file.RmbLineNo = insert.RmbLineNo.ToString
                        Next
                        ' Submit all the changes to the files we made
                        d.SubmitChanges()
                    End If

                    Dim t As Type = Me.GetType()
                    Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                    sb.Append("<script language='javascript'>")
                    sb.Append("closeNewItemPopup();")
                    sb.Append("</script>")
                    Await LoadRmbAsync(hfRmbNo.Value)
                    ScriptManager.RegisterClientScriptBlock(Page, t, "", sb.ToString, False)

                End If
            ElseIf btnSaveLine.CommandName = "Edit" Then
                If ucType.GetMethod("ValidateForm").Invoke(theControl, New Object() {UserId}) = True Then

                    Dim line = From c In d.AP_Staff_RmbLines Where c.RmbLineNo = CInt(btnSaveLine.CommandArgument)
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


                        ' Get all of the electronic receipts for this rmb line
                        Dim line_files = From lf In d.AP_Staff_RmbLine_Files Where lf.RmbLineNo = line.First.RmbLineNo And lf.RMBNo = line.First.RmbNo
                        ' Get the receipt type property
                        Dim receiptType = ucType.GetProperty("ReceiptType")
                        'look for electronic receipt
                        If (Not receiptType Is Nothing AndAlso (CInt(receiptType.GetValue(theControl, Nothing) = 2) AndAlso line_files.Count > 0)) Then
                            ' Set the ImageReceiptId to the first file
                            line.First.ReceiptImageId = line_files.First.FileId
                        Else
                            ' Unset the receipt
                            line.First.ReceiptImageId = Nothing
                            ' Since we aren't supposed to have any receipts with
                            ' this, we should forcefully remove any receipts that
                            ' are already associated with this line
                            Dim files As New List(Of IFileInfo)
                            ' Iterate through all of the line_files we got
                            For Each line_file As AP_Staff_RmbLine_File In line_files
                                ' Add the file to the list
                                files.Add(FileManager.Instance.GetFile(line_file.FileId))
                            Next
                            ' Delete all the files. This should cascade delete to the line_file table as well
                            FileManager.Instance.DeleteFiles(files)
                        End If

                        line.First.AccountCode = ddlAccountCode.SelectedValue
                        line.First.CostCenter = tbCostcenter.Text
                        line.First.LineType = CInt(ddlLineTypes.SelectedValue)
                        line.First.Comment = ucType.GetProperty("Comment").GetValue(theControl, Nothing)
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

                        btnSaveLine.CommandName = "Save"

                    End If
                    Dim t As Type = Me.GetType()
                    Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                    sb.Append("<script language='javascript'>")
                    sb.Append("closeNewItemPopup();")
                    sb.Append("</script>")
                    Await LoadRmbAsync(hfRmbNo.Value)
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
            insert.CostCenter = hfNewChargeTo.Value
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

            insert.Department = If(CC.Count < 1, False, StaffBrokerFunctions.IsDept(PortalId, CC.First))

            btnApprove.Visible = False
            btnSubmit.Visible = True
            btnSubmit.ToolTip = If(btnSubmit.Enabled, "", Translate("btnSubmitHelp"))

            d.AP_Staff_Rmbs.InsertOnSubmit(insert)
            d.SubmitChanges()
            Dim resetMenuTask = LoadMenuAsync()
            Dim loadRmbTask = LoadRmbAsync(insert.RMBNo)

            Dim t As Type = tbNewChargeTo.GetType()
            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            sb.Append("<script language='javascript'>")
            sb.Append("closeNewRmbPopup();")
            sb.Append("</script>")
            Await resetMenuTask
            Await loadRmbTask
            ScriptManager.RegisterClientScriptBlock(tbNewChargeTo, t, "", sb.ToString, False)

        End Sub

        Protected Sub btnSubmit_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSubmit.Click
            Dim RmbNo = hfRmbNo.Value
            Dim rmbs = From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo
            If (rmbs.Count > 0) Then
                Dim rmb = rmbs.First
                Dim receipts = From b In rmb.AP_Staff_RmbLines Where b.Receipt = True And b.ReceiptImageId Is Nothing
                If (receipts.Count > 0) Then
                    btnAddressOk.OnClientClick = "window.open('/DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & RmbNo & "&UID=" & rmb.UserId & "&mode=1', '_blank'); "
                    btnTempAddressChange.OnClientClick = "window.open('/DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & RmbNo & "&UID=" & rmb.UserId & "&mode=1', '_blank'); "
                    btnPermAddressChange.OnClientClick = "window.open('/DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & RmbNo & "&UID=" & rmb.UserId & "&mode=1', '_blank'); "
                End If
            End If
        End Sub

        Protected Async Sub btnAddressOk_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnAddressOk.Click, btnTempAddressChange.Click, btnPermAddressChange.Click
            If (CType(sender, Button).ID = "btnTempAddressChange") Or (CType(sender, Button).ID <> "btnPermAddressChange") Then
                If addAddressToComments() Then
                    If CType(sender, Button).ID <> "btnTempAddressChange" Then
                        tbComments.Text += "**only for this reimbursement**"
                    Else
                        tbComments.Text += "**PLEASE UPDATE my address**"
                    End If
                End If
            End If
            saveIfNecessary()
            Dim rmbs = From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value
            If rmbs.Count > 0 Then
                Dim rmb = rmbs.First
                Dim NewStatus As Integer = rmb.Status
                Dim rmbTotal = CType((From t In d.AP_Staff_RmbLines Where t.RmbNo = rmb.RMBNo Select t.GrossAmount).Sum(), Decimal?).GetValueOrDefault(0)
                Dim requires_receipts = (btnAddressOk.OnClientClick.Length > 0) ' this will contain code to open printable form, if receipts are required
                Dim message As String
                If requires_receipts Then
                    message = Translate("Printout")
                Else
                    message = Translate("SubmittedPopup")
                End If

                If (rmb.MoreInfoRequested) Then
                    rmb.MoreInfoRequested = False
                    rmb.Locked = True
                    tbComments.Enabled = False
                Else
                    NewStatus = RmbStatus.Submitted
                    rmb.Locked = False
                End If

                rmb.Status = NewStatus
                rmb.SpareField1 = rmbTotal.ToString("C") ' currency formatted string
                lblStatus.Text = RmbStatus.StatusName(NewStatus)
                rmb.RmbDate = Now
                lblSubmittedDate.Text = Now.ToShortDateString
                rmb.Period = Nothing
                rmb.Year = Nothing
                btnSubmit.Visible = False

                SubmitChanges()

                Dim refreshMenuTasks = New List(Of Task)
                refreshMenuTasks.Add(loadBasicDraftPaneAsync())
                refreshMenuTasks.Add(loadBasicSubmittedPaneAsync())
                refreshMenuTasks.Add(loadBasicApprovablePaneAsync())

                'dlPending.DataBind()
                'dlSubmitted.DataBind()

                'Send Email to Staff Member
                If NewStatus = RmbStatus.Submitted Then
                    SendApprovalEmail(rmb)
                End If
                Log(rmb.RMBNo, "SUBMITTED")

                'use an alert to switch back to the main window from the printout window
                ScriptManager.RegisterStartupScript(Page, Me.GetType(), "popup_and_select", "closeAddressDialog(); alert(""" & message & """); selectIndex(1)", True)
                Await Task.WhenAll(refreshMenuTasks)

            End If

        End Sub

        Protected Async Sub btnDelete_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnDelete.Click

            Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value
            If rmb.Count > 0 Then
                rmb.First.Status = RmbStatus.Cancelled
                lblStatus.Text = Translate(RmbStatus.StatusName(RmbStatus.Cancelled))
                btnApprove.Visible = False
                btnDelete.Visible = False

                If rmb.First.UserId = UserId Then
                    Log(rmb.First.RMBNo, "DELETED by owner")
                    ScriptManager.RegisterStartupScript(btnDelete, btnDelete.GetType(), "select5", "selectIndex(5)", True)
                Else
                    'Send an email to the end user
                    Dim Message = StaffBrokerFunctions.GetTemplate("RmbCancelled", PortalId)
                    Dim StaffMbr = UserController.GetUserById(PortalId, rmb.First.UserId)
                    Dim comments As String = ""
                    If tbApprComments.Text.Trim().Length > 0 Then
                        comments = Translate("CommentLeft").Replace("[FIRSTNAME]", UserInfo.FirstName).Replace("[COMMENT]", tbApprComments.Text)
                    End If

                    Message = Message.Replace("[STAFFNAME]", StaffMbr.FirstName)
                    Message = Message.Replace("[APPRNAME]", UserInfo.FirstName & " " & UserInfo.LastName)
                    Message = Message.Replace("[APPRFIRSTNAME]", UserInfo.FirstName)
                    Message = Message.Replace("[COMMENTS]", comments)

                    DotNetNuke.Services.Mail.Mail.SendMail("P2C Reimbursements <reimbursements@p2c.com>", StaffMbr.Email, "", Translate("EmailCancelledSubject").Replace("[RMBNO]", rmb.First.RID).Replace("[USERREF]", rmb.First.UserRef), Message, "", "HTML", "", "", "", "")

                    pnlMain.Visible = False
                    pnlSplash.Visible = True

                    Log(rmb.First.RMBNo, "DELETED")
                    ScriptManager.RegisterStartupScript(btnDelete, btnDelete.GetType(), "select0", "selectIndex(0)", True)
                End If

                SubmitChanges()
                Await LoadMenuAsync()
            End If

        End Sub

        Protected Async Sub btnApprove_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnApprove.Click
            saveIfNecessary()
            Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value
            If rmb.Count > 0 Then
                Dim rmbTotal = CType((From a In d.AP_Staff_RmbLines Where a.RmbNo = rmb.First.RMBNo Select a.GrossAmount).Sum(), Decimal?).GetValueOrDefault(0)
                rmb.First.SpareField1 = rmbTotal.ToString("C") ' currency formatted string
                rmb.First.Status = RmbStatus.Approved
                rmb.First.Locked = True
                rmb.First.ApprDate = Now
                rmb.First.ApprUserId = UserId
                rmb.First.Period = Nothing
                rmb.First.Year = Nothing
                SubmitChanges()

                Dim refreshMenuTasks = New List(Of Task)
                refreshMenuTasks.Add(loadBasicSubmittedPaneAsync())
                refreshMenuTasks.Add(loadBasicApprovablePaneAsync())
                refreshMenuTasks.Add(loadBasicApprovedPaneAsync())
                Dim Team As List(Of User) = StaffBrokerFunctions.GetTeam(UserId)
                If (Team.Count > 0) Then
                    refreshMenuTasks.Add(buildTeamApprovedTreeAsync(Team))
                End If
                If (IsAccounts()) Then
                    Dim allStaff = StaffBrokerFunctions.GetStaff()
                    refreshMenuTasks.Add(buildAllApprovedTreeAsync(allStaff))
                End If

                Dim ObjAppr As UserInfo = UserController.GetUserById(PortalId, Me.UserId)
                Dim theUser As UserInfo = UserController.GetUserById(PortalId, rmb.First.UserId)

                'SEND APRROVE EMAIL
                Dim Emessage = ""
                Emessage = StaffBrokerFunctions.GetTemplate("RmbApprovedEmail", PortalId)
                Emessage = Emessage.Replace("[STAFFNAME]", theUser.DisplayName).Replace("[RMBNO]", rmb.First.RID).Replace("[USERREF]", IIf(rmb.First.UserRef <> "", rmb.First.UserRef, "None"))
                Emessage = Emessage.Replace("[APPROVER]", ObjAppr.DisplayName)
                If rmb.First.Changed = True Then
                    Emessage = Emessage.Replace("[CHANGES]", ". " & Translate("EmailApproverChanged"))
                    rmb.First.Changed = False
                    SubmitChanges()
                Else
                    Emessage = Emessage.Replace("[CHANGES]", "")
                End If
                DotNetNuke.Services.Mail.Mail.SendMail("P2C Reimbursements <reimbursements@p2c.com>", theUser.Email, "", Translate("EmailApprovedSubjectP").Replace("[RMBNO]", rmb.First.RID).Replace("[USERREF]", rmb.First.UserRef), Emessage, "", "HTML", "", "", "", "")

                Log(rmb.First.RMBNo, "Approved")
                Dim message As String = Translate("RmbApproved").Replace("[RMBNO]", rmb.First.RID)
                Dim t As Type = btnApprove.GetType()

                Await Task.WhenAll(refreshMenuTasks)

                ScriptManager.RegisterStartupScript(btnApprove, t, "select2", "selectIndex(2); alert(""" & message & """);", True)
                btnApprove.Visible = False
                pnlMain.Visible = False
                pnlSplash.Visible = True

            End If
        End Sub

        Protected Async Sub btnSave_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSave.Click, btnSaveAdv.Click
            saveIfNecessary()
        End Sub

        Protected Async Sub addLinebtn2_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles addLinebtn2.Click

            'ddlLineTypes_SelectedIndexChanged(Me, Nothing)
            'ddlCostcenter.SelectedValue = ddlChargeTo.SelectedValue

            saveIfNecessary()
            tbCostcenter.Text = hfChargeToValue.Value

            ddlLineTypes.Items.Clear()
            Dim lineTypes = From c In d.AP_StaffRmb_PortalLineTypes Where c.PortalId = PortalId Order By c.ViewOrder Select c.AP_Staff_RmbLineType.LineTypeId, c.LocalName, c.PCode, c.DCode

            If StaffBrokerFunctions.IsDept(PortalId, hfChargeToValue.Value) Then
                lineTypes = lineTypes.Where(Function(x) x.DCode <> "")

            Else
                lineTypes = lineTypes.Where(Function(x) x.PCode <> "")
            End If
            ddlLineTypes.DataSource = lineTypes
            ddlLineTypes.DataBind()

            Await ResetNewExpensePopupAsync(True)
            cbRecoverVat.Checked = False
            tbVatRate.Text = ""
            tbShortComment.Text = ""

            'PopupTitle.Text = "Add New Reimbursement Expense"
            btnSaveLine.CommandName = "Save"

            hfOrigCurrency.Value = ""
            hfOrigCurrencyValue.Value = ""

            ifReceipt.Attributes("src") = Request.Url.Scheme & "://" & Request.Url.Authority & "/DesktopModules/AgapeConnect/StaffRmb/ReceiptEditor.aspx?RmbNo=" & hfRmbNo.Value & "&RmbLine=New"
            pnlElecReceipts.Attributes("style") = "display: none;"
            Dim jscript As String = ""
            jscript &= " $('#" & hfOrigCurrency.ClientID & "').attr('value', '');"
            jscript &= " $('#" & hfOrigCurrencyValue.ClientID & "').attr('value', '');"

            Dim t As Type = addLinebtn2.GetType()
            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            sb.Append("<script language='javascript'>")

            sb.Append(jscript & "showNewLinePopup();")
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

        Protected Async Sub tbYouRef_Change(sender As Object, e As System.EventArgs) Handles tbYouRef.TextChanged
            saveIfNecessary()
            Await LoadMenuAsync()
        End Sub

        Protected Async Sub ddlCompany_Change(sender As Object, e As System.EventArgs) Handles ddlCompany.SelectedIndexChanged
            'Await LoadVendorsAsync()
            'tbVendorId.Enabled = True
            ScriptManager.RegisterClientScriptBlock(ddlCompany, ddlCompany.GetType(), "loadVendors", "loadVendorIds();", True)
        End Sub

        Protected Async Sub tbVendorId_Change(sender As Object, e As System.EventArgs) Handles tbVendorId.TextChanged
            Await LoadRemitToAsync()
            tbVendorId.Enabled = True
            ddlRemitTo.Enabled = True
        End Sub

        Protected Async Sub ddlRemitTo_Change(sender As Object, e As System.EventArgs) Handles ddlRemitTo.SelectedIndexChanged
            btnSubmitPostingData.Enabled = True
        End Sub


        Protected Async Sub btnOK_Click(sender As Object, e As System.EventArgs) Handles btnOK.Click
            Dim theLine = From c In d.AP_Staff_RmbLines Where c.RmbLineNo = CInt(hfSplitLineId.Value)
            If theLine.Count > 0 Then
                For Each row As TableRow In tblSplit.Rows
                    Dim RowAmount = CType(row.Cells(1).Controls(0), TextBox).Text
                    Dim RowDesc = CType(row.Cells(0).Controls(0), TextBox).Text
                    If RowAmount = "" Or RowDesc = "" Or RowAmount = "Amount" Or RowDesc = "Description" Then
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
            Await LoadRmbAsync(hfRmbNo.Value)

            Dim t As Type = btnOK.GetType()
            Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
            sb.Append("<script language='javascript'>")
            sb.Append("closePopupSplit();")
            sb.Append("</script>")
            ScriptManager.RegisterStartupScript(btnOK, t, "clostSplit", sb.ToString, False)

        End Sub

        Protected Async Sub btnProcess_Click(sender As Object, e As System.EventArgs) Handles btnSubmitPostingData.Click, btnAccountWarningYes.Click
            saveIfNecessary()
            'Mark as Pending Download in next batch.
            Dim TaskList = New List(Of Task)
            Dim theRmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value)
            Dim Extra = From c In d.AP_Staff_Rmb_Post_Extras Where c.RMBNo = CInt(hfRmbNo.Value)
            Dim PostingData As AP_Staff_Rmb_Post_Extra
            Dim insert = True
            If (Extra.Count > 0) Then
                PostingData = Extra.First
                insert = False
            Else
                PostingData = New AP_Staff_Rmb_Post_Extra
            End If

            'Check Balance
            If CType(sender, Button).ID <> "btnAccountWarningYes" And Settings("WarnIfNegative") Then
                Dim pendingBalanceTask = GetNumericRemainingBalanceAsync(2)
                Dim RmbBalance = theRmb.First.AP_Staff_RmbLines.Where(Function(x) x.CostCenter = x.AP_Staff_Rmb.CostCenter).Sum(Function(x) x.GrossAmount)
                Dim pendingBalance = Await pendingBalanceTask
                If RmbBalance > pendingBalance Then
                    ScriptManager.RegisterStartupScript(Page, Me.GetType(), "popupWarning", "showAccountWarning(); closePostDataDialog();", True)
                    Return
                End If
            End If

            theRmb.First.Status = RmbStatus.PendingDownload
            theRmb.First.ProcDate = Today
            theRmb.First.MoreInfoRequested = False
            theRmb.First.ProcUserId = UserId
            SubmitChanges()
            TaskList.Add(loadBasicApprovedPaneAsync())
            TaskList.Add(loadBasicProcessingPaneAsync())
            Dim Team As List(Of User) = StaffBrokerFunctions.GetTeam(UserId)
            If (Team.Count > 0) Then
                TaskList.Add(buildTeamApprovedTreeAsync(Team))
                TaskList.Add(buildTeamProcessingTreeAsync(Team))
            End If
            If (IsAccounts()) Then
                Dim allStaff = StaffBrokerFunctions.GetStaff()
                TaskList.Add(buildAllApprovedTreeAsync(allStaff))
            End If

            PostingData.RMBNo = CInt(hfRmbNo.Value)
            PostingData.Company = ddlCompany.SelectedValue
            Dim fmt = New DateTimeFormatInfo()
            fmt.ShortDatePattern = "MM/dd/yyyy"
            PostingData.PostingDate = Convert.ToDateTime(dtPostingDate.Text, fmt)
            PostingData.BatchId = tbBatchId.Text
            PostingData.Reference = tbPostingReference.Text
            PostingData.InvoiceNo = tbInvoiceNumber.Text
            PostingData.VendorId = tbVendorId.Text
            PostingData.RemitToAddress = ddlRemitTo.SelectedValue
            Log(theRmb.First.RMBNo, "Processed - this reimbursement will be added to the next download batch")

            TaskList.Add(LoadRmbAsync(hfRmbNo.Value))
            If (insert) Then
                d.AP_Staff_Rmb_Post_Extras.InsertOnSubmit(PostingData)
            End If
            SubmitChanges()
            Await Task.WhenAll(TaskList)

            Dim message = Translate("NextBatch")
            ScriptManager.RegisterStartupScript(Page, Me.GetType(), "closePostData", "closePostDataDialog(); alert(""" & message & """);", True)
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
            HttpContext.Current.Response.End()
        End Sub

        Protected Sub btnMarkProcessed_Click(sender As Object, e As System.EventArgs) Handles btnMarkProcessed.Click
            DownloadBatch(True)

            'Dim RmbList As List(Of Integer) = Session("RmbList")
            'If Not RmbList Is Nothing Then
            '    Dim q = From c In d.AP_Staff_Rmbs Where RmbList.Contains(c.RMBNo) And c.PortalId = PortalId

            '    For Each row In q
            '        row.Status = RmbStatus.Processing
            '        row.ProcDate = Now
            '        Log(row.RMBNo, "Marked as Processed - after a manual download")
            '    Next
            'End If
            'Dim AdvList As List(Of Integer) = Session("AdvList")
            'If Not AdvList Is Nothing Then

            '    Dim r = From c In d.AP_Staff_AdvanceRequests Where AdvList.Contains(c.AdvanceId) And c.PortalId = PortalId

            '    For Each row In r
            '        row.RequestStatus = RmbStatus.Processing
            '        row.ProcessedDate = Now
            '        Log(row.AdvanceId, "Advance Marked as Processed - after a manual download")
            '    Next

            'End If

            'd.SubmitChanges()




            'If hfRmbNo.Value <> "" Then
            '    If hfRmbNo.Value > 0 Then
            '        Await LoadRmbAsync(CInt(hfRmbNo.Value))
            '    Else
            '        LoadAdv(-CInt(hfRmbNo.Value))
            '    End If


            'End If

            'Await LoadMenuAsync()


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
            Dim TaskList As New List(Of Task)
            If theRmb.Status = RmbStatus.Processing Then
                'If the reimbursement has already been downloaded, a warning should be displayed - but hte reimbursement can be simply unprocessed
                theRmb.Status = RmbStatus.Approved
                SubmitChanges()
                Log(theRmb.RMBNo, "UNPROCESSED, after it had been fully processed")
            Else
                'if it has not been downloaded, it will be downloaded very soon. We need to check if a download is already in progress.
                If StaffBrokerFunctions.GetSetting("Datapump", PortalId) = "locked" Then
                    'If a download is in progress, we need to display a "not at this time" message
                    Dim message = "This reimbursement cannot be unprocessed at this time, as it is currently being downloaded by the automatic datapump (transaction broker). You can try again in a few minutes, but be aware that it will already have been processed into your accounts program."
                    Dim t As Type = Me.GetType()
                    ScriptManager.RegisterStartupScript(Page, t, "popup", "alert(""" & message & """);", True)
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
                    SubmitChanges()
                    'Then release the lock.
                    StaffBrokerFunctions.SetSetting("Datapump", "Unlocked", PortalId)
                    Log(theRmb.RMBNo, "UNPROCESSED - before it was downloaded")
                End If
            End If
            TaskList.Add(LoadRmbAsync(hfRmbNo.Value))
            TaskList.Add(loadBasicApprovedPaneAsync())
            TaskList.Add(loadBasicProcessingPaneAsync())
            Dim Team As List(Of User) = StaffBrokerFunctions.GetTeam(UserId)
            If (Team.Count > 0) Then
                TaskList.Add(buildTeamApprovedTreeAsync(Team))
                TaskList.Add(buildTeamProcessingTreeAsync(Team))
            End If
            If (IsAccounts()) Then
                Dim allStaff = StaffBrokerFunctions.GetStaff()
                TaskList.Add(buildAllApprovedTreeAsync(allStaff))
            End If
            Await Task.WhenAll(TaskList)
        End Sub

        Protected Async Sub GridView1_RowCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles GridView1.RowCommand
            If e.CommandName = "myDelete" Then
                ' d.AP_Staff_RmbLineAddStaffs.DeleteAllOnSubmit(From c In d.AP_Staff_RmbLineAddStaffs Where c.RmbLineId = CInt(e.CommandArgument))
                d.AP_Staff_RmbLines.DeleteAllOnSubmit(From c In d.AP_Staff_RmbLines Where c.RmbLineNo = CInt(e.CommandArgument))
                SubmitChanges()
                Await LoadRmbAsync(hfRmbNo.Value)

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
                    btnSaveLine.Enabled = True

                    If lineTypes.Where(Function(x) x.LineTypeId = theLine.First.LineType).Count = 0 Then
                        ddlLineTypes.Items.Add(New ListItem(theLine.First.AP_Staff_RmbLineType.AP_StaffRmb_PortalLineTypes.Where(Function(x) x.PortalId = PortalId).First.LocalName, theLine.First.LineType))
                        '  ddlLineTypes.Items.Add(New ListItem(theLine.First.LineType,"Wrong type"))

                        'Wrong Type... needs changing!
                        lblIncType.Visible = True
                        btnSaveLine.Enabled = False
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

                    Dim receiptMode = 1
                    If theLine.First.VATReceipt Then
                        receiptMode = 0
                        ' If we have any files matching this line, or our receiptImageId is valid
                    ElseIf (From lf In d.AP_Staff_RmbLine_Files Where lf.RmbLineNo = theLine.First.RmbLineNo And lf.RMBNo = theLine.First.RmbNo).Count > 0 Or
                        (Not theLine.First.ReceiptImageId Is Nothing And theLine.First.ReceiptImageId > 0) Then
                        receiptMode = 2
                        ' If we don't have a receipt
                    ElseIf Not theLine.First.Receipt Then
                        receiptMode = -1
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

                    Try
                        tbShortComment.Text = GetLineComment(theLine.First.Comment, theLine.First.OrigCurrency, If(theLine.First.OrigCurrencyAmount Is Nothing, 0, theLine.First.OrigCurrencyAmount), theLine.First.ShortComment, False, Nothing, IIf(theLine.First.AP_Staff_RmbLineType.TypeName = "Mileage", theLine.First.Spare2, ""))
                    Catch
                        tbShortComment.Text = ""
                    End Try

                    'If ddlLineTypes.SelectedValue = 7 Then

                    '    ucType.GetMethod("LoadStaff").Invoke(theControl, New Object() {theLine.First.RmbLineNo, Settings, CanAddPass()})
                    'End If

                    btnSaveLine.CommandName = "Edit"
                    btnSaveLine.CommandArgument = CInt(e.CommandArgument)
                    tbCostcenter.Text = theLine.First.CostCenter
                    ddlAccountCode.SelectedValue = theLine.First.AccountCode

                    ifReceipt.Attributes("src") = Request.Url.Scheme & "://" & Request.Url.Authority & "/DesktopModules/AgapeConnect/StaffRmb/ReceiptEditor.aspx?RmbNo=" & theLine.First.RmbNo & "&RmbLine=" & theLine.First.RmbLineNo
                    ' Check to see if we have any images
                    If receiptMode = 2 Then
                        pnlElecReceipts.Attributes("style") = ""
                    Else
                        pnlElecReceipts.Attributes("style") = "display: none;"
                    End If

                    Dim t As Type = GridView1.GetType()
                    Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                    sb.Append("<script language='javascript'>")
                    sb.Append(jscript & "showNewLinePopup();")
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
                    Dim loadRmbTask = LoadRmbAsync(hfRmbNo.Value)
                    SubmitChanges()

                    Dim theUser = UserController.GetUserById(PortalId, theLine.First.AP_Staff_Rmb.UserId)
                    Dim t As Type = GridView1.GetType()
                    Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder()
                    sb.Append("<script language='javascript'>")
                    sb.Append("window.open('mailto:" & theUser.Email & "?subject=Reimbursment " & theLine.First.AP_Staff_Rmb.RID & ": Deferred Transactions');")
                    sb.Append("</script>")
                    Await loadRmbTask

                    ScriptManager.RegisterStartupScript(GridView1, t, "email", sb.ToString, False)

                End If
            End If

        End Sub

        Protected Async Sub menu_ItemCommand(ByVal source As Object, ByVal e As System.Web.UI.WebControls.DataListCommandEventArgs) Handles dlProcessing.ItemCommand, dlAdvProcessing.ItemCommand, dlApproved.ItemCommand, dlCancelled.ItemCommand, dlToApprove.ItemCommand, dlSubmitted.ItemCommand, dlPending.ItemCommand, dlAdvApproved.ItemCommand, dlAdvSubmitted.ItemCommand, dlAdvToApprove.ItemCommand, dlAdvApproved.ItemCommand
            hfRmbNo.Value = e.CommandArgument
            If e.CommandName = "Goto" Then
                Await LoadRmbAsync(e.CommandArgument)
            ElseIf e.CommandName = "GotoAdvance" Then
                Await LoadAdvAsync(e.CommandArgument)
                'Await LoadMenuAsync()
            End If
        End Sub

        Protected Async Sub menu_subtree_ItemCommand(ByVal node As TreeView, ByVal e As System.EventArgs) Handles tvTeamApproved.SelectedNodeChanged, tvTeamProcessing.SelectedNodeChanged, tvFinance.SelectedNodeChanged
            Await LoadRmbAsync(node.SelectedValue)
            ScriptManager.RegisterStartupScript(GridView1, GridView1.GetType(), "deselect_menu", "deselectPreviousMenuItem()", True)
        End Sub


#End Region
#Region "OnChange Events"
        Protected Async Sub ddlLineTypes_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlLineTypes.SelectedIndexChanged
            Dim resetTask = ResetNewExpensePopupAsync(False)
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
                btnSaveLine.Enabled = True
            End If

            Await resetTask
        End Sub

        Protected Async Sub tbChargeTo_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles tbChargeTo.TextChanged
            'The User selected a new cost centre
            Try
                If (hfChargeToValue.Value.Length = 0) Then
                    tbChargeTo.Text = ""
                    ddlApprovedBy.Items.Clear()
                    Return
                End If
                'Detect if Dept is now Personal or vica versa
                Dim PortalId = CInt(hfPortalId.Value)
                Dim RmbNo = CInt(hfRmbNo.Value)
                Dim TaskList = New List(Of Task)
                Dim Dept = StaffBrokerFunctions.IsDept(PortalId, hfChargeToValue.Value)
                Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo And c.PortalId = PortalId
                If (rmb.Count > 0) Then
                    Dim getAccountBalanceTask = getAccountBalanceAsync(hfChargeToValue.Value, StaffRmbFunctions.logonFromId(PortalId, UserId))
                    Dim getBudgetBalanceTask = getBudgetBalanceAsync(hfChargeToValue.Value, StaffRmbFunctions.logonFromId(PortalId, UserId))
                    rmb.First.CostCenter = hfChargeToValue.Value
                    rmb.First.Department = Dept

                    For Each row In rmb.First.AP_Staff_RmbLines
                        If rmb.First.CostCenter = row.CostCenter Then
                            row.Department = Dept
                            row.AccountCode = GetAccountCode(row.LineType, hfChargeToValue.Value)
                            row.CostCenter = hfChargeToValue.Value
                        End If
                    Next
                    If (rmb.First.Status <> RmbStatus.Draft) Then
                        Undelete_Current_Rmb()
                        rmb.First.Status = RmbStatus.Draft
                        rmb.First.ApprUserId = Nothing
                        SubmitChanges()
                        TaskList.Add(LoadMenuAsync())
                        ScriptManager.RegisterStartupScript(tbChargeTo, tbChargeTo.GetType(), "select0", "selectIndex(0)", True)
                    Else
                        SubmitChanges()
                    End If
                    TaskList.Add(updateApproversListAsync(rmb.First))
                    Dim aBal = Await getAccountBalanceTask
                    Dim bBal = Await getBudgetBalanceTask
                    updateBalanceLabels(aBal, bBal)
                    Await Task.WhenAll(TaskList)
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
            Dim refreshMenu = (Not rmb.First.Status = RmbStatus.Draft)
            rmb.First.Status = RmbStatus.Draft
            SubmitChanges()
            Undelete_Current_Rmb()
            If refreshMenu Then
                Await LoadMenuAsync()
            End If
            ScriptManager.RegisterStartupScript(ddlApprovedBy, ddlApprovedBy.GetType(), "selectDrafts", "selectIndex(0)", True)
        End Sub

        Private Sub Undelete_Current_Rmb()
            lblStatus.Text = Translate(RmbStatus.StatusName(RmbStatus.Draft))
            btnDelete.Visible = True
            btnSubmit.Visible = True
            btnSubmit.Text = Translate("btnSubmit")
            btnSubmit.Enabled = tbChargeTo.Text.Length = 6 And ddlApprovedBy.SelectedIndex > 0 And GridView1.Rows.Count > 0
            btnSubmit.ToolTip = Translate("btnSubmitHelp")
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

        Protected Function GetRmbTitleTeamShort(ByVal RID As Integer, ByVal RmbDate As Date, ByVal total As String) As String
            Try
                Dim rtn As String = "<span style=""font-size: 6.5pt; color: #999999;"">#" & ZeroFill(RID.ToString, 5)

                If (RmbDate > (New Date(2010, 1, 1))) Then
                    rtn = rtn & ": " & RmbDate.ToShortDateString
                End If
                rtn = rtn & " - " & total
                rtn = rtn & "</span>"
                Return rtn
            Catch ex As Exception
                Throw New Exception("Error building title: " + ex.Message)
            End Try
        End Function

        Protected Function GetRmbTitleFinance(ByVal RID As Integer, ByVal ApprDate As Date?, ByVal amount As String) As String
            Try
                Dim DateString = If(ApprDate Is Nothing, "not approved", CType(ApprDate, Date).ToShortDateString)
                Dim rtn As String = "<span style=""font-size: 6.5pt; color: #999999;"">#" & ZeroFill(RID.ToString, 5)
                '  colourize date based on how old it is
                If (ApprDate Is Nothing) Then
                    rtn = rtn & ": <span class='dateproblem'>" & DateString & "</span>"
                ElseIf (ApprDate > Now().AddDays(-1)) Then
                    rtn = rtn & ": <span class='rightaway'>" & DateString & "</span>"
                ElseIf (ApprDate > Now().AddDays(-3)) Then
                    rtn = rtn & ": <span class='aheadofschedule'>" & DateString & "</span>"
                ElseIf (ApprDate > Now().AddDays(-5)) Then
                    rtn = rtn & ": <span class='ontime'>" & DateString & "</span>"
                ElseIf (ApprDate > Now().AddDays(-7)) Then
                    rtn = rtn & ": <span class='late'>" & DateString & "</span>"
                ElseIf (ApprDate > (New Date(2010, 1, 1))) Then
                    rtn = rtn & ": <span class='overdue'>" & DateString & "</span>"
                Else
                    rtn = rtn & ": *date error*"
                End If
                If (amount IsNot Nothing) Then
                    rtn = rtn & " - " & amount
                End If
                rtn = rtn & "</span>"
                Return rtn
            Catch ex As Exception
                Throw New Exception("Error building title: " + ex.Message)
            End Try
        End Function
        Protected Function GetAdvTitleTeamShort(ByVal LocalAdvanceId As Integer, ByVal RequestDate As Date, ByVal name As String) As String
            Try
                Dim rtn As String = "<span style=""font-size: 6.5pt; color: #999999;"">Adv#" & ZeroFill(LocalAdvanceId.ToString, 4)

                If RequestDate > (New Date(2010, 1, 1)) Then
                    rtn = rtn & ": " & RequestDate.ToShortDateString
                End If
                If (name IsNot Nothing) Then
                    rtn = rtn & " - " & name
                End If
                rtn = rtn & "</span>"
                Return rtn
            Catch ex As Exception
                Throw New Exception("Error building adv title: " + ex.Message)
            End Try
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

        Protected Function GetImageType(lineId As Integer) As String
            'Returns the image type of the first image associated with the lineId
            Dim receiptIds = From e In d.AP_Staff_RmbLine_Files Where e.RmbLineNo = lineId Order By e.RecNum Select e.FileId
            If receiptIds.Count() < 1 Then
                Return "no receipts found"
            End If
            Dim file = DotNetNuke.Services.FileSystem.FileManager.Instance.GetFile(receiptIds.first)
            If file Is Nothing Then
                Return "missing file"
            End If
            Return file.Extension.ToLower()
        End Function

        Protected Function GetImageUrl(lineId As Integer) As String
            Dim urls = From e In d.AP_Staff_RmbLine_Files Where e.RmbLineNo = lineId Order By e.RecNum Select e.URL
            If urls.Count() < 1 Then
                Return "no receipts found"
            End If
            If (urls.First) Is Nothing Then
                Return ""
            Else
                Return urls.First
            End If
        End Function

        Protected Function HasMultipleReceipts(lineId As Integer) As Boolean
            Dim receipts = From e In d.AP_Staff_RmbLine_Files Where e.RmbLineNo = lineId Select e
            Return (receipts.Count() > 1)
        End Function

        Protected Function CanEdit(ByVal status As Integer) As Boolean
            Return status <> RmbStatus.Processing And status <> RmbStatus.PendingDownload And status <> RmbStatus.DownloadFailed And (status <> RmbStatus.Approved Or IsAccounts())
        End Function

        Protected Function isStaffAccount() As Boolean
            ' Determine if the currently visible reimbursement is a staff account or a ministry account
            Dim result = StaffRmbFunctions.isStaffAccount(tbChargeTo.Text)
            Return result
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

        Public Async Function GetNumericRemainingBalanceAsync(ByVal mode As Integer) As Task(Of Double)
            Return GetNumericRemainingBalance(mode)
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

            Return Math.Round(AccountBalance + Advance - (rTotal + aTotal), 2)

        End Function

        'Public Function GetRemainingBalance() As String
        '    If (hfAccountBalance.Value = "") Then
        '        Return BALANCE_INCONCLUSIVE
        '    End If
        '    Dim remainingBalance = GetNumericRemainingBalance(1).ToString("0.00")
        '    Return StaffBrokerFunctions.GetFormattedCurrency(PortalId, remainingBalance)
        'End Function

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
        Private Sub SubmitChanges()
            d.SubmitChanges()
        End Sub

        Private Sub saveIfNecessary()
            'Get Rmb
            Dim Rmb As AP_Staff_Rmb
            Try
                Dim RmbNo As Integer
                Dim PortalId As Integer
                RmbNo = CInt(hfRmbNo.Value)
                PortalId = CInt(hfPortalId.Value)
                Dim rmbs = From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo And c.PortalId = PortalId
                If rmbs.Count > 0 Then
                    Rmb = rmbs.First
                Else
                    Return
                End If
            Catch
                Return
            End Try

            Dim save_necessary = False

            hfChargeToValue.Value = tbChargeTo.Text
            If (Rmb.CostCenter <> tbChargeTo.Text) Then
                save_necessary = True
                Rmb.CostCenter = tbChargeTo.Text
                For Each row In (From c In Rmb.AP_Staff_RmbLines Where c.CostCenter = Rmb.CostCenter)
                    row.CostCenter = tbChargeTo.Text
                Next
            End If
            If (ddlApprovedBy.SelectedValue.Length > 0) AndAlso (Rmb.ApprUserId <> ddlApprovedBy.SelectedValue) Then
                save_necessary = True
                Rmb.ApprUserId = ddlApprovedBy.SelectedValue
            End If
            If (Rmb.UserRef <> tbYouRef.Text) Then
                save_necessary = True
                Rmb.UserRef = tbYouRef.Text
            End If
            If (Rmb.UserComment <> tbComments.Text) Then
                save_necessary = True
                Rmb.UserComment = tbComments.Text
            End If
            If (Rmb.ApprComment <> tbApprComments.Text) Then
                save_necessary = True
                Rmb.ApprComment = tbApprComments.Text
            End If
            If (Rmb.AcctComment <> tbAccComments.Text) Then
                save_necessary = True
                Rmb.AcctComment = tbAccComments.Text
            End If
            If (Rmb.MoreInfoRequested <> (cbMoreInfo.Checked Or cbApprMoreInfo.Checked)) Then
                save_necessary = True
                Rmb.Locked = False
                Rmb.MoreInfoRequested = (cbMoreInfo.Checked Or cbApprMoreInfo.Checked)
            End If
            If tbAdvanceAmount.Text = "" Then
                tbAdvanceAmount.Text = 0
            End If

            If (ENABLE_ADVANCE_FUNCTIONALITY) Then
                lblAdvError.Text = ""
                Try
                    Rmb.AdvanceRequest = Double.Parse(tbAdvanceAmount.Text, New CultureInfo("en-US"))
                    If Rmb.AdvanceRequest > Rmb.AP_Staff_RmbLines.Sum(Function(x) x.GrossAmount) Then
                        Rmb.AdvanceRequest = Rmb.AP_Staff_RmbLines.Sum(Function(x) x.GrossAmount)
                        tbAdvanceAmount.Text = Rmb.AdvanceRequest.ToString("0.00")
                    End If
                Catch
                    lblAdvError.Text = Translate("AdvanceError")
                    Return
                End Try
            End If

            If (save_necessary) Then
                SubmitChanges()
            End If
        End Sub

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
                tmc.UpdateTabModuleSetting(TabModuleId, "Expire", 90)
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

        Private Async Function LoadCompaniesAsync() As Task
            ddlCompany.Items.Clear()
            ddlCompany.Items.Add("")
            Dim companies = Await StaffRmbFunctions.getCompanies()
            For Each company In companies
                Dim name = company("CompanyName").ToString
                Dim value = company("CompanyID").ToString
                ddlCompany.Items.Add(New ListItem(name, value))
            Next
            ddlCompany.SelectedIndex = 0
        End Function

        Private Async Function LoadRemitToAsync() As Task
            ddlRemitTo.Items.Clear()
            ddlRemitTo.Items.Add("")
            Dim addresses = Await StaffRmbFunctions.getRemitToAddresses(ddlCompany.SelectedValue, tbVendorId.Text)
            Dim defaultValue = ""
            For Each address In addresses
                Dim star = ""
                Dim value = address("AddressID").ToString
                If address("DefaultRemitToAddress").ToString = "Y" Then
                    star = "*"
                    defaultValue = value
                End If
                Dim name = star & value & ": " & address("Address1").ToString
                ddlRemitTo.Items.Add(New ListItem(name, value))
            Next
            If (defaultValue.Length = 0) Then
                ddlRemitTo.SelectedIndex = 0
            Else
                ddlRemitTo.SelectedValue = defaultValue
                btnSubmitPostingData.Enabled = True
            End If
        End Function

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

        Protected Async Function ResetNewExpensePopupAsync(ByVal blankValues As Boolean) As task
            Try
                Dim lt = From c In d.AP_Staff_RmbLineTypes Where c.LineTypeId = ddlLineTypes.SelectedValue
                If lt.Count > 0 Then
                    Dim Comment As String = ""
                    Dim Amount As Double = 0.0
                    Dim theDate As Date = Today
                    Dim VAT As Boolean = False
                    Dim Receipt As Boolean = True
                    Dim Province As String = Nothing
                    Dim receiptMode As Integer = 1

                    If Not blankValues Then
                        Try
                            If Not (theControl Is Nothing) Then
                                Dim ucTypeOld As Type = theControl.GetType()
                                ' Attempt to get the receiptMode
                                Try
                                    receiptMode = CInt(ucTypeOld.GetProperty("ReceiptType").GetValue(theControl, Nothing))
                                Catch ex As Exception ' We couldn't get one; no big deal, but keep going with this block of code
                                End Try
                                Comment = CStr(ucTypeOld.GetProperty("Comment").GetValue(theControl, Nothing))
                                theDate = CDate(ucTypeOld.GetProperty("theDate").GetValue(theControl, Nothing))
                                Amount = CDbl(ucTypeOld.GetProperty("Amount").GetValue(theControl, Nothing))
                                VAT = CStr(ucTypeOld.GetProperty("VAT").GetValue(theControl, Nothing))
                                Receipt = CStr(ucTypeOld.GetProperty("Receipt").GetValue(theControl, Nothing))
                                Province = CStr(ucTypeOld.GetProperty("Spare1").GetValue(theControl, Nothing))
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                    ' Save the standard values
                    If (Province Is Nothing) Then
                        Province = StaffRmbFunctions.GetDefaultProvince(UserId)
                    End If
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
                    ucType.GetProperty("Spare1").SetValue(theControl, Province, Nothing)
                    ucType.GetProperty("Spare2").SetValue(theControl, "", Nothing)
                    ucType.GetProperty("Spare3").SetValue(theControl, "", Nothing)
                    ucType.GetProperty("Spare4").SetValue(theControl, "", Nothing)
                    ucType.GetProperty("Spare5").SetValue(theControl, "", Nothing)
                    ' Attempt to set the receipttype
                    Try
                        ucType.GetProperty("ReceiptType").SetValue(theControl, receiptMode, Nothing)
                        If (receiptMode = 2) Then ' We have electronic receipts
                            pnlElecReceipts.Attributes("style") = ""
                        Else ' Make sure it's hidden
                            pnlElecReceipts.Attributes("style") = "display: none"
                        End If
                    Catch ex As Exception
                        ' We apparently can't set the receipt type
                        ' Need to ensure the electronic receipts are hidden
                        pnlElecReceipts.Attributes("style") = "display: none;"
                    End Try
                    ucType.GetMethod("Initialize").Invoke(theControl, New Object() {Settings})

                    ddlAccountCode.SelectedValue = GetAccountCode(lt.First.LineTypeId, tbCostcenter.Text)
                End If
            Catch ex As Exception
                StaffBrokerFunctions.EventLog("Error Resetting Expense Popup", ex.ToString, UserId)
            End Try
        End Function

        Protected Sub SendApprovalEmail(ByVal theRmb As AP_Staff_Rmb)
            Try

                Dim SpouseId As Integer = StaffBrokerFunctions.GetSpouseId(theRmb.UserId)
                Dim ownerMessage As String = StaffBrokerFunctions.GetTemplate("RmbConfirmation", PortalId)
                Dim approverMessage As String = StaffBrokerFunctions.GetTemplate("RmbApproverEmail", PortalId)
                Dim owner = UserController.GetUserById(theRmb.PortalId, theRmb.UserId)
                Dim approver = UserController.GetUserById(theRmb.PortalId, theRmb.ApprUserId)
                Dim toEmail = approver.Email
                Dim toName = approver.FirstName
                Dim Approvers = approver.DisplayName
                Dim hasReceipts = (From c In theRmb.AP_Staff_RmbLines Where c.Receipt = True And (c.ReceiptImageId Is Nothing)).Count > 0

                'Email to the submitter here 
                ownerMessage = ownerMessage.Replace("[APPROVER]", Approvers).Replace("[EXTRA]", "").Replace("[STAFFNAME]", owner.FirstName) _
                    .Replace("[RMBNO]", theRmb.RID).Replace("[USERREF]", theRmb.UserRef)
                ownerMessage = ownerMessage.Replace("[STAFFACTION]", If(hasReceipts, Translate("PostReceipts"), Translate("NoPostRecipts")))
                ownerMessage = ownerMessage.Replace("[PRINTOUT]", "<a href='" & Request.Url.Scheme & "://" & Request.Url.Authority & Request.ApplicationPath & "DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & theRmb.RMBNo & "&UID=" & theRmb.UserId & "' target-'_blank' style='width: 134px; display:block;)'><div style='text-align: center; width: 122px; margin: 10px;'><img src='" _
                    & Request.Url.Scheme & "://" & Request.Url.Authority & Request.ApplicationPath & "DesktopModules/AgapeConnect/StaffRmb/Images/PrintoutIcon.jpg' /><br />Printout</div></a><style> a div:hover{border: solid 1px blue;}</style>")
                DotNetNuke.Services.Mail.Mail.SendMail("P2C Reimbursements <reimbursements@p2c.com>", UserInfo.Email, "", Translate("EmailSubmittedSubject").Replace("[RMBNO]", theRmb.RID), ownerMessage, "", "HTML", "", "", "", "")

                'Send Approvers Instructions Here
                If toEmail.Length > 0 Then
                    Dim subject = Translate("SubmittedApprEmailSubject").Replace("[STAFFNAME]", UserInfo.DisplayName)
                    approverMessage = approverMessage.Replace("[STAFFNAME]", UserInfo.DisplayName).Replace("[RMBNO]", theRmb.RID).Replace("[USERREF]", IIf(theRmb.UserRef <> "", theRmb.UserRef, "None"))
                    approverMessage = approverMessage.Replace("[APPRNAME]", toName)
                    approverMessage = approverMessage.Replace("[OLDEXPENSES]", If(hasOldExpenses(), Translate("WarningOldExpenses"), ""))
                    approverMessage = approverMessage.Replace("[COMMENTS]", If(theRmb.UserComment <> "", Translate("EmailComments") & "<br />" & theRmb.UserComment, ""))
                    If StaffRmbFunctions.isStaffAccount(theRmb.CostCenter) Then
                        'Personal Reimbursement
                        approverMessage = approverMessage.Replace("[LOWBALANCE]", If((hfAccountBalance.Value <> String.Empty) AndAlso (hfAccountBalance.Value < GetTotal(hfRmbNo.Value)), Translate("WarningLowBalanceStaffAccount"), ""))
                    Else
                        Dim low_balance = Translate("WarningLowBalance").Replace("[ACCTBAL]", hfAccountBalance.Value).Replace("[BUDGBAL]", hfBudgetBalance.Value).Replace("[ACCT]", tbChargeTo.Text)
                        approverMessage = approverMessage.Replace("[LOWBALANCE]", If(isLowBalance(), low_balance, ""))
                    End If
                    '-- Send FROM owner, so that bounces or out-of-office replies come back to owner.
                    DotNetNuke.Services.Mail.Mail.SendMail("P2C Reimbursements <" & owner.Email & ">", toEmail, "", subject, approverMessage, "", "HTML", "", "", "", "")
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
                        row.Status = RmbStatus.Processing
                        row.ProcDate = Now
                        Log(row.RMBNo, "Marked as Processed - after a manual download")
                    Next
                End If

                If Not AdvList Is Nothing Then

                    Dim r = From c In d.AP_Staff_AdvanceRequests Where AdvList.Contains(c.AdvanceId) And c.PortalId = PortalId

                    For Each row In r
                        row.RequestStatus = RmbStatus.Processing
                        row.ProcessedDate = Now
                        Log(row.AdvanceId, "Advance Marked as Processed - after a manual download")
                    Next

                End If

                d.SubmitChanges()




                If hfRmbNo.Value <> "" Then
                    If hfRmbNo.Value > 0 Then
                        Await LoadRmbAsync(CInt(hfRmbNo.Value))
                    Else
                        Await LoadAdvAsync(-CInt(hfRmbNo.Value))
                    End If


                End If

                Await LoadMenuAsync()



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
            Dim owner = UserController.GetUserById(insert.PortalId, insert.UserId)
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
                DotNetNuke.Services.Mail.Mail.SendMail(owner.Email, toEmail, "", Translate("AdvSubmittedApprEmailSubject").Replace("[STAFFNAME]", UserInfo.FirstName & " " & UserInfo.LastName), message2, "", "HTML", "", "", "", "")

            End If

            ConfMessage = ConfMessage.Replace("[APPROVER]", Approvers)
            '  DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agape.org.uk", UserInfo.Email, "donotreply@agape.org.uk", "Reimbursement #" & theRmb.RMBNo, message, Server.MapPath("/Portals/0/RmbForm" & theRmb.RMBNo & ".htm"), "HTML", "", "", "", "")
            DotNetNuke.Services.Mail.Mail.SendMail("reimbursements@p2c.com", UserInfo.Email, "", Translate("AdvEmailSubmittedSubject").Replace("[ADVNO]", insert.LocalAdvanceId), ConfMessage, "", "HTML", "", "", "", "")


            'Need to load the Advance!
            Await LoadMenuAsync()

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
                        DotNetNuke.Services.Mail.Mail.SendMail("reimbursements@p2c.com", row.Email, "", Translate("AdvEmailApprovedSubject").Replace("[ADVNO]", q.First.LocalAdvanceId).Replace("[APPROVER]", ObjAppr.DisplayName), ApprMessage, "", "HTML", "", "", "", "")

                    Next

                    'SEND APRROVE EMAIL


                    ' Dim Emessage As String = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("AdvApprovedEmail", PortalId))
                    Dim Emessage As String = StaffBrokerFunctions.GetTemplate("AdvApprovedEmail", PortalId)

                    Emessage = Emessage.Replace("[STAFFNAME]", theUser.DisplayName).Replace("[ADVNO]", q.First.LocalAdvanceId)
                    Emessage = Emessage.Replace("[APPROVER]", ObjAppr.DisplayName)

                    d.SubmitChanges()

                    ' DotNetNuke.Services.Mail.Mail.SendMail("donotreply@agape.org.uk", theUser.Email, "donotreply@agape.org.uk", "Rmb#: " & hfRmbNo.Value & "-" & rmb.First.UserRef & " has been approved", Emessage, "", "HTML", "", "", "", "")
                    DotNetNuke.Services.Mail.Mail.SendMail("reimbursements@p2c.com", theUser.Email, "", Translate("AdvEmailApprovedSubject").Replace("[ADVNO]", q.First.LocalAdvanceId).Replace("[APPROVER]", ObjAppr.DisplayName), Emessage, "", "HTML", "", "", "", "")

                    Dim loadAdvTask = LoadAdvAsync(-hfRmbNo.Value)

                    Log(q.First.AdvanceId, "Advance Approved")

                    SendMessage(Translate("AdvanceApproved").Replace("[ADVANCEID]", q.First.LocalAdvanceId), "selectIndex(2);")
                    Await loadAdvTask

                End If
            End If
        End Sub

        Protected Async Sub btnAdvReject_Click(sender As Object, e As System.EventArgs) Handles btnAdvReject.Click, btnAdvCancel.Click
            Dim q = From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = -CInt(hfRmbNo.Value) And c.PortalId = PortalId

            If q.Count > 0 Then
                Dim LockedList() = {RmbStatus.PendingDownload, RmbStatus.DownloadFailed, RmbStatus.Processing}
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
                    DotNetNuke.Services.Mail.Mail.SendMail("reimbursements@p2c.com", StaffMbr.Email, "", Translate("AdvEmailCancelledSubject").Replace("[ADVNO]", q.First.LocalAdvanceId), Message, "", "HTML", "", "", "", "")

                    Await LoadAdvAsync(-hfRmbNo.Value)

                End If
            End If

        End Sub

        Protected Async Sub btnAdvSave_Click(sender As Object, e As System.EventArgs) Handles btnAdvSave.Click
            Dim q = From c In d.AP_Staff_AdvanceRequests Where c.AdvanceId = -CInt(hfRmbNo.Value) And c.PortalId = PortalId
            If q.Count > 0 Then
                Dim LockedList() = {RmbStatus.PendingDownload, RmbStatus.DownloadFailed, RmbStatus.Processing}
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
            Await LoadAdvAsync(-hfRmbNo.Value)
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

                Await LoadAdvAsync(-hfRmbNo.Value)
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

                Await LoadAdvAsync(-hfRmbNo.Value)

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
                lblStatus.Text = Translate(RmbStatus.StatusName(theRmb.First.Status))
                If cbMoreInfo.Checked Then
                    Dim theUser = UserController.GetUserById(PortalId, theRmb.First.UserId)
                    SendMessage(Translate("MoreInfoMsg"), "window.open('mailto:" & theUser.Email & "?subject=Reimbursment " & theRmb.First.RID & ": More info requested');")
                    lblStatus.Text = lblStatus.Text & " - " & Translate("StatusMoreInfo")
                End If
                saveIfNecessary()
            End If
        End Sub

        Protected Async Sub cbApprMoreInfo_CheckedChanged(sender As Object, e As System.EventArgs) Handles cbApprMoreInfo.CheckedChanged
            cbMoreInfo.Checked = cbApprMoreInfo.Checked
            Dim theRmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value) And c.PortalId = PortalId
            If theRmb.Count > 0 Then
                lblStatus.Text = Translate(RmbStatus.StatusName(theRmb.First.Status))
                If cbApprMoreInfo.Checked Then
                    Dim theUser = UserController.GetUserById(PortalId, theRmb.First.UserId)
                    SendMessage(Translate("MoreInfoMsg"), "window.open('mailto:" & theUser.Email & "?subject=Reimbursment " & theRmb.First.RID & ": More info requested');")
                    lblStatus.Text = lblStatus.Text & " - " & Translate("StatusMoreInfo")
                End If
                saveIfNecessary()
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
            Dim currentRmbs = From c In d.AP_Staff_RmbLines Where c.AP_Staff_Rmb.PortalId = PortalId And c.AP_Staff_Rmb.Status = RmbStatus.Processing 'And c.AP_Staff_Rmb.ProcDate > Today.AddDays(-15) And c.Department = False

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
            Try
                submittedNode.Expanded = False
                submittedNode.SelectAction = TreeNodeSelectAction.Expand
                getAlphabeticNode(AllStaffSubmittedNode, letter).ChildNodes.Add(submittedNode)
                For Each row In queryResult '--get details of each reimbursement or advance
                    Dim newNode As New TreeNode()
                    newNode.SelectAction = TreeNodeSelectAction.Select
                    If (type.Equals("rmb")) Then
                        'Dim rmbUser = UserController.GetUserById(PortalId, row.UserId).DisplayName
                        newNode.Text = GetRmbTitleTeamShort(row.RID, row.RmbDate, row.Total)
                        newNode.Value = row.RMBNo
                    Else
                        Dim advUser = UserController.GetUserById(PortalId, row.UserId).DisplayName
                        newNode.Text = GetAdvTitleTeamShort(row.LocalAdvanceId, row.RequestDate, advUser)
                        newNode.Value = row.AdvanceId
                    End If
                    submittedNode.ChildNodes.Add(newNode)

                    If IsSelected(row.RMBNo) Then
                        submittedNode.Expanded = True
                        submittedNode.Parent.Expanded = True
                        submittedNode.Parent.Parent.Expanded = True
                    End If
                Next
            Catch ex As Exception
                Throw New Exception("Error adding items to tree: " + ex.Message)
            End Try
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

        Private Async Function getAccountBalanceAsync(account As String, logon As String) As Task(Of String)
            If account = "" Then
                Return BALANCE_INCONCLUSIVE
            End If
            Try
                Dim accountBalance = Await StaffRmbFunctions.getAccountBalanceAsync(account, logon)
                If accountBalance.Length = 0 Then
                    Return BALANCE_PERMISSION_DENIED
                End If
                Double.Parse(accountBalance)
                Return accountBalance
            Catch
                Return BALANCE_INCONCLUSIVE
            End Try
        End Function

        Private Async Function getBudgetBalanceAsync(account As String, logon As String) As Task(Of String)
            If account = "" Then
                Return BALANCE_INCONCLUSIVE
            End If
            Dim budgetBalance = Await StaffRmbFunctions.getBudgetBalanceAsync(account, logon)
            If budgetBalance.Length = 0 Then
                Return BALANCE_PERMISSION_DENIED
            End If
            Try
                Double.Parse(budgetBalance)
            Catch
                Return BALANCE_INCONCLUSIVE
            End Try
            Return budgetBalance
        End Function

        Private Sub checkLowBalance()
            If (lblStatus.Text = RmbStatus.StatusName(RmbStatus.Submitted) AndAlso isLowBalance()) Then
                Dim accountBalance = hfAccountBalance.Value - GetTotal(hfRmbNo.Value)
                lblWarningLabel.Text = Translate("WarningLowBalance").Replace("[ACCTBAL]", Format(accountBalance, "Currency")) _
                .Replace("[BUDGBAL]", Format(hfBudgetBalance.Value, "Currency")).Replace("[ACCT]", tbChargeTo.Text)
                Dim t As Type = Me.GetType()
                ScriptManager.RegisterClientScriptBlock(Page, t, "", "showWarningDialog();", True)
            End If
        End Sub

        Private Function isLowBalance() As Boolean
            If isStaffAccount() Or (lblBudgetBalance.Text = "-------") Or (lblAccBal.Text = "-------") Then
                Return False
            End If
            Try
                Dim budgetBalance = hfBudgetBalance.Value
                Dim accountBalance = hfAccountBalance.Value - GetTotal(hfRmbNo.Value)
                Dim budgetTolerance = Settings("BudgetTolerance") / 100
                Dim lowestAllowedBalance = budgetBalance - (budgetBalance * budgetTolerance)
                If (accountBalance < lowestAllowedBalance) Then
                    Return True
                End If
            Catch
            End Try
            Return False
        End Function

        Private Sub updateBalanceLabels(accountBalance As String, budgetBalance As String)
            lblAccountBalance.Text = accountBalance
            lblBudgetBalance.Text = budgetBalance
            valueOrNull(hfAccountBalance, accountBalance)
            valueOrNull(hfBudgetBalance, budgetBalance)
            lblAccountBalance.Attributes.Add("class", redIfNegative(hfAccountBalance.Value))
            lblBudgetBalance.Attributes.Add("class", redIfNegative(hfBudgetBalance.Value))
        End Sub

        Private Sub valueOrNull(hf As HiddenField, s As String)
            Try
                hf.Value = Double.Parse(s)
            Catch ex As Exception
                hf.Value = ""
            End Try
        End Sub

        Private Function redIfNegative(amountString As String) As String
            Try
                Dim amount = Double.Parse(amountString)
                If (amount < 0) Then
                    Return "NormalRed"
                End If
            Catch
            End Try
            Return ""
        End Function

        Private Async Function LoadAddressAsync() As Task
            Dim User = StaffBrokerFunctions.GetStaffMember(UserId)
            tbAddressLine1.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "Address1")
            tbAddressLine2.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "Address2")
            tbCity.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "City")
            tbProvince.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "Province")
            tbCountry.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "Country")
            tbPostalCode.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "PostalCode")
        End Function

        Private Function addAddressToComments() As Boolean
            Dim User = StaffBrokerFunctions.GetStaffMember(UserId)
            Dim Address1 = StaffBrokerFunctions.GetStaffProfileProperty(User, "Address1")
            Dim Address2 = StaffBrokerFunctions.GetStaffProfileProperty(User, "Address2")
            Dim City = StaffBrokerFunctions.GetStaffProfileProperty(User, "City")
            Dim Province = StaffBrokerFunctions.GetStaffProfileProperty(User, "Province")
            Dim Country = StaffBrokerFunctions.GetStaffProfileProperty(User, "Country")
            Dim PC = StaffBrokerFunctions.GetStaffProfileProperty(User, "PostalCode")

            If (tbAddressLine1.Text = Address1) _
                And (tbAddressLine2.Text = Address2) _
                And (tbCity.Text = City) _
                And (tbProvince.Text = Province) _
                And (tbCountry.Text = Country) _
                And (tbPostalCode.Text = PC) Then
                Return False
            End If
            Dim new_address = tbAddressLine1.Text & Environment.NewLine & tbAddressLine2.Text & Environment.NewLine & tbCity.Text _
                              & Environment.NewLine & tbProvince.Text & Environment.NewLine & tbCountry.Text & Environment.NewLine & tbPostalCode.Text & Environment.NewLine
            '(remove blank lines)
            new_address = new_address.Replace(Environment.NewLine & Environment.NewLine, Environment.NewLine)
            tbComments.Text += Environment.NewLine & "**Use the following address:" & Environment.NewLine & new_address
            Return True
        End Function

        Private Function hasOldExpenses() As Boolean
            Dim oldest_allowable_date = Today.AddDays(-Settings("Expire"))
            Dim old_lines = (From c In d.AP_Staff_RmbLines Where c.RmbNo = hfRmbNo.Value And c.TransDate < oldest_allowable_date Select c.TransDate).Count()
            Return old_lines > 0
        End Function

    End Class
End Namespace
