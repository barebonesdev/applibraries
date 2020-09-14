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

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace BareMvvm.Core.Bindings
{
    internal class ViewValueChangedHandler
    {
        internal static void HandleViewValueChanged(
            PropertyBinding propertyBinding,
            object dataContext)
        {
            try
            {
                propertyBinding.PreventUpdateForTargetProperty = true;

                var newValue = propertyBinding.TargetProperty.GetValue(propertyBinding.View);

                UpdateSourceProperty(propertyBinding.SourceProperty, dataContext, newValue,
                    propertyBinding.Converter, propertyBinding.ConverterParameter);
            }
            catch (Exception)
            {
                /* TODO: log exception */
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }
            finally
            {
                propertyBinding.PreventUpdateForTargetProperty = false;
            }
        }

        internal static void HandleViewValueChanged<TView, TArgs, TNewValue>(
            PropertyBinding propertyBinding,
            Func<TView, TArgs, TNewValue> newValueFunc,
            object dataContext,
            TArgs args)
#if __ANDROID__ || MONODROID
            where TView : Android.Views.View
#endif
        {
            try
            {
                propertyBinding.PreventUpdateForTargetProperty = true;
                var rawValue = newValueFunc((TView)propertyBinding.View, args);

                UpdateSourceProperty(propertyBinding.SourceProperty,
                    dataContext,
                    rawValue,
                    propertyBinding.Converter,
                    propertyBinding.ConverterParameter);
            }
            catch
#if DEBUG
            (Exception ex)
#endif
            {
                /* TODO: log exception */
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }
            finally
            {
                propertyBinding.PreventUpdateForTargetProperty = false;
            }
        }

        internal static void UpdateSourceProperty<T>(
            PropertyInfo sourceProperty,
            object dataContext,
            T value,
            IValueConverter valueConverter,
            string converterParameter)
        {
            object newValue;

            if (valueConverter != null)
            {
                newValue = valueConverter.ConvertBack(value,
                    sourceProperty.PropertyType,
                    converterParameter,
                    CultureInfo.CurrentCulture);
            }
            else
            {
                // Implicit converter logic for DoubleToString round trip
                if (sourceProperty.PropertyType == typeof(double)
                    && value is string)
                {
                    double valueAsDouble;
                    if (double.TryParse(value as string, out valueAsDouble))
                    {
                        newValue = valueAsDouble;
                    }
                    else
                    {
                        newValue = default(double);
                    }
                }

                // No implicit converter left
                else
                {
                    newValue = value;
                }
            }

            try
            {
                sourceProperty.SetValue(dataContext, newValue);
            }
            catch (Exception ex)
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
#endif

                throw ex;
            }
        }
    }
}