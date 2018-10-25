#if MAC
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
		public nfloat Width { get; internal set; }
		public nfloat Height { get; internal set; }

		public event EventHandler<NSSet> SelectionChanged;
		public event EventHandler<NSIndexSet> DragBegin;
		public override void ItemsSelected (NSCollectionView collectionView, NSSet indexPaths)
		{
			SelectionChanged?.Invoke (this, indexPaths);
		}

		public override CGSize SizeForItem (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, NSIndexPath indexPath)
		{
			var dataSource = (MacToolboxWidgetDataSource)collectionView.DataSource;
			var flowLayout = (MacToolboxWidgetFlowLayout)collectionViewLayout;
			var section = dataSource.Items [(int)indexPath.Section];
			var item = section.Items [(int)indexPath.Item];
			if (!section.IsExpanded || !item.IsVisible) {
				return CGSize.Empty;
			}
			return flowLayout.ItemSize;
		}

		public override bool CanDragItems (NSCollectionView collectionView, NSSet indexPaths, NSEvent theEvent)
		{
			DragBegin?.Invoke (this, null);
			return false;
		}

		public override NSEdgeInsets InsetForSection (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, nint section)
		{
			return new NSEdgeInsets (0, 0, 0, 0);
		}

		public override CGSize ReferenceSizeForHeader (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, nint section)
		{
			return new CGSize (Width - 1, Height);
		}
	}
}
#endif