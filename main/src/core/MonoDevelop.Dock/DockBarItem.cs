//
// DockBarItem.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using Gtk;

namespace MonoDevelop.Components.Docking
{
	class DockBarItem: EventBox
	{
		DockBar bar;
		DockItem it;
		Box box;
		AutoHideBox autoShowFrame;
		AutoHideBox hiddenFrame;
		uint autoShowTimeout = uint.MaxValue;
		uint autoHideTimeout = uint.MaxValue;
		int size;
		
		public DockBarItem (DockBar bar, DockItem it, int size)
		{
			Events = Events | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
			this.size = size;
			this.bar = bar;
			this.it = it;
			VisibleWindow = false;
			UpdateTab ();
		}
		
		public void Close ()
		{
			UnscheduleAutoShow ();
			UnscheduleAutoHide ();
			AutoHide (false);
			bar.RemoveItem (this);
			Destroy ();
		}
		
		public int Size {
			get { return size; }
			set { size = value; }
		}
		
		public void UpdateTab ()
		{
			if (box != null) {
				Remove (box);
				box.Destroy ();
			}
			
			if (bar.Orientation == Gtk.Orientation.Horizontal)
				box = new HBox ();
			else
				box = new VBox ();
			
			if (!string.IsNullOrEmpty (it.Icon))
				box.PackStart (new Gtk.Image (it.Icon, IconSize.Menu), false, false, 0);
				
			if (!string.IsNullOrEmpty (it.Label)) {
				Label lab = new Gtk.Label (it.Label);
				lab.UseMarkup = true;
				if (bar.Orientation == Gtk.Orientation.Vertical)
					lab.Angle = 270;
				box.PackStart (lab, true, true, 0);
			}

			box.ShowAll ();
			box.BorderWidth = 3;
			box.Spacing = 2;
			Add (box);
		}
		
		public MonoDevelop.Components.Docking.DockItem DockItem {
			get {
				return it;
			}
		}
		
		protected override void OnHidden ()
		{
			base.OnHidden ();
			UnscheduleAutoShow ();
			UnscheduleAutoHide ();
			AutoHide (false);
		}

		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Gtk.Style.PaintBox (Style, GdkWindow, StateType.Normal, ShadowType.Out, evnt.Area, this, "", Allocation.Left, Allocation.Top, Allocation.Width, Allocation.Height);
			bool res = base.OnExposeEvent (evnt);
			return res;
		}

		public void Present ()
		{
			AutoShow ();
			GLib.Timeout.Add (200, delegate {
				// Using a small delay because AutoShow uses an animation and setting focus may
				// not work until the item is visible
				it.Widget.ChildFocus (DirectionType.TabForward);

				// Make sure focus is not given to internal children
				Container c = ((Window)it.Widget.Toplevel).Focus.Parent as Container;
				if (c.Children.Length == 0)
					((Window)it.Widget.Toplevel).Focus = c;
				
				ScheduleAutoHide (false);
				return false;
			});
		}

		void AutoShow ()
		{
			UnscheduleAutoHide ();
			if (autoShowFrame == null) {
				if (hiddenFrame != null)
					bar.Frame.AutoHide (it, hiddenFrame, false);
				autoShowFrame = bar.Frame.AutoShow (it, bar, size);
				autoShowFrame.EnterNotifyEvent += OnFrameEnter;
				autoShowFrame.LeaveNotifyEvent += OnFrameLeave;
				autoShowFrame.KeyPressEvent += OnFrameKeyPress;
			}
		}
		
		void AutoHide (bool animate)
		{
			UnscheduleAutoShow ();
			if (autoShowFrame != null) {
				size = autoShowFrame.Size;
				hiddenFrame = autoShowFrame;
				autoShowFrame.Hidden += delegate {
					hiddenFrame = null;
				};
				bar.Frame.AutoHide (it, autoShowFrame, animate);
				autoShowFrame.EnterNotifyEvent -= OnFrameEnter;
				autoShowFrame.LeaveNotifyEvent -= OnFrameLeave;
				autoShowFrame.KeyPressEvent -= OnFrameKeyPress;
				autoShowFrame = null;
			}
		}
		
		void ScheduleAutoShow ()
		{
			UnscheduleAutoHide ();
			if (autoShowTimeout == uint.MaxValue) {
				autoShowTimeout = GLib.Timeout.Add (bar.Frame.AutoShowDelay, delegate {
					autoShowTimeout = uint.MaxValue;
					AutoShow ();
					return false;
				});
			}
		}
		
		void ScheduleAutoHide (bool cancelAutoShow)
		{
			ScheduleAutoHide (cancelAutoShow, false);
		}
		
		void ScheduleAutoHide (bool cancelAutoShow, bool force)
		{
			if (cancelAutoShow)
				UnscheduleAutoShow ();
			if (force)
				it.Widget.FocusChild = null;
			if (autoHideTimeout == uint.MaxValue) {
				autoHideTimeout = GLib.Timeout.Add (force ? 0 : bar.Frame.AutoHideDelay, delegate {
					// Don't hide the item if it has the focus. Try again later.
					if (it.Widget.FocusChild != null)
						return true;
					autoHideTimeout = uint.MaxValue;
					AutoHide (true);
					return false;
				});
			}
		}
		
		void UnscheduleAutoShow ()
		{
			if (autoShowTimeout != uint.MaxValue) {
				GLib.Source.Remove (autoShowTimeout);
				autoShowTimeout = uint.MaxValue;
			}
		}
		
		void UnscheduleAutoHide ()
		{
			if (autoHideTimeout != uint.MaxValue) {
				GLib.Source.Remove (autoHideTimeout);
				autoHideTimeout = uint.MaxValue;
			}
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			ScheduleAutoShow ();
			ModifyBg (StateType.Normal, bar.Style.Background (Gtk.StateType.Prelight));
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			ScheduleAutoHide (true);
			ModifyBg (StateType.Normal, bar.Style.Background (Gtk.StateType.Normal));
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		void OnFrameEnter (object s, Gtk.EnterNotifyEventArgs args)
		{
			AutoShow ();
		}

		void OnFrameKeyPress (object s, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Escape)
				ScheduleAutoHide (true, true);
		}
		
		void OnFrameLeave (object s, Gtk.LeaveNotifyEventArgs args)
		{
			if (args.Event.Detail != Gdk.NotifyType.Inferior)
				ScheduleAutoHide (true);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) {
				if (evnt.Type == Gdk.EventType.TwoButtonPress)
					it.Status = DockItemStatus.Dockable;
				else
					AutoShow ();
			}
			else if (evnt.Button == 3)
				it.ShowDockPopupMenu (evnt.Time);
			return base.OnButtonPressEvent (evnt);
		}
	}
}
