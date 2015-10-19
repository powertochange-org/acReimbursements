' AP_Rmb
'--------
' SpareField1: total for the reimbursement
' SpareField2: Submitter's Director's id - in case of special approval
' SpareField3: DelegateId, when filling out a form on behalf of someone else
' SpareField4:
' SpareField5: 

' AP_Rmb_Line
'-------------
' Spare1: Province
' Spare2: PerDiem meals, Advance UnclearedAmount
' Spare3: Mileage unit index
' Spare4: Mileage origin, ClearingAdvance RmbNo
' Spare5: Mileage destination, ClearingAdvance RmbLineNo

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
        Dim BALANCE_INCONCLUSIVE As String = "unknown"
        Dim BALANCE_PERMISSION_DENIED As String = "**hidden**"
        Dim EDMS_APPROVAL_LOG_MESSAGE As String = "APPROVED by EDMS"
        Dim CLEARED As String = "CLEARED"
        Dim LOG_LEVEL_DEBUG As Integer = 1
        Dim LOG_LEVEL_INFO As Integer = 2
        Dim LOG_LEVEL_WARNING As Integer = 3
        Dim LOG_LEVEL_ERROR As Integer = 4
        Dim LOG_LEVEL_CRITICAL As Integer = 5

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
            ' set versioning info
            lblVersion.Visible = UserController.Instance.GetCurrentUserInfo().IsSuperUser
            lblVersion.Text = String.Format("Version: {0}<br>Dated: {1}",
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                System.IO.File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location).ToShortDateString())

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
            StaffRmbFunctions.setPresidentId(CInt(Settings("PresidentId")))
        End Sub

        Private Async Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Me.Init

            lblError.Visible = False

            Dim TaskList As New List(Of Task)

            If Not String.IsNullOrEmpty(Settings("NoReceipt")) Then
                hfNoReceiptLimit.Value = Settings("NoReceipt")
            End If

            If Page.IsPostBack Then

            Else
                TaskList.Add(LoadCompaniesAsync())
                TaskList.Add(LoadMenuAsync())

                'Initialize US exchange rate from settings
                Dim rateString As String = Settings("USExchangeRate")
                Dim rate As Double = 0
                If (rateString IsNot Nothing) Then
                    Try
                        rate = CType(rateString, Double)
                    Catch ex As Exception
                        rate = 0
                    End Try
                End If
                StaffBrokerFunctions.setUsExchangeRate(rate)

                If Request.QueryString("RmbNo") <> "" Then
                    hfRmbNo.Value = CInt(Request.QueryString("RmbNo"))
                ElseIf Request.QueryString("RmbID") <> "" Then
                    Dim rmbs = From c In d.AP_Staff_Rmbs Where c.RID = Request.QueryString("RmbID")
                    If (rmbs IsNot Nothing) Then
                        hfRmbNo.Value = rmbs.First.RMBNo
                    End If
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
                GridView1.Columns(1).HeaderText = Translate("Extra")
                GridView1.Columns(2).HeaderText = Translate("LineType")
                GridView1.Columns(3).HeaderText = Translate("Comment")
                GridView1.Columns(4).HeaderText = Translate("Amount")
                GridView1.Columns(5).HeaderText = Translate("ReceiptNo")

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
                    TaskList.Add(LoadRmbAsync(hfRmbNo.Value))
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
                Dim userstr = CStr(UserId)
                MoreInfo = From c In d.AP_Staff_Rmbs
                                    Where c.MoreInfoRequested = True And c.Status <> RmbStatus.Processing And c.Status <> RmbStatus.Cancelled And ((c.UserId = UserId) Or c.SpareField3.Equals(userstr)) And c.PortalId = PortalId
                                    Select c.UserRef, c.RID, c.RMBNo
                For Each row In MoreInfo
                    Dim hyp As New HyperLink()
                    hyp.CssClass = "ui-state-highlight ui-corner-all AgapeWarning"
                    hyp.Attributes.Add("style", "padding:8px; margin-top:2px;")
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
                Dim userstr = CStr(UserId)
                Dim Pending = (From c In d.AP_Staff_Rmbs
                               Where c.Status = RmbStatus.Draft And c.PortalId = PortalId And ((c.UserId = UserId) Or c.SpareField3.Equals(userstr))
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
            'This panel lists reimbursements in the Submitted, PendingDirectorApproval, or PendingEDMSApproval statuses
            Try
                Dim userstr = CStr(UserId)
                Dim Submitted = (From c In d.AP_Staff_Rmbs
                                 Where (c.Status = RmbStatus.Submitted Or c.Status = RmbStatus.PendingDirectorApproval Or c.Status = RmbStatus.PendingEDMSApproval) And ((c.UserId = UserId) Or c.SpareField3.Equals(userstr)) And c.PortalId = PortalId
                                 Order By c.RID Descending
                                 Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(Settings("MenuSize"))
                dlSubmitted.DataSource = Submitted
                dlSubmitted.DataBind()

                Dim submitted_count = Submitted.Count
                SubmittedUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error loading submitted rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicApprovablePaneAsync() As Task
            '--list any unapproved reimbursements submitted to this user for approval
            '--Status=Submitted, and ApprUserId = UserId
            '--OR Status=PendingDirectorApproval, SpareField2 = UserId
            '--OR Status=PendingEDMSApproval, and user is EDMS
            '--**NOTE: Director's UserId is stored in rmb.SpareField2 during form submission if director's approval is necessary
            Dim userIsEDMS = (UserId = CType(Settings("EDMSId"), Integer))
            Try
                Dim ApprovableRmbs = (From c In d.AP_Staff_Rmbs
                            Where ((c.Status = RmbStatus.Submitted And c.ApprUserId = UserId) Or (c.Status = RmbStatus.PendingDirectorApproval And c.SpareField2 IsNot Nothing AndAlso c.SpareField2 = UserId)) And c.ApprDate Is Nothing And c.PortalId = PortalId
                            Order By c.RMBNo Descending
                            Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId)
                If userIsEDMS Then
                    ApprovableRmbs = ApprovableRmbs.Union(From c In d.AP_Staff_Rmbs
                                          Where c.Status = RmbStatus.PendingEDMSApproval And c.ApprDate Is Nothing And c.PortalId = PortalId
                                          Order By c.RMBNo Descending
                                          Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId)
                End If
                dlToApprove.DataSource = ApprovableRmbs
                dlToApprove.DataBind()

                Dim approvable_count = ApprovableRmbs.Count
                Dim isApprover = (approvable_count > 0)

                '-- Add a count of approvable items to the 'Submitted' heading
                If approvable_count > 0 Then
                    lblSubmitted.Visible = True
                    lblSubmittedCount.Text = "(" & approvable_count & ")"
                    pnlSubmitted.Attributes.Add("class", "ui-state-highlight ui-corner-all")
                Else
                    lblSubmittedCount.Text = ""
                    pnlSubmitted.Attributes.Remove("class")
                End If

                lblApproveHeading.Visible = isApprover
                SubmittedUpdatePanel.Update()
                pnlSubmitted.Update()
            Catch ex As Exception
                Throw New Exception("Error loading approvable rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicApprovedPaneAsync() As Task
            Try
                Dim userstr = CStr(UserId)
                Dim Approved = (From c In d.AP_Staff_Rmbs
                                Where (c.Status = RmbStatus.Approved Or c.Status = RmbStatus.PendingDownload Or c.Status = RmbStatus.DownloadFailed) _
                                    And ((c.UserId = UserId) Or c.SpareField3.Equals(userstr)) And c.PortalId = PortalId
                                Order By c.RID Descending
                                Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(Settings("MenuSize"))
                dlApproved.DataSource = Approved
                dlApproved.DataBind()
                ApprovedUpdatePanel.Update()
                pnlToProcess.Update()
            Catch ex As Exception
                Throw New Exception("Error loading approved rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicProcessingPaneAsync() As Task
            Try
                Dim userstr = CStr(UserId)
                Dim Complete = (From c In d.AP_Staff_Rmbs
                                Where c.Status = RmbStatus.Processing And ((c.UserId = UserId) Or c.SpareField3.Equals(userstr)) And c.PortalId = PortalId
                                Order By c.RID Descending
                                Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId).Take(Settings("MenuSize"))
                dlProcessing.DataSource = Complete
                dlProcessing.DataBind()
                ProcessingUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error loading processing rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicPaidPaneAsync() As Task
            Try
                Dim userstr = CStr(UserId)
                Dim Complete = (From c In d.AP_Staff_Rmbs
                                Where c.Status = RmbStatus.Paid And ((c.UserId = UserId) Or c.SpareField3.Equals(userstr)) And c.PortalId = PortalId
                                Order By c.RID Descending
                                Select c.RMBNo, c.RmbDate, c.UserRef, c.RID, c.UserId)
                dlPaid.DataSource = Complete
                dlPaid.DataBind()
                PaidUpdatePanel.Update()
            Catch ex As Exception
                Throw New Exception("Error loading paid rmbs: " + ex.Message)
            End Try
        End Function

        Private Async Function loadBasicCancelledTaskAsync() As Task
            Try
                Dim userstr = CStr(UserId)
                Dim days = 30
                Dim expiryDate = Today - New TimeSpan(days, 0, 0, 0) 'only show deleted items for 30 days
                Dim Cancelled = (From c In d.AP_Staff_Rmbs
                                 Where c.Status = RmbStatus.Cancelled And ((c.UserId = UserId) Or c.SpareField3.Equals(userstr)) And ((c.RmbDate Is Nothing) Or (c.RmbDate > expiryDate)) And c.PortalId = PortalId
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
                    Dim TeamApproved = From c In d.AP_Staff_Rmbs
                                       Where c.UserId = team_member.UserID _
                                            And (c.Status = RmbStatus.Approved Or c.Status = RmbStatus.PendingDownload Or c.Status = RmbStatus.DownloadFailed) _
                                            And c.UserId <> UserId And c.PortalId = PortalId
                                       Select c.RMBNo, c.RmbDate, c.RID, c.SpareField1
                    If (TeamApproved.Count > 0) Then
                        Dim TeamMemberApprovedNode As New TreeNode(team_member.LastName & ", " & team_member.FirstName)
                        TeamMemberApprovedNode.SelectAction = TreeNodeSelectAction.Expand
                        TeamMemberApprovedNode.Expanded = False
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

                        TeamApprovedNode.ChildNodes.Add(TeamMemberApprovedNode)
                    End If
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
                    Dim TeamProcessing = From c In d.AP_Staff_Rmbs
                                        Join b In d.AP_StaffBroker_CostCenters
                                            On c.CostCenter Equals b.CostCentreCode _
                                                And c.PortalId Equals b.PortalId
                                        Where c.UserId = team_member.UserID And c.Status = RmbStatus.Processing And c.PortalId = PortalId
                                        Select c.RMBNo, c.RmbDate, c.RID, c.SpareField1

                    If (TeamProcessing.Count > 0) Then
                        Dim TeamMemberProcessingNode As New TreeNode(team_member.LastName & ", " & team_member.FirstName)
                        TeamMemberProcessingNode.Expanded = False
                        TeamMemberProcessingNode.SelectAction = TreeNodeSelectAction.Expand

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

                        TeamProcessingNode.ChildNodes.Add(TeamMemberProcessingNode)
                    End If
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
                    Dim TeamPaid = From c In d.AP_Staff_Rmbs
                                        Join b In d.AP_StaffBroker_CostCenters
                                            On c.CostCenter Equals b.CostCentreCode _
                                                And c.PortalId Equals b.PortalId
                                        Where c.UserId = team_member.UserID And c.Status = RmbStatus.Paid And c.PortalId = PortalId
                                        Select c.RMBNo, c.RmbDate, c.RID, c.SpareField1
                    If (TeamPaid.Count > 0) Then
                        Dim TeamMemberPaidNode As New TreeNode(team_member.LastName & ", " & team_member.FirstName)
                        TeamMemberPaidNode.Expanded = False
                        TeamMemberPaidNode.SelectAction = TreeNodeSelectAction.Expand

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

                        TeamPaidNode.ChildNodes.Add(TeamMemberPaidNode)
                    End If
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
            control.Attributes.Add("id", id)
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
                                   Select c.RMBNo, c.CostCenter, c.RmbDate, c.ApprDate, c.UserRef, c.RID, c.UserId, c.Status, c.SpareField1, c.MoreInfoRequested, _
                                       Receipts = ((c.AP_Staff_RmbLines.Where(Function(x) x.Receipt And ((From f In d.AP_Staff_RmbLine_Files Where f.RmbLineNo = x.RmbLineNo).Count = 0))).Count > 0))
                Dim total = AllApproved.Count

                ' NOTE: GAiN cost centers all start with '69' by convention
                Dim PTCreceiptsTask = buildRmbTreeAsync(Translate("PTCReceipts"), finance_node, From c In AllApproved Where c.Status = RmbStatus.Approved And c.Receipts And Not c.CostCenter.StartsWith("69"))
                Dim PTCno_receiptsTask = buildRmbTreeAsync(Translate("PTCNoReceipts"), finance_node, From c In AllApproved Where c.Status = RmbStatus.Approved And Not c.Receipts And Not c.CostCenter.StartsWith("69"))
                Dim GAiNreceiptsTask = buildRmbTreeAsync(Translate("GAiNReceipts"), finance_node, From c In AllApproved Where c.Status = RmbStatus.Approved And c.Receipts And c.CostCenter.StartsWith("69"))
                Dim GAiNno_receiptsTask = buildRmbTreeAsync(Translate("GAiNNoReceipts"), finance_node, From c In AllApproved Where c.Status = RmbStatus.Approved And Not c.Receipts And c.CostCenter.StartsWith("69"))
                Dim pendingImportTask = buildRmbTreeAsync(Translate("PendingImport"), finance_node, From c In AllApproved Where c.Status >= RmbStatus.PendingDownload)

                Dim PTCreceipts_node = Await PTCreceiptsTask
                Dim PTCno_receipts_node = Await PTCno_receiptsTask
                Dim GAiNreceipts_node = Await GAiNreceiptsTask
                Dim GAiNno_receipts_node = Await GAiNno_receiptsTask
                Dim pending_import_node = Await pendingImportTask

                finance_node.ChildNodes.Add(PTCreceipts_node)
                finance_node.ChildNodes.Add(PTCno_receipts_node)
                finance_node.ChildNodes.Add(GAiNreceipts_node)
                finance_node.ChildNodes.Add(GAiNno_receipts_node)
                finance_node.ChildNodes.Add(pending_import_node)

                tvFinance.Nodes.Clear()
                tvFinance.Nodes.Add(finance_node)
                tvFinance.Visible = IsAccounts()

                '-- Add a count of items to the 'Approved' heading
                If total > 0 Then
                    lblToProcess.Text = "(" & total & ")"
                    pnlToProcess.Attributes.Add("class", "ui-state-highlight ui-corner-all")
                Else
                    lblToProcess.Text = ""
                    pnlToProcess.Attributes.Remove("class")
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
                    Dim flag As Boolean = (rmb.MoreInfoRequested IsNot Nothing AndAlso rmb.MoreInfoRequested)
                    Dim owner = UserController.GetUserById(PortalId, rmb.UserId)
                    Dim initials = (getInitials(owner)).ToLower()
                    rmb_node.Text = GetRmbTitleFinance(initials, rmb.RID, rmb.ApprDate, rmbTotal, flag)
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
                If (ddlCompany.Items.Count = 0) Then
                    Await LoadCompaniesAsync()
                    If (ddlCompany.Items.Count = 0) Then
                        lblErrorMessage.Text = Translate("ErrorCompanies")
                        pnlError.Visible = IsAccounts()
                        Return
                    End If
                End If
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
                Dim ownerName As String
                Try
                    Dim ownerID = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.UserId).First()
                    ownerName = UserController.GetUserById(PortalId, ownerID).DisplayName
                Catch ex As Exception
                    ownerName = ""
                End Try
                Dim initials = getInitials(user)
                ddlCompany.SelectedIndex = -1
                dtPostingDate.Text = Today.ToString("MM/dd/yyyy")
                Dim batchIds = From c In d.AP_Staff_Rmb_Post_Extras Where c.BatchId.Substring(6, 2).Equals(initials) Order By c.PostingDate Descending Select c.BatchId
                If (batchIds.Count() > 0) Then
                    tbBatchId.Text = batchIds.First()
                End If

                Dim linetypes = (From t In d.AP_Staff_RmbLines Where t.RmbNo = hfRmbNo.Value And t.GrossAmount > 0 Select t.LineType).Distinct()
                Dim isAdvanceRequest = linetypes.Count() = 1 And linetypes.Contains(Settings("AdvanceLineType"))
                If (isAdvanceRequest) Then
                    tbPostingReference.Text = "Adv-" & ownerName
                    tbInvoiceNumber.Text = "ADV" & lblRmbNo.Text
                Else
                    tbPostingReference.Text = "Reimb-" & ownerName
                    tbInvoiceNumber.Text = "REIMB" & lblRmbNo.Text
                End If
                tbVendorId.Text = ""
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

                    Dim getAccountBalanceTask = getAccountBalanceAsync(Rmb.CostCenter, StaffRmbFunctions.logonFromId(PortalId, UserId))

                    Dim DRAFT = Rmb.Status = RmbStatus.Draft
                    Dim MORE_INFO = (Rmb.MoreInfoRequested IsNot Nothing AndAlso Rmb.MoreInfoRequested = True)
                    Dim SUBMITTED = Rmb.Status = RmbStatus.Submitted Or Rmb.Status = RmbStatus.PendingDirectorApproval Or Rmb.Status = RmbStatus.PendingEDMSApproval
                    Dim APPROVED = Rmb.Status = RmbStatus.Approved
                    Dim PROCESSING = Rmb.Status = RmbStatus.PendingDownload Or Rmb.Status = RmbStatus.DownloadFailed Or Rmb.Status = RmbStatus.Processing
                    Dim PAID = Rmb.Status = RmbStatus.Paid
                    Dim CANCELLED = Rmb.Status = RmbStatus.Cancelled
                    Dim FORM_HAS_ITEMS = Rmb.AP_Staff_RmbLines.Count > 0

                    Dim user = UserController.GetUserById(PortalId, Rmb.UserId)
                    Dim delegateId As Integer
                    Dim directorId As Integer
                    Dim EDMSId As Integer
                    Try
                        If Rmb.SpareField3 IsNot Nothing Then
                            delegateId = CInt(Rmb.SpareField3)
                        Else
                            delegateId = -1
                        End If
                    Catch
                        delegateId = -1
                    End Try
                    Try
                        directorId = CInt(Rmb.SpareField2)
                    Catch ex As Exception
                        directorId = -1
                    End Try
                    Try
                        EDMSId = CInt(Settings("EDMSId"))
                    Catch ex As Exception
                        EDMSId = -1
                    End Try

                    Dim hasDelegate = (delegateId >= 0)
                    Dim delegateName = If(hasDelegate, UserController.GetUserById(PortalId, delegateId).DisplayName, "")
                    Dim staff_member = StaffBrokerFunctions.GetStaffMember(Rmb.UserId)

                    Dim isDelegate = (UserId = delegateId)
                    Dim isOwner = (UserId = Rmb.UserId) Or isDelegate
                    Dim isSpouse = (StaffBrokerFunctions.GetSpouseId(UserId) = Rmb.UserId)
                    Dim isApprover = Rmb.ApprUserId IsNot Nothing AndAlso ((UserId = Rmb.ApprUserId) And Not (isOwner Or isSpouse)) _
                                    Or ((Rmb.Status = RmbStatus.PendingDirectorApproval) And (UserId = directorId)) _
                                    Or ((Rmb.Status = RmbStatus.PendingEDMSApproval) And (UserId = EDMSId))
                    Dim isFinance = IsAccounts() And Not (isOwner Or isSpouse Or isApprover) And Not DRAFT
                    Dim isAdmin = UserController.Instance.GetCurrentUserInfo.IsInRole("Administrators")
                    Dim hasHadExtraApproval = (From c In d.AP_Staff_Rmb_Logs Where c.RID = Rmb.RID And c.Message.Equals(EDMS_APPROVAL_LOG_MESSAGE)).Count() > 0

                    '--Ensure the user is authorized to view this reimbursement
                    Dim RmbRel As Integer
                    RmbRel = StaffRmbFunctions.Authenticate(UserId, RmbNo, PortalId)
                    If RmbRel = RmbAccess.Denied And Not (isApprover Or isFinance Or isDelegate) Then
                        ScriptManager.RegisterClientScriptBlock(Page, Page.GetType(), "accessDenied", "alert('" + Translate("AccessDenied") + "');", True)
                        pnlMain.Visible = False
                        ltSplash.Text = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("RmbSplash", PortalId))
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
                    If isAdmin Then lblRmbNo.ToolTip = Rmb.RMBNo
                    Dim resetPostingDataTask = ResetPostingDataAsync()
                    imgAvatar.ImageUrl = GetProfileImage(Rmb.UserId)
                    staffInitials.Value = getInitials(user)
                    tbChargeTo.Text = If(Rmb.CostCenter Is Nothing, "", Rmb.CostCenter)
                    tbChargeTo.Enabled = DRAFT Or MORE_INFO Or CANCELLED Or (SUBMITTED And (isOwner Or isSpouse))
                    tbChargeTo.Attributes.Add("placeholder", Translate("tbChargeToHint"))
                    Dim accountName = (From c In d.AP_StaffBroker_CostCenters Where c.CostCentreCode = Rmb.CostCenter Select c.CostCentreName).SingleOrDefault()
                    tbChargeTo.Attributes.Add("title", accountName)
                    tbChargeTo.CssClass = If(isFinance, "finance", "")
                    lblStatus.Text = Translate(RmbStatus.StatusName(Rmb.Status))
                    If (Rmb.MoreInfoRequested) Then
                        lblStatus.Text = lblStatus.Text & " - " & Translate("StatusMoreInfo")
                    End If

                    '*** FORM HEADER ***
                    '--dates
                    lblSubmittedDate.Text = If(Rmb.RmbDate Is Nothing, "", Rmb.RmbDate.Value.ToShortDateString)
                    lblSubBy.Text = If(hasDelegate, delegateName, user.DisplayName)
                    lblBehalf.Text = If(hasDelegate, user.DisplayName, "")
                    lblOnBehalfOf.Visible = (hasDelegate)
                    lblBehalf.Visible = (hasDelegate)
                    If (isFinance) Then
                        lblBehalf.CssClass = "highlight"
                    End If
                    Dim loadAddressTask = LoadAddressAsync(Rmb.UserId)

                    lblApprovedDate.Text = If(Rmb.ApprDate Is Nothing, "", Rmb.ApprDate.Value.ToShortDateString)
                    ttlWaitingApp.Visible = Rmb.ApprDate Is Nothing
                    ttlApprovedBy.Visible = Not Rmb.ApprDate Is Nothing
                    ddlApprovedBy.Visible = DRAFT Or CANCELLED Or ((isApprover Or isOwner Or isSpouse) And SUBMITTED)
                    ddlApprovedBy.Enabled = DRAFT Or CANCELLED Or ((isApprover Or isOwner Or isSpouse) And SUBMITTED)
                    lblApprovedBy.Visible = Not ddlApprovedBy.Visible
                    lblExtraApproval.Visible = hasHadExtraApproval
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
                    lblYouRef.Visible = Not IsAccounts()
                    tbYouRef.Visible = Not IsAccounts()
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
                    tbAccComments.Enabled = isFinance
                    tbAccComments.Text = If(Rmb.AcctComment Is Nothing, "", Rmb.AcctComment)

                    pnlPrivateComments.Visible = isFinance
                    tbPrivAccComments.Text = Rmb.PrivComment
                    btnPrivAccComments.Visible = (Not tbPrivAccComments.Text.Trim().Length > 0)
                    lblPrivAccComments.Visible = (tbPrivAccComments.Text.Trim().Length > 0)
                    tbPrivAccComments.Visible = (tbPrivAccComments.Text.Trim().Length > 0)

                    cbMoreInfo.Visible = (isFinance And APPROVED)
                    cbMoreInfo.Checked = If(Rmb.MoreInfoRequested, Rmb.MoreInfoRequested, False)

                    '--buttons
                    btnSave.Text = Translate("btnSaved")
                    btnSave.Style.Add("display", "none") '--hide, but still generate the button
                    btnDelete.Visible = Not (PROCESSING Or PAID Or CANCELLED)
                    btnDelete.ToolTip = Translate("btnDeleteHelp")


                    '*** REIMBURSEMENT DETAILS ***

                    pnlTaxable.Visible = isFinance And (From c In Rmb.AP_Staff_RmbLines Where c.Taxable = True).Count > 0

                    '--grid
                    ViewState("SortOrder") = "RmbLineNo"
                    GridView1.DataSource = getExpenseLines()
                    GridView1.DataBind()

                    '--buttons
                    btnSaveLine.Visible = ((isOwner Or isSpouse) And Not (PROCESSING Or PAID Or APPROVED)) Or (APPROVED And isFinance)
                    addLinebtn2.Visible = (isOwner Or isSpouse) And Not (PROCESSING Or PAID Or APPROVED)

                    btnPrint.Visible = FORM_HAS_ITEMS
                    btnPrint.OnClientClick = "window.open('/DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & RmbNo & "&UID=" & Rmb.UserId & "', '_blank'); "
                    btnSubmit.Visible = (isOwner Or isSpouse) And (DRAFT Or MORE_INFO Or CANCELLED) And FORM_HAS_ITEMS
                    btnSubmit.Text = If(DRAFT, Translate("btnSubmit"), Translate("btnResubmit"))
                    enableSubmitButton(btnSubmit.Visible And tbChargeTo.Text.Length = 6 And ddlApprovedBy.SelectedIndex > 0 And GridView1.Rows.Count > 0)
                    btnReject.Visible = isApprover And SUBMITTED
                    enableRejectButton(isApprover And SUBMITTED And tbApprComments.Text <> "")
                    btnApprove.Visible = isApprover And SUBMITTED
                    btnApprove.Enabled = isApprover And SUBMITTED
                    btnProcess.Visible = isFinance And APPROVED
                    btnProcess.Enabled = isFinance And APPROVED
                    btnUnProcess.Visible = isFinance And (PROCESSING)
                    btnUnProcess.Enabled = isFinance And (PROCESSING)
                    btnDownload.Visible = (isFinance Or isOwner Or isSpouse) And FORM_HAS_ITEMS

                    tbCostcenter.Enabled = isFinance
                    ddlAccountCode.Enabled = isFinance
                    pnlAccountsOptions.Style.Add("display", If(isFinance, "block", "none"))

                    '--advances
                    Dim uncleared_advances = getUnclearedAdvances(Rmb.UserId)
                    Dim uncleared_amount As Double
                    Try
                        uncleared_amount = (From a In uncleared_advances Select Convert.ToDouble(a.Spare2)).Sum()
                    Catch
                        uncleared_amount = 0
                    End Try
                    pnlAdvance.Visible = (uncleared_amount > 0) And (((FORM_HAS_ITEMS) And (isOwner Or isSpouse) And (Not (PROCESSING Or PAID Or APPROVED))) Or (isFinance And APPROVED))
                    lblOutstandingAdvanceAmount.Text = uncleared_amount.ToString("C")
                    If (uncleared_amount > 0) Then
                        gvUnclearedAdvances.DataSource = (From c In uncleared_advances Order By c.TransDate)
                        gvUnclearedAdvances.DataBind()
                    End If
                    lblAdvanceClearError.Visible = False

                    updateBalanceLabel(Await getAccountBalanceTask)
                    If (isApprover) Then
                        checkLowBalance()
                    End If
                    ScriptManager.RegisterClientScriptBlock(Page, Page.GetType(), "calculate_remaining_balance", "calculate_remaining_balance()", True)
                    Await Task.WhenAll(loadAddressTask, updateApproverListTask, resetPostingDataTask)
                Else
                    pnlMain.Visible = False
                    ltSplash.Text = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("RmbSplash", PortalId))
                    pnlSplash.Visible = True
                End If

            Catch ex As Exception
                lblError.Text = "Error loading Rmb: " & ex.Message & ex.StackTrace
                lblError.Visible = True
            End Try

        End Function

        Private Async Function updateApproversListAsync(ByVal obj As Object) As Task
            Dim approvers As StaffRmbFunctions.Approvers
            Dim approverId = -1
            Try
                ddlApprovedBy.Items.Clear()
                approvers = Await StaffRmbFunctions.getApproversAsync(obj)
                approverId = obj.ApprUserId
                Dim blank As ListItem
                If (tbChargeTo.Text.Length = 0) Then
                    blank = New ListItem(Translate("ddlApprovedByNoAccountHint"), "-1")
                    ddlApprovedBy.Style.Add("color", "gray")
                Else
                    If (approvers.UserIds IsNot Nothing And approvers.UserIds.Count > 0) Then
                        blank = New ListItem("", "-1")
                        ddlApprovedBy.Style.Add("color", "black")
                    Else
                        Try
                            Dim account As Integer = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.UserId).Single()
                            If (Not StaffRmbFunctions.accountBelongsToStaffMember(tbChargeTo.Text, account)) Then
                                blank = New ListItem(Translate("ddlApprovedByInvalidAccountHint"), "-1")
                            Else
                                blank = New ListItem(Translate("ddlApprovedByNoApproversHint"), "-1")
                            End If
                        Catch
                            blank = New ListItem(Translate("ddlApprovedByNoApproversHint"), "-1")
                        End Try
                        ddlApprovedBy.Style.Add("color", "gray")
                    End If
                End If
                blank.Attributes.Add("disabled", "disabled")
                blank.Attributes.Add("selected", "selected")
                blank.Attributes.Add("style", "visibility:hidden") 'hide in dropdown list (display:none doesn't work in firefox)
                ddlApprovedBy.Items.Add(blank)
                If (approvers.UserIds IsNot Nothing) Then
                    For Each row In approvers.UserIds
                        If Not row Is Nothing Then
                            ddlApprovedBy.Items.Add(New ListItem(row.DisplayName, row.UserID))
                        End If
                    Next
                End If
            Catch ex As Exception
                lblErrorMessage.Text = Translate("UpdateApproversError")
                pnlError.Visible = True
                Log(lblRmbNo.Text, LOG_LEVEL_ERROR, "ERROR: updating approvers list. " + ex.ToString)
            End Try
            Try
                ddlApprovedBy.SelectedValue = approverId
                btnSubmit.Visible = True
                enableSubmitButton(approverId >= 0)
                lblApprovedBy.Text = ddlApprovedBy.SelectedItem.ToString
            Catch ex As Exception
                ddlApprovedBy.SelectedValue = -1
                enableSubmitButton(False)
                lblApprovedBy.Text = "[NOBODY]"
            End Try
        End Function

        Private Sub enableSubmitButton(enable As Boolean)
            If (enable) Then btnSubmit.Visible = True
            btnSubmit.Enabled = True
            Dim classes = btnSubmit.Attributes("class")
            If enable Then
                btnSubmit.Attributes("class") = classes.Replace("aspNetDisabled", "")
                btnSubmit.ToolTip = ""
            Else
                btnSubmit.Attributes("class") = classes.Replace("aspNetDisabled", "") & " aspNetDisabled"
                btnSubmit.ToolTip = Translate("btnSubmitHelp")
            End If
        End Sub

        Private Sub enableRejectButton(enable As Boolean)
            btnReject.Enabled = True
            Dim classes = btnReject.Attributes("class").Replace("aspNetDisabled", "")
            btnReject.Attributes("class") = If(enable, classes, classes & " aspNetDisabled")
            btnReject.ToolTip = If(enable, "", Translate("btnRejectHelp"))
        End Sub
