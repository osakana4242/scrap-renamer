using System.Diagnostics;
using System.Globalization;
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

		LanguageComboBox.ItemsSource = CultureInfoUtil.Values;
		LanguageComboBox.DisplayMemberPath = "NativeName"; /// <see cref="CultureInfo.NativeName" />
		LanguageComboBox.SelectedItem = Settings.Instance.languageProp.Value;
		LanguageComboBox.SelectionChanged += OnLanguageSelectionChanged;

		ThemeComboBox.ItemsSource = ThemeMode.Values;
		ThemeComboBox.SelectedItem = Settings.Instance.themeProp.Value;
		ThemeComboBox.SelectionChanged += OnThemeSelectionChanged;

		FontFamilyComboBox.ItemsSource = Fonts.SystemFontFamilies
			.OrderBy(x => x.Source);
		var f = new FontFamily(Settings.Instance.fontFamilyProp.Value);
		FontFamilyComboBox.SelectedItem = f;
		FontFamilyComboBox.DisplayMemberPath = "Source"; /// <see cref="FontFamily.Source" />
		FontFamilyComboBox.SelectedValuePath = "Source"; /// <see cref="FontFamily.Source" />
		FontFamilyComboBox.SelectionChanged += OnSelectionChanged;

		FontSizeComboBox.ItemsSource = new double[] {
			8, 9, 10, 11, 12, 14, 16, 18,
			20, 22, 24, 26, 28, 36, 48, 72
		};
		FontSizeComboBox.SelectionChanged += OnFontSizeSelectionChanged;
		FontSizeComboBox.Text = Settings.Instance.fontSizeProp.Value.ToString();

		ThemeManager.Add(this);
	}

	public void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (LanguageComboBox.SelectedItem is not CultureInfo language) return;
		Settings.Instance.languageProp.Value = language;
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
