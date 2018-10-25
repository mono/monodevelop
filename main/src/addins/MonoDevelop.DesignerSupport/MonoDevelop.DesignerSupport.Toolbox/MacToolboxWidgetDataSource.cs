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
		class HeaderInfo
		{
			public ToolboxWidgetCategory Category { get; set; }
			public NSIndexPath Index { get; set; }
			public HeaderCollectionViewItem View { get; set; }
		}

		internal bool ShowsOnlyImages { get; set; }
		internal readonly List<ToolboxWidgetCategory> Items;

		Dictionary<ToolboxWidgetItem, NSIndexPath> Views = new Dictionary<ToolboxWidgetItem, NSIndexPath> ();
		List<HeaderInfo> Categories = new List<HeaderInfo> ();

		public MacToolboxWidgetDataSource (List<ToolboxWidgetCategory> items)
		{
			Items = items;
		}

		public override NSCollectionViewItem GetItem (NSCollectionView collectionView, NSIndexPath indexPath)
		{
			var item = collectionView.MakeItem (ShowsOnlyImages ? ImageCollectionViewItem.Name : LabelCollectionViewItem.Name, indexPath);
			ToolboxWidgetItem selectedItem = null;
			if (item is LabelCollectionViewItem itmView) {
				selectedItem = Items [(int)indexPath.Section].Items [(int)indexPath.Item];

				itmView.View.ToolTip = selectedItem.Tooltip ?? "";
				itmView.TextField.StringValue = selectedItem.Text;
				itmView.TextField.AccessibilityTitle = selectedItem.Text ?? "";
				itmView.ImageView.Image = selectedItem.Icon.ToNative ();
				//TODO: carefull wih this deprecation (we need a better fix)
				//ImageView needs modify the AccessibilityElement from it's cell, doesn't work from main view
				itmView.ImageView.Cell.AccessibilityElement = false;
				itmView.Selected = false;

			} else if (item is ImageCollectionViewItem imgView) {
				selectedItem = Items [(int)indexPath.Section].Items [(int)indexPath.Item];
				imgView.View.ToolTip = selectedItem.Tooltip ?? "";
				imgView.Image = selectedItem.Icon.ToNative ();
				imgView.AccessibilityTitle = selectedItem.Text ?? "";
				imgView.AccessibilityElement = true;
				imgView.Selected = false;
			}

			if (!Views.ContainsKey (selectedItem)) {
				Views.Add (selectedItem, indexPath);
			}

			return item;
		}

		internal void SelectItem (NSCollectionView collectionView, ToolboxWidgetItem item)
		{
			if (item is ToolboxWidgetCategory cat) {
				var info = Categories.FirstOrDefault (s => s.Category == cat);
				if (info != null) {
					var window = collectionView.Window;
					window.MakeFirstResponder (info.View);
				}
			} else {
				if (Views.TryGetValue (item, out var indexPath)) {
					var elements = new NSSet (new NSObject [] { indexPath });
					collectionView.DeselectAll (null);
					collectionView.SelectItems (elements, NSCollectionViewScrollPosition.None);
				}
			}
		}

		internal void Clear ()
		{
			Views.Clear ();
			Categories.Clear ();
		}

		public override NSView GetView (NSCollectionView collectionView, NSString kind, NSIndexPath indexPath)
		{
			if (indexPath.Section >= Items.Count) {
				return null;
			}
			var toolboxWidget = (MacToolboxWidget)collectionView;
			if (collectionView.MakeSupplementaryView (NSCollectionElementKind.SectionHeader, "HeaderCollectionViewItem", indexPath) is HeaderCollectionViewItem button) {
				var section = Items [(int)indexPath.Section];
				button.SetCustomTitle (section.Text);
				button.IndexPath = indexPath;
				button.CollectionView = toolboxWidget;
				button.Activated -= Button_Activated;
				button.Activated += Button_Activated;
				button.IsCollapsed = toolboxWidget.flowLayout.SectionAtIndexIsCollapsed ((nuint)indexPath.Section);
				if (!Categories.Any (s => s.Category == section)) {
					Categories.Add (new HeaderInfo () { Category = section, Index = indexPath, View = button });
				}
				return button;
			}
			return null;
		}

		void Button_Activated (object sender, EventArgs e)
		{
			var headerCollectionViewItem = (HeaderCollectionViewItem)sender;
			var collectionView = headerCollectionViewItem.CollectionView;
			var indexPath = headerCollectionViewItem.IndexPath;

			var section = Items [(int)indexPath.Section];
			section.IsExpanded = !section.IsExpanded;
			headerCollectionViewItem.IsCollapsed = !section.IsExpanded;
			collectionView.CollectionViewLayout.InvalidateLayout ();
		}

		public override nint GetNumberofItems (NSCollectionView collectionView, nint section)
		{
			if (section >= Items.Count) {
				return 0;
			}
			return Items [(int)section].Items.Count;
		}

		public override nint GetNumberOfSections (NSCollectionView collectionView)
		{
			return Items.Count;
		}

		int GetCategoryIndex (ToolboxWidgetCategory category)
		{
			for (int i = 0; i < Categories.Count; i++) {
				if (Categories [i].Category == category) {
					return i;
				}
			}
			return -1;
		}

		int GetCategoryIndex (ToolboxWidgetItem currentItem)
		{
			for (int i = 0; i < Categories.Count; i++) {
				if (Categories [i].Category.Items.Contains (currentItem)) {
					return i;
				}
			}
			return -1;
		}
	}
}
#endif