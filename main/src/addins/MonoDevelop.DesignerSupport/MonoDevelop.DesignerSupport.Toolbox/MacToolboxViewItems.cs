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
using MonoDevelop.DesignerSupport.Toolbox.NativeViews;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Register ("LabelCollectionViewItem")]
	class LabelCollectionViewItem : NSCollectionViewItem
	{
		internal const int ItemHeight = 30;
		internal const string Name = "LabelViewItem";

		public override string Description => TextField.StringValue;

		public override bool Selected {
			get => base.Selected;
			set {
				base.Selected = value;
				if (contentCollectionView != null) {
					contentCollectionView.IsSelected = value;
				}
			}
		}

		ContentCollectionViewItem contentCollectionView;
		public override void LoadView ()
		{
			View = contentCollectionView = new ContentCollectionViewItem ();
			View.Identifier = Name;
			View.AccessibilityElement = false;

			ImageView = new NSImageView ();
			contentCollectionView.AddArrangedSubview (ImageView);
			TextField = NativeViewHelper.CreateLabel ("", NSTextAlignment.Left, NativeViewHelper.GetSystemFont (false, (int)NSFont.SmallSystemFontSize));
			contentCollectionView.AddArrangedSubview (TextField);
			contentCollectionView.EdgeInsets = new NSEdgeInsets (0, 7, 0, 0);
		}

		public LabelCollectionViewItem (IntPtr handle) : base (handle)
		{

		}
	}

	[Register ("HeaderCollectionViewItem")]
	class HeaderCollectionViewItem : ExpanderButton, INSCollectionViewSectionHeaderView
	{
		static readonly NSImage CollapsedImage = ImageService.GetIcon ("md-disclose-arrow-down", Gtk.IconSize.Menu).ToNative ();
		static readonly NSImage ExpandedImage = ImageService.GetIcon ("md-disclose-arrow-up", Gtk.IconSize.Menu).ToNative ();

		internal const int SectionHeight = 25;
		internal const string Name = "HeaderViewItem";

		bool isCollapsed;
		public bool IsCollapsed {
			get => isCollapsed;
			internal set {
				isCollapsed = value;
				ExpanderImage = value ? CollapsedImage : ExpandedImage;
			}
		}

		public MacToolboxWidget CollectionView { get; internal set; }
		public NSIndexPath IndexPath { get; internal set; }

		public HeaderCollectionViewItem (IntPtr handle) : base (handle)
		{

		}
	}

	[Register ("ImageCollectionViewItem")]
	class ImageCollectionViewItem : NSCollectionViewItem
	{
		internal static CGSize Size = new CGSize (23, 23);

		internal const string Name = "ImageViewItem";
		const int margin = 5;

		public override bool Selected {
			get => base.Selected;
			set {
				base.Selected = value;
				if (contentCollectionView != null) {
					contentCollectionView.IsSelected = value;
				}
			}
		}

		public  NSImage Image {
			get => contentCollectionView.BackgroundImage;
			set {
				contentCollectionView.BackgroundImage = value;
			}
		}

		public string AccessibilityTitle {
			get => contentCollectionView.AccessibilityTitle;
			set {
				contentCollectionView.AccessibilityTitle = value;
			}
		}

		public bool AccessibilityElement {
			get => contentCollectionView.AccessibilityElement;
			set {
				contentCollectionView.AccessibilityElement = value;
			}
		}

		ContentCollectionViewItem contentCollectionView;
		public override void LoadView ()
		{
			View = contentCollectionView = new ContentCollectionViewItem ();
			contentCollectionView.Identifier = Name;
			contentCollectionView.EdgeInsets = new NSEdgeInsets (0, 0, 0, 0);
		}

		public ImageCollectionViewItem (IntPtr handle) : base (handle)
		{

		}
	}

	class ContentCollectionViewItem : NSStackView
	{
		public NSColor BackgroundSelectedColor { get; set; } = NSColor.SecondarySelectedControl;
		public NSImage BackgroundImage { get; internal set; }

		bool isSelected;
		public bool IsSelected {
			get => isSelected;
			set {
				if (isSelected == value) {
					return;
				}
				isSelected = value;
				Layer.BackgroundColor = value ? BackgroundSelectedColor.CGColor : NSColor.Clear.CGColor;
				NeedsDisplay = true;
			}
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			if (BackgroundImage != null) {
				var context = NSGraphicsContext.CurrentContext;
				context.SaveGraphicsState ();

				var center = (Frame.Width - BackgroundImage.Size.Width) / 2;
				BackgroundImage.Draw (new CGRect (center, center, BackgroundImage.Size.Width, BackgroundImage.Size.Height));
				context.RestoreGraphicsState ();
			}
		}

		public ContentCollectionViewItem ()
		{
			Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
			TranslatesAutoresizingMaskIntoConstraints = false;
			WantsLayer = true;
			Layer.BackgroundColor = NSColor.Clear.CGColor;
		}
	}
}
#endif