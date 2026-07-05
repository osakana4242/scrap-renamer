require.config({
	paths: {
		vs: "monaco/vs"
	}
});

let scrapRenamer = {
	editor: null,
};

require([
	"vs/editor/editor.main"
], function () {
	scrapRenamer.editor = monaco.editor.create(
		document.getElementById("container"),
		{
			value:
				`foo.txt
bar.png
baz.cs`,
			language: "plaintext",

			theme: window.scrapRenamer.theme,

			automaticLayout: true
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
		case "appendLines": {
			const text = e.data.lines.join("\n");

			// 一番簡単
			scrapRenamer.editor.setValue(
				scrapRenamer.editor.getValue() + "\n" + text);

			break;
		}
	}
});
