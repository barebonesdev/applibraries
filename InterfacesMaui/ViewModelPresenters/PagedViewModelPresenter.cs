using BareMvvm.Core.ViewModels;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsPortable;

namespace InterfacesMaui.ViewModelPresenters
{
    public class PagedViewModelPresenter : Grid
    {
        public event EventHandler ContentChanged;
        private PropertyChangedEventHandler _viewModelPropertyChangedHandler;

        public PagedViewModelPresenter()
        {
            _viewModelPropertyChangedHandler = new WeakEventHandler<PropertyChangedEventArgs>(_viewModel_PropertyChanged).Handler;
        }

        private PagedViewModel _viewModel;
        public PagedViewModel ViewModel
        {
            get { return _viewModel; }
            set
            {
                if (_viewModel == value)
                {
                    return;
                }

                // De-register old
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= _viewModelPropertyChangedHandler;
                }

                _viewModel = value;

                // Register new
                if (value != null)
                {
                    _viewModel.PropertyChanged += _viewModelPropertyChangedHandler;
                }

                UpdateContent();
            }
        }

        private void _viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Content":
                    UpdateContent();
                    break;
            }
        }
        private void UpdateContent()
        {
            //KeyboardHelper.HideKeyboard(this);

            // Remove previous content
            base.Children.Clear();

            if (ViewModel?.Content != null)
            {
                // Create and set new content
                var view = ViewModelToViewConverter.Convert(ViewModel.Content);
                base.Children.Add(view);
            }

            else if (ViewModel != null)
            {
                //var splashView = ViewModelToViewConverter.GetSplash(this, ViewModel);
                //if (splashView != null)
                //{
                //    base.AddView(splashView);
                //}
            }

            ContentChanged?.Invoke(this, new EventArgs());
        }
    }
}
