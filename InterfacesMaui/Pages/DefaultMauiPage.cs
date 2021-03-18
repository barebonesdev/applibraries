using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace InterfacesMaui.Pages
{
    public class DefaultMauiPage : ContentPage, IPage
	{
		public IView View
		{
			get => (IView)Content;
			set => Content = (View)value;
		}
	}
}
