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

namespace MonoDevelop.DesignerSupport
{
	class EnumDescriptorPropertyInfo
		: DescriptorPropertyInfo, IHavePredefinedValues<string> //, IValidator<IReadOnlyList<string>>, ICoerce<IReadOnlyList<string>>
	{
		Hashtable names = new Hashtable ();
		Array values;
		public EnumDescriptorPropertyInfo (PropertyDescriptor propertyDescriptor, object propertyProvider, ValueSources valueSources)
		: base (propertyDescriptor, propertyProvider, valueSources)
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

		internal override void SetValue<T> (object target, T value)
		{
			try {
				var tc = PropertyDescriptor.Converter;
				if (tc.CanConvertFrom (typeof (T))) {

					if (int.TryParse ((string)(object)value, out var index)) {
						PropertyDescriptor.SetValue (PropertyProvider, values.GetValue (index));
					} else {
						Console.WriteLine ("Error setting de value and parsing {0} as integer in {1} (enum descriptor)", value, PropertyDescriptor.DisplayName);
					}
				}
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		internal override Task<T> GetValueAsync<T> (object target)
		{
			//we need set the index of the selected item
			T result = default;
			try {
				TypeConverter tc = PropertyDescriptor.Converter;
				object cob = PropertyDescriptor.GetValue (PropertyProvider);
				var index = Array.IndexOf (values, cob);
				if (index > -1) {
					result = (T)(object) index.ToString ();
				} else {
					Console.WriteLine ("Error getting and parsing the value {0} as integer in {1} (enum descriptor)", index, PropertyDescriptor.DisplayName);
				}
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
			return Task.FromResult (result);
		}

		public bool IsConstrainedToPredefined => false;

		public bool IsValueCombinable {
			get;
		}

		readonly protected Dictionary<string, string> predefinedValues = new Dictionary<string, string> ();
		public IReadOnlyDictionary<string, string> PredefinedValues => predefinedValues;
	}
}

#endif