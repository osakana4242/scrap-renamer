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

		// ThemeManager.Current.AccentColor = Color.FromRgb(0x00, 0x80, 0x00);
		// ThemeManager.Current.ActualAccentColor

		MainWindow window = new MainWindow();
		if (e.Args.Length > 0) {
			window.OpenFiles(e.Args);
		}

		window.Show();
	}
}
