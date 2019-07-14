using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using BareMvvm.Core.ViewModels;
using System.Collections.Specialized;
using ToolsPortable;

namespace InterfacesiOS.ViewModelPresenters
{
    public class ListOfViewModelsPresenter : UINavigationController
    {
        public ListOfViewModelsPresenter()
        {
            NavigationBarHidden = true;
        }

        private List<Tuple<BaseViewModel, UIViewController>> _liveViews = new List<Tuple<BaseViewModel, UIViewController>>();
        private NotifyCollectionChangedEventHandler _viewModelsCollectionChangedHandler;
        private IEnumerable<BaseViewModel> _viewModels;
        public IEnumerable<BaseViewModel> ViewModels
        {
            get { return _viewModels; }
            set
            {
                if (_viewModels == value)
                {
                    return;
                }

                if (_viewModels is INotifyCollectionChanged)
                {
                    (_viewModels as INotifyCollectionChanged).CollectionChanged -= _viewModelsCollectionChangedHandler;
                }

                _viewModels = value;

                if (_viewModelsCollectionChangedHandler == null)
                {
                    _viewModelsCollectionChangedHandler = new WeakEventHandler<NotifyCollectionChangedEventArgs>(ListOfViewModelsPresenter_CollectionChanged).Handler;
                }

                if (_viewModels is INotifyCollectionChanged)
                {
                    (_viewModels as INotifyCollectionChanged).CollectionChanged += _viewModelsCollectionChangedHandler;
                }

                Reset();
            }
        }

        private void Reset()
        {
            Remove(0, _liveViews.Count);

            List<UIViewController> newControllers = new List<UIViewController>();
            if (_viewModels != null)
            {
                foreach (var model in _viewModels)
                {
                    var view = ViewModelToViewConverter.Convert(model);
                    newControllers.Add(view);
                    _liveViews.Add(new Tuple<BaseViewModel, UIViewController>(model, view));
                }
            }

            base.ViewControllers = newControllers.ToArray();
        }

        private void ListOfViewModelsPresenter_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_viewModels != sender)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    // If simply adding one new one, animate with push
                    if (e.NewItems.Count == 1 && e.NewStartingIndex == _liveViews.Count)
                    {
                        UIViewController viewController = ViewModelToViewConverter.Convert(e.NewItems[0] as BaseViewModel);
                        base.ShowViewController(viewController, null);
                        _liveViews.Add(new Tuple<BaseViewModel, UIViewController>(e.NewItems[0] as BaseViewModel, viewController));
                    }
                    else
                    {
                        Add(e.NewStartingIndex, e.NewItems.OfType<BaseViewModel>().ToArray());
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:

                    // If removing the last (and we have multiple), simply go back
                    if (e.OldItems.Count == 1 && e.OldStartingIndex == _liveViews.Count - 1 && base.ViewControllers.Length > 1)
                    {
                        base.PopViewController(true);
                        _liveViews.RemoveAt(e.OldStartingIndex);
                    }
                    else
                    {
                        Remove(e.OldStartingIndex, e.OldItems.Count);
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    List<UIViewController> final = new List<UIViewController>(base.ViewControllers);
                    MoveRange(final, e.OldStartingIndex, e.NewStartingIndex, e.NewItems.Count);
                    base.ViewControllers = final.ToArray();
                    MoveRange(_liveViews, e.OldStartingIndex, e.NewStartingIndex, e.NewItems.Count);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    Remove(e.OldStartingIndex, e.OldItems.Count);
                    Add(e.NewStartingIndex, e.NewItems.OfType<BaseViewModel>().ToArray());
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Reset();
                    break;
            }
        }

        private static void MoveRange<T>(List<T> list, int oldIndex, int newIndex, int count)
        {
            // Grab items
            T[] items = list.Skip(oldIndex).Take(count).ToArray();
            list.RemoveRange(oldIndex, count);
            list.InsertRange(newIndex, items);
        }

        private void Add(int index, BaseViewModel[] items)
        {
            var viewControllers = base.ViewControllers.ToList();

            foreach (var item in items)
            {
                UIViewController viewController = ViewModelToViewConverter.Convert(item);
                viewControllers.Insert(index, viewController);
                _liveViews.Insert(index, new Tuple<BaseViewModel, UIViewController>(item, viewController));

                index++;
            }

            base.ViewControllers = viewControllers.ToArray();
        }

        private void Remove(int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var vc = _liveViews[index].Item2;
                vc.WillMoveToParentViewController(null);
                vc.RemoveFromParentViewController();
                if (_liveViews.Count == 1)
                {
                    // If last view, we have to call this otherwise it doesn't get called, probably because navigation controllers
                    // won't remove the last view
                    vc.ViewDidDisappear(false);
                }
                _liveViews.RemoveAt(index);
            }
        }
    }
}