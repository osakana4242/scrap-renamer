namespace ScrapRenamer.Views.MainWindow;

public static class PathUtil {
	// 後ろから n 番目のセパレーターの位置でパスを分割する
	public static (string prefix, string suffix) SplitPathAtNthLastSeparator(string path, int n) {
		var index = GetNthLastSeparatorIndex(path, n);

		if (index == -1) {
			return (
				"",
				path);
		} else {
			return (
				path[..index],
				path[index..]);
		}

		static int GetNthLastSeparatorIndex(string text, int n) {
			int separatorCount = 0;
			int firstSeparatorIndex = -1;

			for (int i = text.Length - 1; i >= 0; i--) {
				if (text[i] != '\\') {
					continue;
				}

				firstSeparatorIndex = i;
				separatorCount++;

				if (separatorCount == n) {
					return i;
				}
			}

			return firstSeparatorIndex;
		}
	}

}
