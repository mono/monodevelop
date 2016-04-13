// IPadContent.cs
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
using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui
{
	public abstract class PadContent : IDisposable
	{
		IPadWindow window;
		string icon;
		string title;

		protected PadContent (string title, string icon = null): this()
		{
			this.icon = icon;
			this.title = title;
		}

		protected PadContent ()
		{
			Id = GetType ().FullName;
		}

		public virtual string Id { get; set; }

		public IPadWindow Window {
			get { return window; }
		}

		public abstract Control Control { get; }

		internal void Init (IPadWindow window)
		{
			this.window = window;

			if (title != null)
				window.Title = title;

			if (icon != null)
				window.Icon = icon;
			
			Initialize (window);
		}

		protected virtual void Initialize (IPadWindow window)
		{
		}

		public virtual void Dispose ()
		{
			Control?.Dispose ();
		}
	}
}
