using System;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide.Gui;
using Xwt;
using System.Linq;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Register ("CollectionView")]
	class MacToolboxWidget : NSCollectionView, IToolboxWidget, INativeChildView
	{
		public Action<NSEvent> MouseDownActivated { get; set; }
		public Action<Gdk.EventButton> DoPopupMenu { get; set; }
		public event EventHandler DragBegin;

		bool showCategories = true;
		bool listMode = false;

		readonly List<ToolboxWidgetCategory> categories = new List<ToolboxWidgetCategory> ();

		MacToolboxWidgetDataSource dataSource;
		MacToolboxWidgetFlowLayoutDelegate collectionViewDelegate;
		MacToolboxWidgetFlowLayout flowLayout;

		public IEnumerable<ToolboxWidgetCategory> Categories {
			get { return categories; }
		}
	
		public override void SetFrameSize (CGSize newSize)
		{
			if (Frame.Size != newSize) {
				flowLayout.InvalidateLayout ();
			}
			base.SetFrameSize (newSize);
		}

		public void HideTooltipWindow ()
		{
			//To implement
		}

		public event EventHandler SelectedItemChanged;
		protected virtual void OnSelectedItemChanged (EventArgs args)
		{
			HideTooltipWindow ();
			SelectedItemChanged?.Invoke (this, args);
		}

		public event EventHandler ActivateSelectedItem;
		public event EventHandler Focused;

		protected virtual void OnActivateSelectedItem (EventArgs args)
		{
			ActivateSelectedItem?.Invoke (this, args);
		}

		ToolboxWidgetItem selectedItem;
		public ToolboxWidgetItem SelectedItem {
			get {
				return selectedItem;
			}
			set {
				if (selectedItem != value) {
					selectedItem = value;
					dataSource.SelectItem (this, selectedItem);

					ScrollToSelectedItem ();
					OnSelectedItemChanged (EventArgs.Empty);
				}
			}
		}

		IPadWindow container;

		public MacToolboxWidget (IPadWindow container) : base ()
		{
			this.container = container;
			container.PadContentShown += OnContainerIsShown;

			Initialize ();
		}

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		// Called when created from unmanaged code
		public MacToolboxWidget (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MacToolboxWidget (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Shared initialization code
		public void Initialize ()
		{
			flowLayout = new MacToolboxWidgetFlowLayout ();
			flowLayout.SectionHeadersPinToVisibleBounds = false;
			flowLayout.MinimumInteritemSpacing = 0;
			flowLayout.MinimumLineSpacing = 0;
			flowLayout.SectionFootersPinToVisibleBounds = false;
			CollectionViewLayout = flowLayout;
		
			Delegate = collectionViewDelegate = new MacToolboxWidgetFlowLayoutDelegate ();
			Selectable = true;
			AllowsEmptySelection = true;
			DataSource = dataSource = new MacToolboxWidgetDataSource (categories);

			collectionViewDelegate.DragBegin += (s, e) => {
				DragBegin?.Invoke (this, EventArgs.Empty);
			};

			collectionViewDelegate.SelectionChanged += (s, e) => {
				if (e.Count == 0) {
					return;
				}
				if (e.AnyObject is NSIndexPath indexPath) {
					SelectedItem = categories[(int)indexPath.Section].Items[(int)indexPath.Item];
				}
			};

			BackgroundColors = new NSColor[] { Styles.SearchTextFieldLineBackgroundColor };
		}

		public override void MouseDown (NSEvent theEvent)
		{
			base.MouseDown (theEvent);
			if (this.SelectedItem != null && theEvent.ClickCount > 1) {
				OnActivateSelectedItem (EventArgs.Empty);
			}

			MouseDownActivated?.Invoke (theEvent);
		}

		public override void KeyDown (NSEvent theEvent)
		{
			base.KeyDown (theEvent);
		}

		#region IEncapsuledView

		public void OnKeyPressed (object s, KeyEventArgs e)
		{
			ToolboxWidgetItem nextItem;

			switch (e.Key) {
			case Key.NumPadEnter:
			case Key.Return:
				if (this.SelectedItem != null)
					this.OnActivateSelectedItem (EventArgs.Empty);
				return;
			case Key.NumPadUp:
			case Key.Up:
				if (this.listMode || this.SelectedItem is ToolboxWidgetCategory) {
					this.SelectedItem = GetPrevItem (this.SelectedItem);
				} else {
					nextItem = GetItemAbove (this.SelectedItem);
					this.SelectedItem = nextItem != this.SelectedItem ? nextItem : GetCategory (this.SelectedItem);
				}
				return;
			case Key.NumPadDown:
			case Key.Down:
				if (this.listMode || this.SelectedItem is ToolboxWidgetCategory) {
					this.SelectedItem = GetNextItem (this.SelectedItem);
				} else {
					nextItem = GetItemBelow (this.SelectedItem);
					if (nextItem == this.SelectedItem) {
						ToolboxWidgetCategory category = GetCategory (this.SelectedItem);
						nextItem = GetNextCategory (category);
						if (nextItem == category)
							nextItem = this.SelectedItem;
					}
					this.SelectedItem = nextItem;
				}
				return;
			case Key.NumPadLeft:
			case Key.Left:
				if (this.SelectedItem is ToolboxWidgetCategory) {
					SetCategoryExpanded ((ToolboxWidgetCategory)this.SelectedItem, false);
				} else {
					if (this.listMode) {
						this.SelectedItem = GetCategory (this.SelectedItem);
					} else {
						this.SelectedItem = GetItemLeft (this.SelectedItem);
					}
				}
				return;
			case Key.NumPadRight:
			case Key.Right:
				if (this.SelectedItem is ToolboxWidgetCategory selectedCategory) {
					if (selectedCategory.IsExpanded) {
						if (selectedCategory.ItemCount > 0)
							this.SelectedItem = selectedCategory.Items [0];
					} else {
						SetCategoryExpanded (selectedCategory, true);
					}
				} else {
					if (this.listMode) {
						// nothing
					} else {
						this.SelectedItem = GetItemRight (this.SelectedItem);
					}
				}
				break;
			}
		}

		public void OnKeyReleased (object s, KeyEventArgs e)
		{
			e.Handled = false;
		}

		#endregion

		void SetCategoryExpanded (ToolboxWidgetCategory item, bool v)
		{

		}

		ToolboxWidgetCategory GetCategory (ToolboxWidgetItem item)
		{
			foreach (var category in Categories) {
				if (category.Items.Any (s => s == item)) {
					return category;
				}
			}
			return null;
		}

		ToolboxWidgetItem GetNextItem (ToolboxWidgetItem item) => dataSource.GetNextItem (this, item);

		ToolboxWidgetItem GetPrevItem (ToolboxWidgetItem item) => dataSource.GetPrevItem (this, item);

		ToolboxWidgetItem GetItemLeft (ToolboxWidgetItem item) => dataSource.GetItemLeft (this, item);

		ToolboxWidgetItem GetItemRight (ToolboxWidgetItem item) => dataSource.GetItemRight (this, item);

		ToolboxWidgetItem GetItemAbove (ToolboxWidgetItem item) => dataSource.GetItemAbove (this, item);

		ToolboxWidgetItem GetItemBelow (ToolboxWidgetItem item) => dataSource.GetItemBelow (this, item);

		ToolboxWidgetItem GetNextCategory (ToolboxWidgetCategory category)
		{
			return category;
		}

		public bool IsListMode {
			get => listMode;
			set {
				listMode = value;
				collectionViewDelegate.IsOnlyImage = dataSource.IsOnlyImage = !value;
				QueueDraw ();
			}
		}

		public bool ShowCategories {
			get => showCategories;
			set {
				showCategories = collectionViewDelegate.IsShowCategories = value;
				QueueDraw ();
			}
		}

		public void ScrollToSelectedItem ()
		{
			//to implement
		}

		public IEnumerable<ToolboxWidgetItem> AllItems {
			get {
				foreach (ToolboxWidgetCategory category in this.categories) {
					foreach (ToolboxWidgetItem item in category.Items) {
						yield return item;
					}
				}
			}
		}

		Xwt.Size iconSize = new Xwt.Size (24, 24);

		public void ClearCategories ()
		{
			categories.Clear ();
			iconSize = new Xwt.Size (24, 24);
		}

		public string CustomMessage { get; set; }

		public bool CanIconizeToolboxCategories {
			get {
				foreach (ToolboxWidgetCategory category in categories) {
					if (category.CanIconizeItems)
						return true;
				}
				return false;
			}
		}

		internal void OnContainerIsShown (object sender, EventArgs e)
		{
			RegisterClassForItem (typeof (HeaderCollectionViewItem), HeaderCollectionViewItem.Name);
			RegisterClassForItem (typeof (LabelCollectionViewItem), LabelCollectionViewItem.Name);
			RegisterClassForItem (typeof (ImageCollectionViewItem), ImageCollectionViewItem.Name);
		}

		protected override void Dispose (bool disposing)
		{
			if (container != null) {
				container.PadContentShown -= OnContainerIsShown;
			}
			base.Dispose (disposing);
		}

		public void QueueDraw ()
		{
			dataSource.OnQueueDraw ();
			ReloadData ();
		}

		public void QueueResize ()
		{
			flowLayout.InvalidateLayout ();
		}

		public void AddCategory (ToolboxWidgetCategory category)
		{
			categories.Add (category);
			foreach (ToolboxWidgetItem item in category.Items) {
				if (item.Icon == null)
					continue;

				this.iconSize.Width = Math.Max (this.iconSize.Width, (int)item.Icon.Width);
				this.iconSize.Height = Math.Max (this.iconSize.Height, (int)item.Icon.Height);
			}
		}

		public override NSView MakeSupplementaryView (NSString elementKind, string identifier, NSIndexPath indexPath)
		{
			var item = MakeItem (identifier, indexPath) as HeaderCollectionViewItem;
			if (item == null) {
				return null;
			}

			var toolboxWidgetCategory = categories[(int)indexPath.Section];
			item.ExpandButton.AccessibilityTitle = toolboxWidgetCategory.Tooltip ?? "";
			item.ExpandButton.SetCustomTitle (toolboxWidgetCategory.Text ?? "");
			item.IsCollapsed = flowLayout.SectionAtIndexIsCollapsed ((nuint)indexPath.Section);

			//persisting the expanded value over our models (this is not necessary)
			toolboxWidgetCategory.IsExpanded = !item.IsCollapsed;

			item.ExpandButton.Activated += (sender, e) => {
				ToggleSectionCollapse (item.View);
				item.IsCollapsed = flowLayout.SectionAtIndexIsCollapsed ((nuint)indexPath.Section);
				toolboxWidgetCategory.IsExpanded = !item.IsCollapsed;
			};

			return item.View;
		}

	}
}
