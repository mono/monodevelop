//
// DockNotebookTab.cs
//
// Author:
//       Jose Medrano <jose.medrano@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corp. (http://microsoft.com)
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
using System.Linq;

using Gtk;
using Xwt.Motion;

using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using System.Collections.Generic;
using Cairo;
using MonoDevelop.Ide.Gui;
using Gdk;

namespace MonoDevelop.Components
{
	abstract class CanvasElement 
	{
		internal EventHandler NeedsRedraw;

		internal EventHandler<HandledMotionEventArgs> MouseMove;
		internal EventHandler MouseLeave;
		internal EventHandler MouseEnter;
		internal EventHandler<HandledButtonEventArgs> MouseButtonPress;
		internal EventHandler<HandledButtonEventArgs> MouseButtonRelease;

		internal EventHandler<HandledButtonEventArgs> PopupEvent;
		internal EventHandler<HandledButtonEventArgs> RaiseDragEvent;

		public abstract Gdk.Rectangle Allocation { get; set; }

		public bool IsHovered { get; set; }

		internal List<CanvasElement> Children = new List<CanvasElement> ();

		internal void OnInternalButtonReleaseEvent (HandledButtonEventArgs evnt)
		{
			foreach (var itm in Children) {
				itm.OnInternalButtonReleaseEvent (evnt);
				if (evnt.Handled)
					return;
			}

			if (Allocation.Contains ((int)evnt.X, (int)evnt.Y)) {
				if (!IsHovered)
					OnMouseEnter (EventArgs.Empty);
				OnButtonReleaseEvent (this, EventArgs.Empty);
				MouseButtonRelease?.Invoke (this, evnt);
			} else {
				OnMouseLeave (EventArgs.Empty);
			};
		}

		protected void OnNeedsRedraw ()
		{
			NeedsRedraw?.Invoke (this, EventArgs.Empty);
		}

		protected virtual void OnButtonReleaseEvent (object sender, EventArgs evnt)
		{

		}

		internal void OnInternalButtonPressEvent (HandledButtonEventArgs evnt)
		{
			if (Allocation.Contains ((int)evnt.X, (int)evnt.Y)) {
				foreach (var itm in Children) {
					itm.OnInternalButtonPressEvent (evnt);
					if (evnt.Handled)
						return;
				}

				if (!IsHovered)
					OnMouseEnter (EventArgs.Empty);
				OnButtonPressEvent (this, EventArgs.Empty);
				MouseButtonPress?.Invoke (this, evnt);
			} else {
				OnMouseLeave (EventArgs.Empty);
			};
		}

		protected virtual void OnButtonPressEvent (object sender, EventArgs evnt) 
		{
			
		}

		internal virtual bool OnMouseLeave (EventArgs args)
		{
			IsHovered = false;
			foreach (var item in Children) {
				item.OnMouseLeave (args);
			}
			MouseLeave?.Invoke (this, args);
			return false;
		}

		internal void OnInternalPopupEvent (HandledButtonEventArgs args)
		{
			PopupEvent?.Invoke (this, args);
		}

		internal void OnInternalMotionNotifyEvent (HandledMotionEventArgs args)
		{
			if (Allocation.Contains (args.X, args.Y)) {

				foreach (var itm in Children) {
					itm.OnInternalMotionNotifyEvent (args);
					if (args.Handled)
						return;
				}

				if (!IsHovered)
					OnMouseEnter (EventArgs.Empty);
				OnMotionNotifyEvent (this, args);
				MouseMove?.Invoke (this, args);
			} else {
				OnMouseLeave (EventArgs.Empty);
			};
		}

		protected virtual void OnMotionNotifyEvent (object sender, HandledMotionEventArgs args)
		{

		}

		internal virtual bool OnMouseEnter (EventArgs args)
		{
			if (!IsHovered) {
				IsHovered = true;
				MouseEnter?.Invoke (this, args);
			}
			return false;
		}

		internal void OnRaiseDragEvent (HandledButtonEventArgs args)
		{
			if (Allocation.Contains ((int)args.X, (int)args.Y)) {
				foreach (var itm in Children) {
					itm.OnRaiseDragEvent (args);
					if (args.Handled)
						return;
				}

				RaiseDragEvent?.Invoke (this, args);

			} else {
				OnMouseLeave (EventArgs.Empty);
			};
		}
	}
}
