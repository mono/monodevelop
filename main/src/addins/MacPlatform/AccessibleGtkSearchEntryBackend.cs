//
// AccessibleGtkSearchEntryBackend.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2019 
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
using MonoDevelop.Components;
using Xwt.Backends;
using Xwt.GtkBackend;

namespace MacPlatform
{
	public class AccessibleGtkSearchEntryBackend : TextEntryBackend, ISearchTextEntryBackend
	{
		SearchEntry searchEntry;

		protected override Gtk.Entry TextEntry {
			get {
				return searchEntry.Entry;
			}
		}

		public override void Initialize ()
		{
			searchEntry = new SearchEntry ();
			searchEntry.ForceFilterButtonVisible = true;
			searchEntry.RoundedShape = true;
			searchEntry.HasFrame = true;
			((WidgetBackend)this).Widget = searchEntry;
			searchEntry.Show ();
		}

		public override void SetFocus ()
		{
			base.SetFocus ();
			TextEntry.GrabFocus ();
		}

		public override bool ShowFrame {
			get {
				return searchEntry.HasFrame;
			}
			set {
				searchEntry.HasFrame = value;
			}
		}
	}
}
