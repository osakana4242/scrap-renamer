using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace ScrapRenamer;

public partial class AboutWindow : Window {
	public AboutWindow() {
		InitializeComponent();
		ThemeMode = Settings.Instance.themeProp.Value;

		VersionText.Text = $"バージョン {Version}";
	}

	string Version =>
		Assembly.GetExecutingAssembly()
			.GetCustomAttribute<
				AssemblyInformationalVersionAttribute>()
			?.InformationalVersion ?? "";

	void OnCopyClick(
		object sender,
		RoutedEventArgs e) {
		var text = $"""
		ScrapRenamer
		Version: {Version}
		""";

		Clipboard.SetText(text);
	}
	void OnOkClick(
		object sender,
		RoutedEventArgs e) {
		Close();
	}

	private void OnRequestNavigate(
		object sender,
		RequestNavigateEventArgs e) {
		Process.Start(new ProcessStartInfo {
			FileName = e.Uri.AbsoluteUri,
			UseShellExecute = true,
		});

		e.Handled = true;
	}
}
