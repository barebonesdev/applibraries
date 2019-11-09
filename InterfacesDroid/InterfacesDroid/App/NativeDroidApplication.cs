using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using BareMvvm.Core.App;
using System.Threading.Tasks;
using BareMvvm.Core.Windows;
using InterfacesDroid.Windows;
using InterfacesDroid.ViewModelPresenters;
using ToolsPortable;
using InterfacesDroid.Activities;
using InterfacesDroid.Extensions;
using System.Globalization;

namespace InterfacesDroid.App
{
    public abstract class NativeDroidApplication : Application
    {
        private static WeakReference<NativeDroidApplication> _current;
        public static NativeDroidApplication Current
        {
            get
            {
                if (_current == null)
                {
                    return null;
                }

                NativeDroidApplication answer;
                _current.TryGetTarget(out answer);
                return answer;
            }
        }

        protected NativeDroidApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            // Register the calling assembly (the app) as a ValueConverter source
            BareMvvm.Core.Bindings.XmlBindingApplicator.RegisterAssembly(System.Reflection.Assembly.GetCallingAssembly());

            // Workaround for bug with DateTime.ToString failing in Thai culture
            // Force Thai culture to English
            // https://bugzilla.xamarin.com/show_bug.cgi?id=31228
            if (Java.Util.Locale.Default.ToString() == "th_TH")
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            }
        }

        public override void OnCreate()
        {
            // This will be called whenever anything happens for the first time - including a receiver or service being started.

            _current = new WeakReference<NativeDroidApplication>(this);

            // Register the view model to view mappings
            foreach (var mapping in GetViewModelToViewMappings())
            {
                ViewModelToViewConverter.AddMapping(mapping.Key, mapping.Value);
            }

            // Register splash mappings
            foreach (var mapping in GetViewModelToSplashMappings())
            {
                ViewModelToViewConverter.AddSplashMapping(mapping.Key, mapping.Value);
            }

            // Register the obtain dispatcher function
            PortableDispatcher.ObtainDispatcherFunction = () => { return new AndroidDispatcher(); };
            
            // Register message dialog
            PortableMessageDialog.Extension = (messageDialog) => { AndroidMessageDialog.Show(messageDialog); return Task.FromResult(true); };

            PortableLocalizedResources.CultureExtension = GetCultureInfo;

            // Initialize the app
            PortableApp.InitializeAsync((PortableApp)Activator.CreateInstance(GetPortableAppType()));

            base.OnCreate();
        }

        public abstract Dictionary<Type, Type> GetViewModelToViewMappings();
        public abstract Dictionary<Type, Type> GetViewModelToSplashMappings();

        public abstract Type GetPortableAppType();

        private CultureInfo GetCultureInfo()
        {
            // For now, we're just going to leave it en-US for safety, since we're not sure it'll work well in other locales
            return new CultureInfo("en-US");

            // https://github.com/conceptdev/xamarin-forms-samples/blob/master/TodoL10nResx/PCL/Todo.Android/Locale_Android.cs
            //var androidLocale = Java.Util.Locale.Default;

            //string dotNetLocale = androidLocale.ToString().Replace('_', '-');

            //return new CultureInfo(dotNetLocale);
        }
    }
}