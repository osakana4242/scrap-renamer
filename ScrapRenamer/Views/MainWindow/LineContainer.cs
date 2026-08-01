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
		readonly Dictionary<string, WorkItem> _beforeDirectoryDict = new();
		readonly Dictionary<string, WorkItem> _beforePathDict = new();
		readonly Dictionary<string, WorkItem> _afterPathDict = new();
		readonly LineContainer _owner;

		OverwriteWindowResult? _lastOverwriteWindowResult;

		IReadOnlyList<Line> Lines => _owner.Lines;


		public RenameContext(LineContainer owner) {
			_owner = owner;
		}

		public void Apply() {
			try {
				Debug.WriteLine("Apply");
				BuildWorkList();
				// Chain を片付ける
				ProcessChain();
				// 残りは Cycle
				ProcessCycle();
			} catch (System.OperationCanceledException ex) {
				Debug.Print($"canceled, ex: {ex}");
			}
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
				if (line.isDirectory) {
					_beforeDirectoryDict.Add(line.origPath, item);
				}
				_afterPathDict.Add(nextPath, item);
				_beforePathDict.Add(line.origPath, item);
				_workList.Add(item);
			}
			//
			for (int i = 0; i < _workList.Count; i++) {
				var work = _workList[i];
				foreach (var kv in _beforeDirectoryDict) {
					if (IsUnderDirectory(work.before, kv.Key)) {
						var parentLine = GetLine(kv.Value);
						var childLine = GetLine(work);
						childLine.error =
							string.Format(Localization.Strings.Strings.Error_ParentChildPathOperationNotSupported_Parent, parentLine.origPath);
						parentLine.error =
							string.Format(Localization.Strings.Strings.Error_ParentChildPathOperationNotSupported_Child, childLine.origPath);
					}
				}
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
			// 想定できるエラー
			// - before が存在しない
			// - before がロックされている
			// - after がすでに存在する

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
							switch (_lastOverwriteWindowResult) {
							case OverwriteWindowResult.SkipAll:
							case OverwriteWindowResult.OverwriteAll:
								// 前回の結果を流用する
								break;
							default:
								// ユーザーに対応方法を確認する
								var window = new OverwriteWindow();
								window.ShowDialog();
								_lastOverwriteWindowResult = window.Result;
								break;
							}

							switch (_lastOverwriteWindowResult) {
							case OverwriteWindowResult.Skip:
							case OverwriteWindowResult.SkipAll:
								if (line.isDirectory) {
									line.error = Localization.Strings.Strings.Error_DestinationDirectoryExists;
								} else {
									line.error = Localization.Strings.Strings.Error_DestinationFileExists;
								}
								return;
							case OverwriteWindowResult.Overwrite:
							case OverwriteWindowResult.OverwriteAll:
								overwrite = true;
								return;
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
			} catch (System.OperationCanceledException) {
				throw;
			} catch (System.Exception ex) {
				switch (ex) {
				case System.IO.FileNotFoundException:
					// 移動元のファイルが存在しません
					line.error = Localization.Strings.Strings.Error_SrcFileNotFound;
					break;
				case System.IO.DirectoryNotFoundException:
					if (!System.IO.Path.Exists(line.origPath)) {
						// 移動元のディレクトリが存在しません
						line.error = Localization.Strings.Strings.Error_SrcDirectoryNotFound;
					} else {
						// 移動先のディレクトリが存在しません
						line.error = Localization.Strings.Strings.Error_DestinationDirectoryNotFound;
					}
					break;
				case System.IO.IOException ex2:
					if (line.isDirectory) {
						// 移動元のディレクトリ以下が使用中の可能性があります
						line.error = Localization.Strings.Strings.Error_SrcDirectoryLocked;
					} else {
						// 移動元のファイルが使用中の可能性があります
						line.error = Localization.Strings.Strings.Error_SrcFileLocked;
					}
					break;
				case System.UnauthorizedAccessException:
					if (line.isDirectory) {
						// Error_DirectoryUnautorizedAccess: ディレクトリを移動する権限がありません
						line.error = Localization.Strings.Strings.Error_DirectoryUnautorizedAccess;
					} else {
						// Error_FileUnautorizedAccess: ファイルを移動する権限がありません
						line.error = Localization.Strings.Strings.Error_FileUnautorizedAccess;
					}
					break;
				default:
					line.error = $"{ex.Message}";
					break;
				}
				// preError = $"{line.origPath} の移動失敗に引きずられて失敗しました";
				preError = string.Format(Localization.Strings.Strings.Error_AnotherPathMoveErrorChained, line.origPath);
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

		// 指定ディレクトリ自身を除いた、その配下にあるパス
		static bool IsUnderDirectory(string path, string directory) {
			string prefix = directory
				.TrimEnd(Path.DirectorySeparatorChar)
				+ Path.DirectorySeparatorChar;

			return path
				.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
		}

		record class WorkItem(int index, string before, string after);
	}
}
