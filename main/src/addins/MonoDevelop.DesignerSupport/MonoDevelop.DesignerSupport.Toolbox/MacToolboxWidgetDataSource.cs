#if MAC
using System;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;
using Foundation;
using System.Linq;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	class MacToolboxWidgetDataSource : NSCollectionViewDataSource
	{
		public bool IsOnlyImage { get; set; }

		readonly List<ToolboxWidgetCategory> items;

		Dictionary<ToolboxWidgetItem, NSCollectionViewItem> Views = new Dictionary<ToolboxWidgetItem, NSCollectionViewItem> ();

		public MacToolboxWidgetDataSource (List<ToolboxWidgetCategory> items)
		{
			this.items = items;
		}

		public override NSCollectionViewItem GetItem (NSCollectionView collectionView, NSIndexPath indexPath)
		{
			var item = collectionView.MakeItem (IsOnlyImage ? ImageCollectionViewItem.Name : LabelCollectionViewItem.Name, indexPath);
			ToolboxWidgetItem selectedItem = null;
			if (item is LabelCollectionViewItem itmView) {
				selectedItem = items [(int)indexPath.Section].Items [(int)indexPath.Item];

				itmView.View.ToolTip = selectedItem.Tooltip ?? "";
				itmView.TextField.StringValue = selectedItem.Text;
				itmView.TextField.AccessibilityTitle = selectedItem.Text ?? "";
				//itmView.TextField.AccessibilityHelp = selectedItem.AccessibilityHelp ?? "";
				itmView.ImageView.Image = selectedItem.Icon.ToNative ();
				//TODO: carefull wih this deprecation (we need a better fix)
				//ImageView needs modify the AccessibilityElement from it's cell, doesn't work from main view
				itmView.ImageView.Cell.AccessibilityElement = false;
				itmView.Selected = false;

			} else if (item is ImageCollectionViewItem imgView) {
				selectedItem = items [(int)indexPath.Section].Items [(int)indexPath.Item];
				imgView.View.ToolTip = selectedItem.Tooltip ?? "";
				imgView.Image = selectedItem.Icon.ToNative ();
				imgView.AccessibilityTitle = selectedItem.Text ?? "";
				imgView.AccessibilityElement = true;
				imgView.Selected = false;
			}

			if (!Views.ContainsKey (selectedItem)) {
				Views.Add (selectedItem, item);
			}

			return item;
		}

		internal void SelectItem (NSCollectionView collectionView, ToolboxWidgetItem item)
		{
			if (Views.TryGetValue (item, out var selectedItem)) {
			 	var indexPath =	collectionView.GetIndexPath (selectedItem);
				var elements = new NSSet (new NSObject [] { indexPath });
				collectionView.DeselectAll (null);
				collectionView.SelectItems (elements, NSCollectionViewScrollPosition.None);
			}
		}

		public void OnQueueDraw ()
		{
			Views.Clear ();
		}

		public override NSView GetView (NSCollectionView collectionView, NSString kind, NSIndexPath indexPath)
		{
			var toolboxWidget = (MacToolboxWidget)collectionView;
			if (collectionView.MakeSupplementaryView (NSCollectionElementKind.SectionHeader, "HeaderCollectionViewItem", indexPath) is HeaderCollectionViewItem button) {
				var section = items [(int)indexPath.Section];
				button.SetCustomTitle (section.Text);
				button.IndexPath = indexPath;
				button.CollectionView = toolboxWidget;
				button.Activated -= Button_Activated;
				button.Activated += Button_Activated;
				button.IsCollapsed = toolboxWidget.flowLayout.SectionAtIndexIsCollapsed ((nuint)indexPath.Section);
				return button;
			}
			return null;
		}

		void Button_Activated (object sender, EventArgs e)
		{
			var headerCollectionViewItem = (HeaderCollectionViewItem)sender;
			var collectionView = headerCollectionViewItem.CollectionView;
			var indexPath = headerCollectionViewItem.IndexPath;
			var isCollapsed = collectionView.flowLayout.SectionAtIndexIsCollapsed ((nuint)indexPath.Section);

			collectionView.ToggleSectionCollapse (headerCollectionViewItem);

			headerCollectionViewItem.IsCollapsed = items [(int)indexPath.Section].IsExpanded = collectionView.flowLayout.SectionAtIndexIsCollapsed ((nuint)indexPath.Section);
			//if (isCollapsed) {
			//	collectionView.flowLayout.ItemSize = new CGSize (0, 0);
			//} else {
			//	collectionView.flowLayout.ItemSize = new CGSize (collectionView.Frame.Width-30, HeaderCollectionViewItem.SectionHeight);
			//}
			collectionView.CollectionViewLayout.InvalidateLayout ();
		}

		public override nint GetNumberofItems (NSCollectionView collectionView, nint section)
		{

			return items [(int)section].Items.Count;
		}

		public override nint GetNumberOfSections (NSCollectionView collectionView)
		{
			return items.Count;
		}

		internal ToolboxWidgetItem GetItemRight (NSCollectionView collectionView, ToolboxWidgetItem currentItem)
		{
			if (Views.TryGetValue (currentItem, out var collectionViewItem)) {
				var expectedPoint = new CGPoint  (collectionViewItem.View.Frame.Right + collectionViewItem.View.Frame.Height, collectionViewItem.View.Frame.Y);
				var expectedIndexPath = collectionView.GetIndexPath (expectedPoint);
				if (expectedIndexPath != null) {
					var nextItem = items [(int)expectedIndexPath.Section].Items [(int)expectedIndexPath.Item];
					return nextItem;
				}
			}
			return currentItem;
		}

		internal ToolboxWidgetItem GetItemLeft (NSCollectionView collectionView, ToolboxWidgetItem currentItem)
		{
			if (Views.TryGetValue (currentItem, out var collectionViewItem)) {
				var expectedPoint = new CGPoint (collectionViewItem.View.Frame.Left - collectionViewItem.View.Frame.Height, collectionViewItem.View.Frame.Y);
				var expectedIndexPath = collectionView.GetIndexPath (expectedPoint);
				if (expectedIndexPath != null) {
					var nextItem = items [(int)expectedIndexPath.Section].Items [(int)expectedIndexPath.Item];
					return nextItem;
				}
			}
			return currentItem;
		}

		internal ToolboxWidgetItem GetItemAbove (NSCollectionView collectionView, ToolboxWidgetItem currentItem)
		{
			if (Views.TryGetValue (currentItem, out var collectionViewItem)) {
				var expectedPoint = new CGPoint (collectionViewItem.View.Frame.X, collectionViewItem.View.Frame.Top - collectionViewItem.View.Frame.Height);
				var expectedIndexPath = collectionView.GetIndexPath (expectedPoint);
				if (expectedIndexPath != null) {
					var nextItem = items [(int)expectedIndexPath.Section].Items [(int)expectedIndexPath.Item];
					return nextItem;
				}
			}
			return currentItem;
		}

		internal ToolboxWidgetItem GetItemBelow (NSCollectionView collectionView, ToolboxWidgetItem currentItem)
		{
			if (Views.TryGetValue (currentItem, out var collectionViewItem)) {
				var expectedPoint = new CGPoint (collectionViewItem.View.Frame.X, collectionViewItem.View.Frame.Top + collectionViewItem.View.Frame.Height);
				var expectedIndexPath = collectionView.GetIndexPath (expectedPoint);
				if (expectedIndexPath != null) {
					var nextItem = items [(int)expectedIndexPath.Section].Items [(int)expectedIndexPath.Item];
					return nextItem;
				}
			}
			return currentItem;
		}

		internal ToolboxWidgetItem GetNextItem (NSCollectionView collectionView, ToolboxWidgetItem currentItem)
		{
			for (int i = 0; i < Views.Count; i++) {
				if (Views.ElementAt (i).Key == currentItem) {
					return Views.ElementAt (i + 1).Key;
				}
			}
			return currentItem;
		}

		internal ToolboxWidgetItem GetPrevItem (NSCollectionView collectionView, ToolboxWidgetItem currentItem)
		{
			for (int i = 0; i < Views.Count; i++) {
				if (Views.ElementAt (i).Key == currentItem) {
					return Views.ElementAt (i - 1).Key;
				}
			}
			return currentItem;
		}
	}
}
#endif