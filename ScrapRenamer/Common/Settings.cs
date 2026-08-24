using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using ScrapRenamer.Views.MainWindow;

namespace ScrapRenamer.Common;

public class Settings {
	public static readonly Settings Instance = new Settings();

	public ObservableProperty<ThemeMode> themeProp = new(ThemeMode.System);
	public ObservableProperty<string> fontFamilyProp = new("Lucida Console");
	public ObservableProperty<int> fontSizeProp = new(14);
	public ObservableProperty<RenameMode> renameMode = new(RenameMode.FileName);
	public ObservableProperty<bool> sortOnAdd = new(true);
	public ObservableProperty<SortType> sortType = new(SortType.EditedFileName);


	bool _isInLoad = false;


	public FontFamily FontFamily => new FontFamily(Settings.Instance.fontFamilyProp.Value);


	Settings() {
		themeProp.OnChanged += v => OnChanged();
		fontFamilyProp.OnChanged += v => OnChanged();
		fontSizeProp.OnChanged += v => OnChanged();
		renameMode.OnChanged += v => OnChanged();
		sortOnAdd.OnChanged += v => OnChanged();
		sortType.OnChanged += v => OnChanged();
	}

	public void Load() {
		var path = GetPath();
		if (!File.Exists(path)) {
			return;
		}

		try {
			var json = File.ReadAllText(path);

			var data = Data.FromJson(json);

			themeProp.Value = data.theme == ThemeMode.Dark.Value ?
				ThemeMode.Dark :
				data.theme == ThemeMode.Light.Value ?
					ThemeMode.Light :
					ThemeMode.System;
			fontFamilyProp.Value = data.fontFamily;
			fontSizeProp.Value = data.fontSize;
			renameMode.Value = (RenameMode)data.renameMode;
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
				renameMode = (int)renameMode.Value,
			};
			var json = data.ToJson();

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
		public string theme = ThemeMode.System.Value;
		public string fontFamily = "";
		public int fontSize;
		public int renameMode;

		public static Data FromJson(string json) {
			var inst = new Data();
			if (MiniJSON.Json.Deserialize(json) is not Dictionary<string, object> dict) return inst;
			if (dict.TryGetValue(nameof(inst.theme), out var theme)) inst.theme = (string)theme;
			if (dict.TryGetValue(nameof(inst.fontFamily), out var fontFamily)) inst.fontFamily = (string)fontFamily;
			if (dict.TryGetValue(nameof(inst.fontSize), out var fontSize)) inst.fontSize = (int)fontSize;
			if (dict.TryGetValue(nameof(inst.renameMode), out var renameMode)) inst.renameMode = (int)renameMode;
			return inst;
		}

		public string ToJson() {
			var data = new Dictionary<string, object>() {
				{ nameof(theme), theme },
				{ nameof(fontFamily), fontFamily },
				{ nameof(fontSize), fontSize },
				{ nameof(renameMode), renameMode },
			};
			return MiniJSON.Json.Serialize(
				data);
		}
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

