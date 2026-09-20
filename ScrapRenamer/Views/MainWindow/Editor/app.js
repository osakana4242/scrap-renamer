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
	lineIndex: 0,
};

function debugLog(text) {
	if (!window.scrapRenamer.isDebug) return;
	console.log(text);
	window.chrome.webview.postMessage({
		type: "debugLog",
		text: "" + text,
	});
}

require([
	"vs/editor/editor.main"
], function () {
	debugLog("isDebug: " + window.scrapRenamer.isDebug);
	scrapRenamer.editor = monaco.editor.create(
		document.getElementById("container"),
		{
			value: "",
			language: "plaintext",

			theme: window.scrapRenamer.theme,
			automaticLayout: true,
			gryphMargin: true,
			lineNumbers: "on",
			renderWhitespace: "all",
			minimap: {
				enabled: false
			},
		});

	var editor = scrapRenamer.editor;

	// // Enterキーで改行ではなく次の行に移動する
	// editor.addCommand(
	// 	monaco.KeyCode.Enter,
	// 	() => {
	// 		editor.trigger("keyboard", "cursorDown", {});
	// 	}
	// );

	// 行の入れ替え無効化
	editor.addCommand(
		monaco.KeyMod.Alt | monaco.KeyCode.UpArrow,
		() => {
			editor.trigger("keyboard", "cursorUp", {});
		});
	editor.addCommand(
		monaco.KeyMod.Alt | monaco.KeyCode.DownArrow,
		() => {
			editor.trigger("keyboard", "cursorDown", {});
		});
	
	editor.addCommand(
		monaco.KeyMod.CtrlCmd | monaco.KeyMod.Shift | monaco.KeyCode.KeyP,
		() => {
			editor.trigger("keyboard", "editor.action.quickCommand", {});
		});

	// editor.addCommand(
	// 	monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS,
	// 	() => {
	// 		window.chrome.webview.postMessage({
	// 			type: "apply",
	// 		});
	// 	});

	// debugLog(
	// 	"P: " + monaco.KeyCode.P + ", " +
	// 	"KeyP: " + monaco.KeyCode.KeyP + ", " +
	// 	"KeyMod " + monaco.KeyMod + ", " +
	// 	monaco.KeyMod.WinCtrl + ", " +
	// 	monaco.KeyMod.CtrlCmd + ", " +
	// 	monaco.KeyMod.Ctrl + ", " +
	// 	monaco.KeyCode.Enter);

	scrapRenamer.lineCount = editor.getModel().getLineCount();

	// 選択行の変化を通知
	editor.onDidChangeCursorSelection((e) => {
		const selections = editor.getSelections();
		const lineIndex = selections.length === 1 ?
			selections[0].positionLineNumber - 1:
			-1;
		if (scrapRenamer.lineIndex === lineIndex) return;
		scrapRenamer.lineIndex = lineIndex;
		window.chrome.webview.postMessage({
			type: "cursorSelectionLineChanged",
			text: "" + scrapRenamer.lineIndex,
		});
	});


	editor.onDidScrollChange((e) => {
		editor.layout();

		const ranges = editor.getVisibleRanges();
		const firstVisibleLine = ranges[0].startLineNumber;
		const endVisibleLine = ranges[0].endLineNumber;

		for (var n = firstVisibleLine; n <= endVisibleLine; n++) {
			document.querySelectorAll(".path-decoration-l" + n).forEach((el, i) => {
				const line = scrapRenamer.lines[n - 1];
				const data = getAfterDecoration(line);
				debugLog("Setting data-path for decoration", i, line.origPath);
				el.setAttribute("data-path", data);
			});
		}
	});

	editor.onDidChangeModelContent((e) => {
		debugLog("Content changed:", e);
		const model = editor.getModel();

		if (model.getLineCount() !== scrapRenamer.lineCount) {
			// 元に戻す
			editor.trigger("keyboard", "undo", {});
			debugLog("Line count changed, undoing the change.");
			return;
		}

		refreshDecorations();

		//editor.layout();

		e.changes.forEach(change => {
			const n = change.range.startLineNumber;
			//debugLog("Change:", change, ", lineNumber:", n);
			document.querySelectorAll(".path-decoration-l" + n).forEach((el, i) => {
				//debugLog("Setting data-path for decoration", i, scrapRenamer.lines[n - 1].origPath);
				const line = scrapRenamer.lines[n - 1];
				el.setAttribute("data-path", getAfterDecoration(line));
			});
		});

	});


	// テキストエリアにフォーカスした時
	editor.onDidFocusEditorText(() => {
		debugLog('エディタにフォーカスされました');
		window.chrome.webview.postMessage({
			type: "editorFocusText",
		});
	});	

	// テキストエリアからフォーカスが外れた時
	editor.onDidBlurEditorText(() => {
		debugLog('エディタのフォーカスが外れました');
		window.chrome.webview.postMessage({
			type: "editorBlurText",
		});
	});	

	window.addEventListener("dragover", e => {
		window.chrome.webview.postMessage({
			type: "dragover",
		});
	});

	window.chrome.webview.postMessage({
		type: "editorLoaded",
	});

	// 不要なアクションを除去する
	const actionKeys = editor._actions.keys();

	const includeActionKeys = [
	// "editor.action.setSelectionAnchor",
	// "editor.action.goToSelectionAnchor",
	// "editor.action.selectFromAnchorToCursor",
	// "editor.action.cancelSelectionAnchor",
	// "editor.action.selectToBracket",
	// "editor.action.jumpToBracket",
	// "editor.action.removeBrackets",
	// "editor.action.moveCarretLeftAction",
	// "editor.action.moveCarretRightAction",
	// "editor.action.transposeLetters",
	// "editor.action.clipboardCopyWithSyntaxHighlightingAction",
	// "editor.action.quickFix",
	// "editor.action.refactor",
	// "editor.action.sourceAction",
	// "editor.action.organizeImports",
	// "editor.action.autoFix",
	// "editor.action.fixAll",
	// "codelens.showLensesInCurrentLine",
	// "editor.action.marker.next",
	// "editor.action.marker.prev",
	// "editor.action.marker.nextInFiles",
	// "editor.action.marker.prevInFiles",
	// "editor.action.hideColorPicker",
	// "editor.action.insertColorWithStandaloneColorPicker",
	// "editor.action.commentLine",
	// "editor.action.addCommentLine",
	// "editor.action.removeCommentLine",
	// "editor.action.blockComment",
	// "editor.action.showContextMenu",
	// "cursorUndo",
	// "cursorRedo",
	// "editor.action.pasteAs",
	// "editor.action.pasteAsText",
	// "actions.find",
	// "editor.action.nextMatchFindAction",
	// "editor.action.previousMatchFindAction",
	// "editor.action.startFindReplaceAction",
	// "editor.actions.findWithArgs",
	// "actions.findWithSelection",
	// "editor.action.goToMatchFindAction",
	// "editor.action.nextSelectionMatchFindAction",
	// "editor.action.previousSelectionMatchFindAction",
	// "editor.unfold",
	// "editor.unfoldRecursively",
	// "editor.fold",
	// "editor.foldRecursively",
	// "editor.toggleFoldRecursively",
	// "editor.foldAll",
	// "editor.unfoldAll",
	// "editor.foldAllBlockComments",
	// "editor.foldAllMarkerRegions",
	// "editor.unfoldAllMarkerRegions",
	// "editor.foldAllExcept",
	// "editor.unfoldAllExcept",
	// "editor.toggleFold",
	// "editor.gotoParentFold",
	// "editor.gotoPreviousFold",
	// "editor.gotoNextFold",
	// "editor.createFoldingRangeFromSelection",
	// "editor.removeManualFoldingRanges",
	// "editor.toggleImportFold",
	// "editor.foldLevel1",
	// "editor.foldLevel2",
	// "editor.foldLevel3",
	// "editor.foldLevel4",
	// "editor.foldLevel5",
	// "editor.foldLevel6",
	// "editor.foldLevel7",
	// "editor.action.fontZoomIn",
	// "editor.action.fontZoomOut",
	// "editor.action.fontZoomReset",
	// "editor.action.formatDocument",
	// "editor.action.formatSelection",
	// "editor.action.copyLinesUpAction",
	// "editor.action.copyLinesDownAction",
	// "editor.action.duplicateSelection",
	// "editor.action.moveLinesUpAction",
	// "editor.action.moveLinesDownAction",
	// "editor.action.sortLinesAscending",
	// "editor.action.sortLinesDescending",
	// "editor.action.removeDuplicateLines",
	// "editor.action.trimTrailingWhitespace",
	// "editor.action.deleteLines",
	// "editor.action.indentLines",
	// "editor.action.outdentLines",
	// "editor.action.insertLineBefore",
	// "editor.action.insertLineAfter",
	// "deleteAllLeft",
	// "deleteAllRight",
	// "editor.action.joinLines",
	// "editor.action.transpose",
	"editor.action.transformToUppercase",
	"editor.action.transformToLowercase",
	// "editor.action.reverseLines",
	"editor.action.transformToSnakecase",
	"editor.action.transformToCamelcase",
	"editor.action.transformToPascalcase",
	// "editor.action.transformToTitlecase",
	"editor.action.transformToKebabcase",
	// "editor.action.triggerSuggest",
	// "editor.action.resetSuggestSize",
	// "editor.action.inlineSuggest.trigger",
	// "editor.action.inlineSuggest.showNext",
	// "editor.action.inlineSuggest.showPrevious",
	// "editor.action.inlineSuggest.acceptNextWord",
	// "editor.action.inlineSuggest.acceptNextLine",
	// "editor.action.inlineSuggest.commit",
	// "editor.action.inlineSuggest.toggleShowCollapsed",
	// "editor.action.inlineSuggest.hide",
	// "editor.action.inlineSuggest.jump",
	// "editor.action.inlineSuggest.dev.extractRepro",
	// "editor.action.debugEditorGpuRenderer",
	// "editor.action.showHover",
	// "editor.action.showDefinitionPreviewHover",
	// "editor.action.hideHover",
	// "editor.action.scrollUpHover",
	// "editor.action.scrollDownHover",
	// "editor.action.scrollLeftHover",
	// "editor.action.scrollRightHover",
	// "editor.action.pageUpHover",
	// "editor.action.pageDownHover",
	// "editor.action.goToTopHover",
	// "editor.action.goToBottomHover",
	// "editor.action.increaseHoverVerbosityLevel",
	// "editor.action.decreaseHoverVerbosityLevel",
	// "editor.action.indentationToSpaces",
	// "editor.action.indentationToTabs",
	// "editor.action.indentUsingTabs",
	// "editor.action.indentUsingSpaces",
	// "editor.action.changeTabDisplaySize",
	// "editor.action.detectIndentation",
	// "editor.action.reindentlines",
	// "editor.action.reindentselectedlines",
	// "editor.action.inPlaceReplace.up",
	// "editor.action.inPlaceReplace.down",
	// "editor.action.insertFinalNewLine",
	// "expandLineSelection",
	// "editor.action.linkedEditing",
	// "editor.action.openLink",
	// "editor.action.insertCursorAbove",
	// "editor.action.insertCursorBelow",
	// "editor.action.insertCursorAtEndOfEachLineSelected",
	// "editor.action.addSelectionToNextFindMatch",
	// "editor.action.addSelectionToPreviousFindMatch",
	// "editor.action.moveSelectionToNextFindMatch",
	// "editor.action.moveSelectionToPreviousFindMatch",
	// "editor.action.selectHighlights",
	// "editor.action.changeAll",
	// "editor.action.addCursorsToBottom",
	// "editor.action.addCursorsToTop",
	// "editor.action.focusNextCursor",
	// "editor.action.focusPreviousCursor",
	// "editor.action.triggerParameterHints",
	// "editor.action.rename",
	// "editor.action.smartSelect.expand",
	// "editor.action.smartSelect.shrink",
	// "editor.action.forceRetokenize",
	// "editor.action.wordHighlight.next",
	// "editor.action.wordHighlight.prev",
	// "editor.action.wordHighlight.trigger",
	// "deleteInsideWord",
	// "editor.action.inspectTokens",
	// "editor.action.gotoLine",
	// "editor.action.quickOutline",
	// "editor.action.quickCommand",
	// "editor.action.toggleHighContrast",
	];

	for (const key of actionKeys) {
		if (includeActionKeys.includes(key)) continue;
		editor._actions.delete(key);
	}

	// ローカライズ

	const localizedStrings = {
		// "en": {
		// },
		"ja": {
			"editor.action.transformToUppercase": "大文字にする (a → A)",
			"editor.action.transformToLowercase": "小文字にする (A → a)",
			"editor.action.transformToSnakecase": "スネークケースにする (sanke_case)",
			"editor.action.transformToCamelcase": "キャメルケースにする (camelCase)",
			"editor.action.transformToPascalcase": "パスカルケースにする (PascalCase)",
			"editor.action.transformToKebabcase": "ケバブケースにする (kebab-case)",
		},
	};

	const strings = Object.hasOwn(localizedStrings, window.scrapRenamer.language) ?
		localizedStrings[window.scrapRenamer.language] :
		{};

	for (const key in strings) {
		const action = editor._actions.get(key);
		action.label = strings[key];
	}
});

