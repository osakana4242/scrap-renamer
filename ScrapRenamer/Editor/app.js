require.config({
	paths: {
		vs: "monaco/vs"
	}
});

let scrapRenamer = {
	editor: null,
	pathDecorations: null,
	lineCount: 0,
	lines: [],
};

function debugLog(text) {
	console.log(text);
	window.chrome.webview.postMessage({
		type: "debugLog",
		text: "" + text,
	});
}

require([
	"vs/editor/editor.main"
], function () {
	scrapRenamer.editor = monaco.editor.create(
		document.getElementById("container"),
		{
			value: "",
			language: "plaintext",

			theme: window.scrapRenamer.theme,
			automaticLayout: true,
			gryphMargin: false,
			lineNumbers: "on",
			renderWhitespace: "all",
			minimap: {
				enabled: false
			},
		});

	// // Enterキーで改行ではなく次の行に移動する
	// scrapRenamer.editor.addCommand(
	// 	monaco.KeyCode.Enter,
	// 	() => {
	// 		scrapRenamer.editor.trigger("keyboard", "cursorDown", {});
	// 	}
	// );

	// 行の入れ替え無効化
	scrapRenamer.editor.addCommand(
		monaco.KeyMod.Alt | monaco.KeyCode.UpArrow,
		() => {
			scrapRenamer.editor.trigger("keyboard", "cursorUp", {});
		});
	scrapRenamer.editor.addCommand(
		monaco.KeyMod.Alt | monaco.KeyCode.DownArrow,
		() => {
			scrapRenamer.editor.trigger("keyboard", "cursorDown", {});
		});
	
	scrapRenamer.editor.addCommand(
		monaco.KeyMod.CtrlCmd | monaco.KeyMod.Shift | monaco.KeyCode.KeyP,
		() => {
			scrapRenamer.editor.trigger("keyboard", "editor.action.quickCommand", {});
		});

	scrapRenamer.editor.addCommand(
		monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter,
		() => {
			window.chrome.webview.postMessage({
				type: "apply",
			});
		});

	// debugLog(
	// 	"P: " + monaco.KeyCode.P + ", " +
	// 	"KeyP: " + monaco.KeyCode.KeyP + ", " +
	// 	"KeyMod " + monaco.KeyMod + ", " +
	// 	monaco.KeyMod.WinCtrl + ", " +
	// 	monaco.KeyMod.CtrlCmd + ", " +
	// 	monaco.KeyMod.Ctrl + ", " +
	// 	monaco.KeyCode.Enter);

	scrapRenamer.lineCount = scrapRenamer.editor.getModel().getLineCount();

	scrapRenamer.editor.onDidScrollChange((e) => {
		scrapRenamer.editor.layout();

		const ranges = scrapRenamer.editor.getVisibleRanges();
		const firstVisibleLine = ranges[0].startLineNumber;
		const endVisibleLine = ranges[0].endLineNumber;

		for (var n = firstVisibleLine; n <= endVisibleLine; n++) {
			document.querySelectorAll(".path-decoration-l" + n).forEach((el, i) => {
				const line = scrapRenamer.lines[n - 1];
				const data = line.error != "" ?
					line.error + ", " + line.origPath :
					line.origPath;
				debugLog("Setting data-path for decoration", i, line.origPath);
				el.setAttribute("data-path", data);
			});
		}
	});

	scrapRenamer.editor.onDidChangeModelContent((e) => {
		debugLog("Content changed:", e);
		const model = scrapRenamer.editor.getModel();

		if (model.getLineCount() !== scrapRenamer.lineCount) {
			// 元に戻す
			scrapRenamer.editor.trigger("keyboard", "undo", {});
			debugLog("Line count changed, undoing the change.");
			return;
		}

		refreshDecorations();

		//scrapRenamer.editor.layout();

		e.changes.forEach(change => {
			const n = change.range.startLineNumber;
			//debugLog("Change:", change, ", lineNumber:", n);
			document.querySelectorAll(".path-decoration-l" + n).forEach((el, i) => {
				//debugLog("Setting data-path for decoration", i, scrapRenamer.lines[n - 1].origPath);
				const line = scrapRenamer.lines[n - 1];
				const data = line.error != "" ?
					line.error + ", " + line.origPath:
					line.origPath;
				el.setAttribute("data-path", data);
			});
		});

	});

	window.addEventListener("dragover", e => {
		window.chrome.webview.postMessage({
			type: "dragover",
		});
	});

	// window.addEventListener("drop", e => {
	// 	e.preventDefault();

	// 	const files = Array.from(e.dataTransfer.files);
	// 	// https://developer.mozilla.org/ja/docs/Web/API/File
	// 	const paths = files.map(f => f.name); // ← WebView2なら取れる

	// 	window.chrome.webview.postMessage({
	// 		type: "drop",
	// 		paths: paths
	// 	});
	// });

	window.chrome.webview.postMessage({
		type: "editorLoaded",
	});
});

