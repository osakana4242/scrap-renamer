namespace ScrapRenamer.Views.ProgressWindow;

public sealed class ProgressReport {

	Data _data = new Data();

	public string format => _data.format;

	public string text => _data.text;

	public int progressCount {
		get => _data.progressCount;
		set => _data.progressCount = value;
	}

	public void SetFormat(string format, int progressCount = 0) {
		// フォーマットとカウントの関係がくずれないように、一括で変更する
		_data = new Data() {
			format = format,
			progressCount = progressCount,
		};
	}

	sealed class Data {
		public string format = "";
		public int progressCount;
		public string text => string.Format(format, progressCount);
	}
}
