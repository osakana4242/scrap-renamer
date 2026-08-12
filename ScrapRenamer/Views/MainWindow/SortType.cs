namespace ScrapRenamer;

public enum SortType {
	FileName,
	ExtensionFileName,
	FullPath,
	FullPathExtension,
	EditedFileName,
}

public static class SortTypeExt {
	public static string GetDisplayName(this SortType self) {
		switch (self) {
		case SortType.FileName: return Localization.Strings.Strings.SortTypeItems_FileName;
		case SortType.ExtensionFileName: return Localization.Strings.Strings.SortTypeItems_ExtensionFileName;
		case SortType.FullPath: return Localization.Strings.Strings.SortTypeItems_FullPath;
		case SortType.FullPathExtension: return Localization.Strings.Strings.SortTypeItems_FullPathExtension;
		default: throw new System.NotSupportedException($"self: {self}");
		}
	}
}
