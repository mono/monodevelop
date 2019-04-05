//
// Dialog.cs
//
// Author:
//       therzok <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 therzok
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

namespace MonoDevelop.Components
{
	public class Dialog : Window
	{
		protected Dialog ()
		{
		}

		Dialog (object widget) : base (widget)
		{
		}

		public static implicit operator Gtk.Dialog (Dialog d)
		{
			return d?.GetNativeWidget<Gtk.Dialog> ();
		}

		public static implicit operator Dialog (Gtk.Dialog d)
		{
			if (d == null)
				return null;

			var dialog = GetImplicit<Dialog, Gtk.Dialog>(d);
			if (dialog == null) {
				dialog = new Dialog (d);
				d.Destroyed += delegate {
					GC.SuppressFinalize (dialog);
					dialog.Dispose (true);
				};
			}
			return dialog;
		}
	}
}

