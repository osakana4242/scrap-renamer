namespace ScrapRenamer;

public enum RenameMode {
	FileName,
	FileNameWithoutExtention,
	FullPath,
}

public static class RenameModeExt {
	public static string GetDisplayName(this RenameMode self) {
		switch (self) {
		case RenameMode.FileName: return Localization.Strings.Strings.RenameModeItems_FileName;
		case RenameMode.FileNameWithoutExtention: return Localization.Strings.Strings.RenameModeItems_FileNameWithoutExtention;
		case RenameMode.FullPath: return Localization.Strings.Strings.RenameModeItems_FullPath;
		default: throw new System.NotSupportedException($"self: {self}");
		}
	}
}
