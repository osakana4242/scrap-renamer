using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;

namespace ScrapRenamer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
	static bool _isDebug = true;

	LineContainer _lineContainer = new();
	System.Action? _onTextGet;


	public MainWindow() {
		InitializeComponent();
		UpdateTheme();
		Loaded += MainWindow_Loaded;
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



	void EditorView_WebMessageReceived(
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
			string[] editedLines = null == msg.Text ?
				new string[] {} :
				msg.Text.Split('\n').ToArray();

			for (int i = 0; i < editedLines.Length; i++) {
				if (i >= _lineContainer.Lines.Count)
					break;
				var line = _lineContainer.Lines[i];
				line.editedLine = editedLines[i];
			}
			var act = _onTextGet;
			_onTextGet = null;
			act?.Invoke();
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

		var message = new {
			type = "setLines",
			origPaths = lines.Select(i => i.origPath).ToArray(),
			lines = lines.Select(i => i.editedLine).ToArray(),
		};

		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
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

	void OnExecuteClicked(object sender, RoutedEventArgs e) {
		_onTextGet = () => {
			_lineContainer.Apply();
			Editor_SetLines();
		};

		EditorView.CoreWebView2.PostWebMessageAsJson(
			"""
			{
				"type": "getText"
			}
			""");
	}

	void OnSortClicked(object sender, RoutedEventArgs e) {
		_onTextGet = () => {
			_lineContainer.Sort();
			Editor_SetLines();
		};
		EditorView.CoreWebView2.PostWebMessageAsJson(
			"""
			{
				"type": "getText"
			}
			""");
	}

	// エディターに現テキストを設定する
	void Editor_SetLines() {
		var message = new {
			type = "setLines",
			origPaths = _lineContainer.Lines.Select(i => i.origPath).ToArray(),
			lines = _lineContainer.Lines.Select(i => i.editedLine).ToArray(),
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


	public class EditorMessage {
		[JsonPropertyName("type")]
		public string? Type { get; set; }
		[JsonPropertyName("text")]
		public string? Text { get; set; }
	}

	class LineContainer {
		Mode _mode = Mode.Name;
		public List<Line> Lines { get; set; } = new();

		public bool Add(Line line) {
			if (null != Lines.Find(l => l.origPath == line.origPath)) {
				return false;
			}

			switch (_mode) {
			case Mode.Name:
				line.editedLine = Path.GetFileName(line.origPath);
				break;
			case Mode.FullPath:
				line.editedLine = line.origPath;
				break;
			}

			Lines.Add(line);
			return true;
		}

		public void Sort() {
			Lines.Sort((a, b) => {
				return a.origPath.CompareTo(b.origPath);
			});
		}

		public void Apply() {
			// Apply changes to the lines
			for (int i = 0; i < Lines.Count; i++) {
				var line = Lines[i];
				if (line.origPath != line.editedLine) {
					Debug.WriteLine($"Renaming: {line.origPath} -> {line.editedLine}");
					try {
						var nextPath = line.editedLine;
						switch (_mode) {
						case Mode.Name:
							nextPath = Path.Combine(Path.GetDirectoryName(line.origPath) ?? "", line.editedLine);
							break;
						case Mode.FullPath:
							nextPath = line.editedLine;
							break;
						}
						System.IO.File.Move(line.origPath, nextPath);
						line.origPath = nextPath;
					} catch (Exception ex) {
						Debug.WriteLine($"Failed to rename: {ex.Message}");
					}
				}
			}

		}
	}

	class Line {
		public string origPath = "";
		public string editedLine = "";
	}

	enum Mode {
		Name,
		FullPath,
	}
}
