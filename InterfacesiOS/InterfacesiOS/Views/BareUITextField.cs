using System;
using BareMvvm.Core;
using CoreGraphics;
using UIKit;

namespace InterfacesiOS.Views
{
    public class BareUITextField : UITextField
    {
        public BareUITextField()
        {
            // Listen to text change and update TextField
            // Note that need to listen to EditingDidEnd so that autosuggest corrections are consumed: https://github.com/MvvmCross/MvvmCross/pull/2682
            this.AddTarget(UpdateTextField, UIControlEvent.EditingChanged);
            this.AddTarget(UpdateTextField, UIControlEvent.EditingDidEnd);

            this.AddTarget(EndedEditing, UIControlEvent.EditingDidEnd); // This is fired when clicking a different view
            this.AddTarget(EndedEditing, UIControlEvent.EditingDidEndOnExit); // This is fired when clicking "Done" on keyboard
            this.AddTarget(StartedEditing, UIControlEvent.EditingDidBegin);
        }

        private void StartedEditing(object sender, EventArgs e)
        {
            if (TextField != null)
            {
                try
                {
                    TextField.HasFocus = true;
                }
                catch { }
            }
        }

        private void EndedEditing(object sender, EventArgs e)
        {
            if (TextField != null)
            {
                try
                {
                    TextField.HasFocus = false;
                }
                catch { }
            }
        }

        private void UpdateTextField(object sender, EventArgs e)
        {
            if (TextField != null)
            {
                try
                {
                    TextField.Text = Text;
                }
                catch { }
            }
        }

        private static BareUITextField _currentlyShownFieldWithError;

        private static Lazy<ErrorView> _errorView = new Lazy<ErrorView>(() =>
        {
            var errorView = new ErrorView();
            errorView.Hidden = true;
            UIApplication.SharedApplication.KeyWindow.AddSubview(errorView);
            return errorView;
        });

        private UIButton _errorIcon;

        private void CreateErrorIcon()
        {
            var errorButton = new UIButton(UIButtonType.Custom)
            {
                Frame = new CGRect(0, 0, Frame.Size.Height, Frame.Size.Height),
                TintColor = UIColor.Red
            };

            errorButton.SetImage(UIImage.FromBundle("baseline_error_black_18pt").ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate), UIControlState.Normal);
            errorButton.TouchUpInside += ErrorButton_TouchUpInside;

            _errorIcon = errorButton;
        }

        private void ErrorButton_TouchUpInside(object sender, EventArgs e)
        {
            ShowErrorPopup(this);
        }

        private bool _listeningToTextField;
        private TextField _textField;
        public TextField TextField
        {
            get => _textField;
            set
            {
                if (_textField != value)
                {
                    StopListeningToTextField();

                    _textField = value;

                    StartListeningToTextField();
                    UpdateFromTextField();
                }
            }
        }

        private void StartListeningToTextField()
        {
            if (TextField != null)
            {
                if (!_listeningToTextField)
                {
                    TextField.PropertyChanged += SourceTextField_PropertyChanged;
                    _listeningToTextField = true;
                }
            }
            else
            {
                _listeningToTextField = false;
            }
        }

        private void StopListeningToTextField()
        {
            if (TextField != null)
            {
                if (_listeningToTextField)
                {
                    TextField.PropertyChanged -= SourceTextField_PropertyChanged;
                    _listeningToTextField = false;
                }
            }
            else
            {
                _listeningToTextField = false;
            }
        }

        public override void WillMoveToWindow(UIWindow window)
        {
            if (window == null)
            {
                StopListeningToTextField();
            }
            else
            {
                StartListeningToTextField();
            }

            base.WillMoveToWindow(window);
        }

        private void SourceTextField_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateFromTextField();
        }

        private void UpdateFromTextField()
        {
            if (TextField != null)
            {
                Text = TextField.Text;
                Error = TextField.ValidationState?.ErrorMessage;
            }
        }

        private class ErrorView : UIView
        {
            private UILabel _error;

            public ErrorView()
            {
                TranslatesAutoresizingMaskIntoConstraints = false;

                // Create triangle
                var triangle = new TriangleTop()
                {
                    BackgroundColor = UIColor.Clear,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                AddSubview(triangle);

                // Create red line
                var line = new UIView()
                {
                    BackgroundColor = UIColor.Red,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                AddSubview(line);

                // Create message
                _error = new UILabel()
                {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                    TextColor = UIColor.White,
                    Lines = 0,
                    Font = UIFont.SystemFontOfSize(15),
                    BackgroundColor = UIColor.Black
                };
                _error.SetContentCompressionResistancePriority(250, UILayoutConstraintAxis.Horizontal);
                AddSubview(_error);

                // Set constraints for triangle
                triangle.HeightAnchor.ConstraintEqualTo(10).Active = true;
                triangle.WidthAnchor.ConstraintEqualTo(15).Active = true;
                triangle.TopAnchor.ConstraintEqualTo(this.TopAnchor, -10).Active = true;
                triangle.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -15).Active = true;

                // Set constraints for line
                line.HeightAnchor.ConstraintEqualTo(3).Active = true;
                line.TopAnchor.ConstraintEqualTo(triangle.BottomAnchor).Active = true;
                line.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor).Active = true;
                line.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor).Active = true;

                // Set constraints for error label
                _error.TopAnchor.ConstraintEqualTo(line.BottomAnchor).Active = true;
                _error.BottomAnchor.ConstraintEqualTo(this.BottomAnchor).Active = true;
                _error.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor).Active = true;
                _error.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor).Active = true;
            }

            public string Error
            {
                get => _error.Text;
                set => _error.Text = value;
            }
        }

        private class TriangleTop : UIView
        {
            public override void Draw(CGRect rect)
            {
                using (var context = UIGraphics.GetCurrentContext())
                {
                    context.BeginPath();
                    context.MoveTo(rect.GetMaxX() / 2f, rect.GetMinY());
                    context.AddLineToPoint(rect.GetMaxX(), rect.GetMaxY());
                    context.AddLineToPoint(rect.GetMinX() / 2f, rect.GetMaxY());
                    context.ClosePath();

                    context.SetFillColor(UIColor.Red.CGColor);
                    context.FillPath();
                }
            }
        }

        private string _error;
        public string Error
        {
            get => _error;
            set
            {
                // Using excellent code from https://stackoverflow.com/a/56742493/1454643
                if (_error != value)
                {
                    _error = value;

                    if (value != null)
                    {
                        if (_errorIcon == null)
                        {
                            CreateErrorIcon();
                        }

                        this.RightView = _errorIcon;
                        this.RightViewMode = UITextFieldViewMode.Always;
                        ShowErrorPopup(this);
                    }
                    else
                    {
                        if (_errorIcon != null)
                        {
                            this.RightView = null;
                            if (_currentlyShownFieldWithError == this)
                            {
                                _errorView.Value.Hidden = true;
                            }
                        }
                    }
                }
            }
        }

        private static void ShowErrorPopup(BareUITextField textField)
        {
            return;
            var errorView = _errorView.Value;

            errorView.RemoveAllConstraints();

            errorView.WidthAnchor.ConstraintLessThanOrEqualTo(textField.WidthAnchor).Active = true;
            errorView.TopAnchor.ConstraintEqualTo(textField.BottomAnchor).Active = true;
            errorView.TrailingAnchor.ConstraintEqualTo(textField.TrailingAnchor).Active = true;

            _currentlyShownFieldWithError = textField;
            errorView.Error = textField.Error;
            errorView.Hidden = false;
        }
    }
}