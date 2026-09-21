using System.Globalization;
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

		Settings.Instance.Load();
		CultureInfo.CurrentUICulture = Settings.Instance.languageProp.Value;
		//CultureInfo.CurrentCulture = Settings.Instance.languageProp.Value;

		MainWindow window = new MainWindow(e.Args);
		window.Show();
	}
}
