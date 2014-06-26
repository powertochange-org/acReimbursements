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

    Protected Sub CheckFolderPermissions(ByVal PortalId As Integer, ByVal theFolder As IFolderInfo, ByVal theUserId As Integer)

        ' Before we mess around with any folder permissions, clear the caches. 
        ' This should eliminate some issues we were having with the receipt uploader
        DataCache.ClearFolderCache(PortalId)
        DataCache.ClearFolderPermissionsCache(PortalId)

        Try

       
        Dim rc As New DotNetNuke.Security.Roles.RoleController

        Dim pc As New Permissions.PermissionController
        Dim w As Permissions.PermissionInfo = pc.GetPermissionByCodeAndKey("SYSTEM_FOLDER", "WRITE")(0)
        Dim r As Permissions.PermissionInfo = pc.GetPermissionByCodeAndKey("SYSTEM_FOLDER", "READ")(0)
        FolderManager.Instance.SetFolderPermission(theFolder, w.PermissionID, Nothing, theUserId)
        FolderManager.Instance.SetFolderPermission(theFolder, w.PermissionID, rc.GetRoleByName(PortalId, "Accounts Team").RoleID)

        ' If Not (Permissions.FolderPermissionController.HasFolderPermission(PortalId, theFolder.FolderPath, "READ")) Then
        FolderManager.Instance.SetFolderPermission(theFolder, w.PermissionID, Nothing, UserController.GetCurrentUserInfo.UserID)
        For Each row In StaffBrokerFunctions.GetLeaders(UserController.GetCurrentUserInfo.UserID, True).Distinct()


            FolderManager.Instance.SetFolderPermission(theFolder, w.PermissionID, Nothing, row)
        Next
        'End If


        Catch ex As Exception

        End Try

    End Sub
    Private Sub AddImage(ByVal NavigateUrl As String, Optional ByVal ext As String = "jpg")
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
        link.NavigateUrl = NavigateUrl
        link.Attributes.Add("style", "display: block")

        ' Set up image 
        Dim img = New HtmlImage
        ' If it's a pdf, need to use the generic pdf button
        if ext.tolower = "pdf"
            img.Src = "\images\ButtonImages\pdf.png"
        Else ' Otherwise, we just use the same as the navigation url
            img.Src = NavigateUrl
            ' Also add in the rotation buttons, since these
            ' won't be used when we have a pdf
            div.Controls.Add(createButton("↻", "R" & RmbNo & "L" & RmbLine & "Rec" & RecNum & "." & ext))
            div.Controls.Add(createButton("↺", "R" & RmbNo & "L" & RmbLine & "Rec" & RecNum & "." & ext))
        End if
        ' TODO At some point, this should be changed to: Translate("OpenNewTab")
        img.Alt = "Click to open fullsize in new tab..."
        ' Provide mouse-over
        img.Attributes("title") = "Click to open fullsize in new tab..."
        img.Width = 200

        ' Add the elements appropriately
        link.Controls.Add(img)
        div.Controls.Add(createButton("✖", "R" & RmbNo & "L" & RmbLine & "Rec" & RecNum & "." & ext))
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
    Private Sub AddFileLine(ByVal FileId As Integer, ByVal RmbLine As String, ByVal RmbNo As String, ByVal RecNum As String)
        ' First, check to see if we already have a file with this fileid
        Dim existingFiles = (From lf In d.AP_Staff_RmbLine_Files Where lf.FileId = FileId)
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
        Else
            ' Didn't have anything; insert new row
            Dim insert As New AP_Staff_RmbLine_File
            insert.FileId = FileId
            ' If this isn't a new line
            If (RmbLine <> "New") Then
                ' Set the line number
                insert.RmbLineNo = RmbLine
            End If ' If this IS a new line, the rmbline will get set later
            insert.RMBNo = RmbNo
            insert.RecNum = RecNum
            d.AP_Staff_RmbLine_Files.InsertOnSubmit(insert)
        End If
        d.SubmitChanges()

    End Sub

    Protected Sub btnUploadReceipt_Click(sender As Object, e As System.EventArgs) Handles btnUploadReceipt.Click
        If fuReceipt.HasFile Then
            Dim Filename As String = fuReceipt.FileName
            Dim ext As String = Filename.Substring(Filename.LastIndexOf(".") + 1)
            If imgExt.Contains(ext.ToLower) Then

                Dim PS = CType(HttpContext.Current.Items("PortalSettings"), PortalSettings)

                Dim theRmb = (From c In d.AP_Staff_Rmbs Where c.PortalId = PS.PortalId And c.RMBNo = RmbNo).First


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
                If FolderManager.Instance.FolderExists(PS.PortalId, path) Then
                    theFolder = FolderManager.Instance.GetFolder(PS.PortalId, path)
                Else

                    theFolder = FolderManager.Instance.AddFolder(fm, path)
                End If


                CheckFolderPermissions(PS.PortalId, theFolder, theRmb.UserId)
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
                End if

                ' Add the image to the page
                AddImage(FileManager.Instance.GetUrl(_theFile), _theFile.Extension)
                ' Increment the RecNum
                RecNum += 1

                ' Add this file-rmb line relationship to the database
                AddFileLine(_theFile.FileId, RmbLine, RmbNo, RecNum)








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
        Catch Exception As Exception
            lblError.Text = "Error while attempting to rotate image"
        End Try
    End Sub

    Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
        ' Get the reimbursement and line number
        RmbNo = Request.QueryString("RmbNo")
        RmbLine = Request.QueryString("RmbLine")
        ' Set the receipt number to 0 (We don't have any yet)
        RecNum = 0
        Try
            Dim PS = CType(HttpContext.Current.Items("PortalSettings"), PortalSettings)

            Dim theRmb = (From c In d.AP_Staff_Rmbs Where c.PortalId = PS.PortalId And c.RMBNo = RmbNo).First


            theFolder = FolderManager.Instance.GetFolder(PS.PortalId, "/_RmbReceipts/" & theRmb.UserId)

            CheckFolderPermissions(PS.PortalId, theFolder, theRmb.UserId)



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
                AddImage(FileManager.Instance.GetUrl(file), file.Extension)
            Next

        Catch ex As Exception

        End Try




    End Sub
End Class
