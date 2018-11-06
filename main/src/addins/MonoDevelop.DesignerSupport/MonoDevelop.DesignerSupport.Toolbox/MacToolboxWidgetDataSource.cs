#if MAC
using System;
using System.Collections.Generic;
using AppKit;
using MonoDevelop.Components;
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

		List<HeaderInfo> Categories = new List<HeaderInfo> ();

		public MacToolboxWidgetDataSource (List<ToolboxWidgetCategory> items)
		{
			Items = items;
		}

		public override NSCollectionViewItem GetItem (NSCollectionView collectionView, NSIndexPath indexPath)
		{
			var collectionViewItem = collectionView.MakeItem (ShowsOnlyImages ? ImageCollectionViewItem.Name : LabelCollectionViewItem.Name, indexPath);
			var widgetItem = Items [(int)indexPath.Section].Items [(int)indexPath.Item];

			if (collectionViewItem is LabelCollectionViewItem itmView) {
				itmView.SetCollectionView (collectionView);
				itmView.View.ToolTip = widgetItem.Tooltip ?? "";
				itmView.TextField.StringValue = widgetItem.Text;
				itmView.TextField.AccessibilityTitle = widgetItem.Text ?? "";
				itmView.Image = widgetItem.Icon.ToNSImage ();
				itmView.SelectedImage = widgetItem.Icon.WithStyles ("sel").ToNSImage ();
				//TODO: carefull wih this deprecation (we need a better fix)
				//ImageView needs modify the AccessibilityElement from it's cell, doesn't work from main view
				itmView.ImageView.Cell.AccessibilityElement = false;
				itmView.Refresh ();
			
			} else if (collectionViewItem is ImageCollectionViewItem imgView) {
				imgView.SetCollectionView (collectionView);
				imgView.View.ToolTip = widgetItem.Tooltip ?? "";
				imgView.Image = widgetItem.Icon.ToNSImage ();
				imgView.SelectedImage = widgetItem.Icon.WithStyles ("sel").ToNSImage ();
				imgView.AccessibilityTitle = widgetItem.Text ?? "";
				imgView.AccessibilityElement = true;
				imgView.Refresh ();
			}

			return collectionViewItem;
		}

		internal void Clear ()
		{
			Categories.Clear ();
		}

		public override NSView GetView (NSCollectionView collectionView, NSString kind, NSIndexPath indexPath)
		{
			var toolboxWidget = (MacToolboxWidget)collectionView;
			if (collectionView.MakeSupplementaryView (NSCollectionElementKind.SectionHeader, "HeaderCollectionViewItem", indexPath) is HeaderCollectionViewItem button) {
				var section = Items [(int)indexPath.Section];
				button.TitleTextField.StringValue = section.Text.Replace ("&amp;", "&");
				button.IndexPath = indexPath;
				button.CollectionView = toolboxWidget;
				button.Activated -= Button_Activated;
				button.Activated += Button_Activated;
				button.IsCollapsed = !section.IsExpanded;
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
			var collectionView = (MacToolboxWidget) headerCollectionViewItem.CollectionView;
			var indexPath = headerCollectionViewItem.IndexPath;

			var section = Items [(int)indexPath.Section];
			section.IsExpanded = !section.IsExpanded;
			headerCollectionViewItem.IsCollapsed = !section.IsExpanded;

			collectionView.ToggleSectionCollapse (sender as NSButton);
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