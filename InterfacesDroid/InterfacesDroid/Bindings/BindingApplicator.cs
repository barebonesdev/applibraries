#region File and License Information
/*
<File>
	<License Type="BSD">
		Copyright © 2009 - 2016, Outcoder. All rights reserved.
	
		This file is part of Calcium (http://calciumsdk.net).

		Redistribution and use in source and binary forms, with or without
		modification, are permitted provided that the following conditions are met:
			* Redistributions of source code must retain the above copyright
			  notice, this list of conditions and the following disclaimer.
			* Redistributions in binary form must reproduce the above copyright
			  notice, this list of conditions and the following disclaimer in the
			  documentation and/or other materials provided with the distribution.
			* Neither the name of the <organization> nor the
			  names of its contributors may be used to endorse or promote products
			  derived from this software without specific prior written permission.

		THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
		ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
		WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
		DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
		DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
		(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
		LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
		ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
		(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
		SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
	</License>
	<Owner Name="Daniel Vaughan" Email="danielvaughan@outcoder.com" />
	<CreationDate>$CreationDate$</CreationDate>
</File>
*/
#endregion

using Android.Widget;
using InterfacesDroid.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows.Input;
using ToolsPortable;

namespace BareMvvm.Core.Bindings
{
    internal class BindingApplicator
    {
#if __ANDROID__
        const string viewEnabledPropertyName = nameof(Android.Views.View.Enabled);
#else
		/* The Enabled property name assumes that there is a property on the view 
		 * that can be used to set the enabled state of the view. */
		const string viewEnabledPropertyName = "Enabled";
#endif

        internal static ViewBinderRegistry ViewBinderRegistry { get; } = new ViewBinderRegistry();

        public void ApplyBinding(
            BindingExpression bindingExpression,
            object activity,
            string dataContextPropertyOnActivity,
            IValueConverter converter,
            List<Action> unbindActions)
        {
            PropertyInfo targetProperty = bindingExpression.View.GetType().GetProperty(bindingExpression.Target);

            if (targetProperty == null)
                throw new NullReferenceException("targetProperty on View could not be found. View: " + bindingExpression.View.GetType() + ". Target: " + bindingExpression.Target);

            if (!TryHandleLocalizationBinding(bindingExpression, targetProperty, converter))
            {
                string sourcePath = string.IsNullOrWhiteSpace(dataContextPropertyOnActivity)
                    ? bindingExpression.Source
                    : dataContextPropertyOnActivity + "." + bindingExpression.Source;

                string[] pathSplit = sourcePath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                var localRemoveActions = new List<Action>();

                Bind(bindingExpression, activity, pathSplit, converter, targetProperty, localRemoveActions, unbindActions, 0);
            }
        }

        private bool TryHandleLocalizationBinding(BindingExpression bindingExpression, PropertyInfo targetProperty, IValueConverter converter)
        {
            if (bindingExpression.Source.StartsWith("@"))
            {
                string locName = bindingExpression.Source.Substring(1);
                string locValue = PortableLocalizedResources.GetString(locName);

                SetTargetProperty(locValue, bindingExpression.View, targetProperty, converter, bindingExpression.ConverterParameter);

                return true;
            }

            return false;
        }

        private class TextViewStrikethroughWrapper
        {
            private TextView _tv;
            public TextViewStrikethroughWrapper(TextView tv)
            {
                _tv = tv;
            }

            public bool Strikethrough
            {
                get { return _tv.GetStrikethrough(); }
                set { _tv.SetStrikethrough(value); }
            }
        }

        public void ApplyBinding(
            BindingExpression bindingExpression,
            object dataContext,
            IValueConverter converter,
            List<Action> unbindActions)
        {
            PropertyInfo targetProperty;
            if (bindingExpression.Target == "Strikethrough" && bindingExpression.View is TextView tv)
            {
                targetProperty = typeof(TextViewStrikethroughWrapper).GetProperty(nameof(TextViewStrikethroughWrapper.Strikethrough));
            }
            else
            {
                targetProperty = bindingExpression.View.GetType().GetProperty(bindingExpression.Target);
            }

            if (targetProperty == null)
                throw new NullReferenceException("targetProperty on View could not be found. View: " + bindingExpression.View.GetType() + ". Target: " + bindingExpression.Target + ". This might be because the XML doesn't always match up one-to-one with the generated views, if you add an ID to the xml item you're binding, that'll ensure it can be found.");

            if (!TryHandleLocalizationBinding(bindingExpression, targetProperty, converter))
            {
                string sourcePath = bindingExpression.Source;

                string[] pathSplit = sourcePath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                var localRemoveActions = new List<Action>();

                Bind(bindingExpression, dataContext, pathSplit, converter, targetProperty, localRemoveActions, unbindActions, 0);
            }
        }

