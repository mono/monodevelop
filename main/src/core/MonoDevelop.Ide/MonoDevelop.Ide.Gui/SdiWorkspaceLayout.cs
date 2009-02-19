//  SdiWorkspaceLayout.cs
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
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using MonoDevelop.Core;

using Gtk;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using Mono.Addins;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.DockToolbars;
using MonoDevelop.Components.Docking;

namespace MonoDevelop.Ide.Gui
{	
	/// <summary>
	/// This is the a Workspace with a single document interface.
	/// </summary>
	internal class SdiWorkbenchLayout : IWorkbenchLayout
	{
		static string configFile = Path.Combine (PropertyService.ConfigPath, "EditingLayout.xml");
		const string fullViewModeTag = "[FullViewMode]";
		
		readonly string[] defaultVisiblePads = new string [] {
			"MonoDevelop.Ide.Gui.Pads.FileScout",
			"ClassPad",
			"ProjectPad"
		};
		
#if DUMMY_STRINGS_FOR_TRANSLATION_DO_NOT_COMPILE
		private void DoNotCompile ()
		{
			//The default layout, translated indirectly because it's used as an ID
			GettextCatalog.GetString ("Default");
		}
#endif

		// list of layout names for the current context, without the context prefix
		List<string> layouts = new List<string> ();

		DefaultWorkbench workbench;

		// current workbench context
		WorkbenchContext workbenchContext;
		
		Window wbWindow;
		Container rootWidget;
		DockToolbarFrame toolbarFrame;
		DockFrame dock;
		SdiDragNotebook tabControl;
		EventHandler contextChangedHandler;
		Dictionary<PadCodon, IPadWindow> padWindows = new Dictionary<PadCodon, IPadWindow> ();
		Dictionary<IPadWindow, PadCodon> padCodons = new Dictionary<IPadWindow, PadCodon> ();
		
		bool initialized;
		IWorkbenchWindow lastActive;
		const int MAX_LASTACTIVEWINDOWS = 10;
		LinkedList<IWorkbenchWindow> lastActiveWindows = new LinkedList<IWorkbenchWindow> ();
		bool ignorePageSwitch;
		bool isInFullViewMode = false;

		Gtk.Toolbar[] toolBars;
		Gtk.MenuBar menubar;

		public SdiWorkbenchLayout () {
			contextChangedHandler = new EventHandler (OnContextChanged);
		}
		
		public IWorkbenchWindow ActiveWorkbenchwindow {
			get {
				if (tabControl == null || tabControl.CurrentPage < 0 || tabControl.CurrentPage >= tabControl.NPages)  {
					return null;
				}
				return (IWorkbenchWindow) tabControl.CurrentPageWidget;
			}
		}
		
		Gtk.VBox fullViewVBox = new VBox (false, 0);
		DockItem documentDockItem;
		
