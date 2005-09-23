// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
 
using System;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;

using Gtk;
using Gdl;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Gui.Utils;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Commands;

namespace MonoDevelop.Gui
{	
	/// <summary>
	/// This is the a Workspace with a single document interface.
	/// </summary>
	internal class SdiWorkbenchLayout : IWorkbenchLayout
	{
		static string configFile = Runtime.Properties.ConfigDirectory + "DefaultEditingLayout.xml";

		// contains the fully qualified name of the current layout (ie. Edit.Default)
		string currentLayout = "";
		// list of layout names for the current context, without the context prefix
		ArrayList layouts = new ArrayList ();

		private IWorkbench workbench;

		// current workbench context
		WorkbenchContext workbenchContext;
		
		Window wbWindow;
		Container rootWidget;
		DockToolbarFrame toolbarFrame;
		Dock dock;
		DockLayout dockLayout;
		DragNotebook tabControl;
		EventHandler contextChangedHandler;
		
		WorkbenchContextCodon[] contextCodons;
		bool initialized;

		public SdiWorkbenchLayout () {
			contextChangedHandler = new EventHandler (OnContextChanged);
		}
		
		public IWorkbenchWindow ActiveWorkbenchwindow {
			get {
				if (tabControl == null || tabControl.CurrentPage < 0 || tabControl.CurrentPage >= tabControl.NPages)  {
					return null;
				}
				return (IWorkbenchWindow)workbench.ViewContentCollection[tabControl.CurrentPage].WorkbenchWindow;
			}
		}
		
		public void Attach (IWorkbench wb)
		{
			DefaultWorkbench workbench = (DefaultWorkbench) wb;

			this.workbench = workbench;
			wbWindow = (Window) workbench;
			
			Gtk.VBox vbox = new VBox (false, 0);
			rootWidget = vbox;

			vbox.PackStart (workbench.TopMenu, false, false, 0);
			
			toolbarFrame = new CommandFrame (Runtime.Gui.CommandService.CommandManager);
			vbox.PackStart (toolbarFrame, true, true, 0);
			
			if (workbench.ToolBars != null) {
				for (int i = 0; i < workbench.ToolBars.Length; i++) {
					toolbarFrame.AddBar ((DockToolbar)workbench.ToolBars[i]);
				}
			}
			
			// Create the docking widget and add it to the window.
			dock = new Dock ();
			DockBar dockBar = new DockBar (dock);
			Gtk.HBox dockBox = new HBox (false, 5);
			dockBox.PackStart (dockBar, false, true, 0);
			dockBox.PackStart (dock, true, true, 0);
			toolbarFrame.AddContent (dockBox);

			// Create the notebook for the various documents.
			tabControl = new DragNotebook ();
			tabControl.Scrollable = true;
			tabControl.SwitchPage += new SwitchPageHandler (ActiveMdiChanged);
			tabControl.TabsReordered += new TabsReorderedHandler (OnTabsReordered);
			DockItem item = new DockItem ("Documents", "Documents",
						      DockItemBehavior.Locked | DockItemBehavior.NoGrip);
			item.PreferredWidth = -2;
			item.PreferredHeight = -2;
			item.Add (tabControl);
			item.Show ();
			dock.AddItem (item, DockPlacement.Center);

			workbench.Add (vbox);
			
			vbox.PackEnd (Runtime.Gui.StatusBar.Control, false, true, 0);
			workbench.ShowAll ();
			
			foreach (IViewContent content in workbench.ViewContentCollection)
				ShowView (content);

			// by default, the active pad collection is the full set
			// will be overriden in CreateDefaultLayout() below
			activePadCollection = workbench.PadContentCollection;

			// create DockItems for all the pads
			foreach (IPadContent content in workbench.PadContentCollection)
			{
				AddPad (content, content.DefaultPlacement);
			}
			
			CreateDefaultLayout();
			//RedrawAllComponents();
			wbWindow.Show ();

			workbench.ContextChanged += contextChangedHandler;
		}

		public IXmlConvertable CreateMemento()
		{
			if (initialized)
				return new SdiWorkbenchLayoutMemento (toolbarFrame.GetStatus ());
			else
				return new SdiWorkbenchLayoutMemento (new DockToolbarFrameStatus ());
		}
		
		public void SetMemento(IXmlConvertable memento)
		{
			initialized = true;
			SdiWorkbenchLayoutMemento m = (SdiWorkbenchLayoutMemento) memento;
			toolbarFrame.SetStatus (m.Status);
		}
		
		void OnTabsReordered (Widget widget, int oldPlacement, int newPlacement)
		{
			lock (workbench.ViewContentCollection) {
				IViewContent content = workbench.ViewContentCollection[oldPlacement];
				workbench.ViewContentCollection.RemoveAt (oldPlacement);
				workbench.ViewContentCollection.Insert (newPlacement, content);
				
			}
		}

