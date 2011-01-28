// 
// ExtensionView.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.Addins.Description;
using MonoDevelop.Ide.Gui;
using System.IO;

namespace MonoDevelop.AddinAuthoring
{
	public abstract class ExtensionBaseView: IAttachableViewContent
	{
		IExtensionViewWidget editor;
		AddinDescription desc;
		AddinData data;
		bool changed;
		
		public ExtensionBaseView (AddinDescription desc, AddinData data)
		{
			this.desc = desc;
			this.data = data;
		}
		
		protected abstract IExtensionViewWidget CreateWidget ();

		#region IAttachableViewContent implementation
		public void Selected ()
		{
			string txt = WorkbenchWindow.Document.Editor.Text;
			try {
				desc = data.AddinRegistry.ReadAddinManifestFile (new StringReader (txt), WorkbenchWindow.Document.FileName);
				Control.Sensitive = true;
				editor.SetData (desc, data);
			} catch {
				desc = null;
				Control.Sensitive = false;
			}
			changed = false;
		}

		public void Deselected ()
		{
			if (changed)
				WorkbenchWindow.Document.Editor.Text = AddinAuthoringService.SaveFormattedXml (data.Project.Policies, desc);
		}

		public void BeforeSave ()
		{
			if (changed)
				WorkbenchWindow.Document.Editor.Text = AddinAuthoringService.SaveFormattedXml (data.Project.Policies, desc);
		}

		public void BaseContentChanged ()
		{
		}
		#endregion

		#region IBaseViewContent implementation
		public T GetContent<T> () where T:class
		{
			return null;
		}

		public bool CanReuseView (string fileName)
		{
			return false;
		}

		public void RedrawContent ()
		{
		}

		public IWorkbenchWindow WorkbenchWindow { get; set; }

		public Gtk.Widget Control {
			get {
				if (editor == null) {
					editor = CreateWidget ();
					editor.SetData (desc, data);
					editor.Changed += HandleEditorChanged;
				}
				return (Gtk.Widget) editor;
			}
		}

		void HandleEditorChanged (object sender, EventArgs e)
		{
			changed = true;
		}

		public abstract string TabPageLabel { get; }
		
		#endregion

		#region IDisposable implementation
		public virtual void Dispose ()
		{
		}
		#endregion
	}
	
	public interface IExtensionViewWidget
	{
		void SetData (AddinDescription desc, AddinData data);
		event EventHandler Changed;
	}
}

