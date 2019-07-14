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
using BareMvvm.Core.Windows;
using InterfacesDroid.ViewModelPresenters;
using InterfacesDroid.Activities;
using System.ComponentModel;

namespace InterfacesDroid.Windows
{
    public class NativeDroidAppWindow : INativeAppWindow
    {
        private GenericViewModelPresenter _presenter;

        public event EventHandler<CancelEventArgs> BackPressed;

        public BareActivity Activity { get; private set; }

        public NativeDroidAppWindow(BareActivity activity)
        {
            Activity = activity;
            _presenter = new GenericViewModelPresenter(activity);
            activity.SetContentView(_presenter);
            activity.BackPressed += Activity_BackPressed;
        }

        private void Activity_BackPressed(object sender, System.ComponentModel.CancelEventArgs e)
        {
            BackPressed?.Invoke(this, e);
        }

        public void Register(PortableAppWindow portableWindow)
        {
            portableWindow.PropertyChanged += PortableWindow_PropertyChanged;
            if (portableWindow.ViewModel != null)
                _presenter.ViewModel = portableWindow.ViewModel;
        }

        private void PortableWindow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ViewModel":
                    _presenter.ViewModel = (sender as PortableAppWindow).ViewModel;
                    break;
            }
        }
    }
}