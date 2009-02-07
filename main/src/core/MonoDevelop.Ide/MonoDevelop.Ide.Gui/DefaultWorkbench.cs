//  DefaultWorkbench.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Xml;

using MonoDevelop.Projects;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Components.Commands;

using GLib;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the a Workspace with a multiple document interface.
	/// </summary>
	internal class DefaultWorkbench : Gtk.Window, IWorkbench
	{
		readonly static string mainMenuPath    = "/MonoDevelop/Ide/MainMenu";
		readonly static string viewContentPath = "/MonoDevelop/Ide/Pads";
		readonly static string toolbarsPath    = "/MonoDevelop/Ide/Toolbar";
		
		List<PadCodon> padContentCollection      = new List<PadCodon> ();
		List<IViewContent> viewContentCollection = new List<IViewContent> ();
		
		bool closeAll = false;

		bool fullscreen;
		Rectangle normalBounds = new Rectangle(0, 0, 640, 480);
		
		private IWorkbenchLayout layout = null;

		internal static GType gtype;
		
		public Gtk.MenuBar TopMenu = null;
		private Gtk.Toolbar[] toolbars = null;
		MonoDevelopStatusBar statusBar = new MonoDevelop.Ide.MonoDevelopStatusBar ();
		
		public MonoDevelopStatusBar StatusBar {
			get {
				return statusBar;
			}
		}
		
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
		
		public ReadOnlyCollection<PadCodon> PadContentCollection {
			get {
				Debug.Assert(padContentCollection != null);
				return padContentCollection.AsReadOnly ();
			}
		}
		
		public List<PadCodon> ActivePadContentCollection {
			get {
				if (layout == null)
					return new List<PadCodon> ();
				return layout.PadContentCollection;
			}
		}
		
		public ReadOnlyCollection<IViewContent> ViewContentCollection {
			get {
				Debug.Assert(viewContentCollection != null);
				return viewContentCollection.AsReadOnly ();
			}
		}
		
		internal List<IViewContent> InternalViewContentCollection {
			get {
				Debug.Assert(viewContentCollection != null);
				return viewContentCollection;
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
			LoggingService.LogInfo ("Creating DefaultWorkbench");
		
			windowChangeEventHandler = new EventHandler(OnActiveWindowChanged);

			WidthRequest = normalBounds.Width;
			HeightRequest = normalBounds.Height;

			DeleteEvent += new Gtk.DeleteEventHandler (OnClosing);
			
			if (Gtk.IconTheme.Default.HasIcon ("monodevelop")) 
				Gtk.Window.DefaultIconName = "monodevelop";
			else
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
						if (Services.ProjectService.IsWorkspaceItemFile (file))
							IdeApp.Workspace.OpenWorkspaceItem(file);
						else
							IdeApp.Workbench.OpenDocument (file);
					} catch (Exception e) {
						LoggingService.LogError ("unable to open file {0} exception was :\n{1}", file, e.ToString());
					}
				}
			}
		}
		
		public void InitializeWorkspace()
		{
			// FIXME: GTKize
			IdeApp.ProjectOperations.CurrentProjectChanged += (ProjectEventHandler) DispatchService.GuiDispatch (new ProjectEventHandler(SetProjectTitle));

			FileService.FileRemoved += (EventHandler<FileEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileEventArgs>(CheckRemovedFile));
			FileService.FileRenamed += (EventHandler<FileCopyEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileCopyEventArgs>(CheckRenamedFile));
			
//			TopMenu.Selected   += new CommandHandler(OnTopMenuSelected);
//			TopMenu.Deselected += new CommandHandler(OnTopMenuDeselected);

			TopMenu = IdeApp.CommandService.CreateMenuBar (mainMenuPath);
			toolbars = IdeApp.CommandService.CreateToolbarSet (toolbarsPath);
			foreach (Gtk.Toolbar t in toolbars) {
				t.ToolbarStyle = Gtk.ToolbarStyle.Icons;
				t.IconSize = PropertyService.Get <Gtk.IconSize> ("MonoDevelop.ToolbarSize", Gtk.IconSize.LargeToolbar);
			}
			PropertyService.PropertyChanged += delegate (object sender, MonoDevelop.Core.PropertyChangedEventArgs args) {
				if (args.Key == "MonoDevelop.ToolbarSize") {
					foreach (Gtk.Toolbar t in toolbars) {
						t.IconSize = PropertyService.Get <Gtk.IconSize> ("MonoDevelop.ToolbarSize", Gtk.IconSize.LargeToolbar);
					}
				}
			};
				
			AddinManager.ExtensionChanged += OnExtensionChanged;
		}
		
		void OnExtensionChanged (object s, ExtensionEventArgs args)
		{
			bool changed = false;
			
			if (args.PathChanged (mainMenuPath)) {
				TopMenu = IdeApp.CommandService.CreateMenuBar (mainMenuPath);
				changed = true;
			}
			
			if (args.PathChanged (toolbarsPath)) {
				toolbars = IdeApp.CommandService.CreateToolbarSet (toolbarsPath);
				foreach (Gtk.Toolbar t in toolbars)
					t.ToolbarStyle = Gtk.ToolbarStyle.Icons;
				changed = true;
			}
			
			if (changed && layout != null)
				layout.RedrawAllComponents();
		}
				
		public void CloseContent (IViewContent content)
		{
			if (viewContentCollection.Contains(content)) {
				viewContentCollection.Remove(content);
			}
		}
		
		public void CloseAllViews()
		{
			try {
				closeAll = true;
				List<IViewContent> fullList = new List<IViewContent>(viewContentCollection);
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
			viewContentCollection.Add(content);
			if (PropertyService.Get("SharpDevelop.LoadDocumentProperties", true) && content is IMementoCapable) {
				try {
					Properties memento = GetStoredMemento(content);
					if (memento != null) {
						((IMementoCapable)content).SetMemento(memento);
					}
				} catch (Exception e) {
					LoggingService.LogError ("Can't get/set memento : " + e.ToString());
				}
			}
			
			layout.ShowView(content);
			
			if (bringToFront)
				content.WorkbenchWindow.SelectWindow();
			
			RedrawAllComponents ();
			
			IEditableTextBuffer editor = (IEditableTextBuffer) content.GetContent (typeof(IEditableTextBuffer));
			if (editor != null)
				editor.TextChanged += OnViewTextChanged;
		}
		
		void ShowPadNode (ExtensionNode node)
		{
			if (node is PadCodon) {
				PadCodon pad = (PadCodon) node;
				ShowPad (pad);
				if (layout != null) {
					IPadWindow win = WorkbenchLayout.GetPadWindow (pad);
					if (pad.Label != null)
						win.Title = pad.Label;
					if (pad.Icon != null)
						win.Icon = pad.Icon;
				}
			}
			else if (node is CategoryNode) {
				foreach (ExtensionNode cn in node.ChildNodes)
					ShowPadNode (cn);
			}
		}
		
		void RemovePadNode (ExtensionNode node)
		{
			if (node is PadCodon)
				RemovePad ((PadCodon) node);
			else if (node is CategoryNode) {
				foreach (ExtensionNode cn in node.ChildNodes)
					RemovePadNode (cn);
			}
		}
		
		public void ShowPad (PadCodon content)
		{
			AddPad (content, true);
		}
		
		public void AddPad (PadCodon content)
		{
			AddPad (content, false);
		}
		
		void AddPad (PadCodon content, bool show)
		{
			if (padContentCollection.Contains (content))
				return;

			if (content.HasId) {
				ActionCommand cmd = new ActionCommand ("Pad|" + content.PadId, GettextCatalog.GetString (content.Label), null);
				cmd.DefaultHandler = new PadActivationHandler (this, content);
				cmd.Category = GettextCatalog.GetString ("View");
				cmd.Description = GettextCatalog.GetString ("Show {0}", cmd.Text);
				IdeApp.CommandService.RegisterCommand (cmd);
			}
			padContentCollection.Add (content);
			
			if (layout != null) {
				if (show)
					layout.ShowPad (content);
				else
					layout.AddPad (content);
			}
		}
		
		public void RemovePad (PadCodon codon)
		{
			if (codon.HasId) {
				Command cmd = IdeApp.CommandService.GetCommand (codon.Id);
				if (cmd != null)
					IdeApp.CommandService.UnregisterCommand (cmd);
			}
			padContentCollection.Remove (codon);
			
			if (layout != null)
				layout.RemovePad (codon);
		}
		
		public virtual void BringToFront (PadCodon content)
		{
			if (!layout.IsVisible (content))
				layout.ShowPad (content);

			layout.ActivatePad (content);
		}
		
		public void RedrawAllComponents()
		{
			foreach (IViewContent content in viewContentCollection) {
				content.RedrawContent();
			}
			foreach (PadCodon content in padContentCollection) {
				if (content.Initialized) {
					content.PadContent.RedrawContent();
				}
			}
			layout.RedrawAllComponents();
			//statusBarManager.RedrawStatusbar();
		}
		
		public Properties GetStoredMemento(IViewContent content)
		{
			if (content != null && content.ContentName != null) {
				string directory = System.IO.Path.Combine (PropertyService.ConfigPath, "temp");
				if (!Directory.Exists(directory)) {
					Directory.CreateDirectory(directory);
				}
				string fileName = content.ContentName.Substring(3).Replace('/', '.').Replace('\\', '.').Replace(System.IO.Path.DirectorySeparatorChar, '.');
				string fullFileName = directory + System.IO.Path.DirectorySeparatorChar + fileName;
				// check the file name length because it could be more than the maximum length of a file name
				if (FileService.IsValidPath(fullFileName) && File.Exists(fullFileName)) {
					return Properties.Load (fullFileName);
				}
			}
			return null;
		}
		
		// interface IMementoCapable
		public ICustomXmlSerializer CreateMemento()
		{
			WorkbenchMemento memento   = new WorkbenchMemento (new Properties ());
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
				memento.LayoutMemento = (Properties)layout.CreateMemento ();
			return memento.ToProperties ();
		}
		
		public void SetMemento (ICustomXmlSerializer xmlMemento)
		{
			if (xmlMemento != null) {
				WorkbenchMemento memento = new WorkbenchMemento ((Properties)xmlMemento);
				
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
		
		void CheckRemovedFile(object sender, FileEventArgs e)
		{
			if (e.IsDirectory) {
				IViewContent[] views = new IViewContent [ViewContentCollection.Count];
				ViewContentCollection.CopyTo (views, 0);
				foreach (IViewContent content in views) {
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
		
		void CheckRenamedFile(object sender, FileCopyEventArgs e)
		{
			if (e.IsDirectory) {
				foreach (IViewContent content in ViewContentCollection) {
					if (content.ContentName != null && content.ContentName.StartsWith(e.SourceFile)) {
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
			foreach (PadCodon content in PadContentCollection) {
				if (content.Initialized) {
					content.PadContent.Dispose();
				}
			}
		}
		
		public bool Close() 
		{
			if (!IdeApp.OnExit ())
				return false;

			IdeApp.Workspace.SavePreferences ();
			IdeApp.CommandService.Dispose ();

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
			
			CloseAllViews ();
			
			IdeApp.Workspace.Close (false);
			PropertyService.Set ("SharpDevelop.Workbench.WorkbenchMemento", CreateMemento());
			IdeApp.OnExited ();
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
		
//		bool parsingFile;
		
		void OnViewTextChanged (object sender, TextChangedEventArgs e)
		{
//document does it.
/*			if (!parsingFile) {
				parsingFile = true;
				GLib.Timeout.Add (500, new TimeoutHandler (ParseCurrentFile));
			}*/
		}
		/*
		bool ParseCurrentFile ()
		{
			parsingFile = false;
			
			if (ActiveWorkbenchWindow == null || ActiveWorkbenchWindow.ActiveViewContent == null)
				return false;

			IEditableTextBuffer editable = (IEditableTextBuffer) ActiveWorkbenchWindow.ActiveViewContent.GetContent (typeof(IEditableTextBuffer));
			if (editable == null)
				return false;
			
			string fileName = null;
			
			IViewContent viewContent = ActiveWorkbenchWindow.ViewContent;
			IParseableContent parseableContent = (IParseableContent) ActiveWorkbenchWindow.ActiveViewContent.GetContent (typeof(IParseableContent));
			
			if (parseableContent != null) {
				fileName = parseableContent.ParseableContentName;
			} else {
				fileName = viewContent.IsUntitled ? viewContent.UntitledName : viewContent.ContentName;
			}
			
			if (fileName == null || fileName.Length == 0)
				return false;
			
			if (ProjectDomService.GetParserByFileName (fileName) == null)
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
			IdeApp.Workspace.ParserDatabase.UpdateFile ((Project) data[0], (string) data[1], (string) data[2]);
		}
*/
		public Gtk.Toolbar[] ToolBars {
			get { return toolbars; }
		}
		
		public PadCodon GetPad(Type type)
		{
			foreach (PadCodon pad in PadContentCollection) {
				if (pad.ClassName == type.FullName) {
					return pad;
				}
			}
			return null;
		}
		
		public PadCodon GetPad(string id)
		{
			foreach (PadCodon pad in PadContentCollection) {
				if (pad.PadId == id) {
					return pad;
				}
			}
			return null;
		}
		
		public void InitializeLayout (IWorkbenchLayout workbenchLayout)
		{
			ExtensionNodeList padCodons = AddinManager.GetExtensionNodes (viewContentPath);
			
			foreach (ExtensionNode node in padCodons)
				ShowPadNode (node);
			
			layout = workbenchLayout;
			layout.Attach(this);
			layout.ActiveWorkbenchWindowChanged += windowChangeEventHandler;
			
			foreach (ExtensionNode node in padCodons)
				ShowPadNode (node);

			RedrawAllComponents ();
			
			// Subscribe to changes in the extension
			initializing = true;
			AddinManager.AddExtensionNodeHandler (viewContentPath, OnExtensionChanged);
			initializing = false;
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			// Handle Tab+Control == NextWindow, Tab+Shift+Control == PrevWindow commands.
			if (evnt.Key == Gdk.Key.Tab || evnt.Key == Gdk.Key.ISO_Left_Tab) {
				if ((evnt.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask) {
					bool selectNext = (evnt.State & Gdk.ModifierType.ShiftMask) != Gdk.ModifierType.ShiftMask;
					if (PropertyService.Get ("MonoDevelop.Core.Gui.EnableDocumentSwitchDialog", true)) {
						DocumentSwitcher switcher = new DocumentSwitcher (this, selectNext);
						switcher.ShowAll ();
						switcher.GrabFocus ();
					} else {
						IdeApp.CommandService.DispatchCommand (selectNext ? WindowCommands.NextWindow : WindowCommands.PrevWindow);
					}
					return true;
				}
			}
			
			// Handle Alt+1-0 keys
			if ((evnt.State & Gdk.ModifierType.Mod1Mask) == Gdk.ModifierType.Mod1Mask) {		
				switch (evnt.Key) {
				case Gdk.Key.KP_1:
				case Gdk.Key.Key_1:
					SwitchToDocument (0);
					return true;
				case Gdk.Key.KP_2:
				case Gdk.Key.Key_2:
					SwitchToDocument (1);
					return true;
				case Gdk.Key.KP_3:
				case Gdk.Key.Key_3:
					SwitchToDocument (2);
					return true;
				case Gdk.Key.KP_4:
				case Gdk.Key.Key_4:
					SwitchToDocument (3);
					return true;
				case Gdk.Key.KP_5:
				case Gdk.Key.Key_5:
					SwitchToDocument (4);
					return true;
				case Gdk.Key.KP_6:
				case Gdk.Key.Key_6:
					SwitchToDocument (5);
					return true;
				case Gdk.Key.KP_7:
				case Gdk.Key.Key_7:
					SwitchToDocument (6);
					return true;
				case Gdk.Key.KP_8:
				case Gdk.Key.Key_8:
					SwitchToDocument (7);
					return true;
				case Gdk.Key.KP_9:
				case Gdk.Key.Key_9:
					SwitchToDocument (8);
					return true;
				case Gdk.Key.KP_0:
				case Gdk.Key.Key_0:
					SwitchToDocument (9);
					return true;
				}
			}
			return base.OnKeyPressEvent (evnt); 
		}
		
		void SwitchToDocument (int number)
		{
			if (number >= viewContentCollection.Count || number < 0)
				return;
			viewContentCollection[number].WorkbenchWindow.SelectWindow ();
		}
		
		bool initializing;
		
		void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (initializing)
				return;
			
			if (args.Change == ExtensionChange.Add) {
				ShowPadNode (args.ExtensionNode);
				RedrawAllComponents ();
			}
			else {
				RemovePadNode (args.ExtensionNode);
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

	class PadActivationHandler: CommandHandler
	{
		PadCodon pad;
		DefaultWorkbench wb;
		
		public PadActivationHandler (DefaultWorkbench wb, PadCodon pad)
		{
			this.pad = pad;
			this.wb = wb;
		}
		
		protected override void Run ()
		{
			wb.BringToFront (pad);
		}
	}
}

