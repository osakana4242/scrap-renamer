using System.Windows;
using System.IO;
using Microsoft.Web.WebView2.Core;

namespace Starter;

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
				"Starter",
				"WebView2"));

		await EditorView.EnsureCoreWebView2Async(env);

		var path = Path.Combine(
		AppContext.BaseDirectory,
		"Editor",
		"index.html");

		EditorView.Source = new Uri(path);
	}

}
