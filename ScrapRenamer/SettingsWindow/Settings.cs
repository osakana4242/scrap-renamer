using System.Diagnostics;

namespace ScrapRenamer;

public class Settings {
	public static readonly Settings Instance = new Settings();
	public ReactiveProperty<string> fontFamily = new("MS ゴシック");
	public ReactiveProperty<int> fontSize = new(14);


	public class ReactiveProperty<T> where T : IEquatable<T> {
		T _value;

		public ReactiveProperty(T value) {
			_value = value;
		}

		public event System.Action<T>? OnChanged;

		public virtual T Value {
			get => _value;
			set {
				if (_value.Equals(value)) return;
				Debug.Print($"SetValue {value}");
				_value = value;
				OnChanged?.Invoke(_value);
			}
		}
	}
}

