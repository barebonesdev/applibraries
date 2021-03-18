﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;

namespace InterfacesMaui.Windows
{
	public class DefaultMauiWindow : IWindow
	{
		public IPage Page { get; set; }
		public IMauiContext MauiContext { get; set; }

		public DefaultMauiWindow()
		{
			Page = App.Current.Services.GetService<MainPage>();
		}
	}
}
