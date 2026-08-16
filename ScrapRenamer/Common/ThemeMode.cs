namespace ScrapRenamer.Common;

public record class ThemeMode(string Value) {
	public static ThemeMode System = new("System");
	public static ThemeMode Dark = new("Dark");
	public static ThemeMode Light = new("Light");

	public bool IsDarkMode() {
		if (this == Dark) return true;
		if (this == System) {
			return Platform.Windows.Theme.IsDarkMode();
		}
		return false;
	}

	public override string ToString() {
		return Value;
	}
}
