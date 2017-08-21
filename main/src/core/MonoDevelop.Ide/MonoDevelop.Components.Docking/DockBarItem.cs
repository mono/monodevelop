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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using Xwt.Motion;
using Animations = Xwt.Motion.AnimationExtensions;
using MonoDevelop.Core;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Components.Docking
{	
	class CrossfadeIcon: Gtk.Image, IAnimatable
	{
		// This class should be subclassed from Gtk.Misc, but there is no reasonable way to do that due to there being no bindings to gtk_widget_set_has_window

		Xwt.Drawing.Image primary, secondary;

		double secondaryOpacity;

		public CrossfadeIcon (Xwt.Drawing.Image primary, Xwt.Drawing.Image secondary)
		{
			if (primary == null)
				throw new ArgumentNullException (nameof (primary));
			if (secondary == null)
				throw new ArgumentNullException (nameof (secondary));

			this.primary = primary;
			this.secondary = secondary;
		}

		void IAnimatable.BatchBegin () { }
		void IAnimatable.BatchCommit () { QueueDraw (); }

		public void ShowPrimary ()
		{
			AnimateCrossfade (false);
		}

		public void ShowSecondary ()
		{
			AnimateCrossfade (true);
		}

		void AnimateCrossfade (bool toSecondary)
		{
			this.Animate ("CrossfadeIconSwap",
			              x => secondaryOpacity = x,
			              secondaryOpacity,
			              toSecondary ? 1.0f : 0.0f);
		}

		protected override void OnDestroyed ()
		{
			this.AbortAnimation ("CrossfadeIconSwap");
			base.OnDestroyed ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			requisition.Width = (int) primary.Width;
			requisition.Height = (int) primary.Height;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (Cairo.Context context = Gdk.CairoHelper.Create (evnt.Window)) {
				if (secondaryOpacity < 1.0f)
					RenderIcon (context, primary, 1.0f - (float)Math.Pow (secondaryOpacity, 3.0f));

				if (secondaryOpacity > 0.0f)
					RenderIcon (context, secondary, secondaryOpacity);
			}

			return false;
		}

		void RenderIcon (Cairo.Context context, Xwt.Drawing.Image surface, double opacity)
		{
			context.DrawImage (this, surface.WithAlpha (opacity),
			                          Allocation.X + (Allocation.Width - surface.Width) / 2,
			                          Allocation.Y + (Allocation.Height - surface.Height) / 2);
		}
	}

	class DockBarItem
	{
		internal DockBar Bar { get; set; }
		public DockItem Item { get; set; }
		internal int Size { get; set; }

		internal IDockBarItemControl Control { get; private set; }
		uint autoShowTimeout = uint.MaxValue;
		uint autoHideTimeout = uint.MaxValue;

		AutoHideBox autoShowFrame;
		AutoHideBox hiddenFrame;

		Gdk.Size lastFrameSize;

		public DockBarItem (DockBar bar, DockItem item, int size)
		{
			Bar = bar;
			Item = item;
			Size = size;

			Control = new DockBarItemControl ();
			Control.Initialize (this);
		}

		public void Close ()
		{
			UnscheduleAutoShow ();
			UnscheduleAutoHide ();
			AutoHide (false);
			Bar.RemoveItem (this);
			Control.Close ();
		}

		internal void UnscheduleAutoShow ()
		{
			if (autoShowTimeout != uint.MaxValue) {
				GLib.Source.Remove (autoShowTimeout);
				autoShowTimeout = uint.MaxValue;
			}
		}

		internal void UnscheduleAutoHide ()
		{
			if (autoHideTimeout != uint.MaxValue) {
				GLib.Source.Remove (autoHideTimeout);
				autoHideTimeout = uint.MaxValue;
			}
		}

		public void Present (bool giveFocus)
		{
			AutoShow ();
			if (giveFocus) {
				GLib.Timeout.Add (200, delegate {
					// Using a small delay because AutoShow uses an animation and setting focus may
					// not work until the item is visible
					if (autoShowFrame != null) {
						autoShowFrame.Present ();
					}

					Item.SetFocus ();
					ScheduleAutoHide (false);
					return false;
				});
			}
		}

		public void Minimize ()
		{
			AutoHide (false);
		}

		internal void AutoShow ()
		{
			UnscheduleAutoHide ();

			if (autoShowFrame == null && !Bar.Frame.Control.OverlayWidgetVisible) {
				if (hiddenFrame != null)
					Bar.Frame.Control.AutoHide (Item, hiddenFrame, false);
				autoShowFrame = Bar.Frame.Control.AutoShow (Item, Bar, Size);
				if (!string.IsNullOrEmpty (Item.Label)) {
					autoShowFrame.Title = Item.Label;
				}

				autoShowFrame.EnteredFrame += OnFrameEnter;
				autoShowFrame.ExitedFrame += OnFrameLeave;
				autoShowFrame.KeyPressed += OnFrameKeyPress;

				Control.QueueDraw ();
			}
		}

		internal void AutoHide (bool animate)
		{
			UnscheduleAutoShow ();

			if (autoShowFrame != null) {
				Size = autoShowFrame.PadSize;
				hiddenFrame = autoShowFrame;
				autoShowFrame.Hidden += delegate {
					hiddenFrame = null;
				};
				Bar.Frame.Control.AutoHide (Item, autoShowFrame, animate);
				autoShowFrame.EnteredFrame -= OnFrameEnter;
				autoShowFrame.ExitedFrame -= OnFrameLeave;
				autoShowFrame.KeyPressed -= OnFrameKeyPress;
				autoShowFrame = null;

				Control.QueueDraw ();
			}
		}

		void OnFrameEnter (object s, EventArgs args)
		{
			AutoShow ();
		}

		void OnFrameKeyPress (object s, KeyEventArgs args)
		{
			if (args.Key == Gdk.Key.Escape)
				ScheduleAutoHide (true, true);
		}

		void OnFrameLeave (object s, EventArgs args)
		{
			ScheduleAutoHide (true);
		}

		internal void ScheduleAutoShow ()
		{
			UnscheduleAutoHide ();
			if (autoShowTimeout == uint.MaxValue) {
				autoShowTimeout = GLib.Timeout.Add (Bar.Frame.AutoShowDelay, delegate {
					autoShowTimeout = uint.MaxValue;
					AutoShow ();
					return false;
				});
			}
		}

		internal void ScheduleAutoHide (bool cancelAutoShow)
		{
			ScheduleAutoHide (cancelAutoShow, false);
		}

		internal void ScheduleAutoHide (bool cancelAutoShow, bool force)
		{
			if (cancelAutoShow)
				UnscheduleAutoShow ();
			if (force)
				Item.ClearFocus ();

			if (autoHideTimeout == uint.MaxValue) {
				autoHideTimeout = GLib.Timeout.Add (force ? 0 : Bar.Frame.AutoHideDelay, delegate {
					// Don't hide if the context menu for the item is being shown.
					if (Item.ShowingContextMenu)
						return true;

					if (!force) {
						// Don't hide the item if it has the focus. Try again later.
						if (Item.HasFocusedChild && AutoShowWindowHasFocus)
							return true;
						// Don't hide the item if the mouse pointer is still inside the window. Try again later.
						if (Item.ContainsPointer)
							return true;

						// Don't hide if the mouse pointer is still inside the DockBar item
						if (Control.ContainsPointer) {
							return true;
						}
					}

					autoHideTimeout = uint.MaxValue;
					AutoHide (true);
					return false;
				});
			}
		}

		public bool AutoShowWindowHasFocus {
			get {
				return autoShowFrame != null && autoShowFrame.HasToplevelFocus;
			}
		}

		public void Present ()
		{
			if (autoShowFrame != null) {
				autoShowFrame.Present ();
			}
		}

		public void UpdateTab ()
		{
			Control.UpdateTab ();
		}

		public void Show ()
		{
			Control.Show ();
		}

		public void Hide ()
		{
			Control.Hide ();
		}

		internal void UpdateSize (Gdk.Size newSize)
		{
			if (!lastFrameSize.Equals (newSize)) {
				lastFrameSize = newSize;
				if (autoShowFrame != null)
					Bar.Frame.Control.UpdateSize (Bar, autoShowFrame);

				UnscheduleAutoHide ();
				AutoHide (false);
			}
		}

		internal void OnControlEntered ()
		{
			if (Bar.HoverActivationEnabled && autoShowFrame == null) {
				ScheduleAutoShow ();
				Control.QueueDraw ();
			}
		}

		internal void OnControlExited ()
		{
			ScheduleAutoHide (true);
			if (autoShowFrame == null) {
				Control.QueueDraw ();
			}
		}

		internal void OnControlHidden ()
		{
			UnscheduleAutoShow ();
			UnscheduleAutoHide ();
			AutoHide (false);

			Hidden?.Invoke (this, EventArgs.Empty);
		}

		internal void OnControlShown ()
		{
			Shown?.Invoke (this, EventArgs.Empty);
		}

		internal void OnControlPressed ()
		{
			if (autoShowFrame == null) {
				AutoShow ();
			} else {
				AutoHide (false);
			}
		}

		public event EventHandler<EventArgs> Shown;
		public event EventHandler<EventArgs> Hidden;
	}

	internal interface IDockBarItemControl : IAnimatable
	{
		void Initialize (DockBarItem parentItem);
		void Close ();
		void UpdateTab ();

		DockBarItem ParentItem { get; }
		void QueueDraw ();

		void Show ();
		void Hide ();

		bool ContainsPointer { get; }
	}

	class DockBarItemControl: EventBox, IDockBarItemControl
	{
		DockBarItem parentItem;
		Box box;
		Label label;
		Alignment mainBox;

		MouseTracker tracker;
		CrossfadeIcon crossfade;
		double hoverProgress;

		public DockBarItemControl ()
		{
			var actionHandler = new ActionDelegate (this);
			actionHandler.PerformPress += OnPerformPress;

			Events = Events | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
			CanFocus = true;
			VisibleWindow = false;
		}

		public void Initialize (DockBarItem parentItem)
		{
			this.parentItem = parentItem;

			UpdateTab ();

			var widgetControl = parentItem.Bar.Frame.Control as Widget;
			if (widgetControl != null) {
				parentItem.UpdateSize (widgetControl.Allocation.Size);
				widgetControl.SizeAllocated += HandleBarFrameSizeAllocated;
			} else {
				throw new ToolkitMismatchException ();
			}

			tracker = new MouseTracker (this);
			tracker.TrackMotion = false;
			tracker.HoveredChanged += (sender, e) => {

				if (crossfade == null)
					return;
	
				AnimateHover (tracker.Hovered);
				if (tracker.Hovered)
					crossfade.ShowSecondary ();
				else
					crossfade.ShowPrimary ();
			};

			Styles.Changed += UpdateStyle;

			Accessible.Name = "DockbarItem";
			Accessible.Role = Atk.Role.PushButton;
		}

		public DockBarItem ParentItem {
			get {
				return parentItem;
			}
		}

		public bool ContainsPointer {
			get {
				GetPointer (out int x, out int y);
				return Allocation.Contains (Allocation.X + x, Allocation.Y + y);
			}
		}

		public void Close ()
		{
			Destroy ();
		}

		void IAnimatable.BatchBegin () { }
		void IAnimatable.BatchCommit () { QueueDraw (); }

		void AnimateHover (bool hovered)
		{
			this.Animate ("Hover",
			              x => hoverProgress = x,
			              hoverProgress,
			              hovered ? 1.0f : 0.0f,
			              length: 100);
		}
		
		void HandleBarFrameSizeAllocated (object o, SizeAllocatedArgs args)
		{
			parentItem.UpdateSize (args.Allocation.Size);
		}
		
		protected override void OnDestroyed ()
		{
			this.AbortAnimation ("Hover");
			base.OnDestroyed ();
			var widgetControl = parentItem.Bar.Frame.Control as Widget;
			if (widgetControl != null) {
				widgetControl.SizeAllocated -= HandleBarFrameSizeAllocated;
			} else {
				throw new ToolkitMismatchException ();
			}
			Ide.Gui.Styles.Changed -= UpdateStyle;
		}
		
		public void UpdateTab ()
		{
			var bar = parentItem.Bar;
			var it = parentItem.Item;

			if (Child != null) {
				Widget w = Child;
				Remove (w);
				w.Destroy ();
			}
			
			mainBox = new Alignment (0,0,1,1);
			mainBox.Accessible.SetShouldIgnore (true);
			if (bar.Orientation == Gtk.Orientation.Horizontal) {
				box = new HBox ();
				if (bar.AlignToEnd)
					mainBox.SetPadding (5, 5, 11, 9);
				else
					mainBox.SetPadding (5, 5, 9, 11);
			}
			else {
				box = new VBox ();
				if (bar.AlignToEnd)
					mainBox.SetPadding (11, 9, 5, 5);
				else
					mainBox.SetPadding (9, 11, 5, 5);
			}
			box.Accessible.SetShouldIgnore (true);

			if (it.Icon != null) {
				var desat = it.Icon.WithAlpha (0.5);
				crossfade = new CrossfadeIcon (desat, it.Icon);
				crossfade.Accessible.SetShouldIgnore (true);
				box.PackStart (crossfade, false, false, 0);
				desat.Dispose ();
			}
				
			if (!string.IsNullOrEmpty (it.Label)) {
				label = new Label (it.Label);
				label.Accessible.SetShouldIgnore (true);
				label.UseMarkup = true;
				label.ModifyFont (FontService.SansFont.CopyModified (Styles.FontScale11));

				if (bar.Orientation == Orientation.Vertical)
					label.Angle = 270;

				// fine-tune label alignment issues
				if (Platform.IsMac) {
					if (bar.Orientation == Orientation.Horizontal)
						label.SetAlignment (0, 0.5f);
					else
						label.SetAlignment (0.6f, 0);
				} else {
					if (bar.Orientation == Orientation.Vertical)
						label.SetAlignment (1, 0);
				}
				// TODO: VV: Test Linux

				box.PackStart (label, true, true, 0);

				Accessible.SetLabel (it.Label);
				Accessible.SetTitle (it.Label);
				Accessible.Description = GettextCatalog.GetString ("Show the {0} pad", it.Label);
			} else
				label = null;

			box.Spacing = 2;
			mainBox.Add (box);
			mainBox.ShowAll ();
			Add (mainBox);
			UpdateStyle (this, null); 
			QueueDraw ();
		}

		void UpdateStyle (object sender, EventArgs e)
		{
			if (label != null)
				label.ModifyFg (StateType.Normal, Styles.DockBarLabelColor.ToGdkColor ());
		}

		protected override void OnHidden ()
		{
			base.OnHidden ();
			parentItem.OnControlHidden ();
		}

		protected override void OnShown()
		{
			base.OnShown();
			parentItem.OnControlShown ();
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			parentItem.OnControlEntered ();
			return base.OnEnterNotifyEvent (evnt);
		}

		void OnPerformPress (object sender, EventArgs args)
		{
			parentItem.OnControlPressed ();
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			parentItem.OnControlExited ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		bool itemActivated;

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			var bar = parentItem.Bar;
			var it = parentItem.Item;

			if (bar.Frame.Control.OverlayWidgetVisible)
				return false;
			if (evnt.TriggersContextMenu ()) {
				it.ShowDockPopupMenu (this, evnt);
			} else if (evnt.Button == 1) {
				if (evnt.Type == Gdk.EventType.TwoButtonPress) {
					// Instead of changing the state of the pad here, do it when the button is released.
					// Changing the state will make this bar item to vanish before the ReleaseEvent is received, and in this
					// case the ReleaseEvent may be fired on another widget that is taking the space of this bar item.
					// This was happening for example with the feedback button.
					itemActivated = true;
				} else {
					parentItem.AutoShow ();
					it.Present (true);
				}
			}
			return true;
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (itemActivated) {
				itemActivated = false;
				parentItem.Item.Status = DockItemStatus.Dockable;
			}
			return true;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			var bar = parentItem.Bar;
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				var alloc = Allocation;

				// TODO: VV: Remove preflight gradient features and replace with a flat color

				Cairo.LinearGradient lg;

				if (bar.Orientation == Orientation.Horizontal) {
					lg = new Cairo.LinearGradient (alloc.X, 0, alloc.X + alloc.Width, 0);
				} else {
					lg = new Cairo.LinearGradient (0, alloc.Y, 0, alloc.Y + alloc.Height);
				}

				using (lg) {
					Cairo.Color primaryColor = Styles.DockBarPrelightColor.ToCairoColor ();
					primaryColor.A = hoverProgress;

					Cairo.Color transparent = primaryColor;
					transparent.A = 0;

					lg.AddColorStop (0.0, transparent);
					lg.AddColorStop (0.35, primaryColor);
					lg.AddColorStop (0.65, primaryColor);
					lg.AddColorStop (1.0, transparent);

					context.Rectangle (alloc.ToCairoRect ());
					context.SetSource (lg);
				}
				context.Fill ();
			}

			if (HasFocus) {
				Gtk.Style.PaintFocus (Style, GdkWindow, State, Allocation, this, "button", Allocation.X + 2, Allocation.Y + 2, Allocation.Width - 4, Allocation.Height - 4);
			}
			return base.OnExposeEvent (evnt);
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			QueueDraw ();
			return base.OnFocusInEvent (evnt);
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			QueueDraw ();
			return base.OnFocusOutEvent (evnt);
		}

		protected override void OnActivate ()
		{
			parentItem.AutoShow ();
		}
	}
}