// カーソルが decoration まで移動してしまう問題がある
function refreshDecorations1() {
	const decorations = [];
	const model = scrapRenamer.editor.getModel();

	for (let i = 0; i < scrapRenamer.lines.length; i++) {
		debugLog(
			i + 1,
			model.getLineContent(i + 1),
			model.getLineMaxColumn(i + 1)
		);

		const lineMaxColumn = model.getLineMaxColumn(i + 1);
		decorations.push({
			range: new monaco.Range(
				i + 1,
				lineMaxColumn - 1,
				i + 1,
				lineMaxColumn
			),
			options: {
				after: {
					content: "|   " + scrapRenamer.lines[i].origPath,
					inlineClassName: "path-decoration"
				},
				cursorStops: monaco.editor.InjectedTextCursorStops.BEFORE
			}
		});
	}

	if (scrapRenamer.pathDecorations) {
		scrapRenamer.pathDecorations.clear();
	}

	scrapRenamer.pathDecorations =
		scrapRenamer.editor.createDecorationsCollection(decorations);
	scrapRenamer.editor.layout();
}

// 見た目、操作感ともこれが最高なのだが、
// 行を編集したとたんに、装飾が消えてしまう。
function refreshDecorations2() {
	const decorations = [];
	const model = scrapRenamer.editor.getModel();

	for (let i = 0; i < scrapRenamer.lines.length; i++) {
		const current = model.getLineContent(i + 1);
		const changed = current !== scrapRenamer.lines[i].editedLine;
		const isError = scrapRenamer.lines[i].error != "";
		const lineMaxColumn = model.getLineMaxColumn(i + 1);

		debugLog(
			i + 1,
			model.getLineContent(i + 1),
			model.getLineMaxColumn(i + 1),
			changed
		);
		decorations.push({
			range: new monaco.Range(
				i + 1,
				1,
				i + 1,
				lineMaxColumn
			),
			options: {
				isWholeLine: true,
				afterContentClassName: "path-decoration path-decoration-l" + (i + 1),
				inlineClassName: changed ? "changed-line"
					: isError ? "error-line" : "",
			}
		});
	}

	if (scrapRenamer.pathDecorations) {
		scrapRenamer.pathDecorations.clear();
	}

	// scrapRenamer.pathDecorations =
	// 	scrapRenamer.editor.createDecorationsCollection(decorations);

	scrapRenamer.pathDecorations =
		scrapRenamer.editor.createDecorationsCollection(decorations);
	scrapRenamer.editor.layout();

	const ranges = scrapRenamer.editor.getVisibleRanges();

	const firstVisibleLine = ranges[0].startLineNumber;
	debugLog(firstVisibleLine);
	document.querySelectorAll(".path-decoration").forEach((el, i) => {
		const line = scrapRenamer.lines[firstVisibleLine - 1 + i];
		debugLog("Setting data-path for decoration", i, line.origPath);
		const data = line.error != "" ?
			line.error + ", " + line.origPath:
			line.origPath;
		el.setAttribute("data-path", data);
	});

}

function refreshDecorations() {
	refreshDecorations2();
}

window.chrome.webview.addEventListener("message", e => {
	debugLog(e.data);

	switch (e.data.type) {
		case "clear":
			scrapRenamer.editor.setValue("");
			break;
		case "setTheme":
			monaco.editor.setTheme(e.data.theme);
			break;
		case "getText":
			window.chrome.webview.postMessage({
				type: "text",
				text: scrapRenamer.editor.getValue()
			});
			break;
		case "setLines": {
			const text = e.data.lines.map(l => l.editedLine).
				join("\n");
			scrapRenamer.lineCount = e.data.lines.length;
			scrapRenamer.editor.setValue(text);
			scrapRenamer.lines = e.data.lines;
			refreshDecorations();
			break;
		}
	}
});
