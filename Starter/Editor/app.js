require.config({
	paths: {
		vs: "https://cdn.jsdelivr.net/npm/monaco-editor@0.54.0/min/vs"
	}
});

require([
	"vs/editor/editor.main"
], function () {

	monaco.editor.create(
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

