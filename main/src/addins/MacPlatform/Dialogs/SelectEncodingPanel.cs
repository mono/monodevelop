// 
// MacOpenFileDialogHandler.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects.Text;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace MonoDevelop.Platform.Mac
{
	class SelectEncodingPanel : NSPanel
	{
		NSTableView allTable;
		NSTableView selectedTable;
		EncodingSource allSource;
		EncodingSource selectedSource;
		NSButton addButton, removeButton, upButton, downButton;
		
		public SelectEncodingPanel () : base ()	
		{
			var size = new SizeF (600, 400);
			float padding = 12;
			this.SetContentSize (size);
			
			var view = new NSView (new RectangleF (0, 0, size.Width, size.Height));
			var okButton = new NSButton () {
				Title = GettextCatalog.GetString ("OK"),
				Bordered = true,
				BezelStyle = NSBezelStyle.Rounded,
			};
			okButton.SetButtonType (NSButtonType.MomentaryPushIn);
			okButton.Activated += delegate {
				Dismiss (1);
			};
			this.DefaultButtonCell = okButton.Cell;
			
			var cancelButton = new NSButton () {
				Title = GettextCatalog.GetString ("Cancel"),
				Bordered = true,
				BezelStyle = NSBezelStyle.Rounded,
			};
			cancelButton.Activated += delegate {
				Dismiss (0);
			};
			var buttonBox = new MDBox (LayoutDirection.Horizontal, padding, 0) {
				new MDAlignment (cancelButton, true) { MinWidth = 96, MinHeight = 32 },
				new MDAlignment (okButton, true) { MinWidth = 96, MinHeight = 32 },
			};
			buttonBox.Layout ();
			var buttonView = buttonBox.View;
			var buttonRect = buttonView.Frame;
			buttonRect.Y = 12;
			buttonRect.X = size.Width - buttonRect.Width - padding;
			buttonView.Frame = buttonRect;
			view.AddSubview (buttonView);
			
			float buttonAreaTop = buttonRect.Height + padding * 2;
			
			var label = CreateLabel (GettextCatalog.GetString ("Available encodings:"));
			var labelSize = label.Frame.Size;
			float labelBottom = size.Height - 12 - labelSize.Height;
			label.Frame = new RectangleF (12, labelBottom, labelSize.Width, labelSize.Height);
			view.AddSubview (label);
			
			var moveButtonWidth = 32;
			var tableHeight = labelBottom - buttonAreaTop - padding;
			var tableWidth = size.Width / 2 - padding * 3 - moveButtonWidth + padding / 2;
			
			allTable = new NSTableView (new RectangleF (padding, buttonAreaTop, tableWidth, tableHeight));
			allTable.HeaderView = null;
			var allScroll = new NSScrollView (allTable.Frame) {
				BorderType = NSBorderType.BezelBorder,
				AutohidesScrollers = true,
				HasVerticalScroller = true,
				DocumentView = allTable,
			};
			view.AddSubview (allScroll);
			
			float center = (size.Width + padding) / 2;
			
			var selectedLabel = CreateLabel (GettextCatalog.GetString ("Encodings shown in menu:"));
			var selectedLabelSize = selectedLabel.Frame.Size;
			selectedLabel.Frame = new RectangleF (center, labelBottom, selectedLabelSize.Width, selectedLabelSize.Height);
			view.AddSubview (selectedLabel);
			
			selectedTable = new NSTableView (new RectangleF (center, buttonAreaTop, tableWidth, tableHeight));
			selectedTable.HeaderView = null;
			var selectedScroll = new NSScrollView (selectedTable.Frame) {
				BorderType = NSBorderType.BezelBorder,
				AutohidesScrollers = true,
				HasVerticalScroller = true,
				DocumentView = selectedTable,
			};
			view.AddSubview (selectedScroll);
			
			float buttonLevel = tableHeight / 2 + buttonAreaTop;
			
			var goRightImage = NSImage.ImageNamed ("NSGoRightTemplate");
			
			addButton = new NSButton (
				new RectangleF (tableWidth + padding * 2, buttonLevel + padding / 2,
					moveButtonWidth, moveButtonWidth)) {
				//Title = "\u2192",
				BezelStyle = NSBezelStyle.SmallSquare,
				Image = goRightImage
			};
			addButton.Activated += Add;
			view.AddSubview (addButton);
			
			removeButton = new NSButton (
				new RectangleF (tableWidth + padding * 2, buttonLevel - padding / 2 - moveButtonWidth,
					moveButtonWidth, moveButtonWidth)) {
				//Title = "\u2190",
				BezelStyle = NSBezelStyle.SmallSquare,
				Image = NSImage.ImageNamed ("NSGoLeftTemplate"),
			};
			removeButton.Activated += Remove;
			view.AddSubview (removeButton);
			
			upButton = new NSButton (
				new RectangleF (center + tableWidth + padding, buttonLevel + padding / 2,
					moveButtonWidth, moveButtonWidth)) {
				//Title = "\u2191",
				BezelStyle = NSBezelStyle.SmallSquare,
				Image = MakeRotatedCopy (goRightImage, 90),
			};
			upButton.Activated += MoveUp;
			view.AddSubview (upButton);
			
			downButton = new NSButton (
				new RectangleF (center + tableWidth + padding, buttonLevel - padding / 2 - moveButtonWidth,
					moveButtonWidth, moveButtonWidth)) {
				//Title = "\u2193",
				BezelStyle = NSBezelStyle.SmallSquare,
				Image = MakeRotatedCopy (goRightImage, -90),
			};
			downButton.Activated += MoveDown;
			view.AddSubview (downButton);
			
			var allColumn = new NSTableColumn () {
				DataCell = new NSTextFieldCell () { Wraps = true },
				Width = tableWidth
			};
			allTable.AddColumn (allColumn);
			allTable.DataSource = allSource = new EncodingSource (TextEncoding.SupportedEncodings);
			allTable.Delegate = new EncodingAllDelegate (this);
			
			var selectedColumn = new NSTableColumn () {
				DataCell = new NSTextFieldCell () { Wraps = true },
				Width = tableWidth
			};
			selectedTable.AddColumn (selectedColumn);
			selectedTable.DataSource = selectedSource = new EncodingSource (TextEncoding.ConversionEncodings);
			selectedTable.Delegate = new EncodingSelectedDelegate (this);
			
			UpdateButtons ();
			
			this.ContentView = view;
		}
		
		NSImage MakeRotatedCopy (NSImage original, float degrees)
		{
			var copy = new NSImage (original.Size);
			copy.LockFocus ();
			try {
				var rot = new NSAffineTransform ();
				rot.Translate (original.Size.Width / 2, original.Size.Height / 2);
				rot.RotateByDegrees (degrees);
				rot.Translate (-original.Size.Width / 2, -original.Size.Height / 2);
				rot.Concat ();
				original.Draw (PointF.Empty, RectangleF.Empty, NSCompositingOperation.Copy, 1);
			} finally {
				copy.UnlockFocus ();
			}
			return copy;
		}

		void Add (object sender, EventArgs e)
		{
			var fromIndex = allTable.SelectedRow;
			var encoding = allSource.encodings[fromIndex];
			var toIndex = selectedTable.SelectedRow + 1;
			if (toIndex <= 0)
				toIndex = selectedSource.encodings.Count;
			selectedSource.encodings.Insert (toIndex, encoding);
			selectedTable.ReloadData ();
			selectedTable.SelectRows (new NSIndexSet ((uint)(toIndex)), false);
			UpdateButtons ();
		}
		
		void Remove (object sender, EventArgs e)
		{
			var index = selectedTable.SelectedRow;
			selectedSource.encodings.RemoveAt (index);
			selectedTable.ReloadData ();
			if (index >= selectedSource.encodings.Count)
				index--;
			selectedTable.SelectRows (new NSIndexSet ((uint)(index)), false);
			UpdateButtons ();
		}

		void MoveUp (object sender, EventArgs e)
		{
			var index = selectedTable.SelectedRow;
			var selected = selectedSource.encodings[index];
			selectedSource.encodings[index] = selectedSource.encodings[index - 1];
			selectedSource.encodings[index - 1] = selected;
			selectedTable.ReloadData ();
			selectedTable.SelectRows (new NSIndexSet ((uint)(index - 1)), false);
			UpdateButtons ();
		}
		
		void MoveDown (object sender, EventArgs e)
		{
			var index = selectedTable.SelectedRow;
			var selected = selectedSource.encodings[index];
			selectedSource.encodings[index] = selectedSource.encodings[index + 1];
			selectedSource.encodings[index + 1] = selected;
			selectedTable.ReloadData ();
			selectedTable.SelectRows (new NSIndexSet ((uint)(index + 1)), false);
			UpdateButtons ();
		}
		
		void UpdateButtons ()
		{
			var allIndex = allTable.SelectedRow;
			var allEncoding = allIndex >= 0? allSource.encodings[allIndex] : null;
			addButton.Enabled = allEncoding != null && !selectedSource.encodings.Any (e => e.Id == allEncoding.Id);
			
			var selectedIndex = selectedTable.SelectedRow;
			removeButton.Enabled = selectedIndex >= 0 && selectedSource.encodings.Count > 0;
			upButton.Enabled = selectedIndex > 0;
			downButton.Enabled = selectedIndex >= 0 && selectedIndex < selectedSource.encodings.Count - 1;
		}
		
		static NSTextField CreateLabel (string text)
		{
			var label = new NSTextField () {
				StringValue = text,
				DrawsBackground = false,
				Bordered = false,
				Editable = false,
				Selectable = false,
			};
			label.SizeToFit ();
			return label;
		}
		
		public int RunModal ()
		{
			this.DidResignKey += StopSharedAppModal;
			try {
				return SaveIfOk (NSApplication.SharedApplication.RunModalForWindow (this));
			} finally {
				this.DidResignKey -= StopSharedAppModal;
			}
		}
		
		[Export ("sheetSel")]
		void SheetSel ()
		{
		}
		
		bool sheet;
		
		public int RunModalSheet (NSWindow parent)
		{
			var sel = new MonoMac.ObjCRuntime.Selector ("sheetSel");
			NSApplication.SharedApplication.BeginSheet (this, parent, this, sel, IntPtr.Zero);
			this.DidResignKey += StopSharedAppModal;
			try {
				sheet = true;
				return SaveIfOk (NSApplication.SharedApplication.RunModalForWindow (this));
			} finally {
				sheet = false;
				this.DidResignKey -= StopSharedAppModal;
			}
		}
		
		int SaveIfOk (int ret)
		{
			if (ret != 0)
				TextEncoding.ConversionEncodings = selectedSource.encodings.ToArray ();
			return ret;
		}

		static void StopSharedAppModal (object sender, EventArgs e)
		{
			NSApplication.SharedApplication.StopModal ();
		}
		
		void Dismiss (int code)
		{
			if (sheet) {
				NSApplication.SharedApplication.EndSheet (this, code);
				OrderOut (this);
			} else {
				NSApplication.SharedApplication.StopModal ();
				OrderOut (this);
			}
		}
		
		class EncodingSource : NSTableViewDataSource
		{
			public List<TextEncoding> encodings;
			
			public EncodingSource (IEnumerable<TextEncoding> encodings)
			{
				this.encodings = new List<TextEncoding> (encodings);
			}
			
			public override int GetRowCount (NSTableView tableView)
			{
				return encodings.Count;
			}
			
			public override NSObject GetObjectValue (NSTableView tableView, NSTableColumn tableColumn, int row)
			{
				var encoding = encodings[row];
				return new NSString (string.Format ("{0} ({1})", encoding.Name, encoding.Id));
			}
		}
		
		class EncodingAllDelegate : NSTableViewDelegate
		{
			SelectEncodingPanel parent;
			
			public EncodingAllDelegate (SelectEncodingPanel parent)
			{
				this.parent = parent;
			}
			
			public override void SelectionDidChange (NSNotification notification)
			{
				parent.UpdateButtons ();
			}
		}
		
		class EncodingSelectedDelegate : NSTableViewDelegate
		{
			SelectEncodingPanel parent;
			
			public EncodingSelectedDelegate (SelectEncodingPanel parent)
			{
				this.parent = parent;
			}
			
			public override void SelectionDidChange (NSNotification notification)
			{
				parent.UpdateButtons ();
			}
		}
	}
}
