/* 
 * MacToolbox.cs - A toolbox widget
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
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

#if MAC
using System;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using AppKit;
using CoreGraphics;
using MonoDevelop.Components;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	class MacToolbox : NSStackView, IPropertyPadProvider, IToolboxConfiguration
	{
		const string ToolboxItemContextMenuCommand = "/MonoDevelop/DesignerSupport/ToolboxItemContextMenu";

		const int IconsSpacing = 4;
		ToolboxService toolboxService;

		MacToolboxWidget toolboxWidget;

		public event EventHandler DragBegin;
		public event EventHandler DragSourceUnset;
		public event EventHandler<Gtk.TargetEntry []> DragSourceSet;

		public ItemToolboxNode SelectedNode => toolboxWidget.SelectedItem?.Node;

		NativeViews.ToggleButton catToggleButton;
		NativeViews.ToggleButton compactModeToggleButton;
		NativeViews.SearchTextField filterEntry;

		IPadWindow container;
		Dictionary<string, int> categoryPriorities = new Dictionary<string, int> ();

		NativeViews.ClickedButton toolboxAddButton;
		Xwt.Drawing.Image groupByCategoryImage;

		NSStackView horizontalStackView;

		const int buttonSizeWidth = 25;
		const int buttonSizeHeight = buttonSizeWidth;

		public MacToolbox (ToolboxService toolboxService, IPadWindow container)
		{
			Orientation = NSUserInterfaceLayoutOrientation.Vertical;
			Alignment = NSLayoutAttribute.Leading;
			Spacing = 0;
			Distribution = NSStackViewDistribution.Fill;
			AccessibilityElement = false;
			this.toolboxService = toolboxService;
			this.container = container;

			#region Toolbar

			groupByCategoryImage = ImageService.GetIcon (Stock.GroupByCategory, Gtk.IconSize.Menu);
			var compactImage = ImageService.GetIcon (Stock.CompactDisplay, Gtk.IconSize.Menu);
			var addImage = ImageService.GetIcon (Stock.Add, Gtk.IconSize.Menu);

			horizontalStackView = NativeViewHelper.CreateHorizontalStackView (IconsSpacing);
			AddArrangedSubview (horizontalStackView);

			horizontalStackView.LeftAnchor.ConstraintEqualToAnchor (LeftAnchor, 0).Active = true;
			horizontalStackView.RightAnchor.ConstraintEqualToAnchor (RightAnchor, 0).Active = true;
		
			horizontalStackView.EdgeInsets = new NSEdgeInsets (7, 7, 7, 7);

			//Horizontal container
			filterEntry = new NativeViews.SearchTextField ();
			filterEntry.AccessibilityTitle = GettextCatalog.GetString ("Search Toolbox");
			filterEntry.AccessibilityHelp = GettextCatalog.GetString ("Enter a term to search for it in the toolbox");
			filterEntry.Activated += FilterTextChanged;

			horizontalStackView.AddArrangedSubview (filterEntry);

			filterEntry.SetContentCompressionResistancePriority ((int)NSLayoutPriority.DefaultLow, NSLayoutConstraintOrientation.Horizontal);
			filterEntry.SetContentHuggingPriorityForOrientation ((int)NSLayoutPriority.DefaultLow, NSLayoutConstraintOrientation.Horizontal);

			catToggleButton = new NativeViews.ToggleButton ();
			catToggleButton.Image = groupByCategoryImage.ToNSImage ();
			catToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Show categories");
			catToggleButton.ToolTip = GettextCatalog.GetString ("Show categories");
			catToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle to show categories");
			catToggleButton.Activated += ToggleCategorisation;

			horizontalStackView.AddArrangedSubview (catToggleButton);

			catToggleButton.SetContentCompressionResistancePriority ((int)NSLayoutPriority.DefaultHigh, NSLayoutConstraintOrientation.Horizontal);
			catToggleButton.SetContentHuggingPriorityForOrientation ((int)NSLayoutPriority.DefaultHigh, NSLayoutConstraintOrientation.Horizontal);

			compactModeToggleButton = new NativeViews.ToggleButton ();
			compactModeToggleButton.Image = compactImage.ToNSImage();
			compactModeToggleButton.ToolTip = GettextCatalog.GetString ("Use compact display");
			compactModeToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Compact Layout");
			compactModeToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle for toolbox to use compact layout");
			compactModeToggleButton.Activated += ToggleCompactMode;

			horizontalStackView.AddArrangedSubview (compactModeToggleButton);

			compactModeToggleButton.SetContentCompressionResistancePriority ((int)NSLayoutPriority.DefaultHigh, NSLayoutConstraintOrientation.Horizontal);
			compactModeToggleButton.SetContentHuggingPriorityForOrientation ((int)NSLayoutPriority.DefaultHigh, NSLayoutConstraintOrientation.Horizontal);

			toolboxAddButton = new NativeViews.ClickedButton ();
			toolboxAddButton.Image = addImage.ToNSImage ();
			toolboxAddButton.AccessibilityTitle = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.AccessibilityHelp = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.ToolTip = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.Activated += ToolboxAddButton_Clicked;

			horizontalStackView.AddArrangedSubview (toolboxAddButton);

			toolboxAddButton.SetContentCompressionResistancePriority ((int)NSLayoutPriority.DefaultHigh, NSLayoutConstraintOrientation.Horizontal);
			toolboxAddButton.SetContentHuggingPriorityForOrientation ((int)NSLayoutPriority.DefaultHigh, NSLayoutConstraintOrientation.Horizontal);

			#endregion

			toolboxWidget = new MacToolboxWidget (container) {
				AccessibilityTitle = GettextCatalog.GetString ("Toolbox Toolbar"),
			};

			var scrollView = new NSScrollView () {
				HasVerticalScroller = true,
				HasHorizontalScroller = false,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			scrollView.WantsLayer = true;
			scrollView.DocumentView = toolboxWidget;
			AddArrangedSubview (scrollView);

			//update view when toolbox service updated
			toolboxService.ToolboxContentsChanged += ToolboxService_ToolboxContentsChanged;
			toolboxService.ToolboxConsumerChanged += ToolboxService_ToolboxConsumerChanged;

			filterEntry.Changed += FilterEntry_Changed;

			toolboxWidget.DragBegin += ToolboxWidget_DragBegin;
			toolboxWidget.ActivateSelectedItem += ToolboxWidget_ActivateSelectedItem;
			toolboxWidget.MenuOpened += ToolboxWidget_MenuOpened;
			toolboxWidget.RegionCollapsed += FilterTextChanged;

			//set initial state
			toolboxWidget.ShowCategories = catToggleButton.Active = true;
			compactModeToggleButton.Active = MonoDevelop.Core.PropertyService.Get ("ToolboxIsInCompactMode", false);
			toolboxWidget.IsListMode = !compactModeToggleButton.Active;
		}

		void ToolboxService_ToolboxConsumerChanged (object sender, ToolboxConsumerChangedEventArgs e)
		{
			Refresh ();
		}

		void ToolboxService_ToolboxContentsChanged (object sender, EventArgs e)
		{
			Refresh ();
		}

		void ToolboxWidget_ActivateSelectedItem (object sender, EventArgs e)
		{
			var selectedNode = SelectedNode;
			if (selectedNode != null) {
				DesignerSupport.Service.ToolboxService.SelectItem (selectedNode);
				toolboxService.UseSelectedItem ();
			}
		}

		void FilterEntry_Changed (object sender, EventArgs e)
		{
			Refilter ();
		}

		void ToolboxWidget_DragBegin (object sender, EventArgs e)
		{
			if (this.toolboxWidget.SelectedItem != null) {
				DragBegin?.Invoke (this, e);
			}
		}

		#region Toolbar event handlers

		void ToggleCompactMode (object sender, EventArgs e)
		{
			toolboxWidget.IsListMode = !compactModeToggleButton.Active;
			Refilter ();

			PropertyService.Set ("ToolboxIsInCompactMode", compactModeToggleButton.Active);

			if (compactModeToggleButton.Active) {
				compactModeToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Full Layout");
				compactModeToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle for toolbox to use full layout");
			} else {
				compactModeToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Compact Layout"); ;
				compactModeToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle for toolbox to use compact layout");
			}
		}

		void ToggleCategorisation (object sender, EventArgs e)
		{
			this.toolboxWidget.ShowCategories = catToggleButton.Active;
			Refilter ();
			if (catToggleButton.Active) {
				catToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Hide Categories");
				catToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle to hide toolbox categories");
			} else {
				catToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Show Categories");
				catToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle to show toolbox categories");
			}
		}

		void FilterTextChanged (object sender, EventArgs e)
		{
			Refilter ();
		}

		void Refilter ()
		{
			var cats = categories.Values.ToList ();
			cats.Sort ((a, b) => a.Priority != b.Priority ? b.Priority.CompareTo (a.Priority) : b.Text.CompareTo (a.Text));

			toolboxWidget.CategoryVisibilities.Clear ();
			foreach (var category in cats) {
				bool hasVisibleChild = false;

				foreach (var child in category.Items) {
					if (toolboxWidget.ShowCategories) {
						child.IsVisible = ((ItemToolboxNode)child.Tag).Filter (filterEntry.StringValue) && category.IsExpanded;
					} else {
						child.IsVisible = ((ItemToolboxNode)child.Tag).Filter (filterEntry.StringValue);
					}
					hasVisibleChild |= child.IsVisible;
				}

				category.IsVisible = hasVisibleChild;
				toolboxWidget.AddCategory (category);
			}

			toolboxWidget.RedrawItems (true, true);
		}
		
		async void ToolboxAddButton_Clicked (object sender, EventArgs e)
		{
			catToggleButton.Enabled = compactModeToggleButton.Enabled = toolboxAddButton.Enabled = false;
			await toolboxService.AddUserItems ();
			catToggleButton.Enabled = compactModeToggleButton.Enabled = toolboxAddButton.Enabled = true;
		}

		void ToolboxWidget_MenuOpened (object sender, CGPoint e)
		{
			if (!AllowEditingComponents)
				return;

			var eset = IdeApp.CommandService.CreateCommandEntrySet (ToolboxItemContextMenuCommand);
			IdeApp.CommandService.ShowContextMenu (toolboxWidget, (int)e.X, (int)e.Y, eset, this);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		internal void OnDeleteItem ()
		{
			var selectedNode = SelectedNode;
			if (selectedNode != null) {
				if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to remove the selected Item?"), AlertButton.Delete))
					toolboxService.RemoveUserItem (SelectedNode);
			}
		}

		[CommandUpdateHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		internal void OnUpdateDeleteItem (CommandInfo info)
		{
			var selectedNode = SelectedNode;
			// Hack manually filter out gtk# widgets & container since they cannot be re added
			// because they're missing the toolbox attributes.
			info.Enabled = selectedNode != null && toolboxService.CanRemoveUserItem (selectedNode)
				&& (selectedNode.ItemDomain != GtkWidgetDomain
					|| (selectedNode.Category != "Widgets" && selectedNode.Category != "Container"));
		}
		
		static readonly string GtkWidgetDomain = GettextCatalog.GetString ("GTK# Widgets");

		#endregion

		#region GUI population

		readonly List<ToolboxWidgetCategory> items = new List<ToolboxWidgetCategory> ();
		Dictionary<string, ToolboxWidgetCategory> categories = new Dictionary<string, ToolboxWidgetCategory> ();
	
		void AddItems (IEnumerable<ItemToolboxNode> nodes)
		{
			foreach (var itbn in nodes) {
				var newItem = new ToolboxWidgetItem (itbn);

				if (!categories.ContainsKey (itbn.Category)) {
					var cat = new ToolboxWidgetCategory (itbn.Category);
					int prio;
					if (!categoryPriorities.TryGetValue (itbn.Category, out prio))
						prio = -1;
					cat.Priority = prio;
					cat.IsExpanded = true;
					categories [itbn.Category] = cat;
				}
				if (newItem.Text != null)
					categories [itbn.Category].Add (newItem);
			}
		}

		public void Refresh ()
		{
			// GUI assert here is to catch Bug 434065 - Exception while going to the editor
			Runtime.AssertMainThread ();
			
			if (toolboxService.Initializing) {
				toolboxWidget.CustomMessage = GettextCatalog.GetString ("Initializing...");
				return;
			}
			
			ConfigureToolbar ();

			toolboxWidget.CustomMessage = null;

			toolboxWidget.ClearImageCache ();

			categories.Clear ();

			AddItems (toolboxService.GetCurrentToolboxItems ());

			DragSourceUnset?.Invoke (this, EventArgs.Empty);

			Gtk.TargetEntry [] targetTable = toolboxService.GetCurrentDragTargetTable ();
			if (targetTable != null)
				DragSourceSet?.Invoke (this, targetTable);

			Refilter ();

			compactModeToggleButton.Hidden = !toolboxWidget.CanIconizeToolboxCategories;
			compactModeToggleButton.InvalidateIntrinsicContentSize ();
		
			if (categories.Count == 0) {
				toolboxWidget.CustomMessage = GettextCatalog.GetString ("There are no tools available for the current document.");
			}
		}
			
		void ConfigureToolbar ()
		{
			// Default configuration
			categoryPriorities.Clear ();
			toolboxAddButton.Hidden = false;
			
			toolboxService.Customize (container, this);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				filterEntry.Activated -= FilterTextChanged;

				catToggleButton.Activated -= ToggleCategorisation;

				compactModeToggleButton.Activated -= ToggleCompactMode;

				toolboxAddButton.Activated -= ToolboxAddButton_Clicked;

				toolboxWidget.ActivateSelectedItem -= ToolboxWidget_ActivateSelectedItem;
				toolboxWidget.MenuOpened -= ToolboxWidget_MenuOpened;
				toolboxWidget.DragBegin -= ToolboxWidget_DragBegin;
				toolboxWidget.RegionCollapsed -= FilterTextChanged;

				toolboxService.ToolboxContentsChanged -= ToolboxService_ToolboxContentsChanged;
				toolboxService.ToolboxConsumerChanged -= ToolboxService_ToolboxConsumerChanged;
			}

			base.Dispose (disposing);
		}

		#endregion
		
		#region IPropertyPadProvider
		
		object IPropertyPadProvider.GetActiveComponent ()
		{
			return SelectedNode;
		}

		object IPropertyPadProvider.GetProvider ()
		{
			return SelectedNode;
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
			categoryPriorities [category] = priority;
		}

		public bool AllowEditingComponents {
			get {
				return !toolboxAddButton.Hidden;
			}
			set {
				toolboxAddButton.Hidden = !value;
			}
		}

		#endregion
	}
}
#endif