		void OnContextChanged (object o, EventArgs e)
		{
			SwitchContext (workbench.Context);
		}

		void SwitchContext (WorkbenchContext ctxt)
		{
			PadContentCollection old = activePadCollection;
			
			// switch pad collections
			if (padCollections [ctxt] != null)
				activePadCollection = (PadContentCollection) padCollections [ctxt];
			else
				// this is so, for unkwown contexts, we get the full set of pads
				activePadCollection = workbench.PadContentCollection;

			workbenchContext = ctxt;
			
			// get the list of layouts
			string ctxtPrefix = ctxt.Id + ".";
			string[] list = dockLayout.GetLayouts (false);

			layouts.Clear ();
			foreach (string name in list) {
				if (name.StartsWith (ctxtPrefix)) {
					layouts.Add (name.Substring (ctxtPrefix.Length));
				}
			}
			
			// get the default layout for the new context from the property service
			CurrentLayout = Runtime.Properties.GetProperty
				("MonoDevelop.Gui.SdiWorkbenchLayout." + ctxt.ToString (), "Default");
			
			// make sure invalid pads for the new context are not visible
			foreach (IPadContent content in old)
			{
				if (!activePadCollection.Contains (content))
				{
					DockItem item = dock.GetItemByName (content.Id);
					if (item != null)
						item.HideItem ();
				}
			}
		}
		
		public Gtk.Widget LayoutWidget {
			get { return rootWidget; }
		}
		
