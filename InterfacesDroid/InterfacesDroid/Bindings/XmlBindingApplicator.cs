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

#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using Android.App;
using Android.Content;
using Android.Views;
using Java.Lang;

using Enum = System.Enum;
using Exception = System.Exception;
using Android.Widget;
using ToolsPortable;
using System.Reflection;
using Google.Android.Material.TextField;
using BareMvvm.Core.Binding;
using Android.Content.Res;
using AndroidX.Core.View;
using System.Globalization;
using InterfacesDroid.Helpers;

namespace BareMvvm.Core.Bindings
{
    internal class ApplicationContextHolder
    {
        internal static Context Context { get; set; }
    }

    /// <summary>
    /// See http://www.codeproject.com/Articles/1070662/Data-Binding-in-Xamarin-Android for documentation.
    /// </summary>
	public class XmlBindingApplicator
    {
        private static readonly ViewBinderRegistry ViewBinderRegistry = new ViewBinderRegistry();
        public BindingHost BindingHost { get; private set; } = new BindingHost();

        private static readonly List<Assembly> _assembliesThatNeedProcessing = new List<Assembly>()
        {
            // Include this assembly as it has several converters
            Assembly.GetExecutingAssembly()
        };

#if DEBUG
        private static readonly List<Assembly> _processedAssemblies = new List<Assembly>();
#endif

