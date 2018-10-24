#if MAC
using System;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide.Gui;
using Xwt;
using System.Linq;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Register ("CollectionView")]
	class MacToolboxWidget : NSCollectionView, IToolboxWidget, INativeChildView
	{
		public Action<NSEvent> MouseDownActivated { get; set; }
		public event EventHandler DragBegin;

		bool showCategories = true;
		bool listMode = false;

		readonly List<ToolboxWidgetCategory> categories = new List<ToolboxWidgetCategory> ();

		MacToolboxWidgetDataSource dataSource;
		internal MacToolboxWidgetFlowLayoutDelegate collectionViewDelegate;
		internal MacToolboxWidgetFlowLayout flowLayout;

		public IEnumerable<ToolboxWidgetCategory> Categories {
			get { return categories; }
		}
	
		public void HideTooltipWindow ()
		{
			//To implement
		}

		public event EventHandler SelectedItemChanged;
		protected virtual void OnSelectedItemChanged (EventArgs args)
		{
			HideTooltipWindow ();
			SelectedItemChanged?.Invoke (this, args);
		}

		public event EventHandler ActivateSelectedItem;
		public event EventHandler Focused;

		protected virtual void OnActivateSelectedItem (EventArgs args)
		{
			ActivateSelectedItem?.Invoke (this, args);
		}
	
		ToolboxWidgetItem selectedItem;
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

		IPadWindow container;

		public MacToolboxWidget (IPadWindow container) : base ()
		{
			this.container = container;
			container.PadContentShown += OnContainerIsShown;

			Initialize ();
		}

		ToolboxWidgetItem GetFirstSelectableElement ()
		{
			foreach (var category in Categories) {
				foreach (var item in category.Items) {
					return item;
				}
			}
			return null;
		}

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);

			if (selectedItem == null) {
				SelectedItem = GetFirstSelectableElement ();
			}
			return base.BecomeFirstResponder ();
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

		// Shared initialization code
		public void Initialize ()
		{
			flowLayout = new MacToolboxWidgetFlowLayout ();
			flowLayout.SectionHeadersPinToVisibleBounds = true;
			flowLayout.MinimumInteritemSpacing = 1;
			flowLayout.MinimumLineSpacing = 1;
			CollectionViewLayout = flowLayout;
		
			Delegate = collectionViewDelegate = new MacToolboxWidgetFlowLayoutDelegate ();
			Selectable = true;
			AllowsEmptySelection = true;
			DataSource = dataSource = new MacToolboxWidgetDataSource (categories);

			collectionViewDelegate.DragBegin += (s, e) => {
				DragBegin?.Invoke (this, EventArgs.Empty);
			};

			collectionViewDelegate.SelectionChanged += (s, e) => {
				if (e.Count == 0) {
					return;
				}
				if (e.AnyObject is NSIndexPath indexPath) {
					SelectedItem = categories[(int)indexPath.Section].Items[(int)indexPath.Item];
				}
			};

			BackgroundColors = new NSColor[] { Styles.SearchTextFieldLineBackgroundColor };

			TranslatesAutoresizingMaskIntoConstraints = false;
		}

		public override void MouseDown (NSEvent theEvent)
		{
			base.MouseDown (theEvent);
			if (this.SelectedItem != null && theEvent.ClickCount > 1) {
				OnActivateSelectedItem (EventArgs.Empty);
			}

			MouseDownActivated?.Invoke (theEvent);
		}

		const int IconMargin = 5;

		public void RedrawItems (bool invalidates, bool reloads)
		{
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

		public EventHandler<CGPoint> MenuOpened;

		public override void KeyDown (NSEvent theEvent)
		{
			base.KeyDown (theEvent);
		}

		#region IEncapsuledView

		void SelectItem (ToolboxWidgetItem item)
		{
			dataSource.SelectItem (this, item);
			SelectedItem = item;
		}

		public void OnKeyPressed (object o, Gtk.KeyPressEventArgs ev)
		{

		}

		public void OnKeyReleased (object s, Gtk.KeyReleaseEventArgs ev)
		{

		}

		#endregion

		void SetCategoryExpanded (ToolboxWidgetCategory item, bool v)
		{

		}

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

		public bool IsListMode {
			get => listMode;
			set {
				listMode = value;
				collectionViewDelegate.IsOnlyImage = dataSource.IsOnlyImage = !value;
			}
		}

		public bool ShowCategories {
			get => showCategories;
			set {
				showCategories = collectionViewDelegate.IsShowCategories = value;
			}
		}

		public void ScrollToSelectedItem ()
		{
			//to implement
		}

		public IEnumerable<ToolboxWidgetItem> AllItems {
			get {
				foreach (ToolboxWidgetCategory category in this.categories) {
					foreach (ToolboxWidgetItem item in category.Items) {
						yield return item;
					}
				}
			}
		}

		public void ClearCategories ()
		{
			categories.Clear ();
		}

		public string CustomMessage { get; set; }

		public bool CanIconizeToolboxCategories {
			get {
				foreach (ToolboxWidgetCategory category in categories) {
					if (category.CanIconizeItems)
						return true;
				}
				return false;
			}
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
			base.Dispose (disposing);
		}

		public void AddCategory (ToolboxWidgetCategory category)
		{
			categories.Add (category);
			foreach (ToolboxWidgetItem item in category.Items) {
				if (item.Icon == null)
					continue;
			}
		}
	}
}
#endif