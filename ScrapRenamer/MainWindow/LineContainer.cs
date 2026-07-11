using System.Diagnostics;
using System.IO;

namespace ScrapRenamer;

class LineContainer {
	Mode _mode = Mode.Name;
	public List<Line> Lines { get; set; } = new();

	public bool Add(Line line) {
		if (null != Lines.Find(l => l.origPath == line.origPath)) {
			return false;
		}

		switch (_mode) {
		case Mode.Name:
			line.editedLine = Path.GetFileName(line.origPath);
			break;
		case Mode.FullPath:
			line.editedLine = line.origPath;
			break;
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

		string GetNextPath(Line line) {
			switch (_owner._mode) {
			case Mode.Name:
				return Path.Combine(Path.GetDirectoryName(line.origPath) ?? "", line.editedLine);
			case Mode.FullPath:
				return line.editedLine;
			default:
				throw new System.NotSupportedException($"{_owner._mode}");
			}
		}

		void BuildWorkList() {
			// エラーをリセット
			for (int i = 0; i < Lines.Count; i++) {
				var line = Lines[i];
				line.error = "";
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
				
				var cycleLast = cycleList[cycleList.Count - 1];

				// 工程を2つに分割する
				// 1. 最初に一時的なパスに変更
				// before -> tmp
				var suffix = ".tmp_" + Guid.NewGuid().ToString("N");
				var tmpPath = $"{cycleLast.after}{suffix}";
				var lastBeforeToTmp = new WorkItem(cycleLast.index, cycleLast.before, tmpPath);
				// 2. 最後に目的のパスに変更
				// tmp -> after
				var lastTmpToAfter = new WorkItem(cycleLast.index, tmpPath, cycleLast.after);
				RemoveWorkItem(cycleLast);

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
					System.IO.File.Move(item.before, item.after);
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
