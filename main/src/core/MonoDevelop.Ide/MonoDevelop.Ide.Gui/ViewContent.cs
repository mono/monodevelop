// ViewContent.cs
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
using MonoDevelop.Components;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.Ide.Gui
{
	public abstract class ViewContent : BaseViewContent
	{
		#region ViewContent Members

		string untitledName = "";
		string contentName;
		bool isDirty;

		public string UntitledName {
			get { return untitledName; }
			set { untitledName = value; }
		}

		public string ContentName {
			get { return contentName; }
			set {
				if (value != contentName) {
					contentName = value;
					OnContentNameChanged ();
				}
			}
		}

		public bool IsUntitled {
			get { return (ContentName == null); }
		}

		public virtual bool IsDirty {
			get { return isDirty; }
			set {
				if (value != isDirty) {
					isDirty = value;
					OnDirtyChanged ();
				}
			}
		}

		public virtual bool IsReadOnly {
			get { return false; }
		}

		public virtual bool IsHidden {
			get { return false; }
		}

		public virtual bool IsViewOnly {
			get { return false; }
		}

		public virtual bool IsFile {
			get { return true; }
		}

		public virtual object GetDocumentObject ()
		{
			string path = IsUntitled ? UntitledName : ContentName;
			if (IsFile && !string.IsNullOrEmpty (path) && Owner != null) {
					return Project.Files.GetFile (path);
			}
			return null;
		}

		public virtual string StockIconId {
			get { return null; }
		}

		internal string PathRelativeToProject {
			get { return Owner == null ? null : FileService.AbsoluteToRelativePath (Owner.BaseDirectory, ContentName); }
		}

		public virtual Task Save ()
		{
			return Save (ContentName);
		}
		
		public Task Save (FilePath fileName)
		{
			return Save (new FileSaveInformation (fileName)); 
		}
		
		public virtual Task Save (FileSaveInformation fileSaveInformation)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void DiscardChanges ()
		{
		}

		public virtual Task Load (FileOpenInformation fileOpenInformation)
		{
			return Task.FromResult (true);
		}
		
		public Task Load (FilePath fileName)
		{
			return Load (new FileOpenInformation (fileName, null));
		}
		
		public virtual Task LoadNew (System.IO.Stream content, string mimeType)
		{
			throw new NotSupportedException ();
		}

		internal event EventHandler ContentNameChanged;

		public event EventHandler DirtyChanged;

		#endregion


		protected virtual void OnDirtyChanged ()
		{
			if (DirtyChanged != null)
				DirtyChanged (this, EventArgs.Empty);
		}

		protected virtual void OnContentNameChanged ()
		{
			if (ContentNameChanged != null)
				ContentNameChanged (this, EventArgs.Empty);
		}
	}

	public abstract class AbstractXwtViewContent : ViewContent
	{
		public sealed override Control Control {
			get {
				return (Gtk.Widget)Toolkit.CurrentEngine.GetNativeWidget (Widget);
			}
		}

		public abstract Xwt.Widget Widget {
			get;
		}
	}
}
