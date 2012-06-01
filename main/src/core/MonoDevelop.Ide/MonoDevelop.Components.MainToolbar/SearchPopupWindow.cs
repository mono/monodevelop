//
// SearchPopupWindow.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Components.MainToolbar
{
	public class SearchPopupWindow : Gtk.Window
	{
		SearchPopupWidget widget = new SearchPopupWidget ();

		public SearchPopupWindow () : base(WindowType.Popup)
		{
			Add (widget);
			widget.SizeRequested += delegate(object o, SizeRequestedArgs args) {
				Resize (args.Requisition.Width, args.Requisition.Height);
			};
		}

		public void Update (string searchPattern)
		{
			widget.Update (searchPattern);
		}


		SearchEntry matchEntry;
		public void Attach (SearchEntry matchEntry)
		{
			this.matchEntry = matchEntry;
			matchEntry.Entry.KeyPressEvent += HandleKeyPress;
			matchEntry.Activated += HandleActivated;
		}


		void Detach ()
		{
			if (matchEntry != null) {
				matchEntry.Entry.KeyPressEvent -= HandleKeyPress;
				matchEntry.Activated -= HandleActivated;
				matchEntry = null;
			}
		}

		void HandleActivated (object sender, EventArgs e)
		{
			OpenFile ();
		}

		protected virtual void HandleKeyPress (object o, KeyPressEventArgs args)
		{
			// Up and down move the tree selection up and down
			// for rapid selection changes.
			Gdk.EventKey key = args.Event;
			switch (key.Key) {
			case Gdk.Key.Page_Down:
//				list.ModifySelection (false, true, (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
				args.RetVal = true;
				break;
			case Gdk.Key.Page_Up:
//				list.ModifySelection (true, true, (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
				args.RetVal = true;
				break;
			case Gdk.Key.Up:
//				list.ModifySelection (true, false, (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
				args.RetVal = true;
				break;
			case Gdk.Key.Down:
//				list.ModifySelection (false, false, (args.Event.State & ModifierType.ShiftMask) == ModifierType.ShiftMask);
				args.RetVal = true;
				break;
			case Gdk.Key.Escape:
				Destroy ();
				args.RetVal = true;
				break;
			}
		}

		void OpenFile ()
		{
//			locations.Clear ();
//			if (list.SelectedRows.Count != 0) {
//				foreach (int sel in list.SelectedRows) {
//					var res = lastResult.results [sel];
//					if (res.File == null)
//						continue;
//					var loc = new OpenLocation (res.File, res.Row, res.Column);
//					if (loc.Line == -1) {
//						int i = Query.LastIndexOf (':');
//						if (i != -1) {
//							if (!int.TryParse (Query.Substring (i + 1), out loc.Line))
//								loc.Line = -1;
//						}
//					}
//					locations.Add (loc);
//				}
//				foreach (var loc in locations)
//					IdeApp.Workbench.OpenDocument (loc.Filename, loc.Line, loc.Column);
			Destroy ();
//			}
		}


		protected override void OnDestroyed ()
		{
			Detach ();
			base.OnDestroyed ();
		}
		
	}
}

