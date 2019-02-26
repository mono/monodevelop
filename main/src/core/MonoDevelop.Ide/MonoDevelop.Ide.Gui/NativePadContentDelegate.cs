// NativePadContentDelegate.cs
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

using System;
using AppKit;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.Ide.Gui
{
	public abstract class NativePadContentDelegate : IPadContentDelegate
	{
		NSView container;

		Gtk.Widget widget;
		public Gtk.Widget Control => widget;

		protected NativePadContentDelegate ()
		{
		}

		IPadWindow window;
		public virtual void OnInitialize (IPadWindow window)
		{
			this.window = window;
			this.window.PadContentShown += OnPadContentShown;
			this.window.PadContentHidden += OnPadContentHidden;

			container = GetNativeContentView (window);
			if (container == null) {
				throw new Exception ($"GetContentView cannot be null in a NativePadContent ({this.GetType ()})");
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

		void OnPadContentHidden (object sender, EventArgs e)
		{
			container.Hidden = true;
		}

		void OnPadContentShown (object sender, EventArgs e)
		{
			container.Hidden = false;
		}

		internal void ShowAll () => widget.ShowAll ();

		protected abstract NSView GetNativeContentView (IPadWindow window);

		public virtual void Dispose ()
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
		}
	}
}
#endif