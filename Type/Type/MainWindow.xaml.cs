// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Type.Util;
using WinRT;
using Color = Windows.UI.Color;
using Colors = Microsoft.UI.Colors;
using static System.Net.Mime.MediaTypeNames;
using static System.Collections.Specialized.BitVector32;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Type;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
	WindowsSystemDispatcherQueueHelper _mWsdqHelper;
	MicaController _mBackdropController;
	SystemBackdropConfiguration _mConfigurationSource;
	MicaController _mMicaController;
	private DateTime? _start;
	private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
	public MainWindow()
	{
		InitializeComponent();
		var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
		var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
		var appWindow = AppWindow.GetFromWindowId(windowId);
		appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1000, Height = 550 });
		ExtendsContentIntoTitleBar = true;
		SetTitleBar(AppTitleBar);
		if (!TrySetSystemBackdrop())
			TrySetAcrylicBackdrop();
		_timer.Tick += Timer_Tick;
		_timer.Start();
		TextBox2.AcceptsReturn = false;
		TextBlock.CharacterSpacing = 1;
		Start();
	}

	private void Timer_Tick(object sender, object e)
	{
		Timer.Foreground = new SolidColorBrush(_systemAccentColor);
		Timer.Text = _start != null ? Math.Floor((DateTime.Now - _start).Value.TotalSeconds).ToString(CultureInfo.InvariantCulture) : "";
	}

	private Color _systemAccentColor;
	private async void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
	{

		if (_start == null && TextBox2.Text != "")
		{
			_start = DateTime.Now;
			Timer.Text = "0";
			_systemAccentColor = App.SystemAccentColor();
		}
		TextBlock.Inlines.Clear();

		for (var i = 0; i < TextBox2.Text.Length; i++)
		{
			var color = TextBox2.Text[i] == _text[i] ? _systemAccentColor : Colors.Red;
			var text = color == Colors.Red && _text[i] == ' ' ? TextBox2.Text[i].ToString() : _text[i].ToString();
			UpdateTextBlock(text, color);
		}

		foreach (var character in _text[TextBox2.Text.Length..])
		{
			UpdateTextBlock(character.ToString(), Colors.LightGray);
		}

		if (TextBox2.Text.Length == _text.Length)
			await ShowDialog_Click();

	}

	private void UpdateTextBlock(string text, Color color)
	{
		TextBlock.Inlines.Add(new Run
		{
			Text = text,
			Foreground = new SolidColorBrush(color),
			CharacterSpacing = 1
		});

	}
	private bool _done;
	private void TextBox2_OnLostFocus(object sender, RoutedEventArgs e)
	{
		if (!_done)
			((TextBox)sender).Focus(FocusState.Keyboard);
	}
	private async Task ShowDialog_Click()
	{
		Timer.Text = "";
		var diff = DateTime.Now - _start;
		_done = true;
		_start = null;
		var uncorrectedErrors = Math.Floor(_text.Where((c, i) => c != TextBox2.Text[i]).Count() * (1 / diff.Value.TotalMinutes));
		var wpm = Math.Floor(_text.Length / 5 / diff.Value.TotalMinutes) - uncorrectedErrors;
		if (wpm < 0) wpm = 0;
		var dialog = new ContentDialog
		{
			XamlRoot = Content.XamlRoot,
			Content = $"wpm: {wpm}",
			CloseButtonText = "Done"
		};
		await dialog.ShowAsync();
		Start();
		_done = false;
	}

	private string _text;
	private string _author;
	private void Start()
	{
		Timer.Text = "";
		TextBox2.Text = "";
		var quote = QuoteService.GetQuote();

		_text = quote.quote.Trim();
		TextBox2.MaxLength = _text.Length;
		_author = quote.author;
		TextBlock.Text = _text;
		Author.Text = _author;
		_start = null;
		TextBox2.Focus(FocusState.Keyboard);
	}

	private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
	{
		Start();
	}

	private void TextBox2_Paste(object sender, TextControlPasteEventArgs e)
	{
		e.Handled = true;
		TextBox2.Focus(FocusState.Keyboard);
	}


	#region Set Mica/Acrylic backdrop

	private DesktopAcrylicController _mAcrylicController;

	private void TrySetAcrylicBackdrop()
	{
		if (!DesktopAcrylicController.IsSupported()) return;
		_mWsdqHelper = new WindowsSystemDispatcherQueueHelper();
		_mWsdqHelper.EnsureWindowsSystemDispatcherQueueController();

		// Hooking up the policy object
		_mConfigurationSource = new SystemBackdropConfiguration();
		this.Activated += Window_Activated;
		this.Closed += Window_Closed;
		((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

		// Initial configuration state.
		_mConfigurationSource.IsInputActive = true;
		SetConfigurationSourceTheme();

		_mAcrylicController = new DesktopAcrylicController { TintColor = Colors.Black };

		// Enable the system backdrop.
		// Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
		_mAcrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
		_mAcrylicController.SetSystemBackdropConfiguration(_mConfigurationSource);
	}
	private bool TrySetSystemBackdrop()
	{
		if (!MicaController.IsSupported()) return false;
		_mWsdqHelper = new WindowsSystemDispatcherQueueHelper();
		_mWsdqHelper.EnsureWindowsSystemDispatcherQueueController();

		// Create the policy object.
		_mConfigurationSource = new SystemBackdropConfiguration();
		Activated += Window_Activated;
		Closed += Window_Closed;
		((FrameworkElement)Content).ActualThemeChanged += Window_ThemeChanged;

		// Initial configuration state.
		_mConfigurationSource.IsInputActive = true;
		SetConfigurationSourceTheme();

		_mBackdropController = new MicaController { Kind = MicaKind.BaseAlt };

		// Enable the system backdrop.
		// Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
		_mBackdropController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
		_mBackdropController.SetSystemBackdropConfiguration(_mConfigurationSource);
		return true;
	}
	private void Window_Activated(object sender, WindowActivatedEventArgs args)
	{
		_mConfigurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
	}

	private void Window_Closed(object sender, WindowEventArgs args)
	{
		// Make sure any Mica/Acrylic controller is disposed so it doesn't try to
		// use this closed window.
		if (_mMicaController != null)
		{
			_mMicaController.Dispose();
			_mMicaController = null;
		}

		if (_mAcrylicController != null)
		{
			_mAcrylicController.Dispose();
			_mAcrylicController = null;
		}
		Activated -= Window_Activated;
		_mConfigurationSource = null;
	}

	private void Window_ThemeChanged(FrameworkElement sender, object args)
	{
		if (_mConfigurationSource != null)
		{
			SetConfigurationSourceTheme();
		}
	}

	private void SetConfigurationSourceTheme()
	{
		_mConfigurationSource.Theme = ((FrameworkElement)Content).ActualTheme switch
		{
			ElementTheme.Dark => SystemBackdropTheme.Dark,
			ElementTheme.Light => SystemBackdropTheme.Light,
			ElementTheme.Default => SystemBackdropTheme.Default,
			_ => _mConfigurationSource.Theme
		};
	}
	#endregion
}