        /// <summary>
        /// Registers the assembly calling this method as an assembly that'll be searched for type converters
        /// </summary>
        public static void RegisterThisAssembly()
        {
            RegisterAssembly(Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Registers the specified assembly as a type converter source
        /// </summary>
        /// <param name="assembly"></param>
        public static void RegisterAssembly(Assembly assembly)
        {
#if DEBUG
            // We only do this check in debug, since assuming in release we already would have seen the issue in debug
            if (_processedAssemblies.Contains(assembly))
            {
                Debugger.Break();
                throw new InvalidOperationException("Assembly cannot be registered twice");
            }
#endif

            if (!_assembliesThatNeedProcessing.Contains(assembly))
            {
                _assembliesThatNeedProcessing.Add(assembly);
            }
        }

        static readonly XName bindingXmlNamespace
            = XNamespace.Get("http://schemas.android.com/apk/res-auto") + "Binding";
        static readonly XName idXmlAttribute
            = XNamespace.Get("http://schemas.android.com/apk/res/android") + "id";

        private static Dictionary<string, Type> _valueConverterTypes = new Dictionary<string, Type>();

        private static Dictionary<string, Type> GetValueConverterTypes()
        {
            if (_assembliesThatNeedProcessing.Count > 0)
            {
                foreach (var assembly in _assembliesThatNeedProcessing)
                {
                    foreach (var type in TypeUtility.GetTypes<IValueConverter>(assembly))
                    {
#if DEBUG
                        if (_valueConverterTypes.ContainsKey(type.Name))
                        {
                            // Alert about overwriting type
                            Debugger.Break();
                        }
#endif

                        _valueConverterTypes[type.Name] = type;
                    }

#if DEBUG
                    _processedAssemblies.Add(assembly);
#endif
                }

                _assembliesThatNeedProcessing.Clear();
            }

            return _valueConverterTypes;
        }

        private static Type GetValueConverter(string valueConverterName)
        {
            if (GetValueConverterTypes().TryGetValue(valueConverterName, out Type valueConverterType))
            {
                return valueConverterType;
            }
            else
            {
                return null;
            }
        }

        static readonly Dictionary<int, List<XElement>> layoutCache = new Dictionary<int, List<XElement>>();

        private List<Action> _unbindActions = new List<Action>();

        public void Unregister()
        {
            foreach (var action in _unbindActions)
            {
                try
                {
                    action();
                }
                catch { }
            }

            BindingHost.Unregister();
        }

        public void ApplyBindings(View view, int layoutResourceId)
        {
            Context context = view.Context;

            if (ApplicationContextHolder.Context == null)
            {
                ApplicationContextHolder.Context = context.ApplicationContext;
            }

            // Always get views since we need to localize
            List<View> views = GetAllChildrenInView(view);
            List<XElement> elements = null;

            if (layoutResourceId > -1)
            {
                /* Load the XML elements of the view. */
                elements = GetLayoutAsXmlElements(context, layoutResourceId);
            }

            if (elements != null && elements.Any() && views != null && views.Any())
            {
                /* Get all the binding inside the XML file. */
                var bindingInfos = ExtractBindingsFromLayoutFile(elements, views);
                if (bindingInfos == null || !bindingInfos.Any())
                {
                    return;
                }

                foreach (var bindingInfo in bindingInfos)
                {
                    IValueConverter valueConverter = null;
                    string valueConverterName = bindingInfo.Converter;

                    if (!string.IsNullOrWhiteSpace(valueConverterName))
                    {
                        var converterType = GetValueConverter(valueConverterName);
                        if (converterType != null)
                        {
                            var constructor = converterType.GetConstructor(Type.EmptyTypes);
                            if (constructor != null)
                            {
                                valueConverter = constructor.Invoke(null) as IValueConverter;
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    $"Value converter {valueConverterName} needs "
                                    + "an empty constructor to be instanciated.");
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"There is no converter named {valueConverterName}.");
                        }
                    }

                    ApplyBinding(bindingInfo, valueConverter);
                }
            }
        }

        private void ApplyBinding(
            BindingExpression bindingExpression,
            IValueConverter converter)
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
            {
                string exMessage = "targetProperty on View could not be found. View: " + bindingExpression.View.GetType() + ". Target: " + bindingExpression.Target;

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw new KeyNotFoundException(exMessage);
            }

            // We try localizing, otherwise we bind
            if (!TryHandleLocalizationBinding(bindingExpression, targetProperty, converter))
            {
                BindingRegistration bindingRegistration = null;

                Action<object> bindingCallback = value =>
                {
                    try
                    {
                        SetTargetProperty(
                            rawValue: value,
                            view: bindingExpression.View,
                            targetProperty,
                            converter,
                            bindingExpression.ConverterParameter);
                    }
                    catch (Exception ex)
                    {
                        // View is disposed, should unregister
                        if (ex is TargetInvocationException && ex.InnerException is ObjectDisposedException)
                        {
                            // Note that don't need to call unbind action on the two way view binder since view is already disposed
                            bindingRegistration?.Unregister();
                        }
                        else
                        {
                            if (Debugger.IsAttached)
                            {
                                Debugger.Break();
                            }
                        }
                    }
                };

                BindingHost.SetBinding(bindingExpression.Source, bindingCallback);

                if (bindingExpression.Mode == BindingMode.TwoWay)
                {
                    if (ViewBinderRegistry.TryGetViewBinder(bindingExpression.View.GetType(), bindingExpression.Target, out IViewBinder binder))
                    {
                        var unbindAction = binder.BindView(bindingExpression, BindingHost, converter);
                        if (unbindAction != null)
                        {
                            _unbindActions.Add(unbindAction);
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

        private static bool TryHandleLocalizationBinding(BindingExpression bindingExpression, PropertyInfo targetProperty, IValueConverter converter)
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

        public static void SetTargetProperty(object rawValue,
            object view, PropertyInfo targetProperty, IValueConverter converter, string converterParameter)
        {
            try
            {
                if (targetProperty == null)
                    throw new ArgumentNullException(nameof(targetProperty));

                if (view == null)
                {
                    throw new ArgumentNullException(nameof(view));
                }

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
                else if (view is View androidView && targetProperty.Name == nameof(androidView.BackgroundTintList) && sourcePropertyValue is ColorStateList colorStateList)
                {
                    // Use ViewCompat since this property didn't exist till API 21
                    try
                    {
                        ViewCompat.SetBackgroundTintList(androidView, colorStateList);
                    }
                    catch (Java.Lang.RuntimeException)
                    {
                        // This theoretically shouldn't ever fail, yet it seems to fail sometimes due to a null reference exception
                        // which makes no sense. So I'll just catch it.
                    }
                }
                else if (targetProperty.Name == nameof(View.HasFocus))
                {
                    // Don't do anything, these are only one-way where the viewmodel updates but the view never updates
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

        private class TextViewStrikethroughWrapper
        {
            private TextView _tv;
            public TextViewStrikethroughWrapper(TextView tv)
            {
                _tv = tv;
            }

            public bool Strikethrough
            {
                get => _tv.GetStrikethrough();
                set => _tv.SetStrikethrough(value);
            }
        }

        /// <summary>
        /// Returns the current view (activity) as a list of XML element.
        /// Based on code by Thomas Lebrun http://bit.ly/1OQsD8L
        /// </summary>
        /// <param name="activity">The current activity we want 
        /// to get as a list of XML elements.</param>
        /// <param name="layoutResourceId">The id corresponding to the layout.</param>
        /// <returns>A list of XML elements which represent the XML layout of the view.</returns>
        static List<XElement> GetLayoutAsXmlElements(Context activity, int layoutResourceId)
        {
            List<XElement> result;

            if (layoutCache.TryGetValue(layoutResourceId, out result))
            {
                return result;
            }

            using (XmlReader viewAsXmlReader = activity.Resources.GetLayout(layoutResourceId))
            {
                using (var sb = new StringBuilder())
                {
                    while (viewAsXmlReader.Read())
                    {
                        sb.Append(viewAsXmlReader.ReadOuterXml());
                    }

                    var viewAsXDocument = XDocument.Parse(sb.ToString());
                    result = viewAsXDocument.Descendants().ToList();
                }
            }

            layoutCache[layoutResourceId] = result;

            return result;
        }

        static List<View> GetAllChildrenInView(View rootView)
        {
            List<View> result = new List<View>();
            GetAllChildrenInView(rootView, result);

            return result;
        }

        /// <summary>
        /// Recursive method which returns the list of children contains in a view.
        /// </summary>
        /// <param name="rootView">The start view from which the children will be retrieved.</param>
        /// <param name="children">A cumulative collection of child views.</param>
        static void GetAllChildrenInView(View rootView, List<View> children)
        {
            if (!(rootView is ViewGroup))
            {
                // Localize
                // EditText needs to come before TextView since EditText inherits from TextView
                bool found = LocalizeProperty<EditText>(rootView, (v) => v.Hint, (v, str) => { v.Hint = str; })
                    || LocalizeProperty<TextView>(rootView, (v) => v.Text, (v, str) => { v.Text = str; });

                children.Add(rootView);
                return;
            }

            // This is actually a ViewGroup so need to localize here but still continue with obtaining children so binding works
            LocalizeProperty<TextInputLayout>(rootView, (v) => v.Hint, (v, str) => { v.Hint = str; });

            children.Add(rootView);

            var viewGroup = (ViewGroup)rootView;

            for (var i = 0; i < viewGroup.ChildCount; i++)
            {
                View child = viewGroup.GetChildAt(i);

                GetAllChildrenInView(child, children);
            }
        }

        private static bool LocalizeProperty<T>(View view, Func<T, string> getOriginalText, Action<T, string> assignLocalizedText)
            where T : class
        {
            T castedView = view as T;
            if (castedView == null)
            {
                return false;
            }

            string originalText = getOriginalText(castedView);

            if (originalText != null && originalText.Length > 2 && originalText[0] == '{' && originalText[originalText.Length - 1] == '}')
            {
                assignLocalizedText(castedView, PortableLocalizedResources.GetString(originalText.Substring(1, originalText.Length - 2)));
            }

            // Returning true indicates that the view was of the specified type, so that the caller can stop checking different types
            return true;
        }

        private static readonly Regex sourceRegex = new Regex(@"Source=(@?\w+(.\w+)+)", RegexOptions.Compiled);
        private static readonly Regex targetRegex = new Regex(@"Target=(\w+(.\w+)+)", RegexOptions.Compiled);
        private static readonly Regex converterRegex = new Regex(@"Converter=(\w+)", RegexOptions.Compiled);
        private static readonly Regex converterParameterRegex = new Regex(@"ConverterParameter='([^']+)'|ConverterParameter=(\w+)", RegexOptions.Compiled);
        private static readonly Regex modeRegex = new Regex(@"Mode=(\w+)", RegexOptions.Compiled);
        private static readonly Regex changedEventRegex = new Regex(@"ChangedEvent=(\w+)", RegexOptions.Compiled);

        /// <summary>
        /// Extracts the binding information represented 
        /// as the Binding="" attribute in the XML file.
        /// Based on code by Thomas Lebrun http://bit.ly/1OQsD8L
        /// </summary>
        /// <param name="xmlElements">The list of XML elements from 
        /// which to extract the binding information.</param>
        /// <param name="viewElements">The elements of the view.</param>
        /// <returns>
        /// A list containing all the binding info objects.
        /// </returns>
        private static List<BindingExpression> ExtractBindingsFromLayoutFile(
            List<XElement> xmlElements, List<View> viewElements)
        {
            var result = new List<BindingExpression>();

            if (viewElements.Count == 0 || xmlElements.Count == 0)
            {
                return result;
            }

            var context = viewElements[0].Context;

            for (var i = 0; i < xmlElements.Count; i++)
            {
                var xmlElement = xmlElements.ElementAt(i);

                if (!xmlElement.Attributes(bindingXmlNamespace).Any())
                {
                    continue;
                }

                var bindingAttributes = xmlElement.Attributes(bindingXmlNamespace);

                foreach (var attribute in bindingAttributes)
                {
                    string bindingString = attribute.Value;

                    if (!bindingString.StartsWith("{") || !bindingString.EndsWith("}"))
                    {
                        throw new InvalidOperationException(
                            "The following XML binding operation is not well formatted, it should start "
                            + $"with '{{' and end with '}}:'{Environment.NewLine}{bindingString}");
                    }

                    string[] bindingStrings = bindingString.Split(';');

                    foreach (var bindingText in bindingStrings)
                    {
                        // Source isn't required, by default source is the data context itself
                        string sourceValue = "";
                        var sourceValueMatch = sourceRegex.Match(bindingText);
                        if (sourceValueMatch.Success)
                        {
                            sourceValue = sourceValueMatch.Groups[1].Value;
                        }

                        var targetValue = targetRegex.Match(bindingText).Groups[1].Value;
                        var converterValue = converterRegex.Match(bindingText).Groups[1].Value;

                        var converterParameterGroups = converterParameterRegex.Match(bindingText).Groups;
                        string converterParameterValue = converterParameterGroups[1].Value;
                        if (converterParameterValue.Length == 0)
                        {
                            converterParameterValue = converterParameterGroups[2].Value;
                        }

                        var bindingMode = BindingMode.OneWay;

                        var modeRegexMatch = modeRegex.Match(bindingText);

                        if (modeRegexMatch.Success)
                        {
                            if (!Enum.TryParse(modeRegexMatch.Groups[1].Value, true, out bindingMode))
                            {
                                throw new InvalidOperationException(
                                    "The Mode property of the following XML binding operation "
                                    + "is not well formatted, it should be \'OneWay\' "
                                    + $"or 'TwoWay':{Environment.NewLine}{bindingString}");
                            }
                        }

                        var viewValueChangedEvent = changedEventRegex.Match(bindingText).Groups[1].Value;

                        // Issue with the fancy TextInputLayout is that it changes the View items that it creates, so there's no longer a 1-1 mapping from
                        // the XML to the views. Hence, if we try to get the view element at the same index of the XML element, we might get something different.
                        View view;
                        int id;
                        if (TryGetId(xmlElement, out id))
                        {
                            view = viewElements.FirstOrDefault(v => v.Id == id);
                        }
                        else
                        {
                            view = viewElements.ElementAtOrDefault(i);
                        }

                        if (view == null)
                        {
                            throw new NullReferenceException("view could not be found.");
                        }

                        result.Add(new BindingExpression
                        {
                            View = view,
                            Source = sourceValue,
                            Target = targetValue,
                            Converter = converterValue,
                            ConverterParameter = converterParameterValue,
                            Mode = bindingMode,
                            ViewValueChangedEvent = viewValueChangedEvent,
                        });
                    }
                }
            }

            return result;
        }

        private static bool TryGetId(XElement xmlElement, out int id)
        {
            var val = xmlElement.Attribute(idXmlAttribute)?.Value;
            if (val == null || !val.StartsWith("@"))
            {
                id = -1;
                return false;
            }

            val = val.Substring(1);

            return int.TryParse(val, out id);
        }
    }
}
#endif