using System.Diagnostics;

namespace ScrapRenamer;

class Line {
	public string origPath = "";
	public string editedLine = "";
	string _error = "";
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
