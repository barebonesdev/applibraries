using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using BareMvvm.Core.Windows;
using System.ComponentModel;
using InterfacesiOS.App;

namespace InterfacesiOS.Windows
{
    public class NativeiOSAppWindow : INativeAppWindow
    {
        public event EventHandler<CancelEventArgs> BackPressed;

        public UIWindow NativeWindow { get; private set; }

        public NativeiOSAppWindow(UIWindow nativeWindow)
        {
            NativeWindow = nativeWindow;
        }

        public void Register(PortableAppWindow portableWindow)
        {
            portableWindow.PropertyChanged += PortableWindow_PropertyChanged;
        }

        private void PortableWindow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PortableAppWindow.ViewModel):
                    NativeiOSApplication.Current.ViewManager.RootViewModel = (sender as PortableAppWindow).ViewModel;
                    break;
            }
        }
    }
}