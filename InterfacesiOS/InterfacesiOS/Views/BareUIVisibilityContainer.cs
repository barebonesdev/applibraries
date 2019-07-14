using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;

namespace InterfacesiOS.Views
{
    public class BareUIVisibilityContainer : UIView
    {
        public BareUIVisibilityContainer()
        {
            TranslatesAutoresizingMaskIntoConstraints = false;
        }

        private bool _isVisible = false;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (value == _isVisible)
                {
                    return;
                }

                _isVisible = value;

                if (value)
                {
                    Add();
                }
                else
                {
                    Remove();
                }
            }
        }

        private UIView _child;
        public UIView Child
        {
            get { return _child; }
            set
            {
                if (_child != null)
                {
                    _child.RemoveFromSuperview();
                    this.RemoveConstraints(this.Constraints);
                }

                _child = value;

                if (IsVisible)
                {
                    Add();
                }
            }
        }

        private void Add()
        {
            if (Child != null)
            {
                Add(Child);
                if (this.Constraints.Length == 0)
                {
                    Child.TranslatesAutoresizingMaskIntoConstraints = false;
                    Child.StretchWidthAndHeight(this);
                }
            }
        }

        private void Remove()
        {
            if (Child != null)
            {
                Child.RemoveFromSuperview();
            }
        }
    }
}