		public void Attach (IWorkbench wb)
		{
			DefaultWorkbench workbench = (DefaultWorkbench) wb;

			this.workbench = workbench;
			wbWindow = (Window) workbench;
			
			rootWidget = fullViewVBox;
			
			InstallMenuBar ();
			
			toolbarFrame = new CommandFrame (IdeApp.CommandService);
			fullViewVBox.PackStart (toolbarFrame, true, true, 0);
			
			if (workbench.ToolBars != null) {
				for (int i = 0; i < workbench.ToolBars.Length; i++) {
					toolbarFrame.AddBar ((DockToolbar)workbench.ToolBars[i]);
				}
			}
			
			toolBars = workbench.ToolBars;
			
			// Create the docking widget and add it to the window.
			dock = new DockFrame ();
			toolbarFrame.AddContent (dock);

			// Create the notebook for the various documents.
			tabControl = new SdiDragNotebook ();
			tabControl.Scrollable = true;
			tabControl.SwitchPage += new SwitchPageHandler (ActiveMdiChanged);
			tabControl.ButtonPressEvent += delegate(object sender, ButtonPressEventArgs e) {
				int tab = tabControl.FindTabAtPosition (e.Event.XRoot, e.Event.YRoot);
				if (tab < 0)
					return;
				tabControl.CurrentPage = tab;
				if (e.Event.Type == Gdk.EventType.TwoButtonPress)
					ToggleFullViewMode ();
			};
			
			this.tabControl.PopupMenu += delegate {
				ShowPopup ();
			};
			this.tabControl.ButtonReleaseEvent += delegate (object sender, Gtk.ButtonReleaseEventArgs e) {
				int tab = tabControl.FindTabAtPosition (e.Event.XRoot, e.Event.YRoot);
				if (tab < 0)
					return;
				if (e.Event.Button == 3)
					ShowPopup ();
			};
			
			tabControl.TabsReordered += new TabsReorderedHandler (OnTabsReordered);

			// The main document area
			documentDockItem = dock.AddItem ("Documents");
			documentDockItem.Behavior = DockItemBehavior.Locked;
			documentDockItem.Expand = true;
			documentDockItem.DrawFrame = false;
			documentDockItem.Label = GettextCatalog.GetString ("Documents");
			documentDockItem.Content = tabControl;
			
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

			workbench.Add (fullViewVBox);
			fullViewVBox.ShowAll ();
			fullViewVBox.PackEnd (IdeApp.Workbench.StatusBar, false, true, 0);
			
			foreach (IViewContent content in workbench.ViewContentCollection)
				ShowView (content);

			// by default, the active pad collection is the full set
			// will be overriden in CreateDefaultLayout() below
			activePadCollection = new List<MonoDevelop.Ide.Codons.PadCodon> (workbench.PadContentCollection);

			// create DockItems for all the pads
			foreach (PadCodon content in workbench.PadContentCollection)
			{
				AddPad (content, content.DefaultPlacement);
			}

			
			CreateDefaultLayout();

			workbench.ContextChanged += contextChangedHandler;
		}
		
		void ShowPopup ()
		{
			Gtk.Menu contextMenu = IdeApp.CommandService.CreateMenu ("/MonoDevelop/Ide/ContextMenu/DocumentTab");
			if (contextMenu != null)
				contextMenu.Popup ();
		}
		
		public void ToggleFullViewMode ()
		{
			isInFullViewMode = !isInFullViewMode;
			this.tabControl.LeaveDragMode (0);
			
			if (isInFullViewMode) {
				string fullViewLayout = workbench.Context.Id + "." + CurrentLayout + fullViewModeTag;
				if (!dock.HasLayout (fullViewLayout))
					dock.CreateLayout (fullViewLayout, true);
				dock.CurrentLayout = fullViewLayout;
				foreach (DockItem it in dock.GetItems ()) {
					if (it.Behavior != DockItemBehavior.Locked && it.Visible)
						it.Status = DockItemStatus.AutoHide;
				}
			} else {
				dock.CurrentLayout = workbench.Context.Id + "." + CurrentLayout;
			}
		}
		
		public ICustomXmlSerializer CreateMemento()
		{
			return new SdiWorkbenchLayoutMemento (initialized ? toolbarFrame.GetStatus () : new DockToolbarFrameStatus ()).ToProperties ();
		}
		
		public void SetMemento (ICustomXmlSerializer memento)
		{
			initialized = true;
			SdiWorkbenchLayoutMemento m = new SdiWorkbenchLayoutMemento ((Properties)memento);
			toolbarFrame.SetStatus (m.Status);
		}
		
		void OnTabsReordered (Widget widget, int oldPlacement, int newPlacement)
		{
			IdeApp.Workbench.ReorderDocuments (oldPlacement, newPlacement);
		}

		void OnContextChanged (object o, EventArgs e)
		{
			SwitchContext (workbench.Context);
		}

