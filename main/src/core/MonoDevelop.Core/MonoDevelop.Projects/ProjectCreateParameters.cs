//
// ProjectCreateParameters.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Projects
{
	public class ProjectCreateParameters : IStringTagModel
	{
		Dictionary<string, string> parameters;

		public ProjectCreateParameters ()
		{
			Clear ();
		}

		public void Clear ()
		{
			parameters = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		}

		public void MergeTo (IDictionary<string, string> other)
		{
			foreach (KeyValuePair<string, string> parameter in parameters) {
				other [parameter.Key] = parameter.Value;
			}
		}

		public string this [string name] {
			get {
				string result;
				if (parameters.TryGetValue (name, out result)) {
					return result;
				}
				return string.Empty;
			}
			set {
				parameters [name] = value;
			}
		}

		public bool GetBoolean (string name, bool defaultValue = false)
		{
			string value = this [name];
			if (!string.IsNullOrEmpty (value)) {
				bool result;
				if (bool.TryParse (value, out result)) {
					return result;
				}
			}
			return defaultValue;
		}

		object IStringTagModel.GetValue (string name)
		{
			string result;
			if (parameters.TryGetValue (name, out result))
				return result;
			return null;
		}
	}
}

