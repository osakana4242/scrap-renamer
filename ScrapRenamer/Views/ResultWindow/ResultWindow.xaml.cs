using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using ScrapRenamer.Common;
using ScrapRenamer.Views.SettingsWindow;

namespace ScrapRenamer.Views.ResultWindow;

public partial class ResultWindow : Window {

	public ResultWindow(FlowDocument doc) {
		InitializeComponent();
		ResultTextBox.Document = doc;
		ThemeManager.Add(this);
	}

	public ResultWindow(string text) :
		this(Util.TextToDoc(text)) {
	}

	// public ResultWindow() {
	// 	InitializeComponent();
	// 	ThemeMode = Settings.Instance.themeProp.Value;
	// 	var doc = new FlowDocument();
	// 	var p = new Paragraph();
	// 	p.Inlines.Add(new Run("成功"));
	// 	p.Inlines.Add(new Run("失敗") {
	// 		Foreground = Brushes.Red,
	// 	});
	// 	doc.Blocks.Clear();
	// 	doc.Blocks.Add(p);
	// 	this.ResultTextBox.Document = doc;
	// }

	void OnOkClick(
		object sender,
		RoutedEventArgs e) {
		Close();
	}

	private void OnRequestNavigate(
		object sender,
		RequestNavigateEventArgs e) {
		Process.Start(new ProcessStartInfo {
			FileName = e.Uri.AbsoluteUri,
			UseShellExecute = true,
		});

		e.Handled = true;
	}

	static class Util {
		public static FlowDocument TextToDoc(string text) {
			var doc = new FlowDocument();
			var fontFamily = Settings.Instance.FontFamily;
			var thickness = new Thickness(0); ;
			foreach (var line in text.Split('\n')) {
				var p = new Paragraph() {
					FontFamily = fontFamily,
					FontSize = Settings.Instance.fontSizeProp.Value,
					Margin = thickness,
				};
				p.Inlines.Add(line);
				doc.Blocks.Add(p);
			}
			return doc;
		}
	}
}
