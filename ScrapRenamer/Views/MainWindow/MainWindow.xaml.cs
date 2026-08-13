using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace ScrapRenamer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
	static bool s_isDebug =
#if DEBUG
		true;
#else
		false;
#endif

	LineContainer _lineContainer;
	System.Action<(string text, System.Exception? ex)>? _onTextGet;
	bool _editor_loaded;
	bool _editor_hasFocus;


	public MainWindow() {
		InitializeComponent();
		UpdateTheme();
		Loaded += MainWindow_Loaded;
		Settings.Instance.themeProp.OnChanged += OnThemeChanged;
		Settings.Instance.fontFamilyProp.OnChanged += OnFontFamilyChanged;
		Settings.Instance.fontSizeProp.OnChanged += OnFontSizeChanged;
		Settings.Instance.renameMode.OnChanged += OnRenameModeChanged;
		Settings.Instance.Load();

		_lineContainer = new(Settings.Instance.renameMode.Value);


		RenameModeComboBox.ItemsSource = new[] {
				RenameMode.FileName,
				RenameMode.FileNameWithoutExtension,
				RenameMode.FullPath,
			}.
			Select(item => new {
				Value = item,
				Text = item.GetDisplayName(),
			}).
			ToArray();
		RenameModeComboBox.SelectedValue = Settings.Instance.renameMode.Value;
		RenameModeComboBox.DisplayMemberPath = "Text";
		RenameModeComboBox.SelectedValuePath = "Value";
		RenameModeComboBox.SelectionChanged += OnRenameModeChanged;

		OnRenameModeChanged(Settings.Instance.renameMode.Value);

		PreviewLostKeyboardFocus += Window_PreviewLostKeyboardFocus;
		PreviewGotKeyboardFocus += Window_PreviewGotKeyboardFocus;
		PreviewKeyDown += Window_PreviewKeyDown;
	}

	void UpdateTheme() {
		var isDark =
			Settings.Instance.themeProp.Value == ThemeMode.Dark ||
			Settings.Instance.themeProp.Value == ThemeMode.System &&
			Platform.Windows.Theme.IsDarkMode();


		Platform.Windows.Dwm.SetWindowDarkMode(this, isDark);
		ThemeMode = Settings.Instance.themeProp.Value;

		if (null == EditorView?.CoreWebView2) return;
		string theme = isDark ? "vs-dark" : "vs";
		Editor_SetTheme(theme);
	}
	void CloseMenuAndFocusParent(MenuItem menuItem) {
		var parentMenuItem = WpfUtil.FindParent<MenuItem>(menuItem);

		menuItem.IsSubmenuOpen = false;

		if (parentMenuItem != null) {
			parentMenuItem.Focus();
			return;
		}

		EditorView.Focus();
	}

	async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
		var env = await CoreWebView2Environment.CreateAsync(
			userDataFolder: Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"ScrapRenamer",
				"WebView2"));

		await EditorView.EnsureCoreWebView2Async(env);
		EditorView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;


		if (s_isDebug) {
			EditorView.CoreWebView2.OpenDevToolsWindow();
		}

		var hoge = new {
			isDebug = s_isDebug,
			// フルパスを行末に表示するか
			showFullPathInAfter = false,
		};

		await EditorView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
			$$"""
			window.scrapRenamer = {{JsonSerializer.Serialize(hoge)}};
			""");
		UpdateTheme();

		var path = Path.Combine(
			AppContext.BaseDirectory,
			"Editor",
			"index.html");
		EditorView.DefaultBackgroundColor = Platform.Windows.Theme.IsDarkMode() ?
			System.Drawing.Color.Black :
			System.Drawing.Color.White;

		EditorView.Source = new Uri(path);
		EditorView.WebMessageReceived += EditorView_WebMessageReceived;
		// // 外部からのファイルドロップを禁止する
		EditorView.AllowExternalDrop = true;
		EditorView.Visibility = Visibility.Visible;
		DropOverlay.Visibility = Visibility.Visible;
		StatusBar.Visibility = Visibility.Hidden;
		UpdateVisibility(false);

		// エディターのロード待機
		while (!_editor_loaded) {
			await Dispatcher.Yield();
		}

		UpdateTheme();
		EditorView_SetLines();
		Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
		Debug.Print($"MainWindow_Loaded: {e}");
	}

	void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
		if (e.Key == Key.Escape) {
			if (Keyboard.FocusedElement is MenuItem menuItem) {
				CloseMenuAndFocusParent(menuItem);
				e.Handled = true;
				Debug.Print($"Window_PreviewKeyDown: {e}");
				return;
			}
			if (!_editor_hasFocus) {
				EditorView.Focus();
				e.Handled = true;
				return;
			}
		} else if (e.Key == Key.A && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) {
			AppMenu.Focus();
			AppMenu.IsSubmenuOpen = !AppMenu.IsSubmenuOpen;
			e.Handled = true;
			Debug.Print($"Window_PreviewKeyDown: {e}");
		} else if (e.Key == Key.M && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) {
			RenameModeMenu.Focus();
			RenameModeMenu.IsSubmenuOpen = !RenameModeMenu.IsSubmenuOpen;
			e.Handled = true;
			Debug.Print($"Window_PreviewKeyDown: {e}");
		}
	}

	void Window_PreviewLostKeyboardFocus(
		object sender,
		KeyboardFocusChangedEventArgs e) {
		if (e.NewFocus is DependencyObject newFocus) {
			Debug.Print($"Window_PreviewLostKeyboardFocus: {newFocus}");
		}
	}

	void Window_PreviewGotKeyboardFocus(
		object sender,
		KeyboardFocusChangedEventArgs e) {
		if (e.NewFocus is DependencyObject newFocus) {
			Debug.Print($"Window_PreviewGotKeyboardFocus: {newFocus}");
		}
	}

	void OnOpenMenuClick(
		object sender,
		RoutedEventArgs e) {
		var dialog = new Microsoft.Win32.OpenFileDialog {
			Title = "ファイルを選択",
			Multiselect = true,
			CheckFileExists = true
		};

		if (dialog.ShowDialog() != true) {
			return;
		}

		OpenFiles(dialog.FileNames);
	}

	void OnExitMenuClick(
		object sender,
		RoutedEventArgs e) {
		Application.Current.Shutdown();
	}

	void OnRenameModeMenuClick(
		object sender,
		RoutedEventArgs e) {
		if (sender is not MenuItem menuItem) return;
		if (menuItem.Tag is not RenameMode mode) return;
		RenameModeComboBox.SelectedValue = mode;
	}

	void OnOpenAboutClick(
		object sender,
		RoutedEventArgs e) {
		var window = new AboutWindow() {
			Owner = this
		};
		window.ShowDialog();
	}

	void OnOpenSettingsClick(
		object sender,
		RoutedEventArgs e) {
		var window = new SettingsWindow() {
			Owner = this
		};
		window.ShowDialog();
	}

	void OnUserPreferenceChanged(
		object? sender,
		Microsoft.Win32.UserPreferenceChangedEventArgs e) {
		Debug.WriteLine($"UserPreferenceChanged: {e.Category}");
		if (e.Category == Microsoft.Win32.UserPreferenceCategory.General) {
			UpdateTheme();
		}
	}

	void OnDragOver(object sender, DragEventArgs e) {
		Debug.Print($"OnDragOver: {e}");
		if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
			e.Effects = DragDropEffects.Copy;
		} else {
			e.Effects = DragDropEffects.None;
		}

		e.Handled = true;
	}

	void OnDrop(object sender, DragEventArgs e) {
		if (!e.Data.GetDataPresent(DataFormats.FileDrop))
			return;
		var files = (string[])e.Data.GetData(DataFormats.FileDrop);
		OpenFiles(files);
	}

	public void OpenFiles(string[] files) {
		var lines = new List<Line>();

		foreach (var file in files) {
			Debug.WriteLine(file);
			var line = new Line(file, Settings.Instance.renameMode.Value);
			if (!_lineContainer.Add(line))
				continue;
			lines.Add(line);
		}
		if (Settings.Instance.sortOnAdd.Value) {
			_lineContainer.Sort(Settings.Instance.sortType.Value);
		}

		if (null == EditorView.CoreWebView2) return;

		UpdateVisibility(false);

		if (lines.Count == 0) {
			Debug.WriteLine("No new lines to add.");
			return;
		}
		EditorView_SetLines();
	}

	async Task<string> GetTextAsync() {
		(string text, System.Exception? ex)? ret = null;
		_onTextGet = s => {
			ret = s;
		};

		EditorView_GetText();

		while (null == ret) {
			await Dispatcher.Yield();
		}

		if (null != ret.Value.ex) {
			throw ret.Value.ex;
		}

		return ret.Value.text;
	}

	async Task SyncTextFromEditorAsync() {
		var text = await GetTextAsync();
		string[] editedLines = null == text ?
			new string[] {} :
			text.Split('\n').ToArray();

		for (int i = 0; i < editedLines.Length; i++) {
			if (_lineContainer.Lines.Count <= i)
				break;
			var line = _lineContainer.Lines[i];
			line.editedLine = editedLines[i];
		}
	}

	async Task Apply() {
		await SyncTextFromEditorAsync();
		_lineContainer.Apply();
		var lines = _lineContainer.Lines.
			Select((elem, i) => new {index = i, elem}).
			Where((elem) => elem.elem.processed).ToList();
		var errorLines = lines.Where(i => "" != i.elem.Error).ToList();
		var successLines = lines.Where(i => "" == i.elem.Error).ToList();
		if (0 < errorLines.Count) {
			var doc = new FlowDocument();

			if (0 < errorLines.Count) {
				{
					var p = CreateParagraph();
					p.Foreground = Brushes.Red;
					var s = $"⛔{errorLines.Count} 件のリネームに失敗しました";
					p.Inlines.Add(new Run(s));
					doc.Blocks.Add(p);
				}

				foreach (var line in errorLines) {
					AddParagraph(doc, $"行 {line.index}: ⛔失敗 {System.IO.Path.GetFileName(line.elem.origPath)} → {line.elem.editedLine}");
					AddParagraph(doc, $"フルパス: {line.elem.origPath}");
					AddParagraph(doc, $"理由: {line.elem.Error}");
					AddParagraph(doc, $"");
				}
				AddParagraph(doc, "");
			}

			// {
			// 	var p = CreateParagraph();
			// 	var s = $"✅成功 {successLines.Count} 件";
			// 	p.Inlines.Add(new Run(s));
			// 	doc.Blocks.Add(p);
			// }

			// foreach (var line in successLines) {

			// 	AddParagraph(doc, $"行 {line.index}: ✅成功 {System.IO.Path.GetFileName(line.elem.origPath)} → {line.elem.editedLine}");
			// }

			AddParagraph(doc, "");

			var window = new ResultWindow(doc) {
				Owner = this
			};
			window.ShowDialog();
		}
		EditorView_SetLines();

	}

	static void AddParagraph(FlowDocument doc, string text) {
		var p = new Paragraph() {
			FontFamily = Settings.Instance.FontFamily,
			FontSize = Settings.Instance.fontSizeProp.Value,
			Margin = new Thickness(8),
		};
		p.Inlines.Add(new Run(text));
		doc.Blocks.Add(p);
	}

	static Paragraph CreateParagraph() {
		var p = new Paragraph() {
			FontFamily = Settings.Instance.FontFamily,
			FontSize = Settings.Instance.fontSizeProp.Value,
			Margin = new Thickness(8),
		};
		return p;
	}


	void OnFontFamilyChanged(string fontFamily) {
		if (null == EditorView.CoreWebView2) return;
		EditorView_UpdateOptions(new {
			fontFamily = fontFamily,
		});
	}

	void OnFontSizeChanged(int fontSize) {
		if (null == EditorView.CoreWebView2) return;
		EditorView_UpdateOptions(new {
			fontSize = fontSize,
		});
	}

	void OnThemeChanged(ThemeMode themeMode) {
		UpdateTheme();
	}

	void OnRenameModeChanged(RenameMode mode) {
		foreach (var item in RenameModeMenu.Items) {
			if (item is not MenuItem menuItem2) continue;
			if (menuItem2.Tag is not RenameMode mode2) continue;
			menuItem2.IsChecked = mode2 == mode;
		}
	}

	async void OnRenameModeChanged(object sender, SelectionChangedEventArgs e) {
		if (RenameModeComboBox.SelectedValue is not RenameMode mode) return;

		Settings.Instance.renameMode.Value = mode;

		if (null == EditorView.CoreWebView2) {
			_lineContainer.SetMode(mode);
		} else {
			await SyncTextFromEditorAsync();
			_lineContainer.SetMode(mode);
			EditorView_SetLines();
		}
	}

	async void OnExecuteClicked(object sender, RoutedEventArgs e) {
		await Apply();
		EditorView.Focus();
	}

	void OnResetClicked(object sender, RoutedEventArgs e) {
		_lineContainer.Reset();
		EditorView_SetLines();
		UpdateVisibility(false);
		EditorView.Focus();
	}

	void OnClearClicked(object sender, RoutedEventArgs e) {
		_lineContainer = new LineContainer(Settings.Instance.renameMode.Value);
		EditorView_Clear();
		UpdateVisibility(false);
		EditorView.Focus();
	}

	async void OnSortClicked(object sender, RoutedEventArgs e) {
		try {
			if (sender is not FrameworkElement menuItem) return;
			if (menuItem.Tag is not SortType sortType) {
				// 指定が無い場合はフルパスでソートする
				sortType = Settings.Instance.sortType.Value;
			}

			await SyncTextFromEditorAsync();

			_lineContainer.Sort(sortType);
			EditorView_SetLines();
		} finally {
			EditorView.Focus();
		}
	}

	// ------------------------------------------------------ MARK: EditorView

	async void EditorView_WebMessageReceived(
		object? sender,
		CoreWebView2WebMessageReceivedEventArgs e) {
		var json = e.WebMessageAsJson;

		// MessageBox.Show(json);

		var msg = JsonSerializer.Deserialize<EditorMessage>(json);
		if (msg == null) {
			Debug.WriteLine("Failed to deserialize message.");
			return;
		}

		switch (msg.Type) {
		case "debugLog":
			Debug.WriteLine("from js: " + msg.Text);
			break;
		case "apply":
			Debug.WriteLine("Apply.");
			await Apply();
			break;
		case "cursorSelectionLineChanged":
			if (int.TryParse(msg.Text, out var lineIndex)) {
				if ((uint)lineIndex < (uint)_lineContainer.Lines.Count) {
					var line = _lineContainer.Lines[lineIndex];
					(
						StatusBarTextBlockLeft.Text,
						StatusBarTextBlockRight.Text
					) = PathUtil.SplitPathAtNthLastSeparator(line.origPath, 3);
				} else {
					StatusBarTextBlockLeft.Text = $"";
					StatusBarTextBlockRight.Text = $"";
				}
			}
			break;
		case "editorLoaded":
			Debug.WriteLine("Editor loaded.");
			_editor_loaded = true;
			break;
		case "editorFocusText":
			_editor_hasFocus = true;
			break;
		case "editorBlurText":
			_editor_hasFocus = false;
			break;
		case "dragover":
			Debug.WriteLine("dragover");
			UpdateVisibility(true);
			break;
		case "text":
			var act = _onTextGet;
			_onTextGet = null;
			act?.Invoke((msg.Text ?? "", null));
			break;
		}
	}

	void EditorView_Clear() {
		var message = new { type = "clear", };
		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
	}

	void EditorView_GetText() {
		var message = new { type = "getText", };
		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
	}

	// エディターオプションを設定する
	void EditorView_UpdateOptions(object options) {
		var message = new {
			type = "updateOptions",
			options = options,
		};

		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
	}

	// エディター設定を設定する
	void EditorView_SetSettings() {
		var message = new {
			type = "setSettings",
			settings = new {
				isAfterContentFullpathVisible = false,
			},
		};

		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
	}

	// エディターに現テキストを設定する
	void EditorView_SetLines() {
		if (null == EditorView.CoreWebView2) return;

		var message = new {
			type = "setLines",
			lines = _lineContainer.Lines.Select(i => new {
				origPath = i.origPath,
				origLine = i.OrigLine,
				editedLine = i.editedLine,
				error = i.Error,
				isFolder = i.isDirectory,
			}).ToArray(),
		};

		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
	}

	void Editor_SetTheme(string theme) {
		var message = new {
			type = "setTheme",
			theme = theme
		};
		EditorView.CoreWebView2.PostWebMessageAsJson(
			JsonSerializer.Serialize(message));
	}

	// ------------------------------------------------------------ MARK: ----

	void UpdateVisibility(bool isDragging) {
		if (0 < _lineContainer.Lines.Count && !isDragging) {
			Debug.Print($"A, isDragging: {isDragging}, lineCount: {_lineContainer.Lines.Count}");
			EditorView.Visibility = Visibility.Visible;
			DropOverlay.Visibility = Visibility.Hidden;
			StatusBar.Visibility = Visibility.Visible;
		} else {
			Debug.Print($"B, isDragging: {isDragging}, lineCount: {_lineContainer.Lines.Count}");
			EditorView.Visibility = Visibility.Hidden;
			DropOverlay.Visibility = Visibility.Visible;
			StatusBar.Visibility = Visibility.Hidden;
		}
	}

	// -------------------------------------------------------- MARK: override

	protected override void OnSourceInitialized(EventArgs e) {
		base.OnSourceInitialized(e);
		UpdateTheme();
	}

	protected override void OnActivated(EventArgs e) {
		base.OnActivated(e);
		Debug.Print($"OnActivated: {e}");
	}
	protected override void OnDeactivated(EventArgs e) {
		base.OnDeactivated(e);
		Debug.Print($"OnDeactivated: {e}");
	}
	protected override void OnGotFocus(RoutedEventArgs e) {
		base.OnGotFocus(e);
		Debug.Print($"OnGotFocus: {e}");
	}

	protected override void OnLostFocus(RoutedEventArgs e) {
		base.OnLostFocus(e);
		Debug.Print($"OnLostFocus: {e}");
	}
}
