using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using ScrapRenamer.Common;

namespace ScrapRenamer.Views.SettingsWindow;

public partial class SettingsWindow : Window {

	public SettingsWindow() {
		InitializeComponent();

		ThemeComboBox.ItemsSource = new[] {
			ThemeMode.System,
			ThemeMode.Dark,
			ThemeMode.Light,
		};
		ThemeComboBox.SelectedItem = Settings.Instance.themeProp.Value;
		ThemeComboBox.SelectionChanged += OnThemeSelectionChanged;

		var f = new FontFamily(Settings.Instance.fontFamilyProp.Value);

		FontFamilyComboBox.ItemsSource = Fonts.SystemFontFamilies
			.OrderBy(x => x.Source);

		FontFamilyComboBox.DisplayMemberPath = "Source";
		FontFamilyComboBox.SelectedValuePath = "Source";
		FontFamilyComboBox.SelectedItem = f;
		FontFamilyComboBox.SelectionChanged += OnSelectionChanged;

		FontSizeComboBox.ItemsSource = new double[] {
			8, 9, 10, 11, 12, 14, 16, 18,
			20, 22, 24, 26, 28, 36, 48, 72
		};
		FontSizeComboBox.SelectionChanged += OnFontSizeSelectionChanged;
		FontSizeComboBox.Text = Settings.Instance.fontSizeProp.Value.ToString();
		ThemeManager.Add(this);
	}

	public void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (ThemeComboBox.SelectedItem is not ThemeMode themeMode) return;
		Settings.Instance.themeProp.Value = themeMode;
	}

	public void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (FontFamilyComboBox.SelectedItem is not FontFamily font) return;
		Settings.Instance.fontFamilyProp.Value = font.Source;
	}

	public void OnFontSizeSelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (double.TryParse(FontSizeComboBox.Text, out var fontSize1)) {
			Settings.Instance.fontSizeProp.Value = (int)fontSize1;
		} else if (FontSizeComboBox.SelectedItem is double fontSize2) {
			Settings.Instance.fontSizeProp.Value = (int)fontSize2;
		}
	}
}
