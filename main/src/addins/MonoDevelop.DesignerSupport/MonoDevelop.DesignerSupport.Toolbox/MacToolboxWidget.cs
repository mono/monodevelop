/* 
 * MacToolboxWidget.cs - A toolbox widget
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if MAC
using System;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide.Gui;
using System.Linq;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	class MacToolboxWidget : NSCollectionView, IToolboxWidget
	{
		internal const string ImageViewItemName = "ImageViewItem";
		internal const string LabelViewItemName = "LabelViewItem";
		internal const string HeaderViewItemName = "HeaderViewItem";

		const int IconMargin = 1;

		internal MacToolboxWidgetFlowLayoutDelegate collectionViewDelegate;
		internal NSCollectionViewFlowLayout flowLayout;

		public List<CategoryVisibility> CategoryVisibilities { get; set; } = new List<CategoryVisibility> ();

		public event EventHandler RegionCollapsed;
		public event EventHandler Focused;
		public event EventHandler DragBegin;
		public event EventHandler<CGPoint> MenuOpened;
		public event EventHandler ActivateSelectedItem;

		MacToolboxWidgetDataSource dataSource;

		bool listMode;
		bool showCategories = true;

		public IEnumerable<ToolboxWidgetCategory> Categories {
			get { return CategoryVisibilities.Select (s => s.Category); }
		}

		internal void PerformActivateSelectedItem () => ActivateSelectedItem?.Invoke (this, EventArgs.Empty);

		public NSIndexPath SelectedIndexPath {
			get => SelectionIndexPaths.AnyObject as NSIndexPath;
			set {
				if (value == null) {
					SelectionIndexPaths = new NSSet ();
				} else {
					SelectionIndexPaths = new NSSet (value);
				}
			}
		}

		public ToolboxWidgetItem SelectedItem {
			get {
				if (MacToolboxWidgetDataSource.IsIndexOutOfSync (SelectedIndexPath, CategoryVisibilities)) {
					return null;
				}
				var indexPath = SelectedIndexPath;
				return CategoryVisibilities [(int)indexPath.Section].Items [(int)indexPath.Item];
			}
		}

		public bool IsListMode {
			get => listMode;
			set {
				listMode = value;
				collectionViewDelegate.ShowsOnlyImages = dataSource.ShowsOnlyImages = !value;
			}
		}

		public bool ShowCategories {
			get => showCategories;
			set {
				showCategories = collectionViewDelegate.ShowsCategories = value;
			}
		}

		public IEnumerable<ToolboxWidgetItem> AllItems {
			get {
				foreach (var categoryVisibility in CategoryVisibilities) {
					foreach (var item in categoryVisibility.Category.Items) {
						yield return item;
					}
				}
			}
		}

		public bool CanIconizeToolboxCategories {
			get {
				foreach (var categoryVisibility in CategoryVisibilities) {
					if (categoryVisibility.Category.CanIconizeItems)
						return true;
				}
				return false;
			}
		}

		public MacToolboxWidget ()
		{
			Initialize ();
		}

		public bool IsFocused { get; private set; }

		public override bool ResignFirstResponder ()
		{
			IsFocused = false;
			RedrawSelectedItem ();
			return base.ResignFirstResponder ();
		}

		void RedrawSelectedItem ()
		{
			if (SelectionIndexPaths.Count > 0) {
				var collectionViewItem = GetItem (SelectedIndexPath);
				if (collectionViewItem != null && collectionViewItem.View is ContentCollectionViewItem contentCollectionView) {
					contentCollectionView.RefreshLayer ();
				}
			}
		}

		// Shared initialization code
		public void Initialize ()
		{
			AccessibilityRole = NSAccessibilityRoles.ToolbarRole;
			flowLayout = new NSCollectionViewFlowLayout {
				SectionHeadersPinToVisibleBounds = false,
				MinimumInteritemSpacing = 0,
				MinimumLineSpacing = 0
			};
			CollectionViewLayout = flowLayout;
		
			Delegate = collectionViewDelegate = new MacToolboxWidgetFlowLayoutDelegate ();
			Selectable = true;
			AllowsEmptySelection = true;
			DataSource = dataSource = new MacToolboxWidgetDataSource ();

			dataSource.RegionCollapsed += DataSource_RegionCollapsed;

			collectionViewDelegate.DragBegin += CollectionViewDelegate_DragBegin;
			collectionViewDelegate.SelectionChanged += CollectionViewDelegate_SelectionChanged;

			BackgroundColors = new NSColor [] { Styles.ToolbarBackgroundColor };

			RegisterClassForItem (typeof (HeaderCollectionViewItem), HeaderViewItemName);
			RegisterClassForItem (typeof (LabelCollectionViewItem), LabelViewItemName);
			RegisterClassForItem (typeof (ImageCollectionViewItem), ImageViewItemName);
		}
	
		public event EventHandler<NativeViews.NSEventArgs> KeyDownPressed;

		public override void KeyDown (NSEvent theEvent)
		{
			var args = new NativeViews.NSEventArgs (theEvent);
			KeyDownPressed?.Invoke (this, args);

			if (!args.Handled)
				base.KeyDown (theEvent);
		}

		void DataSource_RegionCollapsed (object sender, NSIndexPath e) => RegionCollapsed?.Invoke (this, EventArgs.Empty);

		void CollectionViewDelegate_DragBegin (object sender, NSIndexSet e) => DragBegin?.Invoke (this, EventArgs.Empty);

		void CollectionViewDelegate_SelectionChanged (object sender, NSSet e)
		{
			 if (e.Count == 0) {
				 return;
			 }
			 if (e.AnyObject is NSIndexPath indexPath) {
				SelectedIndexPath = indexPath;
			 }
		}

		public override void SetFrameSize (CGSize newSize)
		{
			base.SetFrameSize (newSize);
		
			RedrawItems (true, false, false, SelectedItem);
		}

		public override void MouseDown (NSEvent theEvent)
		{
			collectionViewDelegate.IsLastSelectionFromMouseDown = true;
			if (SelectedItem != null && theEvent.ClickCount > 1) {
				PerformActivateSelectedItem ();
			}
			base.MouseDown (theEvent);
		}

		NSIndexPath GetIndexPathFromItem (ToolboxWidgetItem item)
		{
			for (int i = 0; i < CategoryVisibilities.Count; i++) {
				int index = CategoryVisibilities [i].Items.IndexOf (item);
				if (index >= 0) {
					return NSIndexPath.FromItemSection (index, i);
				}
			}
			return null;
		}

		public void RedrawItems (bool invalidates, bool reloads,bool isNewData, ToolboxWidgetItem selectedWidgetItem)
		{
			NSIndexPath selected = null;
			if (!isNewData && selectedWidgetItem != null) {
				selected = GetIndexPathFromItem (selectedWidgetItem);
			}
			if (IsListMode) {
				flowLayout.ItemSize = new CGSize (Math.Max (Frame.Width - IconMargin, 1), LabelCollectionViewItem.ItemHeight);
			} else {
				flowLayout.ItemSize = new CGSize (ImageCollectionViewItem.Size.Width, ImageCollectionViewItem.Size.Height);
			}
			if (ShowCategories) {
				collectionViewDelegate.Width = (nfloat) Math.Max (Frame.Width - IconMargin, 1);
				collectionViewDelegate.Height = HeaderCollectionViewItem.SectionHeight;
			} else {
				collectionViewDelegate.Width = 0;
				collectionViewDelegate.Height = 0;
			}

			if (invalidates) {
				CollectionViewLayout.InvalidateLayout ();
			}
			if (reloads) {
				ReloadData ();
			}

			if (selected != null) {
				SelectionIndexPaths = new NSSet (selected);
			} else {
				SelectionIndexPaths = new NSSet ();
			}
		}

		public override void RightMouseUp (NSEvent theEvent)
		{
			collectionViewDelegate.IsLastSelectionFromMouseDown = true;
			base.RightMouseUp (theEvent);
			var point = ConvertPointFromView (theEvent.LocationInWindow, null);
			var indexPath = base.GetIndexPath (point);
			if (indexPath != null) {
				SelectedIndexPath = indexPath;
				MenuOpened?.Invoke (this, point);
			}
		}

		public override bool BecomeFirstResponder ()
		{
			IsFocused = true;
			RedrawSelectedItem ();
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		internal void ClearImageCache ()
		{
			dataSource.Clear ();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {

				collectionViewDelegate.DragBegin -= CollectionViewDelegate_DragBegin;
				collectionViewDelegate.SelectionChanged -= CollectionViewDelegate_SelectionChanged;

				DataSource = null;
				Delegate = null;
			}

			base.Dispose (disposing);
		}

		public void AddCategory (ToolboxWidgetCategory category)
		{
			var cat = new CategoryVisibility () { Category = category };
			cat.Items = category.Items.Where (s => s.IsVisible).ToList ();
			CategoryVisibilities.Add (cat);
		}
	}

	class CategoryVisibility
	{
		public ToolboxWidgetCategory Category { get; set; }

		public List<ToolboxWidgetItem> Items { get; set; }
	}
}
#endif