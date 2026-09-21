using System.Diagnostics;
using System.Globalization;

namespace ScrapRenamer.Common;

public static class CultureInfoUtil {
	// 対応言語
	public static CultureInfo[] Values { get; private set; } = {
		CultureInfo.GetCultureInfo("en"),
		CultureInfo.GetCultureInfo("ja"),
	};

	static CultureInfo GetClosest(CultureInfo c) {
		foreach (var item in Values) {
			if (item.TwoLetterISOLanguageName == c.TwoLetterISOLanguageName) {
				return item;
			}
		}
		return Values[0];
	}

	static readonly CultureInfo s_default;


	public static CultureInfo Default => s_default;


	static CultureInfoUtil() {
		var current = CultureInfo.CurrentUICulture;
		s_default = GetClosest(current);
		Debug.Print($"LanguageUtil Default a: {current}, b: {s_default}");
	}

	public static CultureInfo Parse(string s) {
		try {
			var c = CultureInfo.GetCultureInfo(s);
			var i = Array.IndexOf(Values, c);
			if (i == -1) return Default;
			return Values[i];
		} catch {
			return Default;
		}
	}
}
