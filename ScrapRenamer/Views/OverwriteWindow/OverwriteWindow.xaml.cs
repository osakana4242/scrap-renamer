namespace ScrapRenamer.View;

using System.Windows;

public partial class OverwriteWindow : Window {

	public OverwriteWindowResult Result { get; private set; }

	public bool ApplyToAll {
		get => ApplyToAllCheckBox.IsChecked == true;
	}

	public OverwriteWindow() {
		InitializeComponent();
	}

	private void OnOverwriteClicked(object sender, RoutedEventArgs e) {
		Result = OverwriteWindowResult.Overwrite;
		DialogResult = true;
	}

	private void OnSkipClicked(object sender, RoutedEventArgs e) {
		Result = OverwriteWindowResult.Skip;
		DialogResult = true;
	}

	private void OnCancelClicked(object sender, RoutedEventArgs e) {
		Result = OverwriteWindowResult.Cancel;
		DialogResult = false;
	}
}
