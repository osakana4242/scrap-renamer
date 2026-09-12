using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using ScrapRenamer.Common;
using ScrapRenamer.Lib.MiniJSON;

namespace ScrapRenamer.Views.MainWindow;

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
	Editor _editor;
	string[] _args = System.Array.Empty<string>();

	internal LineContainer LineContainer => _lineContainer;

	public MainWindow(string[] args) {
		InitializeComponent();
		UpdateTheme();
		_args = args;
		Loaded += MainWindow_Loaded;
		Settings.Instance.themeProp.OnChanged += OnThemeChanged;
		Settings.Instance.fontFamilyProp.OnChanged += OnFontFamilyChanged;
		Settings.Instance.fontSizeProp.OnChanged += OnFontSizeChanged;
		Settings.Instance.renameMode.OnChanged += OnRenameModeChanged;
		Settings.Instance.Load();
		_editor = new Editor(this);

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
		ThemeManager.Add(this);
	}

	void UpdateTheme() {
		if (null == EditorView?.CoreWebView2) return;
		var isDark = Settings.Instance.themeProp.Value.IsDarkMode();
		string theme = isDark ? "vs-dark" : "vs";
		_editor.SetTheme(theme);
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
		try {
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

			// var cultureInfo = new System.Globalization.CultureInfo("en");
			// Thread.CurrentThread.CurrentUICulture = cultureInfo;
			// Thread.CurrentThread.CurrentCulture = cultureInfo;

			var editorParams = new Dictionary<string, object>() {
				{ "isDebug", s_isDebug },
				// ja, en...
				{ "language", Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName },
				// フルパスを行末に表示するか
				{ "showFullPathInAfter", false },
			};

			await EditorView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
				$$"""
				window.scrapRenamer = {{Json.Serialize(editorParams)}};
				""");
			UpdateTheme();

			var path = Path.Combine(
				AppContext.BaseDirectory,
				"bin",
				"Editor",
				"index.html");
			EditorView.DefaultBackgroundColor = Settings.Instance.themeProp.Value.IsDarkMode() ?
				System.Drawing.Color.Black :
				System.Drawing.Color.White;

			EditorView.Source = new Uri(path);
			EditorView.WebMessageReceived += _editor.WebMessageReceived;
			EditorView.AllowExternalDrop = true;
			EditorView.Visibility = Visibility.Visible;
			DropOverlay.Visibility = Visibility.Visible;
			StatusBar.Visibility = Visibility.Hidden;
			UpdateVisibility(false);

			// エディターのロード待機
			while (!_editor.Loaded) {
				await Dispatcher.Yield();
			}

			UpdateTheme();
			_editor.SetLines();
			Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

			// 引数があればパスを追加する
			if (0 < _args.Length) {
				OpenFilesOrSerachDirectory(_args);
			}
			Debug.Print($"MainWindow_Loaded: {e}");
		} catch (Exception ex) {
			var w = new ResultWindow.ResultWindow(ex.ToString()) {
				Owner = this
			};
			w.ShowDialog();
		}
	}

	void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
		if (e.Key == Key.Escape) {
			var focusedElement = Keyboard.FocusedElement;
			if (null == focusedElement) {
				// Editor 内で置換ウィンドウを開いてるときに Esc を押したタイミング
				// _editor.EditorView.Focus();
				// e.Handled = true;
				Debug.Print($"A Window_PreviewKeyDown: {e}");
				return;
			}
			if (focusedElement is MenuItem menuItem) {
				CloseMenuAndFocusParent(menuItem);
				e.Handled = true;
				Debug.Print($"A Window_PreviewKeyDown: {e}, focused: {focusedElement}, type: {focusedElement.GetType().Name}, _editor.HasFocus: {_editor.HasFocus}");
				return;
			}
			Debug.Print($"C Window_PreviewKeyDown: {e}, focused: {focusedElement}, type: {focusedElement.GetType().Name}, _editor.HasFocus: {_editor.HasFocus}");
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

		OpenFilesOrSerachDirectory(dialog.FileNames);
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
		var window = new SettingsWindow.SettingsWindow() {
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
		// Debug.Print($"OnDragOver: {e}");
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
		Debug.Print($"OnDrop, sender: {sender}, e: {e}");
		var files = (string[])e.Data.GetData(DataFormats.FileDrop);
		OpenFilesOrSerachDirectory(files);
	}

	List<string> SearchDirectory(
		string directory,
		ProgressReport report,
		CancellationToken token) {

		List<string> entries = new List<string>();
		SearchDirectory(directory, entries, report, token);
		return entries;
	}

	void SearchDirectory(
		string directory,
		List<string> entries,
		ProgressReport report,
		CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		foreach (var entry in Directory.GetFileSystemEntries(directory)) {
			token.ThrowIfCancellationRequested();

			entries.Add(entry);
			report.format = Localization.Strings.Strings.FileOpenProgress_DirectorySearching;
			report.progressCount = entries.Count;

			if (Directory.Exists(entry)) {
				SearchDirectory(entry, entries, report, token);
			}
		}
		
	}

	public async void OpenFilesOrSerachDirectory(string[] files) {
		Activate();
		ProgressReport report = new ProgressReport();
		CancellationTokenSource cts = new CancellationTokenSource();
		var token = cts.Token;

		Task<List<Line>> task;
		if (files.Length == 1 && Directory.Exists(files[0])) {
			// ディレクトリひとつのときは中を掘る
			var rootDirectory = files[0];

			// サブディレクトリが含まれるときはそれ以下を掘るか確認する
			var firstSubDirectory = Directory.EnumerateDirectories(rootDirectory).FirstOrDefault();
			var result = DirectorySearchConfirmWindow.Result.CurrentDirectory;
			if (null != firstSubDirectory) {
				var message = string.Format(Localization.Strings.Strings.DirectorySearchConfirmWindow_Message, rootDirectory);
				var confirmWnd = new DirectorySearchConfirmWindow(message) {
					Owner = this,
				};
				confirmWnd.ShowDialog();
				result = confirmWnd.GetResult();
			}
			if (result == DirectorySearchConfirmWindow.Result.Cancel) {
				return;
			}
			switch (result) {
			case DirectorySearchConfirmWindow.Result.CurrentDirectory:
				task = Task.Run<List<Line>>(() => {
					List<string> files3 = Directory.GetFileSystemEntries(rootDirectory).ToList();
					return GetLines(files3, report, token);
				});
				break;
			case DirectorySearchConfirmWindow.Result.IncludeSubdirectories:
				task = Task.Run<List<Line>>(() => {
					List<string> files2 = SearchDirectory(rootDirectory, report, token);
					return GetLines(files2, report, token);
				});
				break;
			default:
				throw new NotSupportedException($"result: {result}");
			}
		} else {
			task = Task.Run(() => {
				return GetLines(files, report, token);
			});
		}

		try {
			// task の実行に 200ms 以上かかっていたら Show する
			var delayTask = Task.Delay(200, token);

			if (await Task.WhenAny(task, delayTask) == delayTask) {
				var progressWnd = new ProgressWindow(cts) {
					Owner = this
				};
				try {
					progressWnd.Title = report.text;
					progressWnd.Show();

					// 1000ms置きにステータスを更新
					while (!task.IsCompleted) {
						progressWnd.Title = report.text;
						await Task.WhenAny(task, Task.Delay(100, token));
					}
					progressWnd.Title = report.text;
					await Task.Delay(200, token);
				} finally {
					progressWnd.Close();
				}
			}
			var lines = await task;
			OpenFiles(lines);
		} catch (OperationCanceledException) {
			// キャンセルはスルー
		} catch (Exception ex) {
			// エラーダイアログ
			Debug.Print($"ex: {ex}");
			var resultWnd = new ResultWindow.ResultWindow(string.Format(Localization.Strings.Strings.Error_DirectorySearchFailed, ex.Message)) {
				Owner = this
			};
			resultWnd.ShowDialog();
		}
	}

	List<Line> GetLines(IReadOnlyList<string> files, ProgressReport report, CancellationToken cancellationToken) {
		List<Line> lines = new List<Line>();
		foreach (var file in files) {
			cancellationToken.ThrowIfCancellationRequested();
			//Debug.WriteLine(file);
			var line = new Line(file, Settings.Instance.renameMode.Value);
			if (!_lineContainer.Add(line))
				continue;
			lines.Add(line);
			report.format = Localization.Strings.Strings.FileOpenProgress_FileChecking;
			report.progressCount = lines.Count;
		}
		return lines;
	}

	void OpenFiles(List<Line> lines) {
		var sw = Stopwatch.StartNew();

		if (Settings.Instance.sortOnAdd.Value) {
			_lineContainer.Sort(Settings.Instance.sortType.Value);
		}

		Debug.Print($"StopWatch2, {sw.Elapsed.TotalSeconds:F1}");
		sw.Restart();

		if (null == EditorView.CoreWebView2) return;

		UpdateVisibility(false);

		Debug.Print($"StopWatch3, {sw.Elapsed.TotalSeconds:F1}");
		sw.Restart();

		if (lines.Count == 0) {
			Debug.WriteLine("No new lines to add.");
			return;
		}
		_editor.SetLines();

		Debug.Print($"StopWatch4, {sw.Elapsed.TotalSeconds:F1}");
		sw.Restart();

	}

	async Task SyncTextFromEditorAsync() {
		var text = await _editor.GetTextAsync();
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

	internal async Task Apply() {
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

			var window = new ResultWindow.ResultWindow(doc) {
				Owner = this
			};
			window.ShowDialog();
		}
		_editor.SetLines();

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
		_editor.UpdateOptions(new {
			fontFamily = fontFamily,
		});
	}

	void OnFontSizeChanged(int fontSize) {
		if (null == EditorView.CoreWebView2) return;
		_editor.UpdateOptions(new {
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
			_editor.SetLines();
		}
	}

	async void OnExecuteClicked(object sender, RoutedEventArgs e) {
		await Apply();
		EditorView.Focus();
	}

	void OnResetClicked(object sender, RoutedEventArgs e) {
		_lineContainer.Reset();
		_editor.SetLines();
		UpdateVisibility(false);
		EditorView.Focus();
	}

	void OnClearClicked(object sender, RoutedEventArgs e) {
		_lineContainer = new LineContainer(Settings.Instance.renameMode.Value);
		_editor.Clear();
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
			_editor.SetLines();
		} finally {
			EditorView.Focus();
		}
	}

	internal void UpdateVisibility(bool isDragging) {
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

	// ------------------------------------------------------------------------
	
	class ProgressReport {
		public string format = "";
		public int progressCount;
		public string text => string.Format(format, progressCount);
	}
}
