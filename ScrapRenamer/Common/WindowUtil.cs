using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using ModernWpf;
using ModernWpf.Controls;
using ModernWpf.Controls.Primitives;
using ScrapRenamer.Platform.Windows;

namespace ScrapRenamer.Common;

// ウィンドウのテーマ変更の自動追従
public static class WindowUtil {
	static HashSet<Window> _s_windows = new();

	static WindowUtil() {
		SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
		SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
	}

	// 監視対象に加える
	public static void Add(Window window) {
		if (!_s_windows.Add(window)) return;

		ApplyStyle(window);

		// ウィンドウが不要になったら監視対象から外す
		window.Closed += (object sender, EventArgs args) => {
			Debug.Print($"Removed: {window.GetType().Name}, sender: {sender}, args: {args}");
			Remove(window);
		};
	}

	public static void Remove(Window window) {
		_s_windows.Remove(window);
	}

	// 設定に応じたスタイルを適用する
	static void ApplyStyle(Window window) {
		SolidColorBrush brush = Dwm.IsTitleBarAccentEnabled() ?
			new SolidColorBrush(ThemeManager.Current.ActualAccentColor) :
			new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

		WindowTitleBar.SetBackground(
			window,
			brush
		);
		
		WindowHelper.SetUseModernWindowStyle(window, true);
	}

	static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
		foreach (var window in _s_windows) {
			ApplyStyle(window);
		}
	}
}
