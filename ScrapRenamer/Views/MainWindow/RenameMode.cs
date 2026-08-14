namespace ScrapRenamer.Views.MainWindow;

public enum RenameMode {
	FileName,
	FileNameWithoutExtension,
	FullPath,
}

public static class RenameModeExt {
	public static string GetDisplayName(this RenameMode self) {
		switch (self) {
		case RenameMode.FileName: return Localization.Strings.Strings.RenameModeItems_FileName;
		case RenameMode.FileNameWithoutExtension: return Localization.Strings.Strings.RenameModeItems_FileNameWithoutExtension;
		case RenameMode.FullPath: return Localization.Strings.Strings.RenameModeItems_FullPath;
		default: throw new System.NotSupportedException($"self: {self}");
		}
	}
}
