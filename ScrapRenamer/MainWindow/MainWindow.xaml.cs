using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
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
	}

	void UpdateTheme() {
		var isDark = Env.IsDarkMode();
		SwitchTheme(isDark ? "Dark" : "Light");

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

	void SwitchTheme(string themeName) {
		var dicts = Application.Current.Resources.MergedDictionaries;

		// 既存テーマ削除
		var oldTheme = dicts.FirstOrDefault(d =>
		d.Source != null &&
		d.Source.OriginalString.Contains("Themes/"));

		if (oldTheme != null) {
			Debug.WriteLine($"Remove {oldTheme}");
			dicts.Remove(oldTheme);
		}

		// 新しいテーマ追加
		var newTheme = new ResourceDictionary();
		newTheme.Source = new Uri($"Themes/{themeName}.xaml", UriKind.Relative);

		dicts.Add(newTheme);
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

		string theme = Env.IsDarkMode() ? "vs-dark" : "vs";

		await EditorView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
			$$"""
			window.scrapRenamer = {
				theme: "{{theme}}"
			};
			""");

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
			SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
			break;
		case "dragover":
			Debug.WriteLine("dragover");
			EditorView.Visibility = Visibility.Hidden;
			DropOverlay.Visibility = Visibility.Visible;
			break;
		case "text":
			var act = _onTextGet;
			_onTextGet = null;
			act?.Invoke((msg.Text ?? "", null));
			break;
		}
	}

	protected override void OnActivated(EventArgs e) {
		base.OnActivated(e);
		Debug.Print($"OnActivated: {e}");
		EditorView.Visibility = Visibility.Visible;
	}
	protected override void OnDeactivated(EventArgs e) {
		base.OnDeactivated(e);
		Debug.Print($"OnDeactivated: {e}");
		// EditorView.Visibility = Visibility.Hidden;
	}
	protected override void OnGotFocus(RoutedEventArgs e) {
		base.OnGotFocus(e);
		Debug.Print($"OnGotFocus: {e}");
		EditorView.Visibility = Visibility.Visible;
	}

	protected override void OnLostFocus(RoutedEventArgs e) {
		base.OnLostFocus(e);
		Debug.Print($"OnLostFocus: {e}");
		EditorView.Visibility = Visibility.Hidden;
	}
	void OnUserPreferenceChanged(
		object? sender,
		UserPreferenceChangedEventArgs e) {
		Debug.WriteLine($"UserPreferenceChanged: {e.Category}");
		if (e.Category == UserPreferenceCategory.General) {
			UpdateTheme();
		}
	}

	private void OnDragOver(object sender, DragEventArgs e) {
		Debug.Print($"OnDragOver: {e}");
		// EditorView.Visibility = Visibility.Hidden;
		if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
			e.Effects = DragDropEffects.Copy;
		} else {
			e.Effects = DragDropEffects.None;
		}

		e.Handled = true;
	}

	private void OnDrop(object sender, DragEventArgs e) {
		EditorView.Visibility = Visibility.Visible;
		DropOverlay.Visibility = Visibility.Hidden;
		if (!e.Data.GetDataPresent(DataFormats.FileDrop))
			return;

		var files = (string[])e.Data.GetData(DataFormats.FileDrop);
		System.Array.Sort(files, (a, b) => a.CompareTo(b));
		var lines = new List<Line>();

		foreach (var file in files) {
			Debug.WriteLine(file);
			var line = new Line {
				origPath = file,
				editedLine = file
			};
			if (!_lineContainer.Add(line))
				continue;
			lines.Add(line);
		}

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
		Editor_SetLines();
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
			 }).ToArray(),
		};

		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
	}

	public static class Env {

		public static bool IsDarkMode() {
			object? value = Registry.GetValue(
			@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
			"AppsUseLightTheme",
			1);

			return value is int light && light == 0;
		}
	}
}
