using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ModernWpf;
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
		if (!window.IsLoaded) {
			window.Loaded -= OnLoaded;
			window.Loaded += OnLoaded;
		} else {
			ApplyStyleAfterWindowSourceInitialized(window);
		}

		static void OnLoaded(object sender, EventArgs e) {
			if (sender is not Window window) return;
			// Debug.Print($"sender: {sender.ToString()}, type: {sender.GetType().Name}");
			ApplyStyleAfterWindowSourceInitialized(window);
		}
	}

	// 設定に応じたスタイルを適用する
	static void ApplyStyleAfterWindowSourceInitialized(Window window) {
		var theme = Settings.Instance.themeProp.Value;

		Dwm.SetWindowDarkMode(window, theme.IsDarkMode());

		// ウィンドウ右端、下端にマウスオーバーしてもポインターがリサイズ様に切り替わらない問題があるので true にできない
		var useModernWindowStyle = false;

		ModernWpf.ThemeManager.Current.ApplicationTheme =
			theme == ThemeMode.Light ? ApplicationTheme.Light :
			theme == ThemeMode.Dark ? ApplicationTheme.Dark :
			null;


		SetThemeToMergedDictionaries(theme);

		WindowHelper.SetUseModernWindowStyle(window, useModernWindowStyle);


		window.Background = (SolidColorBrush)Application.Current.Resources[
			"WindowBackgroundBrush"];

		if (useModernWindowStyle) {
			// userModerWindowStyle だと、
			// タイトルバーにアクセントカラーが反映されなくなってしまうため、
			// 手動で設定する。

			ModernWpf.Controls.WindowTitleBar.SetIsIconVisible(window, true);

			var isTitleBarAccentEnabled = Dwm.IsTitleBarAccentEnabled();

			if (isTitleBarAccentEnabled) {
				// アクセントカラーがタイトルバーに反映されてる場合。

				// 背景色
				ModernWpf.Controls.WindowTitleBar.SetBackground(
					window,
					new SolidColorBrush(ModernWpf.ThemeManager.Current.ActualAccentColor));

				// 文字色を白にする

				// タイトル文字
				ModernWpf.Controls.WindowTitleBar.SetForeground(
					window,
					new SolidColorBrush(Color.FromRgb(255, 255, 255)));

				// WindowTitleBar.SetInactiveForeground(
				// 	window,
				// 	white);

				// ボタン文字
				ModernWpf.Controls.WindowTitleBar.SetButtonStyle(
					window,
					CreateTitleBarButtonStyle());
			} else {
				// アクセントカラーがタイトルバーに反映されていない場合。

				// 背景色
				ModernWpf.Controls.WindowTitleBar.SetBackground(
					window,
					new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)));

				// タイトル文字とボタン文字をデフォにする。
				// タイトル文字色
				ModernWpf.Controls.WindowTitleBar.SetForeground(window, window.Foreground);
				// WindowTitleBar.SetInactiveForeground(window, window.Foreground);
				// ボタン文字色
				ModernWpf.Controls.WindowTitleBar.SetButtonStyle(window, null);
			}
		}
	}

	static void ApplyStyle() {
		foreach (var window in _s_windows) {
			ApplyStyle(window);
		}
	}

	// MergedDictionaries を指定のテーマの要素に入れ替える
	static void SetThemeToMergedDictionaries(ThemeMode theme) {
		var themeName = theme.Resolve().ToString();
		var dicts = Application.Current.Resources.MergedDictionaries;

		// 既存テーマ削除
		var oldTheme = dicts.FirstOrDefault(d =>
			d.Source != null && (
				d.Source.OriginalString.Contains($"Themes/{ThemeMode.Light}.xaml") ||
				d.Source.OriginalString.Contains($"Themes/{ThemeMode.Dark}.xaml")));

		if (oldTheme != null) {
			Debug.WriteLine($"Remove {oldTheme}");
			dicts.Remove(oldTheme);
		}

		// 新しいテーマ追加
		var newTheme = new ResourceDictionary();
		newTheme.Source = new Uri($"Themes/{themeName}.xaml", UriKind.Relative);

		dicts.Add(newTheme);
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
