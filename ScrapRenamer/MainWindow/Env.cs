using Microsoft.Win32;

namespace ScrapRenamer;

public partial class MainWindow {
	public static class Env {

		public static bool IsDarkMode() {
			object? value = Registry.GetValue(
			@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
			"AppsUseLightTheme",
			1);

			return value is int light && light == 0;
		}
	}
}
