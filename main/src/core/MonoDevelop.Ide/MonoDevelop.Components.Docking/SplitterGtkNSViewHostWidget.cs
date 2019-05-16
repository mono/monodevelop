// SplitterContainerWidget.cs
//
// Author:
//   Jose Medrano
//

//
// Copyright (C) 2019 Microsoft
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

using Gdk;
using Gtk;

namespace MonoDevelop.Components.Docking
{
	sealed class SplitterMacHostWidget : ISplitterWidget
	{
		readonly GtkNSViewHost host;
		readonly MacSplitterWidget view;

		public SplitterMacHostWidget ()
		{
			view = new MacSplitterWidget ();
			host = new GtkNSViewHost (view);
		}

		public void Init (DockGroup grp, int index)
		{
			view.Init (grp, index);
		}

		public Widget Parent => host.Parent;

		public bool Visible {
			get => host.Visible;
			set => host.Visible = value;
		}

		public Widget Widget => host;

		public void SizeAllocate (Rectangle rect)
		{
			host.SizeAllocate (rect);
		}

		public void Hide ()
		{
			host.Hide ();
		}

		public void Show ()
		{
			host.Show ();
		}

	}
}

#endif
