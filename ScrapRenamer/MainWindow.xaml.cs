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
	public MainWindow() {
		InitializeComponent();
		Loaded += MainWindow_Loaded;
	}

	static bool IsDarkMode() {
		object? value = Registry.GetValue(
		@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
		"AppsUseLightTheme",
		1);

		return value is int light && light == 0;
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
		string theme = IsDarkMode() ? "vs-dark" : "vs";

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

		// EditorView.CoreWebView2.OpenDevToolsWindow();

		string theme = IsDarkMode() ? "vs-dark" : "vs";

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
		EditorView.DefaultBackgroundColor = IsDarkMode() ?
			System.Drawing.Color.Black :
			System.Drawing.Color.White;

		EditorView.Source = new Uri(path);
		EditorView.WebMessageReceived += EditorView_WebMessageReceived;
		// // 外部からのファイルドロップを禁止する
		EditorView.AllowExternalDrop = true;
		// ドロップ時に発生するURL遷移イベントを購読
		// EditorView.CoreWebView2.NavigationStarting += EditorView_NavigationStarting;
		// EditorView.AllowDrop = true;
		// EditorView.DragOver += OnDragOver;
		// EditorView.Drop += OnDrop;
		SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
	}

	bool _firstJump = false;

	void EditorView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e) {
		string url = e.Uri;
		Debug.WriteLine($"NavigationStarting: {url}");
		if (!_firstJump) {
			_firstJump = true;
			return;
		}


		// ファイルがドロップされた場合、URLは "file:///C:/..." などの形式になります
		if (url.StartsWith("file:///", StringComparison.OrdinalIgnoreCase)) {
			// 画面遷移をキャンセルしてブラウザでのファイル展開を防ぐ
			e.Cancel = true;

			// file:/// のプレフィックスを外して、通常のローカルパスに変換
			string filePath = Uri.UnescapeDataString(new Uri(url).LocalPath);

			// WPF側でやりたかった処理を呼び出す
			// 例: ProcessDroppedFiles(filePath);
		}
	}

	void EditorView_WebMessageReceived(
		object? sender,
		CoreWebView2WebMessageReceivedEventArgs e) {
		var json = e.WebMessageAsJson;

		// MessageBox.Show(json);

		var msg = JsonSerializer.Deserialize<EditorMessage>(json);
		if(msg == null) {
			Debug.WriteLine("Failed to deserialize message.");
			return;
		}
		
		switch (msg.Type) {
		case "editorLoaded":
			Debug.WriteLine("Editor loaded.");
			// EditorView.Visibility = Visibility.Visible;
			// UpdateLayout();
			// EditorView.InvalidateMeasure();
			// EditorView.InvalidateArrange();
			break;
		case "text":
			MessageBox.Show(msg.Text);
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

		foreach (var file in files) {
			Debug.WriteLine(file);
		}

		var message = new {
			type = "appendLines",
			lines = files
		};

		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));

	}

	void OnClearClicked(object sender, RoutedEventArgs e) {
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

	public class EditorMessage {
		[JsonPropertyName("type")]
		public string? Type { get; set; }
		[JsonPropertyName("text")]
		public string? Text { get; set; }
	}
}
