//
// ObjectProperties.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;

namespace MonoDevelop.Components.AutoTest
{
	public class ObjectProperties : MarshalByRefObject
	{
		readonly Dictionary<string,AppResult> propertyMap = new Dictionary<string,AppResult> ();

		readonly Dictionary<string,PropertyMetadata> propertyMetaData = new Dictionary<string,PropertyMetadata> ();

		internal ObjectProperties () { }

		internal void Add (string propertyName, AppResult propertyValue, PropertyInfo propertyInfo)
		{
			if (!propertyMap.ContainsKey (propertyName))
				propertyMap.Add (propertyName, propertyValue);
			if (!propertyMetaData.ContainsKey (propertyName))
				propertyMetaData.Add (propertyName, new PropertyMetadata (propertyInfo));
		}

		public ReadOnlyCollection<string> GetPropertyNames ()
		{
			return propertyMap.Keys.ToList ().AsReadOnly ();
		}

		public AppResult this [string propertyName]
		{
			get {
				return propertyMap [propertyName];
			}
		}

		public PropertyMetadata GetMetaData (string propertyName)
		{
			return propertyMetaData [propertyName];
		}
	}
}

