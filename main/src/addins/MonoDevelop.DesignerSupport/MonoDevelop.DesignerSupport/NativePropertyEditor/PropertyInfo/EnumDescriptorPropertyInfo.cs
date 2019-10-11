//
// EnumDescriptorPropertyInfo.cs
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

using System;
using Xamarin.PropertyEditing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.DesignerSupport
{
	class EnumDescriptorPropertyInfo
		: DescriptorPropertyInfo, IHavePredefinedValues<string>
	{
		Hashtable names = new Hashtable ();
		Array values;
		public EnumDescriptorPropertyInfo (TypeDescriptorContext typeDescriptorContext, ValueSources valueSources)
		: base (typeDescriptorContext, valueSources)
		{
			values = Enum.GetValues (PropertyDescriptor.PropertyType);

			var fields = PropertyDescriptor.PropertyType.GetFields ();

			names = new Hashtable ();
			foreach (System.Reflection.FieldInfo f in fields) {
				var att = (DescriptionAttribute)Attribute.GetCustomAttribute (f, typeof (DescriptionAttribute));
				if (att != null)
					names [f.Name] = att.Description;
				else
					names [f.Name] = f.Name;
			}

			for (int i = 0; i < values.Length; i++) {
				//name
				var value = values.GetValue (i);
				string str = PropertyDescriptor.Converter.ConvertToString (value);
				if (names.Contains (str))
					str = (string)names [str];
				predefinedValues.Add (str, i.ToString ());
			}
		}

		static int GetIndexOfValue (Array values, string value)
		{
			for (int i = 0; i < values.Length; i++) {
				if (values.GetValue (i).ToString ().Equals (value)) {
					return i;
				}
			}
			return -1;
		}

		internal override T GetValue<T> (object target)
		{
			//we need set the index of the selected item
			T result = default;
			try {
				result = base.GetValue<T> (target);
				string cob = result as string;
				var index = GetIndexOfValue (values, cob);
				if (index > -1) {
					result = (T)(object)index.ToString ();
				} else {
					throw new InvalidCastException ($"Error in GetValueAsync<T> parsing {index} as integer");
				}
			} catch (Exception ex) {
				LogInternalError ($"Error trying to get and convert a value T:{typeof (T).FullName}", ex);
			}
			return result;
		}

		internal override void SetValue<T> (object target, T value)
		{
			if (EqualityComparer<T>.Default.Equals (value, default)) {
				var defaultValue = PropertyDescriptor.GetValue (PropertyProvider);
				PropertyDescriptor.SetValue (PropertyProvider, defaultValue);
				return;
			}

			try {
				var intValue = (int)Convert.ChangeType (value, typeof (int));
				var objValue = values.GetValue (intValue);
				var tc = PropertyDescriptor.Converter;
				if (tc.CanConvertFrom (objValue.GetType ())) {
					var result = tc.ConvertFrom (objValue);
					PropertyDescriptor.SetValue (PropertyProvider, result);
				} else {
					PropertyDescriptor.SetValue (PropertyProvider, objValue);
				}
			} catch (Exception ex) {
				LogInternalError ($"Error trying to set and convert a value: {value} T:{typeof (T).FullName}... trying to use base converter...", ex);
				base.SetValue<T> (target, value);
			}
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