#End Region

#Region "Button Events"

        Protected Async Sub btnSaveLine_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSaveLine.Click

            'never allow changes to reimbursements after they have been processed
            Dim State As Integer = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.Status).First
            If State = RmbStatus.Paid Or State = RmbStatus.Processing Or State = RmbStatus.PendingDownload Or State = RmbStatus.DownloadFailed Then Return

            'set up
            Dim ucType As Type = theControl.GetType()
            Dim accounting_currency = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)
            Dim ownerId = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.UserId).First
            Dim LineTypeName = d.AP_Staff_RmbLineTypes.Where(Function(c) c.LineTypeId = CInt(ddlLineTypes.SelectedValue)).First.TypeName.ToString()
            Dim lineNo As Integer = Nothing
            Dim repeat As Integer = 1
            Dim insert As Boolean = True
            Dim success As Boolean = False
            Dim imageFiles As IQueryable(Of AP_Staff_RmbLine_File)
            Dim line As AP_Staff_RmbLine = Nothing
            If (btnSaveLine.CommandName = "Edit") Then
                lineNo = CInt(btnSaveLine.CommandArgument)
                Dim lines = From c In d.AP_Staff_RmbLines Where c.RmbLineNo = lineNo
                If (lines.Count > 0) Then line = lines.First
                imageFiles = (From lf In d.AP_Staff_RmbLine_Files Where lf.RMBNo = hfRmbNo.Value And lf.RmbLineNo = lineNo)
                insert = False
            ElseIf (btnSaveLine.CommandName = "Save") Then
                imageFiles = (From lf In d.AP_Staff_RmbLine_Files Where lf.RMBNo = hfRmbNo.Value And lf.RmbLineNo Is Nothing)
                If (ucType.GetProperty("Repeat") IsNot Nothing) Then
                    repeat = ucType.GetProperty("Repeat").GetValue(theControl, Nothing)
                End If
            End If

            Dim q = From c In d.AP_Staff_RmbLines Where c.RmbNo = hfRmbNo.Value And c.Receipt Select c.ReceiptNo
            Dim nextReceiptNo = If(q.Max Is Nothing, 1, q.Max + 1)
            Dim transactionDate = CDate(ucType.GetProperty("theDate").GetValue(theControl, Nothing))
            For I = 1 To repeat 'for controls that allow multiple insertions
                If (insert) Then line = New AP_Staff_RmbLine
                Dim age = DateDiff(DateInterval.Day, transactionDate, Today)

                If (line IsNot Nothing) Then
                    Try
                        If (ucType.GetProperty("ReceiptsAttached") IsNot Nothing) Then
                            ucType.GetProperty("ReceiptsAttached").SetValue(theControl, imageFiles.Count() > 0, Nothing)
                        End If
                    Catch
                    End Try
                    ' check validity
                    If ucType.GetMethod("ValidateForm").Invoke(theControl, New Object() {ownerId}) = True Then
                        If (insert) Then
                            line.RmbNo = hfRmbNo.Value
                            line.Split = False
                            ddlOverideTax.SelectedIndex = If(ucType.GetProperty("Taxable").GetValue(theControl, Nothing), 1, 0)
                        End If
                        ' Only recalculate OutOfDate if the date was changed
                        If (insert Or (transactionDate <> line.TransDate)) Then
                            line.TransDate = transactionDate
                            line.OutOfDate = (age > Settings("Expire"))
                        End If
                        line.AccountCode = ddlAccountCode.SelectedValue
                        line.CostCenter = tbCostcenter.Text
                        line.LineType = CInt(ddlLineTypes.SelectedValue)
                        line.Supplier = CStr(ucType.GetProperty("Supplier").GetValue(theControl, Nothing))
                        line.Comment = CStr(ucType.GetProperty("Comment").GetValue(theControl, Nothing))
                        Try
                            line.OrigCurrency = ucType.GetProperty("Currency").GetValue(theControl, Nothing)
                            line.ExchangeRate = ucType.GetProperty("ExchangeRate").GetValue(theControl, Nothing)
                        Catch
                        End Try
                        line.OrigCurrencyAmount = CDbl(ucType.GetProperty("Amount").GetValue(theControl, Nothing))
                        line.GrossAmount = CDbl(ucType.GetProperty("CADValue").GetValue(theControl, Nothing))
                        line.LargeTransaction = (line.GrossAmount >= Settings("TeamLeaderLimit"))
                        line.Taxable = (ddlOverideTax.SelectedIndex = 1)
                        line.Receipt = CBool(ucType.GetProperty("Receipt").GetValue(theControl, Nothing))
                        line.VATReceipt = CBool(ucType.GetProperty("VAT").GetValue(theControl, Nothing))
                        line.Spare1 = CStr(ucType.GetProperty("Spare1").GetValue(theControl, Nothing))
                        line.Spare2 = CStr(ucType.GetProperty("Spare2").GetValue(theControl, Nothing))
                        line.Spare3 = CStr(ucType.GetProperty("Spare3").GetValue(theControl, Nothing))
                        line.Spare4 = CStr(ucType.GetProperty("Spare4").GetValue(theControl, Nothing))
                        line.Spare5 = CStr(ucType.GetProperty("Spare5").GetValue(theControl, Nothing))
                        ' Mileage
                        Dim mileageString = ""
                        If (LineTypeName.Equals("Mileage")) Then
                            line.Mileage = CInt(ucType.GetProperty("Mileage").GetValue(theControl, Nothing))
                            line.MileageRate = ucType.GetProperty("MileageRate").GetValue(theControl, Nothing)
                            mileageString = GetMileageString(line.Mileage, line.MileageRate)
                        End If
                        ' Short Comment
                        If (insert Or (tbShortComment.Text <> line.ShortComment)) Then
                            line.ShortComment = GetLineComment(line.Comment, line.OrigCurrency, line.OrigCurrencyAmount, tbShortComment.Text, False, Nothing, mileageString)
                        Else
                            line.ShortComment = GetLineComment(line.Comment, line.OrigCurrency, line.OrigCurrencyAmount, "", False, Nothing, mileageString)
                        End If
                        ' Receipts
                        If (CInt(ucType.GetProperty("ReceiptType").GetValue(theControl, Nothing)) = RmbReceiptType.Electronic And imageFiles.Count > 0) Then
                            line.ReceiptNo = Nothing
                        Else
                            line.ReceiptNo = If(line.Receipt, nextReceiptNo, Nothing)
                            ' Since we aren't supposed to have any receipt images with this,
                            ' we should force-remove any receipts associated with this line
                            Dim imagesToRemove As New List(Of IFileInfo)
                            For Each imageFile As AP_Staff_RmbLine_File In imageFiles
                                imagesToRemove.Add(FileManager.Instance.GetFile(imageFile.FileId))
                            Next
                            FileManager.Instance.DeleteFiles(imagesToRemove)
                        End If
                        ' VAT
                        Try
                            If cbRecoverVat.Checked And CDbl(tbVatRate.Text) > 0 Then
                                line.VATRate = CDbl(tbVatRate.Text)
                            Else
                                line.VATRate = Nothing
                            End If
                        Catch ex As Exception
                            line.VATRate = Nothing
                        End Try
                        If (insert) Then
                            d.AP_Staff_RmbLines.InsertOnSubmit(line)
                            d.SubmitChanges()
                            ' Rename the receipt image files and references
                            For Each image In imageFiles
                                Dim file = FileManager.Instance.GetFile(image.FileId)
                                FileManager.Instance.RenameFile(file, "R" & line.RmbNo & "L" & line.RmbLineNo & "Rec" & image.RecNum & "." & file.Extension)
                                image.RmbLineNo = line.RmbLineNo.ToString
                            Next
                        End If
                        d.SubmitChanges()
                        success = True
                    Else ' The form was not valid
                        ReloadInvalidForm()
                        success = False
                    End If
                End If
                transactionDate = transactionDate.AddDays(1) 'add a day for each iteration of repeat
            Next
            ' Reset to 'save' mode
            If (success) Then
                btnSaveLine.CommandName = "Save"
                ' If these changes are being made by somebody other than the form owner or delegate
                ' then mark the form as having been changed
                Dim rmb = (From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value)).First
                If (UserId <> rmb.UserId And (rmb.SpareField3 Is Nothing Or UserId <> CInt(rmb.SpareField3))) Then
                    rmb.Changed = True
                    d.SubmitChanges()
                End If
                'Await LoadRmbAsync(hfRmbNo.Value)
                GridView1.DataSource = getExpenseLines()
                GridView1.DataBind()
                btnSubmit.Visible = True
                enableSubmitButton(btnSubmit.Visible And tbChargeTo.Text.Length = 6 And ddlApprovedBy.SelectedIndex > 0 And GridView1.Rows.Count > 0)
                ScriptManager.RegisterClientScriptBlock(Page, Me.GetType(), "hide_expense_popup", "closeNewItemPopup();", True)
            End If
        End Sub

        Protected Sub btnCancelLine_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCancelLine.Click
            Try
                Dim rmbs = (From r In d.AP_Staff_Rmbs Where r.RMBNo = hfRmbNo.Value)
                If (rmbs.Count > 0) Then
                    Dim path = "/_RmbReceipts/" & rmbs.First.UserId
                    Dim theFolder As IFolderInfo = FolderManager.Instance.GetFolder(rmbs.First.PortalId, path)
                    Dim deletable = From item In d.AP_Staff_RmbLine_Files Select item Where item.RMBNo = hfRmbNo.Value And item.RmbLineNo Is Nothing
                    For Each item In deletable
                        Dim theFile = FileManager.Instance.GetFile(item.FileId)
                        FileManager.Instance.DeleteFile(theFile)
                    Next
                End If
            Catch ex As Exception
                Log(lblRmbNo.Text, LOG_LEVEL_ERROR, "ERROR: removing receipts upon cancel: " & ex.Message)
            End Try
        End Sub

        Private Sub ReloadInvalidForm()
            ' Need to check the current state of electronic receipts and currency conversions 
            ' and set the attribute to match; otherwise, it will get reset to the default state. 
            'Dim currency = theControl.GetType().GetProperty("Currency").GetValue(theControl, Nothing)
            'If (currency <> StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)) Then
            '    Dim jscript = "$('.exchangeRate').val('" & hfExchangeRate.Value + "');"
            '    jscript &= "$('.equivalentCAD').val('" & hfOrigCurrencyValue.Value / hfExchangeRate.Value & "');"
            '    ScriptManager.RegisterClientScriptBlock(Page, Me.GetType(), "fixCurrency", jscript, True)
            'End If
            btnSaveLine.Enabled = True
            theControl.GetType().GetMethod("Initialize").Invoke(theControl, New Object() {Settings})
            If (theControl.GetType().GetProperty("ReceiptType") IsNot Nothing) Then
                If (CInt(theControl.GetType().GetProperty("ReceiptType").GetValue(theControl, Nothing) = RmbReceiptType.Electronic)) Then
                    ' If the receipt type is set to 2, we keep it visible
                    pnlElecReceipts.Attributes("style") = ""
                Else
                    ' Hide it
                    pnlElecReceipts.Attributes("style") = "display: none"
                End If
            End If
        End Sub

        Protected Async Sub btnAddClearingItem_click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnAddClearingItem.Click
            If validateClearingItem() Then
                For Each row In gvUnclearedAdvances.Rows
                    Dim rmblineno As Integer
                    Dim comment As String = ""
                    Dim outstanding As Double = 0
                    Dim payable As Double = 0
                    Try
                        Dim advance_balance = row.FindControl("lblAdvanceBalance").Text
                        outstanding = Double.Parse(advance_balance, NumberStyles.Currency)
                    Catch ex As Exception
                        outstanding = 0
                    End Try
                    Try
                        rmblineno = row.FindControl("hfRmbLineNo").Value
                        comment = row.FindControl("lblAdvanceComment").Text
                        Dim advance_clearing = row.FindControl("tbAdvanceClearing").Text
                        payable = Double.Parse(advance_clearing, NumberStyles.Currency)
                    Catch ex As Exception
                        lblAdvanceClearError.Text = Translate("ErrorClearAdvance") & ": " & ex.Message
                        lblAdvanceClearError.Visible = True
                        ScriptManager.RegisterClientScriptBlock(btnAddClearingItem, btnAddClearingItem.GetType(), "hide_loading_spinner", "updateClearingTotal(); $('#loading').hide();", True)
                        Return
                    End Try
                    'lookup original line item
                    Dim line As AP_Staff_RmbLine
                    Try
                        line = (From c In d.AP_Staff_RmbLines Where c.RmbLineNo = rmblineno).Single()
                    Catch ex As Exception
                        lblAdvanceClearError.Text = ex.Message
                        lblAdvanceClearError.Visible = True
                        ScriptManager.RegisterClientScriptBlock(btnAddClearingItem, btnAddClearingItem.GetType(), "hide_loading_spinner", "updateClearingTotal(); $('#loading').hide();", True)
                        Return
                    End Try
                    If (payable > 0) Then
                        'add line(s) to form
                        Dim insert As New AP_Staff_RmbLine()
                        insert.RmbNo = hfRmbNo.Value.ToString()
                        insert.LineType = line.LineType
                        insert.GrossAmount = 0 - payable
                        insert.OrigCurrency = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)
                        insert.OrigCurrencyAmount = 0 - payable
                        insert.TransDate = Today
                        insert.Comment = "Clear Advance:" & comment
                        insert.ShortComment = insert.Comment.Substring(0, 25)
                        insert.Taxable = False
                        insert.Receipt = False
                        insert.VATReceipt = False
                        insert.Split = False
                        insert.LargeTransaction = False
                        insert.OutOfDate = False
                        insert.Department = line.Department
                        insert.Spare2 = "0" 'Outstanding balance
                        insert.Spare4 = line.RmbNo.ToString() ' original reimbursement number
                        insert.Spare5 = line.RmbLineNo.ToString() ' original line number
                        Dim advancelinetype As Integer = Settings("AdvanceLineType")
                        insert.AccountCode = (From c In d.AP_StaffRmb_PortalLineTypes Where c.PortalId = PortalId And c.LineTypeId = advancelinetype Select c.PCode).Single
                        insert.CostCenter = line.CostCenter
                        insert.Supplier = ""
                        d.AP_Staff_RmbLines.InsertOnSubmit(insert)
                    End If
                Next
                d.SubmitChanges()
                Await LoadRmbAsync(hfRmbNo.Value)
                ScriptManager.RegisterClientScriptBlock(btnAddClearingItem, btnAddClearingItem.GetType(), "close_advance_clearing_popup", "closeClearAdvancePopup();", True)
            Else
                lblAdvanceClearError.Visible = True
                btnAddClearingItem.Enabled = True
                ScriptManager.RegisterClientScriptBlock(btnAddClearingItem, btnAddClearingItem.GetType(), "hide_loading_spinner", "updateClearingTotal(); $('#loading').hide();", True)
            End If
        End Sub

        Private Function validateClearingItem() As Boolean
            Dim clearingTotal As Double = 0
            For Each row In gvUnclearedAdvances.Rows
                Dim outstanding As Double = 0
                Dim payable As Double = 0
                Try
                    Dim advance_balance = row.FindControl("lblAdvanceBalance").Text
                    outstanding = Double.Parse(advance_balance, NumberStyles.Currency)
                Catch ex As Exception
                    outstanding = 0
                End Try
                Try
                    Dim advance_clearing = row.FindControl("tbAdvanceClearing").Text
                    payable = Double.Parse(advance_clearing, NumberStyles.Currency)
                Catch ex As Exception
                    lblAdvanceClearError.Text = Translate("ErrorClearAdvance") & ": " & ex.Message
                    Return False
                End Try
                clearingTotal += payable
                'ensure no amount is < 0 or > outstanding balance, unless it is Finance.
                If (payable < 0 Or payable > outstanding) Then
                    lblAdvanceClearError.Text = Translate("ErrorClearAdvanceAmount")
                    Return False
                End If
            Next
            'ensure total is < rmb total
            Dim rmbTotal = GetTotal(hfRmbNo.Value)
            If (clearingTotal > rmbTotal) And (Not IsAccounts()) Then
                lblAdvanceClearError.Text = Translate("ErrorClearAdvanceTotal").Replace("[TOTAL]", rmbTotal)
                Return False
            End If
            Return True
        End Function

        Protected Async Sub btnReject_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnReject.Click
            If btnReject.Attributes("class").Contains("aspNetDisabled") Then
                'show alert
                Dim jscript As String = "alert('" & Translate("btnRejectHelp") & "');"
                ScriptManager.RegisterClientScriptBlock(btnReject, btnReject.GetType(), "reject_alert", jscript, True)
                Return
            End If
            Dim RmbNo = hfRmbNo.Value
            Dim rmbs = From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo And ((c.Status = RmbStatus.Submitted) Or (c.Status = RmbStatus.PendingDirectorApproval) Or (c.Status = RmbStatus.PendingEDMSApproval))
            If (rmbs.Count > 0) Then
                Dim rmb = rmbs.First
                'Ensure only authorized person can reject a form
                If (rmb.Status = RmbStatus.Submitted) AndAlso (UserId <> rmb.ApprUserId) Then Return
                If (rmb.Status = RmbStatus.PendingDirectorApproval) AndAlso (UserId <> StaffBrokerFunctions.getDirectorFor(rmb.CostCenter, CType(Settings("EDMSId"), Integer))) Then Return
                If (rmb.Status = RmbStatus.PendingEDMSApproval) AndAlso (UserId <> CType(Settings("EDMSId"), Integer)) Then Return

                rmb.Status = RmbStatus.Draft
                rmb.MoreInfoRequested = True
                cbMoreInfo.Checked = True
                d.SubmitChanges()
                Dim taskList = New List(Of Task)
                taskList.Add(loadBasicDraftPaneAsync())
                taskList.Add(loadBasicApprovablePaneAsync())
                taskList.Add(LoadRmbAsync(RmbNo))
                SendRejectionLetter(rmb)
                Await Task.WhenAll(taskList)
                Dim Staffname As String = UserController.GetUserById(rmb.PortalId, rmb.UserId).DisplayName
                If (rmb.SpareField3 IsNot Nothing) Then
                    Try
                        Staffname = Staffname & " and " & UserController.GetUserById(rmb.PortalId, rmb.SpareField3).DisplayName
                    Catch ex As Exception
                        Staffname = "(" & Staffname & ")"
                    End Try
                End If
                Dim message As String = Translate("RejectorMessage").Replace("[STAFFNAME]", Staffname)
                ScriptManager.RegisterClientScriptBlock(btnReject, btnReject.GetType(), "notify_reject", "alert('" + message + "');", True)
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
            If (hfOnBehalfOf.Value.Equals(String.Empty) Or hfOnBehalfOf.Value.Equals(CStr(UserId))) Then 'if onbehalfof is empty or is the form creator
                insert.UserId = UserId
                Log(insert.RID, LOG_LEVEL_INFO, "New Reimbursement CREATED")
            Else
                insert.UserId = hfOnBehalfOf.Value
                insert.SpareField3 = CStr(UserId)  'This is the delegate, filling out the form on behalf of the user.
                Log(insert.RID, LOG_LEVEL_INFO, "New Reimbursement CREATED on behalf of " & tbNewOnBehalfOf.Text)
            End If

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
            btnSubmit.ToolTip = If(Not btnSubmit.Attributes("class").Contains("aspNetDisabled"), "", Translate("btnSubmitHelp"))

            d.AP_Staff_Rmbs.InsertOnSubmit(insert)
            d.SubmitChanges()
            Dim taskList = New List(Of Task)
            taskList.Add(LoadMenuAsync())
            taskList.Add(LoadRmbAsync(insert.RMBNo))
            Await Task.WhenAll(taskList)
            ScriptManager.RegisterClientScriptBlock(Page, Page.GetType(), "hide_loading_spinner", "$('#loading').hide();", True)
        End Sub

        Protected Async Sub btnSubmit_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSubmit.Click
            Dim State As Integer = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.Status).First
            If Not (State = RmbStatus.Draft Or State = RmbStatus.Cancelled Or State = RmbStatus.Submitted) Then Return

            saveIfNecessary()
            If btnSubmit.Attributes("class").Contains("aspNetDisabled") Then
                'show alert
                ScriptManager.RegisterClientScriptBlock(btnSubmit, btnSubmit.GetType, "submitAlert", "alert('" & Translate("btnSubmitHelp") & "');", True)
                Return
            End If

            Dim rmbs = From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value
            If rmbs.Count > 0 Then
                Dim rmb = rmbs.First
                If (rmb.ApprUserId Is Nothing) Then Return
                ' Ensure approver has sufficient permissions
                Dim approvers As StaffRmbFunctions.Approvers
                approvers = Await StaffRmbFunctions.getApproversAsync(rmb)
                Dim authorized = False
                For Each User In approvers.UserIds
                    If User.UserID = rmb.ApprUserId Then authorized = True
                Next
                If Not authorized Then
                    Await updateApproversListAsync(rmb)
                    lblError.Text = "Please select a new approver and click Submit again"
                    lblError.Visible = True
                    Return
                Else
                    lblError.Text = ""
                    lblError.Visible = False
                End If

                updateOutOfDateFlag()
                updateReceiptPermissions(rmb)
                Dim NewStatus As Integer = rmb.Status
                Dim rmbTotal = CType((From t In d.AP_Staff_RmbLines Where t.RmbNo = rmb.RMBNo Select t.GrossAmount).Sum(), Decimal?).GetValueOrDefault(0)
                Dim requires_receipts = ((From b In rmb.AP_Staff_RmbLines Where b.Receipt = True And ((From f In d.AP_Staff_RmbLine_Files Where f.RmbLineNo = b.RmbLineNo).Count = 0)).Count > 0)
                Dim printable = ""
                If (requires_receipts) Then printable = "window.open('/DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & rmb.RMBNo & "&UID=" & rmb.UserId & "&mode=1', '_blank'); "
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
                    If StaffBrokerFunctions.MinistryRequiresExtraApproval(rmb.CostCenter) Then
                        rmb.SpareField2 = StaffBrokerFunctions.getDirectorFor(rmb.CostCenter, CType(Settings("EDMSId"), Integer))
                    End If
                    NewStatus = RmbStatus.Submitted
                    rmb.Locked = False
                End If

                rmb.ApprDate = Nothing
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
                Log(rmb.RID, LOG_LEVEL_INFO, "SUBMITTED to " & UserController.GetUserById(PortalId, rmb.ApprUserId).DisplayName)

                'use an alert to switch back to the main window from the printout window
                ScriptManager.RegisterStartupScript(Page, Me.GetType(), "popup_and_select", printable & " alert(""" & message & """); selectIndex(1)", True)

                Await Task.WhenAll(refreshMenuTasks)

            End If

        End Sub

        Protected Async Sub btnDelete_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnDelete.Click

            Dim rmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value
            If rmb.Count > 0 Then
                Dim State As Integer = rmb.First.Status
                If (State = RmbStatus.Processing Or State = RmbStatus.PendingDownload Or State = RmbStatus.DownloadFailed Or State = RmbStatus.Paid Or State = RmbStatus.Cancelled) Then Return

                rmb.First.Status = RmbStatus.Cancelled
                rmb.First.MoreInfoRequested = False
                lblStatus.Text = Translate(RmbStatus.StatusName(RmbStatus.Cancelled))
                rmb.First.RmbDate = Today
                lblSubmittedDate.Text = Today
                rmb.First.ApprDate = Nothing
                lblApprovedDate.Text = ""
                btnApprove.Visible = False
                btnDelete.Visible = False

                If rmb.First.UserId = UserId Then
                    Log(rmb.First.RID, LOG_LEVEL_INFO, "DELETED by owner")
                    ScriptManager.RegisterStartupScript(btnDelete, btnDelete.GetType(), "select5", "selectIndex(5)", True)
                Else
                    'Send an email to the end user
                    Dim from_email = UserController.GetUserById(PortalId, UserId).Email
                    Dim Message = StaffBrokerFunctions.GetTemplate("RmbCancelled", PortalId)
                    Dim StaffMbr = UserController.GetUserById(PortalId, rmb.First.UserId)
                    Dim delegateId As Integer = -1
                    Try
                        If (rmb.First.SpareField3 IsNot Nothing) Then delegateId = CInt(rmb.First.SpareField3)
                    Catch ex As Exception
                        delegateId = -1
                    End Try
                    Dim DelegateName = If(delegateId >= 0, UserController.GetUserById(PortalId, delegateId).DisplayName, "")
                    Dim DelegateEmail = If(delegateId >= 0, UserController.GetUserById(PortalId, delegateId).Email, "")
                    Dim comments As String = ""
                    If (UserInfo.UserID = rmb.First.ApprUserId) And (tbApprComments.Text.Trim().Length > 0) Then
                        comments = Translate("CommentLeft").Replace("[FIRSTNAME]", UserInfo.FirstName).Replace("[COMMENT]", tbApprComments.Text)
                    ElseIf IsAccounts() And (tbAccComments.Text.Trim().Length > 0) Then
                        comments = Translate("CommentLeft").Replace("[FIRSTNAME]", UserInfo.FirstName).Replace("[COMMENT]", tbAccComments.Text)
                    End If

                    Message = Message.Replace("[STAFFNAME]", If(delegateId >= 0, DelegateName & " (" & Translate("OnBehalfOf") & StaffMbr.DisplayName & ")", StaffMbr.FirstName))
                    Message = Message.Replace("[APPRNAME]", UserInfo.FirstName & " " & UserInfo.LastName)
                    Message = Message.Replace("[APPRFIRSTNAME]", UserInfo.FirstName)
                    Message = Message.Replace("[COMMENTS]", comments)

                    SendEmail("P2C Reimbursements <" & from_email & ">", StaffMbr.Email, DelegateEmail, Translate("EmailCancelledSubject").Replace("[RMBNO]", rmb.First.RID).Replace("[USERREF]", rmb.First.UserRef), Message)

                    pnlMain.Visible = False
                    ltSplash.Text = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("RmbSplash", PortalId))
                    pnlSplash.Visible = True

                    Log(rmb.First.RID, LOG_LEVEL_INFO, "DELETED")
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
                Dim State As Integer = rmb.First.Status
                If Not (State = RmbStatus.Submitted Or State = RmbStatus.PendingDirectorApproval Or State = RmbStatus.PendingEDMSApproval) Then
                    ScriptManager.RegisterStartupScript(btnApprove, btnApprove.GetType(), "not_approved", "alert('" + Translate("ErrorApprovalState") + "');", True)
                    Return
                End If

                Dim message As String = ""
                Dim rmbTotal = CType((From a In d.AP_Staff_RmbLines Where a.RmbNo = rmb.First.RMBNo Select a.GrossAmount).Sum(), Decimal?).GetValueOrDefault(0)
                rmb.First.SpareField1 = rmbTotal.ToString("C") ' currency formatted string
                ' The following If statements are ordered to allow an approver who is also a director to not have to approve twice.
                Dim shouldSendApprovalEmail = False
                If rmb.First.Status = RmbStatus.Submitted Then
                    If UserId = rmb.First.ApprUserId Then
                        message += Translate("RmbApproved").Replace("[RMBNO]", rmb.First.RID) + "\n"
                        If StaffBrokerFunctions.MinistryRequiresExtraApproval(rmb.First.CostCenter) Then
                            rmb.First.Status = RmbStatus.PendingDirectorApproval
                            rmb.First.SpareField2 = StaffBrokerFunctions.getDirectorFor(rmb.First.CostCenter, CType(Settings("EDMSId"), Integer))
                            shouldSendApprovalEmail = True '
                            message += Translate("ExtraApproval") + "\n"
                        ElseIf hasOldExpenses() Then
                            rmb.First.Status = RmbStatus.PendingEDMSApproval
                            shouldSendApprovalEmail = True
                            message += Translate("WarningOldExpenses").Replace("[DAYS]", Settings("Expire"))
                        Else
                            rmb.First.Status = RmbStatus.Approved
                            rmb.First.ApprDate = Now
                            rmb.First.Locked = True
                            SendApprovedEmail(rmb.First)
                        End If
                        Log(rmb.First.RID, LOG_LEVEL_INFO, "APPROVED")
                    End If
                End If
                If rmb.First.Status = RmbStatus.PendingDirectorApproval Then
                    Dim directorId = StaffBrokerFunctions.getDirectorFor(rmb.First.CostCenter, CType(Settings("EDMSId"), Integer))
                    If UserId = directorId Then
                        'Check to see if extra approval is still necessary (the requirement may have been removed)
                        Dim ministryFlagged = StaffBrokerFunctions.MinistryRequiresExtraApproval(rmb.First.CostCenter)
                        Dim rmbFlagged = hasOldExpenses()
                        If ministryFlagged Or rmbFlagged Then
                            rmb.First.Status = RmbStatus.PendingEDMSApproval
                            shouldSendApprovalEmail = True
                        Else
                            rmb.First.Status = RmbStatus.Approved
                            rmb.First.ApprDate = Now
                            rmb.First.Locked = True
                            shouldSendApprovalEmail = False
                            SendApprovedEmail(rmb.First)
                            Log(rmb.First.RID, LOG_LEVEL_INFO, "APPROVED by director (" & UserController.Instance.GetCurrentUserInfo.DisplayName & ")")
                        End If
                        message += Translate("RmbApprovedDirector").Replace("[RMBNO]", rmb.First.RID) + "\n"
                    End If
                End If
                If rmb.First.Status = RmbStatus.PendingEDMSApproval Then
                    If UserId = CType(Settings("EDMSId"), Integer) Then
                        rmb.First.Status = RmbStatus.Approved
                        rmb.First.ApprDate = Now
                        rmb.First.Locked = True
                        shouldSendApprovalEmail = False
                        SendApprovedEmail(rmb.First)
                        message += Translate("RmbApprovedEDMS").Replace("[RMBNO]", rmb.First.RID)
                        Log(rmb.First.RID, LOG_LEVEL_INFO, EDMS_APPROVAL_LOG_MESSAGE)
                    End If
                End If
                If shouldSendApprovalEmail Then SendApprovalEmail(rmb.First)
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
                If message.Length = 0 Then message += Translate("UnauthorizedApprover")
                Await Task.WhenAll(refreshMenuTasks)
                ScriptManager.RegisterStartupScript(btnApprove, btnApprove.GetType(), "select2", "selectIndex(2); alert(""" & message & """);", True)
                btnApprove.Visible = False
                pnlMain.Visible = False
                ltSplash.Text = Server.HtmlDecode(StaffBrokerFunctions.GetTemplate("RmbSplash", PortalId))
                pnlSplash.Visible = True
            End If
        End Sub

        Protected Async Sub btnPrivAccComments_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPrivAccComments.Click
            btnPrivAccComments.Visible = False
            lblPrivAccComments.Visible = True
            tbPrivAccComments.Visible = True
        End Sub

        Protected Async Sub btnSave_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSave.Click
            If (IsAccounts()) Then
                saveFinanceComments()
            End If
            Dim State As Integer = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.Status).First()
            If (State = RmbStatus.Processing Or State = RmbStatus.PendingDownload Or State = RmbStatus.DownloadFailed Or State = RmbStatus.Paid) Then Return
            saveIfNecessary()
        End Sub

        Protected Async Sub addLinebtn2_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles addLinebtn2.Click
            Dim State As Integer = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.Status).First()
            If (State = RmbStatus.Processing Or State = RmbStatus.PendingDownload Or State = RmbStatus.DownloadFailed Or State = RmbStatus.Paid) Then Return

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

            Dim blank = New ListItem(Translate("ddlLineTypesHint"), String.Empty)
            blank.Attributes.Add("disabled", "disabled")
            blank.Attributes.Add("selected", "selected")
            blank.Attributes.Add("style", "color:grey")
            blank.Attributes.Add("style", "visibility:hidden") 'hide in dropdown list (display:none doesn't work in firefox)
            ddlLineTypes.Items.Insert(0, blank)
            ddlLineTypes.SelectedIndex = 0
            phLineDetail.Controls.Clear()
            btnSaveLine.Visible = False

            'Await ResetNewExpensePopupAsync(True)
            cbRecoverVat.Checked = False
            tbVatRate.Text = ""
            tbShortComment.Text = ""

            btnSaveLine.CommandName = "Save"

            ifReceipt.Attributes("src") = Request.Url.Scheme & "://" & Request.Url.Authority & "/DesktopModules/AgapeConnect/StaffRmb/ReceiptEditor.aspx?RmbNo=" & hfRmbNo.Value & "&RmbLine=New"
            pnlElecReceipts.Attributes("style") = "display: none;"
            ScriptManager.RegisterStartupScript(addLinebtn2, addLinebtn2.GetType(), "popupAdd", "showNewLinePopup();", True)
        End Sub

        'Protected Sub btnSettings_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSettings.Click
        '    Response.Redirect(EditUrl("RmbSettings"))
        'End Sub

        Protected Sub btnSplitAdd_Click(sender As Object, e As System.EventArgs) Handles btnSplitAdd.Click
            Dim State As Integer = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.Status).First()
            If (State = RmbStatus.Processing Or State = RmbStatus.PendingDownload Or State = RmbStatus.DownloadFailed Or State = RmbStatus.Paid Or State = RmbStatus.Cancelled) Then Return

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
            Dim State As Integer = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.Status).First()
            If (State = RmbStatus.Processing Or State = RmbStatus.PendingDownload Or State = RmbStatus.DownloadFailed Or State = RmbStatus.Paid Or State = RmbStatus.Cancelled) Then Return

            saveIfNecessary()
            Await LoadMenuAsync()
        End Sub

        Protected Async Sub ddlCompany_Change(sender As Object, e As System.EventArgs) Handles ddlCompany.SelectedIndexChanged
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


        Protected Async Sub btnOK_Click(sender As Object, e As System.EventArgs) Handles btnOK.Click 'this is the OK button on the Split dialog
            Dim State As Integer = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.Status).First()
            If State <> RmbStatus.Approved Then Return

            Dim theLine = From c In d.AP_Staff_RmbLines Where c.RmbLineNo = CInt(hfSplitLineId.Value)
            If theLine.Count > 0 Then
                Dim receipts As List(Of AP_Staff_RmbLine_File) = (From c In d.AP_Staff_RmbLine_Files Where c.RmbLineNo = theLine.First.RmbLineNo).ToList()
                Dim rownumber = 1
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
                    insert.Supplier = theLine.First.Supplier
                    insert.Comment = RowDesc
                    insert.GrossAmount = CDbl(RowAmount)
                    insert.OrigCurrency = theLine.First.OrigCurrency
                    insert.ExchangeRate = theLine.First.ExchangeRate
                    Dim originalAmount As Decimal
                    If (insert.ExchangeRate Is Nothing) Then
                        originalAmount = insert.GrossAmount
                    Else : originalAmount = insert.GrossAmount * insert.ExchangeRate
                    End If
                    insert.OrigCurrencyAmount = Math.Round(originalAmount, 2)
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
                    insert.Taxable = If(theLine.First.Taxable, 1, 0)
                    insert.TransDate = theLine.First.TransDate
                    insert.VATReceipt = theLine.First.VATReceipt
                    insert.CostCenter = theLine.First.CostCenter
                    insert.AccountCode = theLine.First.AccountCode
                    d.AP_Staff_RmbLines.InsertOnSubmit(insert)
                    d.SubmitChanges()
                    'Duplicate any receipts (and receiptImageId)
                    If (receipts.Count > 0) Then
                        Dim receiptnumber = 1
                        For Each receipt In receipts
                            Dim file = FileManager.Instance.GetFile(receipt.FileId)
                            Dim filename = "R" & insert.RmbNo & "L" & insert.RmbLineNo & "Rec" & receiptnumber & "." & file.Extension
                            Dim stream = FileManager.Instance.GetFileContent(file)
                            Dim copy = FileManager.Instance.AddFile(FolderManager.Instance.GetFolder(file.FolderId), filename, stream, False)
                            stream.Close()
                            stream.Dispose()
                            insert.ReceiptImageId = copy.FileId
                            Dim linefile As AP_Staff_RmbLine_File = New AP_Staff_RmbLine_File() With {
                                .RMBNo = insert.RmbNo,
                                .RmbLineNo = insert.RmbLineNo,
                                .RecNum = receiptnumber,
                                .FileId = copy.FileId,
                                .URL = FileManager.Instance.GetUrl(copy)
                            }
                            d.AP_Staff_RmbLine_Files.InsertOnSubmit(linefile)
                            receiptnumber = receiptnumber + 1
                        Next
                    End If
                    rownumber = rownumber + 1
                Next
                d.SubmitChanges()
                ' now Delete the original receipts
                For Each receipt In receipts
                    Dim fileId = receipt.FileId
                    Try
                        Dim file = FileManager.Instance.GetFile(fileId)
                        FileManager.Instance.DeleteFile(file)
                    Catch ex As Exception
                    End Try
                    d.AP_Staff_RmbLine_Files.DeleteAllOnSubmit(From c In d.AP_Staff_RmbLine_Files Where c.FileId = fileId)
                Next
            End If
            d.AP_Staff_RmbLine_Files.DeleteAllOnSubmit(From c In d.AP_Staff_RmbLine_Files Where c.RmbLineNo = theLine.First.RmbLineNo)
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
            If theRmb.First.Status <> RmbStatus.Approved Then Return

            'update the quick-total, in case it was changed after approval
            Dim rmbTotal = CType((From t In d.AP_Staff_RmbLines Where t.RmbNo = theRmb.First.RMBNo Select t.GrossAmount).Sum(), Decimal?).GetValueOrDefault(0)
            theRmb.First.SpareField1 = rmbTotal.ToString("C") ' currency formatted string

            Dim advancelines As IQueryable(Of AP_Staff_RmbLine) = From r In d.AP_Staff_RmbLines Where r.RmbNo = hfRmbNo.Value And r.GrossAmount < 0
            Dim clearAdvanceBalancesTask = updateAdvanceBalances(advancelines)
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
            processingPlaceholder.Controls.AddAt(0, GenerateTreeControl("treeProcessing"))
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

            'TaskList.Add(LoadRmbAsync(hfRmbNo.Value))
            Await Task.WhenAll(TaskList)
            If (insert) Then
                d.AP_Staff_Rmb_Post_Extras.InsertOnSubmit(PostingData)
            End If
            Await clearAdvanceBalancesTask
            SubmitChanges()
            Log(theRmb.First.RID, LOG_LEVEL_INFO, "PROCESSED - this reimbursement will be added to the next download batch")
            pnlMain.Visible = False
            '
            If (tvFinance.Nodes.Count = 1 And tvFinance.Nodes.Item(0).ChildNodes.Count = 5) Then
                tvFinance.Nodes.Item(0).ChildNodes.Item(0).Expand()
                tvFinance.Nodes.Item(0).ChildNodes.Item(1).Expand()
                tvFinance.Nodes.Item(0).ChildNodes.Item(2).Expand()
                tvFinance.Nodes.Item(0).ChildNodes.Item(3).Expand()
            End If
            Dim message = Translate("NextBatch")
            ScriptManager.RegisterStartupScript(Page, Me.GetType(), "closePostData", "closePostDataDialog();  alert(""" & message & """); loadAllProcessingTree();", True)
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

        Protected Async Sub btnUnProcess_Click(sender As Object, e As System.EventArgs) Handles btnUnProcess.Click
            Dim theRmb = (From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value)).First
            If Not (theRmb.Status = RmbStatus.Processing Or theRmb.Status = RmbStatus.PendingDownload Or theRmb.Status = RmbStatus.DownloadFailed) Then Return

            Dim TaskList As New List(Of Task)
            If theRmb.Status = RmbStatus.Processing Then
                'If the reimbursement has already been downloaded, a warning should be displayed - but hte reimbursement can be simply unprocessed
                theRmb.Status = RmbStatus.Approved
                'Remove any posting data associated with this rmb
                If ((From c In d.AP_Staff_Rmb_Post_Extras Where c.RMBNo = theRmb.RMBNo) IsNot Nothing) Then
                    d.AP_Staff_Rmb_Post_Extras.DeleteOnSubmit((From c In d.AP_Staff_Rmb_Post_Extras Where c.RMBNo = theRmb.RMBNo).Single)
                End If
                SubmitChanges()
                Log(theRmb.RID, LOG_LEVEL_WARNING, "UNPROCESSED, after it had been fully processed")
            Else
                'if it has not been downloaded, it will be downloaded very soon. We need to check if a download is already in progress.
                If StaffBrokerFunctions.GetSetting("Datapump", PortalId) = "locked" Then
                    'If a download is in progress, we need to display a "not at this time" message
                    Dim message = "This reimbursement cannot be unprocessed at this time, as it is currently being downloaded by the automatic datapump (transaction broker). You can try again in a few minutes, but be aware that it will already have been processed into your accounts program."
                    Dim t As Type = Me.GetType()
                    ScriptManager.RegisterStartupScript(Page, t, "popup", "alert(""" & message & """);", True)
                    Log(theRmb.RID, LOG_LEVEL_WARNING, "Attempted unprocessed, but could not as it was in the process of being downloaded by the automatic transaction broker")
                    Return
                Else
                    'If not, we need to lock the download progress (to ensure that it is not downloaded whilsts we are playing with it
                    StaffBrokerFunctions.SetSetting("Datapump", "Locked", PortalId)
                    'Then we can unprocess it
                    theRmb.Status = RmbStatus.Approved
                    theRmb.Period = Nothing
                    theRmb.Year = Nothing
                    theRmb.ProcDate = Nothing
                    'Remove any posting data associated with this rmb
                    If ((From c In d.AP_Staff_Rmb_Post_Extras Where c.RMBNo = theRmb.RMBNo) IsNot Nothing) Then
                        d.AP_Staff_Rmb_Post_Extras.DeleteOnSubmit((From c In d.AP_Staff_Rmb_Post_Extras Where c.RMBNo = theRmb.RMBNo).Single)
                    End If
                    SubmitChanges()
                    'Then release the lock.
                    StaffBrokerFunctions.SetSetting("Datapump", "Unlocked", PortalId)
                    Log(theRmb.RID, LOG_LEVEL_INFO, "UNPROCESSED - before it was downloaded")
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

        Private Function getExpenseLines() As IEnumerable
            Select Case ViewState("SortOrder")
                Case "RmbLineNo"
                    Return (From c In d.AP_Staff_RmbLines Where c.RmbNo = hfRmbNo.Value Order By c.RmbLineNo)
                Case "TransDate"
                    Return (From c In d.AP_Staff_RmbLines Where c.RmbNo = hfRmbNo.Value Order By c.TransDate)
                Case "Amount"
                    Return (From c In d.AP_Staff_RmbLines Where c.RmbNo = hfRmbNo.Value Order By c.GrossAmount)
            End Select
        End Function

        Protected Sub GridView1_Sorting(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewSortEventArgs) Handles GridView1.Sorting
            ViewState("SortOrder") = e.SortExpression
            GridView1.DataSource = getExpenseLines()
            GridView1.DataBind()
        End Sub

        Protected Async Sub GridView1_RowCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles GridView1.RowCommand
            Dim State As Integer = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.Status).First()
            If (State = RmbStatus.Processing Or State = RmbStatus.PendingDownload Or State = RmbStatus.DownloadFailed Or State = RmbStatus.Paid Or State = RmbStatus.Cancelled) Then Return

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
                    Dim lineTypes = From c In d.AP_StaffRmb_PortalLineTypes Where c.PortalId = PortalId Order By c.ViewOrder Select c.AP_Staff_RmbLineType.LineTypeId, c.LocalName, c.PCode, c.DCode

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

                    Dim jscript As System.Text.StringBuilder = New System.Text.StringBuilder()
                    Dim ucType As Type = theControl.GetType()
                    Dim ac = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)
                    ucType.GetMethod("Initialize").Invoke(theControl, New Object() {Settings})

                    ucType.GetProperty("Supplier").SetValue(theControl, theLine.First.Supplier, Nothing)
                    ucType.GetProperty("Comment").SetValue(theControl, theLine.First.Comment, Nothing)
                    If (theLine.First.OrigCurrency = ac) Then
                        ucType.GetProperty("Amount").SetValue(theControl, CDbl(theLine.First.GrossAmount), Nothing)
                    Else
                        ucType.GetProperty("Amount").SetValue(theControl, CDbl(theLine.First.OrigCurrencyAmount), Nothing)
                    End If
                    ucType.GetProperty("CADValue").SetValue(theControl, Math.Round(CDbl(theLine.First.GrossAmount), 2), Nothing)
                    If (Not theLine.First.OrigCurrencyAmount Is Nothing) Then
                        'hfOrigCurrencyValue.Value = CDbl(theLine.First.OrigCurrencyAmount)
                        jscript.Append(" $('#" & hfOrigCurrencyValue.ClientID & "').val(" & theLine.First.OrigCurrencyAmount & ");")
                    End If
                    Dim currency = ac
                    If (theLine.First.OrigCurrency IsNot Nothing AndAlso theLine.First.OrigCurrency <> ac) Then
                        currency = theLine.First.OrigCurrency
                    End If
                    jscript.Append(" $('#" & hfOrigCurrency.ClientID & "').val('" & currency & "');")
                    Try
                        ucType.GetMethod("Set_Currency").Invoke(theControl, New Object() {theLine.First.OrigCurrency})
                    Catch ex As Exception
                        'Some controls may not have a Set_Currency method (ie. Advances)
                    End Try
                    If (theLine.First.OutOfDate) Then
                        jscript.Append("check_expense_date(); ")
                    End If
                    ucType.GetProperty("theDate").SetValue(theControl, theLine.First.TransDate, Nothing)
                    ucType.GetProperty("VAT").SetValue(theControl, theLine.First.VATReceipt, Nothing)
                    ucType.GetProperty("Receipt").SetValue(theControl, theLine.First.Receipt, Nothing)
                    ucType.GetProperty("Spare1").SetValue(theControl, theLine.First.Spare1, Nothing)
                    ucType.GetProperty("Spare2").SetValue(theControl, theLine.First.Spare2, Nothing)
                    ucType.GetProperty("Spare3").SetValue(theControl, theLine.First.Spare3, Nothing)
                    ucType.GetProperty("Spare4").SetValue(theControl, theLine.First.Spare4, Nothing)
                    ucType.GetProperty("Spare5").SetValue(theControl, theLine.First.Spare5, Nothing)

                    Dim mileageString As String = ""
                    Try
                        If (ucType.GetProperty("Mileage") IsNot Nothing) Then
                            If (theLine.First.Mileage IsNot Nothing) Then
                                ucType.GetProperty("Mileage").SetValue(theControl, theLine.First.Mileage, Nothing)
                                mileageString = GetMileageString(theLine.First.Mileage, theLine.First.MileageRate)
                            Else
                                ucType.GetProperty("Mileage").SetValue(theControl, 0, Nothing)
                            End If
                        End If
                    Catch
                        'Some controls may not have a Mileage property
                    End Try

                    Dim receiptMode = RmbReceiptType.Standard
                    If theLine.First.VATReceipt Then
                        receiptMode = RmbReceiptType.VAT
                        ' If we have any files matching this line, or our receiptImageId is valid
                    ElseIf (From lf In d.AP_Staff_RmbLine_Files Where lf.RmbLineNo = theLine.First.RmbLineNo And lf.RMBNo = theLine.First.RmbNo).Count > 0 Then
                        receiptMode = RmbReceiptType.Electronic
                        Try
                            If (ucType.GetProperty("ReceiptsAttached") IsNot Nothing) Then
                                ucType.GetProperty("ReceiptsAttached").SetValue(theControl, True, Nothing)
                            End If
                        Catch
                        End Try
                        ' If we don't have a receipt
                    ElseIf Not theLine.First.Receipt Then
                        receiptMode = RmbReceiptType.No_Receipt
                    End If
                    Try
                        ucType.GetProperty("ReceiptType").SetValue(theControl, receiptMode, Nothing)
                    Catch ex As Exception

                    End Try

                    cbRecoverVat.Checked = False
                    ddlOverideTax.SelectedIndex = If(theLine.First.Taxable, 1, 0)
                    tbVatRate.Text = ""
                    If theLine.First.VATRate > 0 Then
                        If theLine.First.VATRate > 0 Then
                            cbRecoverVat.Checked = True
                            tbVatRate.Text = theLine.First.VATRate
                        End If
                    End If

                    Try
                        tbShortComment.Text = GetLineComment(theLine.First.Comment, theLine.First.OrigCurrency, If(theLine.First.OrigCurrencyAmount Is Nothing, 0, theLine.First.OrigCurrencyAmount), theLine.First.ShortComment, False, Nothing, mileageString)
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
                    If receiptMode = RmbReceiptType.Electronic Then
                        pnlElecReceipts.Attributes("style") = ""
                    Else
                        pnlElecReceipts.Attributes("style") = "display: none;"
                    End If

                    Dim t As Type = GridView1.GetType()
                    jscript.Append(" showNewLinePopup();")
                    ScriptManager.RegisterStartupScript(GridView1, t, "popupedit", jscript.ToString, True)
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
                    sb.Append("window.open('mailto:" & theUser.Email & "?subject=Reimbursement " & theLine.First.AP_Staff_Rmb.RID & ": Deferred Transactions');")
                    sb.Append("</script>")
                    Await loadRmbTask

                    ScriptManager.RegisterStartupScript(GridView1, t, "email", sb.ToString, False)

                End If
            End If

        End Sub

        Protected Async Sub menu_ItemCommand(ByVal source As Object, ByVal e As System.Web.UI.WebControls.DataListCommandEventArgs) Handles dlProcessing.ItemCommand, dlApproved.ItemCommand, dlCancelled.ItemCommand, dlToApprove.ItemCommand, dlSubmitted.ItemCommand, dlPending.ItemCommand, dlPaid.ItemCommand
            hfRmbNo.Value = e.CommandArgument
            If e.CommandName = "Goto" Then
                Await LoadRmbAsync(e.CommandArgument)
            End If
        End Sub

        Protected Async Sub menu_subtree_ItemCommand(ByVal node As TreeView, ByVal e As System.EventArgs) Handles tvTeamApproved.SelectedNodeChanged, tvTeamProcessing.SelectedNodeChanged, tvTeamPaid.SelectedNodeChanged, tvFinance.SelectedNodeChanged
            Await LoadRmbAsync(node.SelectedValue)
            ScriptManager.RegisterStartupScript(GridView1, GridView1.GetType(), "deselect_menu", "deselectPreviousMenuItem()", True)
        End Sub


#End Region
#Region "OnChange Events"
        Protected Async Sub ddlLineTypes_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlLineTypes.SelectedIndexChanged
            Dim resetTask = ResetNewExpensePopupAsync(Not btnSaveLine.Visible)
            If lblIncType.Visible And ddlLineTypes.SelectedIndex <> ddlLineTypes.Items.Count - 1 Then
                Dim oldValue = ddlLineTypes.SelectedValue
                ddlLineTypes.Items.Clear()
                Dim lineTypes = From c In d.AP_StaffRmb_PortalLineTypes Where c.PortalId = PortalId Order By c.ViewOrder Select c.AP_Staff_RmbLineType.LineTypeId, c.LocalName, c.PCode, c.DCode

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
            btnSaveLine.Visible = True

            If (btnSaveLine.CommandName.Equals("Save")) Then
                ' reset and show repeat if available
                Try
                    Dim repeat As System.Reflection.PropertyInfo = theControl.GetType().GetProperty("Repeat")
                    If repeat IsNot Nothing Then
                        repeat.SetValue(theControl, 1, Nothing)
                    End If
                Catch ex As Exception
                End Try
            End If
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
                        rmb.First.ApprDate = Nothing
                        rmb.First.ApprUserId = Nothing
                        SubmitChanges()
                        TaskList.Add(LoadMenuAsync())
                        ScriptManager.RegisterStartupScript(tbChargeTo, tbChargeTo.GetType(), "select0", "selectIndex(0)", True)
                    Else
                        SubmitChanges()
                    End If
                    TaskList.Add(updateApproversListAsync(rmb.First))
                    updateBalanceLabel(Await getAccountBalanceTask)
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
            Dim oldApprover As UserInfo
            Try
                oldApprover = UserController.GetUserById(PortalId, rmb.First.ApprUserId)
            Catch ex As Exception
                oldApprover = Nothing
            End Try
            If (oldApprover IsNot Nothing) Then
                Dim newApproverName As String
                Try
                    newApproverName = UserController.GetUserById(PortalId, ddlApprovedBy.SelectedValue).DisplayName
                Catch
                    newApproverName = ""
                End Try

                'If the form is awaiting approval, notifiy the original approver that they are no longer needed
                If (rmb.First.Status = RmbStatus.Submitted AndAlso rmb.First.ApprUserId <> ddlApprovedBy.SelectedValue) Then
                    Dim fromEmail = UserController.Instance.GetCurrentUserInfo.Email
                    Dim subject As String = Translate("ApproverChangedSubject").Replace("[RMBNO]", rmb.First.RID)
                    Dim body As String = Translate("ApproverChangedEmail").Replace("[RMBNO]", rmb.First.RID)
                    SendEmail(fromEmail, oldApprover.Email, "", subject, body)
                    ScriptManager.RegisterStartupScript(ddlApprovedBy, ddlApprovedBy.GetType(), "notify_approver", "alert('" + oldApprover.DisplayName + " was notified that they are no longer required to approve this reimbursement');", True)
                End If
                Log(rmb.First.RID, LOG_LEVEL_INFO, "Approver changed from " + oldApprover.DisplayName + " to " + newApproverName)
            End If
            Try
                rmb.First.ApprUserId = ddlApprovedBy.SelectedValue
            Catch
                rmb.First.ApprUserId = Nothing
            End Try
            Dim refreshMenu = (Not rmb.First.Status = RmbStatus.Draft)
            rmb.First.ApprDate = Nothing
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
            enableSubmitButton(btnSubmit.Visible And tbChargeTo.Text.Length = 6 And ddlApprovedBy.SelectedIndex > 0 And GridView1.Rows.Count > 0)
            btnSubmit.ToolTip = If(Not btnSubmit.Attributes("class").Contains("aspNetDisabled"), "", Translate("btnSubmitHelp"))
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

        Public Function GetSupplier(ByVal Supplier As String)
            If (Supplier.Length > 0) Then
                Return " (" & Supplier & ")"
            End If
            Return ""
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
                CurString = "-" & Mileage


            Else
                If Not String.IsNullOrEmpty(Currency) Then
                    If Currency <> StaffBrokerFunctions.GetSetting("            ", PortalId) Then
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

        Public Function GetRmbTitleTeam(ByVal RID As Integer, ByVal UID As Integer, ByVal RmbDate As Date) As String
            Dim Sm = UserController.GetUserById(PortalId, UID)

            Dim rtn = Left(Sm.DisplayName, 20) & "<br />" & "<span style=""font-size: 6.5pt; color: #999999;"">#" & ZeroFill(RID.ToString, 5)
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

        Protected Function GetRmbTitleFinance(ByVal initials As String, ByVal RID As Integer, ByVal ApprDate As Date?, ByVal amount As String, flag As Boolean) As String
            Try
                Dim DateString = If(ApprDate Is Nothing, "not approved", CType(ApprDate, Date).ToShortDateString)
                Dim rtn As String = "<span class='" & If(flag, "blue_highlight ", "") & "finance tree' onclick='finance_tree_click(this);'>"
                rtn = rtn & "<span style=""font-size: 6.5pt; "">" & initials & " #" & ZeroFill(RID.ToString, 5)
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
                rtn = rtn & "</span></span>"
                Return rtn
            Catch ex As Exception
                Throw New Exception("Error building title: " + ex.Message)
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

            Try
                Dim rmb_status = (From c In d.AP_Staff_Rmbs Where c.RMBNo = RmbNo Select c.Status).First
                Select Case rmb_status
                    Case RmbStatus.Draft
                        Return 0
                    Case RmbStatus.Submitted, RmbStatus.PendingDirectorApproval, RmbStatus.PendingEDMSApproval
                        Return 1
                    Case RmbStatus.Approved, RmbStatus.PendingDownload, RmbStatus.DownloadFailed
                        Return 2
                    Case RmbStatus.Processing
                        Return 3
                    Case RmbStatus.Paid
                        Return 4
                    Case Else
                        Return 0
                End Select
            Catch
                Return 0
            End Try


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

        Protected Function ElectronicReceiptTags(lineId As Integer) As String
            Dim ERR = "<span color='red'>!</span>"
            Dim result = ""
            Dim receipts = From e In d.AP_Staff_RmbLine_Files Where e.RmbLineNo = lineId Order By e.RecNum Select e.URL, e.FileId
            If (receipts.Count < 1) Then
                result = "<img src='/Icons/Sigma/BulkMail_32X32_Standard.png' width=20 alt='mail' title='receipt will be sent by mail'/>"
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
                            result += "<a target='receipt_window' href=" + receipt.URL + "><img id='" + receipt.URL + "' class='viewReceipt" & "' src='/Icons/Sigma/ExtPng_32x32_Standard.png' width=20 alt='img' /></a>"
                        Else
                            result += "<img src='/Icons/Sigma/ErrorWarning_16X16_Standard.png' width=20 alt='missing' title='" & extension & "'/>"
                        End If
                    End If
                Next
            End If
            Return result
        End Function

        Protected Function HasMultipleReceipts(lineId As Integer) As Boolean
            Dim receipts = From e In d.AP_Staff_RmbLine_Files Where e.RmbLineNo = lineId Select e
            Return (receipts.Count() > 1)
        End Function

        Protected Function CanEdit(ByVal status As Integer) As Boolean
            'nobody can edit in these states
            If (status = RmbStatus.Paid) Then Return False
            If (status = RmbStatus.Processing) Then Return False
            If (status = RmbStatus.PendingDownload Or status = RmbStatus.DownloadFailed) Then Return False

            'always editable in these states
            If (status = RmbStatus.Draft) Then Return True
            If (status = RmbStatus.Submitted Or status = RmbStatus.PendingDirectorApproval Or status = RmbStatus.PendingEDMSApproval) Then Return True
            'accounts can always edit
            If (IsAccounts()) Then Return True

            'owner or delegate can edit if we've gotten this far, and the moreinfo flag is set
            Dim rmbs = From c In d.AP_Staff_Rmbs Where (c.RMBNo = hfRmbNo.Value)
            If (rmbs.Count > 0) Then
                If rmbs.First.MoreInfoRequested Then Return True
            End If
            Return False
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

            Dim rTotal As Double = 0
            Dim rT = (From c In d.AP_Staff_RmbLines Where c.AP_Staff_Rmb.PortalId = PortalId And (c.AP_Staff_Rmb.UserId = theStaff.UserId1 Or c.AP_Staff_Rmb.UserId = theStaff.UserId2) And statusList.Contains(c.AP_Staff_Rmb.Status) Select c.GrossAmount)
            If rT.Count > 0 Then
                rTotal = rT.Sum()
            End If

            Return Math.Round(AccountBalance - (rTotal), 2)

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
                enableSubmitButton(False)
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
            ' Never change costcenter once a reimbursement has been processed
            If (Rmb.CostCenter <> tbChargeTo.Text) And (Rmb.ProcDate Is Nothing) Then
                Log(Rmb.RID, LOG_LEVEL_INFO, "CostCenter changed from: " + Rmb.CostCenter + " to: " + tbChargeTo.Text)
                save_necessary = True
                Rmb.CostCenter = tbChargeTo.Text
                For Each row In (From c In Rmb.AP_Staff_RmbLines Where c.CostCenter = Rmb.CostCenter)
                    row.CostCenter = tbChargeTo.Text
                Next
            End If
            ' Never change approver once a reimbursement has been approved
            If (ddlApprovedBy.SelectedValue <> Nothing) AndAlso (Rmb.ApprUserId <> ddlApprovedBy.SelectedValue) AndAlso (Rmb.ApprDate Is Nothing) Then
                Log(Rmb.RID, LOG_LEVEL_INFO, "Approver changed from: " + UserController.GetUserById(Rmb.PortalId, Rmb.ApprUserId).DisplayName + " to: " + UserController.GetUserById(PortalId, ddlApprovedBy.SelectedValue).DisplayName)
                save_necessary = True
                Rmb.ApprUserId = ddlApprovedBy.SelectedValue
            End If
            If (Rmb.UserRef <> tbYouRef.Text) Then
                Log(Rmb.RID, LOG_LEVEL_INFO, "UserRef changed from: " + Rmb.UserRef + " to: " + tbYouRef.Text)
                save_necessary = True
                Rmb.UserRef = tbYouRef.Text
            End If
            If (Rmb.UserComment <> tbComments.Text) Then
                Log(Rmb.RID, LOG_LEVEL_INFO, "User Comment changed to: " + tbComments.Text)
                save_necessary = True
                Rmb.UserComment = tbComments.Text
            End If
            If (Rmb.ApprComment <> tbApprComments.Text) Then
                Log(Rmb.RID, LOG_LEVEL_INFO, "Approver Comment changed to: " + tbApprComments.Text)
                save_necessary = True
                Rmb.ApprComment = tbApprComments.Text
                enableRejectButton(tbApprComments.Text <> "")
            End If
            If (Rmb.AcctComment <> tbAccComments.Text) Then
                Log(Rmb.RID, LOG_LEVEL_INFO, "Finance Comment changed to: " + tbAccComments.Text)
                save_necessary = True
                Rmb.AcctComment = tbAccComments.Text
            End If
            If (Rmb.PrivComment <> tbPrivAccComments.Text) Then
                Log(Rmb.RID, LOG_LEVEL_INFO, "Private Finance comment updated")
                save_necessary = True
                Rmb.PrivComment = tbPrivAccComments.Text
            End If
            If ((Rmb.MoreInfoRequested Is Nothing And (cbMoreInfo.Checked Or cbApprMoreInfo.Checked)) Or
                Rmb.MoreInfoRequested <> (cbMoreInfo.Checked Or cbApprMoreInfo.Checked)) Then
                If (Rmb.MoreInfoRequested Is Nothing Or Rmb.MoreInfoRequested = False) Then
                    Log(Rmb.RID, LOG_LEVEL_INFO, "MoreInfoFlag set")
                Else
                    Log(Rmb.RID, LOG_LEVEL_INFO, "MoreInfoFlag cleared")
                End If
                save_necessary = True
                Rmb.Locked = False
                Rmb.MoreInfoRequested = (cbMoreInfo.Checked Or cbApprMoreInfo.Checked)
            End If

            If (save_necessary) Then
                SubmitChanges()
            End If
        End Sub

        Protected Sub SaveFinanceComments()
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
            Dim changed = False
            If (Rmb.AcctComment <> tbAccComments.Text) Then
                Rmb.AcctComment = tbAccComments.Text
                Log(Rmb.RID, LOG_LEVEL_INFO, "Finance comment changed to: " + tbAccComments.Text)
                changed = True
            End If
            If (Rmb.PrivComment <> tbPrivAccComments.Text) Then
                Rmb.PrivComment = tbPrivAccComments.Text
                Log(Rmb.RID, LOG_LEVEL_INFO, "Private Finance comment updated")
                changed = True
            End If
            If changed Then
                SubmitChanges()
            End If
        End Sub

        Protected Sub Log(ByVal RID As Integer, ByVal logType As Integer, ByVal Message As String)
            Dim entry = New AP_Staff_Rmb_Log()
            entry.Timestamp = DateTime.Now
            entry.LogType = logType
            entry.RID = RID
            entry.Username = UserController.GetUserById(PortalId, UserId).DisplayName
            entry.Message = Message
            d.AP_Staff_Rmb_Logs.InsertOnSubmit(entry)
            d.SubmitChanges()
        End Sub

        Private Function hasElectronicReceipts(lineNo As Integer) As Boolean
            Return (From c In d.AP_Staff_RmbLine_Files Where c.RmbLineNo = lineNo).Count > 0
        End Function

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
                    Dim Supplier As String = ""
                    Dim Comment As String = ""
                    Dim Amount As Double = 0.0
                    Dim theDate As Date = Today
                    Dim taxable As Integer
                    Dim exchangerate As Double = 1
                    Dim VAT As Boolean = False
                    Dim Receipt As Boolean = True
                    Dim Province As String = Nothing
                    Dim receiptMode As Integer = RmbReceiptType.UNSELECTED
                    Dim receiptsAttached As Boolean = False
                    Dim currency As String = StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)

                    If Not blankValues Then
                        Try
                            If Not (theControl Is Nothing) Then
                                Dim ucTypeOld As Type = theControl.GetType()
                                ' Attempt to get the receiptMode
                                Try
                                    receiptMode = CInt(ucTypeOld.GetProperty("ReceiptType").GetValue(theControl, Nothing))
                                Catch ex As Exception ' We couldn't get one; no big deal, but keep going with this block of code
                                End Try
                                Supplier = CStr(ucTypeOld.GetProperty("Supplier").GetValue(theControl, Nothing))
                                Comment = CStr(ucTypeOld.GetProperty("Comment").GetValue(theControl, Nothing))
                                theDate = CDate(ucTypeOld.GetProperty("theDate").GetValue(theControl, Nothing))
                                Amount = CDbl(ucTypeOld.GetProperty("Amount").GetValue(theControl, Nothing))
                                VAT = CStr(ucTypeOld.GetProperty("VAT").GetValue(theControl, Nothing))
                                Receipt = CStr(ucTypeOld.GetProperty("Receipt").GetValue(theControl, Nothing))
                                taxable = ddlOverideTax.SelectedIndex
                                exchangerate = CDbl(ucTypeOld.GetProperty("ExchangeRate").GetValue(theControl, Nothing))
                                Province = CStr(ucTypeOld.GetProperty("Spare1").GetValue(theControl, Nothing))
                                currency = hfOrigCurrency.Value
                                If (ucTypeOld.GetProperty("ReceiptsAttached") IsNot Nothing) Then
                                    receiptsAttached = CBool(ucTypeOld.GetProperty("ReceiptsAttached").GetValue(theControl, Nothing))
                                End If
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                    ' Save the standard values
                    Dim owner As Integer
                    Try
                        owner = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.UserId).First()
                    Catch
                        owner = UserId
                    End Try

                    hfOrigCurrency.Value = currency
                    hfOrigCurrencyValue.Value = 0

                    phLineDetail.Controls.Clear()
                    theControl = LoadControl(lt.First.ControlPath)

                    theControl.ID = "theControl"
                    phLineDetail.Controls.Add(theControl)
                    Dim ucType As Type = theControl.GetType()

                    If (Province Is Nothing) Then
                        Province = StaffRmbFunctions.GetDefaultProvince(owner)
                    End If
                    If (blankValues) Then
                        taxable = If(ucType.GetProperty("Taxable").GetValue(theControl, Nothing), 1, 0)
                    End If
                    ddlOverideTax.SelectedIndex = taxable

                    ucType.GetMethod("Initialize").Invoke(theControl, New Object() {Settings})

                    ucType.GetProperty("Supplier").SetValue(theControl, Supplier, Nothing)
                    ucType.GetProperty("Comment").SetValue(theControl, Comment, Nothing)
                    ucType.GetProperty("ExchangeRate").SetValue(theControl, exchangerate, Nothing)
                    ucType.GetProperty("Amount").SetValue(theControl, Amount, Nothing)
                    ucType.GetProperty("theDate").SetValue(theControl, theDate, Nothing)
                    ucType.GetProperty("VAT").SetValue(theControl, VAT, Nothing)
                    ucType.GetProperty("Receipt").SetValue(theControl, Receipt, Nothing)
                    If (ucType.GetProperty("ReceiptsAttached") IsNot Nothing) Then
                        ucType.GetProperty("ReceiptsAttached").SetValue(theControl, receiptsAttached, Nothing)
                    End If
                    ucType.GetProperty("Spare1").SetValue(theControl, Province, Nothing)
                    ucType.GetProperty("Spare2").SetValue(theControl, "", Nothing)
                    ucType.GetProperty("Spare3").SetValue(theControl, "", Nothing)
                    ucType.GetProperty("Spare4").SetValue(theControl, "", Nothing)
                    ucType.GetProperty("Spare5").SetValue(theControl, "", Nothing)
                    If (ucType.GetProperty("Mileage") IsNot Nothing) Then
                        ucType.GetProperty("Mileage").SetValue(theControl, 0, Nothing)
                    End If
                    ucType.GetMethod("Set_Currency").Invoke(theControl, New Object() {currency})
                    Dim jscript = "$('#" & hfOrigCurrency.ClientID & "').val('" & currency & "');"
                    jscript += "$('.ddlProvince').change(function() {$('.ddlTaxable').prop('selectedIndex', ($('.ddlProvince').val()!='--'));});"
                    ScriptManager.RegisterStartupScript(Page, Me.GetType(), "setCur", jscript, True)
                    ' Attempt to set the receipttype
                    Try
                        ucType.GetProperty("ReceiptType").SetValue(theControl, receiptMode, Nothing)
                        If (receiptMode = RmbReceiptType.Electronic) Then ' We have electronic receipts
                            pnlElecReceipts.Attributes("style") = ""
                        Else ' Make sure it's hidden
                            pnlElecReceipts.Attributes("style") = "display: none"
                        End If
                    Catch ex As Exception
                        ' We apparently can't set the receipt type
                        ' Need to ensure the electronic receipts are hidden
                        pnlElecReceipts.Attributes("style") = "display: none;"
                    End Try
                    ddlAccountCode.SelectedValue = GetAccountCode(lt.First.LineTypeId, tbCostcenter.Text)
                End If
            Catch ex As Exception
                Log(lblRmbNo.Text, LOG_LEVEL_ERROR, "ERROR Resetting Expense Popup. " + ex.ToString)
            End Try
        End Function

        Protected Sub SendEmail(sender As String, recipient As String, cc As String, subject As String, body As String)
            Try
                DotNetNuke.Services.Mail.Mail.SendMail(sender, recipient, cc, "", Services.Mail.MailPriority.Normal, subject, Services.Mail.MailFormat.Html, System.Text.Encoding.ASCII, body, "", "", "", "", "")
            Catch ex As Exception
                lblErrorMessage.Text = "ERROR sending email"
                pnlError.Visible = True
                Log(lblRmbNo.Text, LOG_LEVEL_ERROR, "ERROR sending email: " + ex.Message)
            End Try
        End Sub

        Protected Sub SendMoreinfoEmail(sender As String, comments As String, ByRef Rmb As AP_Staff_Rmb)
            Dim from_email = UserController.GetUserById(PortalId, UserId).Email
            Dim theUser = UserController.GetUserById(PortalId, Rmb.UserId)
            Dim address = theUser.Email
            Dim delegateId = -1
            Try
                If (Rmb.SpareField3 IsNot Nothing) Then delegateId = CInt(Rmb.SpareField3)
            Catch ex As Exception
                delegateId = -1
            End Try
            Dim delegateEmail = If(delegateId >= 0, UserController.GetUserById(PortalId, delegateId).Email, "")
            Dim rmbno = If(Not Rmb.UserRef.Equals(String.Empty), Rmb.UserRef, "#" & Rmb.RID)
            Dim link = NavigateURL() & "?RmbId=" & Rmb.RID
            Dim rmblink = "<a href='" & link & "'>" & link & "</a>"
            Dim subject = Translate("MoreInfoSubject").Replace("[USERREF]", rmbno)
            Dim body = Translate("MoreInfoBody").Replace("[WHO]", sender).Replace("[USERREF]", rmbno).Replace("[RMBLINK]", rmblink).Replace("[COMMENTS]", comments)
            SendEmail("P2C Reimbursements <" & from_email & ">", address, delegateEmail, subject, body)
        End Sub

        Protected Sub SendApprovalEmail(ByVal theRmb As AP_Staff_Rmb)
            'Sends an email to the creator, indicating who the form has been submitted to
            'AND sends an email to the approver, alerting him to the awaiting form.
            Try

                Dim SpouseId As Integer = StaffBrokerFunctions.GetSpouseId(theRmb.UserId)
                Dim ownerMessage As String = StaffBrokerFunctions.GetTemplate("RmbConfirmation", PortalId)
                Dim approverMessage As String = StaffBrokerFunctions.GetTemplate("RmbApproverEmail", PortalId)
                Dim owner = UserController.GetUserById(theRmb.PortalId, theRmb.UserId)
                Dim approver = UserController.GetUserById(theRmb.PortalId, theRmb.ApprUserId)
                Dim delegateId As Integer = -1
                Try
                    If (theRmb.SpareField3 IsNot Nothing) Then delegateId = CInt(theRmb.SpareField3)
                Catch ex As Exception
                    delegateId = -1
                End Try
                Dim DelegateName = If(delegateId >= 0, UserController.GetUserById(PortalId, delegateId).DisplayName, "")
                Dim DelegateEmail = If(delegateId >= 0, UserController.GetUserById(PortalId, delegateId).Email, "")
                Dim amount = theRmb.SpareField1
                Dim extra = If(StaffBrokerFunctions.MinistryRequiresExtraApproval(theRmb.RMBNo), Translate("ExtraApproval"), "")
                Dim toEmail = approver.Email
                Dim toName = approver.FirstName
                Dim hasReceipts = (From c In theRmb.AP_Staff_RmbLines
                                   Where c.Receipt = True And ((From f In d.AP_Staff_RmbLine_Files Where f.RmbLineNo = c.RmbLineNo).Count = 0)).Count > 0

                If theRmb.Status = RmbStatus.Submitted Then
                    ownerMessage = ownerMessage.Replace("[APPROVER]", approver.DisplayName).Replace("[EXTRA]", extra)
                    ownerMessage = ownerMessage.Replace("[STAFFACTION]", If(hasReceipts, Translate("PostReceipts"), Translate("NoPostRecipts")))
                ElseIf theRmb.Status = RmbStatus.PendingDirectorApproval Then
                    Dim director = UserController.GetUserById(PortalId, StaffBrokerFunctions.getDirectorFor(theRmb.CostCenter, CType(Settings("EDMSId"), Integer)))
                    ownerMessage = ownerMessage.Replace("[APPROVER]", director.DisplayName)
                    toEmail = director.Email
                    toName = director.FirstName
                ElseIf theRmb.Status = RmbStatus.PendingEDMSApproval Then
                    Dim edms = UserController.GetUserById(PortalId, CType(Settings("EDMSId"), Integer))
                    ownerMessage = ownerMessage.Replace("[APPROVER]", edms.DisplayName)
                    toEmail = edms.Email
                    toName = edms.FirstName
                End If
                ownerMessage = ownerMessage.Replace("[EXTRA]", "").Replace("[STAFFACTION]", "")
                ownerMessage = ownerMessage.Replace("[STAFFNAME]", If(delegateId >= 0, DelegateName & " (" & Translate("OnBehalfOf") & " " & owner.DisplayName & ")", owner.FirstName))
                ownerMessage = ownerMessage.Replace("[RMBNO]", theRmb.RID).Replace("[USERREF]", theRmb.UserRef)
                ownerMessage = ownerMessage.Replace("[PRINTOUT]", "<a href='" & Request.Url.Scheme & "://" & Request.Url.Authority & Request.ApplicationPath & "DesktopModules/AgapeConnect/StaffRmb/RmbPrintout.aspx?RmbNo=" & theRmb.RMBNo & "&UID=" & theRmb.UserId & "' target-'_blank' style='width: 134px; display:block;)'><div style='text-align: center; width: 122px; margin: 10px;'><img src='" _
                    & Request.Url.Scheme & "://" & Request.Url.Authority & Request.ApplicationPath & "DesktopModules/AgapeConnect/StaffRmb/Images/PrintoutIcon.jpg' /><br />Printout</div></a><style> a div:hover{border: solid 1px blue;}</style>")
                SendEmail("P2C Reimbursements <reimbursements@p2c.com>", owner.Email, DelegateEmail, Translate("EmailSubmittedSubject").Replace("[RMBNO]", theRmb.RID), ownerMessage)

                'Send Approvers Instructions Here
                If toEmail.Length > 0 Then
                    Dim subject = Translate("SubmittedApprEmailSubject").Replace("[STAFFNAME]", owner.DisplayName)
                    approverMessage = approverMessage.Replace("[STAFFNAME]", If(delegateId >= 0, DelegateName & " (" & Translate("OnBehalfOf") & " " & owner.DisplayName & ")", owner.DisplayName))
                    approverMessage = approverMessage.Replace("[RMBNO]", theRmb.RID).Replace("[USERREF]", IIf(theRmb.UserRef <> "", theRmb.UserRef, "None"))
                    approverMessage = approverMessage.Replace("[APPRNAME]", toName)
                    approverMessage = approverMessage.Replace("[EXTRA]", extra)
                    approverMessage = approverMessage.Replace("[AMOUNT]", amount)
                    approverMessage = approverMessage.Replace("[OLDEXPENSES]", If(hasOldExpenses(), Translate("WarningOldExpenses").Replace("[DAYS]", Settings("Expire")), ""))
                    approverMessage = approverMessage.Replace("[COMMENTS]", If(theRmb.UserComment <> "", Translate("EmailComments") & "<br />" & theRmb.UserComment, ""))
                    If StaffRmbFunctions.isStaffAccount(theRmb.CostCenter) Then
                        'Personal Reimbursement
                        Try
                            approverMessage = approverMessage.Replace("[LOWBALANCE]", If(hfAccountBalance.Value < GetTotal(hfRmbNo.Value), Translate("WarningLowBalanceStaffAccount"), ""))
                        Catch
                            approverMessage = approverMessage.Replace("[LOWBALANCE]", "")
                        End Try
                    Else
                        Dim low_balance = Translate("WarningLowBalance").Replace("[ACCTBAL]", hfAccountBalance.Value).Replace("[ACCT]", tbChargeTo.Text)
                        approverMessage = approverMessage.Replace("[LOWBALANCE]", If(isLowBalance(), low_balance, ""))
                    End If
                    '-- Send FROM owner, so that bounces or out-of-office replies come back to owner.
                    Dim fromEmail = If(delegateId >= 0, DelegateEmail, owner.Email)
                    SendEmail("P2C Reimbursements <" & fromEmail & ">", toEmail, "", subject, approverMessage)
                End If

            Catch ex As Exception
                lblError.Text = "Error Sending Approval eMail: " & ex.Message & ex.StackTrace
                lblError.Visible = True
            End Try

        End Sub

        Sub SendApprovedEmail(ByVal theRmb As AP_Staff_Rmb)
            Dim apprname As String = UserController.GetUserById(PortalId, Me.UserId).DisplayName
            Dim from_email As String = UserController.GetUserById(PortalId, Me.UserId).Email
            Dim staffname As String = UserController.GetUserById(PortalId, theRmb.UserId).DisplayName
            Dim emailaddress As String = UserController.GetUserById(PortalId, theRmb.UserId).Email
            Dim delegateId As Integer = -1
            Try
                If (theRmb.SpareField3 IsNot Nothing) Then delegateId = CInt(theRmb.SpareField3)
            Catch ex As Exception
                delegateId = -1
            End Try
            Dim DelegateName = If(delegateId >= 0, UserController.GetUserById(PortalId, delegateId).DisplayName, "")
            Dim DelegateEmail = If(delegateId >= 0, UserController.GetUserById(PortalId, delegateId).Email, "")
            Dim subject = Translate("EmailApprovedSubjectP").Replace("[RMBNO]", theRmb.RID).Replace("[USERREF]", theRmb.UserRef)
            Dim Emessage = ""

            Emessage = StaffBrokerFunctions.GetTemplate("RmbApprovedEmail", PortalId)
            Emessage = Emessage.Replace("[STAFFNAME]", If(delegateId >= 0, DelegateName & " (" & Translate("OnBehalfOf") & " " & staffname & ")", staffname))
            Emessage = Emessage.Replace("[RMBNO]", theRmb.RID).Replace("[USERREF]", IIf(theRmb.UserRef <> "", theRmb.UserRef, "None"))
            Emessage = Emessage.Replace("[APPROVER]", apprname)
            If theRmb.Changed = True Then
                Emessage = Emessage.Replace("[CHANGES]", ". " & Translate("EmailApproverChanged"))
                theRmb.Changed = False
                SubmitChanges()
            Else
                Emessage = Emessage.Replace("[CHANGES]", "")
            End If
            SendEmail("P2C Reimbursements <" & from_email & ">", emailaddress, DelegateEmail, subject, Emessage)
        End Sub

        Sub SendRejectionLetter(theRmb As AP_Staff_Rmb)
            Dim staffname As String = UserController.GetUserById(PortalId, theRmb.UserId).DisplayName
            Dim from_email As String = UserController.GetUserById(PortalId, Me.UserId).Email
            Dim emailaddress As String = UserController.GetUserById(PortalId, theRmb.UserId).Email
            Dim delegateId As Integer = -1
            Try
                If (theRmb.SpareField3 IsNot Nothing) Then delegateId = CInt(theRmb.SpareField3)
            Catch ex As Exception
                delegateId = -1
            End Try
            Dim DelegateName = If(delegateId >= 0, UserController.GetUserById(PortalId, delegateId).DisplayName, "")
            Dim DelegateEmail = If(delegateId >= 0, UserController.GetUserById(PortalId, delegateId).Email, "")
            Dim apprname As String = UserController.GetUserById(PortalId, Me.UserId).DisplayName
            Dim subject As String = Translate("EmailRejectedSubject").Replace("[RMBNO]", theRmb.RID).Replace("[USERREF]", theRmb.UserRef)
            Dim Emessage As String = StaffBrokerFunctions.GetTemplate("RmbRejectedEmail", PortalId)

            Emessage = Emessage.Replace("[STAFFNAME]", If(delegateId >= 0, DelegateName & " (" & Translate("OnBehalfOf") & " " & staffname & ")", staffname))
            Emessage = Emessage.Replace("[APPROVER]", apprname).Replace("[RMBNO]", theRmb.RID).Replace("[USERREF]", theRmb.UserRef)
            SendEmail("P2C Reimbursements <" & from_email & ">", emailaddress, DelegateEmail, subject, Emessage)
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

        Public Function GetMileageString(ByVal mileage As Integer, unitIndex As String) As String
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

        Public Function TypeHasOriginAndDestination(ByVal typeId As Integer) As Boolean
            Try
                Dim tName = (From c In d.AP_Staff_RmbLineTypes Where c.LineTypeId = typeId Select c.TypeName).First().ToLower()
                If (tName.Equals("airfare")) Then Return True
                If (tName.Equals("mileage")) Then Return True
            Catch
            End Try
            Return False
        End Function

        Public Function IsMileageType(ByVal typeId As Integer) As Boolean
            Try
                Dim tName = (From c In d.AP_Staff_RmbLineTypes Where c.LineTypeId = typeId Select c.TypeName).First().ToLower()
                If (tName.Equals("mileage")) Then Return True
            Catch
            End Try
            Return False
        End Function

        Public Function showSecondTypeRow(ByVal typeId As Integer) As Boolean
            Try
                Dim tName = (From c In d.AP_Staff_RmbLineTypes Where c.LineTypeId = typeId Select c.TypeName).First().ToLower()
                If (tName.Equals("per diem")) And IsAccounts() Then Return True
                If (tName.Equals("meals")) Then Return True
            Catch
            End Try
            Return False
        End Function
        Public Function differentExchangeRate(xRate1 As Double, xRate2 As Double) As Boolean
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
                    Dim shortComment = GetLineComment(line.Comment, line.OrigCurrency, line.OrigCurrencyAmount, line.ShortComment, True, getInitials(theUser), If(line.AP_Staff_RmbLineType.TypeName = "Mileage", GetMileageString(line.Mileage, line.MileageRate), ""))
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
                            rtn &= GetOrderedString(getInitials(theUser) & "-Payment for " & ref,
                                                    Debit, Credit)


                        End If

                    Else
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
                            rtn &= GetOrderedString(getInitials(theUser) & "-Payment for " & ref,
                                                    Debit, Credit)


                        End If
                    End If
                End If
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
            Log(0, LOG_LEVEL_INFO, "Downloading Batched Transactions")

            Dim pendDownload = From c In d.AP_Staff_Rmbs Where downloadStatuses.Contains(c.Status) And c.PortalId = PortalId

            Dim export As String = "Account,Subaccount,Ref,Date," & GetOrderedString("Description", "Debit", "Credit", "Company")
            Dim RmbList As New List(Of Integer)
            For Each Rmb In pendDownload
                Log(Rmb.RID, LOG_LEVEL_INFO, "Downloading Rmb")
                export &= DownloadRmbSingle(Rmb.RMBNo)

                RmbList.Add(Rmb.RMBNo)

            Next

            If (MarkAsProcessed) Then

                If Not RmbList Is Nothing Then
                    Dim q = From c In d.AP_Staff_Rmbs Where RmbList.Contains(c.RMBNo) And c.PortalId = PortalId

                    For Each row In q
                        row.Status = RmbStatus.Processing
                        row.ProcDate = Now
                        Log(row.RID, LOG_LEVEL_INFO, "Marked as Processed - after a manual download")
                    Next
                End If

                d.SubmitChanges()

                If hfRmbNo.Value <> "" Then
                    Await LoadRmbAsync(CInt(hfRmbNo.Value))

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


        Public Function GetProfileImage(ByVal UserId As Integer) As String
            Dim username = UserController.GetUserById(PortalId, UserId).Username
            username = Left(username, Len(username) - 1)
            Return "https://staff.powertochange.org/custom-pages/webService.php?type=staff_photo&api_token=V7qVU7n59743KNVgPdDMr3T8&staff_username=" + username
        End Function

        Protected Async Sub cbMoreInfo_CheckedChanged(sender As Object, e As System.EventArgs) Handles cbMoreInfo.CheckedChanged
            cbApprMoreInfo.Checked = cbMoreInfo.Checked
            Dim theRmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value) And c.PortalId = PortalId
            If theRmb.Count > 0 Then
                saveIfNecessary()
                lblStatus.Text = Translate(RmbStatus.StatusName(theRmb.First.Status))
                If cbMoreInfo.Checked Then
                    SendMoreinfoEmail(Translate("Accounts"), theRmb.First.AcctComment, theRmb.First)
                    ScriptManager.RegisterClientScriptBlock(cbMoreInfo, cbMoreInfo.GetType(), "moreinfo", "alert('" + Translate("MoreInfoMsg") + "');", True)
                    lblStatus.Text = lblStatus.Text & " - " & Translate("StatusMoreInfo")
                    Log(theRmb.First.RID, LOG_LEVEL_INFO, "More info requested by Finance: " + theRmb.First.AcctComment)
                End If
            End If
        End Sub

        Protected Async Sub cbApprMoreInfo_CheckedChanged(sender As Object, e As System.EventArgs) Handles cbApprMoreInfo.CheckedChanged
            cbMoreInfo.Checked = cbApprMoreInfo.Checked
            Dim theRmb = From c In d.AP_Staff_Rmbs Where c.RMBNo = CInt(hfRmbNo.Value) And c.PortalId = PortalId
            If theRmb.Count > 0 Then
                saveIfNecessary()
                lblStatus.Text = Translate(RmbStatus.StatusName(theRmb.First.Status))
                If cbApprMoreInfo.Checked Then
                    Dim approver = UserController.GetUserById(PortalId, UserId).DisplayName
                    SendMoreinfoEmail(approver, theRmb.First.ApprComment, theRmb.First)
                    ScriptManager.RegisterClientScriptBlock(cbApprMoreInfo, cbApprMoreInfo.GetType(), "apprmoreinfo", "alert('" + Translate("MoreInfoMsg") + "');", True)
                    lblStatus.Text = lblStatus.Text & " - " & Translate("StatusMoreInfo")
                    Log(theRmb.First.RID, LOG_LEVEL_INFO, "More info requested by Approver(" + approver + "): " + theRmb.First.ApprComment)
                End If
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
            Log(0, LOG_LEVEL_DEBUG, "Step A")
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

            Log(0, LOG_LEVEL_DEBUG, "Step B")
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

                Actions.Add(GetNextActionID, "Reimbursement Settings", "RmbSettings", "", "action_settings.gif", EditUrl("RmbSettings"), False, SecurityAccessLevel.Edit, True, False)

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
                For Each row In queryResult '--get details of each reimbursement
                    Dim newNode As New TreeNode()
                    newNode.SelectAction = TreeNodeSelectAction.Select
                    'Dim rmbUser = UserController.GetUserById(PortalId, row.UserId).DisplayName
                    newNode.Text = GetRmbTitleTeamShort(row.RID, row.RmbDate, row.Total)
                    newNode.Value = row.RMBNo
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

        Private Function getUnclearedAdvances(ByVal userid As Integer) As IQueryable(Of AP_Staff_RmbLine)
            Dim advance_line_type As Integer = Settings("AdvanceLineType")
            Dim result As IQueryable(Of AP_Staff_RmbLine) = From line In d.AP_Staff_RmbLines
                         Join rmb In d.AP_Staff_Rmbs On line.RmbNo Equals rmb.RMBNo
            Where line.LineType = advance_line_type And (line.Spare2 <> CLEARED) And line.Spare2 <> "0" _
            And rmb.Status <> RmbStatus.Draft And rmb.Status <> RmbStatus.Submitted And rmb.Status <> RmbStatus.PendingDirectorApproval And rmb.Status <> RmbStatus.PendingEDMSApproval And rmb.Status <> RmbStatus.Cancelled _
            And rmb.UserId = userid And rmb.PortalId = PortalId
                            Select line
            Return result
        End Function

        Private Async Function updateAdvanceBalances(lines As IQueryable(Of AP_Staff_RmbLine)) As Task
            If lines Is Nothing Then Return
            For Each line In lines
                Dim ID = (From c In d.AP_Staff_Rmbs Where c.RMBNo = hfRmbNo.Value Select c.RID).Single()
                Try
                    Dim advanceLineNo = Integer.Parse(line.Spare5) 'this is the lineid of the original advance
                    Dim clearingAmount As Double = 0 - line.GrossAmount 'this is the amount to clear (which is saved as a negative number)
                    Dim advanceLine = (From c In d.AP_Staff_RmbLines Where c.RmbLineNo = advanceLineNo).Single()
                    Dim currentBalance = Double.Parse(advanceLine.Spare2) 'this is the outstanding balance
                    Dim newBalance = currentBalance - clearingAmount

                    If (newBalance = 0) Then
                        advanceLine.Spare2 = CLEARED
                        Log(ID, LOG_LEVEL_INFO, "Advance cleared")
                    Else
                        advanceLine.Spare2 = newBalance.ToString()
                        Log(ID, LOG_LEVEL_INFO, "Advance reduced by " & clearingAmount)
                    End If
                Catch ex As Exception
                    Log(ID, LOG_LEVEL_ERROR, "Error updating clearing balance: " & ex.Message)
                End Try
            Next
        End Function

        Private Async Function getAccountBalanceAsync(account As String, logon As String) As Task(Of String)
            'Returns entire text for the "balance" area of the form
            ' account balance for personal accounts or budget numbers for ministry accounts
            lbAccountBalance.Visible = True
            hfAccountBalance.Value = Nothing
            If (account.Equals(String.Empty)) Then
                lbAccountBalance.Visible = False
                Return ""
            End If
            If (logon.Equals(String.Empty)) Then
                hlpAccountBalance.Text = "Error: no logon provided"
                Return Translate("AccountBalance") & "<span title='Error: no logon provided'>" + BALANCE_INCONCLUSIVE + "</span>"
            End If

            Dim service_result = Await StaffRmbFunctions.getAccountBalanceAsync(account, logon)
            If service_result.Equals(StaffRmbFunctions.PERMISSION_DENIED_ERROR) Then
                lbAccountBalance.Visible = False
                Return ""
            End If
            If service_result.Equals(StaffRmbFunctions.WEB_SERVICE_ERROR) Then
                hlpAccountBalance.Text = Translate("WebServiceError")
                Return Translate("AccountBalance") & "<span title='" & service_result & " Try re-loading.'>Error</span>"
            End If
            Try
                If (StaffRmbFunctions.isStaffAccount(account)) Then
                    Dim bal = Double.Parse(service_result, NumberStyles.Currency)
                    valueOrNull(hfAccountBalance, bal)
                    Dim neg = If(bal < 0, "NormalRed", "")
                    hlpAccountBalance.Text = Services.Localization.Localization.GetString("lblAccountBalance.Help", LocalResourceFile)
                    Return Translate("AccountBalance") & "<span class='" & neg & "'>" & service_result & "</span>"
                Else
                    Dim budget_string = service_result.Split(":")(0)
                    Dim actual_string = service_result.Split(":")(1)
                    Dim bud = Double.Parse(budget_string, NumberStyles.Currency)
                    Dim act = Double.Parse(actual_string, NumberStyles.Currency)
                    valueOrNull(hfAccountBalance, act - bud)
                    Dim negb = If(bud < 0, "NormalRed", "")
                    Dim nega = If(act < 0, "NormalRed", "")
                    hlpAccountBalance.Text = Services.Localization.Localization.GetString("lblBudgetBalance.Help", LocalResourceFile)
                    Return Translate("BudgetBalance") & "<span class='" & negb & "'>" & budget_string & "</span>&nbsp;&nbsp;&nbsp;&nbsp;" _
                        & Translate("ActualBalance") & "<span class='" & nega & "'>" & actual_string & "</span>"
                End If
            Catch e As Exception
                hlpAccountBalance.Text = "Error: " + e.Message
                Return Translate("AccountBalance") & "<span title='Error:" + e.Message + " accountBalance result:" + service_result + "'>" + BALANCE_INCONCLUSIVE + "</span>"
            End Try
        End Function


        'Private Async Function getBudgetBalanceAsync(account As String, logon As String) As Task(Of String)
        '    If account = "" Then
        '        Return BALANCE_INCONCLUSIVE
        '    End If
        '    Dim budgetBalance = Await StaffRmbFunctions.getBudgetBalanceAsync(account, logon)
        '    If budgetBalance.Length = 0 Then
        '        Return BALANCE_PERMISSION_DENIED
        '    End If
        '    Try
        '        Double.Parse(budgetBalance)
        '    Catch
        '        Return BALANCE_INCONCLUSIVE
        '    End Try
        '    Return budgetBalance
        'End Function

        Private Sub checkLowBalance()
            If (lblStatus.Text = RmbStatus.StatusName(RmbStatus.Submitted) Or lblStatus.Text = RmbStatus.StatusName(RmbStatus.PendingDirectorApproval) Or lblStatus.Text = RmbStatus.StatusName(RmbStatus.PendingEDMSApproval)) AndAlso isLowBalance() Then
                Try
                    Dim accountBalance = hfAccountBalance.Value - GetTotal(hfRmbNo.Value)
                    lblWarningLabel.Text = Translate("WarningLowBalance").Replace("[ACCTBAL]", Format(accountBalance, "Currency")).Replace("[ACCT]", tbChargeTo.Text)
                    Dim t As Type = Me.GetType()
                    ScriptManager.RegisterClientScriptBlock(Page, t, "", "showWarningDialog();", True)
                Catch
                End Try
            End If
        End Sub

        Private Function isLowBalance() As Boolean
            If isStaffAccount() Or (hfAccountBalance.Value.Equals(String.Empty)) Or (lblAccountBalance.Text.Equals(String.Empty)) Or (lblAccountBalance.Text.Equals(BALANCE_PERMISSION_DENIED)) Or (lblAccountBalance.Text.Equals(BALANCE_INCONCLUSIVE)) Then
                Return False
            End If
            Try
                Return (GetTotal(hfRmbNo.Value) > hfAccountBalance.Value)
            Catch
                Return False
            End Try
        End Function

        Private Sub updateBalanceLabel(accountBalance As String)
            lblAccountBalance.Text = accountBalance
            If (GridView1 IsNot Nothing AndAlso GridView1.FooterRow IsNot Nothing) Then
                Dim control As Label = GridView1.FooterRow.FindControl("lblRemainingBalance")
                If control IsNot Nothing Then
                    If (Trim(accountBalance).Length > 0) Then
                        control.Text = If(isStaffAccount(), Translate("lblRemainingBalance"), Translate("lblRemainingBudget"))
                    Else
                        control.Text = ""
                    End If
                End If
            End If
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

        Private Function hasOldExpenses() As Boolean
            Dim oldest_allowable_date = Today.AddDays(-Settings("Expire"))
            Dim advance_line_type As Integer = Settings("AdvanceLineType")
            Dim old_lines = (From c In d.AP_Staff_RmbLines Where c.RmbNo = hfRmbNo.Value And c.LineType <> advance_line_type And c.TransDate < oldest_allowable_date Select c.TransDate).Count()
            Return old_lines > 0
        End Function

        Private Function updateOutOfDateFlag()
            Dim oldest_allowable_date = Today.AddDays(-Settings("Expire"))
            For Each line In (From c In d.AP_Staff_RmbLines Where c.RmbNo = hfRmbNo.Value And c.TransDate < oldest_allowable_date)
                line.OutOfDate = True
            Next
            d.SubmitChanges()
        End Function

        Private Sub updateReceiptPermissions(ByRef theRmb As AP_Staff_Rmb)
            Dim hasReceipts = (From c In theRmb.AP_Staff_RmbLines Where c.Receipt = True And ((From f In d.AP_Staff_RmbLine_Files Where f.RmbLineNo = c.RmbLineNo).Count = 0)).Count > 0
            If (hasReceipts) Then
                Dim pc As New Permissions.PermissionController
                Dim path = "/_RmbReceipts/" & theRmb.UserId
                Dim theFolder = FolderManager.Instance.GetFolder(PortalId, path)

                DataCache.ClearFolderCache(PortalId) ' Clear the folder cache, to ensure we're getting the most up-to-date folder info
                If FolderManager.Instance.FolderExists(PortalId, path) Then
                    ' Add read permissions for the current approver
                    Dim permission As New Permissions.FolderPermissionInfo()
                    permission.FolderID = theFolder.FolderID
                    permission.PortalID = PortalId
                    permission.PermissionID = pc.GetPermissionByCodeAndKey("SYSTEM_FOLDER", "READ")(0).PermissionID
                    permission.AllowAccess = 1
                    permission.UserID = theRmb.ApprUserId

                    Dim folderPermissions = theFolder.FolderPermissions 'load current permissions
                    folderPermissions.Add(permission, True) 'True prevents duplicates
                    Permissions.FolderPermissionController.SaveFolderPermissions(theFolder)
                End If
            End If
        End Sub

        Private Async Function LoadAddressAsync(UserId As Integer) As Task
            Dim tooltip As String
            Try
                Dim User = StaffBrokerFunctions.GetStaffMember(UserId)
                tooltip = Translate("AddressOnFile") & " " & User.DisplayName + Environment.NewLine
                lblAddressName.Text = User.DisplayName
                lblAddressLine1.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "Address1")
                tooltip += lblAddressLine1.Text & Environment.NewLine
                lblAddressLine2.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "Address2")
                tooltip += lblAddressLine2.Text & Environment.NewLine
                lblCity.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "City")
                tooltip += lblCity.Text & ", "
                lblProvince.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "Province")
                tooltip += lblProvince.Text & ", "
                lblCountry.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "Country")
                tooltip += lblCountry.Text & Environment.NewLine
                lblPostalCode.Text = StaffBrokerFunctions.GetStaffProfileProperty(User, "PostalCode")
                tooltip += lblPostalCode.Text
            Catch
                lblAddressName.Text = ""
                lblAddressLine1.Text = ""
                lblAddressLine2.Text = ""
                lblCity.Text = ""
                lblProvince.Text = ""
                lblCountry.Text = ""
                lblPostalCode.Text = ""
                tooltip = "<no address found>"
            End Try
            If (lblBehalf.Visible) Then
                lblBehalf.ToolTip = tooltip
            Else
                lblSubBy.ToolTip = tooltip
            End If
        End Function

        Private Function getInitials(user As UserInfo) As String
            Dim result As String = ""
            'empty name
            If (String.IsNullOrEmpty(user.DisplayName)) Then Return result
            result = result & user.DisplayName.Substring(0, 1)
            'single character name
            If (Len(user.DisplayName) = 1) Then Return result
            'single word name
            If (Not user.DisplayName.Contains(" ")) Then Return result & user.DisplayName.Substring(1, 1)
            result = result & user.DisplayName.Substring(user.DisplayName.IndexOf(" ") + 1, 1)
            Return result
        End Function

    End Class
End Namespace
