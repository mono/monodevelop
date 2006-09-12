// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ?Â¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Xml;

using MonoDevelop.Projects;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core.Gui.Dialogs;

using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui.Dialogs;

using GLib;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the a Workspace with a multiple document interface.
	/// </summary>
	internal class DefaultWorkbench : Gtk.Window, IWorkbench
	{
		readonly static string mainMenuPath    = "/SharpDevelop/Workbench/MainMenu";
		readonly static string viewContentPath = "/SharpDevelop/Workbench/Pads";
		readonly static string toolbarsPath = "/SharpDevelop/Workbench/ToolBar";
		
		PadContentCollection viewContentCollection       = new PadContentCollection();
		ViewContentCollection workbenchContentCollection = new ViewContentCollection();
		
		bool closeAll = false;

		bool fullscreen;
		Rectangle normalBounds = new Rectangle(0, 0, 640, 480);
		
		private IWorkbenchLayout layout = null;

		internal static GType gtype;
		
		public Gtk.MenuBar TopMenu = null;
		private Gtk.Toolbar[] toolbars = null;
		
		enum TargetList {
			UriList = 100
		}

		Gtk.TargetEntry[] targetEntryTypes = new Gtk.TargetEntry[] {
			new Gtk.TargetEntry ("text/uri-list", 0, (uint)TargetList.UriList)
		};
		
		public bool FullScreen {
			get {
				return fullscreen;
			}
			set {
				fullscreen = value;
				if (fullscreen) {
					this.Fullscreen ();
				} else {
					this.Unfullscreen ();
				}
			}
		}
		
		/*
		public string Title {
			get {
				return Text;
			}
			set {
				Text = value;
			}
		}*/
		
		EventHandler windowChangeEventHandler;
		
		public IWorkbenchLayout WorkbenchLayout {
			get {
				//FIXME: i added this, we need to fix this shit
				//				if (layout == null) {
				//	layout = new SdiWorkbenchLayout ();
				//	layout.Attach(this);
				//}
				return layout;
			}
		}
		
		public PadContentCollection PadContentCollection {
			get {
				Debug.Assert(viewContentCollection != null);
				return viewContentCollection;
			}
		}
		
		public ViewContentCollection ViewContentCollection {
			get {
				Debug.Assert(workbenchContentCollection != null);
				return workbenchContentCollection;
			}
		}
		
		public IWorkbenchWindow ActiveWorkbenchWindow {
			get {
				if (layout == null) {
					return null;
				}
				return layout.ActiveWorkbenchwindow;
			}
		}

		public DefaultWorkbench() : base (Gtk.WindowType.Toplevel)
		{
			Title = "MonoDevelop";
			Runtime.LoggingService.Info ("Creating DefaultWorkbench");
		
			windowChangeEventHandler = new EventHandler(OnActiveWindowChanged);

			WidthRequest = normalBounds.Width;
			HeightRequest = normalBounds.Height;

			DeleteEvent += new Gtk.DeleteEventHandler (OnClosing);
			this.Icon = Services.Resources.GetBitmap ("md-sharp-develop-icon");
			//this.WindowPosition = Gtk.WindowPosition.None;

			Gtk.Drag.DestSet (this, Gtk.DestDefaults.Motion | Gtk.DestDefaults.Highlight | Gtk.DestDefaults.Drop, targetEntryTypes, Gdk.DragAction.Copy);
			DragDataReceived += new Gtk.DragDataReceivedHandler (onDragDataRec);
			
			IdeApp.CommandService.SetRootWindow (this);
		}

		void onDragDataRec (object o, Gtk.DragDataReceivedArgs args)
		{
			if (args.Info != (uint) TargetList.UriList)
				return;
			string fullData = System.Text.Encoding.UTF8.GetString (args.SelectionData.Data);

			foreach (string individualFile in fullData.Split ('\n')) {
				string file = individualFile.Trim ();
				if (file.StartsWith ("file://")) {
					file = new Uri(file).LocalPath;

					try {
						if (Services.ProjectService.IsCombineEntryFile (file))
							IdeApp.ProjectOperations.OpenCombine(file);
						else
							IdeApp.Workbench.OpenDocument (file);
					} catch (Exception e) {
						Runtime.LoggingService.ErrorFormat ("unable to open file {0} exception was :\n{1}", file, e.ToString());
					}
				}
			}
		}
		
		public void InitializeWorkspace()
		{
			// FIXME: GTKize
			IdeApp.ProjectOperations.CurrentProjectChanged += (ProjectEventHandler) Services.DispatchService.GuiDispatch (new ProjectEventHandler(SetProjectTitle));

			Services.FileService.FileRemoved += (FileEventHandler) Services.DispatchService.GuiDispatch (new FileEventHandler(CheckRemovedFile));
			Services.FileService.FileRenamed += (FileEventHandler) Services.DispatchService.GuiDispatch (new FileEventHandler(CheckRenamedFile));
			
//			TopMenu.Selected   += new CommandHandler(OnTopMenuSelected);
//			TopMenu.Deselected += new CommandHandler(OnTopMenuDeselected);

			TopMenu = IdeApp.CommandService.CreateMenuBar (mainMenuPath);
			toolbars = IdeApp.CommandService.CreateToolbarSet (toolbarsPath);
			foreach (Gtk.Toolbar t in toolbars)
				t.ToolbarStyle = Gtk.ToolbarStyle.Icons;
				
			Runtime.AddInService.ExtensionChanged += OnExtensionChanged;
		}
		
		void OnExtensionChanged (string path)
		{
			bool changed = false;
			
			if (path.StartsWith (mainMenuPath)) {
				TopMenu = IdeApp.CommandService.CreateMenuBar (mainMenuPath);
				changed = true;
			}
			
			if (path.StartsWith (toolbarsPath)) {
				toolbars = IdeApp.CommandService.CreateToolbarSet (toolbarsPath);
				foreach (Gtk.Toolbar t in toolbars)
					t.ToolbarStyle = Gtk.ToolbarStyle.Icons;
				changed = true;
			}
			
			if (changed && layout != null)
				layout.RedrawAllComponents();
		}
				
		public void CloseContent(IViewContent content)
		{
			if (Runtime.Properties.GetProperty("SharpDevelop.LoadDocumentProperties", true) && content is IMementoCapable) {
				StoreMemento(content);
			}
			if (workbenchContentCollection.Contains(content)) {
				workbenchContentCollection.Remove(content);
			}
		}
		
		public void CloseAllViews()
		{
			try {
				closeAll = true;
				ViewContentCollection fullList = new ViewContentCollection(workbenchContentCollection);
				foreach (IViewContent content in fullList) {
					IWorkbenchWindow window = content.WorkbenchWindow;
					window.CloseWindow(true, true, 0);
				}
			} finally {
				closeAll = false;
				OnActiveWindowChanged(null, null);
			}
		}
		
		public virtual void ShowView (IViewContent content, bool bringToFront)
		{
			Debug.Assert(layout != null);
			ViewContentCollection.Add(content);
			if (Runtime.Properties.GetProperty("SharpDevelop.LoadDocumentProperties", true) && content is IMementoCapable) {
				try {
					IXmlConvertable memento = GetStoredMemento(content);
					if (memento != null) {
						((IMementoCapable)content).SetMemento(memento);
					}
				} catch (Exception e) {
					Runtime.LoggingService.Error ("Can't get/set memento : " + e.ToString());
				}
			}
			
			layout.ShowView(content);
			
			if (bringToFront)
				content.WorkbenchWindow.SelectWindow();

			RedrawAllComponents ();
			
			if (content is IEditableTextBuffer) {
				((IEditableTextBuffer)content).TextChanged += new EventHandler (OnViewTextChanged);
			}
		}
		
		public virtual void ShowPad (IPadContent content)
		{
			PadContentCollection.Add(content);
			
			if (layout != null)
				layout.ShowPad (content);
		}
		
		public virtual void BringToFront (IPadContent content)
		{
			if (!layout.IsVisible (content))
				layout.ShowPad (content);

			layout.ActivatePad (content);
		}
		
		public void RedrawAllComponents()
		{
			foreach (IViewContent content in workbenchContentCollection) {
				content.RedrawContent();
			}
			foreach (IPadContent content in viewContentCollection) {
				content.RedrawContent();
			}
			layout.RedrawAllComponents();
			//statusBarManager.RedrawStatusbar();
		}
		
		public IXmlConvertable GetStoredMemento(IViewContent content)
		{
			if (content != null && content.ContentName != null) {
				string directory = Runtime.Properties.ConfigDirectory + "temp";
				if (!Directory.Exists(directory)) {
					Directory.CreateDirectory(directory);
				}
				string fileName = content.ContentName.Substring(3).Replace('/', '.').Replace('\\', '.').Replace(System.IO.Path.DirectorySeparatorChar, '.');
				string fullFileName = directory + System.IO.Path.DirectorySeparatorChar + fileName;
				// check the file name length because it could be more than the maximum length of a file name
				if (Runtime.FileUtilityService.IsValidFileName(fullFileName) && File.Exists(fullFileName)) {
					IXmlConvertable prototype = ((IMementoCapable)content).CreateMemento();
					XmlDocument doc = new XmlDocument();
					doc.Load (File.OpenRead (fullFileName));
					
					return (IXmlConvertable)prototype.FromXmlElement((XmlElement)doc.DocumentElement.ChildNodes[0]);
				}
			}
			return null;
		}
		
		public void StoreMemento(IViewContent content)
		{
			if (content.ContentName == null) {
				return;
			}
			string directory = Runtime.Properties.ConfigDirectory + "temp";
			if (!Directory.Exists(directory)) {
				Directory.CreateDirectory(directory);
			}
			
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version=\"1.0\"?>\n<Mementoable/>");
			
			XmlAttribute fileAttribute = doc.CreateAttribute("file");
			fileAttribute.InnerText = content.ContentName;
			doc.DocumentElement.Attributes.Append(fileAttribute);
			
			IXmlConvertable memento = ((IMementoCapable)content).CreateMemento();
			
			doc.DocumentElement.AppendChild(memento.ToXmlElement(doc));
			
			string fileName = content.ContentName.Substring(3).Replace('/', '.').Replace('\\', '.').Replace(System.IO.Path.DirectorySeparatorChar, '.');
			// check the file name length because it could be more than the maximum length of a file name
			string fullFileName = directory + System.IO.Path.DirectorySeparatorChar + fileName;
			if (Runtime.FileUtilityService.IsValidFileName(fullFileName)) {
				doc.Save (fullFileName);
			}
		}
		
		// interface IMementoCapable
		public IXmlConvertable CreateMemento()
		{
			WorkbenchMemento memento   = new WorkbenchMemento();
			int x, y, width, height;
			GetPosition (out x, out y);
			GetSize (out width, out height);
			if (GdkWindow.State == 0) {
				memento.Bounds = new Rectangle (x, y, width, height);
			} else {
				memento.Bounds = normalBounds;
			}
			memento.WindowState = GdkWindow.State;

			memento.FullScreen  = fullscreen;
			if (layout != null)
				memento.LayoutMemento = layout.CreateMemento ();
			return memento;
		}
		
		public void SetMemento(IXmlConvertable xmlMemento)
		{
			if (xmlMemento != null) {
				WorkbenchMemento memento = (WorkbenchMemento)xmlMemento;
				
				normalBounds = memento.Bounds;
				Move (normalBounds.X, normalBounds.Y);
				Resize (normalBounds.Width, normalBounds.Height);
				if (memento.WindowState == Gdk.WindowState.Maximized) {
					Maximize ();
				} else if (memento.WindowState == Gdk.WindowState.Iconified) {
					Iconify ();
				}
				//GdkWindow.State = memento.WindowState;
				FullScreen = memento.FullScreen;

				if (layout != null && memento.LayoutMemento != null)
					layout.SetMemento (memento.LayoutMemento);
			}
			Decorated = true;
		}
		
		protected /*override*/ void OnResize(EventArgs e)
		{
			// FIXME: GTKize
			
		}
		
		protected /*override*/ void OnLocationChanged(EventArgs e)
		{
			// FIXME: GTKize
		}
		
		void CheckRemovedFile(object sender, FileEventArgs e)
		{
			if (e.IsDirectory) {
				foreach (IViewContent content in ViewContentCollection) {
					if (content.ContentName.StartsWith(e.FileName)) {
						content.WorkbenchWindow.CloseWindow(true, true, 0);
					}
				}
			} else {
				foreach (IViewContent content in ViewContentCollection) {
					if (content.ContentName != null &&
					    content.ContentName == e.FileName) {
						content.WorkbenchWindow.CloseWindow(true, true, 0);
						return;
					}
				}
			}
		}
		
		void CheckRenamedFile(object sender, FileEventArgs e)
		{
			if (e.IsDirectory) {
				foreach (IViewContent content in ViewContentCollection) {
					if (content.ContentName.StartsWith(e.SourceFile)) {
						content.ContentName = e.TargetFile + content.ContentName.Substring(e.SourceFile.Length);
					}
				}
			} else {
				foreach (IViewContent content in ViewContentCollection) {
					if (content.ContentName != null &&
					    content.ContentName == e.SourceFile) {
						content.ContentName = e.TargetFile;
						return;
					}
				}
			}
		}
		
		protected /*override*/ void OnClosing(object o, Gtk.DeleteEventArgs e)
		{
			if (Close()) {
				Gtk.Application.Quit ();
			} else {
				e.RetVal = true;
			}
		}
		
		protected /*override*/ void OnClosed(EventArgs e)
		{
			layout.Detach();
			foreach (IPadContent content in PadContentCollection) {
				content.Dispose();
			}
		}
		
		public bool Close() 
		{
			IdeApp.ProjectOperations.SaveCombinePreferences ();

			bool showDirtyDialog = false;

			foreach (IViewContent content in ViewContentCollection)
			{
				if (content.IsDirty) {
					showDirtyDialog = true;
					break;
				}
			}

			if (showDirtyDialog) {
				DirtyFilesDialog dlg = new DirtyFilesDialog ();
				int response = dlg.Run ();
				if (response != (int)Gtk.ResponseType.Ok)
					return false;
			}
			
			IdeApp.ProjectOperations.CloseCombine (false);
			Runtime.Properties.SetProperty("SharpDevelop.Workbench.WorkbenchMemento", CreateMemento());
			OnClosed (null);
			return true;
		}
		
		void SetProjectTitle(object sender, ProjectEventArgs e)
		{
			if (e.Project != null) {
				Title = String.Concat(e.Project.Name, " - ", "MonoDevelop");
			} else {
				Title = "MonoDevelop";
			}
		}
		
		void OnActiveWindowChanged(object sender, EventArgs e)
		{
			if (!closeAll && ActiveWorkbenchWindowChanged != null) {
				ActiveWorkbenchWindowChanged(this, e);
			}
		}
		
		bool parsingFile;
		
		void OnViewTextChanged (object sender, EventArgs e)
		{
			if (!parsingFile) {
				parsingFile = true;
				GLib.Timeout.Add (500, new TimeoutHandler (ParseCurrentFile));
			}
		}
		
		bool ParseCurrentFile ()
		{
			parsingFile = false;
			
			if (ActiveWorkbenchWindow == null || ActiveWorkbenchWindow.ActiveViewContent == null)
				return false;

			IEditableTextBuffer editable = ActiveWorkbenchWindow.ActiveViewContent as IEditableTextBuffer;
			if (editable == null)
				return false;
			
			string fileName = null;
			
			IViewContent viewContent = ActiveWorkbenchWindow.ViewContent;
			IParseableContent parseableContent = ActiveWorkbenchWindow.ActiveViewContent as IParseableContent;
			
			if (parseableContent != null) {
				fileName = parseableContent.ParseableContentName;
			} else {
				fileName = viewContent.IsUntitled ? viewContent.UntitledName : viewContent.ContentName;
			}
			
			if (fileName == null || fileName.Length == 0)
				return false;
			
			if (Services.ParserService.GetParser (fileName) == null)
				return false;
			
			string text = editable.Text;
			if (text == null)
				return false;
		
			System.Threading.ThreadPool.QueueUserWorkItem (new System.Threading.WaitCallback (AsyncParseCurrentFile), new object[] { viewContent.Project, fileName, text });
			
			return false;
		}
		
		void AsyncParseCurrentFile (object ob)
		{
			object[] data = (object[]) ob;
			IdeApp.ProjectOperations.ParserDatabase.UpdateFile ((Project) data[0], (string) data[1], (string) data[2]);
		}

		public Gtk.Toolbar[] ToolBars {
			get { return toolbars; }
		}
		
		public IPadContent GetPad(Type type)
		{
			foreach (IPadContent pad in PadContentCollection) {
				if (pad.GetType() == type) {
					return pad;
				}
			}
			return null;
		}
		
		public void InitializeLayout (IWorkbenchLayout workbenchLayout)
		{
			PadCodon[] padCodons = (PadCodon[]) Runtime.AddInService.GetTreeItems (viewContentPath, typeof(PadCodon));
			
			foreach (PadCodon codon in padCodons)
				ShowPad (codon.Pad);
			
			layout = workbenchLayout;
			layout.Attach(this);
			layout.ActiveWorkbenchWindowChanged += windowChangeEventHandler;
			
			foreach (PadCodon codon in padCodons) {
				IPadWindow win = WorkbenchLayout.GetPadWindow (codon.Pad);
				if (codon.Label != null)
					win.Title = codon.Label;
				if (codon.Icon != null)
					win.Icon = codon.Icon;
			}

			RedrawAllComponents ();
			
			// Subscribe to changes in the extension
			Runtime.AddInService.RegisterExtensionItemListener (viewContentPath, OnExtensionChanged, false);
		}
		
		void OnExtensionChanged (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add) {
				PadCodon codon = (PadCodon) item;
				ShowPad (codon.Pad);
				
				IPadWindow win = WorkbenchLayout.GetPadWindow (codon.Pad);
				if (codon.Label != null)
					win.Title = codon.Label;
				if (codon.Icon != null)
					win.Icon = codon.Icon;

				RedrawAllComponents ();
			}
		}
		
		// Handle keyboard shortcuts


		public event EventHandler ActiveWorkbenchWindowChanged;

		/// Context switching specific parts
		WorkbenchContext context = WorkbenchContext.Edit;
		
		public WorkbenchContext Context {
			get { return context; }
			set {
				context = value;
				if (ContextChanged != null)
					ContextChanged (this, new EventArgs());
			}
		}

		public event EventHandler ContextChanged;
	}
}