        void Bind(
            BindingExpression bindingExpression,
            object dataContext,
            string[] sourcePath,
            IValueConverter converter,
            PropertyInfo targetProperty,
            IList<Action> localRemoveActions,
            IList<Action> globalRemoveActions,
            int position)
        {
            object currentContext = dataContext;

            var pathSplitLength = sourcePath.Length;
            int lastIndex = pathSplitLength - 1;
            PropertyBinding[] propertyBinding = new PropertyBinding[1];

            if (pathSplitLength == 0)
            {
                // If the source is the data context itself

                // then set the target property
                SetTargetProperty(currentContext, bindingExpression.View,
                    targetProperty, converter, bindingExpression.ConverterParameter);

                // The following loop won't execute
            }

            for (int i = position; i < pathSplitLength; i++)
            {
                if (currentContext == null)
                {
                    break;
                }

                var inpc = currentContext as INotifyPropertyChanged;

                string sourceSegment = sourcePath[i];
                var sourceProperty = currentContext.GetType().GetProperty(sourceSegment);

                if (i == lastIndex) /* The value. */
                {
                    /* Add a property binding between the source (the viewmodel) 
					 * and the target (the view) so we can update the target property 
					 * when the source property changes (a OneWay binding). */
                    propertyBinding[0] = new PropertyBinding
                    {
                        SourceProperty = sourceProperty,
                        TargetProperty = targetProperty,
                        Converter = converter,
                        ConverterParameter = bindingExpression.ConverterParameter,
                        View = bindingExpression.View
                    };

                    {
                        /* When this value changes, the value must be pushed to the target. */

                        if (inpc != null && bindingExpression.Mode != BindingMode.OneTime)
                        {
                            WeakReference contextReference = new WeakReference(currentContext);

                            // First have to declare the property, so we can use it inside the delegate
                            PropertyChangedEventHandler handler = null;
                            handler = new WeakEventHandler<PropertyChangedEventArgs>(delegate (object sender, PropertyChangedEventArgs args)
                            {
                                if (args.PropertyName != sourceSegment)
                                {
                                    return;
                                }

                                PropertyBinding binding = propertyBinding[0];

                                if (binding != null)
                                {
                                    if (binding.PreventUpdateForTargetProperty)
                                    {
                                        return;
                                    }

                                    try
                                    {
                                        binding.PreventUpdateForSourceProperty = true;

                                        object context = contextReference.Target;

                                        if (context != null)
                                            SetTargetProperty(sourceProperty, context,
                                                binding.View, binding.TargetProperty,
                                                binding.Converter, binding.ConverterParameter);
                                    }
                                    catch (Exception ex) when (ex is System.ObjectDisposedException || ex is System.Reflection.TargetInvocationException)
                                    {
                                        // If disposed, we should unregister
                                        inpc.PropertyChanged -= handler;
                                    }
                                    finally
                                    {
                                        binding.PreventUpdateForSourceProperty = false;
                                    }
                                }
                            }).Handler;

                            inpc.PropertyChanged += handler;
                            WeakReference<INotifyPropertyChanged> inpcReference = new WeakReference<INotifyPropertyChanged>(inpc);

                            Action removeHandler = () =>
                            {
                                INotifyPropertyChanged referencedInpc;
                                inpcReference.TryGetTarget(out referencedInpc);
                                if (referencedInpc != null)
                                {
                                    referencedInpc.PropertyChanged -= handler;
                                }

                                propertyBinding[0] = null;
                            };

                            localRemoveActions.Add(removeHandler);
                            globalRemoveActions.Add(removeHandler);
                        }
                    }

                    /* Determine if the target is an event, 
					 * in which case use that to trigger an update. */

                    var bindingEvent = bindingExpression.View.GetType().GetEvent(bindingExpression.Target);

                    if (bindingEvent != null)
                    {
                        /* The target is an event of the view. */
                        if (sourceProperty != null)
                        {
                            /* The source must be an ICommand so we can call its Execute method. */
                            var command = sourceProperty.GetValue(currentContext) as ICommand;
                            if (command == null)
                            {
                                throw new InvalidOperationException(
                                    $"The source property {bindingExpression.Source}, "
                                    + $"bound to the event {bindingEvent.Name}, "
                                    + "needs to implement the interface ICommand.");
                            }

                            /* Subscribe to the specified event to execute 
							 * the command when the event is raised. */
                            var executeMethodInfo = typeof(ICommand).GetMethod(nameof(ICommand.Execute), new[] { typeof(object) });

                            Action action = () =>
                            {
                                executeMethodInfo.Invoke(command, new object[] { null });
                            };

                            Action removeAction = DelegateUtility.AddHandler(bindingExpression.View, bindingExpression.Target, action);
                            localRemoveActions.Add(removeAction);
                            globalRemoveActions.Add(removeAction);

                            /* Subscribe to the CanExecuteChanged event of the command 
							 * to disable or enable the view associated to the command. */
                            var view = bindingExpression.View;

                            var enabledProperty = view.GetType().GetProperty(viewEnabledPropertyName);
                            if (enabledProperty != null)
                            {
                                enabledProperty.SetValue(view, command.CanExecute(null));

                                Action canExecuteChangedAction = () => enabledProperty.SetValue(view, command.CanExecute(null));
                                removeAction = DelegateUtility.AddHandler(
                                    command, nameof(ICommand.CanExecuteChanged), canExecuteChangedAction);

                                localRemoveActions.Add(removeAction);
                                globalRemoveActions.Add(removeAction);
                            }
                        }
                        else /* sourceProperty == null */
                        {
                            /* If the Source property of the data context 
							 * is not a property, check if it's a method. */
                            var sourceMethod = currentContext.GetType().GetMethod(sourceSegment,
                                BindingFlags.Public | BindingFlags.NonPublic
                                | BindingFlags.Instance | BindingFlags.Static);

                            if (sourceMethod == null)
                            {
                                throw new InvalidOperationException(
                                    $"No property or event named {bindingExpression.Source} "
                                    + $"found to bind it to the event {bindingEvent.Name}.");
                            }

                            var parameterCount = sourceMethod.GetParameters().Length;
                            if (parameterCount > 1)
                            {
                                /* Only calls to methods without parameters are supported. */
                                throw new InvalidOperationException(
                                    $"Method {sourceMethod.Name} should not have zero or one parameter "
                                    + $"to be called when event {bindingEvent.Name} is raised.");
                            }

                            /* It's a method therefore subscribe to the specified event 
							 * to execute the method when event is raised. */
                            var context = currentContext;
                            Action removeAction = DelegateUtility.AddHandler(
                                bindingExpression.View,
                                bindingExpression.Target,
                                () => { sourceMethod.Invoke(context, parameterCount > 0 ? new[] { context } : null); });

                            localRemoveActions.Add(removeAction);
                            globalRemoveActions.Add(removeAction);
                        }
                    }
                    else /* bindingEvent == null */
                    {
                        if (sourceProperty == null)
                        {
                            throw new InvalidOperationException(
                                $"Source property {bindingExpression.Source} does not exist "
                                + $"on {currentContext?.GetType().Name ?? "null"}.");
                        }

                        /* Set initial binding value. */
                        SetTargetProperty(sourceProperty, currentContext, bindingExpression.View,
                            targetProperty, converter, bindingExpression.ConverterParameter);

                        if (bindingExpression.Mode == BindingMode.TwoWay)
                        {
                            /* TwoWay bindings require that the ViewModel property be updated 
							 * when an event is raised on the bound view. */
                            string changedEvent = bindingExpression.ViewValueChangedEvent;
                            if (!string.IsNullOrWhiteSpace(changedEvent))
                            {
                                var context = currentContext;

                                Action changeAction = () =>
                                {
                                    var pb = propertyBinding[0];
                                    if (pb == null)
                                    {
                                        return;
                                    }

                                    ViewValueChangedHandler.HandleViewValueChanged(pb, context);
                                };

                                var view = bindingExpression.View;
                                var removeHandler = DelegateUtility.AddHandler(view, changedEvent, changeAction);

                                localRemoveActions.Add(removeHandler);
                                globalRemoveActions.Add(removeHandler);
                            }
                            else
                            {
                                var binding = propertyBinding[0];
                                IViewBinder binder;
                                if (ViewBinderRegistry.TryGetViewBinder(
                                        binding.View.GetType(), binding.TargetProperty.Name, out binder))
                                {
                                    var unbindAction = binder.BindView(binding, currentContext);
                                    if (unbindAction != null)
                                    {
                                        localRemoveActions.Add(unbindAction);
                                        globalRemoveActions.Add(unbindAction);
                                    }
                                }
                                else
                                {
                                    if (Debugger.IsAttached)
                                    {
                                        Debugger.Break();
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    /* The source is a child of another object, 
					 * therefore we must subscribe to the parents PropertyChanged event 
					 * and re-bind when the child changes. */

                    if (inpc != null && bindingExpression.Mode != BindingMode.OneTime)
                    {
                        WeakReference contextReference = new WeakReference(currentContext);

                        var iCopy = i;

                        PropertyChangedEventHandler handler
                            = new WeakEventHandler<PropertyChangedEventArgs>(delegate (object sender, PropertyChangedEventArgs args)
                        {
                            if (args.PropertyName != sourceSegment)
                            {
                                return;
                            }

                            /* Remove existing child event subscribers. */
                            for (int j = position; j < localRemoveActions.Count; j++)
                            {
                                var removeAction = localRemoveActions[j];
                                try
                                {
                                    removeAction();
                                }
                                catch
                                {
                                    /* TODO: log error. */
                                }

                                localRemoveActions.Remove(removeAction);
                                globalRemoveActions.Remove(removeAction);
                            }

                            propertyBinding[0] = null;

                            /* Bind child bindings. */
                            object context = contextReference.Target;
                            if (context != null)
                            {
                                Bind(bindingExpression,
                                    context,
                                    sourcePath,
                                    converter,
                                    targetProperty,
                                    localRemoveActions, globalRemoveActions, iCopy);
                            }
                        }).Handler;

                        inpc.PropertyChanged += handler;

                        WeakReference<INotifyPropertyChanged> inpcReference = new WeakReference<INotifyPropertyChanged>(inpc);

                        Action removeHandler = () =>
                        {
                            INotifyPropertyChanged referencedInpc;
                            inpcReference.TryGetTarget(out referencedInpc);
                            if (referencedInpc != null)
                            {
                                referencedInpc.PropertyChanged -= handler;
                            }

                            propertyBinding[0] = null;
                        };

                        localRemoveActions.Add(removeHandler);
                        globalRemoveActions.Add(removeHandler);
                    }

                    currentContext = sourceProperty?.GetValue(currentContext);
                }
            }
        }

        static void SetTargetProperty(PropertyInfo sourceProperty, object dataContext,
            object view, PropertyInfo targetProperty, IValueConverter converter, string converterParameter)
        {
            try
            {
                if (sourceProperty == null)
                    throw new ArgumentNullException(nameof(sourceProperty));

                /* Get the value of the source (the viewmodel) 
                 * property by using the converter if provided. */
                var rawValue = sourceProperty.GetValue(dataContext);

                SetTargetProperty(rawValue, view, targetProperty, converter, converterParameter);
            }
            catch (ArgumentException ex)
            {
                throw new Exception("Setting property error. View: " + view.GetType() + ". Target: " + targetProperty.Name + ". CanWrite: " + targetProperty.CanWrite, ex);
            }
        }

        static void SetTargetProperty(object rawValue,
            object view, PropertyInfo targetProperty, IValueConverter converter, string converterParameter)
        {
            try
            {
                if (targetProperty == null)
                    throw new ArgumentNullException(nameof(targetProperty));

                // Use the converter
                var sourcePropertyValue = converter == null
                    ? rawValue
                    : converter.Convert(rawValue,
                        targetProperty.PropertyType,
                        converterParameter,
                        CultureInfo.CurrentCulture);

                /* Need some implicit type coercion here. 
                 * Perhaps pull that in from Calciums property binding system. */
                var property = targetProperty;
                if (property.PropertyType == typeof(string)
                    && !(sourcePropertyValue is string)
                    && sourcePropertyValue != null)
                {
                    sourcePropertyValue = sourcePropertyValue.ToString();
                }
                else if (property.PropertyType == typeof(Android.Views.ViewStates))
                {
                    if (!(sourcePropertyValue is Android.Views.ViewStates))
                    {
                        // Implicit visibility converter
                        bool? shouldBeVisible = null;
                        if (sourcePropertyValue is bool boolean)
                        {
                            shouldBeVisible = boolean;
                        }
                        else
                        {
                            // If not null, visible
                            shouldBeVisible = sourcePropertyValue != null;
                        }

                        if (shouldBeVisible != null)
                        {
                            sourcePropertyValue = shouldBeVisible.Value ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
                        }
                    }
                }

                if (targetProperty.DeclaringType == typeof(TextViewStrikethroughWrapper))
                {
                    var wrapper = new TextViewStrikethroughWrapper(view as TextView);
                    targetProperty.SetValue(wrapper, sourcePropertyValue);
                }
                else
                {
                    targetProperty.SetValue(view, sourcePropertyValue);
                }
            }
            catch (ArgumentException ex)
            {
                throw new Exception("Setting property error. View: " + view.GetType() + ". Target: " + targetProperty.Name + ". CanWrite: " + targetProperty.CanWrite, ex);
            }
        }
    }
}