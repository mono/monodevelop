// AbstractPadContent.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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

using System;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	public abstract class AbstractPadContent : IPadContent
	{
		protected AbstractPadContent () : this (null, null)
		{
		}

		public AbstractPadContent (string title) : this (title, null)
		{
		}

		private IconId icon;
		private string title;
		public AbstractPadContent (string title, IconId icon)
		{
			this.Id = GetType ().FullName;
			this.icon = icon;
			this.title = title;
		}

		public string Id { get; set; }

		private IPadWindow window = null;
		public IPadWindow Window {
			get { return window; }
		}

		#region IPadContent Members

		public virtual void Initialize (IPadWindow container)
		{
			if (title != null)
				container.Title = title;

			if (icon != IconId.Null)
				container.Icon = icon;

			window = container;
		}

		public abstract Gtk.Widget Control {
			get;
		}

		public virtual void RedrawContent ()
		{
		}

		#endregion

		#region IDisposable Members

		public virtual void Dispose ()
		{
		}

		#endregion
	}
}
