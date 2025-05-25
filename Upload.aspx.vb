Public Class Upload
	Inherits System.Web.UI.Page

	Dim sFolder As String = "upload"

	Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

		If Request.QueryString("folder") <> "" Then
			sFolder = Request.QueryString("folder")
		End If

		Dim sFolderPath As String = Server.MapPath(sFolder)
		If System.IO.Directory.Exists(sFolderPath) = False Then
			Response.Write("Folder does not exist: " & sFolderPath)
			Response.End()
		End If

		If Request.QueryString("subfolder") <> "" Then
			GetSubFolder(Request.QueryString("subfolder"), Request.QueryString("indent"), Request.QueryString("folderId"), Request.QueryString("parentIds"))
			Response.End()
		End If

		If Request.HttpMethod = "POST" Then

			If Request.Form("hdnAction") = "Delete" Then
				'Delete files
				If (Not Request.Form.GetValues("chkDelete") Is Nothing) Then
					For i As Integer = 0 To Request.Form.GetValues("chkDelete").Length - 1
						Dim sFileName As String = Request.Form.GetValues("chkDelete")(i)

						Try
							System.IO.File.Delete(sFolderPath & "\" & sFileName)
						Catch ex As Exception
							'Ignore error
							Throw New Exception("Could not delete: " & sFolderPath & "\" & sFileName)
						End Try
					Next
				End If

				'Delete Folders
				If (Not Request.Form.GetValues("chkDeleteFolder") Is Nothing) Then
					For i As Integer = 0 To Request.Form.GetValues("chkDeleteFolder").Length - 1
						Dim sFolder As String = Request.Form.GetValues("chkDeleteFolder")(i)

						Try
							ClearFolder(sFolder)
							System.IO.Directory.Delete(sFolder)
						Catch ex As Exception
							'Ignore error
						End Try
					Next
				End If

			ElseIf Request.Form("hdnAction") = "DownloadAll" Then
				DownloadAll(sFolderPath)

			Else
				'Upload Files
				For i As Integer = 0 To Request.Files.Count - 1
					Dim oFile As System.Web.HttpPostedFile = Request.Files(i)
					Dim sFileName As String = System.IO.Path.GetFileName(oFile.FileName)

					If Request.Form("chkUnzip") <> "" AndAlso GetExtFromFileName(sFileName).ToLower() = "zip" Then
						Dim sTempZipFilePath As String = sFolderPath & "\" & System.Guid.NewGuid().ToString("N") + ".zip"
						oFile.SaveAs(sTempZipFilePath)
						Dim oZip As New ICSharpCode.SharpZipLib.Zip.FastZip
						oZip.ExtractZip(sTempZipFilePath, sFolderPath, Nothing)
						System.IO.File.Delete(sTempZipFilePath)

					Else
						oFile.SaveAs(sFolderPath & "\" & sFileName)
					End If

				Next
			End If

		End If

	End Sub

	Private Sub DownloadAll(ByVal sFolderPath As String)
		Dim oMemoryStream As New IO.MemoryStream()
		Dim oZipFile As ICSharpCode.SharpZipLib.Zip.ZipOutputStream = New ICSharpCode.SharpZipLib.Zip.ZipOutputStream(oMemoryStream)
		AddFilesToZip(oZipFile, sFolderPath, sFolderPath)
		oZipFile.Finish()

		Response.ContentType = "application/x-zip-compressed"
		Response.AddHeader("Content-Disposition", "attachment; filename=All.zip")
		Response.BinaryWrite(oMemoryStream.ToArray())

		oMemoryStream.Close()
		oZipFile.Close()
	End Sub

	Private oFilesInZip As New Hashtable

	Private Sub AddFilesToZip(ByRef oZipFile As ICSharpCode.SharpZipLib.Zip.ZipOutputStream, ByVal sFolderName As String, ByVal sBaseFolderName As String)
		Dim oFiles As String() = IO.Directory.GetFiles(sFolderName)
		For Each sFileName As String In oFiles
			Dim oFileInfo As New IO.FileInfo(sFileName)
			Dim entryName As String = sFileName.Replace(sBaseFolderName, "")
			AddFileToZip(oZipFile, sFileName, entryName)
		Next

		Dim oSubFolders As String() = IO.Directory.GetDirectories(sFolderName)
		For Each sSubFolderName As String In oSubFolders
			AddFilesToZip(oZipFile, sSubFolderName, sBaseFolderName)
		Next
	End Sub

	Private Sub AddFileToZip(ByRef oZipFile As ICSharpCode.SharpZipLib.Zip.ZipOutputStream, ByVal sFileName As String, ByVal sEntryName As String)
		sEntryName = ICSharpCode.SharpZipLib.Zip.ZipEntry.CleanName(sEntryName)

		'Exit If file already in zip
		If oFilesInZip.ContainsKey(sEntryName) Then
			Exit Sub
		Else
			oFilesInZip.Add(sEntryName, "1")
		End If

		Dim fi As New IO.FileInfo(sFileName)
		Dim newEntry As New ICSharpCode.SharpZipLib.Zip.ZipEntry(sEntryName)

		newEntry.DateTime = fi.LastWriteTime
		newEntry.Size = fi.Length

		oZipFile.PutNextEntry(newEntry)
		Dim buffer As Byte() = New Byte(4095) {}
		Using streamReader As IO.FileStream = IO.File.OpenRead(sFileName)
			ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(streamReader, oZipFile, buffer)
		End Using
		oZipFile.CloseEntry()
	End Sub

	Private Sub ClearFolder(ByVal FolderName As String)
		Dim dir As System.IO.DirectoryInfo = New System.IO.DirectoryInfo(FolderName)

		For Each fi As System.IO.FileInfo In dir.GetFiles()
			fi.IsReadOnly = False
			fi.Delete()
		Next

		For Each di As System.IO.DirectoryInfo In dir.GetDirectories()
			ClearFolder(di.FullName)
			di.Delete()
		Next
	End Sub

	Private Sub GetSubFolder(ByVal sSubFolderPath As String, ByVal iIndent As Integer, ByVal sFolderId As String, ByVal sParentIds As String)
		Dim sFolderIds As String = sFolderId
		If sParentIds <> "" Then
			sFolderIds = sParentIds & "," & sFolderId
		End If

		Dim sIndentCss As String = " style='padding-left:" & (iIndent * 15) & "px' "

		Dim sUploadFolderPath As String = Server.MapPath(sFolder)
		Dim sFolderPath As String = Server.MapPath(".")
		Dim sSubFolderName As String = Replace(Replace(sSubFolderPath, sFolderPath & "\", ""), "\", "/")
		Dim oFiles As String() = System.IO.Directory.GetFiles(sSubFolderPath)

		For i As Integer = 0 To oFiles.Length - 1
			Dim sFilePath As String = oFiles(i)
			Dim oFileInfo As New System.IO.FileInfo(sFilePath)
			Dim sFileName As String = oFileInfo.Name
			Dim sSize As String = FormatNumber((oFileInfo.Length / 1024), 0)
			If sSize = "0" AndAlso oFileInfo.Length > 0 Then sSize = "1"

			Dim sImg As String = GetFileImg(sFileName)

			Response.Write("<tr class='" & GetClassNameFromId(sFolderIds) & "' data-type='File' data-parent-id=""" & sFolderId & """ data-parent-ids=""" & sFolderIds & """>")
			Response.Write("<td" & sIndentCss & ">")
			'Response.Write("<span style='display:none'>" & sSubFolderPath & "</span>") ' for datatable sorting
			Response.Write("<span style='display:none'>File</span>")
			Response.Write("<img class='File' src='images/ext/" & sImg & "'> ")
			Response.Write("<a href=""" & sSubFolderName & "/" & sFileName & """ target='_blank'>" & sFileName + "</a></td>")
			Response.Write("<td>" & sSize & " KB</td>")
			Response.Write("<td>" & oFileInfo.LastWriteTime.ToShortDateString() & " " & oFileInfo.LastWriteTime.ToShortTimeString() & "</td>")
			Response.Write("<td><input type=checkbox name=chkDelete value=""" & Replace(sFilePath, sUploadFolderPath & "\", "") & """>")
			Response.Write("</tr>")
		Next

		Dim oFolders As String() = System.IO.Directory.GetDirectories(sSubFolderPath)
		For i As Integer = 0 To oFolders.Length - 1
			Dim sSubSubFolderPath As String = oFolders(i)
			Dim oFolderInfo As New IO.DirectoryInfo(sSubSubFolderPath)
			Response.Write("<tr  class='" & GetClassNameFromId(sFolderIds) & "' data-type='Folder' data-indent='" & iIndent & "' data-parent-id=""" & sFolderId & """ data-parent-ids=""" & sFolderIds & """ data-folder=""" & sSubSubFolderPath & """>")
			Response.Write("<td" & sIndentCss & ">")
			'Response.Write("<span style='display:none'>" & sSubSubFolderPath & "</span>") ' for datatable sorting
			Response.Write("<span style='display:none'>Folder</span>")
			Response.Write("<img src='images/plus.gif'class='Plus' onclick='ExpandFolder(this)'> ")
			Response.Write("<img class='Folder' src='images/folder_closed.gif' onclick='ExpandFolder(this)'> ")
			Response.Write("<span onclick='ExpandFolder(this)' class='FolderName'>" & oFolderInfo.Name + "</span></td>")
			Response.Write("<td></td>")
			Response.Write("<td>" & oFolderInfo.LastWriteTime.ToShortDateString() & " " & oFolderInfo.LastWriteTime.ToShortTimeString() & "</td>")
			Response.Write("<td><input type=checkbox name=chkDeleteFolder value=""" & sSubSubFolderPath & """>")
			Response.Write("</tr>")
		Next

	End Sub

	Function GetClassNameFromId(ByVal sFolderIds As String) As String
		Dim sRet As String = ""
		Dim oFolderIds As String() = sFolderIds.Split(",")
		For i As Integer = 0 To oFolderIds.Length - 1
			sRet += " p" & oFolderIds(i)
		Next

		Return Trim(sRet)
	End Function

	Public Sub ShowFiles()
		Dim sFolderPath As String = Server.MapPath(sFolder)
		Dim oFiles As String() = System.IO.Directory.GetFiles(sFolderPath)
		Dim oFolders As String() = System.IO.Directory.GetDirectories(sFolderPath)

		If oFiles.Length = 0 And oFolders.Length = 0 Then
			Exit Sub
		End If

		Response.Write("<table id='tbServer' class='table table-striped'>" & vbCrLf)
		Response.Write("<thead><tr>" & vbCrLf)
		Response.Write("<th>File name</th>")
		Response.Write("<th>Size</th>")
		Response.Write("<th>Date Modified</th>")
		Response.Write("<th><label><input type=checkbox name=chkDeleteAll onclick='DeleteAll(this)'> Delete</label></th></tr></thead><tbody>")

		For i As Integer = 0 To oFiles.Length - 1
			Dim sFilePath As String = oFiles(i)
			Dim oFileInfo As New System.IO.FileInfo(sFilePath)
			Dim sFileName As String = oFileInfo.Name
			Dim sSize As String = FormatNumber((oFileInfo.Length / 1024), 0)
			If sSize = "0" AndAlso oFileInfo.Length > 0 Then sSize = "1"

			Dim sImg As String = GetFileImg(sFileName)

			Response.Write("<tr data-type='File'>")
			Response.Write("<td><span style='display:none'>File</span><img class='File' src='images/ext/" & sImg & "'> ")
			Response.Write("<a href=""" & sFolder & "/" & sFileName & """ target='_blank'>" & sFileName + "</a></td>")
			Response.Write("<td>" & sSize & " KB</td>")
			Response.Write("<td>" & oFileInfo.LastWriteTime.ToShortDateString() & " " & oFileInfo.LastWriteTime.ToShortTimeString() & "</td>")
			Response.Write("<td><input type=checkbox name=chkDelete value=""" & sFileName & """>")
			Response.Write("</tr>")
		Next

		For i As Integer = 0 To oFolders.Length - 1
			Dim sSubFolderPath As String = oFolders(i)
			Dim oFolderInfo As New IO.DirectoryInfo(sSubFolderPath)
			Response.Write("<tr data-type='Folder' data-indent='0' data-folder=""" & sSubFolderPath & """>")
			Response.Write("<td>")
			'Response.Write("<span style='display:none'>" & sSubFolderPath & "</span>") ' for datatable sorting
			Response.Write("<span style='display:none'>Folder</span>")
			Response.Write("<img src='images/plus.gif'class='Plus' onclick='ExpandFolder(this)'> ")
			Response.Write("<img class='Folder' src='images/folder_closed.gif' onclick='ExpandFolder(this)'> ")
			Response.Write("<span onclick='ExpandFolder(this)' class='FolderName'>" & oFolderInfo.Name + "</span></td>")
			Response.Write("<td></td>")
			Response.Write("<td>" & oFolderInfo.LastWriteTime.ToShortDateString() & " " & oFolderInfo.LastWriteTime.ToShortTimeString() & "</td>")
			Response.Write("<td><input type=checkbox name=chkDeleteFolder value=""" & sSubFolderPath & """>")
			Response.Write("</tr>")
		Next

		Response.Write("</tbody></table>")
	End Sub

	Private Function GetFileImg(sFileName As String) As String
		Dim sFileExt As String = GetExtFromFileName(sFileName)
		Return GetFileExtImg(sFileExt)
	End Function

	Private Function GetFileExtImg(sFileExt As String) As String
		Select Case LCase(Trim(sFileExt))
			Case "bmp" : Return "bmp.gif"
			Case "tif", "tiff" : Return "tif.png"
			Case "doc", "rtf" : Return "doc.gif"
			Case "exe", "bat" : Return "exe.gif"
			Case "gif", "jpg", "png" : Return "gif.gif"
			Case "htm", "tml" : Return "htm.gif"
			Case "mdb" : Return "mdb.gif"
			Case "mp3", "mpg", "avi", "mid" : Return "mp3.gif"
			Case "mpp" : Return "mpp.gif"
			Case "pdf" : Return "pdf.gif"
			Case "ppt" : Return "ppt.gif"
			Case "rpt" : Return "rpt.gif"
			Case "txt" : Return "txt.gif"
			Case "xls", "csv", "lsx" : Return "xls.gif"
			Case "xml" : Return "xml.gif"
			Case "zip", "cab" : Return "zip.gif"
			Case "eml" : Return "eml.gif"
			Case "swf" : Return "swf.gif"
			Case "vsd" : Return "vsd.gif"
			Case "xlt" : Return "xlt.gif"

			Case "xls", "xlsx" : Return "xls.gif"
			Case "doc", "docx" : Return "doc.gif"
			Case "ppt", "pptx" : Return "ppt.gif"

			Case "msg" : Return "ml.gif"

			Case Else : Return "all.gif"
		End Select
	End Function

	Private Function GetExtFromFileName(ByVal s As String) As String
		If s = "" Then
			Return ""
		End If

		Dim iPos As Integer = s.LastIndexOf(".")
		If iPos = -1 Then
			Return ""
		End If

		Return s.Substring(iPos + 1)
	End Function

End Class