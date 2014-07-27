//
// JavaScriptFormattingPolicy.cs
//
// Author:
//       Harsimran Bath <harsimranbath@gmail.com>
//
// Copyright (c) 2014 Harsimran Bath
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
// THE SOFTWARE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.JavaScript.Formatting
{
	[PolicyType ("JavaScript formatting")]
	public class JavaScriptFormattingPolicy : IEquatable<JavaScriptFormattingPolicy>
	{
		List<JavaScriptFormattingSettings> formats = new List<JavaScriptFormattingSettings> ();
		JavaScriptFormattingSettings defaultFormat = new JavaScriptFormattingSettings ();

		public JavaScriptFormattingPolicy ()
		{
		}

		[ItemProperty]
		public List<JavaScriptFormattingSettings> Formats {
			get { return formats; }
		}

		[ItemProperty]
		public JavaScriptFormattingSettings DefaultFormat {
			get { return defaultFormat; }
		}

		public bool Equals (JavaScriptFormattingPolicy other)
		{
			if (!defaultFormat.Equals (other.defaultFormat))
				return false;

			if (formats.Count != other.formats.Count)
				return false;

			List<JavaScriptFormattingSettings> list = new List<JavaScriptFormattingSettings> (other.formats);
			foreach (JavaScriptFormattingSettings fs in formats) {
				bool found = false;
				for (int n = 0; n < list.Count; n++) {
					if (fs.Equals (list [n])) {
						list.RemoveAt (n);
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			return true;
		}

		public JavaScriptFormattingPolicy Clone ()
		{
			JavaScriptFormattingPolicy clone = new JavaScriptFormattingPolicy ();
			clone.defaultFormat = defaultFormat.Clone ();
			foreach (var f in formats)
				clone.formats.Add (f.Clone ());
			return clone;
		}
	}
}
