using System.Diagnostics;
using System.IO;
using ScrapRenamer.View;

namespace ScrapRenamer;

class LineContainer {
	RenameMode _mode;
	public List<Line> Lines { get; set; } = new();

	public LineContainer(RenameMode mode) {
		_mode = mode;
	}

	public void SetMode(RenameMode mode) {
		foreach (var line in Lines) {
			line.Mode = mode;
		}
		_mode = mode;
	}

	public bool Add(Line line) {
		if (null != Lines.Find(l => l.origPath == line.origPath)) {
			return false;
		}
		Lines.Add(line);
		return true;
	}

	public void Sort() {
		Lines.Sort((a, b) => {
			return a.origPath.CompareTo(b.origPath);
		});
	}

	public void Apply() {
		var context = new RenameContext(this);
		context.Apply();
	}

	// リネームのパターン
	//
	// Chain
	// 末尾から解決してけば衝突しない
	//
	// 1.
	// a -> b
	//
	// 2.
	// a -> b
	// b -> c
	// 
	// 3.
	// a -> b
	// b -> c
	// c -> d
	//
	// Cycle
	// どこかで一時的な名前を経由しないと解決できない
	//
	// 1. (リネームしてない)
	// a -> a
	//
	// 2.
	// a -> b
	// b -> a
	//
	// 3.
	// a -> b
	// b -> c
	// c -> a
	//
	class RenameContext {
		readonly List<WorkItem> _workList = new();
		readonly Dictionary<string, WorkItem> _beforePathDict = new();
		readonly Dictionary<string, WorkItem> _afterPathDict = new();
		readonly LineContainer _owner;


		IReadOnlyList<Line> Lines => _owner.Lines;


		public RenameContext(LineContainer owner) {
			_owner = owner;
		}

		public void Apply() {
			Debug.WriteLine("Apply");
			BuildWorkList();
			// Chain を片付ける
			ProcessChain();
			// 残りは Cycle
			ProcessCycle();
		}

		void RemoveWorkItem(WorkItem item) {
			_workList.Remove(item);
			_beforePathDict.Remove(item.before);
			_afterPathDict.Remove(item.after);
		}

		Line GetLine(WorkItem item) => Lines[item.index];

		string GetNextPath(Line line) => line.GetNextPath();

		void BuildWorkList() {
			// エラーをリセット
			for (int i = 0; i < Lines.Count; i++) {
				var line = Lines[i];
				line.error = "";
				line.processed = false;
			}
			//
			for (int i = 0; i < Lines.Count; i++) {
				var line = Lines[i];
				if (line.origPath == line.editedLine) {
					continue;
				}
				var nextPath = GetNextPath(line);
				Debug.WriteLine($"Renaming: {line.origPath} -> {line.editedLine} ({nextPath})");
				WorkItem item = new(i, line.origPath, nextPath);
				if (item.before == item.after) {
					// リネームしてない
					continue;
				}

				line.processed = true;

				if (_afterPathDict.TryGetValue(nextPath, out var otherItem)) {
					line.error = $"{otherItem.index}: {otherItem.before} とリネーム先が衝突";
					var otherLine = GetLine(otherItem);
					otherLine.error = $"{item.index}: {item.before} とリネーム先が衝突";
					continue;
				}
				_afterPathDict.Add(nextPath, item);
				_beforePathDict.Add(line.origPath, item);
				_workList.Add(item);
			}
		}

		void ProcessChain() {
			List<WorkItem> chainList = new();
			WorkItem? chainFirst;
			while ((chainFirst = FindChainFirst()) != null) {
				chainList.Clear();
				var item1 = chainFirst;
				chainList.Add(item1);
				while (_beforePathDict.TryGetValue(item1.after, out var item2)) {
					chainList.Add(item2);
					item1 = item2;
				}
				string preError = "";
				for (int j = chainList.Count - 1; 0 <= j; j--) {
					var item = chainList[j];
					RenameItem(item, ref preError);
				}
			}
		}

