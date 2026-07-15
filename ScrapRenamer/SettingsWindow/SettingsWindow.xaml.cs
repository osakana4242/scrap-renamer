using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace ScrapRenamer;

public partial class SettingsWindow : Window {
	public SettingsWindow() {
		InitializeComponent();

		var f = new FontFamily(Settings.Instance.fontFamily.Value);

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
		FontSizeComboBox.Text = Settings.Instance.fontSize.Value.ToString();
	}

	public void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (FontFamilyComboBox.SelectedItem is not FontFamily font) return;
		Settings.Instance.fontFamily.Value = font.Source;
	}

	public void OnFontSizeSelectionChanged(object sender, SelectionChangedEventArgs e) {
			Debug.Print($"FontSize1 {FontSizeComboBox.SelectedItem}");
		if (double.TryParse(FontSizeComboBox.Text, out var fontSize1)) {
			Debug.Print("FontSize2");
			Settings.Instance.fontSize.Value = (int)fontSize1;
		} else if (FontSizeComboBox.SelectedItem is double fontSize2) {
			Debug.Print("FontSize3");
			Settings.Instance.fontSize.Value = (int)fontSize2;
		}
	}
}