		public string CurrentLayout {
			get {
				return currentLayout.Substring (currentLayout.IndexOf (".") + 1);
			}
			set {
				// save previous layout first
				if (currentLayout != "")
					dockLayout.SaveLayout (currentLayout);
				
				string newLayout = workbench.Context.Id + "." + value;

				if (layouts.Contains (value))
				{
					dockLayout.LoadLayout (newLayout);
				}
				else
				{
					if (currentLayout == "")
						// if the layout doesn't exists and we need to
						// load a layout (ie.  we've just been
						// created), load the default so old layout
						// xml files work smoothly
						dockLayout.LoadLayout (null);
					
					// the layout didn't exist, so save it and add it to our list
					dockLayout.SaveLayout (newLayout);
					layouts.Add (value);
				}
				currentLayout = newLayout;
				toolbarFrame.CurrentLayout = newLayout;

				// persist the selected layout for the current context
				Runtime.Properties.SetProperty ("MonoDevelop.Gui.SdiWorkbenchLayout." +
				                             workbenchContext.Id, value);
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
			dockLayout.DeleteLayout (layout);
		}


		// pad collection for the current workbench context
		PadContentCollection activePadCollection;

		// set of PadContentCollection objects for the different workbench contexts
		Hashtable padCollections = new Hashtable ();

		public PadContentCollection PadContentCollection {
			get {
				return activePadCollection;
			}
		}
		
		DockItem GetDockItem (IPadContent content)
		{
			if (activePadCollection.Contains (content))
			{
				DockItem item = dock.GetItemByName (content.Id);
				return item;
			}
			return null;
		}
		
		void CreateDefaultLayout()
		{
			contextCodons = (WorkbenchContextCodon[])(AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/Contexts").BuildChildItems(this)).ToArray(typeof(WorkbenchContextCodon));
			PadContentCollection collection;
			
			// Set the pads specific of each context
			foreach (WorkbenchContextCodon codon in contextCodons)
			{
				collection = new PadContentCollection ();
				WorkbenchContext ctx = WorkbenchContext.GetContext (codon.ID);
				padCollections [ctx] = collection;

				foreach (ContextPadCodon padCodon in codon.Pads) {
					IPadContent pad = workbench.PadContentCollection [padCodon.ID];
					if (pad != null)
						collection.Add (pad);
				}
			}
			
			//Runtime.LoggingService.Info(" Default Layout created.");
			dockLayout = new DockLayout (dock);
			if (System.IO.File.Exists (configFile)) {
				dockLayout.LoadFromFile (configFile);
			} else {
				dockLayout.LoadFromFile ("../data/options/DefaultEditingLayout.xml");
			}
			
			SwitchContext (workbench.Context);
		}

		public void Detach()
		{
			workbench.ContextChanged -= contextChangedHandler;

			//Runtime.LoggingService.Info("Call to SdiWorkSpaceLayout.Detach");
			dockLayout.SaveLayout (currentLayout);
			dockLayout.SaveToFile (configFile);
			rootWidget.Remove(((DefaultWorkbench)workbench).TopMenu);
			wbWindow.Remove(rootWidget);
			activePadCollection = null;
		}
		
		void GetPlacement (string placementString, out DockPlacement dockPlacement, out DockItem originItem)
		{
			// placementString can be: left, right, top, bottom, or a relative
			// position, for example: "ProjectPad/left" would show the pad at
			// the left of the project pad. When using
			// relative placements several positions can be provided. If the
			// pad can be placed in the first position, the next one will be
			// tried. For example "ProjectPad/left; bottom".
			
			dockPlacement = DockPlacement.None;
			string[] placementOptions = placementString.Split (';');
			foreach (string placementOption in placementOptions) {
				int i = placementOption.IndexOf ('/');
				if (i == -1) {
					dockPlacement = (DockPlacement) Enum.Parse (typeof(DockPlacement), placementOption, true);
					break;
				} else {
					string id = placementOption.Substring (0, i);
					originItem = dock.GetItemByName (id); 
					if (originItem != null && originItem.IsAttached) {
						dockPlacement = (DockPlacement) Enum.Parse (typeof(DockPlacement), placementOption.Substring (i+1), true);
						return;
					}
				}
			}

			if (dockPlacement != DockPlacement.None) {
				// If there is a pad in the same position, place the new one
				// over the existing one with a new tab.
				foreach (IPadContent pad in activePadCollection) {
					string[] places = pad.DefaultPlacement.Split (';');
					foreach (string p in places)
						if (string.Compare (p.Trim(), dockPlacement.ToString(), true) == 0) {
							originItem = GetDockItem (pad);
							if (originItem != null && originItem.IsAttached) {
								dockPlacement = DockPlacement.Center;
								return;
							}
						}
				}
			}
			
			originItem = null;
		}
		
		void AddPad (IPadContent content, string placement)
		{
			DockItem item = new DockItem (content.Id,
								 content.Title,
								 content.Icon,
								 DockItemBehavior.Normal);

			Gtk.Label label = item.TabLabel as Gtk.Label;
			label.UseMarkup = true;

			if (content is Widget)
				item.Add (content.Control);
			else {
				CommandRouterContainer crc = new CommandRouterContainer (content.Control, content, true);
				crc.Show ();
				item.Add (crc);
			}
				
			item.Show ();
			item.HideItem ();

			content.TitleChanged += new EventHandler (UpdatePad);
			content.IconChanged += new EventHandler (UpdatePad);
			
			DockPad (item, placement);

			if (!activePadCollection.Contains (content))
				activePadCollection.Add (content);
		}
		
		void DockPad (DockItem item, string placement)
		{
			DockPlacement dockPlacement = DockPlacement.None;
			DockItem ot = null;
			
			if (placement != null)
				GetPlacement (placement, out dockPlacement, out ot);
				
			if (dockPlacement != DockPlacement.None && dockPlacement != DockPlacement.Floating) {
				if (ot != null) {
					item.DockTo (ot, dockPlacement);
				}
				else {
					ot = dock.GetItemByName ("Documents"); 
					item.DockTo (ot, dockPlacement);
				}
			}
			else
				dock.AddItem (item, dockPlacement);
			item.Show ();
		}
		
		void UpdatePad (object source, EventArgs args)
		{
			IPadContent content = (IPadContent) source;
			DockItem item = GetDockItem (content);
			if (item != null) {
				Gtk.Label label = item.TabLabel as Gtk.Label;
				label.Markup = content.Title;
				item.LongName = content.Title;
				item.StockId = content.Icon;
			}
		}

		public void ShowPad (IPadContent content)
		{
			DockItem item = GetDockItem (content);
			if (item != null) {
			
				// TODO: ShowItem is not working properly in the
				// managed Gdl.
/*				if (item.DefaultPosition != null)
					item.ShowItem();
				else
*/					DockPad (item, content.DefaultPlacement);
			}
			else
				AddPad (content, content.DefaultPlacement);
		}
		
		public bool IsVisible (IPadContent padContent)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null)
				return item.IsAttached;
			return false;
		}
		
