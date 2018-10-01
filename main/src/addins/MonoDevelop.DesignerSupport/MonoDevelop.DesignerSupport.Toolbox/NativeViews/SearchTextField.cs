//
// SearchTextField.cs
//
// Author:
//       josemedranojimenez <jose.medrano@xamarin.com>
//
// Copyright (c) 2018 (c) Jose Medrano
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
using AppKit;
using CoreGraphics;
using Foundation;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class SearchTextField : NSSearchField
	{
		public string Text {
			get { return StringValue; }
			set {
				StringValue = value;
			}
		}

		public SearchTextField ()
		{
			WantsLayer = true;
		}

		public SearchTextField (IntPtr handle) : base (handle)
		{
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);
			if (Layer != null) {
				Layer.BorderWidth = Styles.SearchTextFieldLineBorderWidth;
				Layer.BorderColor = Styles.SearchTextFieldLineBorderColor.CGColor;
				Layer.BackgroundColor = Styles.SearchTextFieldLineBackgroundColor.CGColor;
			}
		}
	}
}
