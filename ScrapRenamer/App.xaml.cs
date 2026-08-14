using System.Windows;
using ScrapRenamer.Views.MainWindow;

namespace ScrapRenamer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
	protected override void OnStartup(StartupEventArgs e) {
		base.OnStartup(e);

		MainWindow window = new MainWindow();
		if (e.Args.Length > 0) {
			window.OpenFiles(e.Args);
		}

		window.Show();
	}
}
