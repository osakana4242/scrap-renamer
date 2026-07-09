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

	record class WorkItem(int index, string before, string after);

	public void Apply() {

		List<WorkItem> workList = new();
		Dictionary<string, WorkItem> beforePathDict = new();
		Dictionary<string, WorkItem> afterPathDict = new();

		for (int i = 0; i < Lines.Count; i++) {
			var line = Lines[i];
			if (line.origPath == line.editedLine) {
				continue;
			}
			Debug.WriteLine($"Renaming: {line.origPath} -> {line.editedLine}");
			var nextPath = line.editedLine;
			switch (_mode) {
			case Mode.Name:
				nextPath = Path.Combine(Path.GetDirectoryName(line.origPath) ?? "", line.editedLine);
				break;
			case Mode.FullPath:
				nextPath = line.editedLine;
				break;
			}
			WorkItem item = new(i, line.origPath, nextPath);

			if (afterPathDict.TryGetValue(nextPath, out var otherItem)) {
				line.error = $"{otherItem.index}: {otherItem.before} とリネーム先が衝突";
				var otherLine = Lines[otherItem.index];
				otherLine.error = $"{item.index}: {item.before} とリネーム先が衝突";
				continue;
			}
			afterPathDict.Add(nextPath, item);
			beforePathDict.Add(line.origPath, item);
			workList.Add(item);
		}


		List<WorkItem> chainList = new();

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


		// Chain を片付ける
		WorkItem? chainFirst;
		while ((chainFirst = FindChainFirst(workList, afterPathDict)) != null) {
			chainList.Clear();
			var item1 = chainFirst;
			chainList.Add(item1);
			while (beforePathDict.TryGetValue(item1.after, out var item2)) {
				chainList.Add(item2);
				item1 = item2;
			}
			string preError = "";
			for (int j = chainList.Count - 1; 0 <= j; j--) {
				var item = chainList[j];
				var line = Lines[item.index];
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
				} finally {
					workList.Remove(item);
					beforePathDict.Remove(item.before);
					afterPathDict.Remove(item.after);
				}
			}
		}

		// 残りは Cycle
		HashSet<string> visited = new();
		while (0 < workList.Count) {
			chainList.Clear();
			visited.Clear();
			{
				var item1 = workList[0];
				chainList.Add(item1);
				visited.Add(item1.before);
				while (beforePathDict.TryGetValue(item1.after, out var item2)) {
					var isCycle = visited.Contains(item2.after);
					chainList.Add(item2);
					visited.Add(item2.before);
					if (isCycle) {
						break;
					}
					item1 = item2;
				}
			}
			var preError = "";
			var chainLast = chainList[chainList.Count - 1];
			{
				var tmpPath = Path.Combine(
							Path.GetDirectoryName(chainLast.after)!,
							Guid.NewGuid().ToString("N"));
				var line = Lines[chainLast.index];
				try {
					if (line.error != "") {
						preError = line.error;
					} else if (preError != "") {
						line.error = preError;
					} else {
						System.IO.File.Move(chainLast.before, tmpPath);
						line.origPath = tmpPath;

						var chainLast2 = new WorkItem(chainLast.index, tmpPath, chainLast.after);
						chainList.Insert(0, chainLast2);
					}
				} catch (System.Exception ex) {
					line.error = $"{ex.Message}";
					preError = $"{line.origPath} のリネーム失敗に引きずられて失敗";
				} finally {
					workList.Remove(chainLast);
					beforePathDict.Remove(chainLast.before);
					afterPathDict.Remove(chainLast.after);
				}
			}

			for (int j = chainList.Count - 1; 0 <= j; j--) {
				var item = chainList[j];
				var line = Lines[item.index];
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
				} finally {
					workList.Remove(item);
					beforePathDict.Remove(item.before);
					afterPathDict.Remove(item.after);
				}
			}
		}
	}

	static WorkItem? FindChainFirst(
		IReadOnlyList<WorkItem> workList,
		IReadOnlyDictionary<string, WorkItem> afterPathSet) {

		for (int i = 0; i < workList.Count; i++) {
			var item1 = workList[i];
			var isChainFirst = !afterPathSet.ContainsKey(item1.before);
			if (isChainFirst) {
				return item1;
			}
		}
		return null;
	}

}
