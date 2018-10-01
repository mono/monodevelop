using System;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Register ("ProgressIndicator")]
	public class ProgressIndicator : NSProgressIndicator
	{
		NSTextField titleLabel;

		#region Constructors

		// Called when created from unmanaged code
		public ProgressIndicator (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		public ProgressIndicator ()
		{
			Initialize ();


		}

		[Export ("init:")]
		public ProgressIndicator (CGRect rect) : base (rect)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public ProgressIndicator (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
			titleLabel = new NSTextField ();
			AddSubview (titleLabel);

			//titleLabel.CenterXAnchor.ConstraintEqualToAnchor(CenterXAnchor, 0).Active = true;
			//titleLabel.CenterYAnchor.ConstraintEqualToAnchor(CenterYAnchor, 0).Active = true;

			//TranslatesAutoresizingMaskIntoConstraints = false;
			Indeterminate = true;
			Style = NSProgressIndicatorStyle.Spinning;
			UsesThreadedAnimation = false;
		}

		#endregion

		#region Properties

		public string Title {
			get {
				return titleLabel.StringValue;
			}
			set {
				titleLabel.StringValue = value;
			}
		}

		#endregion
	}
}
