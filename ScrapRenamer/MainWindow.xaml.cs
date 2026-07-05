using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace ScrapRenamer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {

	LineContainer _lineContainer = new();

	public MainWindow() {
		InitializeComponent();
		Loaded += MainWindow_Loaded;
	}

	void OnUserPreferenceChanged(
		object? sender,
		UserPreferenceChangedEventArgs e) {
		Debug.WriteLine($"UserPreferenceChanged: {e.Category}");
		if (e.Category == UserPreferenceCategory.General) {
			UpdateTheme();
		}
	}

	void UpdateTheme() {
		string theme = Env.IsDarkMode() ? "vs-dark" : "vs";

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

		EditorView.CoreWebView2.OpenDevToolsWindow();

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
		EditorView.AllowExternalDrop = false;
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
		case "text":
			MessageBox.Show(msg.Text);
			string[] editedLines = null == msg.Text ?
				new string[] {} :
				msg.Text.Split('\n').ToArray();

			for (int i = 0; i < editedLines.Length; i++) {
				if (i >= _lineContainer.Lines.Count)
					break;
				var line = _lineContainer.Lines[i];
				line.editedLine = editedLines[i];
			}

			_lineContainer.Apply();

			break;
		}

	}

	private void OnDragOver(object sender, DragEventArgs e) {
		if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
			e.Effects = DragDropEffects.Copy;
		} else {
			e.Effects = DragDropEffects.None;
		}

		e.Handled = true;
	}

	private void OnDrop(object sender, DragEventArgs e) {
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

		EditorView.CoreWebView2.PostWebMessageAsJson(
			"""
			{
				"type": "getText"
			}
			""");

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
