
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ScrapRenamer.Views.MainWindow;

class Editor {
	MainWindow _owner;
	System.Action<(string text, System.Exception? ex)>? _onTextGet;
	bool _loaded;
	bool _hasFocus;


	public Editor(MainWindow owner) {
		_owner = owner;
	}


	public bool Loaded => _loaded;
	public bool HasFocus => _hasFocus;
	public WebView2 EditorView => _owner.EditorView;


	public async void WebMessageReceived(
		object? sender,
		CoreWebView2WebMessageReceivedEventArgs e) {
		var json = e.WebMessageAsJson;

		// MessageBox.Show(json);

		var msg = JsonSerializer.Deserialize<EditorMessage>(json);
		if (msg == null) {
			Debug.WriteLine("Failed to deserialize message.");
			return;
		}

		switch (msg.Type) {
		case "debugLog":
			Debug.WriteLine("from js: " + msg.Text);
			break;
		case "apply":
			Debug.WriteLine("Apply.");
			await _owner.Apply();
			break;
		case "cursorSelectionLineChanged":
			if (int.TryParse(msg.Text, out var lineIndex)) {
				if ((uint)lineIndex < (uint)_owner.LineContainer.Lines.Count) {
					var line = _owner.LineContainer.Lines[lineIndex];
					(
						_owner.StatusBarTextBlockLeft.Text,
						_owner.StatusBarTextBlockRight.Text
					) = PathUtil.SplitPathAtNthLastSeparator(line.origPath, 3);
				} else {
					_owner.StatusBarTextBlockLeft.Text = $"";
					_owner.StatusBarTextBlockRight.Text = $"";
				}
			}
			break;
		case "editorLoaded":
			Debug.WriteLine("Editor loaded.");
			_loaded = true;
			break;
		case "editorFocusText":
			_hasFocus = true;
			break;
		case "editorBlurText":
			_hasFocus = false;
			break;
		case "dragover":
			Debug.WriteLine("dragover");
			_owner.UpdateVisibility(true);
			break;
		case "text":
			var act = _onTextGet;
			_onTextGet = null;
			act?.Invoke((msg.Text ?? "", null));
			break;
		}
	}

	public void Clear() {
		var message = new { type = "clear", };
		PostWebMessageAsJson(message);
	}

	public void GetText() {
		var message = new { type = "getText", };
		PostWebMessageAsJson(message);
	}

	public async Task<string> GetTextAsync() {
		(string text, System.Exception? ex)? ret = null;
		_onTextGet = s => {
			ret = s;
		};

		GetText();

		while (null == ret) {
			await Dispatcher.Yield();
		}

		if (null != ret.Value.ex) {
			throw ret.Value.ex;
		}

		return ret.Value.text;
	}

	// エディターオプションを設定する
	public void UpdateOptions(object options) {
		var message = new {
			type = "updateOptions",
			options = options,
		};
		PostWebMessageAsJson(message);
	}

	// エディター設定を設定する
	public void SetSettings() {
		var message = new {
			type = "setSettings",
			settings = new {
				isAfterContentFullpathVisible = false,
			},
		};
		PostWebMessageAsJson(message);
	}

	// エディターに現テキストを設定する
	public void SetLines() {
		if (null == EditorView.CoreWebView2) return;

		var message = new {
			type = "setLines",
			lines = _owner.LineContainer.Lines.Select(i => new {
				origPath = i.origPath,
				origLine = i.OrigLine,
				editedLine = i.editedLine,
				error = i.Error,
				isDirectory = i.isDirectory,
			}).ToArray(),
		};
		PostWebMessageAsJson(message);
	}

	public void SetTheme(string theme) {
		var message = new {
			type = "setTheme",
			theme = theme
		};
		PostWebMessageAsJson(message);
	}

	void PostWebMessageAsJson(object obj) {
		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(obj));
	}
}
