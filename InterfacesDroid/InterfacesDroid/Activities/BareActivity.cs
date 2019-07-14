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
using System.ComponentModel;
using Android.Support.V7.App;

namespace InterfacesDroid.Activities
{
    public class BareActivity : AppCompatActivity
    {
        public event EventHandler<CancelEventArgs> BackPressed;

        public override void OnBackPressed()
        {
            CancelEventArgs args = new CancelEventArgs();

            BackPressed?.Invoke(this, args);

            if (!args.Cancel)
            {
                base.OnBackPressed();
            }
        }
    }
}