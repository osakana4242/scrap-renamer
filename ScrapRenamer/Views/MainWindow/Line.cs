using System.Diagnostics;
using System.IO;

namespace ScrapRenamer;

class Line {
	public string origPath = "";
	public string editedLine = "";
	public readonly bool isDirectory;
	public bool processed = false;
	string _error = "";

	public Line(string origPath) {
		this.origPath = origPath;
		isDirectory = System.IO.Directory.Exists(origPath);
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

	public string GetNextPath(RenameMode mode) {
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
