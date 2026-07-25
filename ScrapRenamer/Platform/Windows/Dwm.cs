using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScrapRenamer.Platform.Windows;

// ウィンドウのタイトルバーの配色を設定する
// Dwm: Desktop Window Manager
public static class Dwm {

	const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

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
