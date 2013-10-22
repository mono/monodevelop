// DebugValueWindow.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Linq;
using Mono.Debugging.Client;
using MonoDevelop.Debugger;
using MonoDevelop.Components;
using Gtk;
using Mono.TextEditor;
using Gdk;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor
{
	public class BaseWindow : Gtk.Window
	{
		public BaseWindow () : base(Gtk.WindowType.Toplevel)
		{
			this.SkipPagerHint = true;
			this.SkipTaskbarHint = true;
			this.Decorated = false;
			this.BorderWidth = 2;
			//HACK: this should be WindowTypeHint.Tooltip, but GTK on mac is buggy and doesn't allow keyboard
			//input to WindowType.Toplevel windows with WindowTypeHint.Tooltip hint
			this.TypeHint = WindowTypeHint.PopupMenu;
			this.AllowShrink = false;
			this.AllowGrow = false;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			int winWidth, winHeight;
			this.GetSize (out winWidth, out winHeight);
			evnt.Window.DrawRectangle (Style.BaseGC (StateType.Normal), true, 0, 0, winWidth - 1, winHeight - 1);
			evnt.Window.DrawRectangle (Style.MidGC (StateType.Normal), false, 0, 0, winWidth - 1, winHeight - 1);
			foreach (var child in this.Children)
				this.PropagateExpose (child, evnt);
			return false;
		}
	}
	
	public class DebugValueWindow : PopoverWindow
	{
		ObjectValueTreeView tree;
		ScrolledWindow sw;
//		PinWindow pinWindow;
//		TreeIter currentPinIter;
		
		public DebugValueWindow (Mono.TextEditor.TextEditor editor, int offset, StackFrame frame, ObjectValue value, PinnedWatch watch): base (Gtk.WindowType.Toplevel)
		{
			this.TypeHint = WindowTypeHint.PopupMenu;
			this.AllowShrink = false;
			this.AllowGrow = false;
			this.Decorated = false;

			TransientFor = (Gtk.Window) editor.Toplevel;
			
			// Avoid getting the focus when the window is shown. We'll get it when the mouse enters the window
			AcceptFocus = false;
			
			sw = new ScrolledWindow ();
			sw.HscrollbarPolicy = PolicyType.Never;
			sw.VscrollbarPolicy = PolicyType.Never;
			
			tree = new ObjectValueTreeView ();
			sw.Add (tree);
			ContentBox.Add (sw);
			
			tree.Frame = frame;
			tree.CompactView = true;
			tree.AllowAdding = false;
			tree.AllowEditing = true;
			tree.HeadersVisible = false;
			tree.AllowPinning = true;
			tree.RootPinAlwaysVisible = true;
			tree.PinnedWatch = watch;
			DocumentLocation location = editor.Document.OffsetToLocation (offset);
			tree.PinnedWatchLine = location.Line;
			tree.PinnedWatchFile = ((ExtensibleTextEditor)editor).View.ContentName;
			
			tree.AddValue (value);
			tree.Selection.UnselectAll ();
			tree.SizeAllocated += OnTreeSizeChanged;
			tree.PinStatusChanged += delegate {
				Destroy ();
			};
			
//			tree.MotionNotifyEvent += HandleTreeMotionNotifyEvent;
			
			sw.ShowAll ();
			
//			pinWindow = new PinWindow (this);
//			pinWindow.SetPinned (false);
//			pinWindow.ButtonPressEvent += HandlePinWindowButtonPressEvent;
			
			tree.StartEditing += delegate {
				Modal = true;
			};
			
			tree.EndEditing += delegate {
				Modal = false;
			};

			ShowArrow = true;
			Theme.CornerRadius = 3;
		}

//		void HandlePinWindowButtonPressEvent (object o, ButtonPressEventArgs args)
//		{
//			tree.CreatePinnedWatch (currentPinIter);
//		}
		
//		[GLib.ConnectBefore]
//		void HandleTreeMotionNotifyEvent (object o, MotionNotifyEventArgs args)
//		{
//			PlacePinWindow ();
//		}
//		
//		protected override void OnSizeAllocated (Rectangle allocation)
//		{
//			base.OnSizeAllocated (allocation);
//			PlacePinWindow ();
//		}
//		
//		void PlacePinWindow ()
//		{
//			int mx, my;
//			ModifierType mm;
//			if (tree.BinWindow == null)
//				return;
//			tree.BinWindow.GetPointer (out mx, out my, out mm);
//			
//			int cx, cy;
//			TreePath path;
//			TreeViewColumn col;
//			if (!tree.GetPathAtPos (mx, my, out path, out col, out cx, out cy))
//				return;
//			
//			tree.Model.GetIter (out currentPinIter, path);
//			Rectangle cr = tree.GetCellArea (path, tree.Columns [1]);
//		
//			int ox, oy;
//			tree.BinWindow.GetOrigin (out ox, out oy);
//			
//			if (mx < cr.Right - 30) {
//				pinWindow.Hide ();
//				return;
//			}
//			
//			int x, y, w, h;
//			GetPosition (out x, out y);
//			GetSize (out w, out h);
//			pinWindow.Move (x + w, oy + cr.Y);
//			pinWindow.Show ();
//		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			if (!AcceptFocus)
				AcceptFocus = true;
			return base.OnEnterNotifyEvent (evnt);
		}

		void OnTreeSizeChanged (object s, SizeAllocatedArgs a)
		{
			int x,y,w,h;
			GetPosition (out x, out y);
			h = (int) sw.Vadjustment.Upper;
			w = (int) sw.Hadjustment.Upper;
			int dy = y + h - this.Screen.Height;
			int dx = x + w - this.Screen.Width;
			
			if (dy > 0 && sw.VscrollbarPolicy == PolicyType.Never) {
				sw.VscrollbarPolicy = PolicyType.Always;
				sw.HeightRequest = h - dy - 10;
			} else if (sw.VscrollbarPolicy == PolicyType.Always && sw.Vadjustment.Upper == sw.Vadjustment.PageSize) {
				sw.VscrollbarPolicy = PolicyType.Never;
				sw.HeightRequest = -1;
			}
			
			if (dx > 0 && sw.HscrollbarPolicy == PolicyType.Never) {
				sw.HscrollbarPolicy = PolicyType.Always;
				sw.WidthRequest = w - dx - 10;
			} else if (sw.HscrollbarPolicy == PolicyType.Always && sw.Hadjustment.Upper == sw.Hadjustment.PageSize) {
				sw.HscrollbarPolicy = PolicyType.Never;
				sw.WidthRequest = -1;
			}
			// Force a redraw of the whole window. This is a workaround for bug 7538
			QueueDraw ();
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (MonoDevelop.Core.Platform.IsMac || MonoDevelop.Core.Platform.IsWindows) {
				// fails on linux see: Bug 8481 - Debug value tooltips very often appear at the top-left corner of the screen instead of near the element to inspect 
				const int edgeGap = 2;
				int oldY, x, y;
				
				this.GetPosition (out x, out y);
				oldY = y;
				
				Gdk.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtPoint (x, y));
				if (allocation.Height <= geometry.Height && y + allocation.Height >= geometry.Y + geometry.Height - edgeGap)
					y = geometry.Top + (geometry.Height - allocation.Height - edgeGap);
				if (y < geometry.Top + edgeGap)
					y = geometry.Top + edgeGap;
				
				if (y != oldY) {
					Move (x, y);
					// If the window is moved, hide the arrow since it will be pointing to the wrong place
					ShowArrow = false;
				}
			}
			base.OnSizeAllocated (allocation);
		}
	}
	
	
	// This class shows the pin button, to be used to pin a watch value
	// This window is used instead of the pin support in ObjectValueTreeView
	// to avoid some flickering caused by some weird gtk# behavior when scrolling
	// (see bug #632215).
	
	class PinWindow: BaseWindow
	{
		Gtk.Image icon;
		
		public PinWindow (Gtk.Window parent)
		{
			Events |= EventMask.ButtonPressMask;
			TransientFor = parent;
			DestroyWithParent = true;
			
			icon = new Gtk.Image ();
			Add (icon);
			icon.ShowAll ();
			AcceptFocus = false;
		}
		
		public void SetPinned (bool pinned)
		{
			if (pinned)
				icon.Pixbuf = ImageService.GetPixbuf ("md-pin-down", IconSize.Menu);
			else
				icon.Pixbuf = ImageService.GetPixbuf ("md-pin-up", IconSize.Menu);
		}
	}
}
