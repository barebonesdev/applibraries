using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InterfacesMaui.ViewModelPresenters
{
    public class ViewModelToViewConverter
    {
        private static Dictionary<Type, Type> ViewModelToViewMappings = new Dictionary<Type, Type>();

        public static void AddMapping(Type viewModelType, Type viewType)
        {
            ViewModelToViewMappings[viewModelType] = viewType;
        }
        public static View Convert(object value)
        {
            if (value == null)
                return null;

            Type viewType;

            View view = null;

            if (ViewModelToViewMappings.TryGetValue(value.GetType(), out viewType))
            {
                view = CreateView(viewType);
            }

            else if (value is PagedViewModelWithPopups)
            {
                view = new PagedViewModelWithPopupsPresenter(root.Context);
            }

            else if (value is PagedViewModel)
            {
                view = new PagedViewModelPresenter(root.Context);
            }

            else
            {
                throw new NotImplementedException("ViewModel type was unknown: " + value.GetType());
            }

            // Get the ViewModel property
            var viewModelProperty = view.GetType().GetProperties().FirstOrDefault(p => p.Name.Equals("ViewModel"));
            if (viewModelProperty == null)
            {
                throw new InvalidOperationException("View must have a ViewModel property");
            }

            // And set the property
            viewModelProperty.SetValue(view, value);

            // And return the view
            return view;
        }

        private static View CreateView(Type viewType)
        {
            try
            {
                return (View)Activator.CreateInstance(viewType);
            }
            catch (TargetInvocationException ex)
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
                throw new TargetInvocationException("View likely didn't have the correct constructor.", ex);
            }
        }
    }
}
