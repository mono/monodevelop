// 
// JobHistoryWindow.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using MonoDevelop.Ide.Jobs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Jobs
{
	class JobHistoryWindow: Window
	{
		Table list;
		MiniButton toggle;
		
		public JobHistoryWindow (MiniButton toggle): base (Gtk.WindowType.Toplevel)
		{
			this.toggle = toggle;
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.CanFocus = true;
			this.DestroyWithParent = true;
			this.Decorated = false;
			this.SkipTaskbarHint = true;
			
			ScrolledWindow sw = new ScrolledWindow ();
			sw.ShadowType = ShadowType.Out;
			sw.HscrollbarPolicy = PolicyType.Never;
			sw.VscrollbarPolicy = PolicyType.Never;
			Add (sw);
			
			list = new Table (1, 3, false);
			list.RowSpacing = 3;
			list.ColumnSpacing = 3;
			list.BorderWidth = 3;
			sw.AddWithViewport (list);
			list.Show ();
			sw.Show ();
			
			Fill ();
			
			Requisition req = SizeRequest ();
			if (req.Width < 150) WidthRequest = 150;
			if (req.Height < 100) HeightRequest = 100;
			
			int maxHeight = (int) ((float)IdeApp.Workbench.RootWindow.Screen.Height * 0.4);
			if (req.Height > maxHeight) {
				sw.VscrollbarPolicy = PolicyType.Automatic;
				sw.HeightRequest = maxHeight;
			}
		}
		
/*		[GLib.ConnectBefore]
		void HandleListButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
		{
			TreePath path;
			TreeViewColumn col;
			list.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path, out col);
			
			TreeIter it;
			store.GetIter (out it, path);
			JobInstance ji = (JobInstance) store.GetValue (it, 2);
			
			if (col == stopCol) {
				string oper = (string) store.GetValue (it, 3);
				if (oper == Gtk.Stock.Stop) {
					ji.Monitor.AsyncOperation.Cancel ();
					Destroy ();
				}
				else if (oper == Gtk.Stock.Execute && ji.Job.Reusable) {
					ji.Job.Run ();
					Destroy ();
				}
			} else if (ji.HasStatusView) {
				ji.ShowStatusView (this, Gtk.PositionType.Left, 100, true);
				Destroy ();
			}
		}

		void HandleListRowActivated (object o, RowActivatedArgs args)
		{
			TreeIter it;
			store.GetIter (out it, args.Path);
			JobInstance ji = (JobInstance) store.GetValue (it, 2);
			if (ji.Monitor.AsyncOperation.IsCompleted && ji.Job.Reusable) {
				ji.Job.Run ();
				Destroy ();
			}
		}*/
		
		void Fill ()
		{
			foreach (Widget w in list.Children) {
				list.Remove (w);
				w.Destroy ();
			}
			
			uint n=0;
			
			foreach (JobInstance ji in JobService.GetJobHistory ()) {
				if (n > 0) {
					HSeparator sep = new HSeparator ();
					sep.Show ();
					list.Attach (sep, 0u, 3u, n, n + 1);
					n++;
				}
				
				string desc = string.Empty;
				if (!ji.Monitor.AsyncOperation.IsCompleted)
					desc += "<span color='blue'>";
				desc += "<b>" + GLib.Markup.EscapeText (ji.Job.Title) + "</b>";
				if (!ji.Monitor.AsyncOperation.IsCompleted)
					desc += "</span>";
				if (!string.IsNullOrEmpty (ji.Job.Description))
					desc += "\n<small>" + GLib.Markup.EscapeText (ji.Job.Description) + "</small>";
				if (!string.IsNullOrEmpty (ji.StatusMessage))
					desc += "\n<small>" + GLib.Markup.EscapeText (ji.StatusMessage) + "</small>";
				
				Image img = new Image (ji.ComposedStatusIcon, IconSize.Menu);
				img.Yalign = 0;
				img.Show ();
				
				Label label = new Label ();
				label.Markup = desc;
				label.Yalign = 0;
				label.Xalign = 0;
				label.Show ();
				
				HBox opsBox = new HBox ();
				Gtk.Widget mw;
				ji.Job.FillExtendedStatusPanel (ji, opsBox, out mw);
				opsBox.Show ();
				VBox rbox = new VBox ();
				rbox.PackStart (opsBox, false, false, 0);
				rbox.Show ();
				
				list.Attach (img, 0u, 1u, n, n + 1);
				list.Attach (label, 1u, 2u, n, n + 1);
				list.Attach (rbox, 2u, 3u, n, n + 1);
				
				n++;
			}
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape) {
				Destroy ();
				return false;
			}
			else
				return base.OnKeyPressEvent (evnt);
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			Destroy ();
			return false;
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			GLib.Timeout.Add (50, delegate {
				// Depress after a small delay. It will avoid an immediate reshow if the user is clicking
				// the history button to hide the list
				toggle.Pressed = false;
				return false;
			});
		}
	}
	
	class JobRow: HBox
	{
		public JobInstance Job;
		
		public JobRow (): base (false, 3)
		{
		}
	}
}
