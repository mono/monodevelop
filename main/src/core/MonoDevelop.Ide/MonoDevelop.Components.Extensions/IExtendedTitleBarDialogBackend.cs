//
// IExtendedTitleBarWindowBackend.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using Xwt.Backends;
using Xwt.GtkBackend;

namespace MonoDevelop.Components.Extensions
{
	public interface IExtendedTitleBarDialogBackend: IDialogBackend
	{
		void SetHeaderContent (IWidgetBackend backend);
	}

	class GtkExtendedTitleBarDialogBackend: DialogBackend, IExtendedTitleBarDialogBackend
	{
		HeaderBox toolbar;

		public override void Initialize ()
		{
			base.Initialize ();
			toolbar = new HeaderBox ();
			toolbar.GradientBackground = true;
			toolbar.SetMargins (0, 1, 0, 0);
			MainBox.PackStart (toolbar, false, false, 0);
			((Gtk.Box.BoxChild)MainBox [toolbar]).Position = 0;
		}

		public void SetHeaderContent (IWidgetBackend backend)
		{
			if (toolbar.Child != null) {
				WidgetBackend.RemoveChildPlacement (toolbar.Child);
				toolbar.Remove (toolbar.Child);
			}
			if (backend != null) {
				toolbar.Child = WidgetBackend.GetWidgetWithPlacement (backend);
				toolbar.Show ();
			} else {
				toolbar.Hide ();
			}
		}
	}
}

