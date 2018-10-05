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
using System.Timers;
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
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MonoDevelop.MacInterop;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	class StatusIcon : NSButton, StatusBarIcon
	{
		StatusBar bar;
		readonly ObjCRuntime.Selector OnButtonClickedSelector = new ObjCRuntime.Selector ("OnButtonActivated:");

		public StatusIcon (StatusBar bar) : base (CGRect.Empty)
		{
			Bordered = false;
			var trackingArea = new NSTrackingArea (CGRect.Empty, NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.InVisibleRect | NSTrackingAreaOptions.MouseEnteredAndExited, this, null);
			AddTrackingArea (trackingArea);

			Target = this;
			Action = OnButtonClickedSelector;

			this.bar = bar;
		}

		public override CGRect Frame {
			get {
				return base.Frame;
			}
			set {
				base.Frame = value;
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

		string tooltip;
		public new string ToolTip {
			get {
				return tooltip;
			}

			set {
				tooltip = value;
				AccessibilityTitle = tooltip;
				AccessibilityValue = new NSString (tooltip);
			}
		}

		string help;
		public string Help {
			get {
				return help;
			}

			set {
				help = value;
				AccessibilityHelp = value;
			}
		}

		public new string Title { get; set; }
		public override string AccessibilityLabel {
			get {
				return Title;
			}
		}

		Xwt.Drawing.Image image;
		public new Xwt.Drawing.Image Image {
			get { return image; }
			set {
				image = value;
				base.Image = value.ToNSImage ();
				SetFrameSize (new CGSize (image.Width, image.Height));
			}
		}

		public override void MouseEntered (NSEvent theEvent)
		{
			Entered?.Invoke (this, EventArgs.Empty);
		}

		public override void MouseExited (NSEvent theEvent)
		{
			Exited?.Invoke (this, EventArgs.Empty);
		}

		[Export ("OnButtonActivated:")]
		void ButtonClicked (NSObject sender)
		{
			NotifyClicked (Xwt.PointerButton.Left);
		}

		internal void NotifyClicked (Xwt.PointerButton button)
		{
			Clicked?.Invoke (this, new StatusBarIconClickedEventArgs {
				Button = button,
			});
		}

		public override bool AccessibilityPerformPress ()
		{
			NotifyClicked (Xwt.PointerButton.Left);

			return true;
		}

		public event EventHandler<StatusBarIconClickedEventArgs> Clicked;
		public event EventHandler<EventArgs> Entered;
		public event EventHandler<EventArgs> Exited;

		public override bool AcceptsFirstResponder ()
		{
			return true;
		}

		public override bool BecomeFirstResponder ()
		{
			Entered?.Invoke (this, EventArgs.Empty);
			return true;
		}

		public override bool ResignFirstResponder ()
		{
			Exited?.Invoke (this, EventArgs.Empty);
			return true;
		}
	}

	class BuildResultsView : NSView, INSAccessibilityStaticText
	{
		public enum ResultsType
		{
			Warning,
			Error
		};

		public ResultsType Type { get; set; }

		NSAttributedString resultString;
		int resultCount;
		public int ResultCount {
			get {
				return resultCount;
			}
			set {
				resultCount = value;
				resultString = new NSAttributedString (value.ToString (), foregroundColor: Styles.BaseForegroundColor.ToNSColor (),
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
			AccessibilityIdentifier = "MainToolbar.StatusDisplay.BuildResults";
			AccessibilityHelp = "Number of errors or warnings in the current project";
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			if (iconImage == null || resultString == null) {
				return;
			}

			iconImage.Draw (new CGRect (0, (Frame.Size.Height - iconImage.Size.Height) / 2, iconImage.Size.Width, iconImage.Size.Height));
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

		string INSAccessibilityStaticText.AccessibilityValue {
			get {
				if (Type == ResultsType.Warning) {
					return GettextCatalog.GetPluralString ("{0} warning", "%d warnings", resultCount, resultCount);
				} else {
					return GettextCatalog.GetPluralString ("{0} error", "%d errors", resultCount, resultCount);
				}
			}
		}
	}

	// We need a separate layer backed view to put over the NSTextFields because the NSTextField draws itself differently
	// if it is layer backed so we can't make it or its superview layer backed.
	class ProgressView : NSView, INSAccessibilityProgressIndicator
	{
		const string ProgressLayerFadingId = "ProgressLayerFading";
		const string growthAnimationKey = "bounds";

		CALayer progressLayer;
		Stack<double> progressMarks = new Stack<double> ();
		bool inProgress;
		double oldFraction;

		const int barHeight = 2;
		const int barY = 0;

		public ProgressView ()
		{
			int barWidth = 0;

#if DEBUG_PROGRESSBAR
			barWidth = 100;
#endif
			WantsLayer = true;
			Layer.CornerRadius = MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 3 : 4;

			progressLayer = new CALayer ();
			Layer.AddSublayer (progressLayer);
			Layer.BorderWidth = 0;

			var xamBlue = NSColor.FromRgba (52f / 255, 152f / 255, 219f / 255, 1f);
			progressLayer.BackgroundColor = xamBlue.CGColor;
			progressLayer.BorderWidth = 0;
			progressLayer.FillMode = CAFillMode.Forwards;
			progressLayer.Frame = new CGRect (0, barY, barWidth, barHeight);
			progressLayer.AnchorPoint = new CGPoint (0, 0);

			AccessibilityIdentifier = "MainToolbar.StatusDisplay.Progress";
			AccessibilityHelp = "The progress of the current action";
			AccessibilityHidden = true;
		}

		NSNumber INSAccessibilityProgressIndicator.AccessibilityValue {
			get {
				// Convert the number to a percentage
				var percent = (int)(oldFraction * 100.0f);
				return new NSNumber (percent);
			}
		}

		void SetProgressValue (double p)
		{
			oldFraction = p;
			var percent = (int)(oldFraction * 100.0f);
			((INSAccessibility)this).AccessibilityValue = new NSNumber (percent);
		}

		public void BeginProgress ()
		{
			SetProgressValue (0.0);
			progressLayer.RemoveAllAnimations ();

			progressLayer.Hidden = false;
			progressLayer.Opacity = 1;
			progressLayer.Frame = new CGRect (0, barY, 0, barHeight);

			AccessibilityHidden = false;
		}

		public void SetProgressFraction (double work)
		{
			if (oldFraction == work)
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
				progressLayer.Hidden = true;
				AccessibilityHidden = true;

				// We don't set the progress to 0 here, because EndProgress can be called many times
				// for a single task, and if we do then the progress bar pulses from 0 every time
			}
			inProgress = false;
		}

		CAAnimation CreateMoveAndGrowAnimation (CALayer progress, double growToFraction)
		{
			CABasicAnimation grow = CABasicAnimation.FromKeyPath ("bounds");
			grow.Duration = 0.2;
			grow.FillMode = CAFillMode.Forwards;
			grow.RemovedOnCompletion = false;
			grow.From = NSValue.FromCGRect (new CGRect (0, barY, Frame.Width * (nfloat)oldFraction, barHeight));
			grow.To = NSValue.FromCGRect (new CGRect (0, barY, Frame.Width * (nfloat)growToFraction, barHeight));
			return grow;
		}

		CAAnimation CreateAutoPulseAnimation ()
		{
			CABasicAnimation move = CABasicAnimation.FromKeyPath ("position.x");
			move.From = NSNumber.FromDouble (-frameAutoPulseWidth);
			move.To = NSNumber.FromDouble (Frame.Width + frameAutoPulseWidth);
			move.RepeatCount = float.PositiveInfinity;
			move.RemovedOnCompletion = false;
			move.Duration = 4;
			return move;
		}

		void AttachFadeoutAnimation (CALayer progress, CAAnimation animation, Func<bool> fadeoutVerifier)
		{
			animation.AnimationStopped += (sender, e) => {
				if (!fadeoutVerifier ()) {
					return;
				}

				if (!e.Finished) {
					return;
				}

				CABasicAnimation fadeout = CABasicAnimation.FromKeyPath ("opacity");
				fadeout.From = NSNumber.FromDouble (1);
				fadeout.To = NSNumber.FromDouble (0);
				fadeout.Duration = 0.5;
				fadeout.FillMode = CAFillMode.Forwards;
				fadeout.RemovedOnCompletion = false;
				fadeout.AnimationStopped += (sender2, e2) => {
					if (!e2.Finished)
						return;

					// Reset all the properties.
					inProgress = false;

					progress.Hidden = true;

					progress.Opacity = 1;
					progress.Frame = new CGRect (0, barY, 0, barHeight);
					progress.RemoveAllAnimations ();
					SetProgressValue (0.0);

					progress.Hidden = false;
				};
				progress.Name = ProgressLayerFadingId;
				progress.AddAnimation (fadeout, "opacity");
			};
			progress.AddAnimation (animation, growthAnimationKey);
		}

		public void StartProgress (double newFraction)
		{
			progressMarks.Clear ();
			var grp = CreateMoveAndGrowAnimation (progressLayer, newFraction);

			SetProgressValue (newFraction);

			AttachFadeoutAnimation (progressLayer, grp, () => {
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

		public void StartProgressAutoPulse ()
		{
			var move = CreateAutoPulseAnimation ();
			AttachFadeoutAnimation (progressLayer, move, () => true);
		}
	}

	class CancelButton : NSButton, INSAccessibilityButton
	{
		readonly NSImage stopIcon = MultiResImage.CreateMultiResImage ("status-stop-16", string.Empty);

		public CancelButton ()
		{
			Image = stopIcon;
			Hidden = true;
			Bordered = false;
			ImagePosition = NSCellImagePosition.ImageOnly;
			SetButtonType (NSButtonType.MomentaryChange);
			AddTrackingArea (new NSTrackingArea (CGRect.Empty, NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.ActiveAlways | NSTrackingAreaOptions.InVisibleRect, this, null));

			AccessibilityHelp = GettextCatalog.GetString ("Cancel the current operation");
			AccessibilityTitle = GettextCatalog.GetString ("Cancel");

			var nsa = (INSAccessibility) this;
			nsa.AccessibilityIdentifier = "MainToolbar.StatusDisplay.Cancel";
		}

		string INSAccessibilityButton.AccessibilityLabel {
			get {
				return GettextCatalog.GetString ("Cancel");
			}
		}

		public override bool AcceptsFirstResponder ()
		{
			return true;
		}
	}

	[Register]
	class StatusBar : NSFocusButton, MonoDevelop.Ide.StatusBar
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
		string text;
		MessageType messageType;
		NSColor textColor;
		NSImage image;
		IconId icon;
		AnimatedIcon iconAnimation;
		IDisposable xwtAnimation;
		readonly BuildResultsView buildResults;
		readonly CancelButton cancelButton;

		SearchBar searchBar;
		internal SearchBar SearchBar {
			get => searchBar;
			set {
				searchBar = value;
				cancelButton.NextKeyView = value;
			}
		}

		NSAttributedString GetStatusString (string text, NSColor color)
		{
			nfloat fontSize = NSFont.SystemFontSize;
			if (Window != null && Window.Screen != null) {
				fontSize -= Window.Screen.BackingScaleFactor == 2 ? 2 : 1;
			} else {
				fontSize -= 1;
			}

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
		ProgressView progressView;

		TaskEventHandler updateHandler;
		public StatusBar ()
		{
			var nsa = (INSAccessibility)this;

			// Pretend that this button is a Group
			AccessibilityRole = NSAccessibilityRoles.GroupRole;
			nsa.AccessibilityIdentifier = "MainToolbar.StatusDisplay";

			Cell = new ColoredButtonCell ();
			BezelStyle = NSBezelStyle.TexturedRounded;
			Title = "";
			Enabled = false;

			LoadStyles ();

			// We don't need to resize the Statusbar here as a style change will trigger a complete relayout of the Awesomebar
			Ide.Gui.Styles.Changed += LoadStyles;

			textField.Cell = new VerticallyCenteredTextFieldCell (0f);
			textField.Cell.StringValue = "";

			textField.AccessibilityRole = NSAccessibilityRoles.StaticTextRole;
			textField.AccessibilityEnabled = true;
			textField.AccessibilityHelp = GettextCatalog.GetString ("Status of the current operation");

			var tfNSA = (INSAccessibility)textField;
			tfNSA.AccessibilityIdentifier = "MainToolbar.StatusDisplay.Status";

			UpdateApplicationNamePlaceholderText ();

			// The rect is empty because we use InVisibleRect to track the whole of the view.
			textFieldArea = new NSTrackingArea (CGRect.Empty, NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.InVisibleRect, this, null);
			textField.AddTrackingArea (textFieldArea);

			imageView.Frame = new CGRect (0.5, 0, 0, 0);
			imageView.Image = ImageService.GetIcon (Stock.StatusSteady).ToNSImage ();

			// Hide this image from accessibility
			imageView.AccessibilityElement = false;

			buildResults = new BuildResultsView ();
			buildResults.Hidden = true;

			cancelButton = new CancelButton ();
			cancelButton.Activated += (o, e) => {
				cts?.Cancel ();
			};

			NextKeyView = cancelButton;

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
					buildResults.Type = ec > 0 ? BuildResultsView.ResultsType.Error : BuildResultsView.ResultsType.Warning;

					buildImageId = ec > 0 ? "md-status-error-count" : "md-status-warning-count";
					buildResults.IconImage = ImageService.GetIcon (buildImageId, Gtk.IconSize.Menu).ToNSImage ();

					RepositionStatusIcons ();
				});
			};

			updateHandler (null, null);

			TaskService.Errors.TasksAdded += updateHandler;
			TaskService.Errors.TasksRemoved += updateHandler;
			BrandingService.ApplicationNameChanged += ApplicationNameChanged;

			AddSubview (cancelButton);
			AddSubview (buildResults);
			AddSubview (imageView);
			AddSubview (textField);

			progressView = new ProgressView ();
			AddSubview (progressView);

			var newChildren = new NSObject [] {
				textField, buildResults, progressView
			};
			AccessibilityChildren = newChildren;
		}

		void UpdateApplicationNamePlaceholderText ()
		{
			textField.Cell.PlaceholderAttributedString = GetStatusString (BrandingService.ApplicationLongName, ColorForType (MessageType.Ready));

			var nsa = (INSAccessibility)textField;
			nsa.AccessibilityValue = new NSString (BrandingService.ApplicationLongName);
		}

		void ApplicationNameChanged (object sender, EventArgs e)
		{
			UpdateApplicationNamePlaceholderText ();
		}

		public override void DidChangeBackingProperties ()
		{
			base.DidChangeBackingProperties ();
			ReconstructString ();
			RepositionContents ();
		}

		void LoadStyles (object sender = null, EventArgs args = null)
		{
			Appearance = IdeTheme.GetAppearance ();

			UpdateApplicationNamePlaceholderText ();
			textColor = ColorForType (messageType);
			ReconstructString ();
		}

		protected override void Dispose (bool disposing)
		{
			TaskService.Errors.TasksAdded -= updateHandler;
			TaskService.Errors.TasksRemoved -= updateHandler;
			Ide.Gui.Styles.Changed -= LoadStyles;
			BrandingService.ApplicationNameChanged -= ApplicationNameChanged;
			base.Dispose (disposing);
		}

		static void DrawSeparator (nfloat x, CGRect dirtyRect)
		{
			var sepRect = new CGRect (x - 6.5, MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 4 : 3, 1, 16);
			if (sepRect.IntersectsWith (dirtyRect)) {
				NSColor.LightGray.SetFill ();
				NSBezierPath.FillRect (sepRect);
			}
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			if (statusIcons.Count != 0 && !buildResults.Hidden)
				DrawSeparator (LeftMostStatusItemX (), dirtyRect);

			if (statusIcons.Count != 0 || !buildResults.Hidden) {
				if (cancelButton.Hidden)
					return;

				DrawSeparator (LeftMostBuildResultX (), dirtyRect);
			}
		}

		public override void ViewDidMoveToWindow ()
		{
			base.ViewDidMoveToWindow ();
			ReconstructString ();
			RepositionContents ();
		}

		NSImage statusReadyImage = ImageService.GetIcon (Stock.StatusSteady).ToNSImage ();
		void ReconstructString ()
		{
			var updatePopover = popoverForStatus && popover != null;
			if (string.IsNullOrEmpty (text)) {
				textField.AttributedStringValue = new NSAttributedString ("");
				UpdateApplicationNamePlaceholderText ();
				imageView.Image = statusReadyImage;
				if (updatePopover) {
					DestroyPopover (null, null);
				}
			} else {
				textField.AttributedStringValue = GetStatusString (text, textColor);
				imageView.Image = image;
				if (updatePopover) {
					DestroyPopover (null, null);
					ShowPopoverForStatusBar ();
				}
			}
		}

		readonly List<StatusIcon> statusIcons = new List<StatusIcon> ();

		// Xamarin.Mac has a bug where NSView.NextKeyView cannot be set to null
		// Work around it here by rebinding it ourselves
		// https://github.com/xamarin/xamarin-macios/issues/4558
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		static extern void void_objc_msgSend_IntPtr (IntPtr receiver, IntPtr selector, IntPtr nextKeyViewHandle);
		static readonly IntPtr setNextKeyViewSelector = ObjCRuntime.Selector.GetHandle ("setNextKeyView:");
		static void NullableSetNextKeyView (NSView parent, NSView nextKeyView)
		{
			if (parent == null) {
				throw new ArgumentNullException (nameof (parent));
			}

			void_objc_msgSend_IntPtr (parent.Handle, setNextKeyViewSelector, nextKeyView != null ? nextKeyView.Handle : IntPtr.Zero);
		}

		// Used by AutoTest.
		internal string[] StatusIcons => statusIcons.Select(x => x.ToolTip).ToArray ();

		internal void RemoveStatusIcon (StatusIcon icon)
		{
			// For keyboard focus the icons are the reverse of the order they're stored in the list
			// because they're displayed in order right to left
			var index = statusIcons.IndexOf (icon);
			if (index != -1) {
				if (index == statusIcons.Count - 1) {
					cancelButton.NextKeyView = statusIcons.Count > 1 ? (NSView)statusIcons[index - 1] : (NSView)SearchBar;
				} else {
					var previous = statusIcons[index + 1];
					var next = index > 0 ? (NSView)statusIcons[index - 1] : (NSView)cancelButton;

					previous.NextKeyView = next;
				}
			}

			statusIcons.Remove (icon);

			icon.Entered -= ShowPopoverForIcon;
			icon.Exited -= DestroyPopover;
			icon.Clicked -= DestroyPopover;

			if (!popoverForStatus && popover != null)
				DestroyPopover (null, null);

			RepositionStatusIcons ();
		}

		nfloat LeftMostStatusItemX ()
		{
			if (statusIcons.Count == 0) {
				return Frame.Width;
			}

			return statusIcons.Last ().Frame.X;
		}

		nfloat LeftMostBuildResultX ()
		{
			if (buildResults.Hidden)
				return LeftMostStatusItemX ();

			return buildResults.Frame.X;
		}

		nfloat DrawSeparatorIfNeededBuildResults (nfloat right)
		{
			NeedsDisplay = true;

			if (statusIcons.Count == 0) {
				return right;
			}

			return right - 12;
		}

		nfloat DrawSeparatorIfNeededCancelButton (nfloat right)
		{
			NeedsDisplay = true;

			if (!buildResults.Hidden)
				return buildResults.Frame.X - 12;

			if (statusIcons.Count == 0)
				return right;

			return right - 12;
		}

		IconId buildImageId;

		void PositionBuildResults (nfloat right)
		{
			right = DrawSeparatorIfNeededBuildResults (right);
			right -= buildResults.Frame.Width;
			buildResults.SetFrameOrigin (new CGPoint (right, buildResults.Frame.Y));
		}

		void PositionCancelButton (nfloat right)
		{
			right = DrawSeparatorIfNeededCancelButton (right);
			right -= cancelButton.Frame.Width;
			cancelButton.SetFrameOrigin (new CGPoint (right, cancelButton.Frame.Y));
		}

		internal void RepositionStatusIcons ()
		{
			nfloat right = Frame.Width - 6;

			foreach (var item in statusIcons) {
				right -= item.Bounds.Width + 1;
				item.Frame = new CGRect (right, MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 4 : 3, item.Bounds.Width, item.Bounds.Height);
			}

			PositionBuildResults (right);
			PositionCancelButton (right);

			right -= 2;

			if (!cancelButton.Hidden) { // We have a cancel button.
				right = cancelButton.Frame.X;
			} else if (!buildResults.Hidden) { // We have a build result layer.
				right = buildResults.Frame.X;
			}

			textField.SetFrameSize (new CGSize (right - 3 - textField.Frame.Left, Frame.Height));
		}

		public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			var statusIcon = new StatusIcon (this) {
				Image = pixbuf,
			};
			statusIcons.Add (statusIcon);

			if (statusIcons.Count == 1) {
				statusIcon.NextKeyView = SearchBar;
			} else {
				var previousIcon = statusIcons[statusIcons.Count - 2];
				statusIcon.NextKeyView = previousIcon;
			}
			cancelButton.NextKeyView = statusIcon;

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
			SetMessageSourcePad (null);
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
			messageType = statusType;
			textColor = ColorForType (statusType);

			var nsa = (INSAccessibility)textField;
			nsa.AccessibilityValue = new NSString (message);

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
			BeginProgress (null, name);
		}

		public void BeginProgress (IconId image, string name)
		{
			EndProgress ();
			ShowMessage (image, name);

			if (AutoPulse)
				progressView.StartProgressAutoPulse ();
			else
				progressView.BeginProgress ();
		}


		public void SetProgressFraction (double work)
		{
			progressView.SetProgressFraction (work);
		}

		public void EndProgress ()
		{
			progressView.EndProgress ();
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
		bool popoverForStatus;

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

			popoverForStatus = false;
			var icon = (StatusIcon) sender;

			if (!CreatePopoverForIcon (icon))
				return;

			popover.Show (icon.Frame, this, NSRectEdge.MinYEdge);
		}

		void ShowPopoverForStatusBar ()
		{
			if (popover != null)
				return;

			popoverForStatus = true;
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

		bool isFirstResponder;
		public override void KeyDown (NSEvent theEvent)
		{
			if (isFirstResponder && (theEvent.KeyCode == KeyCodes.Enter || theEvent.KeyCode == KeyCodes.Space)) {
				sourcePad?.BringToFront (true);
				return;
			}
			base.KeyDown (theEvent);
		}

		public override bool AcceptsFirstResponder ()
		{
			return true;
		}

		public override bool BecomeFirstResponder ()
		{
			isFirstResponder = true;
			return true;
		}

		public override bool ResignFirstResponder ()
		{
			isFirstResponder = false;
			return base.ResignFirstResponder ();
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

		CancellationTokenSource cts;
		public void SetCancellationTokenSource (CancellationTokenSource source)
		{
			cts = source;

			bool willHide = cts == null;
			cancelButton.ToolTip = willHide ? string.Empty : GettextCatalog.GetString ("Cancel operation");
			if (cancelButton.Hidden != willHide) {
				cancelButton.Hidden = willHide;
				RepositionStatusIcons ();
			}
		}

		void RepositionContents ()
		{
			nfloat yOffset = 0f;
			if (Window != null && Window.Screen != null && Window.Screen.BackingScaleFactor == 2) {
				yOffset = 0.5f;
			}

			imageView.Frame = new CGRect (6, 0, 16, Frame.Height);
			textField.Frame = new CGRect (imageView.Frame.Right, yOffset, Frame.Width - 16, Frame.Height);

			buildResults.Frame = new CGRect (buildResults.Frame.X, buildResults.Frame.Y, buildResults.Frame.Width, Frame.Height);
			cancelButton.Frame = new CGRect (cancelButton.Frame.X, cancelButton.Frame.Y, 16, Frame.Height);
			RepositionStatusIcons ();

			progressView.Frame = new CGRect (0.5f, Frame.Height - 2f, Frame.Width - 2, 2);
		}
	}
}
