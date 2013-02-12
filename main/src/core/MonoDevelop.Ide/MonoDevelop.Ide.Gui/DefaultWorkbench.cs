//  DefaultWorkbench.cs
//
// Author:
//   Mike Krüger
//   Lluis Sanchez Gual
//
//  This file was derived from a file from #Develop 2.0
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
//  Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// 
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
// 
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;

using MonoDevelop.Projects;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Docking;

using GLib;
using MonoDevelop.Components.DockToolbars;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Extensions;
using Mono.TextEditor;
using MonoDevelop.Components.MainToolbar;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the a Workspace with a multiple document interface.
	/// </summary>
	internal class DefaultWorkbench : WorkbenchWindow, ICommandRouter
	{
		readonly static string mainMenuPath    = "/MonoDevelop/Ide/MainMenu";
		readonly static string appMenuPath    = "/MonoDevelop/Ide/AppMenu";
		readonly static string viewContentPath = "/MonoDevelop/Ide/Pads";
//		readonly static string toolbarsPath    = "/MonoDevelop/Ide/Toolbar";
		readonly static string stockLayoutsPath    = "/MonoDevelop/Ide/WorkbenchLayouts";
		
		static string configFile = UserProfile.Current.ConfigDir.Combine ("EditingLayout.xml");
		const string fullViewModeTag = "[FullViewMode]";
		const int MAX_LASTACTIVEWINDOWS = 10;
		const int MinimumWidth = 1000;
		const int MinimumHeight = 600;
		
		// list of layout names for the current context, without the context prefix
		List<string> layouts = new List<string> ();
		
		List<PadCodon> padContentCollection      = new List<PadCodon> ();
		List<IViewContent> viewContentCollection = new List<IViewContent> ();
		Dictionary<PadCodon, IPadWindow> padWindows = new Dictionary<PadCodon, IPadWindow> ();
		Dictionary<IPadWindow, PadCodon> padCodons = new Dictionary<IPadWindow, PadCodon> ();
		
		IWorkbenchWindow lastActive;
		LinkedList<IWorkbenchWindow> lastActiveWindows = new LinkedList<IWorkbenchWindow> ();
		
		bool closeAll;
		bool ignorePageSwitch;

		bool fullscreen;
		Rectangle normalBounds = new Rectangle(0, 0, MinimumWidth, MinimumHeight);
		
		Gtk.Container rootWidget;
		DockToolbarFrame toolbarFrame;
		DockFrame dock;
		SdiDragNotebook tabControl;
		Gtk.MenuBar topMenu;
		Gtk.VBox fullViewVBox;
		DockItem documentDockItem;
		MainToolbar toolbar;
		MonoDevelopStatusBar bottomBar;

		enum TargetList {
			UriList = 100
		}

		Gtk.TargetEntry[] targetEntryTypes = new Gtk.TargetEntry[] {
			new Gtk.TargetEntry ("text/uri-list", 0, (uint)TargetList.UriList)
		};
		
#if DUMMY_STRINGS_FOR_TRANSLATION_DO_NOT_COMPILE
		private void DoNotCompile ()
		{
			//The default layout, translated indirectly because it's used as an ID
			GettextCatalog.GetString ("Default");
		}
#endif
		
		public event EventHandler ActiveWorkbenchWindowChanged;
		
		public MonoDevelop.Ide.StatusBar StatusBar {
			get {
				return toolbar.StatusBar;
			}
		}

		public MainToolbar Toolbar {
			get {
				return toolbar;
			}
		}

		public Gtk.MenuBar TopMenu {
			get { return topMenu; }
		}

		public MonoDevelopStatusBar BottomBar {
			get { return bottomBar; }
		}
		
		internal IWorkbenchWindow ActiveWorkbenchWindow {
			get {
				if (tabControl == null || tabControl.CurrentTabIndex < 0 || tabControl.CurrentTabIndex >= tabControl.TabCount)  {
					return null;
				}
				return (IWorkbenchWindow) tabControl.CurrentTab.Content;
			}
		}
		
		public DockFrame DockFrame {
			get { return dock; }
		}
		
		public bool FullScreen {
			get {
				return DesktopService.GetIsFullscreen (this);
			}
			set {
				DesktopService.SetIsFullscreen (this, value);
			}
		}

		public IList<string> Layouts {
			get {
				return layouts;
			}
		}
		
		public string CurrentLayout {
			get {
				if (dock != null && dock.CurrentLayout != null) {
					string s = dock.CurrentLayout;
					s = s.Substring (s.IndexOf (".") + 1);
					if (s.EndsWith (fullViewModeTag))
						return s.Substring (0, s.Length - fullViewModeTag.Length);
					return s;
				}
				else
					return "";
			}
			set {
				var oldLayout = dock.CurrentLayout;
				
				InitializeLayout (value);
				toolbarFrame.CurrentLayout = dock.CurrentLayout = value;
				
				DestroyFullViewLayouts (oldLayout);
				
				// persist the selected layout
				PropertyService.Set ("MonoDevelop.Core.Gui.CurrentWorkbenchLayout", value);
			}
		}
		
		public void DeleteLayout (string name)
		{
			string layout = name;
			layouts.Remove (name);
			dock.DeleteLayout (layout);
		}
		
		public List<PadCodon> PadContentCollection {
			get {
				return padContentCollection;
			}
		}
		
		internal List<IViewContent> InternalViewContentCollection {
			get {
				Debug.Assert(viewContentCollection != null);
				return viewContentCollection;
			}
		}
		
		public DefaultWorkbench()
		{
			Title = BrandingService.ApplicationName;
			LoggingService.LogInfo ("Creating DefaultWorkbench");
			
			WidthRequest = normalBounds.Width;
			HeightRequest = normalBounds.Height;

			DeleteEvent += new Gtk.DeleteEventHandler (OnClosing);
			
			SetAppIcons ();

			//this.WindowPosition = Gtk.WindowPosition.None;

			Gtk.Drag.DestSet (this, Gtk.DestDefaults.Motion | Gtk.DestDefaults.Highlight | Gtk.DestDefaults.Drop, targetEntryTypes, Gdk.DragAction.Copy);
			DragDataReceived += new Gtk.DragDataReceivedHandler (onDragDataRec);
			
			IdeApp.CommandService.SetRootWindow (this);
		}
		
		void SetAppIcons ()
		{
			//first try to get the icon from the GTK icon theme
			var appIconName = BrandingService.GetString ("ApplicationIconId")
				?? BrandingService.ApplicationName.ToLower ();
			if (Gtk.IconTheme.Default.HasIcon (appIconName)) {
				Gtk.Window.DefaultIconName = appIconName;
				return;
			}

			if (!Platform.IsMac) {
				//branded icons
				var iconsEl = BrandingService.GetElement ("ApplicationIcons");
				if (iconsEl != null) {
					try {
						this.IconList = iconsEl.Elements ("Icon")
							.Select (el => new Gdk.Pixbuf (BrandingService.GetFile ((string)el))).ToArray ();
						return;
					} catch (Exception ex) {
						LoggingService.LogError ("Could not load app icons", ex);
					}
				}
			}
			
			//built-ins
			this.IconList = new Gdk.Pixbuf[] {
				ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.MonoDevelop, Gtk.IconSize.Menu),
				ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.MonoDevelop, Gtk.IconSize.Button),
				ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.MonoDevelop, Gtk.IconSize.Dnd),
				ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.MonoDevelop, Gtk.IconSize.Dialog)
			};
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
			
			if (!DesktopService.SetGlobalMenu (IdeApp.CommandService, mainMenuPath, appMenuPath)) {
				CreateMenuBar ();
			}
			
			AddinManager.ExtensionChanged += OnExtensionChanged;
		}
		
		void OnExtensionChanged (object s, ExtensionEventArgs args)
		{
			if (args.PathChanged (mainMenuPath) || args.PathChanged (appMenuPath)) {
				if (DesktopService.SetGlobalMenu (IdeApp.CommandService, mainMenuPath, appMenuPath))
					return;
				
				UninstallMenuBar ();
				CreateMenuBar ();
				InstallMenuBar ();
			}
		}

		void CreateMenuBar ()
		{
			topMenu = IdeApp.CommandService.CreateMenuBar (mainMenuPath);
			var appMenu = IdeApp.CommandService.CreateMenu (appMenuPath);
			if (appMenu != null && appMenu.Children.Length > 0) {
				var item = new MenuItem (BrandingService.ApplicationName);
				item.Submenu = appMenu;
				topMenu.Insert (item, 0);
			}
		}
		
		void InstallMenuBar ()
		{
			if (topMenu != null) {
				((VBox)rootWidget).PackStart (topMenu, false, false, 0);
				((Gtk.Box.BoxChild) rootWidget [topMenu]).Position = 0;
				topMenu.ShowAll ();
			}
		}
				
		void UninstallMenuBar ()
		{
			if (topMenu == null)
				return;
			
			rootWidget.Remove (topMenu);
			topMenu = null;
		}
		
		public void CloseContent (IViewContent content)
		{
			if (viewContentCollection.Contains(content)) {
				if (content.Project != null)
					content.Project.NameChanged -= HandleProjectNameChanged;
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
					if (window != null)
						window.CloseWindow(true);
				}
			} finally {
				closeAll = false;
				OnActiveWindowChanged (null, null);
			}
		}
		
		public virtual void ShowView (IViewContent content, bool bringToFront)
		{
			viewContentCollection.Add (content);
			
			if (PropertyService.Get ("SharpDevelop.LoadDocumentProperties", true) && content is IMementoCapable) {
				try {
					Properties memento = GetStoredMemento(content);
					if (memento != null) {
						((IMementoCapable)content).Memento = memento;
					}
				} catch (Exception e) {
					LoggingService.LogError ("Can't get/set memento : " + e.ToString());
				}
			}
			
			Gdk.Pixbuf mimeimage = null;
			
			if (content.StockIconId != null ) {
				mimeimage = ImageService.GetPixbuf (content.StockIconId, IconSize.Menu);
			}
			else if (content.IsUntitled && content.UntitledName == null) {
				mimeimage = DesktopService.GetPixbufForType ("gnome-fs-regular", Gtk.IconSize.Menu);
			} else {
				mimeimage = DesktopService.GetPixbufForFile (content.ContentName ?? content.UntitledName, Gtk.IconSize.Menu);
			}			

			var tab = tabControl.InsertTab (-1);

			SdiWorkspaceWindow sdiWorkspaceWindow = new SdiWorkspaceWindow (this, content, tabControl, tab);
			sdiWorkspaceWindow.TitleChanged += delegate { SetWorkbenchTitle (); };
			sdiWorkspaceWindow.Closed += CloseWindowEvent;
			sdiWorkspaceWindow.Show ();

			tab.Content = sdiWorkspaceWindow;
			if (mimeimage != null)
				tab.Icon = mimeimage;

			if (content.Project != null)
				content.Project.NameChanged += HandleProjectNameChanged;
			
			if (bringToFront)
				content.WorkbenchWindow.SelectWindow();

			// The insertion of the tab may have changed the active view (or maybe not, this is checked in OnActiveWindowChanged)
			OnActiveWindowChanged (null, null);
		}

		void HandleProjectNameChanged (object sender, SolutionItemRenamedEventArgs e)
		{
			SetWorkbenchTitle ();
		}
		
		void ShowPadNode (ExtensionNode node)
		{
			if (node is PadCodon) {
				PadCodon pad = (PadCodon) node;
				RegisterPad (pad);
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
		
		void RegisterPad (PadCodon content)
		{
			if (content.HasId) {
				string lab = content.Label.Length > 0 ? GettextCatalog.GetString (content.Label) : "";
				ActionCommand cmd = new ActionCommand ("Pad|" + content.PadId, lab, null);
				cmd.DefaultHandler = new PadActivationHandler (this, content);
				cmd.Category = GettextCatalog.GetString ("View");
				cmd.Description = GettextCatalog.GetString ("Show {0}", cmd.Text);
				IdeApp.CommandService.RegisterCommand (cmd);
			}
			padContentCollection.Add (content);
		}
		
		void AddPad (PadCodon content, bool show)
		{
			DockItem item = GetDockItem (content);
			if (padContentCollection.Contains (content)) {
				if (show && item != null)
					item.Visible = true;
				return;
			}

			RegisterPad (content);
			
			if (item != null) {
				if (show)
					item.Visible = true;
			} else {
				AddPad (content, content.DefaultPlacement, content.DefaultStatus);
			}
		}
		
		public void RemovePad (PadCodon codon)
		{
			if (codon.HasId) {
				Command cmd = IdeApp.CommandService.GetCommand (codon.Id);
				if (cmd != null)
					IdeApp.CommandService.UnregisterCommand (cmd);
			}
			DockItem item = GetDockItem (codon);
			padContentCollection.Remove (codon);
			PadWindow win = (PadWindow) GetPadWindow (codon);
			if (win != null) {
				win.NotifyDestroyed ();
				Counters.PadsLoaded--;
				padCodons.Remove (win);
			}
			if (item != null)
				dock.RemoveItem (item);
			padWindows.Remove (codon);
		}
		
		public void BringToFront (PadCodon content)
		{
			BringToFront (content, false);
		}
		
		public virtual void BringToFront (PadCodon content, bool giveFocus)
		{
			if (!IsVisible (content))
				ShowPad (content);

			ActivatePad (content, giveFocus);
		}
		
		void SetWorkbenchTitle ()
		{
			try {
				IWorkbenchWindow window = ActiveWorkbenchWindow;
				if (window != null) {
					if (window.ViewContent.IsUntitled) {
						SetDefaultTitle ();
					} else {
						string post = String.Empty;
						if (window.ViewContent.IsDirty) {
							post = "*";
						}
						if (window.ViewContent.Project != null) {
							Title = window.ViewContent.Project.Name + " - " + window.ViewContent.PathRelativeToProject + post + " - " + BrandingService.ApplicationName;
						} else {
							Title = window.ViewContent.ContentName + post + " - " + BrandingService.ApplicationName;
						}
					}
				} else {
					SetDefaultTitle ();
					if (IsInFullViewMode)
						this.ToggleFullViewMode ();
				}
			} catch (Exception) {
				SetDefaultTitle ();
			}
		}
		
		void SetDefaultTitle ()
		{
			if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
				Title = IdeApp.ProjectOperations.CurrentSelectedProject.Name + " - " + BrandingService.ApplicationName;
			} else {
				Title = BrandingService.ApplicationName;
			}
		}
		
		public Properties GetStoredMemento (IViewContent content)
		{
			if (content != null && content.ContentName != null) {
				string directory = UserProfile.Current.CacheDir.Combine ("temp");
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
		
		public ICustomXmlSerializer Memento {
			get {
				WorkbenchMemento memento   = new WorkbenchMemento (new Properties ());
				int x, y, width, height;
				GetPosition (out x, out y);
				GetSize (out width, out height);
				// HACK: always capture bounds on OS X because we don't restore Gdk.WindowState.Maximized due to
				// the bug mentioned below. So we simular Maximized by capturing the Maximized size.
				if (GdkWindow.State == 0 || Platform.IsMac) {
					memento.Bounds = new Rectangle (x, y, width, height);
				} else {
					memento.Bounds = normalBounds;
				}
				memento.WindowState = GdkWindow.State;
				memento.FullScreen  = fullscreen;
				memento.ToolbarStatus = toolbarFrame.GetStatus ();
				return memento.ToProperties ();
			}
			set {
				if (value != null) {
					WorkbenchMemento memento = new WorkbenchMemento ((Properties)value);
					
					normalBounds = memento.Bounds;
					Move (normalBounds.X, normalBounds.Y);
					Resize (normalBounds.Width, normalBounds.Height);
					
					// HACK: don't restore Gdk.WindowState.Maximized on OS X, because there's a bug in 
					// GdkWindow.State that means it doesn't reflect the real state, it only reflects values set
					// internally by GTK e.g. by Maximize calls. Because MD restores state, if it ever becomes 
					// Maximized it becomes "stuck" and it's difficult for the user to make it "normal".
					if (memento.WindowState == Gdk.WindowState.Maximized && !Platform.IsMac) {
						Maximize ();
					} else if (memento.WindowState == Gdk.WindowState.Iconified) {
						Iconify ();
					}
					//GdkWindow.State = memento.WindowState;
					FullScreen = memento.FullScreen;
					toolbarFrame.SetStatus (memento.ToolbarStatus);
				}
				Decorated = true;
			}
		}
		
		void CheckRemovedFile (object sender, FileEventArgs args)
		{
			foreach (FileEventInfo e in args) {
				if (e.IsDirectory) {
					IViewContent[] views = new IViewContent [viewContentCollection.Count];
					viewContentCollection.CopyTo (views, 0);
					foreach (IViewContent content in views) {
						if (content.ContentName.StartsWith (e.FileName)) {
							((SdiWorkspaceWindow)content.WorkbenchWindow).CloseWindow (true, true);
						}
					}
				} else {
					foreach (IViewContent content in viewContentCollection) {
						if (content.ContentName != null &&
							content.ContentName == e.FileName) {
							((SdiWorkspaceWindow)content.WorkbenchWindow).CloseWindow (true, true);
							return;
						}
					}
				}
			}
		}
		
		void CheckRenamedFile(object sender, FileCopyEventArgs args)
		{
			foreach (FileCopyEventInfo e in args) {
				if (e.IsDirectory) {
					foreach (IViewContent content in viewContentCollection) {
						if (content.ContentName != null && ((FilePath)content.ContentName).IsChildPathOf (e.SourceFile)) {
							content.ContentName = e.TargetFile.Combine (((FilePath) content.ContentName).FileName);
						}
					}
				} else {
					foreach (IViewContent content in viewContentCollection) {
						if (content.ContentName != null &&
						    content.ContentName == e.SourceFile) {
							content.ContentName = e.TargetFile;
							return;
						}
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
		
		protected void OnClosed(EventArgs e)
		{
			//don't allow the "full view" layouts to persist - they are always derived from the "normal" layout
			foreach (var fv in dock.Layouts)
				if (fv.EndsWith (fullViewModeTag))
					dock.DeleteLayout (fv);
			try {
				dock.SaveLayouts (configFile);
			} catch (Exception ex) {
				LoggingService.LogError ("Error while saving layout.", ex);
			}
			UninstallMenuBar ();
			Remove (rootWidget);
			
			foreach (PadCodon content in PadContentCollection) {
				if (content.Initialized)
					content.PadContent.Dispose();
			}
		}
		
		public bool Close() 
		{
			if (!IdeApp.OnExit ())
				return false;

			IdeApp.Workspace.SavePreferences ();

			bool showDirtyDialog = false;

			foreach (IViewContent content in viewContentCollection)
			{
				if (content.IsDirty) {
					showDirtyDialog = true;
					break;
				}
			}

			if (showDirtyDialog) {
				DirtyFilesDialog dlg = new DirtyFilesDialog ();
				dlg.Modal = true;
				if (MessageService.ShowCustomDialog (dlg, this) != (int)Gtk.ResponseType.Ok)
					return false;
			}
			
			if (!IdeApp.Workspace.Close (false, false))
				return false;
			
			CloseAllViews ();
			
			PropertyService.Set ("SharpDevelop.Workbench.WorkbenchMemento", this.Memento);
			IdeApp.OnExited ();
			OnClosed (null);
			
			IdeApp.CommandService.Dispose ();
			
			return true;
		}
		
		void SetProjectTitle(object sender, ProjectEventArgs e)
		{
			SetWorkbenchTitle ();
		}
		
		void OnActiveWindowChanged (object sender, EventArgs e)
		{
			if (ignorePageSwitch)
				return;
			
			if (lastActive == ActiveWorkbenchWindow)
				return;

			WelcomePage.WelcomePageService.HideWelcomePage ();

			if (lastActive != null)
				((SdiWorkspaceWindow)lastActive).OnDeactivated ();

			if (lastActiveWindows.Count > MAX_LASTACTIVEWINDOWS)
				lastActiveWindows.RemoveFirst ();

			lastActiveWindows.Remove (lastActive);
			lastActiveWindows.AddLast (lastActive);
			lastActive = ActiveWorkbenchWindow;
			SetWorkbenchTitle ();

			if (!closeAll && ActiveWorkbenchWindow != null)
				((SdiWorkspaceWindow)ActiveWorkbenchWindow).OnActivated ();

			if (!closeAll && ActiveWorkbenchWindowChanged != null) {
				ActiveWorkbenchWindowChanged(this, e);
			}
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
		
		public void InitializeLayout ()
		{
			AddinManager.AddExtensionNodeHandler (stockLayoutsPath, OnLayoutsExtensionChanged);
			
			ExtensionNodeList padCodons = AddinManager.GetExtensionNodes (viewContentPath);
			foreach (ExtensionNode node in padCodons)
				ShowPadNode (node);
			
			CreateComponents ();
			
			// Subscribe to changes in the extension
			initializing = true;
			AddinManager.AddExtensionNodeHandler (viewContentPath, OnExtensionChanged);
			initializing = false;
		}

		void CreateComponents ()
		{
			fullViewVBox = new VBox (false, 0);
			rootWidget = fullViewVBox;
			
			InstallMenuBar ();
			Realize ();
			toolbar = DesktopService.CreateMainToolbar (this);
			DesktopService.SetMainWindowDecorations (this);
			var toolbarBox = new HBox ();
			fullViewVBox.PackStart (toolbarBox, false, false, 0);
			toolbarFrame = new CommandFrame (IdeApp.CommandService);

			fullViewVBox.PackStart (toolbarFrame, true, true, 0);

			// Create the docking widget and add it to the window.
			dock = new DockFrame ();
			
			dock.CompactGuiLevel = ((int)IdeApp.Preferences.WorkbenchCompactness) + 1;
			IdeApp.Preferences.WorkbenchCompactnessChanged += delegate {
				dock.CompactGuiLevel = ((int)IdeApp.Preferences.WorkbenchCompactness) + 1;
			};
			
			/* Side bar is experimental. Disabled for now
			HBox hbox = new HBox ();
			VBox sideBox = new VBox ();
			sideBox.PackStart (new SideBar (workbench, Orientation.Vertical), false, false, 0);
			hbox.PackStart (sideBox, false, false, 0);
			hbox.ShowAll ();
			sideBox.NoShowAll = true;
			hbox.PackStart (dock, true, true, 0);
			DockBar bar = dock.ExtractDockBar (PositionType.Left);
			bar.AlwaysVisible = true;
			sideBox.PackStart (bar, true, true, 0);
			toolbarFrame.AddContent (hbox);
			*/

			toolbarFrame.AddContent (dock);
			
			// Create the notebook for the various documents.
			tabControl = new SdiDragNotebook ();
			tabControl.SwitchPage += OnActiveWindowChanged;
			tabControl.PageRemoved += OnActiveWindowChanged;
			tabControl.TabClosed += CloseClicked;
		
			tabControl.TabActivated += delegate(object sender, TabEventArgs e) {
				ToggleFullViewMode ();
			};

			this.tabControl.DoPopupMenu = ShowPopup;
			
			tabControl.TabsReordered += new TabsReorderedHandler (OnTabsReordered);

			Add (fullViewVBox);
			fullViewVBox.ShowAll ();
			bottomBar = new MonoDevelopStatusBar ();
			fullViewVBox.PackEnd (bottomBar, false, true, 0);
			bottomBar.ShowAll ();
			toolbarBox.PackStart (this.toolbar, true, true, 0);

			// In order to get the correct bar height we need to calculate the tab size using the
			// correct style (the style of the window). At this point the widget is not yet a child
			// of the window, so its style is not yet the correct one.
			tabControl.InitSize (this);
			var barHeight = tabControl.BarHeight;

			// The main document area
			documentDockItem = dock.AddItem ("Documents");
			documentDockItem.Behavior = DockItemBehavior.Locked;
			documentDockItem.Expand = true;
			documentDockItem.DrawFrame = false;
			documentDockItem.Label = GettextCatalog.GetString ("Documents");
			documentDockItem.Content = tabControl;

			DockVisualStyle style = new DockVisualStyle ();
			style.PadTitleLabelColor = Styles.PadLabelColor;
			style.PadBackgroundColor = Styles.PadBackground;
			style.InactivePadBackgroundColor = Styles.InactivePadBackground;
			style.PadTitleHeight = barHeight;
			dock.DefaultVisualStyle = style;

			style = new DockVisualStyle ();
			style.PadTitleLabelColor = Styles.PadLabelColor;
			style.PadTitleHeight = barHeight;
			style.ShowPadTitleIcon = false;
			style.UppercaseTitles = false;
			style.ExpandedTabs = true;
			style.PadBackgroundColor = Styles.BrowserPadBackground;
			style.InactivePadBackgroundColor = Styles.InactiveBrowserPadBackground;
			style.TreeBackgroundColor = Styles.BrowserPadBackground;
			dock.SetDockItemStyle ("ProjectPad", style);
			dock.SetDockItemStyle ("ClassPad", style);

//			dock.SetRegionStyle ("Documents/Left", style);
			//dock.SetRegionStyle ("Documents/Right", style);

//			style = new DockVisualStyle ();
//			style.SingleColumnMode = true;
//			dock.SetRegionStyle ("Documents/Left;Documents/Right", style);
//			dock.SetDockItemStyle ("Documents", style);

			// Add some hiden items to be used as position reference
			DockItem dit = dock.AddItem ("__left");
			dit.DefaultLocation = "Documents/Left";
			dit.Behavior = DockItemBehavior.Locked;
			dit.DefaultVisible = false;
			
			dit = dock.AddItem ("__right");
			dit.DefaultLocation = "Documents/Right";
			dit.Behavior = DockItemBehavior.Locked;
			dit.DefaultVisible = false;
			
			dit = dock.AddItem ("__top");
			dit.DefaultLocation = "Documents/Top";
			dit.Behavior = DockItemBehavior.Locked;
			dit.DefaultVisible = false;
			
			dit = dock.AddItem ("__bottom");
			dit.DefaultLocation = "Documents/Bottom";
			dit.Behavior = DockItemBehavior.Locked;
			dit.DefaultVisible = false;

			if (MonoDevelop.Core.Platform.IsMac)
				bottomBar.HasResizeGrip = true;
			else {
				if (GdkWindow != null && GdkWindow.State == Gdk.WindowState.Maximized)
					bottomBar.HasResizeGrip = false;
				SizeAllocated += delegate {
					if (GdkWindow != null)
						bottomBar.HasResizeGrip = GdkWindow.State != Gdk.WindowState.Maximized;
				};
			}

			// create DockItems for all the pads
			foreach (PadCodon content in padContentCollection)
				AddPad (content, content.DefaultPlacement, content.DefaultStatus);
			
			try {
				if (System.IO.File.Exists (configFile)) {
					dock.LoadLayouts (configFile);
					foreach (string layout in dock.Layouts) {
						if (!layouts.Contains (layout) && !layout.EndsWith (fullViewModeTag))
							layouts.Add (layout);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}
		
		void InitializeLayout (string name)
		{
			if (!layouts.Contains (name))
				layouts.Add (name);
			
			if (dock.Layouts.Contains (name))
				return;
			
			dock.CreateLayout (name, true);
			dock.CurrentLayout = name;
			documentDockItem.Visible = true;
			
			LayoutExtensionNode stockLayout = null;
			foreach (LayoutExtensionNode node in AddinManager.GetExtensionNodes (stockLayoutsPath)) {
				if (node.Name == name) {
					stockLayout = node;
					break;
				}
			}
			
			if (stockLayout == null)
				return;
			
			HashSet<string> visible = new HashSet<string> ();
			
			foreach (LayoutPadExtensionNode pad in stockLayout.ChildNodes) {
				DockItem it = dock.GetItem (pad.Id);
				if (it != null) {
					it.Visible = true;
					string loc = pad.Placement ?? it.DefaultLocation;
					if (!string.IsNullOrEmpty (loc))
					    it.SetDockLocation (ToDockLocation (loc));
					DockItemStatus stat = pad.StatusSet ? pad.Status : it.DefaultStatus;
					it.Status = stat;
					visible.Add (pad.Id);
				}
			}
			
			foreach (PadCodon node in padContentCollection) {
				if (!visible.Contains (node.Id) && node.DefaultLayouts != null && (node.DefaultLayouts.Contains (stockLayout.Id) || node.DefaultLayouts.Contains ("*"))) {
					DockItem it = dock.GetItem (node.Id);
					if (it != null) {
						it.Visible = true;
						if (!string.IsNullOrEmpty (node.DefaultPlacement))
							it.SetDockLocation (ToDockLocation (node.DefaultPlacement));
						it.Status = node.DefaultStatus;
						visible.Add (node.Id);
					}
				}
			}
			
			foreach (DockItem it in dock.GetItems ()) {
				if (!visible.Contains (it.Id) && ((it.Behavior & DockItemBehavior.Sticky) == 0) && it != documentDockItem)
					it.Visible = false;
			}
		}
		
		void ShowPopup (int tabIndex, Gdk.EventButton evt)
		{
			this.tabControl.CurrentTabIndex = tabIndex;
			IdeApp.CommandService.ShowContextMenu (this.tabControl, evt, "/MonoDevelop/Ide/ContextMenu/DocumentTab");
		}
		
		void OnTabsReordered (Widget widget, int oldPlacement, int newPlacement)
		{
			IdeApp.Workbench.ReorderDocuments (oldPlacement, newPlacement);
		}
		
		public void ResetToolbars ()
		{
			toolbarFrame.ResetToolbarPositions ();
		}
		
		bool IsInFullViewMode {
			get {
				return dock.CurrentLayout.EndsWith (fullViewModeTag);
			}
		}
		
		//don't allow the "full view" layouts to persist - they are always derived from the "normal" layout
		//else they will diverge
		void DestroyFullViewLayouts (string oldLayout)
		{
			if (oldLayout != null && oldLayout.EndsWith (fullViewModeTag)) {
				dock.DeleteLayout (oldLayout);
				toolbarFrame.DeleteLayout (oldLayout);
			}
		}
		
		public void ToggleFullViewMode ()
		{
			if (IsInFullViewMode) {
				var oldLayout = dock.CurrentLayout;
				toolbarFrame.CurrentLayout = dock.CurrentLayout = CurrentLayout;
				DestroyFullViewLayouts (oldLayout);
			} else {
				string fullViewLayout = CurrentLayout + fullViewModeTag;
				if (!dock.HasLayout (fullViewLayout))
					dock.CreateLayout (fullViewLayout, true);
				toolbarFrame.CurrentLayout = dock.CurrentLayout = fullViewLayout;
				foreach (DockItem it in dock.GetItems ()) {
					if (it.Behavior != DockItemBehavior.Locked && it.Visible)
						it.Status = DockItemStatus.AutoHide;
				}
				foreach (var tb in toolbarFrame.Toolbars)
					tb.Status = new DockToolbarStatus (tb.Id, false, tb.Position);
			}
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			// Handle Alt+1-0 keys
			Gdk.ModifierType winSwitchModifier = Platform.IsMac
				? KeyBindingManager.SelectionModifierControl
				: KeyBindingManager.SelectionModifierAlt;
			
			if ((evnt.State & winSwitchModifier) != 0 && (evnt.State & (Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask)) != (Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask)) {
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
			}
			else {
				RemovePadNode (args.ExtensionNode);
			}
		}
		
		void OnLayoutsExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				layouts.Add (((LayoutExtensionNode)args.ExtensionNode).Name);
			else
				layouts.Remove (((LayoutExtensionNode)args.ExtensionNode).Name);
		}
		
		#region View management
		
		bool SelectLastActiveWindow (IWorkbenchWindow cur)
		{
			if (lastActiveWindows.Count == 0)
				return false;
			IWorkbenchWindow last = null;
			do {
				last = lastActiveWindows.Last.Value;
				lastActiveWindows.RemoveLast ();
			} while (lastActiveWindows.Count > 0 && (last == cur || last == null || (last != null && last.ViewContent == null)));

			if (last != null && last != cur) {
				last.SelectWindow ();
				return true;
			}
			return false;
		}
		
		void CloseWindowEvent (object sender, WorkbenchWindowEventArgs e)
		{
			SdiWorkspaceWindow f = (SdiWorkspaceWindow) sender;
			lastActiveWindows.Remove (f);

			if (f.ViewContent != null) {
				CloseContent (f.ViewContent);
				if (e.WasActive && !SelectLastActiveWindow (f))
					OnActiveWindowChanged(this, null);
			}
			lastActiveWindows.Remove (f);
		}
		
		void CloseClicked (object o, TabEventArgs e)
		{
			((SdiWorkspaceWindow)e.Tab.Content).CloseWindow (false, true);
		}

		internal void RemoveTab (int pageNum, bool animate)
		{
			try {
				// Weird switch page events are fired when a tab is removed.
				// This flag avoids unneeded events.
				ignorePageSwitch = true;
				IWorkbenchWindow w = ActiveWorkbenchWindow;
				tabControl.RemoveTab (pageNum, animate);
				ignorePageSwitch = false;
				if (w != ActiveWorkbenchWindow)
					OnActiveWindowChanged (null, null);
			} finally {
				ignorePageSwitch = false;
			}
		}
		
		#endregion

		#region Dock Item management
		
		public IPadWindow GetPadWindow (PadCodon content)
		{
			IPadWindow w;
			padWindows.TryGetValue (content, out w);
			return w;
		}
		
		public bool IsVisible (PadCodon padContent)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null)
				return item.Visible;
			return false;
		}
		
		public bool IsContentVisible (PadCodon padContent)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null)
				return item.ContentVisible;
			return false;
		}
		
		public void HidePad (PadCodon padContent)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null) 
				item.Visible = false;
		}
		
		public void ActivatePad (PadCodon padContent, bool giveFocus)
		{
			WelcomePage.WelcomePageService.HideWelcomePage ();

			DockItem item = GetDockItem (padContent);
			if (item != null)
				item.Present (giveFocus);
		}
		
		public bool IsSticky (PadCodon padContent)
		{
			DockItem item = GetDockItem (padContent);
			return item != null && (item.Behavior & DockItemBehavior.Sticky) != 0;
		}

		public void SetSticky (PadCodon padContent, bool sticky)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null) {
				if (sticky)
					item.Behavior |= DockItemBehavior.Sticky;
				else
					item.Behavior &= ~DockItemBehavior.Sticky;
			}
		}
		
		internal DockItem GetDockItem (PadCodon content)
		{
			if (padContentCollection.Contains (content)) {
				DockItem item = dock.GetItem (content.PadId);
				return item;
			}
			return null;
		}
		
		void CreatePadContent (bool force, PadCodon padCodon, PadWindow window, DockItem item)
		{
			if (force || item.Content == null) {
				IPadContent newContent = padCodon.InitializePadContent (window);

				Gtk.Widget pcontent;
				if (newContent is Widget) {
					pcontent = newContent.Control;
				} else {
					PadCommandRouterContainer crc = new PadCommandRouterContainer (window, newContent.Control, newContent, true);
					crc.Show ();
					pcontent = crc;
				}
				
				PadCommandRouterContainer router = new PadCommandRouterContainer (window, pcontent, toolbarFrame, false);
				router.Show ();
				item.Content = router;
			}
		}
		
		string ToDockLocation (string loc)
		{
			string location = "";
			foreach (string s in loc.Split (' ')) {
				if (string.IsNullOrEmpty (s))
					continue;
				if (location.Length > 0)
					location += ";";
				if (s.IndexOf ('/') == -1)
					location += "__" + s.ToLower () + "/CenterBefore";
				else
					location += s;
			}
			return location;
		}
		
		void AddPad (PadCodon padCodon, string placement, DockItemStatus defaultStatus)
		{
			PadWindow window = new PadWindow (this, padCodon);
			window.Icon = padCodon.Icon;
			padWindows [padCodon] = window;
			padCodons [window] = padCodon;
			
			window.StatusChanged += new EventHandler (UpdatePad);
			
			string location = ToDockLocation (placement);
			
			DockItem item = dock.AddItem (padCodon.PadId);
			item.Label = GettextCatalog.GetString (padCodon.Label);
			item.Icon = ImageService.GetPixbuf (padCodon.Icon, IconSize.Menu);
			item.DefaultLocation = location;
			item.DefaultVisible = false;
			item.DefaultStatus = defaultStatus;
			item.DockLabelProvider = padCodon;
			window.Item = item;
			
			if (padCodon.Initialized) {
				CreatePadContent (true, padCodon, window, item);
			} else {
				item.ContentRequired += delegate {
					CreatePadContent (false, padCodon, window, item);
				};
			}
			
			item.VisibleChanged += delegate {
				if (item.Visible)
					window.NotifyShown ();
				else
					window.NotifyHidden ();
			};
			
			item.ContentVisibleChanged += delegate {
				if (item.ContentVisible)
					window.NotifyContentShown ();
				else
					window.NotifyContentHidden ();
			};
			
			if (!padContentCollection.Contains (padCodon))
				padContentCollection.Add (padCodon);
		}
		
		void UpdatePad (object source, EventArgs args)
		{
			IPadWindow window = (IPadWindow) source;
			if (!padCodons.ContainsKey (window)) 
				return;
			PadCodon codon = padCodons [window];
			DockItem item = GetDockItem (codon);
			if (item != null) {
				string windowTitle = GettextCatalog.GetString (window.Title); 
				if (String.IsNullOrEmpty (windowTitle)) 
					windowTitle = GettextCatalog.GetString (codon.Label);
				if (window.HasErrors && !window.ContentVisible)
					windowTitle = "<span foreground='red'>" + windowTitle + "</span>";
				else if (window.HasNewData && !window.ContentVisible)
					windowTitle = "<b>" + windowTitle + "</b>";
				item.Label = windowTitle;
				item.Icon  = ImageService.GetPixbuf (window.Icon, IconSize.Menu);
			}
		}
		
		#endregion

		#region ICommandRouter implementation

		object ICommandRouter.GetNextCommandTarget ()
		{
			return toolbar;
		}

		#endregion
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
			wb.BringToFront (pad, true);
		}
	}

	class PadCommandRouterContainer: CommandRouterContainer
	{
		public PadCommandRouterContainer (PadWindow window, Gtk.Widget child, object target, bool continueToParent): base (child, target, continueToParent)
		{
		}
	}
	
	// The SdiDragNotebook class allows redirecting the command route to the ViewCommandHandler
	// object of the selected document, which implement some default commands.
	
	class SdiDragNotebook: DockNotebook, ICommandDelegatorRouter, ICommandBar
	{
		public SdiDragNotebook ()
		{
			NextButtonClicked += delegate {
				IdeApp.CommandService.DispatchCommand (Ide.Commands.NavigationCommands.NavigateForward);
			};
			PreviousButtonClicked += delegate {
				IdeApp.CommandService.DispatchCommand (Ide.Commands.NavigationCommands.NavigateBack);
			};
			IdeApp.CommandService.RegisterCommandBar (this);
		}
		
		public object GetNextCommandTarget ()
		{
			return Parent;
		}

		public object GetDelegatedCommandTarget ()
		{
			SdiWorkspaceWindow win = CurrentTab != null ? (SdiWorkspaceWindow) CurrentTab.Content : null;
			return win != null ? win.CommandHandler : null;
		}
		
		#region ICommandBar implementation
		bool isEnabled = true;

		void ICommandBar.Update (object activeTarget)
		{
			var ci = IdeApp.CommandService.GetCommandInfo (Ide.Commands.NavigationCommands.NavigateForward);
			NextButtonEnabled = isEnabled && ci.Enabled && ci.Visible;

			ci = IdeApp.CommandService.GetCommandInfo (Ide.Commands.NavigationCommands.NavigateBack);
			PreviousButtonEnabled = isEnabled && ci.Enabled && ci.Visible;
		}

		void ICommandBar.SetEnabled (bool enabled)
		{
			isEnabled = enabled;
		}
		#endregion
	}
}