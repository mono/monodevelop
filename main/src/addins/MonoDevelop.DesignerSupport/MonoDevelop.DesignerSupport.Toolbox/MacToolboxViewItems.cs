//
// MacToolboxViewItems.cs - Native views (NSCollectionItems) required to use with MacToolboxWidget (NSCollectionView)
//
// Author:
//   Jose Medrano <josmed@microsoft.com>
//
// Copyright (C) 2018 Microsoft, Corp
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#if MAC
using System;
using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Components.Mac;
using Xwt.Mac;
using System.Text.RegularExpressions;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	abstract class BaseCollectionViewItem : NSCollectionViewItem
	{
		//public override string Description => TextField.StringValue;
		protected ContentCollectionViewItem contentCollectionView;
		public NSImage SelectedImage { get; set; }
		public NSImage Image { get; set; }

		public override bool Selected {
			get => base.Selected;
			set {
				base.Selected = value;
				if (contentCollectionView != null) {
					contentCollectionView.IsSelected = value;
				}
				Refresh ();
			}
		}

		public abstract void Refresh ();

		internal void SetCollectionView (NSCollectionView collectionView) => contentCollectionView.SetCollectionView (collectionView);

		public BaseCollectionViewItem (IntPtr handle) : base (handle)
		{

		}

		protected void OnLoadContentView ()
		{
			View = contentCollectionView = new ContentCollectionViewItem ();

			contentCollectionView.PerformPress += ContentCollectionView_PerformPress;
		}

		void ContentCollectionView_PerformPress (object sender, EventArgs e)
		{
			var widget = ((MacToolboxWidget)CollectionView);
			var currentIndex = widget.GetIndexPath (this);
			if (currentIndex != null) {
				CollectionView.SelectItems (new NSSet (currentIndex), NSCollectionViewScrollPosition.None);
				widget.PerformActivateSelectedItem ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				contentCollectionView.PerformPress -= ContentCollectionView_PerformPress;
			}
			base.Dispose (disposing);
		}
	}

	[Register ("LabelCollectionViewItem")]
	class LabelCollectionViewItem : BaseCollectionViewItem
	{
		internal const int ItemHeight = 30;
		public override string Description => TextField.StringValue;

		public LabelCollectionViewItem (IntPtr handle) : base (handle)
		{
		}

		public void SetText (string formattedText) 
		{
			TextField.AttributedStringValue = NativeViewHelper.GetAttributedStringFromFormattedText (formattedText);
		}

		public override void Refresh ()
		{
			if (Selected) {
				ImageView.Image = SelectedImage;
				TextField.TextColor = Styles.LabelSelectedForegroundColor;
			} else {
				ImageView.Image = Image;
				TextField.TextColor = NSColor.LabelColor;
			}
		}

		public override void LoadView ()
		{
			OnLoadContentView ();

			View.Identifier = MacToolboxWidget.LabelViewItemName;

			ImageView = new NSImageView () { TranslatesAutoresizingMaskIntoConstraints = false };
			ImageView.AccessibilityElement = ImageView.Cell.AccessibilityElement = false;
			contentCollectionView.AddArrangedSubview (ImageView);

			TextField = new NSLabel {
				StringValue = String.Empty,
				Alignment = NSTextAlignment.Left,
				Font = NativeViewHelper.GetSystemFont (false, (int)NSFont.SmallSystemFontSize)
			};
			TextField.AccessibilityElement = TextField.Cell.AccessibilityElement = false;
			contentCollectionView.AddArrangedSubview (TextField);
			contentCollectionView.EdgeInsets = new NSEdgeInsets (0, 7, 0, 0);
		}
	}

	[Register ("ImageCollectionViewItem")]
	class ImageCollectionViewItem : BaseCollectionViewItem
	{
		internal static CGSize Size = new CGSize (23, 23);

		public override void Refresh ()
		{
			if (Selected) {
				ImageView.Image = SelectedImage;
			} else {
				ImageView.Image = Image;
			}
		}

		public override void LoadView ()
		{
			OnLoadContentView ();

			View.Identifier = MacToolboxWidget.ImageViewItemName;

			contentCollectionView.EdgeInsets = new NSEdgeInsets (0, 0, 0, 0);

			ImageView = new NSImageView () { TranslatesAutoresizingMaskIntoConstraints = false };
			ImageView.AccessibilityElement = ImageView.Cell.AccessibilityElement = false;

			contentCollectionView.AddArrangedSubview (ImageView);
			ImageView.CenterXAnchor.ConstraintEqualToAnchor (contentCollectionView.CenterXAnchor, 0).Active = true;
		}
	
		public ImageCollectionViewItem (IntPtr handle) : base (handle)
		{

		}
	}

	[Register ("HeaderCollectionViewItem")]
	class HeaderCollectionViewItem : NSButton, INSCollectionViewSectionHeaderView
	{
		static readonly NSImage CollapsedImage = ImageService.GetIcon ("md-disclose-arrow-down", Gtk.IconSize.Menu).ToNSImage ();
		static readonly NSImage ExpandedImage = ImageService.GetIcon ("md-disclose-arrow-up", Gtk.IconSize.Menu).ToNSImage ();

		public NSTextField TitleTextField { get; private set; }
		public MacToolboxWidget CollectionView { get; internal set; }
		public NSIndexPath IndexPath { get; internal set; }

		internal const int SectionHeight = 25;

		NSImageView ExpanderImageView;

		bool isCollapsed;
		public bool IsCollapsed {
			get => isCollapsed;
			internal set {
				isCollapsed = value;
				ExpanderImageView.Image = value ? CollapsedImage : ExpandedImage;
			}
		}

		public event EventHandler Focused;

		public override bool CanBecomeKeyView => true;

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		public void SetText (string formattedText)
		{
			TitleTextField.AttributedStringValue = NativeViewHelper.GetAttributedStringFromFormattedText (formattedText);
			TitleTextField.AccessibilityTitle = Regex.Replace (formattedText, @"<[^>]*>", string.Empty);
		}

		public HeaderCollectionViewItem (IntPtr handle) : base (handle)
		{
			BezelStyle = NSBezelStyle.RegularSquare;
			SetButtonType (NSButtonType.OnOff);
			Bordered = false;

			Font = NativeViewHelper.GetSystemFont (false, (int)NSFont.SmallSystemFontSize);
			Title = "";

			TitleTextField = new NSLabel {
				StringValue = String.Empty,
				Font = Font,
				AccessibilityElement = false
			};

			AddSubview (TitleTextField);
			TitleTextField.LeftAnchor.ConstraintEqualToAnchor (LeftAnchor, 10).Active = true;
			TitleTextField.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor, 0).Active = true;

			ExpanderImageView = new NSImageView () {  TranslatesAutoresizingMaskIntoConstraints = false, AccessibilityElement = false };
			AddSubview (ExpanderImageView);

			ExpanderImageView.RightAnchor.ConstraintEqualToAnchor (RightAnchor, -5).Active = true;
			ExpanderImageView.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor, 0).Active = true;

			WantsLayer = true;
			Layer.BackgroundColor = Styles.HeaderBackgroundColor.CGColor;
			Layer.BorderColor = Styles.HeaderBorderBackgroundColor.CGColor;
			Layer.BorderWidth = 1;
		
			IsCollapsed = isCollapsed;
		}
	}

	class ContentCollectionViewItem : NSStackView, INSAccessibilityButton
	{
		public event EventHandler PerformPress;

		bool isSelected;
		public bool IsSelected {
			get => isSelected;
			set {
				if (isSelected == value) {
					return;
				}
				isSelected = value;

				RefreshLayer ();
			}
		}

		internal void RefreshLayer ()
		{
			if (isSelected) {
				if (collectionView == null || collectionView.IsFocused) {
					Layer.BackgroundColor = Styles.CellBackgroundSelectedColor.CGColor;
				} else {
					Layer.BackgroundColor = Styles.CellBackgroundUnfocusedSelectedColor.CGColor;
				}
			} else {
				Layer.BackgroundColor = NSColor.Clear.CGColor;
			}
		}

		public override bool AccessibilityPerformPress ()
		{
			PerformPress?.Invoke (this, EventArgs.Empty);
			return true;
		}

		MacToolboxWidget collectionView;
		public ContentCollectionViewItem ()
		{
			AccessibilityElement = true;
			Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
			TranslatesAutoresizingMaskIntoConstraints = false;
			WantsLayer = true;
			IsSelected = isSelected;
		}

		internal void SetCollectionView (NSCollectionView value)
		{
			collectionView = value as MacToolboxWidget;
		}
	}
}
#endif