using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace ScrapRenamer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
	static bool _isDebug = false;

	LineContainer _lineContainer = new();
	System.Action<(string text, System.Exception? ex)>? _onTextGet;


	public MainWindow() {
		InitializeComponent();
		UpdateTheme();
		Loaded += MainWindow_Loaded;
		Title = "ScrapRenamer v1.0.0a";
		Settings.Instance.themeProp.OnChanged += OnThemeChanged;
		Settings.Instance.fontFamilyProp.OnChanged += OnFontFamilyChanged;
		Settings.Instance.fontSizeProp.OnChanged += OnFontSizeChanged;
		Settings.Instance.Load();
	}

	void UpdateTheme() {
		var isDark =
			Settings.Instance.themeProp.Value == ThemeMode.Dark ||
			Settings.Instance.themeProp.Value == ThemeMode.System &&
			Env.IsDarkMode();


		Dwm.SetWindowDarkMode(this, isDark);
		ThemeMode = Settings.Instance.themeProp.Value;

		if (null == EditorView?.CoreWebView2) return;
		string theme = isDark ? "vs-dark" : "vs";

		var message = new {
			type = "setTheme",
			theme = theme
		};

		Debug.WriteLine($"UpdateTheme: {theme}");

		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));

	}

	async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
		var env = await CoreWebView2Environment.CreateAsync(
			userDataFolder: Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"ScrapRenamer",
				"WebView2"));

		await EditorView.EnsureCoreWebView2Async(env);
		EditorView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;


		if (_isDebug) {
			EditorView.CoreWebView2.OpenDevToolsWindow();
		}

		await EditorView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
			$$"""
			window.scrapRenamer = {
			};
			""");
		UpdateTheme();

		var path = Path.Combine(
			AppContext.BaseDirectory,
			"Editor",
			"index.html");
		EditorView.DefaultBackgroundColor = Env.IsDarkMode() ?
			System.Drawing.Color.Black :
			System.Drawing.Color.White;

		EditorView.Source = new Uri(path);
		EditorView.WebMessageReceived += EditorView_WebMessageReceived;
		// // 外部からのファイルドロップを禁止する
		EditorView.AllowExternalDrop = true;
		EditorView.Visibility = Visibility.Visible;
		DropOverlay.Visibility = Visibility.Visible;
		StatusBar.Visibility = Visibility.Hidden;
		UpdateVisibility(false);
	}



	async void EditorView_WebMessageReceived(
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
			await Apply();

			break;
		case "editorLoaded":
			Debug.WriteLine("Editor loaded.");
			UpdateTheme();
			SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
			break;
		case "dragover":
			Debug.WriteLine("dragover");
			UpdateVisibility(true);
			break;
		case "text":
			var act = _onTextGet;
			_onTextGet = null;
			act?.Invoke((msg.Text ?? "", null));
			break;
		}
	}


	void OnOpenMenuClick(
		object sender,
		RoutedEventArgs e)
	{
		var dialog = new OpenFileDialog {
			Title = "ファイルを選択",
			Multiselect = true,
			CheckFileExists = true
		};

		if (dialog.ShowDialog() != true) {
			return;
		}

		OpenFiles(dialog.FileNames);
	}

	void OnExitMenuClick(
		object sender,
		RoutedEventArgs e) {
		Application.Current.Shutdown();
	}

	void OnOpenAboutClick(
		object sender,
		RoutedEventArgs e) {
		var window = new AboutWindow() {
			Owner = this
		};
		window.ShowDialog();
	}

	void OnOpenSettingsClick(
		object sender,
		RoutedEventArgs e) {
		var window = new SettingsWindow() {
			Owner = this
		};
		window.ShowDialog();
	}

	void OnUserPreferenceChanged(
		object? sender,
		UserPreferenceChangedEventArgs e) {
		Debug.WriteLine($"UserPreferenceChanged: {e.Category}");
		if (e.Category == UserPreferenceCategory.General) {
			UpdateTheme();
		}
	}

	void OnDragOver(object sender, DragEventArgs e) {
		Debug.Print($"OnDragOver: {e}");
		if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
			e.Effects = DragDropEffects.Copy;
		} else {
			e.Effects = DragDropEffects.None;
		}

		e.Handled = true;
	}

	void OnDrop(object sender, DragEventArgs e) {
		if (!e.Data.GetDataPresent(DataFormats.FileDrop))
			return;
		var files = (string[])e.Data.GetData(DataFormats.FileDrop);
		OpenFiles(files);
	}

	void OpenFiles(string[] files) {
		System.Array.Sort(files, (a, b) => a.CompareTo(b));
		var lines = new List<Line>();

		foreach (var file in files) {
			Debug.WriteLine(file);
			var line = new Line(file);
			if (!_lineContainer.Add(line))
				continue;
			lines.Add(line);
		}

		UpdateVisibility(false);

		if (lines.Count == 0) {
			Debug.WriteLine("No new lines to add.");
			return;
		}
		Editor_SetLines();
	}

	void OnClearClicked(object sender, RoutedEventArgs e) {
		_lineContainer = new LineContainer();
		EditorView.CoreWebView2.PostWebMessageAsJson(
			"""
			{
				"type": "clear"
			}
			""");
		UpdateVisibility(false);
	}

	async Task<string> GetTextAsync() {
		(string text, System.Exception? ex)? ret = null;
		_onTextGet = s => {
			ret = s;
		};

		EditorView.CoreWebView2.PostWebMessageAsJson(
			"""
			{
				"type": "getText"
			}
			""");

		while (null == ret) {
			await Dispatcher.Yield();
		}

		if (null != ret.Value.ex) {
			throw ret.Value.ex;
		}

		return ret.Value.text;
	}

	async Task SyncTextFromEditorAsync() {
		var text = await GetTextAsync();
		string[] editedLines = null == text ?
			new string[] {} :
			text.Split('\n').ToArray();

		for (int i = 0; i < editedLines.Length; i++) {
			if (i >= _lineContainer.Lines.Count)
				break;
			var line = _lineContainer.Lines[i];
			line.editedLine = editedLines[i];
		}
	}

	async Task Apply() {
		await SyncTextFromEditorAsync();
		_lineContainer.Apply();
		var lines = _lineContainer.Lines.
			Select((elem, i) => new {index = i, elem}).
			Where((elem) => elem.elem.processed);
		var errorLines = lines.Where(i => "" != i.elem.error).ToList();
		var successLines = lines.Where(i => "" == i.elem.error).ToList();
		var doc = new FlowDocument();

		if (0 < errorLines.Count) {
			{
				var p = CreateParagraph();
				p.Foreground = Brushes.Red;
				var s = $"⛔失敗 {errorLines.Count} 件";
				p.Inlines.Add(new Run(s));
				doc.Blocks.Add(p);
			}


			foreach (var line in errorLines) {
				AddParagraph(doc, $"行 {line.index}: ⛔失敗 {line.elem.origPath} → {line.elem.editedLine}");
				AddParagraph(doc, $"({line.elem.error})");
				AddParagraph(doc, $"");
			}
			AddParagraph(doc, "");
		}

		{
			var p = CreateParagraph();
			var s = $"✅成功 {successLines.Count} 件";
			p.Inlines.Add(new Run(s));
			doc.Blocks.Add(p);
		}

		foreach (var line in successLines) {
			AddParagraph(doc, $"行 {line.index}: ✅成功 {line.elem.origPath} → {line.elem.editedLine}");
		}

		AddParagraph(doc, "");

		var window = new ResultWindow(doc) {
			Owner = this
		};
		window.ShowDialog();
		Editor_SetLines();

	}

	static void AddParagraph(FlowDocument doc, string text) {
		var p = new Paragraph() {
			FontFamily = Settings.Instance.FontFamily,
			FontSize = Settings.Instance.fontSizeProp.Value,
			Margin = new Thickness(8),
		};
		p.Inlines.Add(new Run(text));
		doc.Blocks.Add(p);
	}

	static Paragraph CreateParagraph() {
		var p = new Paragraph() {
			FontFamily = Settings.Instance.FontFamily,
			FontSize = Settings.Instance.fontSizeProp.Value,
			Margin = new Thickness(8),
		};
		return p;
	}


	void OnFontFamilyChanged(string fontFamily) {
		if (null == EditorView.CoreWebView2) return;
		var message = new {
			type = "updateOptions",
			options = new {
				fontFamily = fontFamily,
			}
		};
		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
	}

	void OnFontSizeChanged(int fontSize) {
		if (null == EditorView.CoreWebView2) return;
		var message = new {
			type = "updateOptions",
			options = new {
				fontSize = fontSize,
			}
		};
		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
	}

	void OnThemeChanged(ThemeMode themeMode) {
		UpdateTheme();
	}

	async void OnExecuteClicked(object sender, RoutedEventArgs e) {
		await Apply();
	}

	async void OnSortClicked(object sender, RoutedEventArgs e) {
		await SyncTextFromEditorAsync();
		_lineContainer.Sort();
		Editor_SetLines();
	}

	// エディターに現テキストを設定する
	void Editor_SetLines() {
		var message = new {
			type = "setLines",
			lines = _lineContainer.Lines.Select(i => new {
				origPath = i.origPath,
				editedLine = i.editedLine,
				error = i.error,
				isFolder = i.isDirectory,
			}).ToArray(),
		};

		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
	}

	void UpdateVisibility(bool isDragging) {
		if (0 < _lineContainer.Lines.Count && !isDragging) {
			Debug.Print($"A, isDragging: {isDragging}, lineCount: {_lineContainer.Lines.Count}");
			EditorView.Visibility = Visibility.Visible;
			DropOverlay.Visibility = Visibility.Hidden;
		} else {
			Debug.Print($"B, isDragging: {isDragging}, lineCount: {_lineContainer.Lines.Count}");
			EditorView.Visibility = Visibility.Hidden;
			DropOverlay.Visibility = Visibility.Visible;
		}
	}
	
	// -------------------------------------------------------- MARK: override

	protected override void OnSourceInitialized(EventArgs e) {
		base.OnSourceInitialized(e);
		UpdateTheme();
	}

	protected override void OnActivated(EventArgs e) {
		base.OnActivated(e);
		Debug.Print($"OnActivated: {e}");
	}
	protected override void OnDeactivated(EventArgs e) {
		base.OnDeactivated(e);
		Debug.Print($"OnDeactivated: {e}");
	}
	protected override void OnGotFocus(RoutedEventArgs e) {
		base.OnGotFocus(e);
		Debug.Print($"OnGotFocus: {e}");
	}

	protected override void OnLostFocus(RoutedEventArgs e) {
		base.OnLostFocus(e);
		Debug.Print($"OnLostFocus: {e}");
	}

}
