//
// MDEntry.cs
//
// Author:
//       Cody Russell <cody@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (https://xamarin.com)
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
using MonoDevelop.Core;

namespace MonoDevelop.Components
{
	[System.ComponentModel.Category ("MonoDevelop.Components")]
	[System.ComponentModel.ToolboxItem(true)]
	public class MDEntry : Gtk.Entry
	{
		public MDEntry ()
		{
			SetupMenu ();
		}

		public MDEntry (IntPtr raw) : base (raw)
		{
			SetupMenu ();
		}

		public MDEntry (string initial_text) : base (initial_text)
		{
			SetupMenu ();
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evt)
		{
			if (evt.Type == Gdk.EventType.ButtonPress && evt.Button == 3) {
				UpdateMenuItemSensitivities ();
				context_menu.Show (this, evt);
				return false;
			}

			return base.OnButtonPressEvent (evt);
		}

		void SetupMenu ()
		{
			context_menu = new ContextMenu ();
			cut = new ContextMenuItem { Label = GettextCatalog.GetString ("Cut") };
			cut.Clicked += (sender, e) => CutSelectedText ();
			context_menu.Items.Add (cut);

			copy = new ContextMenuItem { Label = GettextCatalog.GetString ("Copy") };
			copy.Clicked += (sender, e) => CopyText ();
			context_menu.Items.Add (copy);

			paste = new ContextMenuItem { Label = GettextCatalog.GetString ("Paste") };
			paste.Clicked += (sender, e) => PasteText ();
			context_menu.Items.Add (paste);

			delete = new ContextMenuItem { Label = GettextCatalog.GetString ("Delete") };
			delete.Clicked += (sender, e) => DeleteSelectedText ();

			select_all = new ContextMenuItem { Label = GettextCatalog.GetString ("Select All") };
			select_all.Clicked += (sender, e) => SelectAllText ();
			context_menu.Items.Add (select_all);
		}

		void CutSelectedText ()
		{
			if (IsEditable) {
				int selection_start, selection_end;

				if (GetSelectionBounds (out selection_start, out selection_end)) {
					var text = GetChars (selection_start, selection_end);
					var clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

					clipboard.Text = text;
					DeleteText (selection_start, selection_end);
				}
			} else {
				ErrorBell ();
			}
		}

		void CopyText ()
		{
			int selection_start, selection_end;

			if (GetSelectionBounds (out selection_start, out selection_end)) {
				var text = GetChars (selection_start, selection_end);
				var clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

				clipboard.Text = text;
			}
		}

		void PasteText ()
		{
			if (IsEditable) {
				var clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

				clipboard.RequestText ((cb, text) => {
					InsertText (text);
				});
			} else {
				ErrorBell ();
			}
		}

		void DeleteSelectedText ()
		{
			if (IsEditable) {
				int selection_start, selection_end;

				if (GetSelectionBounds (out selection_start, out selection_end)) {
					DeleteText (selection_start, selection_end);
				}
			}
		}

		void SelectAllText ()
		{
			SelectRegion (0, Text.Length - 1);
		}

		void UpdateMenuItemSensitivities ()
		{
			copy.Sensitive = select_all.Sensitive = (Text.Length > 0);
			cut.Sensitive = (Text.Length > 0 && IsEditable);
			paste.Sensitive = this.IsEditable;
		}

		ContextMenu context_menu;
		ContextMenuItem cut;
		ContextMenuItem copy;
		ContextMenuItem paste;
		ContextMenuItem delete;
		ContextMenuItem select_all;
	}
}
