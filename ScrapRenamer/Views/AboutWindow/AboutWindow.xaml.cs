using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using ScrapRenamer.Common;
using ScrapRenamer.Views.SettingsWindow;

namespace ScrapRenamer;


public partial class AboutWindow : Window {
	public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

	public AboutWindow() {
		InitializeComponent();
		ThemeMode = Settings.Instance.themeProp.Value;

		VersionText.Text = string.Format(
			Localization.Strings.Strings.AboutWindow_VersionFormat,
			Version);
		WindowUtil.Add(this);
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

		if (sender is Hyperlink hyperlink &&
				hyperlink.Inlines.FirstInline is Run run) {
			Process.Start(new ProcessStartInfo(run.Text) {
				UseShellExecute = true,
			});
		}

		e.Handled = true;
	}
}
