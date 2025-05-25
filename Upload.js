var iFiles = 0;
var iDoneFiles = 0;
var iFolderId = 0;
var oTable = null;
var bTableDirty = false;

function OnLoad() {

	_("file1").addEventListener("change", FileSelectHandler, false);

	var xhr = new XMLHttpRequest();
	if (xhr.upload) {
		
		var filedrag = _("divDropHere");
		if (filedrag){
			filedrag.addEventListener("dragover", FileDragHover, false);
			filedrag.addEventListener("dragleave", FileDragHover, false);
			filedrag.addEventListener("drop", FileSelectHandler, false);
			filedrag.style.display = "block";
		}

		_("btnUpload").style.display = "none";
	}

	SetupDataTable();
	SetupDeleteBtn();
}

function SetupDataTable() {
	var h = $(window).height() - 310;
	oTable = $('#tbServer').DataTable({
		scrollY: h,
		scrollCollapse: false,
		paging: false,
		searching: true,
		ordering: true,
		order: [],
		"scrollX": true,
		info: false
	});

	$('.dataTables_scrollHeadInner').mousedown(function (e) {
		CleanDirtyTable()
	});

	$("input[type='search']").focus(function () {
		CleanDirtyTable();
		$("input[type='search']").focus();
	})
}

function CleanDirtyTable() {
	if (bTableDirty) {
		ResetDataTable();
		SetupDeleteBtn();
		bTableDirty = false;
	}
}

function Delete() {
	if (confirm("Delete?")) {
		form1.hdnAction.value = "Delete";
		form1.submit();
	}
}

function DownloadAll() {
	if (confirm("Download all as one zip file?")) {
		form1.hdnAction.value = "DownloadAll";
		form1.submit();
	}
}

function SetupDeleteBtn() {
	$("input[name='chkDelete'],input[name='chkDeleteFolder'],input[name='chkDeleteAll']").click(function () {
		$("#btnDelete").show("slow");
	})
}

function FileDragHover(e) {
	e.stopPropagation();
	e.preventDefault();
	e.target.className = (e.type=="dragover")?"hover":"";
}

function FileSelectHandler(e) {
	FileDragHover(e);

	var oFiles = e.target.files || e.dataTransfer.files;
	if (oFiles.length == 0) return;

	var sHtml = "";

	for (var i = 0; i < oFiles.length; i++) {
		var iSize = oFiles[i].size;
		var sName = oFiles[i].name;
		sHtml += "<tr><td>" + sName + "</td>"
			       + "<td>" + (iSize / 1024).formatNumber(0, ',', '.') + " KB</td>"
		            + "<td id=progressBar" + i + "></td></tr>";
	}

	if (sHtml != "") {
		_("divStatus").innerHTML = "<table border=0 class='table table-striped'>" + sHtml + "</table>";
	}

	iFiles = oFiles.length;

	for (var i = 0; i < oFiles.length; i++) {
		UploadFile(oFiles[i], i);
	}
}

function UploadFile(file, i) {
	var xhr = new XMLHttpRequest();
	if (xhr.upload) {
		var progress = _("progressBar" + i).appendChild(document.createElement("div"));
		progress.className = "progressBar";
		progress.innerHTML = "&nbsp;";

		// progress bar
		xhr.upload.addEventListener("progress", function (e) {
			var pc = parseInt(100 - (e.loaded / e.total * 100));
			progress.style.backgroundPosition = pc + "% 0";
		}, false);

		// file received/failed
		xhr.onreadystatechange = function (e) {
			if (xhr.readyState == 4) {
				progress.className = "progressBar " + (xhr.status == 200 ? "progressSuccess" : "progressFailed");
				if (xhr.status == 200) {
					iDoneFiles += 1;
					if (iFiles == iDoneFiles) {
						//upload done: refresh
						location = location.href;
						return;
					}
				}
			}
		};

		var oFormData = new FormData();
		oFormData.append("myfile" + i, file);
		oFormData.append("chkUnzip", form1.chkUnzip.checked ? "1": "");
		xhr.open("POST", _("form1").action, true);
		xhr.send(oFormData);
	}
}

function DeleteAll(o){
	var oBoxes = document.getElementsByTagName("input");
	for (var i=1; i<oBoxes.length; i++){
		oBoxes[i].checked = o.checked;
	}
}

function _(id) {
	return document.getElementById(id);
}

Number.prototype.formatNumber = function(decPlaces, thouSeparator, decSeparator) {
    var n = this,
        decPlaces = isNaN(decPlaces = Math.abs(decPlaces)) ? 2 : decPlaces,
        decSeparator = decSeparator == undefined ? "." : decSeparator,
        thouSeparator = thouSeparator == undefined ? "," : thouSeparator,
        sign = n < 0 ? "-" : "",
        i = parseInt(n = Math.abs(+n || 0).toFixed(decPlaces)) + "",
        j = (j = i.length) > 3 ? j % 3 : 0;
    return sign + (j ? i.substr(0, j) + thouSeparator : "") + i.substr(j).replace(/(\d{3})(?=\d)/g, "$1" + thouSeparator) + (decPlaces ? decSeparator + Math.abs(n - i).toFixed(decPlaces).slice(2) : "");
};

function ExpandFolder(o) {
	var tr = $(o).parent().parent();
	var oImg = tr.find("img.Plus");

	var id = tr.attr("data-folder-id");
	if (id + "" != "undefined") {
		

		if (oImg.attr("src").indexOf("plus.gif") != -1) {
			oImg.attr("src", "images/minus.gif");

			//expand files and folders
			var oChildren = tr.parent().find("tr[data-parent-id='" + id + "']");
			oChildren.show("fast");
		} else {			
			oImg.attr("src", "images/plus.gif");

			//collapse files, folders and subfolders
			var oChildren = tr.parent().find("tr.p" + id);
			oChildren.hide("fast");
			oChildren.find("img.Plus").attr("src", "images/plus.gif");
		}
		return;
	}

	var sFolder = tr.attr("data-folder");
	var iIndent = tr.attr("data-indent");
	iIndent = parseInt(iIndent) + 1;

	var sParentIds = tr.attr("data-parent-ids") || "";

	iFolderId += 1;
	tr.attr("data-folder-id", iFolderId);

	oImg.attr("src", "images/minus.gif");

	$.post("?subfolder=" + escape(sFolder) + "&indent=" + iIndent + "&folderId=" + iFolderId + "&parentIds=" + sParentIds, function (data) {
		$(tr).after(data);
		SetupDeleteBtn();
		if (oTable) oTable.columns.adjust();
		bTableDirty = true;
		//ResetDataTable();
	});

}

function ResetDataTable() {
	$("#tbServer thead tr").remove();
	var oHeadRow = $(".dataTables_scrollHeadInner").find("table thead tr").clone();
	$("#tbServer thead").append(oHeadRow);
	var oTbl = $("#tbServer").clone();
	oTable.destroy();
	$("#tbServer").remove();
	$("#divTbl").append(oTbl)
	SetupDataTable();
}
