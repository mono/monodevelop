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
using System.Reflection;
using System.Collections;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.ComponentModel;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public class Toolbox : VBox
	{
		ToolboxService toolboxService;
		ToolboxStore store;
		NodeView nodeView;
		ItemToolboxNode selectedNode;
		Hashtable expandedCategories = new Hashtable ();
		
		CompactToolboxView compactToolboxView;
		ScrolledWindow scrolledWindow;
		Toolbar toolbar;
		ToggleToolButton filterToggleButton;
		ToggleToolButton catToggleButton;
		ToggleToolButton compactModeToggleButton;
		Entry filterEntry;
		
		public Toolbox (ToolboxService toolboxService)
		{			
			this.toolboxService = toolboxService;
			
			#region Toolbar
			toolbar = new Toolbar ();
			toolbar.ToolbarStyle = ToolbarStyle.Icons;
			toolbar.IconSize = IconSize.Menu;
			base.PackStart (toolbar, false, false, 0);
		
			filterToggleButton = new ToggleToolButton ();
			filterToggleButton.IconWidget = new Image (Stock.Find, IconSize.Menu);
			filterToggleButton.Toggled += new EventHandler (toggleFiltering);
			toolbar.Insert (filterToggleButton, 0);
			
			catToggleButton = new ToggleToolButton ();
			catToggleButton.IconWidget = new Image ("md-design-categorise", IconSize.Menu);
			catToggleButton.Toggled += new EventHandler (toggleCategorisation);
			toolbar.Insert (catToggleButton, 1);
			
			compactModeToggleButton = new ToggleToolButton ();
			compactModeToggleButton.IconWidget = new Image ("md-design-listboxtoggle", IconSize.Menu);
			compactModeToggleButton.Toggled += new EventHandler (ToggleCompactMode);
			toolbar.Insert (compactModeToggleButton, 2);
	
			SeparatorToolItem sep = new SeparatorToolItem();
			toolbar.Insert (sep, 3);
			
			ToolButton toolboxAddButton = new ToolButton (Stock.Add);
			toolbar.Insert (toolboxAddButton, 4);
			toolboxAddButton.Clicked += new EventHandler (toolboxAddButton_Clicked);
			
			filterEntry = new Entry();
			filterEntry.WidthRequest = 150;
			filterEntry.Changed += new EventHandler (filterTextChanged);
			
			#endregion
			
			compactToolboxView = new CompactToolboxView ();
			compactToolboxView.SelectionChanged += delegate {
				selectedNode = this.compactToolboxView.CurrentlySelected as ItemToolboxNode;
				toolboxService.SelectItem (selectedNode);
			};
			compactToolboxView.DragBegin += delegate(object sender, Gtk.DragBeginArgs e) {
				toolboxService.DragSelectedItem (compactToolboxView, e.Context);
			};
			compactToolboxView.ButtonReleaseEvent += OnButtonRelease;
			
			scrolledWindow = new ScrolledWindow ();
			base.PackEnd (scrolledWindow, true, true, 0);
			
						
			//Initialise model
			
			store = new ToolboxStore ();
			
			//HACK: see #81942 (caused by #82087)
			typeof (Gtk.NodeStore).GetField ("node_hash", BindingFlags.Instance | BindingFlags.NonPublic).SetValue (store, new Hashtable ());
			
			//initialise view
			nodeView = new InternalNodeView (store);
			nodeView.Selection.Mode = SelectionMode.Single;
			nodeView.HeadersVisible = false;
			
			//cell renderers
			CellRendererPixbuf pixbufRenderer = new CellRendererPixbuf ();
			CellRendererText textRenderer = new CellRendererText ();
//			textRenderer.Ellipsize = Pango.EllipsizeMode.End;
			
			//Main column with text, icons
			TreeViewColumn col = new TreeViewColumn ();
			
			col.PackStart (pixbufRenderer, false);
			col.SetAttributes (pixbufRenderer,
			                      "pixbuf", ToolboxStore.Columns.Icon,
			                      "visible", ToolboxStore.Columns.IconVisible
//			                   , "cell-background-gdk", ToolboxStore.Columns.BackgroundColour
			                   );
			
			col.PackEnd (textRenderer, true);
			col.SetAttributes (textRenderer,
			                      "text", ToolboxStore.Columns.Label,
			                      "weight", ToolboxStore.Columns.FontWeight
//			                   ,  "cell-background-gdk", ToolboxStore.Columns.BackgroundColour
			                   );
			
			nodeView.AppendColumn (col);
			
			//Initialise self
			scrolledWindow.ShadowType = ShadowType.None;
			scrolledWindow.VscrollbarPolicy = PolicyType.Automatic;
			scrolledWindow.HscrollbarPolicy = PolicyType.Never;
			scrolledWindow.WidthRequest = 150;
			scrolledWindow.Add (nodeView);
			
			//selection events
			nodeView.NodeSelection.Changed += OnSelectionChanged;
			nodeView.RowActivated  += OnRowActivated;
			nodeView.DragBegin += OnDragBegin;

			
			//update view when toolbox service updated
			toolboxService.ToolboxContentsChanged += delegate { Refresh (); };
			toolboxService.ToolboxConsumerChanged += delegate { Refresh (); };
			Refresh ();
			
			//track expanded state of nodes
			nodeView.RowCollapsed += new RowCollapsedHandler (whenRowCollapsed);
			nodeView.RowExpanded += new RowExpandedHandler (whenRowExpanded);
			nodeView.ButtonReleaseEvent += OnButtonRelease;
			
			//set initial state
			filterToggleButton.Active = false;
			catToggleButton.Active = true;
			
			this.ShowAll ();
		}
		
		#region Toolbar event handlers
		
		void ToggleCompactMode (object sender, EventArgs e)
		{
			if (compactModeToggleButton.Active) {
				Remove (this.scrolledWindow);
				PackEnd (this.compactToolboxView, true, true, 0);
				ShowAll ();
			} else {
				Remove (this.compactToolboxView);
				PackEnd (scrolledWindow, true, true, 0);
				ShowAll ();
			}
		}
		
		void toggleFiltering (object sender, EventArgs e)
		{
			if (!filterToggleButton.Active && (base.Children.Length == 3)) {
				filterEntry.Text = "";
				base.Remove (filterEntry);
			} else if (base.Children.Length == 2) {
				base.PackStart (filterEntry, false, false, 4);
				filterEntry.Show ();
				filterEntry.GrabFocus ();
			} else throw new Exception ("Unexpected number of widgets");
		}
		
		void toggleCategorisation (object sender, EventArgs e)
		{
			store.SetCategorised (catToggleButton.Active);
			this.compactToolboxView.ShowCategories = catToggleButton.Active;
			EnsureState ();
		}
		
		private void filterTextChanged (object sender, EventArgs e)
		{
			store.SetFilter (filterEntry.Text);
			this.compactToolboxView.Filter = filterEntry.Text;
			EnsureState ();
		}
		
		void toolboxAddButton_Clicked (object sender, EventArgs e)
		{
			toolboxService.AddUserItems ();
		}
		
		private void OnButtonRelease(object sender, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3) {
				ShowPopup ();
			}
		}
		
		void ShowPopup ()
		{
			CommandEntrySet eset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/DesignerSupport/ToolboxItemContextMenu");
			IdeApp.CommandService.ShowContextMenu (eset, this);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		internal void OnDeleteItem ()
		{
			toolboxService.RemoveUserItem (selectedNode);
		}

		[CommandUpdateHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		internal void OnUpdateDeleteItem (CommandInfo info)
		{
			info.Enabled = selectedNode != null;
		}
		
		#endregion
		
		#region GUI population
		
		public void Refresh ()
		{
			store.Clear ();
			ICollection nodes = toolboxService.GetCurrentToolboxItems ();
			Gtk.Drag.SourceUnset (nodeView);
			Gtk.TargetEntry[] targetTable = toolboxService.GetCurrentDragTargetTable ();
			if (targetTable != null)
				Gtk.Drag.SourceSet (nodeView, Gdk.ModifierType.Button1Mask, targetTable, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			
			Gtk.Drag.SourceUnset (this.compactToolboxView);
			if (targetTable != null)
				Gtk.Drag.SourceSet (this.compactToolboxView, Gdk.ModifierType.Button1Mask, targetTable, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			this.compactToolboxView.SetNodes (nodes);
			
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
			selectedNode = nodeView.NodeSelection.SelectedNode as ItemToolboxNode;		
			toolboxService.SelectItem (selectedNode);
		}
		
		private void OnRowActivated (object sender, RowActivatedArgs e)
		{
			selectedNode = store.GetNode(e.Path) as ItemToolboxNode;		
			toolboxService.SelectItem (selectedNode);
			toolboxService.UseSelectedItem ();
		}
		
		void OnDragBegin (object o, Gtk.DragBeginArgs arg)
		{
			try {
				toolboxService.DragSelectedItem (nodeView, arg.Context);
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError (ex.ToString ());
			}
		}
		#endregion	
	}

	class InternalNodeView: NodeView
	{
		public InternalNodeView (NodeStore store): base (store)
		{
		}
		
		protected override void OnDragDataDelete (Gdk.DragContext context)
		{
			// This method is necessary to avoid a GTK warning about the
			// need to override drag_data_delete.
		}
	}
}
