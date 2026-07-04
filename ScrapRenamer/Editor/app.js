require.config({
	paths: {
		vs: "monaco/vs"
	}
});

let editor;

require([
	"vs/editor/editor.main"
], function () {

	editor = monaco.editor.create(
		document.getElementById("container"),
		{
			value:
				`foo.txt
bar.png
baz.cs`,
			language: "plaintext",

			theme: "vs-dark",

			automaticLayout: true
		});
});

window.chrome.webview.addEventListener("message", e => {
	console.log(e.data);

	switch (e.data.type) {
		case "clear":
			editor.setValue("");
			break;
		case "getText":
			window.chrome.webview.postMessage({
				type: "text",
				text: editor.getValue()
			});
			break;
	}
});
