using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BareMvvm.Core.ViewModels;
using Foundation;
using UIKit;
using System.Collections.Specialized;
using ToolsPortable;

namespace InterfacesiOS.ViewModelPresenters
{
    public class PagedViewModelWithPopupsPresenter : PagedViewModelPresenter
    {
        private ListOfViewModelsPresenter _listPresenter;
        private bool _destroyed = false;

        public new PagedViewModelWithPopups ViewModel
        {
            get { return base.ViewModel as PagedViewModelWithPopups; }
            set { base.ViewModel = value; }
        }

        public PagedViewModelWithPopupsPresenter()
        {
            _listPresenter = new ListOfViewModelsPresenter();
        }

        private NotifyCollectionChangedEventHandler _popupsCollectionChangedHandler;
        protected override void OnViewModelChanged(PagedViewModel oldViewModel, PagedViewModel currentViewModel)
        {
            _listPresenter.ViewModels = ViewModel?.Popups;

            Deregister(oldViewModel);

            if (_popupsCollectionChangedHandler == null)
            {
                _popupsCollectionChangedHandler = new WeakEventHandler<NotifyCollectionChangedEventArgs>(Popups_CollectionChanged).Handler;
            }

            PagedViewModelWithPopups newModel = currentViewModel as PagedViewModelWithPopups;
            if (newModel != null)
            {
                newModel.Popups.CollectionChanged += _popupsCollectionChangedHandler;
            }

            UpdateVisibility();

            base.OnViewModelChanged(oldViewModel, currentViewModel);
        }

        private void Deregister(BaseViewModel oldViewModel)
        {
            PagedViewModelWithPopups old = oldViewModel as PagedViewModelWithPopups;

            if (old != null)
            {
                old.Popups.CollectionChanged -= _popupsCollectionChangedHandler;
            }
        }

        private bool _isShown;
        private void UpdateVisibility()
        {
            if (ViewModel == null || ViewModel.Popups.Count == 0 || _destroyed)
            {
                if (_isShown)
                {
                    _listPresenter.DismissViewController(true, null);
                    _isShown = false;
                }
            }
            else
            {
                if (!_isShown)
                {
                    ShowDetailViewController(_listPresenter, null);
                    _isShown = true;
                }
            }
        }

        private void Popups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewModel?.Popups != sender)
            {
                return;
            }

            UpdateVisibility();
        }

        internal override void Destroy()
        {
            // For handling the case where the parent view gets swapped out somewhere underneath us
            Deregister(ViewModel);
            _destroyed = true;
            UpdateVisibility();
            _listPresenter.ViewModels = null;

            base.Destroy();
        }
    }
}