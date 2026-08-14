using System.Text.Json.Serialization;

namespace ScrapRenamer.Views.MainWindow;

public class EditorMessage {
	[JsonPropertyName("type")]
	public string? Type { get; set; }
	[JsonPropertyName("text")]
	public string? Text { get; set; }
}
