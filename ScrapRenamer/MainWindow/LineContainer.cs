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
		// Apply changes to the lines
		for (int i = 0; i < Lines.Count; i++) {
			var line = Lines[i];
			if (line.origPath != line.editedLine) {
				Debug.WriteLine($"Renaming: {line.origPath} -> {line.editedLine}");
				try {
					var nextPath = line.editedLine;
					switch (_mode) {
					case Mode.Name:
						nextPath = Path.Combine(Path.GetDirectoryName(line.origPath) ?? "", line.editedLine);
						break;
					case Mode.FullPath:
						nextPath = line.editedLine;
						break;
					}
					System.IO.File.Move(line.origPath, nextPath);
					line.origPath = nextPath;
				} catch (Exception ex) {
					Debug.WriteLine($"Failed to rename: {ex.Message}");
				}
			}
		}

	}
}
