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
using System.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.DesignerSupport
{
	class FlagDescriptorPropertyInfo
		: DescriptorPropertyInfo, IHavePredefinedValues<string>
	{
		public FlagDescriptorPropertyInfo (PropertyDescriptor propertyDescriptor, object propertyProvider, ValueSources valueSources) : base (propertyDescriptor, propertyProvider, valueSources)
		{
			IsValueCombinable = true;
			foreach (object value in System.Enum.GetValues (propertyDescriptor.PropertyType)) {
				ulong uintVal = Convert.ToUInt64 (value);
				predefinedValues.Add (value.ToString (), uintVal.ToString ());
			}
		}

		internal override void SetValue<T> (object target, T value)
		{
			var selectedValues = (System.String []) (object)value;
			ulong result = default;
			foreach (var selectedValue in selectedValues) {
				result += Convert.ToUInt64 (selectedValue);
			}
			PropertyDescriptor.SetValue (PropertyProvider, Enum.ToObject (PropertyDescriptor.PropertyType, result));
		}

		internal override Task<T> GetValueAsync<T> (object target)
		{
			//we need set the index of the selected item
			try {
				var myValue = (Enum)target;
				var result = new List<string> ();
				foreach (Enum item in Enum.GetValues (target.GetType ())) 
				{
					if (myValue.HasFlag (item)) {
						ulong uintVal = Convert.ToUInt64 (item);
						result.Add (uintVal.ToString ());
					}
				}
				return Task.FromResult<T> ((T)(object)result);
			} catch (Exception ex) {
				LogGetValueAsyncError (ex);
			}
			return Task.FromResult <T> (default);
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