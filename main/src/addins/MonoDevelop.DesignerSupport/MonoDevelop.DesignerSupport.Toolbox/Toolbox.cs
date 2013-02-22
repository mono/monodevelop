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
using System.Linq;
using System.Collections.Generic;
using Gtk;
using System.Drawing.Design;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide;
using Mono.TextEditor;
using MonoDevelop.Components;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public class Toolbox : VBox, IPropertyPadProvider, IToolboxConfiguration
	{
		ToolboxService toolboxService;
		
		ItemToolboxNode selectedNode;
	//	Hashtable expandedCategories = new Hashtable ();
		
		ToolboxWidget toolboxWidget;
		ScrolledWindow scrolledWindow;
		
		ToggleButton catToggleButton;
		ToggleButton compactModeToggleButton;
		SearchEntry filterEntry;
		MonoDevelop.Ide.Gui.PadFontChanger fontChanger;
		IPadWindow container;
		Dictionary<string,int> categoryPriorities = new Dictionary<string, int> ();
		Button toolboxAddButton;
		
		public Toolbox (ToolboxService toolboxService, IPadWindow container)
		{			
			this.toolboxService = toolboxService;
			this.container = container;
			
			#region Toolbar
			DockItemToolbar toolbar = container.GetToolbar (PositionType.Top);
		
			filterEntry = new SearchEntry();
			filterEntry.Ready = true;
			filterEntry.HasFrame = true;
			filterEntry.WidthRequest = 150;
			filterEntry.Changed += new EventHandler (filterTextChanged);
			filterEntry.Show ();

			toolbar.Add (filterEntry, true);
			
			catToggleButton = new ToggleButton ();
			catToggleButton.Image = new Image (ImageService.GetPixbuf ("md-design-categorise", IconSize.Menu));
			catToggleButton.Toggled += new EventHandler (toggleCategorisation);
			catToggleButton.TooltipText = GettextCatalog.GetString ("Show categories");
			toolbar.Add (catToggleButton);
			
			compactModeToggleButton = new ToggleButton ();
			compactModeToggleButton.Image = new Image (ImageService.GetPixbuf ("md-design-listboxtoggle", IconSize.Menu));
			compactModeToggleButton.Toggled += new EventHandler (ToggleCompactMode);
			compactModeToggleButton.TooltipText = GettextCatalog.GetString ("Use compact display");
			toolbar.Add (compactModeToggleButton);
	
			toolboxAddButton = new Button (new Gtk.Image (Gtk.Stock.Add, IconSize.Menu));
			toolbar.Add (toolboxAddButton);
			toolboxAddButton.TooltipText = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.Clicked += new EventHandler (toolboxAddButton_Clicked);
			toolbar.ShowAll ();

			#endregion
			
			toolboxWidget = new ToolboxWidget ();
			toolboxWidget.SelectedItemChanged += delegate {
				selectedNode = this.toolboxWidget.SelectedItem != null ? this.toolboxWidget.SelectedItem.Tag as ItemToolboxNode : null;
				toolboxService.SelectItem (selectedNode);
			};
			this.toolboxWidget.DragBegin += delegate(object sender, Gtk.DragBeginArgs e) {
				
				if (this.toolboxWidget.SelectedItem != null) {
					this.toolboxWidget.HideTooltipWindow ();
					toolboxService.DragSelectedItem (this.toolboxWidget, e.Context);
				}
			};
			this.toolboxWidget.ActivateSelectedItem += delegate {
				toolboxService.UseSelectedItem ();
			};
			
			fontChanger = new MonoDevelop.Ide.Gui.PadFontChanger (toolboxWidget, toolboxWidget.SetCustomFont, toolboxWidget.QueueResize);
			
			this.toolboxWidget.DoPopupMenu = ShowPopup;
			
			scrolledWindow = new MonoDevelop.Components.CompactScrolledWindow ();
			base.PackEnd (scrolledWindow, true, true, 0);
			base.FocusChain = new Gtk.Widget [] { scrolledWindow };
			
			//Initialise self
			scrolledWindow.ShadowType = ShadowType.None;
			scrolledWindow.VscrollbarPolicy = PolicyType.Automatic;
			scrolledWindow.HscrollbarPolicy = PolicyType.Never;
			scrolledWindow.WidthRequest = 150;
			scrolledWindow.Add (this.toolboxWidget);
			
			//update view when toolbox service updated
			toolboxService.ToolboxContentsChanged += delegate { Refresh (); };
			toolboxService.ToolboxConsumerChanged += delegate { Refresh (); };
			Refresh ();
			
			//set initial state
			this.toolboxWidget.ShowCategories = catToggleButton.Active = true;
			compactModeToggleButton.Active = MonoDevelop.Core.PropertyService.Get ("ToolboxIsInCompactMode", false);
			this.toolboxWidget.IsListMode  = !compactModeToggleButton.Active;
			this.ShowAll ();
		}
		
		#region Toolbar event handlers
		
		void ToggleCompactMode (object sender, EventArgs e)
		{
			this.toolboxWidget.IsListMode = !compactModeToggleButton.Active;
			MonoDevelop.Core.PropertyService.Set ("ToolboxIsInCompactMode", compactModeToggleButton.Active);
		}
		
		void toggleCategorisation (object sender, EventArgs e)
		{
			this.toolboxWidget.ShowCategories = catToggleButton.Active;
		}
		
		void filterTextChanged (object sender, EventArgs e)
		{
			foreach (Category cat in toolboxWidget.Categories) {
				bool hasVisibleChild = false;
				foreach (Item child in cat.Items) {
					child.IsVisible = ((ItemToolboxNode) child.Tag).Filter (filterEntry.Entry.Text);
					hasVisibleChild |= child.IsVisible;
				}
				cat.IsVisible = hasVisibleChild;
			}
			toolboxWidget.QueueDraw ();
			toolboxWidget.QueueResize ();
		}
		
		void toolboxAddButton_Clicked (object sender, EventArgs e)
		{
			toolboxService.AddUserItems ();
		}
		
		void ShowPopup (Gdk.EventButton evt)
		{
			if (!AllowEditingComponents)
				return;
			CommandEntrySet eset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/DesignerSupport/ToolboxItemContextMenu");
			IdeApp.CommandService.ShowContextMenu (this, evt, eset, this);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		internal void OnDeleteItem ()
		{
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to remove the selected Item?"), AlertButton.Delete))
				toolboxService.RemoveUserItem (selectedNode);
		}

		[CommandUpdateHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		internal void OnUpdateDeleteItem (CommandInfo info)
		{
			// Hack manually filter out gtk# widgets & container since they cannot be re added
			// because they're missing the toolbox attributes.
			info.Enabled = selectedNode != null
				&& (selectedNode.ItemDomain != GtkWidgetDomain
				    || (selectedNode.Category != "Widgets" && selectedNode.Category != "Container"));
		}
		
		static readonly string GtkWidgetDomain = GettextCatalog.GetString ("GTK# Widgets");
		
		#endregion
		
		#region GUI population
		
		Dictionary<string, Category> categories = new Dictionary<string, Category> ();
		void AddItems (IEnumerable<ItemToolboxNode> nodes)
		{
			foreach (ItemToolboxNode itbn in nodes) {
				Item newItem = new Item (itbn);
				if (!categories.ContainsKey (itbn.Category)) {
					var cat = new Category (itbn.Category);
					int prio;
					if (!categoryPriorities.TryGetValue (itbn.Category, out prio))
						prio = -1;
					cat.Priority = prio;
					categories[itbn.Category] = cat;
				}
				if (newItem.Text != null)
					categories[itbn.Category].Add (newItem);
			}
		}
		
		public void Refresh ()
		{
			// GUI assert here is to catch Bug 434065 - Exception while going to the editor
			DispatchService.AssertGuiThread ();
			
			if (toolboxService.Initializing) {
				toolboxWidget.CustomMessage = GettextCatalog.GetString ("Initializing...");
				return;
			}
			
			ConfigureToolbar ();
			
			toolboxWidget.CustomMessage = null;
			
			categories.Clear ();
			AddItems (toolboxService.GetCurrentToolboxItems ());
			
			Drag.SourceUnset (toolboxWidget);
			toolboxWidget.ClearCategories ();
			
			var cats = categories.Values.ToList ();
			cats.Sort ((a,b) => a.Priority != b.Priority ? a.Priority.CompareTo (b.Priority) : a.Text.CompareTo (b.Text));
			cats.Reverse ();
			foreach (Category category in cats) {
				category.IsExpanded = true;
				toolboxWidget.AddCategory (category);
			}
			toolboxWidget.QueueResize ();
			Gtk.TargetEntry[] targetTable = toolboxService.GetCurrentDragTargetTable ();
			if (targetTable != null)
				Drag.SourceSet (toolboxWidget, Gdk.ModifierType.Button1Mask, targetTable, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			compactModeToggleButton.Visible = toolboxWidget.CanIconizeToolboxCategories;
		}
		
		void ConfigureToolbar ()
		{
			// Default configuration
			categoryPriorities.Clear ();
			toolboxAddButton.Visible = true;
			
			toolboxService.Customize (container, this);
		}
		
		protected override void OnDestroyed ()
		{
			if (fontChanger != null) {
				fontChanger.Dispose ();
				fontChanger = null;
			}
			base.OnDestroyed ();
		}
		
		#endregion
		
		#region IPropertyPadProvider
		
		object IPropertyPadProvider.GetActiveComponent ()
		{
			return selectedNode;
		}

		object IPropertyPadProvider.GetProvider ()
		{
			return selectedNode;
		}

		void IPropertyPadProvider.OnEndEditing (object obj)
		{
		}

		void IPropertyPadProvider.OnChanged (object obj)
		{
		}
		
		#endregion

		#region IToolboxConfiguration implementation
		public void SetCategoryPriority (string category, int priority)
		{
			categoryPriorities[category] = priority;
		}

		public bool AllowEditingComponents {
			get {
				return toolboxAddButton.Visible;
			}
			set {
				toolboxAddButton.Visible = value;
			}
		}
		#endregion
	}
}
