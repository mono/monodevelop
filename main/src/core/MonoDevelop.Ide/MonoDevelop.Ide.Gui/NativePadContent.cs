// NarivePadContent.cs
//
// Author:
//   Jose Medrano (josmed@microsoft.com)
//
// Copyright (c) Microsoft
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

#if MAC
using AppKit;
using MonoDevelop.Components;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.Ide.Gui
{
	public abstract class NativePadContent : PadContent
	{
		NSView container;
		Gtk.Widget widget;

		public override Control Control => widget;

		protected NativePadContent ()
		{
		}

		protected NativePadContent (string title, string icon = null) : base (title, icon)
		{
		}

		IPadWindow window;
		protected override void Initialize (IPadWindow window)
		{
			this.window = window;
			this.window.PadContentShown += OnPadContentShown;
			this.window.PadContentHidden += OnPadContentHidden;

			container = GetContentView (window);
			if (container == null) {
				throw new System.Exception ($"GetContentView cannot be null in a NativePadContent ({this.GetType ()})");
			}
			widget = GtkMacInterop.NSViewToGtkWidget (container);
			widget.CanFocus = true;
			widget.Sensitive = true;

			OnContentInitialized (widget);

			widget.ShowAll ();
		}

		protected virtual void OnContentInitialized (Gtk.Widget widget)
		{
			//to implement
		}

		void OnPadContentHidden (object sender, System.EventArgs e)
		{
			container.Hidden = true;
		}

		void OnPadContentShown (object sender, System.EventArgs e)
		{
			container.Hidden = false;
		}

		internal void ShowAll () => widget.ShowAll ();

		protected abstract NSView GetContentView (IPadWindow window);

		public override void Dispose ()
		{
			if (widget != null) {
			
				widget.Destroy ();
				widget.Dispose ();
				widget = null;
			}

			if (window != null) {
				window.PadContentShown -= OnPadContentShown;
				window.PadContentHidden -= OnPadContentHidden;
			}
			base.Dispose ();
		}
	}
}
#endif