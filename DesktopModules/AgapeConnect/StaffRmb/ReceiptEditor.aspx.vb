Imports DotNetNuke

Imports DotNetNuke.Services.FileSystem
Imports System.Drawing.Imaging
Imports System.Drawing
Imports System.IO
Imports StaffRmb


Partial Class DesktopModules_AgapeConnect_StaffRmb_ReceiptEditor
    Inherits System.Web.UI.Page
    Private imgExt() As String = {"jpg", "jpeg", "gif", "png", "bmp", "pdf"}
    Private LocalResourceFile As String
    ' Variables for file saving
    Private RmbNo As String
    Private RmbLine As String
    Private RecNum As String
    Private theFolder As IFolderInfo
    Private theRmb As AP_Staff_Rmb 
    ' The data context
    Dim d As New StaffRmb.StaffRmbDataContext
    Protected Sub Page_Init(sender As Object, e As System.EventArgs) Handles Me.Init
        Dim PS = CType(HttpContext.Current.Items("PortalSettings"), PortalSettings)




        Dim FileName As String = "StaffRmb"

        'System.IO.Path.GetFileNameWithoutExtension(Me.AppRelativeVirtualPath)

        ' this will fix it when its dynamically loaded using LoadControl method 
        'Me.LocalResourceFile = Me.LocalResourceFile & FileName & ".ascx.resx"
        LocalResourceFile = "/DesktopModules/AgapeConnect/StaffRmb/App_LocalResources/StaffRmb.ascx.resx"



        Dim Locale = PS.CultureCode

        Dim AppLocRes As New System.IO.DirectoryInfo(Server.MapPath(LocalResourceFile.Replace(FileName & ".ascx.resx", "")))
        If Locale = PS.CultureCode Then
            'look for portal varient
            If AppLocRes.GetFiles(FileName & ".ascx.Portal-" & PS.PortalId & ".resx").Count > 0 Then
                LocalResourceFile = LocalResourceFile.Replace("resx", "Portal-" & PS.PortalId & ".resx")
            End If
        Else

            If AppLocRes.GetFiles(FileName & ".ascx." & Locale & ".Portal-" & PS.PortalId & ".resx").Count > 0 Then
                'lookFor a CulturePortalVarient
                LocalResourceFile = LocalResourceFile.Replace("resx", Locale & ".Portal-" & PS.PortalId & ".resx")
            ElseIf AppLocRes.GetFiles(FileName & ".ascx." & Locale & ".resx").Count > 0 Then
                'look for a CultureVarient
                LocalResourceFile = LocalResourceFile.Replace("resx", Locale & ".resx")
            ElseIf AppLocRes.GetFiles(FileName & ".ascx.Portal-" & PS.PortalId & ".resx").Count > 0 Then
                'lookFor a PortalVarient
                LocalResourceFile = LocalResourceFile.Replace("resx", "Portal-" & PS.PortalId & ".resx")
            End If
        End If

    End Sub
    Public Function Translate(ByVal ResourceString As String) As String
        Dim rtn As String
        Try
            rtn = DotNetNuke.Services.Localization.Localization.GetString(ResourceString & ".Text", LocalResourceFile)

        Catch ex As Exception
            rtn = DotNetNuke.Services.Localization.Localization.GetString(ResourceString & ".Text", "/DesktopModules/AgapeConnect/StaffRmb/App_LocalResources/RmbPrintout.ascx.resx")

        End Try

        Return rtn

    End Function

    Protected Async Function CheckFolderPermissions(ByVal PortalId As Integer, ByVal theFolder As IFolderInfo, ByVal theUserId As Integer) As Threading.Tasks.Task
        ' Get the write permission
        Dim pc As New Permissions.PermissionController
        Dim w As Permissions.PermissionInfo = pc.GetPermissionByCodeAndKey("SYSTEM_FOLDER", "WRITE")(0)

        ' Get a list of all the folderPermissions we currently have
        Dim folderPermissions = theFolder.FolderPermissions

        ' Set up the first permission
        Dim permission As New Permissions.FolderPermissionInfo()
        ' Set up some default values for the permission
        initFolderPermission(permission, theFolder.FolderID, PortalId, w.PermissionID)

        ' Set the user id to be this user
        permission.UserID = theUserId
        ' Add folder permissions, with a check for duplicates. 
        ' This duplicate check (the 'True' parameter) will classify this as a "duplicate" if this permission
        ' has the same PermissionID, UserID, and RoleID as a pre-existing one, and not add it if it is a duplicate
        folderPermissions.Add(permission, True)

        ' Get all the possible approvers for this reimbursement
        For Each approver In (Await StaffRmbFunctions.getApproversAsync(theRmb)).UserIds
            ' Create a new permission for this approver
            permission = New Permissions.FolderPermissionInfo()
            ' Initialize all the variables
            initFolderPermission(permission, theFolder.FolderID, PortalId, w.PermissionID)
            ' Set the userid to the approver's id
            permission.UserID = approver.UserID
            ' Add permission for approver
            folderPermissions.Add(permission, True)
        Next

        ' Get the supervisors for this staff member
        Await StaffRmbFunctions.managersInDepartmentAsync(StaffRmbFunctions.logonFromId(PortalId, theUserId))
        For Each leaderId In (StaffBrokerFunctions.GetLeaders(theUserId, False))
            ' Create a new permission for this leader
            permission = New Permissions.FolderPermissionInfo()
            ' Initialize all the variables
            initFolderPermission(permission, theFolder.FolderID, PortalId, w.PermissionID)
            ' Set the userid to the leader's id
            permission.UserID = leaderId
            ' Add permission for leader
            folderPermissions.Add(permission, True)
        Next

        ' Finally, add permissions for the accounts team:
        permission = New Permissions.FolderPermissionInfo()
        ' Initialize new folder permission
        initFolderPermission(permission, theFolder.FolderID, PortalId, w.PermissionID)

        ' Set the role ID
        Dim rc As New DotNetNuke.Security.Roles.RoleController
        permission.RoleID = rc.GetRoleByName(PortalId, "Accounts Team").RoleID
        folderPermissions.Add(permission, True)

        ' Once we're finished adding these folder permissions, save it all
        Permissions.FolderPermissionController.SaveFolderPermissions(theFolder)
    End Function

    ' Simple helper function to initialize a folder permission
    Private Sub initFolderPermission(folderPermission As Permissions.FolderPermissionInfo, ByVal FolderID As Integer, ByVal PortalID As Integer, ByVal PermissionID As Integer)
        folderPermission.FolderID = FolderID
        folderPermission.PortalID = PortalID
        folderPermission.PermissionID = PermissionID
        folderPermission.AllowAccess = 1
    End Sub
    Private Sub AddImage(ByVal file As IFileInfo)
        ' Create the div to contain this new image
        Dim div = New HtmlGenericControl("DIV")
        div.Attributes.Add("style", "display: inline-block; margin: 5px; vertical-align: top;")

        ' Set up the link 
        Dim link = New HyperLink
        'link.BorderStyle = "Solid"
        link.BorderColor = Color.DarkGray
        'link.BorderWidth = "1pt"
        link.Target = "_blank"
        link.Visible = "True"
        link.NavigateUrl = FileManager.Instance.GetUrl(file)
        link.Attributes.Add("style", "display: block")

        ' Set up image 
        Dim img = New HtmlImage
        ' If it's a pdf, need to use the generic pdf button
        If file.Extension.ToLower = "pdf" Then
            img.Src = "images/pdf.png"
            img.Width = 64
        Else ' Otherwise, we just use the same as the navigation url
            img.Src = link.NavigateUrl
            img.Width = 200
            ' IE-specific hack to reload images
            If (Request.Browser.Browser = "InternetExplorer" OrElse Request.Browser.Browser = "IE") Then
                ' Append the timestamp to the img's source
                img.Src = img.Src & "&r=" & DateTime.Now.Ticks
            End If
            ' Also add in the rotation buttons, since these
            ' won't be used when we have a pdf
            div.Controls.Add(createButton("↻", file.FileName))
            div.Controls.Add(createButton("↺", file.FileName))
        End If
        ' TODO At some point, this should be changed to: Translate("OpenNewTab")
        img.Alt = "Click to open fullsize in new tab..."
        ' Provide mouse-over
        img.Attributes("title") = "Click to open fullsize in new tab..."


        ' Add the elements appropriately
        link.Controls.Add(img)
        div.Controls.Add(createButton("✖", file.FileName))
        div.Controls.Add(link)
        currentReceipts.Controls.Add(div)
    End Sub

    Private Function createButton(ByVal Text As String, ByVal FileName As String) As Button
        Dim btn = New Button
        btn.CssClass = "aButton"
        ' btnL.Font-Size = "Small"
        btn.Text = Text
        ' Set the filename attribute of this button to the current value
        btn.Attributes.Add("FileName", FileName)
        ' Set up the proper event handler
        Select Case Text
            Case "↻", "↺" ' Rotation buttons
                AddHandler btn.Click, AddressOf Rotate
            Case "✖"      ' Delete button
                AddHandler btn.Click, AddressOf Delete
        End Select

        Return btn
    End Function

    ' A sub to insert a file - line relationship 
    Private Sub AddFileLine(ByVal File As IFileInfo, ByVal RmbLine As String, ByVal RmbNo As String, ByVal RecNum As String)
        ' First, check to see if we already have a file with this fileid
        Dim existingFiles = (From lf In d.AP_Staff_RmbLine_Files Where lf.FileId = File.FileId)
        If existingFiles.Count > 0 Then ' If we got something back
            Dim ef = existingFiles.First
            ' Update the fields
            If (RmbLine <> "New") Then
                ' Set the line number
                ef.RmbLineNo = RmbLine
            Else
                ' Explicitly set it back to nothing; it'll get set once we've inserted the rmb line
                ef.RmbLineNo = Nothing
            End If
            ef.RMBNo = RmbNo
            ef.RecNum = RecNum
            ' Set the url, using the fully-qualified url for this domain
            ef.URL = Request.Url.Scheme & "://" & Request.Url.Authority & FileManager.Instance.GetUrl(File)
        Else
            ' Didn't have anything; insert new row
            Dim insert As New AP_Staff_RmbLine_File
            insert.FileId = File.FileId
            ' If this isn't a new line
            If (RmbLine <> "New") Then
                ' Set the line number
                insert.RmbLineNo = RmbLine
            End If ' If this IS a new line, the rmbline will get set later
            insert.RMBNo = RmbNo
            insert.RecNum = RecNum
            ' Set the url, using the fully-qualified url
            insert.URL = Request.Url.Scheme & "://" & Request.Url.Authority & FileManager.Instance.GetUrl(File)
            d.AP_Staff_RmbLine_Files.InsertOnSubmit(insert)
        End If
        d.SubmitChanges()

    End Sub

    Protected Async Sub btnUploadReceipt_Click(sender As Object, e As System.EventArgs) Handles btnUploadReceipt.Click
        If fuReceipt.HasFile Then
            Dim Filename As String = fuReceipt.FileName
            Dim ext As String = Filename.Substring(Filename.LastIndexOf(".") + 1)
            If imgExt.Contains(ext.ToLower) Then

                Dim PS = CType(HttpContext.Current.Items("PortalSettings"), PortalSettings)

                Dim fm = FolderMappingController.Instance.GetFolderMapping(PS.PortalId, "Secure")


                If Not FolderManager.Instance.FolderExists(PS.PortalId, "/_RmbReceipts/") Then
                    Dim f1 = FolderManager.Instance.AddFolder(fm, "/_RmbReceipts")
                    Dim rc As New DotNetNuke.Security.Roles.RoleController

                    Dim pc As New Permissions.PermissionController

                    Dim w As Permissions.PermissionInfo = pc.GetPermissionByCodeAndKey("SYSTEM_FOLDER", "WRITE")(0)
                    FolderManager.Instance.SetFolderPermission(f1, w.PermissionID, rc.GetRoleByName(PS.PortalId, "Accounts Team").RoleID)
                End If


                Dim theFolder As IFolderInfo
                Dim path = "/_RmbReceipts/" & theRmb.UserId
                ' Clear the folder cache, to ensure we're getting the most up-to-date folder info
                DataCache.ClearFolderCache(PS.PortalId)
                If FolderManager.Instance.FolderExists(PS.PortalId, path) Then
                    If Not Directory.Exists(Server.MapPath("~/portals/" & PS.PortalId & path)) Then
                        Directory.CreateDirectory(Server.MapPath("~/portals/" & PS.PortalId & path))
                    End If
                    theFolder = FolderManager.Instance.GetFolder(PS.PortalId, path)
                Else
                    theFolder = FolderManager.Instance.AddFolder(fm, path)
                End If

                ' Set the proper folder permissions
                Await CheckFolderPermissions(PS.PortalId, theFolder, theRmb.UserId)
                Dim _theFile As IFileInfo

                If ext.ToLower = "pdf" Then
                    _theFile = FileManager.Instance.AddFile(theFolder, "R" & RmbNo & "L" & RmbLine & "Rec" & RecNum + 1 & ".pdf", fuReceipt.FileContent, True)
                Else



                    Dim img = New Bitmap(fuReceipt.FileContent)
                    Dim newWidth = 1000

                    Dim Quality = 72
                    If (Not img.HorizontalResolution = Nothing) Then
                        newWidth = img.Width * Quality / img.HorizontalResolution
                    End If
                    If (img.Width > 1000) Then
                        newWidth = 1000
                    End If

                    Dim newHeight = newWidth / (img.Width / img.Height)

                    Dim result = New Bitmap(newWidth, newHeight, Drawing.Imaging.PixelFormat.Format32bppPArgb)
                    result.SetResolution(img.HorizontalResolution, img.VerticalResolution)
                    Dim g = Graphics.FromImage(result)
                    g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic


                    g.DrawImage(img, New Rectangle(0, 0, newWidth, newHeight), New Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel)

                    Dim myMemoryStream As New IO.MemoryStream
                    '  Dim codecInfo = ImageUtils.GetEncoderInfo("Jpeg")


                    '  Dim parameters As EncoderParameters = New EncoderParameters(1)
                    '  parameters.Param(0) = New EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 10L)

                    result.Save(myMemoryStream, ImageFormat.Jpeg)
                    result.Dispose()
                    g.Dispose()



                    _theFile = FileManager.Instance.AddFile(theFolder, "R" & RmbNo & "L" & RmbLine & "Rec" & RecNum + 1 & ".jpg", myMemoryStream, True)
                    myMemoryStream.Dispose()
                End If

                ' Add the image to the page
                AddImage(_theFile)
                ' Increment the RecNum
                RecNum += 1

                ' Add this file-rmb line relationship to the database
                AddFileLine(_theFile, RmbLine, RmbNo, RecNum)
            Else
                'Not image file
                lblError.Text = "* File must end in .jpg, .jpeg, .gif, .png, .bmp or .pdf<br />"
            End If
        End If
    End Sub


    Private Sub Delete(sender As Object, e As System.EventArgs)
        ' Get the button that caused the event
        Dim b As Button = CType(sender, Button)

        ' Get the file for this receipt
        Dim theFile = FileManager.Instance.GetFile(theFolder, b.Attributes("FileName"))

        ' Get the parent of the parent of this button; this represents the div
        ' containing all of the receipts for this line
        Dim receipts = b.Parent.Parent
        ' Remove the div for this receipt
        receipts.Controls.Remove(b.Parent)

        ' Delete the file; deletes it from the filesystem as well as the db, and
        ' cascades down to the line-file table
        FileManager.Instance.DeleteFile(theFile)
    End Sub

    Private Sub Rotate(sender As Object, e As System.EventArgs)
        Try
            ' Get access to the button that caused this event
            Dim b As Button = CType(sender, Button)
            ' Get current file
            Dim theFile = FileManager.Instance.GetFile(theFolder, b.Attributes("FileName"))

            Dim img = New Bitmap(theFile.PhysicalPath & ".resources")

            ' If this is the left rotation
            If (b.Text = "↻") Then
                img.RotateFlip(RotateFlipType.Rotate90FlipNone)
            ElseIf (b.Text = "↺") Then ' Right rotation
                img.RotateFlip(RotateFlipType.Rotate270FlipNone)
            Else ' This shouldn't ever happen
                Throw New Exception
            End If
            Dim newWidth = img.Width
            Dim newHeight = img.Height

            Dim myMemoryStream As New IO.MemoryStream
            img.Save(myMemoryStream, ImageFormat.Jpeg)

            img.Dispose()

            ' Replace the image with the rotated version
            FileManager.Instance.AddFile(theFolder, theFile.FileName, myMemoryStream, True)
            myMemoryStream.Dispose()
            ' A little hack to force internet explorer to reload the image; otherwise, it 
            ' won't show the rotation
            If (Request.Browser.Browser = "InternetExplorer" OrElse Request.Browser.Browser = "IE") Then
                Try
                    ' Get the html image
                    Dim htmlImage = CType(b.Parent.Controls.Item(3).Controls.Item(0), HtmlImage)
                    ' Append timestamp to image's url, forcing IE to reload it
                    htmlImage.Src = htmlImage.Src & "&r=" & DateTime.Now.Ticks
                Catch Exception As Exception
                    lblError.Text = "The image was probably rotated successfully, but the view could not be updated properly"
                End Try
            End If

        Catch Exception As Exception
            lblError.Text = "Error while attempting to rotate image"
        End Try
    End Sub

    Protected Async Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
        ' Get the reimbursement and line number
        RmbNo = Request.QueryString("RmbNo")
        RmbLine = Request.QueryString("RmbLine")
        ' Set the receipt number to 0 (We don't have any yet)
        RecNum = 0
        Try
            Dim PS = CType(HttpContext.Current.Items("PortalSettings"), PortalSettings)

            ' Set the rmb for this receipt
            theRmb = (From c In d.AP_Staff_Rmbs Where c.PortalId = PS.PortalId And c.RMBNo = RmbNo).First
            ' Clear folder cache, to make sure we're getting the up-to-date folder info
            DataCache.ClearFolderCache(PS.PortalId)
            ' Try to get the folder; if this fails, we'll throw an exception and break out of this block
            theFolder = FolderManager.Instance.GetFolder(PS.PortalId, "/_RmbReceipts/" & theRmb.UserId)
            ' Set the proper folder permissions
            Await CheckFolderPermissions(PS.PortalId, theFolder, theRmb.UserId)
            Dim theFiles As Object
            ' If this isn't a new line we're creating
            If (RmbLine <> "New") Then
                ' Get all of the files associated with this existing rmb line
                theFiles = (From lf In d.AP_Staff_RmbLine_Files Where lf.RmbLineNo = RmbLine And lf.RMBNo = RmbNo Order By RecNum)
            Else
                ' Get all of the ones for this reimbursement that don't have a line yet
                theFiles = (From lf In d.AP_Staff_RmbLine_Files Where lf.RmbLineNo Is Nothing And lf.RMBNo = RmbNo Order By RecNum)
            End If

            For Each line_file As AP_Staff_RmbLine_File In theFiles
                ' Keep re-setting the receipt number to the latest one
                RecNum = line_file.RecNum
                ' Get the actual file
                Dim file = FileManager.Instance.GetFile(line_file.FileId)
                ' Add each of the files to the page
                AddImage(file)
            Next

        Catch ex As Exception

        End Try




    End Sub
End Class
