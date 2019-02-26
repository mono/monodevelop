//
// ToolboxPad.cs: The pad that hold the MD toolbox.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
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
using System;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using Xwt;
using Gtk;
#if MAC
using MonoDevelop.Components.Mac;
#endif

namespace MonoDevelop.DesignerSupport
{
	public class ToolboxPad : PadContent
	{
		Gtk.Widget widget;

#if MAC
		Toolbox.MacToolbox toolbox;
		IPadWindow window;
#endif
		protected override void Initialize (IPadWindow window)
		{
			base.Initialize (window);
#if MAC
			this.window = window;
			toolbox = new Toolbox.MacToolbox (DesignerSupport.Service.ToolboxService, window);
			widget = GtkMacInterop.NSViewToGtkWidget (toolbox);
			widget.CanFocus = true;
			widget.Sensitive = true;
			widget.KeyPressEvent += toolbox.OnKeyPressed;
			widget.KeyReleaseEvent += toolbox.KeyReleased;

			widget.DragBegin += Widget_DragBegin;
			widget.DragEnd += Widget_DragEnd;
			widget.Focused += Widget_Focused;

			this.window.PadContentShown += Container_PadContentShown;
			this.window.PadContentHidden += Container_PadContentHidden;

			toolbox.ContentFocused += Toolbox_ContentFocused;
			toolbox.DragSourceSet += Toolbox_DragSourceSet;
			toolbox.DragBegin += Toolbox_DragBegin;

			widget.ShowAll ();
#else
			widget = new Toolbox.Toolbox (DesignerSupport.Service.ToolboxService, window);
#endif
		}

#if MAC

		void Container_PadContentShown (object sender, EventArgs args) => toolbox.Hidden = false;
		void Container_PadContentHidden (object sender, EventArgs args) => toolbox.Hidden = true;

		private void Widget_DragEnd (object o, DragEndArgs args)
		{
			isDragging = false;
		}

		void Widget_Focused (object sender, EventArgs args)
		{
			toolbox.FocusSelectedView();
		}

		void Widget_DragBegin (object sender, DragBeginArgs args)
		{
			if (!isDragging) {
				DesignerSupport.Service.ToolboxService.DragSelectedItem (widget, args.Context);
				isDragging = true;
			}
		}

		void Toolbox_DragSourceSet (object sender, Gtk.TargetEntry [] e)
		{
			targets = new Gtk.TargetList ();
			targets.AddTable (e);
		}

		void Toolbox_ContentFocused (object sender, EventArgs args)
		{
			if (!widget.HasFocus) {
				widget.HasFocus = true;
				toolbox.FocusSelectedView ();
			}
		}

		void Toolbox_DragBegin (object sender, EventArgs args)
		{
			var selectedNode = toolbox.SelectedNode;
			if (!isDragging && selectedNode != null) {

				DesignerSupport.Service.ToolboxService.SelectItem (selectedNode);

				Gtk.Drag.SourceUnset (widget);

				// Gtk.Application.CurrentEvent and other copied gdk_events seem to have a problem
				// when used as they use gdk_event_copy which seems to crash on de-allocating the private slice.
				IntPtr currentEvent = GtkWorkarounds.GetCurrentEventHandle ();
				Gtk.Drag.Begin (widget, targets, Gdk.DragAction.Copy | Gdk.DragAction.Move, 1, new Gdk.Event (currentEvent, false));

				// gtk_drag_begin does not store the event, so we're okay
				GtkWorkarounds.FreeEvent (currentEvent);
			}
		}

		Gtk.TargetList targets = new Gtk.TargetList ();
		bool isDragging;

		public override void Dispose ()
		{
			if (window != null) {
				window.PadContentShown -= Container_PadContentShown;
				window.PadContentHidden -= Container_PadContentHidden;
				window = null;
			}

			if (widget != null) {
				widget.DragBegin -= Widget_DragBegin;
				widget.DragEnd -= Widget_DragEnd;
				widget.Focused -= Widget_Focused;
				widget.KeyPressEvent -= toolbox.OnKeyPressed;
				widget.KeyReleaseEvent -= toolbox.KeyReleased;
				widget.Destroy ();
				widget.Dispose ();
				widget = null;
			}
			if (toolbox != null) {
				toolbox.ContentFocused -= Toolbox_ContentFocused;
				toolbox.DragBegin -= Toolbox_DragBegin;
				toolbox.DragSourceSet -= Toolbox_DragSourceSet;
				toolbox.Dispose ();
				toolbox = null;
			}
			base.Dispose ();
		}
#endif

#region AbstractPadContent implementations
		
		public override Control Control {
			get { return widget; }
		}
		
#endregion
	}
}
