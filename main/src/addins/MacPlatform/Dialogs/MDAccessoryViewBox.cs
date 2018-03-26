//
// MacAccessoryViewBox.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
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
using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using CoreGraphics;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.MacInterop;

namespace MonoDevelop.MacIntegration
{
	class MDAccessoryViewBox : MDBox
	{
		readonly List<MDAlignment> labels = new List<MDAlignment> ();
		readonly List<MDAlignment> controls = new List<MDAlignment> ();

		public MDAccessoryViewBox () : base (LayoutDirection.Vertical, 2, 2)
		{
		}

		public void AddControl (NSControl control, string text)
		{
			int minWidth = string.IsNullOrEmpty (text) ? 0 : 200;
			var labelAlignment = new MDAlignment (new MDLabel (text) { Alignment = NSTextAlignment.Right }, true);
			labels.Add (labelAlignment);

			var controlAlignment = new MDAlignment (control, true) { MinWidth = minWidth };
			controls.Add (controlAlignment);

			Add (new MDBox (LayoutDirection.Horizontal, 2, 0) {
				{ labelAlignment },
				{ controlAlignment },
			});
		}

		public override void Layout ()
		{
			if (labels.Count > 0) {
				float w = labels.Max (l => l.MinWidth);
				foreach (var l in labels) {
					l.MinWidth = w;
					l.XAlign = LayoutAlign.Begin;
				}
			}

			if (controls.Count > 0) {
				float w = controls.Max (c => c.MinWidth);
				foreach (var c in controls) {
					c.MinWidth = w;
					c.XAlign = LayoutAlign.Begin;
				}
			}

			base.Layout ();
		}
	}
}