// 行末に表示する情報
function getAfterDecoration(line) {
	var s = "";
	if (line.error != "") {
		s = line.error;
	}
	if (window.scrapRenamer.showFullPathInAfter) {
		if (s != "") {
			s += ", ";
		}
		s += line.origPath;
	}
	return s;
}

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
					content: "|   " + getAfterDecoration(scrapRenamer.lines[i]),
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
// 行を編集したとたんに、装飾が消えてしまうが、
// 都度、装飾を更新すれば問題無い。
function refreshDecorations2() {
	const decorations = [];
	const model = scrapRenamer.editor.getModel();

	for (let i = 0; i < scrapRenamer.lines.length; i++) {
		const line = scrapRenamer.lines[i];
		const current = model.getLineContent(i + 1);
		const changed = current !== line.origLine;
		const isError = line.error != "";
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
				beforeContentClassName: line.isDirectory ? "directory-icon" : "file-icon",
				afterContentClassName: "path-decoration path-decoration-l" + (i + 1),
				inlineClassName: changed ? "changed-line"
					: isError ? "error-line" : "",
			}
		});
	}

	if (scrapRenamer.pathDecorations) {
		scrapRenamer.pathDecorations.clear();
	}

	scrapRenamer.pathDecorations =
		scrapRenamer.editor.createDecorationsCollection(decorations);
	scrapRenamer.editor.layout();

	const ranges = scrapRenamer.editor.getVisibleRanges();

	const firstVisibleLine = ranges[0].startLineNumber;
	debugLog(firstVisibleLine);
	document.querySelectorAll(".path-decoration").forEach((el, i) => {
		const line = scrapRenamer.lines[firstVisibleLine - 1 + i];
		debugLog("Setting data-path for decoration", i, line.origPath);
		el.setAttribute("data-path", getAfterDecoration(line));
	});

}

function refreshDecorations() {
	refreshDecorations2();
}

window.chrome.webview.addEventListener("message", e => {
	debugLog("type: " + e.data.type + ", data:" + JSON.stringify(e.data));

	switch (e.data.type) {
		case "clear":
			scrapRenamer.editor.setValue("");
			break;
		case "setTheme":
			monaco.editor.setTheme(e.data.theme);
			break;
		case "setSettings":
			for (let key in e.data.settings) {
				scrapRenamer.settings[key] = e.data.settings[key];
			}
			break;
		case "getText":
			window.chrome.webview.postMessage({
				type: "text",
				text: scrapRenamer.editor.getValue()
			});
			break;
		case "updateOptions":
			scrapRenamer.editor.updateOptions(e.data.options);
			// scrapRenamer.editor.updateOptions({
			// 	"fontFamily": e.data.options.fontFamily
			// });
			// debugLog("jsFontFamily: " + e.data.options.fontFamily);
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