		void ProcessCycle() {
			List<WorkItem> cycleList = new();
			HashSet<string> visited = new();
			while (0 < _workList.Count) {
				cycleList.Clear();
				visited.Clear();
				{
					var item1 = _workList[0];
					cycleList.Add(item1);
					visited.Add(item1.before);
					while (_beforePathDict.TryGetValue(item1.after, out var item2)) {
						var isCycle = visited.Contains(item2.after);
						cycleList.Add(item2);
						visited.Add(item2.before);
						if (isCycle) {
							break;
						}
						item1 = item2;
					}
				}

				var last = cycleList[cycleList.Count - 1];
				cycleList.RemoveAt(cycleList.Count - 1);

				// 工程を2つに分割する
				// 1. 最初に一時的なパスに変更
				// before -> tmp
				var suffix = ".tmp_" + Guid.NewGuid().ToString("N");
				var tmpPath = $"{last.after}{suffix}";
				var lastBeforeToTmp = new WorkItem(last.index, last.before, tmpPath);
				// 2. 最後に目的のパスに変更
				// tmp -> after
				var lastTmpToAfter = new WorkItem(last.index, tmpPath, last.after);
				RemoveWorkItem(last);

				var preError = "";

				RenameItem(lastBeforeToTmp, ref preError);
				for (int j = cycleList.Count - 1; 0 <= j; j--) {
					var item = cycleList[j];
					RenameItem(item, ref preError);
				}
				RenameItem(lastTmpToAfter, ref preError);
			}
		}

		void RenameItem(WorkItem item, ref string preError) {
			var line = GetLine(item);
			try {
				if (line.error != "") {
					preError = line.error;
				} else if (preError != "") {
					line.error = preError;
				} else {
					Debug.WriteLine($"Move, '{item.before}' to '{item.after}'");

					if (line.Mode == RenameMode.FullPath) {
						var parent = Path.GetDirectoryName(item.after);
						if (parent != null) {
							Directory.CreateDirectory(parent);
						}
					}

					var protectedDirectory = GetProtectedDirectory(item.before, item.after);

					if (line.isDirectory) {
						System.IO.Directory.Move(item.before, item.after);
						DeleteEmptyDirectories(
							item.before,
							protectedDirectory);
					} else {


						bool overwrite = false;

						if (System.IO.File.Exists(item.after)) {
							var window = new OverwriteWindow();
							window.ShowDialog();
							switch (window.Result) {
							case OverwriteWindowResult.Skip:
								line.error = $"同名のファイルが存在";
								return;
							case OverwriteWindowResult.Overwrite:
								overwrite = true;
								break;
							case OverwriteWindowResult.Cancel:
								throw new System.OperationCanceledException();
							}
						}

						System.IO.File.Move(item.before, item.after, overwrite);
						DeleteEmptyDirectories(
							Path.GetDirectoryName(item.before),
							protectedDirectory);
					}

					line.origPath = item.after;
				}
			} catch (System.Exception ex) {
				line.error = $"{ex.Message}";
				preError = $"{line.origPath} のリネーム失敗に引きずられて失敗";
				Debug.WriteLine($"ex: {ex}, before: {item.before}, after: {item.after}");
			} finally {
				RemoveWorkItem(item);
			}
		}

		// 空ディレクトリを再帰的に削除する。
		// ただし protectedDirectory は削除しない。
		static void DeleteEmptyDirectories(
			string? directory,
			string protectedDirectory) {
			while (!string.IsNullOrEmpty(directory)) {
				try {
					if (string.Equals(
							directory,
							protectedDirectory,
							StringComparison.OrdinalIgnoreCase)) {

						break;
					}

					if (Directory.EnumerateFileSystemEntries(directory).Any()) {
						break;
					}

					Directory.Delete(directory);

					directory = Path.GetDirectoryName(directory);
				} catch (System.Exception ex) {
					Debug.Print($"ex: {ex}, directory: {directory}");
				}
			}
		}

		static string GetProtectedDirectory(string before, string after) {
			var beforeDir = Path.GetDirectoryName(before)!;
			var afterDir = Path.GetDirectoryName(after)!;

			var beforeParts = beforeDir.Split('\\');
			var afterParts = afterDir.Split('\\');

			int count = Math.Min(beforeParts.Length, afterParts.Length);

			int sameCount = 0;
			while (sameCount < count &&
				string.Equals(
					beforeParts[sameCount],
					afterParts[sameCount],
					StringComparison.OrdinalIgnoreCase)) {

				sameCount++;
			}

			if (sameCount == 0) {
				return "";
			}

			return string.Join('\\', beforeParts[..sameCount]);
		}


		WorkItem? FindChainFirst() {

			for (int i = 0; i < _workList.Count; i++) {
				var item1 = _workList[i];
				var isChainFirst = !_afterPathDict.ContainsKey(item1.before);
				if (isChainFirst) {
					return item1;
				}
			}
			return null;
		}

		record class WorkItem(int index, string before, string after);
	}
}
