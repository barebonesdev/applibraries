using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using System.ComponentModel;
using ToolsPortable;
using InterfacesiOS.Views;
using CoreAnimation;
using System.Collections;
using CoreGraphics;

namespace InterfacesiOS.Binding
{
    public class BindingHost
    {
        private PropertyChangedEventHandler _bindingObjectPropertyChangedHandler;
        private object _bindingObject;
        /// <summary>
        /// The DataContext for binding
        /// </summary>
        public object BindingObject
        {
            get { return _bindingObject; }
            set
            {
                if (value == _bindingObject)
                {
                    return;
                }

                // Unregister old
                if (_bindingObject is INotifyPropertyChanged && _bindingObjectPropertyChangedHandler != null)
                {
                    (_bindingObject as INotifyPropertyChanged).PropertyChanged -= _bindingObjectPropertyChangedHandler;
                }

                _bindingObject = value;

                // Register new
                if (value is INotifyPropertyChanged)
                {
                    if (_bindingObjectPropertyChangedHandler == null)
                    {
                        _bindingObjectPropertyChangedHandler = new WeakEventHandler<PropertyChangedEventArgs>(BindingObject_PropertyChanged).Handler;
                    }
                    (value as INotifyPropertyChanged).PropertyChanged += _bindingObjectPropertyChangedHandler;
                }

                if (value != null)
                {
                    UpdateAllBindings();
                }
            }
        }

        private void BindingObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
#if DEBUG
            try
            {
#endif
                // Be sure to call ToArray(), since a binding change could cause a new binding to
                // be added while we're enumerating, which would throw an exception
                foreach (var b in _bindings.Where(i => i.Item1 == e.PropertyName).ToArray())
                {
                    b.Item2.Invoke();
                }
#if DEBUG
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Break();
                throw ex;
            }
#endif
        }

        private void UpdateAllBindings()
        {
            foreach (var b in _bindings.ToArray())
            {
                b.Item2.Invoke();
            }
        }

