using InterfacesMaui.Pages;
using InterfacesMaui.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Hosting;

namespace InterfacesMaui.App
{
    public class NativeMauiApplication : MauiApp
	{
		public override IAppHostBuilder CreateBuilder() =>
			base.CreateBuilder()
				.RegisterCompatibilityRenderers()
				.ConfigureServices((ctx, services) =>
				{
					services.AddTransient<DefaultMauiPage>();
					services.AddTransient<IWindow, DefaultMauiWindow>();
				})
				.ConfigureFonts((hostingContext, fonts) =>
				{
					fonts.AddFont("ionicons.ttf", "IonIcons");
				});

		public override IWindow CreateWindow(IActivationState state)
        {
            Microsoft.Maui.Controls.Compatibility.Forms.Init(state);
            return Services.GetService<IWindow>();
        }
    }
}
