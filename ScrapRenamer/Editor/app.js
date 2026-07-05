require.config({
	paths: {
		vs: "monaco/vs"
	}
});

let scrapRenamer = {
	editor: null,
	pathDecorations: null,
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
			scrapRenamer.editor.setValue(
				text);

			const decorations = [];

			for (let i = 0; i < e.data.origPaths.length; i++) {

				decorations.push({
					range: new monaco.Range(i + 1, 1, i + 1, 1),
					options: {
						isWholeLine: true,
						after: {
							content: "HOGE", //e.data.origPaths[i], // "C:\\Users\\me\\Documents\\foo.txt",
							inlineClassName: "path-decoration"
						}
					}
				});
			}

			if (scrapRenamer.pathDecorations) {
				scrapRenamer.pathDecorations.clear();
			}

			scrapRenamer.pathDecorations =
				scrapRenamer.editor.createDecorationsCollection(decorations);



			break;
		}
	}
});
