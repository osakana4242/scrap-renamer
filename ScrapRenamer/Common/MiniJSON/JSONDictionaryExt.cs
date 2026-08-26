using System.Collections.Generic;

namespace ScrapRenamer.Common.MiniJSON {
	public static class JsonDictionaryExt {
		public static bool TryGetValue_Ext(this Dictionary<string, object> self, string key, ref string outValue) {
			if (!self.TryGetValue(key, out var v)) return false;
			if (v is not string s) return false;
			outValue = s;
			return true;
		}

		public static bool TryGetValue_Ext(this Dictionary<string, object> self, string key, ref int outValue) {
			if (!self.TryGetValue(key, out var v)) return false;
			if (v is not long l) return false;
			outValue = (int)l;
			return true;
		}
	}
}
