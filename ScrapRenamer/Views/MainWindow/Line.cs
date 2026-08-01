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

			var nextFullPath = GetNextPath();

			_mode = value;

			switch (_mode) {
			case RenameMode.FileName:
				editedLine = Path.GetFileName(nextFullPath);
				break;
			case RenameMode.FileNameWithoutExtension:
				editedLine = Path.GetFileNameWithoutExtension(nextFullPath);
				break;
			case RenameMode.FullPath:
				editedLine = nextFullPath;
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
		Reset();
	}

	public void Reset() {
		switch (_mode) {
		case RenameMode.FileName:
			editedLine = Path.GetFileName(origPath);
			break;
		case RenameMode.FileNameWithoutExtension:
			editedLine = Path.GetFileNameWithoutExtension(origPath);
			break;
		case RenameMode.FullPath:
			editedLine = origPath;
			break;
		}
		error = "";
		processed = false;		
	}

	public string GetNextPath() {
		switch (_mode) {
		case RenameMode.FileName:
			return Path.Combine(
				Path.GetDirectoryName(origPath) ?? "",
				editedLine);
		case RenameMode.FileNameWithoutExtension:
			return Path.Combine(
				Path.GetDirectoryName(origPath) ?? "",
				editedLine) +
				Path.GetExtension(origPath);
		case RenameMode.FullPath:
			return editedLine;
		default:
			throw new System.NotSupportedException($"{_mode}");
		}
	}
}
