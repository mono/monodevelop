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

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Register ("CollectionView")]
	class MacToolboxWidget : NSCollectionView, IToolboxWidget, INativeChildView
	{
		const int IconMargin = 1;

		internal MacToolboxWidgetFlowLayoutDelegate collectionViewDelegate;
		internal NSCollectionViewFlowLayout flowLayout;

		public event EventHandler Focused;
		public event EventHandler DragBegin;
		public event EventHandler<CGPoint> MenuOpened;
		public event EventHandler SelectedItemChanged;
		public event EventHandler ActivateSelectedItem;
		public Action<NSEvent> MouseDownActivated { get; set; }

		readonly List<ToolboxWidgetCategory> categories = new List<ToolboxWidgetCategory> ();

		IPadWindow container;
		NSTextField messageTextField;
		ToolboxWidgetItem selectedItem;
		MacToolboxWidgetDataSource dataSource;

		bool listMode;
		bool showCategories = true;

		public IEnumerable<ToolboxWidgetCategory> Categories {
			get { return categories; }
		}
	
		protected virtual void OnActivateSelectedItem (EventArgs args)
		{
			ActivateSelectedItem?.Invoke (this, args);
		}

		protected virtual void OnSelectedItemChanged (EventArgs args)
		{
			SelectedItemChanged?.Invoke (this, args);
		}

		public ToolboxWidgetItem SelectedItem {
			get {
				return selectedItem;
			}
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnSelectedItemChanged (EventArgs.Empty);
				}
			}
		}

		public string CustomMessage {
			get => messageTextField.StringValue;
			set {
				if (string.IsNullOrEmpty (value)) {
					messageTextField.StringValue = "";
					messageTextField.Hidden = true;
				} else {
					messageTextField.StringValue = value;
					messageTextField.Hidden = false;
				}
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
				foreach (var category in this.categories) {
					foreach (var item in category.Items) {
						yield return item;
					}
				}
			}
		}

		public bool CanIconizeToolboxCategories {
			get {
				foreach (var category in categories) {
					if (category.CanIconizeItems)
						return true;
				}
				return false;
			}
		}

		public MacToolboxWidget (IPadWindow container) : base ()
		{
			this.container = container;
			container.PadContentShown += OnContainerIsShown;

			Initialize ();
		}

		// Called when created from unmanaged code
		public MacToolboxWidget (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MacToolboxWidget (NSCoder coder) : base (coder)
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
				var collectionViewItem = GetItem ((NSIndexPath)SelectionIndexPaths.ElementAt (0));
				if (collectionViewItem != null && collectionViewItem.View != null) {
					collectionViewItem.View.NeedsDisplay = true;
				}
			}
		}

		// Shared initialization code
		public void Initialize ()
		{
			TranslatesAutoresizingMaskIntoConstraints = false;
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
			DataSource = dataSource = new MacToolboxWidgetDataSource (categories);

			collectionViewDelegate.DragBegin += CollectionViewDelegate_DragBegin;
			collectionViewDelegate.SelectionChanged += CollectionViewDelegate_SelectionChanged;

			var fontSmall = NativeViewHelper.GetSystemFont (false, (int)NSFont.SmallSystemFontSize);
			messageTextField = NativeViewHelper.CreateLabel ("", NSTextAlignment.Center, fontSmall);
			messageTextField.LineBreakMode = NSLineBreakMode.ByWordWrapping;
			messageTextField.SetContentCompressionResistancePriority (250, NSLayoutConstraintOrientation.Horizontal);
			AddSubview (messageTextField);

			BackgroundColors = new NSColor [] { Styles.ToolbarBackgroundColor };
		}

		void CollectionViewDelegate_DragBegin (object sender, NSIndexSet e)
		{
			DragBegin?.Invoke (this, EventArgs.Empty);
		}

		void CollectionViewDelegate_SelectionChanged (object sender, NSSet e)
		{
			 if (e.Count == 0) {
				 return;
			 }
			 if (e.AnyObject is NSIndexPath indexPath) {
				 SelectedItem = categories [(int)indexPath.Section].Items [(int)indexPath.Item];
			 }
		}

		public override void SetFrameSize (CGSize newSize)
		{
			base.SetFrameSize (newSize);
			var frame = messageTextField.Frame;
			messageTextField.Frame = new CGRect (frame.Location, newSize);
		}

		public override void MouseDown (NSEvent theEvent)
		{
			base.MouseDown (theEvent);
			if (SelectedItem != null && theEvent.ClickCount > 1) {
				OnActivateSelectedItem (EventArgs.Empty);
			}

			MouseDownActivated?.Invoke (theEvent);
		}

		public void RedrawItems (bool invalidates, bool reloads)
		{
			NSIndexPath selected = null;
			if (SelectionIndexPaths.Count > 0) {
				selected = (NSIndexPath)SelectionIndexPaths.ElementAt (0);
			}
			if (IsListMode) {
				flowLayout.ItemSize = new CGSize (Frame.Width - IconMargin, LabelCollectionViewItem.ItemHeight);
			} else {
				flowLayout.ItemSize = new CGSize (ImageCollectionViewItem.Size.Width, ImageCollectionViewItem.Size.Height);
			}
			if (ShowCategories) {
				collectionViewDelegate.Width = Frame.Width - IconMargin;
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
			}
		}

		public override void RightMouseUp (NSEvent theEvent)
		{
			base.RightMouseUp (theEvent);
			var point = ConvertPointFromView (theEvent.LocationInWindow, null);
			var indexPath = base.GetIndexPath (point);
			if (indexPath != null) {
				SelectedItem = categories [(int)indexPath.Section].Items [(int)indexPath.Item];
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

		#region INativeChildView

		public void OnKeyPressed (object o, Gtk.KeyPressEventArgs ev)
		{

		}

		public void OnKeyReleased (object s, Gtk.KeyReleaseEventArgs ev)
		{

		}

		#endregion

		ToolboxWidgetCategory GetCategory (ToolboxWidgetItem item)
		{
			foreach (var category in Categories) {
				if (category.Items.Any (s => s == item)) {
					return category;
				}
			}
			return null;
		}

		ToolboxWidgetItem GetNextCategory (ToolboxWidgetCategory category)
		{
			return category;
		}

		internal void ClearData ()
		{
			categories.Clear ();
			dataSource.Clear ();
		}

		internal void OnContainerIsShown (object sender, EventArgs e)
		{
			RegisterClassForItem (typeof (HeaderCollectionViewItem), HeaderCollectionViewItem.Name);
			RegisterClassForItem (typeof (LabelCollectionViewItem), LabelCollectionViewItem.Name);
			RegisterClassForItem (typeof (ImageCollectionViewItem), ImageCollectionViewItem.Name);

			NSNotificationCenter.DefaultCenter.AddObserver (FrameChangedNotification, (s => {
				if (s.Object == this) {
					RedrawItems (true, false);
				}
			}));
		}

		protected override void Dispose (bool disposing)
		{
			if (container != null) {
				container.PadContentShown -= OnContainerIsShown;
			}

			collectionViewDelegate.DragBegin -= CollectionViewDelegate_DragBegin;
			collectionViewDelegate.SelectionChanged -= CollectionViewDelegate_SelectionChanged;

			DataSource = null;
			Delegate = null;

			base.Dispose (disposing);
		}

		public void AddCategory (ToolboxWidgetCategory category)
		{
			categories.Add (category);
			foreach (var item in category.Items) {
				if (item.Icon == null)
					continue;
			}
		}
	}
}
#endif