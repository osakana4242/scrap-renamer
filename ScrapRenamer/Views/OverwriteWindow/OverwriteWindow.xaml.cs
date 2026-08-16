namespace ScrapRenamer.Views.OverwriteWindow;

using System.Windows;
using ScrapRenamer.Common;

public partial class OverwriteWindow : Window {
	public ThemeMode ThemeMode { get; set; } = ThemeMode.System;
	public OverwriteWindowResult Result { get; private set; }

	public OverwriteWindow() {
		InitializeComponent();
		WindowUtil.Add(this);
	}

	void OnOverwriteClicked(object sender, RoutedEventArgs e) {
		Result = OverwriteWindowResult.Overwrite;
		DialogResult = true;
	}

	void OnOverwriteAllClicked(object sender, RoutedEventArgs e) {
		Result = OverwriteWindowResult.OverwriteAll;
		DialogResult = true;
	}

	void OnSkipClicked(object sender, RoutedEventArgs e) {
		Result = OverwriteWindowResult.Skip;
		DialogResult = true;
	}

	void OnSkipAllClicked(object sender, RoutedEventArgs e) {
		Result = OverwriteWindowResult.SkipAll;
		DialogResult = true;
	}

	void OnCancelClicked(object sender, RoutedEventArgs e) {
		Result = OverwriteWindowResult.Cancel;
		DialogResult = false;
	}
}
