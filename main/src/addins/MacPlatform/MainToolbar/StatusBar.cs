//
// StatusBar.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using AppKit;
using Foundation;
using CoreAnimation;
using CoreGraphics;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	class StatusIcon : NSView, StatusBarIcon
	{
		StatusBar bar;
		NSImageView imageView;

		public StatusIcon (StatusBar bar) : base (CGRect.Empty)
		{
			imageView = new NSImageView (CGRect.Empty);
			AddSubview (imageView);

			var trackingArea = new NSTrackingArea (CGRect.Empty, NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.InVisibleRect | NSTrackingAreaOptions.MouseEnteredAndExited, this, null);
			AddTrackingArea (trackingArea);

			this.bar = bar;
		}

		public override CGRect Frame {
			get {
				return base.Frame;
			}
			set {
				base.Frame = value;
				imageView.Frame = new CGRect (0, 0, value.Width, value.Height);
			}
		}

		public void SetAlertMode (int seconds)
		{
			// Create fade-out fade-in animation.
		}

		public new void Dispose ()
		{
			bar.RemoveStatusIcon (this);
			RemoveFromSuperview ();
			base.Dispose ();
		}

		public new string ToolTip {
			get;
			set;
		}

		Xwt.Drawing.Image image;
		public Xwt.Drawing.Image Image {
			get { return image; }
			set {
				image = value;
				imageView.Image = value.ToNSImage ();
				SetFrameSize (new CGSize (image.Width, image.Height));
			}
		}

		public override void MouseEntered (NSEvent theEvent)
		{
			if (Entered != null) {
				Entered (this, EventArgs.Empty);
			}
		}

		public override void MouseExited (NSEvent theEvent)
		{
			if (Exited != null) {
				Exited (this, EventArgs.Empty);
			}
		}

		public override void MouseUp (NSEvent theEvent)
		{
			NotifyClicked (StatusBar.NSEventButtonToXwt (theEvent));
		}

		internal void NotifyClicked (Xwt.PointerButton button)
		{
			if (Clicked != null)
				Clicked (this, new StatusBarIconClickedEventArgs {
					Button = button,
				});
		}

		public event EventHandler<StatusBarIconClickedEventArgs> Clicked;
		public event EventHandler<EventArgs> Entered;
		public event EventHandler<EventArgs> Exited;
	}

	class BuildResultsView : NSView
	{
		NSAttributedString resultString;
		int resultCount;
		public int ResultCount { 
			get {
				return resultCount;
			}
			set {
				resultCount = value;
				resultString = new NSAttributedString (value.ToString (), foregroundColor: NSColor.Text,
					font: NSFont.SystemFontOfSize (NSFont.SmallSystemFontSize - 1));
				ResizeToFit ();
			}
		}

		NSImage iconImage;
		public NSImage IconImage {
			get {
				return iconImage;
			}
			set {
				iconImage = value;
				ResizeToFit ();
			}
		}

		public BuildResultsView () : base (new CGRect (0, 0, 0, 0))
		{
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			if (iconImage == null || resultString == null) {
				return;
			}

			iconImage.Draw (new CGRect (0, (Frame.Size.Height - iconImage.Size.Height) / 2 + 0.5, iconImage.Size.Width, iconImage.Size.Height));
			resultString.DrawAtPoint (new CGPoint (iconImage.Size.Width, (Frame.Size.Height - resultString.Size.Height) / 2));
		}

		void ResizeToFit ()
		{
			if (iconImage == null || resultString == null) {
				return;
			}

			var stringSize = resultString.GetSize ();
			Frame = new CGRect (Frame.X, Frame.Y, iconImage.Size.Width + stringSize.Width, Frame.Height);
			NeedsDisplay = true;
		}

		public override void MouseDown (NSEvent theEvent)
		{
			IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ().BringToFront ();
		}
	}

	[Register]
	class StatusBar : NSTextField, MonoDevelop.Ide.StatusBar
	{
		public enum MessageType
		{
			Ready,
			Information,
			Warning,
			Error,
		}

		const string ProgressLayerFadingId = "ProgressLayerFading";
		const string growthAnimationKey = "bounds";
		StatusBarContextHandler ctxHandler;
		Stack<double> progressMarks = new Stack<double> ();
		bool currentTextIsMarkup;
		string text;
		MessageType messageType;
		NSColor textColor;
		NSImage image;
		IconId icon;
		AnimatedIcon iconAnimation;
		IDisposable xwtAnimation;
		readonly BuildResultsView buildResults;

		NSAttributedString GetStatusString (string text, NSColor color)
		{
			nfloat fontSize = NSFont.SystemFontSize;
			if (Window != null) {
				fontSize -= Window.Screen.BackingScaleFactor == 2 ? 2 : 1;
			} else {
				fontSize -= 1;
			}

			Console.WriteLine ("Font size: {0}", fontSize);
			return new NSAttributedString (text, new NSStringAttributes {
				ForegroundColor = color,
				ParagraphStyle = new NSMutableParagraphStyle {
					HeadIndent = imageView.Frame.Width,
					LineBreakMode = NSLineBreakMode.TruncatingMiddle,
				},
				Font = NSFont.SystemFontOfSize (fontSize),
			});
		}

		readonly NSImageView imageView = new NSImageView {
			ImageFrameStyle = NSImageFrameStyle.None,
			Editable = false,
		};

		readonly NSTextField textField = new NSTextField {
			AllowsEditingTextAttributes = true,
			Bordered = false,
			DrawsBackground = false,
			Bezeled = false,
			Editable = false,
			Selectable = false,
		};
		NSTrackingArea textFieldArea;
		CALayer progressLayer;

		TaskEventHandler updateHandler;
		public StatusBar ()
		{
			AllowsEditingTextAttributes = Selectable = Editable = false;
			LoadStyles ();
			Ide.Gui.Styles.Changed += (o, e) => LoadStyles ();
			textField.Cell = new VerticallyCenteredTextFieldCell (yOffset: -0.5f);
			textField.Cell.StringValue = "";
			textField.Cell.PlaceholderAttributedString = GetStatusString (BrandingService.ApplicationName, ColorForType (MessageType.Ready));

			// The rect is empty because we use InVisibleRect to track the whole of the view.
			textFieldArea = new NSTrackingArea (CGRect.Empty, NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.InVisibleRect, this, null);
			textField.AddTrackingArea (textFieldArea);

			imageView.Image = ImageService.GetIcon (Stock.StatusSteady).ToNSImage ();

			buildResults = new BuildResultsView ();
			buildResults.Hidden = true;
			AddSubview (buildResults);

			// Fixes a render glitch of a whiter bg than the others.
			if (MacSystemInformation.OsVersion >= MacSystemInformation.Yosemite)
				BezelStyle = NSTextFieldBezelStyle.Rounded;

			WantsLayer = true;
			Layer.CornerRadius = MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 6 : 4;
			ctxHandler = new StatusBarContextHandler (this);

			updateHandler = delegate {
				int ec = 0, wc = 0;

				foreach (var t in TaskService.Errors) {
					if (t.Severity == TaskSeverity.Error)
						ec++;
					else if (t.Severity == TaskSeverity.Warning)
						wc++;
				}

				Runtime.RunInMainThread (delegate {
					buildResults.Hidden = (ec == 0 && wc == 0);
					buildResults.ResultCount = ec > 0 ? ec : wc;

					buildImageId = ec > 0 ? "md-status-error-count" : "md-status-warning-count";
					buildResults.IconImage = ImageService.GetIcon (buildImageId, Gtk.IconSize.Menu).ToNSImage ();

					RepositionStatusIcons ();
				});
			};

			updateHandler (null, null);

			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidChangeBackingPropertiesNotification,
			                                                notification => Runtime.RunInMainThread (() => {
																ReconstructString ();
																RepositionContents ();
															}));

			TaskService.Errors.TasksAdded += updateHandler;
			TaskService.Errors.TasksRemoved += updateHandler;

			AddSubview (imageView);
			AddSubview (textField);
		}

		void LoadStyles (object sender = null, EventArgs args = null)
		{
			textColor = ColorForType (messageType);
			ReconstructString ();
		}

		protected override void Dispose (bool disposing)
		{
			TaskService.Errors.TasksAdded -= updateHandler;
			TaskService.Errors.TasksRemoved -= updateHandler;
			Ide.Gui.Styles.Changed -= LoadStyles;
			base.Dispose (disposing);
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);
			if (statusIcons.Count == 0 || buildResults.Hidden) {
				return;
			}

			var x = LeftMostStatusItemX ();
			var sepRect = new CGRect (x - 9, MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 5 : 4, 1, 16);
			if (!sepRect.IntersectsWith (dirtyRect)) {
				return;
			}

			NSColor.LightGray.SetFill ();
			NSBezierPath.FillRect (sepRect);
		}

		public override void ViewDidMoveToWindow ()
		{
			base.ViewDidMoveToWindow ();
			ReconstructString ();
			RepositionContents ();
		}

		void ReconstructString ()
		{
			if (string.IsNullOrEmpty (text)) {
				textField.AttributedStringValue = new NSAttributedString ("");
				textField.Cell.PlaceholderAttributedString = GetStatusString (BrandingService.ApplicationName, ColorForType (MessageType.Ready));
				imageView.Image = ImageService.GetIcon (Stock.StatusSteady).ToNSImage ();
			} else {
				textField.AttributedStringValue = GetStatusString (text, textColor);
				imageView.Image = image;
			}

			DestroyPopover (null, null);
		}

		readonly List<StatusIcon> statusIcons = new List<StatusIcon> ();

		internal void RemoveStatusIcon (StatusIcon icon)
		{
			statusIcons.Remove (icon);

			icon.Entered -= ShowPopoverForIcon;
			icon.Exited -= DestroyPopover;
			icon.Clicked -= DestroyPopover;

			RepositionStatusIcons ();
		}

		nfloat LeftMostStatusItemX ()
		{
			if (statusIcons.Count == 0) {
				return Frame.Width;
			}

			return statusIcons.Last ().Frame.X;
		}

		nfloat DrawSeparatorIfNeeded (nfloat right)
		{
			NeedsDisplay = true;

			if (statusIcons.Count == 0) {
				return right;
			}

			return right - 9;
		}

		IconId buildImageId;

		void PositionBuildResults (nfloat right)
		{
			right = DrawSeparatorIfNeeded (right);
			right -= (3 + buildResults.Frame.Width);
			buildResults.SetFrameOrigin (new CGPoint (right, buildResults.Frame.Y));
		}

		internal void RepositionStatusIcons ()
		{
			nfloat right = Frame.Width - 3;

			foreach (var item in statusIcons) {
				right -= item.Bounds.Width + 1;
				item.Frame = new CGRect (right, MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 5 : 4, item.Bounds.Width, item.Bounds.Height);
			}

			PositionBuildResults (right);

			right -= 2;

			if (!buildResults.Hidden) { // We have a build result layer.
				textField.SetFrameSize (new CGSize (buildResults.Frame.X - 3 - textField.Frame.Left, Frame.Height));
			} else
				textField.SetFrameSize (new CGSize (right - 3 - textField.Frame.Left, Frame.Height));
		}

		public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			var statusIcon = new StatusIcon (this) {
				Image = pixbuf,
			};
			statusIcons.Add (statusIcon);

			statusIcon.Entered += ShowPopoverForIcon;
			statusIcon.Exited += DestroyPopover;

			// We need to destroy the popover otherwise the window doesn't come up correctly
			statusIcon.Clicked += DestroyPopover;

			AddSubview (statusIcon);
			RepositionStatusIcons ();

			return statusIcon;
		}

		public StatusBarContext CreateContext ()
		{
			return ctxHandler.CreateContext ();
		}

		public void ShowReady ()
		{
			ShowMessage (null, "", false, MessageType.Ready);
		}

		static Pad sourcePad;
		public void SetMessageSourcePad (Pad pad)
		{
			sourcePad = pad;
		}

		public void ShowError (string error)
		{
			ShowMessage (Stock.StatusError, error, false, MessageType.Error);

		}

		public void ShowWarning (string warning)
		{
			ShowMessage (Stock.StatusWarning, warning, false, MessageType.Warning);
		}

		public void ShowMessage (string message)
		{
			ShowMessage (null, message, false, MessageType.Information);
		}

		public void ShowMessage (string message, bool isMarkup)
		{
			ShowMessage (null, message, true, MessageType.Information);
		}

		public void ShowMessage (IconId image, string message)
		{
			ShowMessage (image, message, false, MessageType.Information);
		}

		public void ShowMessage (IconId image, string message, bool isMarkup)
		{
			ShowMessage (image, message, isMarkup, MessageType.Information);
		}

		public void ShowMessage (IconId image, string message, bool isMarkup, MessageType statusType)
		{
			Runtime.AssertMainThread ();

			bool changed = LoadText (message, isMarkup, statusType);
			LoadPixbuf (image);
			if (changed)
				ReconstructString ();
		}

		bool LoadText (string message, bool isMarkup, MessageType statusType)
		{
			message = message ?? "";
			message = message.Replace (Environment.NewLine, " ").Replace ("\n", " ").Trim ();

			if (message == text)
				return false;

			text = message;
			currentTextIsMarkup = isMarkup;
			messageType = statusType;
			textColor = ColorForType (statusType);

			return true;
		}

		NSColor ColorForType (MessageType messageType)
		{
			switch (messageType) {
				case MessageType.Error:
					return Styles.StatusErrorTextColor.ToNSColor ();
				case MessageType.Warning:
					return Styles.StatusWarningTextColor.ToNSColor ();
				case MessageType.Ready:
					return Styles.StatusReadyTextColor.ToNSColor ();
				default:
					return Styles.BaseForegroundColor.ToNSColor ();
			}
		}

		static bool iconLoaded;
		void LoadPixbuf (IconId iconId)
		{
			// We dont need to load the same image twice
			if (icon == iconId && iconLoaded)
				return;

			icon = iconId;
			iconAnimation = null;

			// clean up previous running animation
			if (xwtAnimation != null) {
				xwtAnimation.Dispose ();
				xwtAnimation = null;
			}

			// if we have nothing, use the default icon
			if (iconId == IconId.Null)
				iconId = Stock.StatusSteady;

			// load image now
			if (ImageService.IsAnimation (iconId, Gtk.IconSize.Menu)) {
				iconAnimation = ImageService.GetAnimatedIcon (iconId, Gtk.IconSize.Menu);
				image = iconAnimation.FirstFrame.ToNSImage ();
				xwtAnimation = iconAnimation.StartAnimation (p => {
					image = p.ToNSImage ();
					ReconstructString ();
				});
			} else {
				image = ImageService.GetIcon (iconId).ToNSImage ();
			}

			iconLoaded = true;
		}

		public void BeginProgress (string name)
		{
			EndProgress ();
			ShowMessage (name);
			oldFraction = 0;

			if (AutoPulse)
				StartProgressAutoPulse ();
		}

		public void BeginProgress (IconId image, string name)
		{
			EndProgress ();
			ShowMessage (image, name);
			oldFraction = 0;

			if (AutoPulse)
				StartProgressAutoPulse ();
		}

		bool inProgress;
		double oldFraction;
		public void SetProgressFraction (double work)
		{
			if (AutoPulse)
				return;

			progressMarks.Push (work);
			if (!inProgress) {
				inProgress = true;
				StartProgress (progressMarks.Peek ());
			}
		}

		public void EndProgress ()
		{
			progressMarks.Clear ();
			if (progressLayer != null) {
				progressLayer.RemoveAnimation (growthAnimationKey);
				if (inProgress == false) {
					progressLayer.RemoveFromSuperLayer ();
					progressLayer = null;
				}
			}
			inProgress = false;
			AutoPulse = false;
		}

		public void Pulse ()
		{
			// Nothing to do here.
		}

		public MonoDevelop.Ide.StatusBar MainContext {
			get {
				return ctxHandler.MainContext;
			}
		}

		public bool AutoPulse {
			get;
			set;
		}

		static CGColor xamBlue = new CGColor (52f / 255, 152f / 255, 219f / 255);
		static nfloat verticalOffset = 2;
		CALayer CreateProgressBarLayer (double width)
		{
			CALayer progress = progressLayer;
			if (progress == null) {
				progress = CALayer.Create ();
				progress.BackgroundColor = xamBlue;
				progress.BorderColor = xamBlue;
				progress.FillMode = CAFillMode.Forwards;
				progress.Frame = new CGRect (0, Frame.Height - barHeight - verticalOffset, (nfloat)width, barHeight);

				progressLayer = progress;
				Layer.AddSublayer (progress);
			}
			return progress;
		}

		CAAnimation CreateMoveAndGrowAnimation (CALayer progress, double growToFraction)
		{
			CAAnimationGroup grp = CAAnimationGroup.CreateAnimation ();
			grp.Duration = 0.2;
			grp.FillMode = CAFillMode.Forwards;
			grp.RemovedOnCompletion = false;

			CABasicAnimation move = CABasicAnimation.FromKeyPath ("position.x");
			double oldOffset = (progress.Frame.Width / 2) * oldFraction;
			double newOffset = (progress.Frame.Width / 2) * growToFraction;
			move.From = NSNumber.FromDouble (oldOffset);
			move.To = NSNumber.FromDouble (newOffset);

			CABasicAnimation grow = CABasicAnimation.FromKeyPath ("bounds");
			grow.From = NSValue.FromCGRect (new CGRect (0, 0, progress.Frame.Width * (nfloat)oldFraction, barHeight));
			grow.To = NSValue.FromCGRect (new CGRect (0, 0, progress.Frame.Width * (nfloat)growToFraction, barHeight));
			grp.Animations = new [] {
				move,
				grow,
			};
			return grp;
		}

		CAAnimation CreateAutoPulseAnimation ()
		{
			CABasicAnimation move = CABasicAnimation.FromKeyPath ("position.x");
			move.From = NSNumber.FromDouble (-frameAutoPulseWidth);
			move.To = NSNumber.FromDouble (Layer.Frame.Width + frameAutoPulseWidth);
			move.RepeatCount = float.PositiveInfinity;
			move.RemovedOnCompletion = false;
			move.Duration = 4;
			return move;
		}

		void AttachFadeoutAnimation (CALayer progress, CAAnimation animation, Func<bool> fadeoutVerifier)
		{
			animation.AnimationStopped += (sender, e) => {
				if (!fadeoutVerifier ())
					return;

				CABasicAnimation fadeout = CABasicAnimation.FromKeyPath ("opacity");
				fadeout.From = NSNumber.FromDouble (1);
				fadeout.To = NSNumber.FromDouble (0);
				fadeout.Duration = 0.5;
				fadeout.FillMode = CAFillMode.Forwards;
				fadeout.RemovedOnCompletion = false;
				fadeout.AnimationStopped += (sender2, e2) => {
					if (!e2.Finished)
						return;

					inProgress = false;
					progress.Opacity = 0;
					progress.RemoveAllAnimations ();
					progress.RemoveFromSuperLayer ();
				};
				progress.Name = ProgressLayerFadingId;
				progress.AddAnimation (fadeout, "opacity");
			};
			progress.AddAnimation (animation, growthAnimationKey);
		}

		const int barHeight = 2;
		void StartProgress (double newFraction)
		{
			progressMarks.Clear ();
			var progress = CreateProgressBarLayer (Layer.Frame.Width);
			var grp = CreateMoveAndGrowAnimation (progress, newFraction);
			oldFraction = newFraction;

			AttachFadeoutAnimation (progress, grp, () => {
				if (oldFraction < 1 && inProgress) {
					if (progressMarks.Count != 0) {
						StartProgress (progressMarks.Peek ());
					} else {
						inProgress = false;
					}
					return false;
				}
				return true;
			});
		}

		const double frameAutoPulseWidth = 100;
		void StartProgressAutoPulse ()
		{
			var progress = CreateProgressBarLayer (frameAutoPulseWidth);
			var move = CreateAutoPulseAnimation ();
			AttachFadeoutAnimation (progress, move, () => true);
		}

		static NSAttributedString GetPopoverString (string text)
		{
			return new NSAttributedString (text, new NSStringAttributes {
				ParagraphStyle = new NSMutableParagraphStyle {
					Alignment = NSTextAlignment.Center,
				},
				Font = NSFont.SystemFontOfSize (NSFont.SystemFontSize - 1),
			});
		}

		NSPopover popover;

		void CreatePopoverCommon (nfloat width, string text)
		{
			popover = new NSPopover {
				ContentViewController = new NSViewController (null, null),
				Animates = false
			};

			var attrString = GetPopoverString (text);

			var height = attrString.BoundingRectWithSize (new CGSize (width, nfloat.MaxValue),
				NSStringDrawingOptions.UsesFontLeading | NSStringDrawingOptions.UsesLineFragmentOrigin).Height;
			
			popover.ContentViewController.View = new NSTextField {
				Frame = new CGRect (0, 0, width, height + 14),
				DrawsBackground = false,
				Bezeled = true,
				Editable = false,
				Cell = new VerticallyCenteredTextFieldCell (yOffset: -1),
			};
			((NSTextField)popover.ContentViewController.View).AttributedStringValue = attrString;
		}

		bool CreatePopoverForIcon (StatusBarIcon icon)
		{
			string tooltip = icon.ToolTip;
			if (tooltip == null)
				return false;

			CreatePopoverCommon (230, tooltip);
			return true;
		}

		void CreatePopoverForStatusBar ()
		{
			CreatePopoverCommon (Frame.Width, textField.AttributedStringValue.Value);
		}

		void ShowPopoverForIcon (object sender, EventArgs args)
		{
			if (popover != null)
				return;

			var icon = (StatusIcon) sender;

			if (!CreatePopoverForIcon (icon))
				return;

			popover.Show (icon.Frame, this, NSRectEdge.MinYEdge);
		}

		void ShowPopoverForStatusBar ()
		{
			if (popover != null)
				return;

			CreatePopoverForStatusBar ();
			popover.Show (textField.Frame, this, NSRectEdge.MinYEdge);
		}

		void DestroyPopover (object sender, EventArgs args)
		{
			if (popover != null)
				popover.Close ();
			popover = null;
		}
			
		public override void MouseEntered (NSEvent theEvent)
		{
			base.MouseEntered (theEvent);

			var width = textField.AttributedStringValue.BoundingRectWithSize (new CGSize (nfloat.MaxValue, textField.Frame.Height),
				NSStringDrawingOptions.UsesFontLeading | NSStringDrawingOptions.UsesLineFragmentOrigin).Width;
			if (width > textField.Frame.Width) {
				ShowPopoverForStatusBar ();
			}
		}

		public override void MouseExited (NSEvent theEvent)
		{
			base.MouseExited (theEvent);
			DestroyPopover (null, null);
		}

		internal static Xwt.PointerButton NSEventButtonToXwt (NSEvent theEvent)
		{
			Xwt.PointerButton button = Xwt.PointerButton.Left;
			switch ((NSEventType)(long)theEvent.ButtonNumber) {
			case NSEventType.LeftMouseDown:
				button = Xwt.PointerButton.Left;
				break;
			case NSEventType.RightMouseDown:
				button = Xwt.PointerButton.Right;
				break;
			case NSEventType.OtherMouseDown:
				button = Xwt.PointerButton.Middle;
				break;
			}

			return button;
		}

		public override void MouseDown (NSEvent theEvent)
		{
			base.MouseDown (theEvent);

			CGPoint location = ConvertPointFromView (theEvent.LocationInWindow, null);
			if (textField.IsMouseInRect (location, textField.Frame) && sourcePad != null) {
				sourcePad.BringToFront (true);
			}
		}

		public override CGRect Frame {
			get {
				return base.Frame;
			}
			set {
				base.Frame = value;
				RepositionContents ();
			}
		}

		void RepositionContents ()
		{
			nfloat yOffset = 0;
			if (Window != null && Window.Screen.BackingScaleFactor == 1) {
				yOffset = -1;
			}

			imageView.Frame = new CGRect (6, 0, 16, Frame.Height);
			textField.Frame = new CGRect (imageView.Frame.Right, yOffset, Frame.Width - 16, Frame.Height);

			buildResults.Frame = new CGRect (buildResults.Frame.X, buildResults.Frame.Y, buildResults.Frame.Width, Frame.Height);
			RepositionStatusIcons ();
		}
	}
}
