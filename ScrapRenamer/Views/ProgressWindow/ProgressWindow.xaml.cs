using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using ScrapRenamer.Common;
using ScrapRenamer.Views.SettingsWindow;

namespace ScrapRenamer;


public partial class ProgressWindow : Window {

	CancellationTokenSource _cts = new CancellationTokenSource();

	public ProgressWindow(CancellationTokenSource cts) {
		InitializeComponent();
		ThemeManager.Add(this);
		_cts = cts;
	}

	protected override void OnClosed(EventArgs e) {
		_cts.Cancel();
		base.OnClosed(e);
	}

	void OnCancelClick(
		object sender,
		RoutedEventArgs e) {
		Close();
	}
}
