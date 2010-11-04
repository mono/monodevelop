// AbstractViewContent.cs
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
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui
{
	public abstract class AbstractViewContent : AbstractBaseViewContent, IViewContent
	{
		#region IViewContent Members

		private string untitledName = "";
		public virtual string UntitledName {
			get { return untitledName; }
			set { untitledName = value; }
		}

		private string contentName;
		public virtual string ContentName {
			get { return contentName; }
			set {
				if (value != contentName) {
					contentName = value;
					OnContentNameChanged (EventArgs.Empty);
				}
			}
		}

		public bool IsUntitled {
			get { return (contentName == null); }
		}

		private bool isDirty;
		public virtual bool IsDirty {
			get { return isDirty; }
			set {
				if (value != isDirty) {
					isDirty = value;
					OnDirtyChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool IsReadOnly {
			get { return false; }
		}

		public virtual bool IsViewOnly { get; set; }

		public virtual bool IsFile {
			get { return true; }
		}

		public virtual string StockIconId {
			get { return null; }
		}

		public virtual Project Project { get; set; }

		public string PathRelativeToProject {
			get { return Project == null ? null : FileService.AbsoluteToRelativePath (Project.BaseDirectory, ContentName); }
		}

		public virtual void Save ()
		{
			OnBeforeSave (EventArgs.Empty);
			this.Save (contentName);
		}

		public virtual void Save (string fileName)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void DiscardChanges ()
		{
			
		}

		public abstract void Load (string fileName);


		public event EventHandler ContentNameChanged;

		public event EventHandler DirtyChanged;

		public event EventHandler BeforeSave;

		public event EventHandler ContentChanged;

		#endregion


		public virtual void OnContentChanged (EventArgs e)
		{
			if (ContentChanged != null)
				ContentChanged (this, e);
		}

		public virtual void OnDirtyChanged (EventArgs e)
		{
			if (DirtyChanged != null)
				DirtyChanged (this, e);
		}

		public virtual void OnBeforeSave (EventArgs e)
		{
			if (BeforeSave != null)
				BeforeSave (this, e);
		}

		public virtual void OnContentNameChanged (EventArgs e)
		{
			if (ContentNameChanged != null)
				ContentNameChanged (this, e);
		}
	}
}
