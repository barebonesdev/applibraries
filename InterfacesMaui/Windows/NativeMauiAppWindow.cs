using BareMvvm.Core.Snackbar;
using BareMvvm.Core.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterfacesMaui.Windows
{
    public class NativeMauiAppWindow : INativeAppWindow
    {
        public BareSnackbarManager SnackbarManager => throw new NotImplementedException();

        public event EventHandler<CancelEventArgs> BackPressed;

        public void Register(PortableAppWindow portableWindow)
        {
            throw new NotImplementedException();
        }
    }
}
