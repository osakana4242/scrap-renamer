using System.Diagnostics;

namespace ScrapRenamer;

class Line {
	public string origPath = "";
	public string editedLine = "";
	public readonly bool isFolder;
	string _error = "";

	public Line(string origPath) {
		this.origPath = origPath;
		isFolder = System.IO.Directory.Exists(origPath);
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
}
