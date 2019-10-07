#if MAC
using System;
using AppKit;
using CoreGraphics;
using Foundation;
using System.Linq;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	class MacToolboxWidgetFlowLayoutDelegate : NSCollectionViewDelegateFlowLayout
	{
		internal bool ShowsOnlyImages { get; set; }
		internal bool ShowsCategories { get; set; }
		public nfloat Width { get; internal set; }
		public nfloat Height { get; internal set; }

		public bool IsLastSelectionFromMouseDown { get; set; }

		public event EventHandler<NSSet> SelectionChanged;
		public event EventHandler<NSIndexSet> DragBegin;

		public override void ItemsSelected (NSCollectionView collectionView, NSSet indexPaths)
		{
			SelectionChanged?.Invoke (this, indexPaths);
		}

		public override NSSet ShouldSelectItems (NSCollectionView collectionView, NSSet indexPaths)
		{
			//HACK: This allows handle the selection when using the keyboard.
			//this is necessary to break the default behaviour of NSCollectionView while keypress UP between sections
			//the hack allows select last item from next section instead the first one.
			//we avoid to use this in mouse down events
			if (!IsLastSelectionFromMouseDown) {
				var toolboxWidget = (MacToolboxWidget)collectionView;
				var lastSelectedItem = toolboxWidget.SelectedIndexPath;

				if (indexPaths.AnyObject is NSIndexPath indexPath) {
					if (indexPath != null && lastSelectedItem != null && lastSelectedItem.Section > indexPath.Section) {
						var lastItemFromSection = toolboxWidget.GetNumberOfItems (indexPath.Section) - 1;
						return new NSSet<NSIndexPath> (NSIndexPath.FromItemSection (lastItemFromSection, indexPath.Section));
					}
				}
			} else {
				IsLastSelectionFromMouseDown = false;
			}

			return indexPaths;
		}

		public override CGSize SizeForItem (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, NSIndexPath indexPath)
		{
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