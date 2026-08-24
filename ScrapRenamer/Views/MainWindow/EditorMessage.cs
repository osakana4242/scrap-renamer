namespace ScrapRenamer.Views.MainWindow;

public class EditorMessage {
	public string? type;
	public string? text;

	public static EditorMessage FromJson(string json) {
		var inst = new EditorMessage();
		var dict = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
		if (null == dict) return inst;
		if (dict.TryGetValue(nameof(inst.type), out var type)) inst.type = (string)type;
		if (dict.TryGetValue(nameof(inst.text), out var text)) inst.text = (string)text;
		return inst;
	}

	public string ToJson() {
		var data = new Dictionary<string, object?>() {
			{ nameof(type), type},
			{ nameof(text), text},
		};
		return MiniJSON.Json.Serialize(
			data);
	}

}
