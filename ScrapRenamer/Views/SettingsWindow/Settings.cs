using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace ScrapRenamer;

public class Settings {
	public static readonly Settings Instance = new Settings();

	public ObservableProperty<ThemeMode> themeProp = new(ThemeMode.System);
	public ObservableProperty<string> fontFamilyProp = new("MS ゴシック");
	public ObservableProperty<int> fontSizeProp = new(14);


	bool _isInLoad = false;


	public FontFamily FontFamily => new FontFamily(Settings.Instance.fontFamilyProp.Value);


	Settings() {
		themeProp.OnChanged += v => OnChanged();
		fontFamilyProp.OnChanged += v => OnChanged();
		fontSizeProp.OnChanged += v => OnChanged();
	}

	public void Load() {
		var path = GetPath();
		if (!File.Exists(path)) {
			return;
		}

		try {
			var json = File.ReadAllText(path);

			var data = JsonSerializer.Deserialize<Data>(json);
			if (data == null) {
				return;
			}

			themeProp.Value = data.theme == ThemeMode.Dark.Value ?
				ThemeMode.Dark :
				data.theme == ThemeMode.Light.Value ?
					ThemeMode.Light :
					ThemeMode.System;
			fontFamilyProp.Value = data.fontFamily;
			fontSizeProp.Value = data.fontSize;
		} catch (Exception ex) {
			Debug.Print(ex.ToString());
		}
	}

	public void Save() {
		try {
			var path = GetPath();
			var directory = Path.GetDirectoryName(path);

			if (!string.IsNullOrEmpty(directory)) {
				Directory.CreateDirectory(directory);
			}

			var data = new Data() {
				theme = themeProp.Value.Value,
				fontFamily = fontFamilyProp.Value,
				fontSize = fontSizeProp.Value,
			};
			var json = JsonSerializer.Serialize(
				data,
				new JsonSerializerOptions {
					WriteIndented = true,
				});

			File.WriteAllText(path, json);
		} catch (Exception ex) {
			Debug.Print(ex.ToString());
		}
	}

	void OnChanged() {
		if (_isInLoad) return;
		Save();
	}

	static string GetPath() {
		return Path.Combine(
			AppContext.BaseDirectory,
			"ScrapRenamer.json");
	}

	// Json シリアライズ用
	class Data {
		public string theme { get; set; } = ThemeMode.System.Value;
		public string fontFamily { get; set; } = "";
		public int fontSize { get; set; }
	}

	public class ObservableProperty<T> {
		T _value;

		public ObservableProperty(T value) {
			_value = value;
		}

		public event System.Action<T>? OnChanged;

		public virtual T Value {
			get => _value;
			set {
				if (EqualityComparer<T>.Default.Equals(_value, value)) return;
				Debug.Print($"SetValue {value}");
				_value = value;
				OnChanged?.Invoke(_value);
			}
		}
	}
}

