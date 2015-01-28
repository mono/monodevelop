//
// ContextMenu.cs
//
// Author:
//       Greg Munn <greg.munn@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc
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
using MonoDevelop.Core;

namespace MonoDevelop.Components
{
	public class ContextMenu
	{
		readonly ContextMenuItemCollection items;

		public ContextMenu ()
		{
			items = new ContextMenuItemCollection (this);
		}

		public ContextMenuItemCollection Items {
			get { return items; }
		}

		/// <summary>
		/// Removes all separators of the menu which follow another separator
		/// </summary>
		public void CollapseSeparators ()
		{
			bool wasSeparator = true;
			for (int n=0; n<Items.Count; n++) {
				if (Items[n] is SeparatorContextMenuItem) {
					if (wasSeparator)
						Items.RemoveAt (n--);
					else
						wasSeparator = true;
				} else
					wasSeparator = false;
			}
			if (Items.Count > 0 && Items[Items.Count - 1] is SeparatorContextMenuItem)
				Items.RemoveAt (Items.Count - 1);
		}

		public void Show (Gtk.Widget parent, Gdk.EventButton evt)
		{
			#if MAC
			if (Platform.IsMac) {
				ContextMenuExtensionsMac.ShowContextMenu (parent, evt, this);
				return;
			}
			#endif

			ContextMenuExtensionsGtk.ShowContextMenu (parent, evt, this);
		}
	}
}