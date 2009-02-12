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
		
		const string USE_CUSTOM_FONT_KEY = "MonoDevelop.Core.Gui.Pads.UseCustomFont";
		const string CUSTOM_FONT_KEY = "MonoDevelop.Core.Gui.Pads.CustomFont";
		
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
				PropertyService.PropertyChanged += PropertyChanged;
			}
			
			if (PropertyService.Get (USE_CUSTOM_FONT_KEY, false)) {
				string name = styleSource.Style.FontDescription.ToString ();
				Update (PropertyService.Get (CUSTOM_FONT_KEY, name));
			}
		}
		
		void PropertyChanged (object sender, PropertyChangedEventArgs prop)
		{
			switch (prop.Key) {
			case USE_CUSTOM_FONT_KEY:
				string name = styleSource.Style.FontDescription.ToString ();
				if ((bool) prop.NewValue)
					name = PropertyService.Get (CUSTOM_FONT_KEY, name);
				
				Update (name);
				if (resizer != null)
					resizer ();
				break;
				
			case CUSTOM_FONT_KEY:
				if (!(PropertyService.Get<bool> (USE_CUSTOM_FONT_KEY)))
					break;
				
				Update ((string) prop.NewValue);
				if (resizer != null)
					resizer ();
				break;
			}
		}
		
		void Update (string name)
		{
			FontDescription desc = Pango.FontDescription.FromString (name);
				if (desc != null)
					updater (desc);
		}
		
		public void Dispose ()
		{
			if (styleSource != null) {
				PropertyService.PropertyChanged -= PropertyChanged;
				styleSource = null;
			}
		}
	}
}
