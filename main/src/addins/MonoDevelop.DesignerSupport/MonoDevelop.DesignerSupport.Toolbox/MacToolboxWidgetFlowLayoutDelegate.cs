using System;
using AppKit;
using CoreGraphics;
using Foundation;
using System.Linq;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	class MacToolboxWidgetFlowLayout : NSCollectionViewFlowLayout
	{
	
	}

	class MacToolboxWidgetFlowLayoutDelegate : NSCollectionViewDelegateFlowLayout
	{
		public bool IsOnlyImage { get; set; }
		public bool IsShowCategories { get; set; }

		public event EventHandler<NSSet> SelectionChanged;
		public event EventHandler<NSIndexSet> DragBegin;
		public override void ItemsSelected (NSCollectionView collectionView, NSSet indexPaths)
		{
			SelectionChanged?.Invoke (this, indexPaths);
		}

		public override bool CanDragItems (NSCollectionView collectionView, NSSet indexPaths, NSEvent theEvent)
		{
			DragBegin?.Invoke (this, null);
			return false;
		}

		public override CGSize SizeForItem (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, NSIndexPath indexPath)
		{
			var categories = ((MacToolboxWidget)collectionView).Categories;
			var category = categories.ElementAt ((int)indexPath.Section);
			var selectedItem = category.Items[(int)indexPath.Item];
			if (!category.IsExpanded || !selectedItem.IsVisible) {
				return new CGSize (0, 0);
			}

			if (IsOnlyImage) {
				return ImageCollectionViewItem.Size;
			}
			var delegateFlowLayout = (MacToolboxWidgetFlowLayout)collectionViewLayout;
			var sectionInset = delegateFlowLayout.SectionInset;
			return new CGSize (collectionView.Frame.Width - sectionInset.Right - sectionInset.Left, LabelCollectionViewItem.ItemHeight);
		}

		public override NSEdgeInsets InsetForSection (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, nint section)
		{
			return new NSEdgeInsets (0, 0, 0, 0);
		}

		public override CGSize ReferenceSizeForHeader (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, nint section)
		{
			return new CGSize (0, 1);
		}

		public override CGSize ReferenceSizeForFooter (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, nint section)
		{
			return CGSize.Empty;
		}

		public override NSSet ShouldDeselectItems (NSCollectionView collectionView, NSSet indexPaths)
		{
			return indexPaths;
		}

		public override NSSet ShouldSelectItems (NSCollectionView collectionView, NSSet indexPaths)
		{
			return indexPaths;
		}
	}
}
