// AbstractBaseViewContent.cs
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
	public abstract class AbstractBaseViewContent : IBaseViewContent
	{
		#region IBaseViewContent Members

		public abstract Gtk.Widget Control {
			get;
		}

		private IWorkbenchWindow workbenchWindow = null;
		public virtual IWorkbenchWindow WorkbenchWindow {
			get { return workbenchWindow; }
			set {
				if (workbenchWindow != value) {
					workbenchWindow = value;
					OnWorkbenchWindowChanged (EventArgs.Empty);
				}
			}
		}

		public virtual string TabPageLabel {
			get { return "Abstract Content"; }
		}

		public virtual void RedrawContent ()
		{
		}

		public virtual bool CanReuseView (string fileName)
		{
			return false;
		}

		public virtual object GetContent (Type contentType)
		{
			return contentType.IsInstanceOfType (this) ? this : null;
		}

		#endregion

		#region IDisposable Members

		public virtual void Dispose ()
		{
			if (Control != null) {
				Control.Dispose ();
			}
		}

		#endregion

		public event EventHandler WorkbenchWindowChanged;
		protected virtual void OnWorkbenchWindowChanged (EventArgs e)
		{
			if (WorkbenchWindowChanged != null) {
				WorkbenchWindowChanged (this, e);
			}
		}
	}
}
