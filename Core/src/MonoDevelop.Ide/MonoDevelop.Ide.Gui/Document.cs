
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
		}
		
		public string FileName {
			get { return Window.ViewContent.ContentName; }
			set { Window.ViewContent.ContentName = value; }
		}
		
		public bool IsDirty {
			get { return Window.ViewContent.ContentName == null || Window.ViewContent.IsDirty; }
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
						Runtime.FileUtilityService.ObservedSave (new NamedFileOperationDelegate(Window.ViewContent.Save), fileName + "~");
					}
					Runtime.FileUtilityService.ObservedSave (new NamedFileOperationDelegate(Window.ViewContent.Save), fileName);
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
				fdiag.SetFilename (Window.ViewContent.ContentName);
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
				Runtime.FileUtilityService.ObservedSave (new NamedFileOperationDelegate(Window.ViewContent.Save), filename + "~");
			}
			
			// do actual save
			if (Runtime.FileUtilityService.ObservedSave (new NamedFileOperationDelegate(Window.ViewContent.Save), filename) == FileOperationResult.OK) {
				IdeApp.Workbench.RecentOpen.AddLastFile (filename, null);
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
	}
}

