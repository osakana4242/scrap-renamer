using System.Windows;
using System.Windows.Shapes;
using System.IO;

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
		await EditorView.EnsureCoreWebView2Async();

		var path = System.IO.Path.Combine(
		AppContext.BaseDirectory,
		"Editor",
		"index.html");

		EditorView.Source = new Uri(path);
	}

}
