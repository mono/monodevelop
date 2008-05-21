//
// Document.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using Mono.Addins;

namespace MonoDevelop.Ide.Gui
{
	public class Document
	{
		IWorkbenchWindow window;
		TextEditorExtension editorExtension;
		bool editorChecked;
		TextEditor textEditor;
		
		internal IWorkbenchWindow Window {
			get { return window; }
		}
		
		public object GetContent (Type type)
		{
			//check whether the ViewContent can return the type directly
			object ret = Window.ViewContent.GetContent (type);
			if (ret != null)
				return ret;
			
			//no, so look through the TexteditorExtensions as well
			TextEditorExtension nextExtension = editorExtension;
			while (nextExtension != null) {
				if (type.IsAssignableFrom (nextExtension.GetType ()))
					return nextExtension;
				nextExtension = nextExtension.Next as TextEditorExtension;
			}
			return null;
		}
		
		public T GetContent <T>()
		{
			return (T) GetContent (typeof(T));
		}
		
		internal Document (IWorkbenchWindow window)
		{
			this.window = window;
			window.Closed += OnClosed;
			window.ActiveViewContentChanged += OnActiveViewContentChanged;
			IdeApp.Workspace.ItemRemovedFromSolution += OnEntryRemoved;
		}
		
		public string FileName {
			get { return Window.ViewContent.ContentName; }
			set { Window.ViewContent.ContentName = value; }
		}
		
		public bool IsDirty {
			get { return Window.ViewContent.ContentName == null || Window.ViewContent.IsDirty; }
			set { Window.ViewContent.IsDirty = value; }
		}
		
		public bool HasProject {
			get { return Window.ViewContent.Project != null; }
		}
		
		public Project Project {
			get { return Window.ViewContent.Project; }
		}
		
		public string PathRelativeToProject {
			get { return Window.ViewContent.PathRelativeToProject; }
		}
		
		public void Select ()
		{
			window.SelectWindow ();
		}
		
		public object ActiveView {
			get { return window.ActiveViewContent; }
		}
		
		public string UntitledName {
			get { return Window.ViewContent.UntitledName; }
			set { Window.ViewContent.UntitledName = value; }
		}
		
		public bool IsUntitled {
			get { return Window.ViewContent.IsUntitled; }
		}
		
		public string Title {
			get {
				IViewContent view = Window.ViewContent;
				return view.IsUntitled ? view.UntitledName : view.ContentName;
			}
		}
		
		public TextEditor TextEditor {
			get {
				if (!editorChecked) {
					editorChecked = true;
					textEditor = TextEditor.GetTextEditor (Window.ViewContent);
				}
				return textEditor;
			}
		}
		
		public bool IsViewOnly {
			get { return Window.ViewContent.IsViewOnly; }
		}
		
		public void Reload ()
		{
			ICustomXmlSerializer memento = null;
			IMementoCapable mc = GetContent<IMementoCapable> ();
			if (mc != null) {
				memento = mc.CreateMemento();
			}
			window.ViewContent.Load (window.ViewContent.ContentName);
			if (memento != null) {
				mc.SetMemento(memento);
			}
		}
		
		public virtual void Save ()
		{
			if (Window.ViewContent.IsViewOnly || !Window.ViewContent.IsDirty)
				return;

			if (!Window.ViewContent.IsFile) {
				Window.ViewContent.Save ();
				return;
			}
			
			if (Window.ViewContent.ContentName == null) {
				SaveAs ();
			} else {
				FileAttributes attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;

				if (!File.Exists (Window.ViewContent.ContentName) || (File.GetAttributes(window.ViewContent.ContentName) & attr) != 0) {
					SaveAs ();
				} else {						
					string fileName = Window.ViewContent.ContentName;
					// save backup first						
					if((bool) PropertyService.Get ("SharpDevelop.CreateBackupCopy", false)) {
						Window.ViewContent.Save (fileName + "~");
						FileService.NotifyFileChanged (fileName);
					}
					Window.ViewContent.Save (fileName);
					FileService.NotifyFileChanged (fileName);
					OnSaved (EventArgs.Empty);
				}
			}
		}
		
		public void SaveAs ()
		{
			SaveAs (null);
		}
		
