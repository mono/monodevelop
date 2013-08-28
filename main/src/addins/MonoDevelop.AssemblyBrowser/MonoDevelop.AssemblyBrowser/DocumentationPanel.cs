// 
// DocumentationPanel.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Gdk;
using MonoDevelop.Core;

namespace MonoDevelop.AssemblyBrowser
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class DocumentationPanel : Gtk.DrawingArea
	{
		Pango.Layout layout;
		
		string markup;
		public string Markup {
			get { return markup; }
			set { markup = value; CalculateSize ();}
		}
		
		public DocumentationPanel ()
		{
			layout = new Pango.Layout (PangoContext);
			layout.Wrap = Pango.WrapMode.Word;
			layout.FontDescription = Pango.FontDescription.FromString (PropertyService.Get<string> ("FontName"));
		}
		
		protected override void OnDestroyed ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
			base.OnDestroyed ();
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			Gdk.Window win = evnt.Window;
			win.DrawRectangle (Style.BaseGC (string.IsNullOrEmpty (Markup) ? StateType.Insensitive : StateType.Normal), true, evnt.Area);
			int x = 0;
			int y = 0;
			if (string.IsNullOrEmpty (Markup)) {
				layout.SetMarkup (GettextCatalog.GetString ("No documentation available."));
				int width, height;
				layout.GetPixelSize (out width, out height);
				x = (Allocation.Width - width) / 2;
				y = (Allocation.Height - height) / 2;
			} else {
				layout.SetMarkup (Markup);
			}
			layout.Width = Allocation.Width * (int)Pango.Scale.PangoScale;
			win.DrawLayout (Style.TextGC (StateType.Normal), x, y, layout);
			return true;
		}
		
		public void CalculateSize ()
		{
			layout.SetMarkup (Markup ?? "");
			layout.Width = Allocation.Width * (int)Pango.Scale.PangoScale;
			int width, height;
			layout.GetPixelSize (out width, out height);
			SetSizeRequest (width, height);
		}
 
	}
}
