using System;
using System.Collections.Generic;
using AppKit;
using Foundation;
namespace MonoDevelop.DesignerSupport.Toolbox
{
	class MacToolboxWidgetDataSource : NSCollectionViewDataSource
	{
		public bool IsOnlyImage { get; set; }

		readonly List<ToolboxWidgetCategory> items;
		public MacToolboxWidgetDataSource (List<ToolboxWidgetCategory> items)
		{
			this.items = items;
		}

		public override NSCollectionViewItem GetItem (NSCollectionView collectionView, NSIndexPath indexPath)
		{
			var item = collectionView.MakeItem (IsOnlyImage ? ImageCollectionViewItem.Name : LabelCollectionViewItem.Name, indexPath);
			if (item is LabelCollectionViewItem itmView) {
				var selectedItem = items [(int)indexPath.Section].Items [(int)indexPath.Item];

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
				var selectedItem = items [(int)indexPath.Section].Items [(int)indexPath.Item];
				imgView.View.ToolTip = selectedItem.Tooltip ?? "";
				imgView.Image = selectedItem.Icon.ToNative ();
				imgView.AccessibilityTitle = selectedItem.Text ?? "";
				imgView.AccessibilityElement = true;
				imgView.Selected = false;
			}
			return item;
		}

		public override NSView GetView (NSCollectionView collectionView, NSString kind, NSIndexPath indexPath)
		{
			return collectionView.MakeSupplementaryView (kind, HeaderCollectionViewItem.Name, indexPath);
		}

		public override nint GetNumberofItems (NSCollectionView collectionView, nint section)
		{

			return items [(int)section].Items.Count;
		}

		public override nint GetNumberOfSections (NSCollectionView collectionView)
		{
			return items.Count;
		}
	}
}