		void SwitchContext (WorkbenchContext ctxt)
		{
			List<PadCodon> old = activePadCollection;
			
			// switch pad collections
			if (padCollections [ctxt] != null)
				activePadCollection = padCollections [ctxt];
			else
				// this is so, for unkwown contexts, we get the full set of pads
				activePadCollection = new List<MonoDevelop.Ide.Codons.PadCodon> (workbench.PadContentCollection);

			workbenchContext = ctxt;
			
			// get the list of layouts
			string ctxtPrefix = ctxt.Id + ".";

			layouts.Clear ();
			foreach (string name in dock.Layouts) {
				if (name.StartsWith (ctxtPrefix) && !name.EndsWith (fullViewModeTag)) {
					layouts.Add (name.Substring (ctxtPrefix.Length));
				}
			}
			
			// get the default layout for the new context from the property service
			CurrentLayout = PropertyService.Get
				("MonoDevelop.Core.Gui.SdiWorkbenchLayout." + ctxt.Id, "Default");
			
			// make sure invalid pads for the new context are not visible
			foreach (PadCodon content in old)
			{
				if (!activePadCollection.Contains (content))
				{
					DockItem item = dock.GetItem (content.PadId);
					if (item != null)
						item.Visible = false;
				}
			}
		}
		
		public Gtk.Widget LayoutWidget {
			get { return rootWidget; }
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
				// Leave dragging mode, to avoid problems due to widget relocating
				tabControl.LeaveDragMode (0);
				isInFullViewMode = false;
				
				string newLayout = workbench.Context.Id + "." + value;
				if (!((IList)dock.Layouts).Contains (newLayout)) {
					dock.CreateLayout (newLayout, true);
					layouts.Add (value);
				}
				dock.CurrentLayout = newLayout;
				toolbarFrame.CurrentLayout = newLayout;

				// persist the selected layout for the current context
				PropertyService.Set ("MonoDevelop.Core.Gui.SdiWorkbenchLayout." +
				                                workbenchContext.Id, 
				                                value);
			}
		}

		public string[] Layouts {
			get {
				string[] result = new string [layouts.Count];
				layouts.CopyTo (result);
				return result;
			}
		}
		
		public void DeleteLayout (string name)
		{
			string layout = workbench.Context.Id + "." + name;
			layouts.Remove (name);
			dock.DeleteLayout (layout);
		}


		// pad collection for the current workbench context
		List<PadCodon> activePadCollection;

		// set of PadContentCollection objects for the different workbench contexts
		Dictionary<WorkbenchContext, List<PadCodon>> padCollections = new Dictionary<WorkbenchContext, List<PadCodon>> ();

		public List<PadCodon> PadContentCollection {
			get {
				return activePadCollection;
			}
		}
		
		DockItem GetDockItem (PadCodon content)
		{
			if (activePadCollection.Contains (content))
			{
				DockItem item = dock.GetItem (content.PadId);
				return item;
			}
			return null;
		}
		
		void CreateDefaultLayout()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/WorkbenchContexts", OnExtensionChanged);
			
