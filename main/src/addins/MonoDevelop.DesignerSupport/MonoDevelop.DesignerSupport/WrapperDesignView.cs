//
// WrapperDesignView.cs: base class for wrapping an IViewContent. Heavily based on 
//         MonoDevelop.GtkCore.GuiBuilder.CombinedDesignView
//
// Author:
//   Michael Hutchinson
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Michael Hutchinson
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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

using System;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;

namespace MonoDevelop.DesignerSupport
{
	
	public class WrapperDesignView : AbstractViewContent
	{
		IViewContent content;
		Gtk.VBox contentBox;
		Gtk.Widget topBar;
		
		public WrapperDesignView  (IViewContent content)
		{
			this.content = content;
			this.contentBox = new Gtk.VBox ();
			this.contentBox.PackEnd (content.Control, true, true, 0);
			this.contentBox.ShowAll ();
			
			content.ContentChanged += new EventHandler (OnTextContentChanged);
			content.DirtyChanged += new EventHandler (OnTextDirtyChanged);
			
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (OnActiveDocumentChanged);
		}
		
		public override string TabPageLabel {
			get {
				return content.TabPageLabel;
			}
		}
		
		public Gtk.Widget TopBar {
			get {
				return topBar;
			}
			protected set {				
				if (topBar != null)
					contentBox.Remove (topBar);
				
				if (value != null)		
					contentBox.PackStart (value, false, false, 0);
				topBar = value;
			}
		}
		
		protected IViewContent Content {
			get { return content; }
		}
		
		public override MonoDevelop.Projects.Project Project {
			get { return base.Project; }
			set { 
				base.Project = value; 
				content.Project = value; 
			}
		}
		
		protected override void OnWorkbenchWindowChanged (EventArgs e)
		{
			base.OnWorkbenchWindowChanged (e);
			content.WorkbenchWindow = WorkbenchWindow;
		}
		
		public override void Dispose ()
		{
			content.ContentChanged -= new EventHandler (OnTextContentChanged);
			content.DirtyChanged -= new EventHandler (OnTextDirtyChanged);
			IdeApp.Workbench.ActiveDocumentChanged -= new EventHandler (OnActiveDocumentChanged);
			base.Dispose ();
		}
		
		public override void Load (string fileName)
		{
			ContentName = fileName;
			content.Load (fileName);
		}
		
		public override void LoadNew (System.IO.Stream content, string mimeType)
		{
			this.content.LoadNew (content, mimeType);
		}
		
		public override Gtk.Widget Control {
			get { return contentBox; }
		}
		
		public override void Save (string fileName)
		{
			content.Save (fileName);
		}
		
		public override bool IsDirty {
			get {
				return content.IsDirty;
			}
			set {
				content.IsDirty = value;
			}
		}
		
		public override bool IsReadOnly
		{
			get {
				return content.IsReadOnly;
			}
		}
		
		public override string ContentName {
			get { return content.ContentName; }
			set { content.ContentName = value; }
		}
				
		void OnTextContentChanged (object s, EventArgs args)
		{
			OnContentChanged (args);
		}
		
		void OnTextDirtyChanged (object s, EventArgs args)
		{
			OnDirtyChanged (args);
		}
		
		void OnActiveDocumentChanged (object s, EventArgs args)
		{
			if (IdeApp.Workbench.ActiveDocument.GetContent<WrapperDesignView> () == this)
				OnDocumentActivated ();
		}
		
		
		protected virtual void OnDocumentActivated ()
		{
		}
		
		public override object GetContent (Type type)
		{
			return base.GetContent (type) ?? content.GetContent (type);
		}
	}
}
