using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScrapRenamer;

static class Dwm {
	[DllImport("dwmapi.dll")]
	static extern int DwmSetWindowAttribute(
		IntPtr hwnd,
		int dwAttribute,
		ref int pvAttribute,
		int cbAttribute);

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
}