			try {
				if (System.IO.File.Exists (configFile)) {
					dock.LoadLayouts (configFile);
				} else if (System.IO.File.Exists ("../data/options/DefaultEditingLayout2.xml")) {
					dock.LoadLayouts ("../data/options/DefaultEditingLayout2.xml");
				} else {
					dock.CreateLayout ("Edit.Default", true);
					dock.CurrentLayout = "Edit.Default";
					DockItem it = null;
					foreach (string s in defaultVisiblePads) {
						it = dock.GetItem (s);
						if (it != null)
							it.Visible = true;
					}
					it.Present (false);
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}

		void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				WorkbenchContextCodon codon = (WorkbenchContextCodon) args.ExtensionNode;
				List<PadCodon> collection = new List<PadCodon> ();
				WorkbenchContext ctx = WorkbenchContext.GetContext (codon.Id);
				padCollections [ctx] = collection;

				foreach (ContextPadCodon padCodon in codon.Pads) {
					PadCodon pad = workbench.GetPad (padCodon.Id);
					if (pad != null)
						collection.Add (pad);
				}
			}
			else {
				WorkbenchContextCodon codon = (WorkbenchContextCodon) args.ExtensionNode;
				WorkbenchContext ctx = WorkbenchContext.GetContext (codon.Id);
				padCollections.Remove (ctx);
			}
		}
		
		public void Detach()
		{
			workbench.ContextChanged -= contextChangedHandler;

			dock.SaveLayouts (configFile);
			UninstallMenuBar ();
			wbWindow.Remove(rootWidget);
			activePadCollection = null;
		}
		
		void CreatePadContent (bool force, PadCodon padCodon, PadWindow window, DockItem item)
		{
			if (force || item.Content == null) {
				IPadContent newContent = padCodon.PadContent;
				newContent.Initialize (window);
			
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
		
		void AddPad (PadCodon padCodon, string placement)
		{
			PadWindow window = new PadWindow (this, padCodon);
			window.Icon = "md-output-icon";
			padWindows [padCodon] = window;
			padCodons [window] = padCodon;
			
			window.TitleChanged += new EventHandler (UpdatePad);
			window.IconChanged += new EventHandler (UpdatePad);
			
			string location = "";
			foreach (string s in placement.Split (' ')) {
				if (string.IsNullOrEmpty (s))
					continue;
				if (location.Length > 0)
					location += ";";
				if (s.IndexOf ('/') == -1)
					location += "__" + s.ToLower () + "/CenterBefore";
				else
					location += s;
			}
			
			string windowTitle = GettextCatalog.GetString (padCodon.Label);
			DockItem item = dock.AddItem (padCodon.PadId);
			item.Label = windowTitle;
			item.Icon = window.Icon;
			item.DefaultLocation = location;
			item.DefaultVisible = false;
			
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
			
			if (!activePadCollection.Contains (padCodon))
				activePadCollection.Add (padCodon);
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
				item.Label = windowTitle;
				item.Icon  = window.Icon;
			}
		}

		public void ShowPad (PadCodon content)
		{
			DockItem item = GetDockItem (content);
			if (item != null)
				item.Visible = true;
			else
				AddPad (content, content.DefaultPlacement);
		}
		
		public void AddPad (PadCodon content)
		{
			DockItem item = GetDockItem (content);
			if (item == null)
				AddPad (content, content.DefaultPlacement);
		}
		
		public void RemovePad (PadCodon content)
		{
			PadWindow win = (PadWindow) padWindows [content];
			win.NotifyDestroyed ();
			
			DockItem item = GetDockItem (content);
			if (item != null)
				dock.RemoveItem (item);
			padWindows.Remove (content);
			padCodons.Remove (win);
			
			foreach (List<PadCodon> pads in padCollections.Values) 
				pads.Remove (content);
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
		
		public void ActivatePad (PadCodon padContent)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null)
				item.Present (true);
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
		
		public void RedrawAllComponents()
		{
			// If the toolbar or menubar has changed, replace it in the layout
			
			DefaultWorkbench wb = (DefaultWorkbench) workbench;
			if (wb.ToolBars != toolBars) {
				string cl = toolbarFrame.CurrentLayout;
				DockToolbarFrameStatus mem = toolbarFrame.GetStatus ();
				toolBars = wb.ToolBars;
				toolbarFrame.ClearToolbars ();
				if (toolBars != null) {
					foreach (DockToolbar tb in toolBars) {
						tb.ShowAll ();
						toolbarFrame.AddBar (tb);
					}
				}
				toolbarFrame.SetStatus (mem);
				toolbarFrame.CurrentLayout = cl;
			}

			InstallMenuBar ();
		}
		
		void InstallMenuBar ()
		{
			DefaultWorkbench wb = (DefaultWorkbench) workbench;
			
			if (wb.TopMenu == menubar)
				return;
			
			if (MonoDevelop.Core.Gui.Services.PlatformService.CanInstallGlobalMenu) {
				MonoDevelop.Core.Gui.Services.PlatformService.InstallGlobalMenu (wb.TopMenu);
			}
			else {
				((VBox)rootWidget).PackStart (wb.TopMenu, false, false, 0);
				((Gtk.Box.BoxChild) rootWidget [wb.TopMenu]).Position = 0;
				wb.TopMenu.ShowAll ();
				
				if (menubar != null)
					rootWidget.Remove (menubar);
			}
			menubar = wb.TopMenu;
		}
		
		void UninstallMenuBar ()
		{
			if (menubar == null)
				return;
			
			if (MonoDevelop.Core.Gui.Services.PlatformService.CanInstallGlobalMenu) {
				MonoDevelop.Core.Gui.Services.PlatformService.UninstallGlobalMenu ();
			}
			else {
				rootWidget.Remove(((DefaultWorkbench)workbench).TopMenu);
			}
			
			menubar = null;
		}
		
		public IPadWindow GetPadWindow (PadCodon content)
		{
			return padWindows [content];
		}
		
		bool SelectLastActiveWindow ()
		{
			if (lastActiveWindows.Count == 0 || lastActive == ActiveWorkbenchwindow)
				return false;
			IWorkbenchWindow last = null;
			do {
				last = lastActiveWindows.Last.Value;
				lastActiveWindows.RemoveLast ();
			} while (lastActiveWindows.Count > 0 && (last == null || (last != null && last.ViewContent == null)));
			if (last != null)  {
				last.SelectWindow ();
				return true;
			}
			return false;
		}
		
		public void CloseWindowEvent (object sender, EventArgs e)
		{
			SdiWorkspaceWindow f = (SdiWorkspaceWindow) sender;
			
			// Unsubscribe events to avoid memory leaks
			f.TabLabel.CloseClicked -= new EventHandler (closeClicked);
			
			if (f.ViewContent != null) {
				((IWorkbench)wbWindow).CloseContent (f.ViewContent);
				if (!SelectLastActiveWindow ())
					ActiveMdiChanged(this, null);
			}
		}
		
		public IWorkbenchWindow ShowView (IViewContent content)
		{	
			Gtk.Image mimeimage = null;
			
			if (content.StockIconId != null ) {
				mimeimage = new Gtk.Image (content.StockIconId, IconSize.Menu );
			}
			else if (content.IsUntitled && content.UntitledName == null) {
				mimeimage = new Gtk.Image (IdeApp.Services.PlatformService.GetPixbufForType ("gnome-fs-regular", Gtk.IconSize.Menu));
			} else {
				mimeimage = new Gtk.Image (IdeApp.Services.PlatformService.GetPixbufForFile (content.ContentName ?? content.UntitledName, Gtk.IconSize.Menu));
			}			

			TabLabel tabLabel = new TabLabel (new Label (), mimeimage != null ? mimeimage : new Gtk.Image (""));
			tabLabel.CloseClicked += new EventHandler (closeClicked);			
			tabLabel.ClearFlag (WidgetFlags.CanFocus);
			SdiWorkspaceWindow sdiWorkspaceWindow = new SdiWorkspaceWindow (workbench, content, tabControl, tabLabel);

			sdiWorkspaceWindow.Closed += new EventHandler (CloseWindowEvent);
			tabControl.InsertPage (sdiWorkspaceWindow, tabLabel, -1);
			
			tabLabel.Show ();
			return sdiWorkspaceWindow;
		}

		void closeClicked (object o, EventArgs e)
		{
			Widget tabLabel = ((Widget)o);
			foreach (Widget child in tabControl.Children) {
				if (tabControl.GetTabLabel (child) == tabLabel) {
					int pageNum = tabControl.PageNum (child);
					((SdiWorkspaceWindow)child).CloseWindow (false, false, pageNum);
					break;
				}
			}
		}

		public void RemoveTab (int pageNum) {
			try {
				// Weird switch page events are fired when a tab is removed.
				// This flag avoids unneeded events.
				ignorePageSwitch = true;
				IWorkbenchWindow w = ActiveWorkbenchwindow;
				tabControl.RemovePage (pageNum);
				ignorePageSwitch = false;
				if (w != ActiveWorkbenchwindow)
					ActiveMdiChanged (null, null);
			} finally {
				ignorePageSwitch = false;
			}
		}

		/// <summary>
		/// Moves to the next tab.
		/// </summary>          
		public void NextTab()
		{
			this.tabControl.NextPage ();
		}
		
		/// <summary>
		/// Moves to the previous tab.
		/// </summary>          
		public void PreviousTab()
		{
			this.tabControl.PrevPage ();
		}
		
		public void ActiveMdiChanged (object sender, SwitchPageArgs e)
		{
			if (ignorePageSwitch)
				return;
			
			if (lastActive == ActiveWorkbenchwindow)
				return;
			
			if (lastActiveWindows.Count > MAX_LASTACTIVEWINDOWS)
				lastActiveWindows.RemoveFirst ();
			lastActiveWindows.AddLast (lastActive);
			
			lastActive = ActiveWorkbenchwindow;
			
			try {
				if (ActiveWorkbenchwindow != null) {
					if (ActiveWorkbenchwindow.ViewContent.IsUntitled) {
						((Gtk.Window)workbench).Title = "MonoDevelop";
					} else {
						string post = String.Empty;
						if (ActiveWorkbenchwindow.ViewContent.IsDirty) {
							post = "*";
						}
						if (ActiveWorkbenchwindow.ViewContent.Project != null)
						{
							((Gtk.Window)workbench).Title = ActiveWorkbenchwindow.ViewContent.Project.Name + " - " + ActiveWorkbenchwindow.ViewContent.PathRelativeToProject + post + " - MonoDevelop";
						}
						else
						{
							((Gtk.Window)workbench).Title = ActiveWorkbenchwindow.ViewContent.ContentName + post + " - MonoDevelop";
						}
					}
				} else {
					((Gtk.Window)workbench).Title = "MonoDevelop";
					if (isInFullViewMode) 
						this.ToggleFullViewMode ();
				}
			} catch {
				((Gtk.Window)workbench).Title = "MonoDevelop";
			}
			if (ActiveWorkbenchWindowChanged != null) {
				ActiveWorkbenchWindowChanged(this, e);
			}
		}
		
		public event EventHandler ActiveWorkbenchWindowChanged;
		
		
		internal class SdiWorkbenchLayoutMemento
		{
			Properties properties = new Properties ();
			
			public DockToolbarFrameStatus Status {
				get {
					return properties.Get ("status", new DockToolbarFrameStatus ());
				}
				set {
					properties.Set ("status", value);
				}
			}

			public Properties ToProperties ()
			{
				return properties;
			}
			
			public SdiWorkbenchLayoutMemento (Properties properties)
			{
				this.properties = properties;
			}
			public SdiWorkbenchLayoutMemento (DockToolbarFrameStatus status)
			{
				Status = status;
			}
		}
	}

	class PadCommandRouterContainer: CommandRouterContainer
	{
		PadWindow window;
		
		public PadCommandRouterContainer (PadWindow window, Gtk.Widget child, object target, bool continueToParent): base (child, target, continueToParent)
		{
			this.window = window;
		}
		
		public override object GetDelegatedCommandTarget ()
		{
			// This pad has currently the focus. Set the actve pad property.
			PadWindow.LastActivePadWindow = window;
			return base.GetDelegatedCommandTarget ();
		}

	}
	
	// The SdiDragNotebook class allows redirecting the command route to the ViewCommandHandler
	// object of the selected document, which implement some default commands.
	
	class SdiDragNotebook: DragNotebook, ICommandDelegatorRouter
	{
		public object GetNextCommandTarget ()
		{
			return Parent;
		}

		public object GetDelegatedCommandTarget ()
		{
			SdiWorkspaceWindow win = (SdiWorkspaceWindow) CurrentPageWidget;
			return win != null ? win.CommandHandler : null;
		}
	}
}
