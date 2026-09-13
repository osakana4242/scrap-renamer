using System.Diagnostics;
using System.Windows;
using ScrapRenamer.Common;

namespace ScrapRenamer.Views.ProgressWindow;

public sealed partial class ProgressWindow : Window {

	// 表示を開始するまでの遅延時間
	const int FirstDelayMsec = 200;
	// 進捗の更新間隔
	const int IntervalMsec = 100;
	// 表示終了までの遅延時間
	const int LastDelayMsec = 200;

	Task _task;
	ProgressReport _report;
	CancellationTokenSource _cts;

	public ProgressWindow(Task task, ProgressReport report, CancellationTokenSource cts) {
		InitializeComponent();
		ThemeManager.Add(this);
		_task = task;
		_report = report;
		_cts = cts;
		Loaded += OnLoaded;
	}

	// タスクの終了を待つ
	// 一定時間経過してもタスクが終わらない場合は進行状況の表示をする
	public static async Task<T> Show<T>(Window owner, Task<T> task, ProgressReport report, CancellationTokenSource cts) {
		try {
			owner.IsEnabled = false;
			await Task.Delay(FirstDelayMsec, cts.Token);
			if (!task.IsCompleted) {
				var wnd = new ProgressWindow(task, report, cts);
				try {
					wnd.Owner = owner;
					wnd.ShowDialog();
				} finally {
					wnd.Close();
				}
			}
			return await task;
		} finally {
			Debug.Print($"ProgressWindow6");
			owner.IsEnabled = true;
			Debug.Print($"ProgressWindow7");
		}
	}

	// 100ms 置きにステータスを更新する
	// ここで発生する例外は ShowDialog 呼び出し側で処理する
	async void OnLoaded(object sender, RoutedEventArgs args) {
		Title = _report.text;
		while (!_task.IsCompleted) {
			Debug.Print($"Poling...");
			Title = _report.text;
			await Task.Delay(IntervalMsec);
		}
		Title = _report.text;
		await Task.Delay(LastDelayMsec);
		Close();
	}

	void OnCancelClick(object sender, RoutedEventArgs e) {
		Close();
	}
}
