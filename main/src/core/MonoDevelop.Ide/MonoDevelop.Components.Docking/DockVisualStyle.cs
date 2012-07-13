//
// DockVisualStyle.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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

namespace MonoDevelop.Components.Docking
{
	public class DockVisualStyle
	{
		public static readonly Gdk.Color DefaultColor = new Gdk.Color (0,0,0);

		public Gdk.Color PadBackgroundColor { get; set; }
		public Gdk.Color PadTitleLabelColor { get; set; }
		public DockTabStyle TabStyle { get; set; }
		public Gdk.Color TreeBackgroundColor { get; set; }
		public bool ShowPadTitleIcon { get; set; }
		public bool UppercaseTitles { get; set; }
		public bool ExpandedTabs { get; set; }
		public Gdk.Color InactivePadBackgroundColor { get; set; }

		public DockVisualStyle ()
		{
			ShowPadTitleIcon = true;
		}
	}

	public enum DockTabStyle
	{
		Normal,
		Simple
	}
}

