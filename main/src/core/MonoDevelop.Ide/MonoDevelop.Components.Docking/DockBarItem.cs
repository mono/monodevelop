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
using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.Components.Docking
{	
	class CrossfadeIcon: Gtk.Image
	{
		// This class should be subclassed from Gtk.Misc, but there is no reasonable way to do that due to there being no bindings to gtk_widget_set_has_window

		SurfaceWrapper primarySurface;
		SurfaceWrapper secondarySurface;
		Gdk.Pixbuf primary, secondary;

		float secondaryOpacity;

		public CrossfadeIcon (Gdk.Pixbuf primary, Gdk.Pixbuf secondary)
		{
			if (primary == null)
				throw new ArgumentNullException ("primary");
			if (secondary == null)
				throw new ArgumentNullException ("secondary");

			this.primary = primary.Copy ();
			this.secondary = secondary.Copy ();
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();

			using (Cairo.Context context = Gdk.CairoHelper.Create (GdkWindow)) {
				primarySurface = new SurfaceWrapper (context, primary);
				secondarySurface = new SurfaceWrapper (context, secondary);
			}

			primary.Dispose ();
			primary = null;

			secondary.Dispose ();
			secondary = null;
		}

		public void ShowPrimary ()
		{
			float startValue = secondaryOpacity;
			Components.Animation.Animate (this, name: "CrossfadeIconSwap",
			                              transform: x => startValue * (1.0f - x),
			                              callback: x => { secondaryOpacity = x; QueueDraw (); });
		}

		public void ShowSecondary ()
		{
			float startValue = secondaryOpacity;
			Components.Animation.Animate (this, name: "CrossfadeIconSwap",
			                              transform: x => startValue + x * (1.0f - startValue),
			                              callback: x => { secondaryOpacity = x; QueueDraw (); });
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			requisition.Width += primarySurface.Width;
			requisition.Height += primarySurface.Height;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (Cairo.Context context = Gdk.CairoHelper.Create (evnt.Window)) {
				if (secondaryOpacity < 1.0f)
					RenderIcon (context, primarySurface, 1.0f - (float)Math.Pow (secondaryOpacity, 3.0f));

				if (secondaryOpacity > 0.0f)
					RenderIcon (context, secondarySurface, secondaryOpacity);
			}

			return false;
		}

		void RenderIcon (Cairo.Context context, SurfaceWrapper surface, float opacity)
		{
			context.SetSourceSurface (surface.Surface, 
			                          Allocation.X + (Allocation.Width - surface.Width) / 2,
			                          Allocation.Y + (Allocation.Height - surface.Height) / 2);

			context.PaintWithAlpha (opacity);
		}
	}

	class DockBarItem: EventBox
	{
		DockBar bar;
		DockItem it;
		Box box;
		Label label;
		Alignment mainBox;
		AutoHideBox autoShowFrame;
		AutoHideBox hiddenFrame;
		uint autoShowTimeout = uint.MaxValue;
		uint autoHideTimeout = uint.MaxValue;
		int size;
		Gdk.Size lastFrameSize;
		MouseTracker tracker;
		Tweener hoverAnimTweener;
		CrossfadeIcon crossfade;

		public DockBarItem (DockBar bar, DockItem it, int size)
		{
			Events = Events | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
			this.size = size;
			this.bar = bar;
			this.it = it;
			VisibleWindow = false;
			UpdateTab ();
			lastFrameSize = bar.Frame.Allocation.Size;
			bar.Frame.SizeAllocated += HandleBarFrameSizeAllocated;

			tracker = new MouseTracker (this);
			tracker.TrackMotion = false;
			tracker.HoveredChanged += (sender, e) => {
				hoverAnimTweener.Stop ();
				hoverAnimTweener.Start ();

				if (crossfade == null)
					return;
	
				if (tracker.Hovered)
					crossfade.ShowSecondary ();
				else
					crossfade.ShowPrimary ();
			};

			hoverAnimTweener = new Tweener (100, 16);
			hoverAnimTweener.ValueUpdated += (sender, e) => QueueDraw ();
			hoverAnimTweener.Finished += (sender, e) => QueueDraw ();
		}
		
		void HandleBarFrameSizeAllocated (object o, SizeAllocatedArgs args)
		{
			if (!lastFrameSize.Equals (args.Allocation.Size)) {
				lastFrameSize = args.Allocation.Size;
				if (autoShowFrame != null)
					bar.Frame.UpdateSize (bar, autoShowFrame);
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			bar.Frame.SizeAllocated -= HandleBarFrameSizeAllocated;
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
			if (Child != null) {
				Widget w = Child;
				Remove (w);
				w.Destroy ();
			}
			
			mainBox = new Alignment (0,0,1,1);
			if (bar.Orientation == Gtk.Orientation.Horizontal) {
				box = new HBox ();
				if (bar.AlignToEnd)
					mainBox.SetPadding (3, 3, 11, 9);
				else
					mainBox.SetPadding (3, 3, 9, 11);
			}
			else {
				box = new VBox ();
				if (bar.AlignToEnd)
					mainBox.SetPadding (11, 9, 3, 3);
				else
					mainBox.SetPadding (9, 11, 3, 3);
			}
			
			Gtk.Widget customLabel = null;
			if (it.DockLabelProvider != null)
				customLabel = it.DockLabelProvider.CreateLabel (bar.Orientation);
			
			if (customLabel != null) {
				customLabel.ShowAll ();
				box.PackStart (customLabel, true, true, 0);
			}
			else {
				if (it.Icon != null) {
					Gdk.Pixbuf desat = it.Icon.Copy ();
					desat.SaturateAndPixelate (desat, 0.5f, false);
					crossfade = new CrossfadeIcon (desat, it.Icon);
					box.PackStart (crossfade, false, false, 0);
					desat.Dispose ();
				}
					
				if (!string.IsNullOrEmpty (it.Label)) {
					label = new Gtk.Label (it.Label);
					label.UseMarkup = true;
					if (bar.Orientation == Gtk.Orientation.Vertical)
						label.Angle = 270;
					box.PackStart (label, true, true, 0);
				} else
					label = null;
			}

			box.Spacing = 2;
			mainBox.Add (box);
			mainBox.ShowAll ();
			Add (mainBox);
			QueueDraw ();
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

		public void Present (bool giveFocus)
		{
			AutoShow ();
			if (giveFocus) {
				GLib.Timeout.Add (200, delegate {
					// Using a small delay because AutoShow uses an animation and setting focus may
					// not work until the item is visible
					it.SetFocus ();
					ScheduleAutoHide (false);
					return false;
				});
			}
		}

		public void Minimize ()
		{
			AutoHide (false);
		}

		void AutoShow ()
		{
			Console.WriteLine ("Show");
			UnscheduleAutoHide ();
			if (autoShowFrame == null) {
				if (hiddenFrame != null)
					bar.Frame.AutoHide (it, hiddenFrame, false);
				autoShowFrame = bar.Frame.AutoShow (it, bar, size);
				autoShowFrame.EnterNotifyEvent += OnFrameEnter;
				autoShowFrame.LeaveNotifyEvent += OnFrameLeave;
				autoShowFrame.KeyPressEvent += OnFrameKeyPress;
				QueueDraw ();
			}
		}
		
		void AutoHide (bool animate)
		{
			Console.WriteLine ("Hide");
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
				QueueDraw ();
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
					if (it.Widget.FocusChild != null && !force)
						return true;
					// Don't hide the item if the mouse pointer is still inside the window. Try again later.
					int px, py;
					it.Widget.GetPointer (out px, out py);
					if (it.Widget.Visible && it.Widget.IsRealized && it.Widget.Allocation.Contains (px + it.Widget.Allocation.X, py + it.Widget.Allocation.Y) && !force)
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
			if (bar.HoverActivationEnabled && autoShowFrame == null) {
				ScheduleAutoShow ();
				QueueDraw ();
			}
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			ScheduleAutoHide (true);
			if (autoShowFrame == null) {
				QueueDraw ();
			}
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

		bool itemActivated;

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.TriggersContextMenu ()) {
				it.ShowDockPopupMenu (evnt.Time);
			} else if (evnt.Button == 1) {
				if (evnt.Type == Gdk.EventType.TwoButtonPress) {
					// Instead of changing the state of the pad here, do it when the button is released.
					// Changing the state will make this bar item to vanish before the ReleaseEvent is received, and in this
					// case the ReleaseEvent may be fired on another widget that is taking the space of this bar item.
					// This was happening for example with the feedback button.
					itemActivated = true;
				} else {
					AutoShow ();
					it.Present (true);
				}
			}
			return true;
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (itemActivated) {
				itemActivated = false;
				it.Status = DockItemStatus.Dockable;
			}
			return true;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				float value = hoverAnimTweener.IsRunning ? hoverAnimTweener.Value : 1.0f;
				value = tracker.Hovered ? value : 1.0f - value;
				var alloc = Allocation;
				var siblings = (Parent as Gtk.Container).Children;

				bool drawStartSeperator = siblings[0] != this || bar.AlignToEnd;
				bool drawEndSeparator = siblings[siblings.Length - 1] != this || !bar.AlignToEnd;

				Cairo.LinearGradient lg;

				if (bar.Orientation == Orientation.Horizontal) {
					lg = new Cairo.LinearGradient (alloc.X, 0, alloc.X + alloc.Width, 0);
				} else {
					lg = new Cairo.LinearGradient (0, alloc.Y, 0, alloc.Y + alloc.Height);
				}

				Cairo.Color primaryColor = Styles.DockBarPrelightColor;
				primaryColor.A = value;

				Cairo.Color transparent = primaryColor;
				transparent.A = 0;

				lg.AddColorStop (0.0, transparent);
				lg.AddColorStop (0.35, primaryColor);
				lg.AddColorStop (0.65, primaryColor);
				lg.AddColorStop (1.0, transparent);

				context.Rectangle (alloc.ToCairoRect ());
				context.Pattern = lg;
				context.Fill ();

				lg.Destroy ();

				if (bar.Orientation == Orientation.Horizontal) {
					Cairo.PointD start = new Cairo.PointD (alloc.X + 0.5, alloc.Y);
					Cairo.PointD end = new Cairo.PointD (alloc.X + 0.5, alloc.Y + alloc.Height);
					Cairo.PointD offset = new Cairo.PointD (1, 0);

					if (drawStartSeperator)
						DrawEngravedLine (context, start, end, offset, Styles.DockBarSeparatorColorDark, Styles.DockBarSeparatorColorLight, value);

					start.X += alloc.Width - 2;
					end.X += alloc.Width - 2;

					if (drawEndSeparator)
						DrawEngravedLine (context, start, end, offset, Styles.DockBarSeparatorColorDark, Styles.DockBarSeparatorColorLight, value);
				} else {
					Cairo.PointD start = new Cairo.PointD (alloc.X, alloc.Y + 0.5);
					Cairo.PointD end = new Cairo.PointD (alloc.X + alloc.Width, alloc.Y + 0.5);
					Cairo.PointD offset = new Cairo.PointD (0, 1);

					if (drawStartSeperator)
						DrawEngravedLine (context, start, end, offset, Styles.DockBarSeparatorColorDark, Styles.DockBarSeparatorColorLight, value);

					start.Y += alloc.Height - 2;
					end.Y += alloc.Height - 2;

					if (drawEndSeparator)
						DrawEngravedLine (context, start, end, offset, Styles.DockBarSeparatorColorDark, Styles.DockBarSeparatorColorLight, value);
				}

				context.LineWidth = 1;
				Cairo.Color strokeColor = Styles.DockBarSeparatorColorDark;
				strokeColor.A *= value;
				context.Color = strokeColor;
				context.Stroke ();
			}
			return base.OnExposeEvent (evnt);
		}

		void DrawEngravedLine (Cairo.Context context, Cairo.PointD start, Cairo.PointD end, Cairo.PointD engraveOffset, Cairo.Color main, Cairo.Color engrave, float alpha)
		{
			Cairo.Color color = main;
			color.A *= alpha;
			context.MoveTo (start.X, start.Y);
			context.LineTo (end.X, end.Y);
			context.Color = color;
			context.LineWidth = 1;
			context.Stroke ();

			color = engrave;
			color.A *= alpha;
			context.MoveTo (start.X + engraveOffset.X, start.Y + engraveOffset.Y);
			context.LineTo (end.X + engraveOffset.X, end.Y + engraveOffset.Y);
			context.Color = color;
			context.Stroke ();
		}
	}
}
