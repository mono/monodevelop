
using System;
using System.IO;
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public class Workbench
	{
		DocumentCollection documents = new DocumentCollection ();
		PadCollection pads;
		ProgressMonitorManager monitors = new ProgressMonitorManager ();
		DefaultWorkbench workbench;
		RecentOpen recentOpen = null;
		IStatusBarService statusBarService;
		
		public event EventHandler ActiveDocumentChanged;
		
		internal void Initialize ()
		{
			workbench = new DefaultWorkbench ();
			
			workbench.InitializeWorkspace();
			workbench.WorkbenchLayout = new SdiWorkbenchLayout ();
			workbench.UpdateViews(null, null);
			
			((Gtk.Window)workbench).Visible = false;
			workbench.ActiveWorkbenchWindowChanged += new EventHandler (OnDocumentChanged);
			Runtime.Properties.PropertyChanged += new PropertyEventHandler(TrackPropertyChanges);
			
			if (Services.DebuggingService != null) {
				Services.DebuggingService.PausedEvent += (EventHandler) Services.DispatchService.GuiDispatch (new EventHandler (OnDebuggerPaused));
			}
			
			Services.FileService.FileRemoved += (FileEventHandler) Services.DispatchService.GuiDispatch (new FileEventHandler (IdeApp.Workbench.RecentOpen.FileRemoved));
			Services.FileService.FileRenamed += (FileEventHandler) Services.DispatchService.GuiDispatch (new FileEventHandler (IdeApp.Workbench.RecentOpen.FileRenamed));
			
			pads = null;	// Make sure we get an up to date pad list.
		}
		
		/// <remarks>
		/// This method handles the redraw all event for specific changed IDE properties
		/// </remarks>
		void TrackPropertyChanges(object sender, MonoDevelop.Core.Properties.PropertyEventArgs e)
		{
			if (e.OldValue != e.NewValue) {
				switch (e.Key) {
					case "MonoDevelop.Core.Gui.VisualStyle":
					case "CoreProperties.UILanguage":
						workbench.RedrawAllComponents();
						break;
				}
			}
		}
		
		internal void Show (string workbenchMemento)
		{
			RootWindow.Show ();
			workbench.SetMemento ((IXmlConvertable)Runtime.Properties.GetProperty (workbenchMemento, workbench.CreateMemento()));
			RootWindow.Visible = true;
			workbench.RedrawAllComponents ();
			RootWindow.Present ();
		}
		
		internal void Close ()
		{
			((DefaultWorkbench)workbench).Close();
		}
		
		public RecentOpen RecentOpen {
			get {
				if (recentOpen == null)
					recentOpen = new RecentOpen ();
				return recentOpen;
			}
		}
		
		public DocumentCollection Documents {
			get { return documents; }
		}

		public Document ActiveDocument {
			get { return FindDocument (workbench.ActiveWorkbenchWindow); }
		}
		
		public PadCollection Pads {
			get {
				if (pads == null) {
					pads = new PadCollection ();
					foreach (IPadContent pc in workbench.PadContentCollection)
						WrapPad (pc);
				}
				return pads;
			}
		}
		
		public Gtk.Window RootWindow {
			get { return (Gtk.Window) workbench; }
		}
		
		public WorkbenchContext Context {
			get { return workbench.Context; }
			set {
				if (workbench.Context != value) {
					workbench.Context = value;
					pads = null;
				}
			}
		}
		
		public bool FullScreen {
			get { return workbench.FullScreen; }
			set { workbench.FullScreen = value; }
		}
		
		public string CurrentLayout {
			get { return workbench.WorkbenchLayout != null ? workbench.WorkbenchLayout.CurrentLayout : ""; }
			set { workbench.WorkbenchLayout.CurrentLayout = value; }
		}

		public string[] Layouts {
			get { return workbench.WorkbenchLayout != null ? workbench.WorkbenchLayout.Layouts : new string[0]; }
		}
		
		public ProgressMonitorManager ProgressMonitors {
			get { return monitors; }
		}
		
		public IStatusBarService StatusBar {
			get {
				if (statusBarService == null)
					statusBarService = (IStatusBarService) ServiceManager.GetService (typeof(IStatusBarService));
				return statusBarService;
			}
		}
		
		public void DeleteLayout (string name)
		{
			workbench.WorkbenchLayout.DeleteLayout (name);
		}
		
		public void SaveAll ()
		{
			foreach (Document doc in Documents)
				doc.Save ();
		}
		
		public void CloseAllDocuments ()
		{
			Document[] docs = new Document [Documents.Count];
			Documents.CopyTo (docs, 0);
			foreach (Document doc in docs)
				doc.Close ();
		}

		public Pad ShowPad (IPadContent content)
		{
			workbench.ShowPad (content);
			return WrapPad (content);
		}
		
		public Document OpenDocument (string fileName)
		{
			return OpenDocument (fileName, true);
		}
		
		public Document OpenDocument (string fileName, bool bringToFront)
		{
			return OpenDocument (fileName, -1, -1, bringToFront);
		}
		
		public Document OpenDocument (string fileName, int line, int column, bool bringToFront)
		{
			foreach (Document doc in Documents) {
				if (doc.FileName == fileName) {
					if (bringToFront)
						doc.Select ();
					if (line != -1 && doc.Window.ViewContent is IPositionable) {
						((IPositionable)doc.Window.ViewContent).JumpTo (line, column != -1 ? column : 0);
					}
					return doc;
				}
			}

			IProgressMonitor pm = ProgressMonitors.GetStatusProgressMonitor (string.Format (GettextCatalog.GetString ("Opening {0}"), fileName), Stock.OpenFileIcon, false);
			FileInformation openFileInfo = new FileInformation();
			openFileInfo.ProgressMonitor = pm;
			openFileInfo.FileName = fileName;
			openFileInfo.BringToFront = bringToFront;
			openFileInfo.Line = line;
			openFileInfo.Column = column;
			RealOpenFile (openFileInfo);
			
			if (!pm.AsyncOperation.Success)
				return null;
			
			return WrapDocument (openFileInfo.NewContent.WorkbenchWindow);
		}
		
		public Document OpenDocument (IViewContent content, bool bringToFront)
		{
			workbench.ShowView (content, bringToFront);
			return WrapDocument (content.WorkbenchWindow);
		}
		
		public Document NewDocument (string defaultName, string language, string content)
		{
			IDisplayBinding binding = Services.DisplayBindings.GetBindingPerLanguageName(language);
			IViewContent newContent;
			
			if (binding != null) {
				newContent = binding.CreateContentForLanguage(language, content, defaultName);
				if (newContent == null) {
					throw new ApplicationException(String.Format("Created view content was null{3}DefaultName:{0}{3}Language:{1}{3}Content:{2}", defaultName, language, content, Environment.NewLine));
				}
				newContent.UntitledName = defaultName;
				newContent.IsDirty      = false;
				workbench.ShowView(newContent, true);
				
				Services.DisplayBindings.AttachSubWindows(newContent.WorkbenchWindow);
			} else {
				throw new ApplicationException("Can't create display binding for language " + language);				
			}
			
			return WrapDocument (newContent.WorkbenchWindow);
		}
		
		void OnDocumentChanged (object s, EventArgs a)
		{
			if (ActiveDocumentChanged != null)
				ActiveDocumentChanged (s, a);
		}
		
		Document WrapDocument (IWorkbenchWindow window)
		{
			Document doc = new Document (window);
			window.Closing += new WorkbenchWindowEventHandler (OnWindowClosing);
			window.Closed += new EventHandler (OnWindowClosed);
			Documents.Add (doc);
			return doc;
		}
		
		Pad WrapPad (IPadContent padContent)
		{
			Pad pad = new Pad (workbench, padContent);
			Pads.Add (pad);
			return pad;
		}
		
		void OnWindowClosing (object sender, WorkbenchWindowEventArgs args)
		{
			IWorkbenchWindow window = (IWorkbenchWindow) sender;
			if (!args.Forced && window.ViewContent != null && window.ViewContent.IsDirty) {
				
				QuestionResponse response = Services.MessageService.AskQuestionWithCancel (GettextCatalog.GetString ("Do you want to save the current changes?"));
				
				if (response == QuestionResponse.Cancel) {
					args.Cancel = true;
					return;
				}

				if (response == QuestionResponse.Yes) {
					if (window.ViewContent.ContentName == null) {
						while (true) {
							FindDocument (window).Save ();
							if (window.ViewContent.IsDirty) {
								if (Services.MessageService.AskQuestion(GettextCatalog.GetString ("Do you really want to discard your changes ?"))) {
									break;
								}
							} else {
								break;
							}
						}
						
					} else {
						Runtime.FileUtilityService.ObservedSave(new FileOperationDelegate(window.ViewContent.Save), window.ViewContent.ContentName , FileErrorPolicy.ProvideAlternative);
					}
				}
			}
		}
		
		void OnWindowClosed (object sender, EventArgs args)
		{
			IWorkbenchWindow window = (IWorkbenchWindow) sender;
			Documents.Remove (FindDocument (window)); 
		}
		
		void RealOpenFile (object openFileInfo)
		{
			string fileName;
			FileInformation oFileInfo = openFileInfo as FileInformation;
			IProgressMonitor monitor = oFileInfo.ProgressMonitor;

			using (monitor)
			{
				fileName = oFileInfo.FileName;
				
				if (fileName == null) {
					monitor.ReportError (GettextCatalog.GetString ("Invalid file name"), null);
					return;
				}
	
				string origName = fileName;
	
				if (fileName.StartsWith ("file://"))
					fileName = fileName.Substring (7);
	
				if (!fileName.StartsWith ("http://"))
					fileName = System.IO.Path.GetFullPath (fileName);
				
				//Debug.Assert(Runtime.FileUtilityService.IsValidFileName(fileName));
				if (Runtime.FileUtilityService.IsDirectory (fileName)) {
					monitor.ReportError (string.Format (GettextCatalog.GetString ("{0} is a directory"), fileName), null);
					return;
				}
				// test, if file fileName exists
				if (!fileName.StartsWith("http://")) {
					// test, if an untitled file should be opened
					if (!System.IO.Path.IsPathRooted(origName)) { 
						foreach (Document doc in Documents) {
							if (doc.Window.ViewContent.IsUntitled && doc.Window.ViewContent.UntitledName == origName) {
								doc.Select ();
								oFileInfo.NewContent = doc.Window.ViewContent;
								return;
							}
						}
					} else 
					if (!File.Exists (fileName)) {
						monitor.ReportError (string.Format (GettextCatalog.GetString ("File not found: {0}"), fileName), null);
						return;
					}
				}
				
				foreach (Document doc in Documents) {
					if (doc.FileName == fileName) {
						if (oFileInfo.BringToFront) {
							doc.Select ();
							if (oFileInfo.Line != -1 && doc.Window.ViewContent is IPositionable) {
								((IPositionable)doc.Window.ViewContent).JumpTo (oFileInfo.Line, oFileInfo.Column != -1 ? oFileInfo.Column : 0);
							}
						}
						oFileInfo.NewContent = doc.Window.ViewContent;
						return;
					}
				}
				
				IDisplayBinding binding = Services.DisplayBindings.GetBindingPerFileName(fileName);
				
				if (binding != null) {
					Project project = null;
					Combine combine = null;
					GetProjectAndCombineFromFile (fileName, out project, out combine);
					
					if (combine != null && project != null)
					{
						if (Runtime.FileUtilityService.ObservedLoad(new NamedFileOperationDelegate(new LoadFileWrapper (workbench, binding, project, oFileInfo).Invoke), fileName) == FileOperationResult.OK) {
							RecentOpen.AddLastFile (fileName, project.Name);
						}
					}
					else
					{
						if (Runtime.FileUtilityService.ObservedLoad(new NamedFileOperationDelegate(new LoadFileWrapper (workbench, binding, null, oFileInfo).Invoke), fileName) == FileOperationResult.OK) {
							RecentOpen.AddLastFile (fileName, null);
						}
					}
				} else {
					try {
						// FIXME: this doesn't seem finished yet in Gtk#
						//MimeType mimetype = new MimeType (new Uri ("file://" + fileName));
						//if (mimetype != null) {
						//	mimetype.DefaultAction.Launch ();
						//} else {
							Gnome.Url.Show ("file://" + fileName);
						//}
					} catch {
						if (Runtime.FileUtilityService.ObservedLoad(new NamedFileOperationDelegate (new LoadFileWrapper (workbench, Services.DisplayBindings.LastBinding, null, oFileInfo).Invoke), fileName) == FileOperationResult.OK) {
							RecentOpen.AddLastFile (fileName, null);
						}
					}
				}
			}
		}
		
		void GetProjectAndCombineFromFile (string fileName, out Project project, out Combine combine)
		{
			combine = IdeApp.ProjectOperations.CurrentOpenCombine;
			project = null;
			
			if (combine != null)
			{
				foreach (Project projectaux in combine.GetAllProjects())
				{
					if (projectaux.IsFileInProject (fileName))
						project = projectaux;
				}
			}
		}
		
		void OnDebuggerPaused (object o, EventArgs e)
		{
			IDebuggingService dbgr = Services.DebuggingService;
			if (dbgr != null) {
				if (dbgr.CurrentFilename != String.Empty)
					IdeApp.Workbench.OpenDocument (dbgr.CurrentFilename);
			}
		}
		
		internal Document FindDocument (IWorkbenchWindow window)
		{
			foreach (Document doc in Documents)
				if (doc.Window == window)
					return doc;
			return null;
		}
		
		internal Pad FindPad (IPadContent padContent)
		{
			foreach (Pad pad in Pads)
				if (pad.Content == padContent)
					return pad;
			return null;
		}
	}
	
	class FileInformation
	{
		public IProgressMonitor ProgressMonitor;
		public string FileName;
		public bool BringToFront;
		public int Line;
		public int Column;
		public IViewContent NewContent;
	}
	
	class LoadFileWrapper
	{
		IDisplayBinding binding;
		Project project;
		FileInformation fileInfo;
		IWorkbench workbench;
		IViewContent newContent;
		
		public LoadFileWrapper (IWorkbench workbench, IDisplayBinding binding, FileInformation fileInfo)
		{
			this.workbench = workbench;
			this.fileInfo = fileInfo;
			this.binding = binding;
		}
		
		public LoadFileWrapper (IWorkbench workbench, IDisplayBinding binding, Project project, FileInformation fileInfo)
		{
			this.workbench = workbench;
			this.fileInfo = fileInfo;
			this.binding = binding;
			this.project = project;
		}
		
		public void Invoke(string fileName)
		{
			newContent = binding.CreateContentForFile(fileName);
			if (project != null)
			{
				newContent.HasProject = true;
				newContent.Project = project;
			}
			workbench.ShowView (newContent, fileInfo.BringToFront);
			Services.DisplayBindings.AttachSubWindows(newContent.WorkbenchWindow);
			
			if (fileInfo.Line != -1 && newContent is IPositionable) {
				GLib.Timeout.Add (10, new GLib.TimeoutHandler (JumpToLine));
			}
			fileInfo.NewContent = newContent;
		}
		
		public bool JumpToLine ()
		{
			((IPositionable)newContent).JumpTo (Math.Max(1, fileInfo.Line), Math.Max(1, fileInfo.Column));
			return false;
		}
	}
}
