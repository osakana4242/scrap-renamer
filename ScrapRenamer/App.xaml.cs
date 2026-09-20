using System.Windows;
using System.Windows.Media;
using ModernWpf;
using ScrapRenamer.Common;
using ScrapRenamer.Views.MainWindow;

namespace ScrapRenamer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
	protected override void OnStartup(StartupEventArgs e) {
		base.OnStartup(e);

#if DEBUG
		// en 確認用
		var cultureInfo = new System.Globalization.CultureInfo("en");
		Thread.CurrentThread.CurrentUICulture = cultureInfo;
		Thread.CurrentThread.CurrentCulture = cultureInfo;
#endif

		Settings.Instance.Load();

		MainWindow window = new MainWindow(e.Args);
		window.Show();
	}
}
