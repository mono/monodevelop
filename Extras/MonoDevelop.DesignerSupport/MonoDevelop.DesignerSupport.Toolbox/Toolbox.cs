 /* 
 * Toolbox.cs - A toolbox widget
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using Gtk;
using System.Collections;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.ComponentModel;
using AspNetEdit.Editor.ComponentModel;

namespace AspNetEdit.Gui.Toolbox
{
	public class Toolbox : VBox
	{
		private ServiceContainer parentServices;
		ToolboxService toolboxService;
		ToolboxStore store;
		NodeView nodeView;
		BaseToolboxNode selectedNode;
		Hashtable expandedCategories = new Hashtable ();
		
		private ScrolledWindow scrolledWindow;
		private Toolbar toolbar;
		private ToggleToolButton filterToggleButton;
		private ToggleToolButton catToggleButton;
		private Entry filterEntry;
		
		public Toolbox(ServiceContainer parentServices)
		{			
			this.parentServices = parentServices;

			//we need this service, so create it if not present
			toolboxService = parentServices.GetService (typeof (IToolboxService)) as ToolboxService;
			if (toolboxService == null) {
				toolboxService = new ToolboxService ();
				parentServices.AddService (typeof (IToolboxService), toolboxService);
			}
			
			#region Toolbar
			toolbar = new Toolbar ();
			toolbar.ToolbarStyle = ToolbarStyle.Icons;
			toolbar.IconSize = IconSize.SmallToolbar;
			base.PackStart (toolbar, false, false, 0);
		
			filterToggleButton = new ToggleToolButton ();
			filterToggleButton.IconWidget = new Image (Stock.MissingImage, IconSize.SmallToolbar);
			filterToggleButton.Toggled += new EventHandler (toggleFiltering);
			toolbar.Insert (filterToggleButton, 0);
			
			catToggleButton = new ToggleToolButton ();
			catToggleButton.IconWidget = new Image (Stock.MissingImage, IconSize.SmallToolbar);
			catToggleButton.Toggled += new EventHandler (toggleCategorisation);
			toolbar.Insert (catToggleButton, 1);
			
			SeparatorToolItem sep = new SeparatorToolItem();
			toolbar.Insert (sep, 2);
			
			filterEntry = new Entry();
			filterEntry.WidthRequest = 150;
			filterEntry.Changed += new EventHandler (filterTextChanged);
			
			#endregion
			
			scrolledWindow = new ScrolledWindow ();
			base.PackEnd (scrolledWindow, true, true, 0);
			
						
			//Initialise model
			
			store = new ToolboxStore ();
			
			//initialise view
			nodeView = new NodeView (store);
			nodeView.Selection.Mode = SelectionMode.Single;
			nodeView.HeadersVisible = false;
			
			//cell renderers
			CellRendererPixbuf pixbufRenderer = new CellRendererPixbuf ();
			CellRendererText textRenderer = new CellRendererText ();
			textRenderer.Ellipsize = Pango.EllipsizeMode.End;
			
			//Main column with text, icons
			TreeViewColumn col = new TreeViewColumn ();
			
			col.PackStart (pixbufRenderer, false);
			col.SetAttributes (pixbufRenderer,
			                      "pixbuf", ToolboxStore.Columns.Icon,
			                      "visible", ToolboxStore.Columns.IconVisible,
			                      "cell-background-gdk", ToolboxStore.Columns.BackgroundColour);
			
			col.PackEnd (textRenderer, true);
			col.SetAttributes (textRenderer,
			                      "text", ToolboxStore.Columns.Label,
			                      "weight", ToolboxStore.Columns.FontWeight,
			                      "cell-background-gdk", ToolboxStore.Columns.BackgroundColour);
			
			nodeView.AppendColumn (col);
			
			//Initialise self
			scrolledWindow.VscrollbarPolicy = PolicyType.Automatic;
			scrolledWindow.HscrollbarPolicy = PolicyType.Never;
			scrolledWindow.WidthRequest = 150;
			scrolledWindow.AddWithViewport (nodeView);
			
			//selection events
			nodeView.NodeSelection.Changed += OnSelectionChanged;
			nodeView.RowActivated  += OnRowActivated;
			
			//update view when toolbox service updated
			toolboxService.ToolboxChanged += new EventHandler (tbsChanged);
			Refresh ();
			
			//track expanded state of nodes
			nodeView.RowCollapsed += new RowCollapsedHandler (whenRowCollapsed);
			nodeView.RowExpanded += new RowExpandedHandler (whenRowExpanded);
			
			//set initial state
			filterToggleButton.Active = false;
			catToggleButton.Active = true;
		}
		
		private void tbsChanged (object sender, EventArgs e)
		{
			Refresh ();
		}
		
		#region Toolbar event handlers
		
		private void toggleFiltering (object sender, EventArgs e)
		{
			if (!filterToggleButton.Active && (base.Children.Length == 3)) {
				filterEntry.Text = "";
				base.Remove (filterEntry);
			}
			else if (base.Children.Length == 2) {
				base.PackStart (filterEntry, false, false, 4);
				filterEntry.Show ();
				filterEntry.GrabFocus ();
			}
			else throw new Exception ("Unexpected number of widgets");
		}
		
		private void toggleCategorisation (object sender, EventArgs e)
		{
			store.SetCategorised (catToggleButton.Active);
			EnsureState ();
			
		}
		
		private void filterTextChanged (object sender, EventArgs e)
		{
			store.SetFilter (filterEntry.Text);
			EnsureState ();
		}
		
		#endregion
		
		#region GUI population
		
		public void Refresh ()
		{
			Repopulate (true);
		}
		
		private void Repopulate	(bool categorised)
		{
			IDesignerHost host = parentServices.GetService (typeof (IDesignerHost)) as IDesignerHost;
			IToolboxService toolboxService = parentServices.GetService (typeof (IToolboxService)) as IToolboxService;
			if (toolboxService == null || host == null) return;
			
			store.Clear ();
			
			ToolboxItemCollection tools = toolboxService.GetToolboxItems (host);
			if (tools == null) return;
			
			ArrayList nodes = new ArrayList (tools.Count);
			
			CategoryNameCollection catNames = toolboxService.CategoryNames;
				
			foreach (string name in catNames) {				
				tools = toolboxService.GetToolboxItems (name, host);
				foreach (ToolboxItem ti in tools) {
					ToolboxItemToolboxNode node = new ToolboxItemToolboxNode (ti);
					node.Category = name;
					nodes.Add (node);
				}
			}
			
			store.SetNodes (nodes);
			EnsureState ();
		}
		
		#endregion
		
		#region Maintain state
		
		private void EnsureState ()
		{
			if (store.Categorised) {
				//LAMESPEC: why can't we just get a TreePath or a count from the NodeStore?
				TreePath tp = new Gtk.TreePath ("0");
				CategoryToolboxNode node = (CategoryToolboxNode) store.GetNode (tp);
				while (node != null) {
					if (expandedCategories [node.Label] != null)
						nodeView.ExpandRow (tp, false);
					tp.Next ();
					node = (CategoryToolboxNode) store.GetNode (tp);
				}
			}
			
			if (selectedNode != null) {
				//LAMESPEC: why oh why is there no easy way to find if a node is in the store?
				//FIXME: This doesn't survive all store rebuilds, for some reason
				foreach (BaseToolboxNode b in store)
					if (b == selectedNode) {
						nodeView.NodeSelection.SelectNode (selectedNode);
						break;
					}
			}
		}
		
		private void whenRowCollapsed (object o, RowCollapsedArgs rca)
		{
			CategoryToolboxNode node =  store.GetNode (rca.Path) as CategoryToolboxNode;
			if (node != null)
			        expandedCategories [node.Label] = null;
		}
		
		private void whenRowExpanded (object o, RowExpandedArgs rea)
		{
			CategoryToolboxNode node =  store.GetNode (rea.Path) as CategoryToolboxNode;
			if (node != null)
				expandedCategories [node.Label] = true;
		}
		
		
		#endregion
		
		#region Activation/selection handlers, drag'n'drop source, selection state
		
		private void OnSelectionChanged (object sender, EventArgs e)
		{
			selectedNode = nodeView.NodeSelection.SelectedNode as BaseToolboxNode;
			
			if (selectedNode is ToolboxItemToolboxNode) {
				//get the services
				DesignerHost host = parentServices.GetService (typeof (IDesignerHost)) as DesignerHost;
				IToolboxService toolboxService = parentServices.GetService (typeof (IToolboxService)) as IToolboxService;
				if (toolboxService == null || host == null)	return;
				
				toolboxService.SetSelectedToolboxItem (((ToolboxItemToolboxNode) selectedNode).ToolboxItem);
			}
		}
		
		private void OnRowActivated (object sender, RowActivatedArgs e)
		{
			ItemToolboxNode activatedNode = store.GetNode(e.Path) as ItemToolboxNode;
			
			DesignerHost host = parentServices.GetService (typeof (IDesignerHost)) as DesignerHost;
			IToolboxService toolboxService = parentServices.GetService (typeof (IToolboxService)) as IToolboxService;
			
			//toolboxitem needs to trigger extra events from toolboxService
			if (selectedNode is ToolboxItemToolboxNode) {
				if (toolboxService == null || host == null)	return;
				toolboxService.SetSelectedToolboxItem (((ToolboxItemToolboxNode) activatedNode).ToolboxItem);
				activatedNode.Activate (host);
				toolboxService.SelectedToolboxItemUsed ();
			}
			else {
				activatedNode.Activate (host);
			}
		}	
		#endregion	
	}
}
