namespace ScrapRenamer.Common;

public record class ThemeMode(string Value) {
	
	public static ThemeMode System = new("System");
	public static ThemeMode Dark = new("Dark");
	public static ThemeMode Light = new("Light");


	public string MonacoEditorTheme => IsDarkMode() ?
		"vs-dark" :
		"vs";


	// System なら Dark or Light に具体化する。それ以外はそのまま。
	public ThemeMode Resolve() {
		if (this == System) {
			return Lib.Platform.Windows.Theme.IsDarkMode() ?
				Dark :
				Light;
		}
		return this;
	}

	public bool IsDarkMode() => Resolve() == Dark;

	public override string ToString() {
		return Value;
	}
}
