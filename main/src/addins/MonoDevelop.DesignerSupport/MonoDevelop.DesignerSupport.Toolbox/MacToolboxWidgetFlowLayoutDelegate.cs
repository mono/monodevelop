#if MAC
using System;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	class MacToolboxWidgetFlowLayoutDelegate : NSCollectionViewDelegateFlowLayout
	{
		internal bool ShowsOnlyImages { get; set; }
		internal bool ShowsCategories { get; set; }
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
			var macToolboxWidget = (MacToolboxWidget)collectionView;
			var dataSource = (MacToolboxWidgetDataSource)collectionView.DataSource;
			var flowLayout = (NSCollectionViewFlowLayout)collectionViewLayout;
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