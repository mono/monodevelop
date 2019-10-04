//
// FlagDescriptorPropertyInfo.cs
//
// Author:
//       jmedrano <josmed@microsoft.com>
//
// Copyright (c) 2018 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#if MAC

using Xamarin.PropertyEditing;
using System.ComponentModel;
using System;
using System.Collections.Generic;

namespace MonoDevelop.DesignerSupport
{
	class FlagDescriptorPropertyInfo
		: DescriptorPropertyInfo, IHavePredefinedValues<string>
	{
		Array elements;
		public FlagDescriptorPropertyInfo (TypeDescriptorContext typeDescriptorContext, ValueSources valueSources) : base (typeDescriptorContext, valueSources)
		{
			IsValueCombinable = true;

			elements = Enum.GetValues (typeDescriptorContext.PropertyDescriptor.PropertyType);

			foreach (object value in elements) {
				ulong uintVal = Convert.ToUInt64 (value);
				predefinedValues.Add (value.ToString (), uintVal.ToString ());
			}
		}

		internal override void SetValue<T> (object target, T value)
		{
			if (EqualityComparer<T>.Default.Equals (value, default)) {
				var defaultValue = PropertyDescriptor.GetValue (PropertyProvider);
				PropertyDescriptor.SetValue (PropertyProvider, defaultValue);
				return;
			}

			try {
				var selectedValues = (System.String []) (object)value;
				ulong result = default;
				foreach (var selectedValue in selectedValues) {
					result += Convert.ToUInt64 (selectedValue);
				}
				PropertyDescriptor.SetValue (PropertyProvider, Enum.ToObject (PropertyDescriptor.PropertyType, result));
			} catch (Exception ex) {
				LogInternalError ($"Error trying to set value: {value}", ex);
			}
		}

		internal override T GetValue<T> (object target)
		{
			if (typeof (T) == typeof (IReadOnlyList<System.String>)) {
				//we need set the index of the selected item
				try {
					var currentObject = PropertyDescriptor.GetValue (PropertyProvider);
					var currentEnum = (Enum)currentObject;
					var result = new List<string> ();
					foreach (Enum item in elements) {
						if (currentEnum.HasFlag (item)) {
							ulong uintVal = Convert.ToUInt64 (item);
							result.Add (uintVal.ToString ());
						}
					}

					return (T)(object)result.AsReadOnly ();
				} catch (Exception ex) {
					LogInternalError ($"Error trying to get values from a Enum", ex);
				}
			}

			var baseValue = base.GetValue<T> (target);
			return baseValue;
		}

		public bool IsConstrainedToPredefined => true;

		public bool IsValueCombinable {
			get;
		}

		readonly protected Dictionary<string, string> predefinedValues = new Dictionary<string, string> ();
		public IReadOnlyDictionary<string, string> PredefinedValues => predefinedValues;
	}
}

#endif