using ScrapRenamer.Common.MiniJSON;

namespace ScrapRenamer.Views.MainWindow;

public class EditorMessage {
	public string type = "";
	public string text = "";

	public static EditorMessage FromJson(string json) {
		var inst = new EditorMessage();
		var dict = Json.Deserialize(json) as Dictionary<string, object>;
		if (null == dict) return inst;
		dict.TryGetValue_Ext(nameof(inst.type), ref inst.type);
		dict.TryGetValue_Ext(nameof(inst.text), ref inst.text);
		return inst;
	}

	public string ToJson() {
		var data = new Dictionary<string, object?>() {
			{ nameof(type), type},
			{ nameof(text), text},
		};
		return Json.Serialize(
			data);
	}

}
