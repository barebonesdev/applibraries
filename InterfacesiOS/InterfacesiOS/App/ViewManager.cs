using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using BareMvvm.Core.ViewModels;
using BareMvvm.Core.App;
using InterfacesiOS.ViewModelPresenters;

namespace InterfacesiOS.App
{
    public class ViewManager
    {
        public void AddMapping(Type viewModelType, Type viewControllerType)
        {
            ViewModelToViewConverter.AddMapping(viewModelType, viewControllerType);
        }

        private BaseViewModel _rootViewModel;
        public BaseViewModel RootViewModel
        {
            get { return _rootViewModel; }
            set
            {
                if (_rootViewModel == value)
                {
                    return;
                }

                _rootViewModel = value;

                NativeiOSApplication.Current.Window.RootViewController = value != null ? ViewModelToViewConverter.Convert(value) : null;
            }
        }
    }
}