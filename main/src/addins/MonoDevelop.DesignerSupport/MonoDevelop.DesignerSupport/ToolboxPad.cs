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
using MonoDevelop.Components.Mac;

namespace MonoDevelop.DesignerSupport
{
	public class ToolboxPad : PadContent
	{
		Gtk.Widget widget;

#if MAC
		Toolbox.MacToolbox toolbox;
#endif
		protected override void Initialize (IPadWindow container)
		{
			base.Initialize (container);
#if MAC
			var nativeEnabled = Environment.GetEnvironmentVariable ("NATIVE_TOOLBAR")?.ToLower () == "true";
			if (nativeEnabled) {

				toolbox = new Toolbox.MacToolbox (DesignerSupport.Service.ToolboxService, container);
				widget = GtkMacInterop.NSViewToGtkWidget (toolbox);
				widget.CanFocus = true;
				widget.Sensitive = true;
				widget.KeyPressEvent += toolbox.OnKeyPressed;
				widget.KeyReleaseEvent += toolbox.KeyReleased;

				widget.DragEnd += (o, args) => {
					isDragging = false;
				};

				widget.Focused += (s, e) => {
					toolbox.FocusSelectedView ();
				};

				toolbox.ContentFocused += (s, e) => {
					if (!widget.HasFocus) {
						widget.HasFocus = true;
						toolbox.FocusSelectedView ();
					}
				};
				toolbox.DragSourceSet += (s, e) => {
					Gtk.Drag.SourceUnset (widget);
					targets = new Gtk.TargetList ();
					targets.AddTable (e);
				};
				toolbox.DragBegin += (object sender, EventArgs e) => {
					if (!isDragging) {
						isDragging = true;
						Gtk.Drag.Begin (widget, targets, Gdk.DragAction.Copy | Gdk.DragAction.Move, 1, Gtk.Global.CurrentEvent ?? new Gdk.Event (IntPtr.Zero));
						DesignerSupport.Service.ToolboxService.DragSelectedItem (widget, null);
					}
				};

				widget.ShowAll ();
			} else {
#endif
				widget = new Toolbox.Toolbox (DesignerSupport.Service.ToolboxService, container);
#if MAC
			}
#endif
		}
#if MAC
		Gtk.TargetList targets = new Gtk.TargetList ();
		bool isDragging = false;
		public override void Dispose ()
		{
			widget.KeyPressEvent -= toolbox.OnKeyPressed;
			widget.KeyReleaseEvent -= toolbox.KeyReleased;
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
