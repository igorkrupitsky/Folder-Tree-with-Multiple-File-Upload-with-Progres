<%@ Page Language="vb" CodeFile="Upload.aspx.vb" Inherits="Upload" %>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1">

    <title>File upload</title>

    <script src="Upload.js"></script>
    <link rel="stylesheet" type="text/css" media="all" href="Upload.css?v=1" />
    <script src="jquery/jquery-1.12.3.min.js"></script>
    <link href="bootstrap/3.3.6/css/bootstrap.min.css" rel="stylesheet">
    <script src="bootstrap/3.3.6/js/bootstrap.min.js"></script>
    <script src="external/datatables.min.js"></script>
    <link href="external/datatables.min.css" rel="stylesheet" />
</head>
<body onload="OnLoad()">
<form id="form1" action="Upload.aspx?folder=<%=Request.QueryString("folder")%>" method="POST" enctype="multipart/form-data">
 <div class="container">
     <div class="row" id="divUploadPanel">
    
            <div class="col-xs-10">

                <div class="form-group">
                    <label for="file1">Files to upload:</label>

                        <span class="glyphicon glyphicon-remove-circle" style="cursor: pointer;" title="Close"
                            onclick="$('#divUploadPanel').hide('slow')" 
                            onmouseover="$('#divUploadPanel').css('background-color','#ebf4e8')"  
                            onmouseout="$('#divUploadPanel').css('background-color','white')"></span>

                    <input class="form-control" type="file" id="file1" name="file1" multiple="multiple" />
                </div>

                <% If Request.Browser.IsBrowser("InternetExplorer") = False OrElse CInt(Request.Browser.Version) > 10 Then%>
                <div id="divDropHere" style="padding: 40px">or drop files here</div>
                <% End If%>
            </div>

            <div class="col-xs-2 " style="padding-top: 12px;">

                <div class="btn-group-vertical">
                    <input class="btn btn-default" type="button" value="Refresh" onclick="location = location.href" />
                    <button class="btn btn-default" type="button" name="btnDelete" id="btnDelete" style="display: none;" onclick="Delete()">Delete</button>
                    <button class="btn btn-default" id="btnUpload" type="submit">Upload files</button>
                    <button class="btn btn-default" type="button"  onclick="DownloadAll()">Download all</button>
                </div>
                
                <div class="checkbox">
                    <label title="Extract zip files after upload"><input name="chkUnzip" type="checkbox" /> Expand zip files</label>
                </div>

 
            </div>

         </div>

        <div class="row">
            <div class="col-xs-12" id="divTbl">

                <div id="divStatus"></div>

                <%ShowFiles()%>

            </div>
        </div>

</div>
<input type="hidden" name="hdnAction" />
</form>
</body>
</html>
