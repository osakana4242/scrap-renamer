namespace System.Windows;

public class ThemeMode {
	public static ThemeMode System = new("System");
	public static ThemeMode Dark = new("Dark");
	public static ThemeMode Light = new("Light");

	readonly string _value;

	public string Value => _value;

	public ThemeMode(string value) {
		_value = value;
	}
}
