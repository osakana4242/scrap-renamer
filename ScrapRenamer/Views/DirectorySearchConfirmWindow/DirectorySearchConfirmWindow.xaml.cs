using System.Windows;
using ScrapRenamer.Common;

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
