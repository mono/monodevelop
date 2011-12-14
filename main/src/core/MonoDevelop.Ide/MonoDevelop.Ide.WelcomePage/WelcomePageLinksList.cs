// 
// WelcomePageLinksList.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
using Gtk;
using System.Xml.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageLinksList : VBox
	{
		public WelcomePageLinksList (XElement el)
		{
			IconSize iconSize = IconSize.Menu;
			var iconSizeAtt = el.Attribute ("iconSize");
			if (iconSizeAtt != null) {
				iconSize = (IconSize) Enum.Parse (typeof (IconSize), (string) iconSizeAtt);
			}
			
			var homogeneousAtt = el.Attribute ("homogeneous");
			if (homogeneousAtt != null)
				this.Homogeneous = (bool) homogeneousAtt;
			
			foreach (var child in el.Elements ()) {
				if (child.Name != "Link")
					throw new InvalidOperationException ("Unexpected child '" + child.Name + "'");
				var button = new WelcomePageLinkButton (child, iconSize);
				this.PackStart (button, true, false, 0);
			}
		}
	}
}