using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace ScrapRenamer.Common.Platform.Windows;

// ウィンドウのタイトルバーの配色を設定する
// Dwm: Desktop Window Manager
public static class Dwm {

	const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	// ウィンドウのタイトルバーにアクセントカラーを適用する設定になっているか
	public static bool IsTitleBarAccentEnabled() {
		using var key = Registry.CurrentUser.OpenSubKey(
		@"Software\Microsoft\Windows\DWM");

		return key?.GetValue("ColorPrevalence") is int value &&
			   value != 0;
	}

	public static void SetWindowDarkMode(Window window, bool b) {
		var hwnd = new WindowInteropHelper(window).Handle;

		int enabled = b ? 1 : 0;
		DwmSetWindowAttribute(
			hwnd,
			DWMWA_USE_IMMERSIVE_DARK_MODE,
			ref enabled,
			sizeof(int));
	}

	[DllImport("dwmapi.dll")]
	static extern int DwmSetWindowAttribute(
		IntPtr hwnd,
		int dwAttribute,
		ref int pvAttribute,
		int cbAttribute);

}
