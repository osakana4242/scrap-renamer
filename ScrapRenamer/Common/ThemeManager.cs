using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ModernWpf;
using ModernWpf.Controls;
using ModernWpf.Controls.Primitives;
using ScrapRenamer.Common.Platform.Windows;

namespace ScrapRenamer.Common;

// ウィンドウのテーマ変更の自動追従
public static class ThemeManager {
	static HashSet<Window> _s_windows = new();
	static readonly ImageSource _s_appIcon =
		new BitmapImage(new Uri(
			"pack://application:,,,/AppIcon/AppIcon_x32.png"));

	static ThemeManager() {
		SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
		SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
		Settings.Instance.themeProp.OnChanged += (v) => {
			ApplyStyle();
		};
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
		window.Icon = _s_appIcon;
		WindowHelper.SetUseModernWindowStyle(window, true);
		WindowTitleBar.SetIsIconVisible(window, true);

		var theme = Settings.Instance.themeProp.Value;
		ModernWpf.ThemeManager.Current.ApplicationTheme =
			theme == ThemeMode.Light ? ApplicationTheme.Light :
			theme == ThemeMode.Dark ? ApplicationTheme.Dark :
			null;

		Dwm.SetWindowDarkMode(window, theme.IsDarkMode());

		var isTitleBarAccentEnabled = Dwm.IsTitleBarAccentEnabled();

		if (isTitleBarAccentEnabled) {
			// アクセントカラーがタイトルバーに反映されてる場合。

			// 背景色
			WindowTitleBar.SetBackground(
				window,
				new SolidColorBrush(ModernWpf.ThemeManager.Current.ActualAccentColor));

			// 文字色を白にする

			// タイトル文字
			WindowTitleBar.SetForeground(
				window,
				new SolidColorBrush(Color.FromRgb(255, 255, 255)));

			// WindowTitleBar.SetInactiveForeground(
			// 	window,
			// 	white);

			// ボタン文字
			WindowTitleBar.SetButtonStyle(
				window,
				CreateTitleBarButtonStyle());
		} else {
			// アクセントカラーがタイトルバーに反映されていない場合。

			// 背景色
			WindowTitleBar.SetBackground(
				window,
				new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)));

			// タイトル文字とボタン文字をデフォにする。
			// タイトル文字色
			WindowTitleBar.SetForeground(window, window.Foreground);
			// WindowTitleBar.SetInactiveForeground(window, window.Foreground);
			// ボタン文字色
			WindowTitleBar.SetButtonStyle(window, null);
		}
	}

	static void ApplyStyle() {
		foreach (var window in _s_windows) {
			ApplyStyle(window);
		}
	}

	static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
		ApplyStyle();
	}


	static Style CreateTitleBarButtonStyle() {
		var baseStyle = (Style)Application.Current.FindResource(
		typeof(TitleBarButton));

		var style = new Style(
		typeof(TitleBarButton),
		baseStyle);

		var white = new SolidColorBrush(
		Color.FromRgb(255, 255, 255));

		style.Setters.Add(new Setter(
			TitleBarButton.ForegroundProperty,
			white));

		// style.Setters.Add(new Setter(
		// 	TitleBarButton.InactiveForegroundProperty,
		// 	white));

		return style;
	}
}
