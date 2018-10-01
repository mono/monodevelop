using AppKit;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using System;
using System.Collections.Generic;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public class NativeLabel : NSTextField
	{
		public NativeLabel (string text, NSFont font = null, NSTextAlignment alignment = NSTextAlignment.Left)
		{
			TranslatesAutoresizingMaskIntoConstraints = false;
			StringValue = text ?? "";
			Font = font ?? NativeViewHelper.GetSystemFont (false);
			Editable = false;
			Bordered = false;
			Bezeled = false;
			DrawsBackground = false;
			Selectable = false;
			Alignment = alignment;
		}
	}

	public class GroupedTitle : NSTextView
	{

	}

	public class TableImage : NSImageView
	{
		public TableImage (Xwt.Drawing.Image image)
		{
			Image = image.ToNative ();
			TranslatesAutoresizingMaskIntoConstraints = false;
		}
		readonly CGSize forcedSize;
	}

	public class TableViewItem : NSStackView
	{
		const int size = 20;

		TableImage imageView;
		NativeLabel label;

		public TableViewItem ()
		{
			Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
			Alignment = NSLayoutAttribute.CenterY;
			Spacing = 10;
			Distribution = NSStackViewDistribution.Fill;
			TranslatesAutoresizingMaskIntoConstraints = false;
		}

		internal void OnInitializeView ()
		{
			imageView = new TableImage (Image);
			AddArrangedSubview (this.imageView);
			imageView.WidthAnchor.ConstraintEqualToConstant (size).Active = true;
			imageView.HeightAnchor.ConstraintEqualToConstant (size).Active = true;

			label = new NativeLabel (Label);
			AddArrangedSubview (label);
		}

		public Xwt.Drawing.Image Image { get; set; }
		public string Label { get; set; }

		public string Group { get; set; }
		public bool IsGrouped => string.IsNullOrEmpty (Group);
	}


	public class TableViewRowSelectionSource : NSTableViewSource
	{
		protected bool IsRowValid (nint row) => data != null && row >= 0 && row < data.Count;

		public event EventHandler SelectionChanged;

		readonly List<TableViewItem> data;

		public TableViewRowSelectionSource (List<TableViewItem> data)
		{
			this.data = data;
		}

		#region NSTableViewSource overrides

		public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
		{
			var cell = tableView.MakeView (tableColumn.Identifier, tableView) as TableViewItem;
			if (cell == null) {
				cell.OnInitializeView ();
			}
			return data [(int)row];
		}

		public override void SelectionDidChange (NSNotification notification)
		{
			if (notification.Object is NSTableView tableView && IsRowValid (tableView.SelectedRow)) {
				SelectionChanged?.Invoke (this, EventArgs.Empty);
			};
		}

		public override nint GetRowCount (NSTableView tableView) => data?.Count ?? 0;

		#endregion
	}

	public class TableListView : NSTableView
	{
		public event EventHandler SelectionChanged;

		readonly TableViewRowSelectionSource source;
		List<TableViewItem> data = new List<TableViewItem> ();

		public TableViewItem SelectedItem {
			get {
				if (SelectedRow < 0 || SelectedRow >= data.Count) {
					return null;
				}
				return data[(int)SelectedRow];
			}
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}

		public override void TakeDoubleValueFrom (NSObject sender)
		{
			base.TakeDoubleValueFrom (sender);
		}

		public TableListView ()
		{
			TranslatesAutoresizingMaskIntoConstraints = false;
			AllowsColumnSelection = true;
			AllowsExpansionToolTips = true;
			AllowsMultipleSelection = false;
			ColumnAutoresizingStyle = NSTableViewColumnAutoresizingStyle.Uniform;
			AutosaveTableColumns = false;
			FocusRingType = NSFocusRingType.None;
			GridStyleMask = NSTableViewGridStyle.SolidVerticalLine | NSTableViewGridStyle.DashedHorizontalGridLine;
			RowSizeStyle = NSTableViewRowSizeStyle.Custom;
			RowHeight = 17;

			source = new TableViewRowSelectionSource (data);
			Source = source;
			source.SelectionChanged += Source_SelectionChanged;
		}

		public void SetData (IEnumerable<TableViewItem> data)
		{
			this.data.Clear ();
			this.data.AddRange (data);
			ReloadData ();
		}

		void Source_SelectionChanged (object sender, EventArgs e)
		{
			SelectionChanged?.Invoke (this, EventArgs.Empty);
		}

		protected override void Dispose (bool disposing)
		{
			source.SelectionChanged -= Source_SelectionChanged;
			base.Dispose (disposing);
		}

	}
}
