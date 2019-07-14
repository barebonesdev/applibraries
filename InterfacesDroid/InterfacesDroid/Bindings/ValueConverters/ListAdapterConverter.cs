#if __ANDROID__
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
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace BareMvvm.Core.Bindings
{
	public class ListAdapterConverter : IValueConverter
	{
		static readonly Dictionary<string, FieldInfo> adapterDictionary 
			= new Dictionary<string, FieldInfo>();
		 
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
			{
				return null;
			}

			Type valueType = value.GetType();
			if (valueType.IsGenericType)
			{
				Type[] typeArguments = valueType.GetGenericArguments();
				if (typeArguments.Any())
				{
					if (typeArguments.Count() > 1)
					{
						throw new Exception("List contains to many type arguments. Unable to create " 
							+ nameof(BindableListAdapter<object>) + " in ListAdapterConverter.");
					}

					Type itemType = typeArguments[0];
					Type listType = typeof(BindableListAdapter<>);
					Type[] typeArgs = { itemType };
					Type constructed = listType.MakeGenericType(typeArgs);

					string layoutName = parameter?.ToString();

					if (layoutName != null)
					{
						var dotIndex = layoutName.LastIndexOf(".", StringComparison.Ordinal);
						string propertyName = layoutName.Substring(dotIndex + 1, layoutName.Length - (dotIndex + 1));
						string typeName = layoutName.Substring(0, dotIndex);

						FieldInfo fieldInfo;
						if (!adapterDictionary.TryGetValue(layoutName, out fieldInfo))
						{
							Type type = Type.GetType(typeName, false, true);
							if (type == null)
							{
								throw new Exception("Unable to locate layout type code for layout " + layoutName
								                    + " Type could not be resolved.");
							}

							fieldInfo = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Static);

							if (fieldInfo != null)
							{
								adapterDictionary[layoutName] = fieldInfo;
							}
						}
						
						if (fieldInfo == null)
						{
							throw new Exception("Unable to locate layout type code for layout " 
								+ layoutName + " FieldInfo is null.");
						}

						int resourceId = (int)fieldInfo.GetValue(null);

						var result = Activator.CreateInstance(constructed, value, resourceId);
						return result;
					}
				}
			}
			else
			{
				throw new Exception("Value is not a generic collection." + parameter);
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
#endif