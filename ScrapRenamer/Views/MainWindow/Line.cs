using System.Diagnostics;
using System.IO;

namespace ScrapRenamer;

class Line {
	public string origPath = "";
	public string editedLine = "";
	public readonly bool isDirectory;
	public bool processed = false;

	RenameMode _mode;
	string _error = "";

	public RenameMode Mode {
		get => _mode;
		set {
			if (_mode == value) return;
			_mode = value;
			
			var p = GetNextPath();
			switch (_mode) {
			case RenameMode.Name:
				editedLine = Path.GetFileName(origPath);
				break;
			case RenameMode.FullPath:
				editedLine = origPath;
				break;
			}
		}
	}

	public string error {
		get => _error;
		set {
			if (value != "") {
				Debug.WriteLine($"error: {value}");
			}
			_error = value;
		}
	}

	public Line(string origPath, RenameMode mode) {
		this.origPath = origPath;
		isDirectory = System.IO.Directory.Exists(origPath);
		_mode = mode;

		switch (_mode) {
		case RenameMode.Name:
			editedLine = Path.GetFileName(origPath);
			break;
		case RenameMode.FullPath:
			editedLine = origPath;
			break;
		}
	}

	public string GetNextPath() {
		switch (_mode) {
		case RenameMode.Name:
			return Path.Combine(Path.GetDirectoryName(origPath) ?? "", editedLine);
		case RenameMode.FullPath:
			return editedLine;
		default:
			throw new System.NotSupportedException($"{_mode}");
		}
	}

	public string GetNextPath(RenameMode mode) {
		var p = GetNextPath();
		switch (mode) {
		case RenameMode.Name:
			return Path.Combine(Path.GetDirectoryName(origPath) ?? "", editedLine);
		case RenameMode.FullPath:
			return editedLine;
		default:
			throw new System.NotSupportedException($"{mode}");
		}
	}

}
