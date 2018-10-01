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

namespace MonoDevelop.DesignerSupport
{
	public class ToolboxPad : PadContent
	{
		Toolbox.MacToolbox toolbox;
		Gtk.Widget widget;

		public ToolboxPad ()
		{
		}
		
		protected override void Initialize (IPadWindow container)
		{
			base.Initialize (container);

			Xwt.Toolkit.Load (Xwt.ToolkitType.XamMac).Invoke (() => {
				toolbox = new Toolbox.MacToolbox (DesignerSupport.Service.ToolboxService, container);
			});

			var wd = Xwt.Toolkit.CurrentEngine.WrapWidget (toolbox, NativeWidgetSizing.DefaultPreferredSize);
			widget = (Gtk.Widget)Xwt.Toolkit.CurrentEngine.GetNativeWidget (wd);

			toolbox.DragSourceUnset += (s, e) => {	
				Gtk.Drag.SourceUnset (widget);
			};

			toolbox.DragSourceSet += (s, e) => {
				Gtk.Drag.SourceSet (widget, Gdk.ModifierType.Button1Mask, e, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			};

			toolbox.DragBegin += (object sender, EventArgs e) => {
				Gtk.Application.Invoke ((s,ev) => {
					DesignerSupport.Service.ToolboxService.DragSelectedItem (widget, null);
				});

			};
		}
		
		#region AbstractPadContent implementations
		
		public override Control Control {
			get { return widget; }
		}
		
		#endregion
	}
}
