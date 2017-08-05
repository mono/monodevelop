//
// WrapperDesignView.cs: base class for wrapping an ViewContent. Heavily based on 
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

using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonoDevelop.DesignerSupport
{
	
	public class WrapperDesignView : ViewContent
	{
		ViewContent content;
		Gtk.VBox contentBox;
		Gtk.Widget topBar;
		
		public WrapperDesignView  (ViewContent content)
		{
			this.content = content;
			this.contentBox = new Gtk.VBox ();
			this.contentBox.PackEnd (content.Control, true, true, 0);
			this.contentBox.ShowAll ();
			
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
		
		protected ViewContent Content {
			get { return content; }
		}
		
		protected override void OnSetProject (MonoDevelop.Projects.Project project)
		{
			base.OnSetProject (project);
			content.Project = project;
		}

		protected override void OnSetOwner (MonoDevelop.Projects.SolutionItem owner)
		{
			base.OnSetOwner (owner);
			content.Owner = owner;
		}

		public override ProjectReloadCapability ProjectReloadCapability {
			get {
				return content.ProjectReloadCapability;
			}
		}
		
		protected override void OnWorkbenchWindowChanged ()
		{
			base.OnWorkbenchWindowChanged ();
			content.WorkbenchWindow = WorkbenchWindow;
		}
		
		public override void Dispose ()
		{
			content.DirtyChanged -= new EventHandler (OnTextDirtyChanged);
			IdeApp.Workbench.ActiveDocumentChanged -= new EventHandler (OnActiveDocumentChanged);
			base.Dispose ();
		}
		
		public override Task Load (FileOpenInformation fileOpenInformation)
		{
			ContentName = fileOpenInformation.FileName;
			return content.Load (ContentName);
		}
		
		public override Task LoadNew (System.IO.Stream content, string mimeType)
		{
			return this.content.LoadNew (content, mimeType);
		}
		
		public override Control Control {
			get { return contentBox; }
		}
		
		public override Task Save (FileSaveInformation fileSaveInformation)
		{
			return content.Save (fileSaveInformation);
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
		
		protected override void OnContentNameChanged ()
		{
			base.OnContentNameChanged ();
			content.ContentName = ContentName;
		}

		void OnTextDirtyChanged (object s, EventArgs args)
		{
			OnDirtyChanged ();
		}
		
		void OnActiveDocumentChanged (object s, EventArgs args)
		{
			if (IdeApp.Workbench.ActiveDocument.GetContent<WrapperDesignView> () == this)
				OnDocumentActivated ();
		}
		
		
		protected virtual void OnDocumentActivated ()
		{
		}

		protected override IEnumerable<object> OnGetContents (Type type)
		{
			return base.OnGetContents (type).Concat (content.GetContents (type));
		}
	}
}
