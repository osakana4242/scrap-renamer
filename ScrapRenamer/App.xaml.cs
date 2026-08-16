using System.Windows;
using System.Windows.Media;
using ModernWpf;
using ScrapRenamer.Views.MainWindow;

namespace ScrapRenamer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
	protected override void OnStartup(StartupEventArgs e) {
		base.OnStartup(e);


		// en 確認用
		var cultureInfo = new System.Globalization.CultureInfo("en");
		Thread.CurrentThread.CurrentUICulture = cultureInfo;
		Thread.CurrentThread.CurrentCulture = cultureInfo;

		MainWindow window = new MainWindow();
		if (e.Args.Length > 0) {
			window.OpenFiles(e.Args);
		}

		window.Show();
	}
}