		public void HidePad (IPadContent padContent)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null)
				item.HideItem();
		}
		
		public void ActivatePad (IPadContent padContent)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null)
				item.Present (null);
		}
		
		public void RedrawAllComponents()
		{
			foreach (IPadContent content in ((IWorkbench)workbench).PadContentCollection) {
				DockItem item = dock.GetItemByName (content.Id);
				if (item != null)
					item.LongName = content.Title;
			}
		}
		
		public void CloseWindowEvent(object sender, EventArgs e)
		{
			// FIXME: GTKize

			SdiWorkspaceWindow f = (SdiWorkspaceWindow)sender;
			if (f.ViewContent != null) {
				((IWorkbench)wbWindow).CloseContent(f.ViewContent);
				ActiveMdiChanged(this, null);
			}

		}
		
		public IWorkbenchWindow ShowView(IViewContent content)
		{	
			Gtk.Image mimeimage = null;
			if (content.IsUntitled) {
				mimeimage = new Gtk.Image (FileIconLoader.GetPixbufForType ("gnome-fs-regular", 16));
			} else {
				mimeimage = new Gtk.Image (FileIconLoader.GetPixbufForFile (content.ContentName, 16));
			}
			
			TabLabel tabLabel = new TabLabel (new Label (), mimeimage != null ? mimeimage : new Gtk.Image (""));
			tabLabel.Button.Clicked += new EventHandler (closeClicked);
			tabLabel.Button.StateChanged += new StateChangedHandler (stateChanged);
			tabLabel.ClearFlag (WidgetFlags.CanFocus);
			SdiWorkspaceWindow sdiWorkspaceWindow = new SdiWorkspaceWindow(content, tabControl, tabLabel);

			sdiWorkspaceWindow.CloseEvent += new EventHandler(CloseWindowEvent);
			tabControl.InsertPage (sdiWorkspaceWindow, tabLabel, -1);
		
			tabLabel.Show();
			return sdiWorkspaceWindow;
		}

		void stateChanged (object o, StateChangedArgs e)
		{
			if (((Gtk.Widget)o).State == Gtk.StateType.Active)
				((Gtk.Widget)o).State = Gtk.StateType.Normal;
		}

		void closeClicked (object o, EventArgs e)
		{
			int pageNum = -1;
			Widget parent = ((Widget)o).Parent;
			foreach (Widget child in tabControl.Children) {
				if (tabControl.GetTabLabel (child) == parent) {
					pageNum = tabControl.PageNum (child);
					break;
				}
			}
			if (pageNum != -1) {
				workbench.ViewContentCollection [pageNum].WorkbenchWindow.CloseWindow (false, false, pageNum);
			}
		}

		public void RemoveTab (int pageNum) {
			tabControl.RemovePage (pageNum);
		}

		/// <summary>
		/// Moves to the next tab.
		/// </summary>          
		public void NextTab()
		{
			this.tabControl.NextPage();
		}
		
		/// <summary>
		/// Moves to the previous tab.
		/// </summary>          
		public void PreviousTab()
		{
			this.tabControl.PrevPage();
		}
		
		public void ActiveMdiChanged(object sender, SwitchPageArgs e)
		{
			try {
				if (ActiveWorkbenchwindow != null) {
					if (ActiveWorkbenchwindow.ViewContent.IsUntitled) {
						((Gtk.Window)WorkbenchSingleton.Workbench).Title = "MonoDevelop";
					} else {
						string post = String.Empty;
						if (ActiveWorkbenchwindow.ViewContent.IsDirty) {
							post = "*";
						}
						if (ActiveWorkbenchwindow.ViewContent.HasProject)
						{
							((Gtk.Window)WorkbenchSingleton.Workbench).Title = ActiveWorkbenchwindow.ViewContent.Project.Name + " - " + ActiveWorkbenchwindow.ViewContent.PathRelativeToProject + post + " - MonoDevelop";
						}
						else
						{
							((Gtk.Window)WorkbenchSingleton.Workbench).Title = ActiveWorkbenchwindow.ViewContent.ContentName + post + " - MonoDevelop";
						}
					}
				} else {
					((Gtk.Window)WorkbenchSingleton.Workbench).Title = "MonoDevelop";
				}
			} catch {
				((Gtk.Window)WorkbenchSingleton.Workbench).Title = "MonoDevelop";
			}
			if (ActiveWorkbenchWindowChanged != null) {
				ActiveWorkbenchWindowChanged(this, e);
			}
		}
		
		public event EventHandler ActiveWorkbenchWindowChanged;
		
		
		internal class SdiWorkbenchLayoutMemento: IXmlConvertable
		{
			public DockToolbarFrameStatus Status;
			
			public SdiWorkbenchLayoutMemento (DockToolbarFrameStatus status)
			{
				Status = status;
			}
			
			public object FromXmlElement (XmlElement element)
			{
				try {
					StringReader r = new StringReader (element.OuterXml);
					XmlSerializer s = new XmlSerializer (typeof(DockToolbarFrameStatus));
					Status = (DockToolbarFrameStatus) s.Deserialize (r);
				} catch {
					Status = new DockToolbarFrameStatus ();
				}
				return this;
			}
			
			public XmlElement ToXmlElement (XmlDocument doc)
			{
				StringWriter w = new StringWriter ();
				XmlSerializer s = new XmlSerializer (typeof(DockToolbarFrameStatus));
				s.Serialize (w, Status);
				w.Close ();
				
				XmlDocumentFragment docFrag = doc.CreateDocumentFragment();
				docFrag.InnerXml = w.ToString ();
				return docFrag ["DockToolbarFrameStatus"];
			}
		}
	}
	
}
