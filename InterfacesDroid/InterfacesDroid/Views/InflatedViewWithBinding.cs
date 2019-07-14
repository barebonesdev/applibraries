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
using BareMvvm.Core.Bindings;
using System.Diagnostics;
using Android.Util;
using InterfacesDroid.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InterfacesDroid.Views
{
    public class InflatedViewWithBinding
        : RelativeLayout, INotifyPropertyChanged
    {
        private XmlBindingApplicator _bindingApplicator = new XmlBindingApplicator();

        private int _resource;

        public InflatedViewWithBinding(int resource, ViewGroup root) : base(root.Context)
        {
            // By default we disable auto fill. Classes inheriting from this (like LoginView) can choose to re-enable auto fill.
            AutofillHelper.DisableForAll(this);

            // Issue: Since we place our content in a frame layout, we can't control wrap_content or match_parent from the level below
            _resource = resource;
            var view = CreateView(LayoutInflater.FromContext(root.Context), resource, this);
            base.AddView(view);
        }

        public InflatedViewWithBinding(int resource, Context context, IAttributeSet attrs) : base(context, attrs)
        {
            // Issue: Since we place our content in a frame layout, we can't control wrap_content or match_parent from the level below
            _resource = resource;
            var view = CreateView(LayoutInflater.FromContext(context), resource, this);
            base.AddView(view);
        }

        public InflatedViewWithBinding(int resource, Context context) : base(context)
        {
            // By default we disable auto fill. Classes inheriting from this (like LoginView) can choose to re-enable auto fill.
            AutofillHelper.DisableForAll(this);

            // Issue: Since we place our content in a frame layout, we can't control wrap_content or match_parent from the level below
            _resource = resource;
            var view = CreateView(LayoutInflater.FromContext(context), resource, this);
            base.AddView(view);
        }

        private View _viewForBinding;
        protected virtual View CreateView(LayoutInflater inflater, int resourceId, ViewGroup root)
        {
            _viewForBinding = inflater.Inflate(resourceId, root, false); // Setting this to false but including the root ensures that the resource's root layout properties will be respected
            return _viewForBinding;
        }

        private WeakReference _dataContext;

        public event PropertyChangedEventHandler PropertyChanged;

        public object DataContext
        {
            get { return _dataContext.Target; }
            set
            {
                object oldValue = _dataContext?.Target;

                _bindingApplicator.RemoveBindings();

                if (value != null)
                {
                    _dataContext = new WeakReference(value);
                }
                else
                {
                    _dataContext = null;
                }

                if (value != null && _viewForBinding != null)
                {
                    try
                    {
                        _bindingApplicator.ApplyBindings(_viewForBinding, value, _resource);
                    }

                    catch (Exception ex)
                    {
                        if (Debugger.IsAttached)
                        {
                            Console.WriteLine(ex);
                            Debugger.Break();
                        }
                    }
                }

                OnDataContextChanged(oldValue, value);
            }
        }

        protected virtual void OnDataContextChanged(object oldValue, object newValue)
        {
            // Nothing
        }

        ~InflatedViewWithBinding()
        {
            _bindingApplicator.RemoveBindings();
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        protected void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}