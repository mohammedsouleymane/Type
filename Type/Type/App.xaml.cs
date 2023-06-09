﻿// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using System.Drawing;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Type
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public partial class App : Application
	{
		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Invoked when the application is launched.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
		{
			SystemAccentColor();
			await QuoteService.GetQuotes();
			m_window = new MainWindow();
			m_window.Activate();
		}

		private Window m_window;

		public static Windows.UI.Color SystemAccentColor()
		{
			var accentColor = Current.Resources["SystemAccentColor"];
			var accentColorToColor = ColorTranslator.FromHtml(accentColor.ToString());
			return Windows.UI.Color.FromArgb(accentColorToColor.A, accentColorToColor.R, accentColorToColor.G,
				accentColorToColor.B);
		}
	}
}