        private List<Tuple<string, Action>> _bindings = new List<Tuple<string, Action>>();
        public void SetTextFieldTextBinding(UITextField textField, string propertyName, Func<object, string> converter = null, Func<string, object> backConverter = null)
        {
            textField.AddTarget(new WeakEventHandler<EventArgs>(delegate
            {
                object value;
                
                if (backConverter != null)
                {
                    value = backConverter(textField.Text);
                }
                else
                {
                    value = textField.Text;
                }

                BindingObject.GetType().GetProperty(propertyName).SetValue(BindingObject, value);

            }).Handler, UIControlEvent.EditingChanged);

            SetBinding(propertyName, delegate
            {
                object sourceValue = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);
                string text;

                if (converter != null)
                {
                    text = converter(sourceValue);
                }
                else
                {
                    text = sourceValue as string;
                }

                textField.Text = text;
            });
        }

        public void SetTextFieldTextBinding<T>(UITextField textField, string propertyName, Func<T, string> converter, Func<string, T> backConverter)
        {
            SetTextFieldTextBinding(textField, propertyName, converter: (o) =>
            {
                if (o is T)
                {
                    return converter((T)o);
                }
                throw new NotImplementedException();
            }, backConverter: (text) =>
            {
                return (object)backConverter(text);
            });
        }

        public void SetTextViewTextBinding(UITextView textView, string propertyName)
        {
            textView.Changed += new WeakEventHandler(delegate
            {
                BindingObject.GetType().GetProperty(propertyName).SetValue(BindingObject, textView.Text);
            }).Handler;

            SetBinding(propertyName, delegate
            {
                textView.Text = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject) as string;
            });
        }

        public void SetSliderBinding(UISlider slider, string propertyName, bool twoWay = false)
        {
            if (twoWay)
            {
                slider.ValueChanged += delegate
                {
                    BindingObject.GetType().GetProperty(propertyName).SetValue(BindingObject, slider.Value);
                };
            }

            SetBinding(propertyName, delegate
            {
                float val = float.Parse(BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject).ToString());
                slider.Value = val;
            });
        }

        public void SetDateBinding(BareUIInlineDatePicker datePicker, string propertyName)
        {
            datePicker.DateChanged += new WeakEventHandler<DateTime?>((sender, date) =>
            {
                var prop = BindingObject.GetType().GetProperty(propertyName);

                if (prop.PropertyType == typeof(DateTime?))
                {
                    prop.SetValue(BindingObject, date);
                }
                else
                {
                    prop.SetValue(BindingObject, date.GetValueOrDefault());
                }
            }).Handler;

            SetBinding(propertyName, delegate
            {
                object value = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);
                datePicker.GetType().GetProperty(nameof(datePicker.Date)).SetValue(datePicker, value);
            });
        }

        public void SetTimeBinding(UIDatePicker datePicker, string propertyName)
        {
            datePicker.ValueChanged += delegate
            {
                var prop = BindingObject.GetType().GetProperty(propertyName);
                prop.SetValue(BindingObject, BareUIHelper.NSDateToDateTime(datePicker.Date).TimeOfDay);
            };

            SetBinding(propertyName, delegate
            {
                TimeSpan value = (TimeSpan)BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);
                datePicker.Date = BareUIHelper.DateTimeToNSDate(DateTime.Today.Add(value));
            });
        }

        public void SetTimeBinding(BareUIInlineTimePicker timePicker, string propertyName)
        {
            timePicker.TimeChanged += new WeakEventHandler<TimeSpan?>((sender, time) =>
            {
                var prop = BindingObject.GetType().GetProperty(propertyName);

                if (prop.PropertyType == typeof(TimeSpan?))
                {
                    prop.SetValue(BindingObject, time);
                }
                else
                {
                    prop.SetValue(BindingObject, time.GetValueOrDefault());
                }
            }).Handler;

            SetBinding(propertyName, delegate
            {
                object value = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);
                timePicker.GetType().GetProperty(nameof(timePicker.Time)).SetValue(timePicker, value);
            });
        }

        private List<EventHandler<EventArgs>> _toggledViaHeaderHandlers = new List<EventHandler<EventArgs>>();
        public void SetSwitchBinding(BareUISwitch switchView, string propertyName)
        {
            var handler = new EventHandler<EventArgs>(delegate
            {
                var prop = BindingObject.GetType().GetProperty(propertyName);

                prop.SetValue(BindingObject, switchView.Switch.On);
            });

            // In order to avoid disposing, we need to store the handler.
            // Using a weak event handler causes it to get disposed.
            // Using a strong event handler causes it to not get let go.
            _toggledViaHeaderHandlers.Add(handler);

            switchView.ToggledViaHeader += new WeakEventHandler(handler).Handler;

            SetSwitchBinding(switchView.Switch, propertyName);
        }

        public void SetSwitchBinding(UISwitch switchView, string propertyName)
        {
            switchView.ValueChanged += new WeakEventHandler(delegate
            {
                var prop = BindingObject.GetType().GetProperty(propertyName);

                prop.SetValue(BindingObject, switchView.On);
            }).Handler;

            SetBinding(propertyName, delegate
            {
                bool value = (bool)BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);
                switchView.On = value;
            });
        }

        public void SetSelectedColorBinding(BareUIInlineColorPickerView pickerView, string propertyName)
        {
            pickerView.SelectionChanged += new WeakEventHandler<CGColor>((sender, color) =>
            {
                var property = BindingObject.GetType().GetProperty(propertyName);
                if (property.PropertyType == typeof(byte[]))
                {
                    property.SetValue(BindingObject, BareUIHelper.ToColorBytes(color));
                }
                else if (property.PropertyType == typeof(CGColor))
                {
                    property.SetValue(BindingObject, color);
                }
            }).Handler;

            SetBinding(propertyName, delegate
            {
                var value = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);
                if (value is CGColor color)
                {
                    pickerView.SelectedColor = color;
                }
                else if (value is byte[] bytes)
                {
                    pickerView.SelectedColor = BareUIHelper.ToCGColor(bytes);
                }
            });
        }

        public void SetSelectedItemBinding(BareUIInlinePickerView pickerView, string propertyName)
        {
            pickerView.SelectionChanged += new WeakEventHandler<object>((sender, item) =>
            {
                BindingObject.GetType().GetProperty(propertyName).SetValue(BindingObject, item);
            }).Handler;

            SetBinding(propertyName, delegate
            {
                pickerView.SelectedItem = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);
            });
        }

        public void SetItemsSourceBinding(BareUIInlinePickerView pickerView, string propertyName)
        {
            SetBinding(propertyName, delegate
            {
                pickerView.ItemsSource = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject) as IEnumerable;
            });
        }

        public void SetVisibilityBinding(BareUIVisibilityContainer visibilityContainer, string propertyName, bool invert = false)
        {
            SetBinding(propertyName, delegate
            {
                var value = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);
                bool boolean;
                if (value is bool b)
                {
                    boolean = b;
                }
                else if (value is string s)
                {
                    boolean = !string.IsNullOrWhiteSpace(s);
                }
                else
                {
                    boolean = value != null;
                }

                if (invert)
                {
                    boolean = !boolean;
                }

                visibilityContainer.IsVisible = boolean;
            });
        }

        public void SetTableViewSourceBinding(UITableView tableView, string propertyName, Func<UITableViewSource> createTableSourceAction)
        {
            SetBinding(propertyName, delegate
            {
                if (BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject) == null)
                {
                    tableView.Source = null;
                }
                else
                {
                    tableView.Source = createTableSourceAction();
                }
            });
        }

        public void SetIsEnabledBinding(UIView view, string propertyName)
        {
            SetBinding<bool>(propertyName, (isEnabled) =>
            {
                if (isEnabled)
                {
                    view.UserInteractionEnabled = true;
                    view.Alpha = 1;
                }
                else
                {
                    view.UserInteractionEnabled = false;
                    view.Alpha = 0.5f;
                }
            });
        }

        public void SetLabelTextBinding(UILabel label, string propertyName, Func<object, string> converter = null)
        {
            SetBinding(propertyName, delegate
            {
                var value = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);

                string valueText;
                if (converter != null)
                {
                    valueText = converter.Invoke(value);
                }
                else if (value is string valueStr)
                {
                    valueText = valueStr;
                }
                else if (value is DayOfWeek)
                {
                    valueText = DateTools.ToLocalizedString((DayOfWeek)value);
                }
                else if (value == null)
                {
                    valueText = "";
                }
                else
                {
                    valueText = value.ToString();
                }

                label.Text = valueText;
            });
        }

        public void SetLabelTextBinding<T>(UILabel label, string propertyName, Func<T, string> converter = null)
        {
            SetLabelTextBinding(label, propertyName, (obj) =>
            {
                return converter(obj is T ? (T)obj : default(T));
            });
        }

        /// <summary>
        /// Only one-way binding
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="propertyName"></param>
        public void SetIsCheckedBinding(UITableViewCell cell, string propertyName)
        {
            SetBinding<bool>(propertyName, (isChecked) =>
            {
                cell.Accessory = isChecked ? UITableViewCellAccessory.Checkmark : UITableViewCellAccessory.None;
            });
        }

        public void SetColorBinding(CAShapeLayer layer, string propertyName)
        {
            SetBinding(propertyName, delegate
            {
                byte[] colorArray = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject) as byte[];

                if (colorArray != null)
                {
                    layer.FillColor = BareUIHelper.ToCGColor(colorArray);
                }
                else
                {
                    layer.FillColor = null;
                }
            });
        }

        public void SetBackgroundColorBinding(UIView view, string propertyName)
        {
            SetBinding(propertyName, delegate
            {
                UIColor colorValue = null;
                var value = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);

                if (value is byte[] colorArray)
                {
                    colorValue = BareUIHelper.ToColor(colorArray);
                }
                else if (value is CGColor cgColor)
                {
                    colorValue = new UIColor(cgColor);
                }
                else if (value is UIColor uiColor)
                {
                    colorValue = uiColor;
                }

                view.BackgroundColor = colorValue;
            });
        }

        public void SetBinding<T>(string propertyName, Action<T> action)
        {
            SetBinding(propertyName, delegate
            {
                object value = BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);
                if (value == null)
                {
                    action(default(T));
                }
                else
                {
                    action((T)value);
                }
            });
        }

        public void SetBinding(string propertyName, Action action)
        {
            SetBindings(new string[] { propertyName }, action);
        }

        public void SetBindings(string[] propertyNames, Action action)
        {
            foreach (var propName in propertyNames)
            {
                _bindings.Add(new Tuple<string, Action>(propName, action));
            }

            if (BindingObject != null)
            {
                action.Invoke();
            }
        }

        public void SetVisibilityBinding(UIView view, string propertyName, bool invert = false)
        {
            SetBinding(propertyName, delegate
            {
                bool isVisible = (bool)BindingObject.GetType().GetProperty(propertyName).GetValue(BindingObject);

                if (invert)
                {
                    isVisible = !isVisible;
                }

                view.Hidden = !isVisible;
            });
        }
    }
}