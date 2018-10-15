using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing.Design;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using AppKit;
using Xwt;
using CoreGraphics;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public class MacToolbox : NSStackView, IPropertyPadProvider, IToolboxConfiguration
	{
		ToolboxService toolboxService;

		ItemToolboxNode selectedNode;
		IToolboxWidget toolboxWidget;

		public event EventHandler DragBegin;
		public event EventHandler DragSourceUnset;
		public event EventHandler<Gtk.TargetEntry[]> DragSourceSet;

		NativeViews.ToggleButton catToggleButton;
		NativeViews.ToggleButton compactModeToggleButton;
		readonly NativeViews.SearchTextField filterEntry;

		MonoDevelop.Ide.Gui.PadFontChanger fontChanger;

		IPadWindow container;
		Dictionary<string, int> categoryPriorities = new Dictionary<string, int> ();

		NativeViews.ClickedButton toolboxAddButton;

		Xwt.Drawing.Image groupByCategoryImage;

		readonly List<ToolboxWidgetCategory> items = new List<ToolboxWidgetCategory> ();

		NSStackView verticalStackView;

		const int IconsSpacing = 4;

		public MacToolbox (ToolboxService toolboxService, IPadWindow container)
		{
			Orientation = NSUserInterfaceLayoutOrientation.Vertical;
			Alignment = NSLayoutAttribute.Leading;
			Spacing = 0;
			Distribution = NSStackViewDistribution.Fill;

			this.toolboxService = toolboxService;
			this.container = container;

			#region Toolbar

			//DockItemToolbar toolbar = container.GetToolbar (DockPositionType.Top);
			groupByCategoryImage = ImageService.GetIcon (Ide.Gui.Stock.GroupByCategory, Gtk.IconSize.Menu);
			var compactImage = ImageService.GetIcon ("md-compact-display", Gtk.IconSize.Menu);
			var addImage = ImageService.GetIcon (Ide.Gui.Stock.Add, Gtk.IconSize.Menu);

			verticalStackView = NativeViewHelper.CreateHorizontalStackView (IconsSpacing);
			AddArrangedSubview (verticalStackView);
			verticalStackView.EdgeInsets = new NSEdgeInsets (7, 7, 7, 2);

			//Horizontal container
			filterEntry = new NativeViews.SearchTextField ();
			filterEntry.AccessibilityTitle = GettextCatalog.GetString ("Search Toolbox");
			filterEntry.AccessibilityHelp = GettextCatalog.GetString ("Enter a term to search for it in the toolbox");
			filterEntry.Activated += filterTextChanged;

			verticalStackView.AddArrangedSubview (filterEntry);

			catToggleButton = new NativeViews.ToggleButton ();
			catToggleButton.Image = groupByCategoryImage.ToNative ();
			catToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Show categories");
			catToggleButton.ToolTip = GettextCatalog.GetString ("Show categories");
			catToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle to show categories");
			catToggleButton.Activated += toggleCategorisation;

			verticalStackView.AddArrangedSubview (catToggleButton);

			compactModeToggleButton = new NativeViews.ToggleButton ();
			compactModeToggleButton.Image = compactImage.ToNative ();
			compactModeToggleButton.ToolTip = GettextCatalog.GetString ("Use compact display");
			compactModeToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Compact Layout");
			compactModeToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle for toolbox to use compact layout");
			compactModeToggleButton.Activated += ToggleCompactMode;

			verticalStackView.AddArrangedSubview (compactModeToggleButton);

			toolboxAddButton = new NativeViews.ClickedButton ();
			toolboxAddButton.Image = addImage.ToNative ();
			toolboxAddButton.AccessibilityTitle = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.AccessibilityHelp = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.ToolTip = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.Activated += toolboxAddButton_Clicked;

			verticalStackView.AddArrangedSubview (toolboxAddButton);

			#endregion

			MacToolboxWidget collectionView;
			toolboxWidget = collectionView = new MacToolboxWidget (container) {
				AccessibilityTitle = GettextCatalog.GetString ("Toolbar items"),
				AccessibilityHelp = GettextCatalog.GetString ("Here are all the toolbox items to select")
			};


			var scrollView = new NativeViews.ScrollContainerView ();
			scrollView.DocumentView = (NSView)toolboxWidget;
			AddArrangedSubview (scrollView);
			//Initialise self

			//update view when toolbox service updated
			toolboxService.ToolboxContentsChanged += delegate { Refresh (); };
			toolboxService.ToolboxConsumerChanged += delegate { Refresh (); };
			Refresh ();

			filterEntry.Changed += (s, e) => {
				refilter ();
			};

			toolboxWidget.SelectedItemChanged += delegate {
				selectedNode = this.toolboxWidget.SelectedItem != null ? this.toolboxWidget.SelectedItem.Tag as ItemToolboxNode : null;
				toolboxService.SelectItem (selectedNode);
			};

			collectionView.DragBegin += (object sender, EventArgs e) => {
				if (this.toolboxWidget.SelectedItem != null) {
					this.toolboxWidget.HideTooltipWindow ();
					DragBegin?.Invoke (this, e);
				}
			};

			this.toolboxWidget.ActivateSelectedItem += delegate {
				toolboxService.UseSelectedItem ();
			};

			this.toolboxWidget.DoPopupMenu = ShowPopup;

			//set initial state
			this.toolboxWidget.ShowCategories = catToggleButton.Active = true;
			compactModeToggleButton.Active = MonoDevelop.Core.PropertyService.Get ("ToolboxIsInCompactMode", false);
			this.toolboxWidget.IsListMode  = !compactModeToggleButton.Active;
		}

		public override void SetFrameSize (CGSize newSize)
		{
			toolboxWidget.QueueResize ();
			base.SetFrameSize (newSize);
		}

		#region Toolbar event handlers

		void ToggleCompactMode (object sender, EventArgs e)
		{
			toolboxWidget.IsListMode = !compactModeToggleButton.Active;

			PropertyService.Set ("ToolboxIsInCompactMode", compactModeToggleButton.Active);

			if (compactModeToggleButton.Active) {
				compactModeToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Full Layout");
				compactModeToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle for toolbox to use full layout");
			} else {
				compactModeToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Compact Layout"); ;
				compactModeToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle for toolbox to use compact layout");
			}
		}

		void toggleCategorisation (object sender, EventArgs e)
		{
			this.toolboxWidget.ShowCategories = catToggleButton.Active;
			if (catToggleButton.Active) {
				catToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Hide Categories");
				catToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle to hide toolbox categories");
			} else {
				catToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Show Categories");
				catToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle to show toolbox categories");
			}
		}
		
		void filterTextChanged (object sender, EventArgs e)
		{
			refilter ();
		}

		void refilter ()
		{
			foreach (ToolboxWidgetCategory cat in toolboxWidget.Categories) {
				bool hasVisibleChild = false;
				foreach (ToolboxWidgetItem child in cat.Items) {
					child.IsVisible = ((ItemToolboxNode)child.Tag).Filter (filterEntry.Text);
					hasVisibleChild |= child.IsVisible;
				}
				cat.IsVisible = hasVisibleChild;
			}
			toolboxWidget.QueueDraw ();
			toolboxWidget.QueueResize ();
		}
		
		async void toolboxAddButton_Clicked (object sender, EventArgs e)
		{
			await toolboxService.AddUserItems ();
		}
		
		void ShowPopup (Gdk.EventButton evt)
		{
			if (!AllowEditingComponents)
				return;
			CommandEntrySet eset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/DesignerSupport/ToolboxItemContextMenu");
			if (evt != null) {
				IdeApp.CommandService.ShowContextMenu ((NSView) toolboxWidget, evt, eset, this);
			} else {
				//IdeApp.CommandService.ShowContextMenu (toolboxWidget, (int) Frame.Left, (int)Frame.Top, eset, this);
			}
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
					categories[itbn.Category] = cat;
				}
				if (newItem.Text != null)
					categories[itbn.Category].Add (newItem);
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
			
			categories.Clear ();

			AddItems (toolboxService.GetCurrentToolboxItems ());

			DragSourceUnset?.Invoke (this, EventArgs.Empty);
			toolboxWidget.ClearCategories ();

			var cats = categories.Values.ToList ();
			cats.Sort ((a, b) => a.Priority != b.Priority ? a.Priority.CompareTo (b.Priority) : a.Text.CompareTo (b.Text));
			cats.Reverse ();
			foreach (ToolboxWidgetCategory category in cats) {
				category.IsExpanded = true;
				toolboxWidget.AddCategory (category);
			}
			toolboxWidget.QueueResize ();
			Gtk.TargetEntry[] targetTable = toolboxService.GetCurrentDragTargetTable ();
			if (targetTable != null)
				DragSourceSet?.Invoke (this, targetTable); // Drag.SourceSet (toolboxWidget, Gdk.ModifierType.Button1Mask, targetTable, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			compactModeToggleButton.Visible = toolboxWidget.CanIconizeToolboxCategories;
			refilter ();
		}
			
		void ConfigureToolbar ()
		{
			// Default configuration
			categoryPriorities.Clear ();
			toolboxAddButton.Visible = true;
			
			toolboxService.Customize (container, this);
		}

		protected override void Dispose (bool disposing)
		{
			catToggleButton.Activated -= toggleCategorisation;
			compactModeToggleButton.Activated -= ToggleCompactMode;
			toolboxAddButton.Activated -= toolboxAddButton_Clicked;

			if (fontChanger != null) {
				fontChanger.Dispose ();
				fontChanger = null;
			}
			base.Dispose (disposing);
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
