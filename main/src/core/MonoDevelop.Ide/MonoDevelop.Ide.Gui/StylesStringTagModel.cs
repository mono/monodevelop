//
// StylesStringTagModel.cs
//
// Author:
//       Vsevolod Kukol <sevoku@xamarin.com>
//
// Copyright (c) 2016 Vsevolod Kukol
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
using System.Reflection;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Ide.Gui
{
	class StylesStringTagModel : IStringTagModel
	{
		static readonly Dictionary<string, PropertyInfo> tagGetters = new Dictionary<string, PropertyInfo> ();

		public object GetValue (string name)
		{
			PropertyInfo styleProp = null;
			if (!tagGetters.TryGetValue (name, out styleProp)) {
				styleProp = typeof (Styles).GetProperty (name, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
				if (styleProp == null)
					return null;
				tagGetters [name] = styleProp;
			}
			if (styleProp == null)
				return null;
			var value = styleProp.GetValue (typeof(Styles), null);
			if (value is Xwt.Drawing.Color)
				return ((Xwt.Drawing.Color)value).ToHexString (false);
			return value.ToString ();
		}
	}
}

