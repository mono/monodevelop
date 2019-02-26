//
// MacToolboxPadDelegate.cs
//
// Authors:
//  Jose Medrano <josmed@microsoft.com>
//
// Copyright (C) 2018 Microsoft Corp
//
//
// This source code is licenced under The MIT License:
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
#if MAC

using System;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.DesignerSupport
{
	class MacToolboxPadDelegate : NativePadContentDelegate, IPadContentDelegate
	{
		Widget widget;

		Toolbox.MacToolbox toolbox;

		protected override void OnContentInitialized (Widget widget)
		{
			this.widget = widget;

			widget.KeyPressEvent += toolbox.OnKeyPressed;
			widget.KeyReleaseEvent += toolbox.OnKeyReleased;

			widget.DragBegin += Widget_DragBegin;
			widget.DragEnd += Widget_DragEnd;
			widget.Focused += Widget_Focused;
		}

		void Widget_Focused (object o, FocusedArgs args)
		{
			toolbox.FocusSelectedView ();
		}

		void Widget_DragEnd (object o, DragEndArgs args)
		{
			isDragging = false;
		}

		void Widget_DragBegin (object o, DragBeginArgs args)
		{
			if (!isDragging) {
				DesignerSupport.Service.ToolboxService.DragSelectedItem (widget, args.Context);
				isDragging = true;
			}
		}

		protected override AppKit.NSView GetNativeContentView (IPadWindow window)
		{
			toolbox = new Toolbox.MacToolbox (DesignerSupport.Service.ToolboxService, window);
			toolbox.ContentFocused += Toolbox_ContentFocused;
			toolbox.DragSourceSet += Toolbox_DragSourceSet;
			toolbox.DragBegin += Toolbox_DragBegin;

			return toolbox;
		}

		void Toolbox_DragBegin (object sender, EventArgs e)
		{
			var selectedNode = toolbox.SelectedNode;
			if (!isDragging && selectedNode != null) {

				DesignerSupport.Service.ToolboxService.SelectItem (selectedNode);

				Drag.SourceUnset (widget);

				// Gtk.Application.CurrentEvent and other copied gdk_events seem to have a problem
				// when used as they use gdk_event_copy which seems to crash on de-allocating the private slice.
				var currentEvent = GtkWorkarounds.GetCurrentEventHandle ();
				Drag.Begin (widget, targets, Gdk.DragAction.Copy | Gdk.DragAction.Move, 1, new Gdk.Event (currentEvent, false));

				// gtk_drag_begin does not store the event, so we're okay
				GtkWorkarounds.FreeEvent (currentEvent);

			}
		}

		void Toolbox_DragSourceSet (object sender, TargetEntry [] e)
		{
			targets = new TargetList ();
			targets.AddTable (e);
		}

		void Toolbox_ContentFocused (object sender, EventArgs e)
		{
			if (!widget.HasFocus) {
				widget.HasFocus = true;
				toolbox.FocusSelectedView ();
			}
		}

		TargetList targets = new TargetList ();
		bool isDragging;

		public override void Dispose ()
		{
			if (widget != null) {
				widget.KeyPressEvent -= toolbox.OnKeyPressed;
				widget.KeyReleaseEvent -= toolbox.OnKeyReleased;
				widget.DragBegin -= Widget_DragBegin;
				widget.DragEnd -= Widget_DragEnd;
				widget.Focused -= Widget_Focused;
				//the base class disposes the object
				widget = null;
			}

			if (toolbox != null) {
				toolbox.ContentFocused -= Toolbox_ContentFocused;
				toolbox.DragSourceSet -= Toolbox_DragSourceSet;
				toolbox.DragBegin -= Toolbox_DragBegin;
				toolbox.Dispose ();
				toolbox = null;
			}
			base.Dispose ();
		}

	}
}

#endif