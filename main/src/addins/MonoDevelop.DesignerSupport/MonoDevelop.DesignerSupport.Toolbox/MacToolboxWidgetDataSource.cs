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

	 	readonly Dictionary<ToolboxWidgetItem, (NSImage Image, NSImage SelectedImage)> cachedImages = new Dictionary<ToolboxWidgetItem, (NSImage Image, NSImage SelectedImage)> ();

		internal event EventHandler<NSIndexPath> RegionCollapsed;
		internal bool ShowsOnlyImages { get; set; }

		public override NSCollectionViewItem GetItem (NSCollectionView collectionView, NSIndexPath indexPath)
		{
			var widget = (MacToolboxWidget)collectionView;
			var collectionViewItem = collectionView.MakeItem (ShowsOnlyImages ? ImageCollectionViewItem.Name : LabelCollectionViewItem.Name, indexPath);
			var widgetItem = widget.CategoryVisibilities [(int)indexPath.Section].Items [(int)indexPath.Item];

			if (!cachedImages.TryGetValue (widgetItem, out var catchedImages)) {
				catchedImages = (widgetItem.Icon.ToNSImage (), widgetItem.Icon.WithStyles ("sel").ToNSImage ());
				cachedImages.Add (widgetItem, catchedImages);
			}

			if (collectionViewItem is LabelCollectionViewItem itmView) {
				itmView.SetCollectionView (collectionView);
				itmView.View.ToolTip = widgetItem.Tooltip ?? "";
				itmView.TextField.StringValue = widgetItem.Text;
				itmView.TextField.AccessibilityTitle = widgetItem.Text ?? "";
				itmView.Image = catchedImages.Image;
				itmView.SelectedImage = catchedImages.SelectedImage;
				//TODO: carefull wih this deprecation (we need a better fix)
				//ImageView needs modify the AccessibilityElement from it's cell, doesn't work from main view
				itmView.ImageView.Cell.AccessibilityElement = false;
				itmView.Refresh ();
			
			} else if (collectionViewItem is ImageCollectionViewItem imgView) {
				imgView.SetCollectionView (collectionView);
				imgView.View.ToolTip = widgetItem.Tooltip ?? "";
				imgView.Image = catchedImages.Image;
				imgView.SelectedImage = catchedImages.SelectedImage;
				imgView.AccessibilityTitle = widgetItem.Text ?? "";
				imgView.AccessibilityElement = true;
				imgView.Refresh ();
			}

			return collectionViewItem;
		}

		public override NSView GetView (NSCollectionView collectionView, NSString kind, NSIndexPath indexPath)
		{
			var toolboxWidget = (MacToolboxWidget)collectionView;
			if (collectionView.MakeSupplementaryView (NSCollectionElementKind.SectionHeader, "HeaderCollectionViewItem", indexPath) is HeaderCollectionViewItem button) {
				var section = toolboxWidget.CategoryVisibilities [(int)indexPath.Section].Category;
				button.TitleTextField.StringValue = section.Text.Replace ("&amp;", "&");
				button.IndexPath = indexPath;
				button.CollectionView = toolboxWidget;
				button.Activated -= Button_Activated;
				button.Activated += Button_Activated;
				button.IsCollapsed = !section.IsExpanded;
				return button;
			}
			return null;
		}

		void Button_Activated (object sender, EventArgs e)
		{
			var headerCollectionViewItem = (HeaderCollectionViewItem)sender;

			var collectionView = headerCollectionViewItem.CollectionView;
			var indexPath = headerCollectionViewItem.IndexPath;
			var category = collectionView.CategoryVisibilities [(int)indexPath.Section].Category;

			category.IsExpanded = !category.IsExpanded;
			RegionCollapsed?.Invoke (this, indexPath);
		}

		public override nint GetNumberofItems (NSCollectionView collectionView, nint section)
		{
			var toolboxWidget = (MacToolboxWidget)collectionView;
			if (section >= toolboxWidget.CategoryVisibilities.Count) {
				//because multitask our control sections could be unsync when our current document changes
				return 0;
			}
			return toolboxWidget.CategoryVisibilities [(int)section].Items.Count;
		}

		public override nint GetNumberOfSections (NSCollectionView collectionView)
		{
			var toolboxWidget = (MacToolboxWidget)collectionView;
			return toolboxWidget.CategoryVisibilities.Count;
		}

		internal void Clear ()
		{
			cachedImages.Clear ();
		}
	}
}
#endif