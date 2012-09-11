// 
// PadFontChanger.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using Pango;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	
	
	public sealed class PadFontChanger : IDisposable
	{
		Gtk.Widget styleSource;
		Action<FontDescription> updater;
		Action resizer;

		public PadFontChanger (Gtk.Widget styleSource, Action<FontDescription> updater)
			: this (styleSource, updater, null)
		{
		}
		
		public PadFontChanger (Gtk.Widget styleSource, Action<FontDescription> updater, Action resizer)
		{
			this.styleSource = styleSource;
			this.updater = updater;
			this.resizer = resizer;
			
			if (styleSource != null) {
				IdeApp.Preferences.CustomPadFontChanged += PropertyChanged;
			}
			
			Update ();
		}
		
		void PropertyChanged (object sender, EventArgs prop)
		{
			Update ();
			if (resizer != null)
				resizer ();
		}
		
		void Update ()
		{
			var font = IdeApp.Preferences.CustomPadFont;
			if (font != null)
				updater (font);
		}
		
		public void Dispose ()
		{
			if (styleSource != null) {
				IdeApp.Preferences.CustomPadFontChanged -= PropertyChanged;
				styleSource = null;
			}
		}
	}
}
