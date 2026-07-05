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
	origPaths: [],
};

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

	// Enterキーで改行ではなく次の行に移動する
	scrapRenamer.editor.addCommand(
		monaco.KeyCode.Enter,
		() => {
			scrapRenamer.editor.trigger("keyboard", "cursorDown", {});
		}
	);




	scrapRenamer.lineCount = scrapRenamer.editor.getModel().getLineCount();

	scrapRenamer.editor.onDidScrollChange((e) => {
		scrapRenamer.editor.layout();

		const origPaths = scrapRenamer.origPaths;
		const ranges = scrapRenamer.editor.getVisibleRanges();
		const firstVisibleLine = ranges[0].startLineNumber;
		const endVisibleLine = ranges[0].endLineNumber;

		for (var n = firstVisibleLine; n <= endVisibleLine; n++) {
			document.querySelectorAll(".path-decoration-l" + n).forEach((el, i) => {
				console.log("Setting data-path for decoration", i, origPaths[n - 1]);
				el.setAttribute("data-path", origPaths[n - 1]);
			});
		}
	});

	scrapRenamer.editor.onDidChangeModelContent((e) => {
		console.log("Content changed:", e);
		const model = scrapRenamer.editor.getModel();

		if (model.getLineCount() !== scrapRenamer.lineCount) {
			// 元に戻す
			scrapRenamer.editor.trigger("keyboard", "undo", {});
			console.log("Line count changed, undoing the change.");
			return;
		}

		scrapRenamer.editor.layout();

		const origPaths = scrapRenamer.origPaths;

		e.changes.forEach(change => {
			const n = change.range.startLineNumber;
			console.log("Change:", change, ", lineNumber:", n);
			document.querySelectorAll(".path-decoration-l" + n).forEach((el, i) => {
				console.log("Setting data-path for decoration", i, origPaths[n - 1]);
				el.setAttribute("data-path", origPaths[n - 1]);
			});
		});

		// const ranges = scrapRenamer.editor.getVisibleRanges();
		// const firstVisibleLine = ranges[0].startLineNumber;
		// console.log(firstVisibleLine);

		// document.querySelectorAll(".path-decoration").forEach((el, i) => {
		// 	console.log("Setting data-path for decoration", i, origPaths[firstVisibleLine - 1 + i]);
		// 	el.setAttribute("data-path", origPaths[firstVisibleLine - 1 + i]);
		// });


	});

	window.addEventListener("dragover", e => {
		e.preventDefault();
	});

	window.addEventListener("drop", e => {
		e.preventDefault();

		const files = Array.from(e.dataTransfer.files);
		// https://developer.mozilla.org/ja/docs/Web/API/File
		const paths = files.map(f => f.name); // ← WebView2なら取れる

		window.chrome.webview.postMessage({
			type: "drop",
			paths: paths
		});
	});

	window.chrome.webview.postMessage({
		type: "editorLoaded",
	});
});

// カーソルが decoration まで移動してしまう問題がある
function refreshDecorations1() {
	const decorations = [];
	const model = scrapRenamer.editor.getModel();
	const origPaths = scrapRenamer.origPaths;

	for (let i = 0; i < origPaths.length; i++) {
		console.log(
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
					content: "|   " + origPaths[i],
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
	const origPaths = scrapRenamer.origPaths;

	for (let i = 0; i < origPaths.length; i++) {
		console.log(
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
				afterContentClassName: "path-decoration path-decoration-l" + (i + 1),
				// after: {
				// 	content: "|    " + origPaths[i], // "C:\\Users\\me\\Documents\\foo.txt",
				// 	inlineClassName: "path-decoration",
				// 	cursorStops: monaco.editor.InjectedTextCursorStops.NEVER,
				// 	zIndex: 3,
				// }
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

	// document.querySelectorAll(".path-decoration").forEach((el, i) => {
	// 	console.log("Setting data-path for decoration", i, origPaths[i]);
	// 	el.setAttribute("data-path", origPaths[i]);
	// });

	const ranges = scrapRenamer.editor.getVisibleRanges();

	const firstVisibleLine = ranges[0].startLineNumber;
	console.log(firstVisibleLine);
	document.querySelectorAll(".path-decoration").forEach((el, i) => {
		console.log("Setting data-path for decoration", i, origPaths[firstVisibleLine - 1 + i]);
		el.setAttribute("data-path", origPaths[firstVisibleLine - 1 + i]);
	});

}

function refreshDecorations() {
	refreshDecorations2();
}

window.chrome.webview.addEventListener("message", e => {
	console.log(e.data);

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
			const text = e.data.lines.
				join("\n");
			scrapRenamer.lineCount = e.data.lines.length;
			scrapRenamer.editor.setValue(text);
			scrapRenamer.origPaths = e.data.origPaths;
			scrapRenamer.lines = e.data.lines;
			refreshDecorations();
			break;
		}
	}
});
