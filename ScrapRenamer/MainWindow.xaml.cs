using System.Windows;
using System.IO;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScrapRenamer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();
		Loaded += MainWindow_Loaded;
	}

	async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
		var env = await CoreWebView2Environment.CreateAsync(
			userDataFolder: Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"ScrapRenamer",
				"WebView2"));

		await EditorView.EnsureCoreWebView2Async(env);

		var path = Path.Combine(
		AppContext.BaseDirectory,
		"Editor",
		"index.html");

		EditorView.Source = new Uri(path);
		EditorView.WebMessageReceived += EditorView_WebMessageReceived;
	}

	void EditorView_WebMessageReceived(
		object? sender,
		CoreWebView2WebMessageReceivedEventArgs e) {
		var json = e.WebMessageAsJson;

		MessageBox.Show(json);

		var msg = JsonSerializer.Deserialize<EditorMessage>(json);

		if (msg?.Type == "text") {
			MessageBox.Show(msg.Text);
		}
	}

	void OnClearClicked(object sender, RoutedEventArgs e) {
		EditorView.CoreWebView2.PostWebMessageAsJson("""
{
	"type": "clear"
}
""");
	}

	void OnExecuteClicked(object sender, RoutedEventArgs e) {

		EditorView.CoreWebView2.PostWebMessageAsJson("""
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
