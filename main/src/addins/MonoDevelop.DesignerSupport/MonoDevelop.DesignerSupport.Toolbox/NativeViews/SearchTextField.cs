using System;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class SearchTextField : NSSearchField
	{
		public string Text {
			get { return StringValue; }
			set {
				StringValue = value;
			}
		}

		public SearchTextField ()
		{
			WantsLayer = true;
		}

		public SearchTextField (IntPtr handle) : base (handle)
		{
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);
			if (Layer != null) {
				Layer.BorderWidth = Styles.SearchTextFieldLineBorderWidth;
				Layer.BorderColor = Styles.SearchTextFieldLineBorderColor.CGColor;
				Layer.BackgroundColor = Styles.SearchTextFieldLineBackgroundColor.CGColor;
			}
		}
	}
}