		public void SaveAs (string filename)
		{
			if (Window.ViewContent.IsViewOnly || !Window.ViewContent.IsFile)
				return;

			ICustomizedCommands cmds = GetContent <ICustomizedCommands> ();
			if (cmds != null) {
				if (!cmds.SaveAsCommand()) {
					return;
				}
			}
			
			string encoding = null;
			
			IEncodedTextContent tbuffer = GetContent <IEncodedTextContent> ();
			if (tbuffer != null) {
				encoding = tbuffer.SourceEncoding;
				if (encoding == null)
					encoding = TextEncoding.DefaultEncoding;
			}
				
			if (filename == null) {
				FileSelectorDialog fdiag = new FileSelectorDialog (GettextCatalog.GetString ("Save as..."), Gtk.FileChooserAction.Save);
				try {
					if (Window.ViewContent.IsUntitled)
						fdiag.CurrentName = Window.ViewContent.UntitledName;
					else {
						fdiag.SetCurrentFolder (Path.GetDirectoryName (Window.ViewContent.ContentName));
						fdiag.CurrentName = Path.GetFileName (Window.ViewContent.ContentName);
					}
					fdiag.Encoding = encoding;
					fdiag.ShowEncodingSelector = (tbuffer != null);
					int response = fdiag.Run ();
					filename = fdiag.Filename;
					encoding = fdiag.Encoding;
					if (response != (int)Gtk.ResponseType.Ok)
						return;
				} finally {
					fdiag.Destroy ();
				}
			}
		
			if (!FileService.IsValidFileName (filename)) {
				MessageService.ShowMessage (GettextCatalog.GetString ("File name {0} is invalid", filename));
				return;
			}
			// detect preexisting file
			if(File.Exists(filename)){
				if(MessageService.Confirm (GettextCatalog.GetString ("File {0} already exists. Overwrite?", filename), AlertButton.OverwriteFile)) {
					return;
				}
			}
			
			// save backup first
			if((bool) PropertyService.Get ("SharpDevelop.CreateBackupCopy", false)) {
				if (tbuffer != null && encoding != null)
					tbuffer.Save (filename + "~", encoding);
				else
					Window.ViewContent.Save (filename + "~");
			}
			
			// do actual save
			if (tbuffer != null && encoding != null)
				tbuffer.Save (filename, encoding);
			else
				Window.ViewContent.Save (filename);

			FileService.NotifyFileChanged (filename);
			IdeApp.Workbench.RecentOpen.AddLastFile (filename, null);
			
			OnSaved (EventArgs.Empty);
		}
		
		public virtual bool IsBuildTarget
		{
			get
			{
				if (this.IsViewOnly)
					return false;
				if (Window.ViewContent.ContentName != null)
					return Services.ProjectService.CanCreateSingleFileProject(Window.ViewContent.ContentName);
				
				return false;
			}
		}
		
		public virtual IAsyncOperation Build ()
		{
			return IdeApp.ProjectOperations.BuildFile (Window.ViewContent.ContentName);
		}
		
		public virtual IAsyncOperation Rebuild ()
		{
			return Build ();
		}
		
		public virtual void Clean ()
		{
		}
		
		public virtual IAsyncOperation Run ()
		{
			return IdeApp.ProjectOperations.ExecuteFile (Window.ViewContent.ContentName);
		}
		
		public virtual IAsyncOperation Debug ()
		{
			return IdeApp.ProjectOperations.DebugFile (Window.ViewContent.ContentName);
		}
		
		public void Close ()
		{
			Window.CloseWindow (false, true, 0);
		}
		
		protected virtual void OnSaved (EventArgs args)
		{
			if (Saved != null)
				Saved (this, args);
		}
		
		void OnClosed (object s, EventArgs a)
		{
			if (window is SdiWorkspaceWindow)
				((SdiWorkspaceWindow)window).DetachFromPathedDocument ();
			window.Closed -= OnClosed;
			window.ActiveViewContentChanged -= OnActiveViewContentChanged;
			IdeApp.Workspace.ItemRemovedFromSolution -= OnEntryRemoved;
			OnClosed (a);
			
			while (editorExtension != null) {
				editorExtension.Dispose ();
				editorExtension = editorExtension.Next as TextEditorExtension;
			}
		}
		
		void OnActiveViewContentChanged (object s, EventArgs args)
		{
			OnViewChanged (args);
		}
		
		protected virtual void OnClosed (EventArgs args)
		{
			if (Closed != null)
				Closed (this, args);
		}
		
		protected virtual void OnViewChanged (EventArgs args)
		{
			if (ViewChanged != null)
				ViewChanged (this, args);
		}
		
		internal void OnDocumentAttached ()
		{
			IExtensibleTextEditor editor = GetContent<IExtensibleTextEditor> ();
			if (editor == null)
				return;
		
			// If the new document is a text editor, attach the extensions
			
			TextEditorExtension[] extensions = (TextEditorExtension[]) AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/TextEditorExtensions", typeof(TextEditorExtension), false);
			
			editorExtension = null;
			TextEditorExtension last = null;
			
			foreach (TextEditorExtension ext in extensions) {
				if (ext.ExtendsEditor (this, editor)) {
					if (editorExtension == null)
						editorExtension = ext;
					if (last != null)
						last.Next = ext;
					last = ext;
					ext.Initialize (this);
				}
			}
			if (editorExtension != null)
				last.Next = editor.AttachExtension (editorExtension);
			
			if (window is SdiWorkspaceWindow)
				((SdiWorkspaceWindow)window).AttachToPathedDocument (GetContent<MonoDevelop.Ide.Gui.Content.IPathedDocument> ());
		}
		
		internal object ExtendedCommandTargetChain {
			get { return editorExtension; }
		}
		
		void OnEntryRemoved (object sender, SolutionItemEventArgs args)
		{
			if (args.SolutionItem == window.ViewContent.Project)
				window.ViewContent.Project = null;
		}
		
		public event EventHandler Closed;
		public event EventHandler Saved;
		public event EventHandler ViewChanged;
	}
}

