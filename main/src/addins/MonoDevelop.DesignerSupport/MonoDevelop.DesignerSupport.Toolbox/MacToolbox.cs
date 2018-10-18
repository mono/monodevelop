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
	public interface INativeChildView
	{
		event EventHandler Focused;
		void OnKeyPressed (object s, KeyEventArgs e);
		void OnKeyReleased (object s, KeyEventArgs e);
	}

	public class MacToolbox : NSStackView, IPropertyPadProvider, IToolboxConfiguration
	{
		const int IconsSpacing = 4;

		ToolboxService toolboxService;
		MacToolboxWidget toolboxWidget;

		public event EventHandler DragBegin;
		public event EventHandler DragSourceUnset;
		public event EventHandler<Gtk.TargetEntry []> DragSourceSet;
		public event EventHandler ContentFocused;

		public ItemToolboxNode selectedNode;

		NativeViews.ToggleButton catToggleButton;
		NativeViews.ToggleButton compactModeToggleButton;
		NativeViews.SearchTextField filterEntry;

		IPadWindow container;
		PadFontChanger fontChanger;
		Dictionary<string, int> categoryPriorities = new Dictionary<string, int> ();

		NativeViews.ClickedButton toolboxAddButton;
		Xwt.Drawing.Image groupByCategoryImage;

		readonly List<ToolboxWidgetCategory> items = new List<ToolboxWidgetCategory> ();
		NSStackView horizontalStackView;

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

			horizontalStackView = NativeViewHelper.CreateHorizontalStackView (IconsSpacing);

			AddArrangedSubview (horizontalStackView);

			horizontalStackView.EdgeInsets = new NSEdgeInsets (7, 7, 7, 7);

			//Horizontal container
			filterEntry = new NativeViews.SearchTextField ();
			filterEntry.AccessibilityTitle = GettextCatalog.GetString ("Search Toolbox");
			filterEntry.AccessibilityHelp = GettextCatalog.GetString ("Enter a term to search for it in the toolbox");
			filterEntry.Activated += filterTextChanged;
			filterEntry.Focused += (s, e) => ChangeFocusedView (s as INativeChildView);

			horizontalStackView.AddArrangedSubview (filterEntry);
			AddWidgetToFocusChain (filterEntry);

			catToggleButton = new NativeViews.ToggleButton ();
			catToggleButton.Image = groupByCategoryImage.ToNative ();
			catToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Show categories");
			catToggleButton.ToolTip = GettextCatalog.GetString ("Show categories");
			catToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle to show categories");
			catToggleButton.Activated += toggleCategorisation;
			catToggleButton.Focused += (s, e) => ChangeFocusedView (s as INativeChildView);

			horizontalStackView.AddArrangedSubview (catToggleButton);
			AddWidgetToFocusChain (catToggleButton);

			compactModeToggleButton = new NativeViews.ToggleButton ();
			compactModeToggleButton.Image = compactImage.ToNative ();
			compactModeToggleButton.ToolTip = GettextCatalog.GetString ("Use compact display");
			compactModeToggleButton.AccessibilityTitle = GettextCatalog.GetString ("Compact Layout");
			compactModeToggleButton.AccessibilityHelp = GettextCatalog.GetString ("Toggle for toolbox to use compact layout");
			compactModeToggleButton.Activated += ToggleCompactMode;
			compactModeToggleButton.Focused += (s, e) => ChangeFocusedView (s as INativeChildView);

			horizontalStackView.AddArrangedSubview (compactModeToggleButton);
			AddWidgetToFocusChain (compactModeToggleButton);

			toolboxAddButton = new NativeViews.ClickedButton ();
			toolboxAddButton.Image = addImage.ToNative ();
			toolboxAddButton.AccessibilityTitle = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.AccessibilityHelp = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.ToolTip = GettextCatalog.GetString ("Add toolbox items");
			toolboxAddButton.Activated += toolboxAddButton_Clicked;
			toolboxAddButton.Focused += (s, e) => ChangeFocusedView (s as INativeChildView);

			horizontalStackView.AddArrangedSubview (toolboxAddButton);
			AddWidgetToFocusChain (toolboxAddButton);

			#endregion

			toolboxWidget = new MacToolboxWidget (container) {
				AccessibilityTitle = GettextCatalog.GetString ("Toolbar items"),
				AccessibilityHelp = GettextCatalog.GetString ("Here are all the toolbox items to select")
			};
			AddWidgetToFocusChain (toolboxWidget);

			var scrollView = new NativeViews.ScrollContainerView ();
			scrollView.DocumentView = (NSView)toolboxWidget;

			AddArrangedSubview (scrollView);
			//Initialise self

			//update view when toolbox service updated
			toolboxService.ToolboxContentsChanged += delegate { Refresh (); };
			toolboxService.ToolboxConsumerChanged += delegate { Refresh (); };
			Refresh ();

			filterEntry.Changed += (s, e) => { refilter (); };
		
			toolboxWidget.SelectedItemChanged += delegate {
				selectedNode = this.toolboxWidget.SelectedItem != null ? this.toolboxWidget.SelectedItem.Tag as ItemToolboxNode : null;
				toolboxService.SelectItem (selectedNode);
			};

			toolboxWidget.DragBegin += (object sender, EventArgs e) => {
				if (this.toolboxWidget.SelectedItem != null) {
					this.toolboxWidget.HideTooltipWindow ();
					DragBegin?.Invoke (this, e);
				}
			};

			toolboxWidget.MouseDownActivated += (NSEvent obj) => {
				ContentFocused?.Invoke (this, EventArgs.Empty);
			};

			toolboxWidget.ActivateSelectedItem += delegate {
				toolboxService.UseSelectedItem ();
			};

			toolboxWidget.MenuOpened += ToolboxWidget_MenuOpened;

			//set initial state
			toolboxWidget.ShowCategories = catToggleButton.Active = true;
			compactModeToggleButton.Active = MonoDevelop.Core.PropertyService.Get ("ToolboxIsInCompactMode", false);
			toolboxWidget.IsListMode = !compactModeToggleButton.Active;
		}

		internal void FocusSelectedView ()
		{
			if (Window == null) {
				return;
			}
			if (FocusedView is NSView focusView && Window.FirstResponder != focusView && focusView.AcceptsFirstResponder ()) {
				Window.MakeFirstResponder (focusView);
			}
		}

		#region Focus Chain

		int focusedViewIndex = -1;
		internal const int TabKey = 65056;
		INativeChildView FocusedView => focusedViewIndex == -1 ? null : nativeChildViews [focusedViewIndex];
		List<INativeChildView> nativeChildViews = new List<INativeChildView> ();
	
		void AddWidgetToFocusChain (INativeChildView view)
		{
			nativeChildViews.Add (view);
			view.Focused += (s, e) => ChangeFocusedView (s as INativeChildView);

			if (focusedViewIndex == -1) {
				focusedViewIndex = 0;
			}
		}

		void ChangeFocusedView (INativeChildView view)
		{
			for (int i = 0; i < nativeChildViews.Count; i++) {
				if (nativeChildViews [i] == view) {
					focusedViewIndex = i;
				}
			}
		}

		void FocusPreviousItem (KeyEventArgs keyEventArgs = null)
		{
			if (focusedViewIndex <= 0) {
				//leave element
				//((NSView)focusedView).ResignFirstResponder ();
				Window.ResignFirstResponder ();
				if (keyEventArgs != null) {
					keyEventArgs.Handled = false;
				}
			} else {
				focusedViewIndex--;
				Window.MakeFirstResponder ((NSView)FocusedView);
			}
		}

		void FocusNextItem (KeyEventArgs keyEventArgs = null)
		{
			if (focusedViewIndex >= nativeChildViews.Count - 1) {
				//leave element
				Window.ResignFirstResponder ();
				if (keyEventArgs != null) {
					keyEventArgs.Handled = false;
				}
			} else {
				focusedViewIndex++;
				Window.MakeFirstResponder ((NSView)FocusedView);
			}
		}

		#endregion

		internal void KeyReleased (object s, KeyEventArgs e)
		{
			e.Handled = true;
			FocusedView?.OnKeyReleased (s, e);
		}

		internal void OnKeyPressed (object s, KeyEventArgs e)
		{
			e.Handled = true;

			if ((int) e.Key == TabKey || e.Key == Key.Tab) {
				if (e.Modifiers == ModifierKeys.Shift) {
					FocusPreviousItem (e);
					return;
				}
				if (e.Modifiers == ModifierKeys.None) {
					FocusNextItem (e);
				}
			}

			if (FocusedView is NSButton btn) {
				if (e.Modifiers == ModifierKeys.None) {
					if (e.Key == Key.Right) {
						FocusNextItem (e);
						return;
					}
					if (e.Key == Key.Left) {
						FocusPreviousItem (e);
						return;
					}
				}
			}

			FocusedView?.OnKeyPressed (s, e);
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
		}
		
		async void toolboxAddButton_Clicked (object sender, EventArgs e)
		{
			await toolboxService.AddUserItems ();
		}

		void ToolboxWidget_MenuOpened (object sender, CGPoint e)
		{
			if (!AllowEditingComponents)
				return;

			var eset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/DesignerSupport/ToolboxItemContextMenu");
			IdeApp.CommandService.ShowContextMenu (toolboxWidget, (int)e.X, (int)e.Y, eset, this);
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
			Gtk.TargetEntry [] targetTable = toolboxService.GetCurrentDragTargetTable ();
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
			categoryPriorities [category] = priority;
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
