using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ScrapRenamer.Views.MainWindow;

public static class WpfUtil {
	public static T? FindParent<T>(DependencyObject element)
		where T : DependencyObject {
		while (element != null) {
			element = element is Visual || element is Visual3D
				? VisualTreeHelper.GetParent(element)
				: LogicalTreeHelper.GetParent(element);

			if (element is T result) {
				return result;
			}
		}

		return null;
	}
}
