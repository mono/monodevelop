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
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Gui
{
	public class Document
	{
		IWorkbenchWindow window;
		
		internal IWorkbenchWindow Window {
			get { return window; }
		}
		
		public object Content {
			get { return Window.ViewContent; }
		}
		
		internal Document (IWorkbenchWindow window)
		{
			this.window = window;
			window.Closed += new EventHandler (OnClosed);
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
			get { return Window.ViewContent.HasProject; }
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
		
/*		public void JumpTo (int line, int column)
		{
			IViewContent content = Window.ViewContent;
			if (content is IPositionable) {
				((IPositionable)content).JumpTo (line, column);
			}
		}
*/
		public virtual void Save ()
		{
			if (!Window.ViewContent.IsDirty)
				return;

			if (Window.ViewContent.ContentName == null) {
				SaveAs ();
			} else {
				FileAttributes attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;

				if (!File.Exists (Window.ViewContent.ContentName) || (File.GetAttributes(window.ViewContent.ContentName) & attr) != 0) {
					SaveAs ();
				} else {						
					string fileName = Window.ViewContent.ContentName;
					// save backup first						
					if((bool) Runtime.Properties.GetProperty ("SharpDevelop.CreateBackupCopy", false)) {
						Window.ViewContent.Save (fileName + "~");
					}
					Window.ViewContent.Save (fileName);
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
			if (Window.ViewContent is ICustomizedCommands) {
				if (((ICustomizedCommands)window.ViewContent).SaveAsCommand()) {
					return;
				}
			}
			
			if (filename == null) {
				FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Save as..."), Gtk.FileChooserAction.Save);
				fdiag.CurrentName = Window.ViewContent.UntitledName;
				int response = fdiag.Run ();
				filename = fdiag.Filename;
				fdiag.Hide ();
				if (response != (int)Gtk.ResponseType.Ok)
					return;
			}
		
			if (!Runtime.FileUtilityService.IsValidFileName (filename)) {
				Services.MessageService.ShowMessage(String.Format (GettextCatalog.GetString ("File name {0} is invalid"), filename));
				return;
			}
			// detect preexisting file
			if(File.Exists(filename)){
				if(!Services.MessageService.AskQuestion(String.Format (GettextCatalog.GetString ("File {0} already exists.  Overwrite?"), filename))){
					return;
				}
			}
			// save backup first
			if((bool) Runtime.Properties.GetProperty ("SharpDevelop.CreateBackupCopy", false)) {
				Window.ViewContent.Save (filename + "~");
			}
			
			// do actual save
			Window.ViewContent.Save (filename);
			IdeApp.Workbench.RecentOpen.AddLastFile (filename, null);
			
			OnSaved (EventArgs.Empty);
		}
		
		public virtual bool IsBuildTarget
		{
			get
			{
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
			OnClosed (a);
		}
		
		protected virtual void OnClosed (EventArgs args)
		{
			if (Closed != null)
				Closed (this, args);
		}
		
		public event EventHandler Closed;
		public event EventHandler Saved;
	}
}

