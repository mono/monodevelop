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
		Gtk.Widget widget;
		Widget xWidget;
		Toolbox.MacToolbox toolbox;

		protected override void Initialize (IPadWindow container)
		{
			base.Initialize (container);

			var nativeEnabled = Environment.GetEnvironmentVariable ("NATIVE_TOOLBAR")?.ToLower () == "true";
			if (nativeEnabled) {

				Xwt.Toolkit.Load (Xwt.ToolkitType.XamMac).Invoke (() => {
					toolbox = new Toolbox.MacToolbox (DesignerSupport.Service.ToolboxService, container);
				});

				xWidget = Xwt.Toolkit.CurrentEngine.WrapWidget (toolbox, NativeWidgetSizing.DefaultPreferredSize);
				widget = (Gtk.Widget)Xwt.Toolkit.CurrentEngine.GetNativeWidget (xWidget);

				xWidget.CanGetFocus = true;
				xWidget.Sensitive = true;
				xWidget.KeyPressed += toolbox.OnKeyPressed;
				xWidget.KeyReleased += toolbox.KeyReleased;

				xWidget.GotFocus += (s, e) => {
					toolbox.FocusSelectedView ();
				};

				toolbox.ContentFocused += (s, e) => {
					if (!xWidget.HasFocus) {
						xWidget.SetFocus ();
					}
				};
			} else {
				widget = new Toolbox.Toolbox (DesignerSupport.Service.ToolboxService, container);
			}
		}

		public override void Dispose ()
		{
			if (xWidget != null) {
				xWidget.KeyPressed += toolbox.OnKeyPressed;
				xWidget.KeyReleased += toolbox.KeyReleased;
			}
			base.Dispose ();
		}

		#region AbstractPadContent implementations
		
		public override Control Control {
			get { return widget; }
		}
		
		#endregion
	}
}
