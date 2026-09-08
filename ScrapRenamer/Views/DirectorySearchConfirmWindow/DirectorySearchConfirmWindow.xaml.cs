using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using ScrapRenamer.Common;
using ScrapRenamer.Views.SettingsWindow;

namespace ScrapRenamer;


public partial class DirectorySearchConfirmWindow : Window {

	Result _result = Result.Cancel;

	public DirectorySearchConfirmWindow(string message) {
		InitializeComponent();
		ThemeManager.Add(this);
		MessageTextBlock.Text = message;
	}

	public Result GetResult() => _result;

	protected override void OnClosed(EventArgs e) {
		base.OnClosed(e);
	}

	void OnCurrentDirectoryClick(
		object sender,
		RoutedEventArgs e) {
		_result = Result.CurrentDirectory;
		Close();
	}

	void OnIncludeSubdirectoriesClick(
		object sender,
		RoutedEventArgs e) {
		_result = Result.IncludeSubdirectories;
		Close();
	}

	public enum Result {
		Cancel,
		CurrentDirectory,
		IncludeSubdirectories,
	}
}
