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

namespace MonoDevelop.MacIntegration.MainToolbar
{
	class StatusIcon : StatusBarIcon
	{
		StatusBar bar;
		CALayer layer;

		public StatusIcon (StatusBar bar, CALayer layer, NSTrackingArea trackingArea)
		{
			this.bar = bar;
			this.layer = layer;
			TrackingArea = trackingArea;
		}

		public void SetAlertMode (int seconds)
		{
			// Create fade-out fade-in animation.
		}

		public void Dispose ()
		{
			layer.RemoveFromSuperLayer ();
			bar.RemoveStatusLayer (layer);
		}

		public string ToolTip {
			get;
			set;
		}

		internal NSTrackingArea TrackingArea {
			get;
			set;
		}

		Xwt.Drawing.Image image;
		public Xwt.Drawing.Image Image {
			get { return image; }
			set {
				image = value;
				layer.SetImage (value, bar.Window.BackingScaleFactor);
			}
		}

		internal void NotifyClicked (Xwt.PointerButton button)
		{
			if (Clicked != null)
				Clicked (this, new StatusBarIconClickedEventArgs {
					Button = button,
				});
		}

		public event EventHandler<StatusBarIconClickedEventArgs> Clicked;
	}

	[Register]
	class StatusBar : NSTextField, MonoDevelop.Ide.StatusBar
	{
		const string ProgressLayerId = "ProgressLayer";
		const string ProgressLayerFadingId = "ProgressLayerFading";
		const string StatusIconPrefixId = "StatusLayer";
		const string BuildIconLayerId = "BuildIconLayer";
		const string BuildTextLayerId = "BuildTextLayer";
		const string SeparatorLayerId = "SeparatorLayer";
		const string growthAnimationKey = "bounds";
		StatusBarContextHandler ctxHandler;
		Stack<double> progressMarks = new Stack<double> ();
		bool currentTextIsMarkup;
		string text;
		NSColor textColor;
		NSImage image;
		IconId icon;
		AnimatedIcon iconAnimation;
		IDisposable xwtAnimation;

		NSAttributedString GetStatusString (string text, NSColor color)
		{
			return new NSAttributedString (text, new NSStringAttributes {
				ForegroundColor = color,
				ParagraphStyle = new NSMutableParagraphStyle {
					HeadIndent = imageView.Frame.Width,
					LineBreakMode = NSLineBreakMode.TruncatingMiddle,
				},
				Font = NSFont.SystemFontOfSize (NSFont.SystemFontSize - 2),
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

		TaskEventHandler updateHandler;
		public StatusBar ()
		{
			AllowsEditingTextAttributes = Selectable = Editable = false;

			textField.Cell = new VerticallyCenteredTextFieldCell (yOffset: -0.5f);
			textField.Cell.StringValue = "";
			textField.Cell.PlaceholderAttributedString = GetStatusString (BrandingService.ApplicationName, NSColor.DisabledControlText);
			imageView.Image = ImageService.GetIcon (Stock.StatusSteady).ToNSImage ();

			// Fixes a render glitch of a whiter bg than the others.
			if (MacSystemInformation.OsVersion >= MacSystemInformation.Yosemite)
				BezelStyle = NSTextFieldBezelStyle.Rounded;

			WantsLayer = true;
			Layer.CornerRadius = MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 6 : 4;
			ctxHandler = new StatusBarContextHandler (this);

			updateHandler = delegate {
				int ec=0, wc=0;

				foreach (Task t in TaskService.Errors) {
					if (t.Severity == TaskSeverity.Error)
						ec++;
					else if (t.Severity == TaskSeverity.Warning)
						wc++;
				}

				DispatchService.GuiDispatch (delegate {
					if (ec > 0) {
						buildResultVisible = true;
						buildResultText.AttributedString = new NSAttributedString (ec.ToString (), foregroundColor: NSColor.Text,
							font: NSFont.SystemFontOfSize (NSFont.SmallSystemFontSize - 1));
						buildResultText.ContentsScale = Window.BackingScaleFactor;
						buildResultIcon.SetImage (buildImageId = "md-status-error-count", Window.BackingScaleFactor);
					} else if (wc > 0) {
						buildResultVisible = true;
						buildResultText.AttributedString = new NSAttributedString (wc.ToString (), foregroundColor: NSColor.Text,
							font: NSFont.SystemFontOfSize (NSFont.SmallSystemFontSize - 1));
						buildResultText.ContentsScale = Window.BackingScaleFactor;
						buildResultIcon.SetImage (buildImageId = "md-status-warning-count", Window.BackingScaleFactor);
					} else
						buildResultVisible = false;

					CATransaction.DisableActions = true;
					nfloat buildResultPosition = DrawBuildResults ();
					CATransaction.DisableActions = false;
					if (buildResultPosition == nfloat.PositiveInfinity)
						return;
					textField.SetFrameSize (new CGSize (buildResultPosition - 6 - textField.Frame.Left, Frame.Height));
				});
			};

			updateHandler (null, null);

			TaskService.Errors.TasksAdded += updateHandler;
			TaskService.Errors.TasksRemoved += updateHandler;

			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidChangeBackingPropertiesNotification, notif => DispatchService.GuiDispatch (() => {
				if (Window == null)
					return;

				ReconstructString (updateTrackingAreas: true);
				foreach (var layer in Layer.Sublayers) {
					if (layer.Name != null && layer.Name.StartsWith (StatusIconPrefixId, StringComparison.Ordinal))
						layer.SetImage (layerToStatus [layer.Name].Image, Window.BackingScaleFactor);
				}
				if (buildResultVisible) {
					buildResultIcon.SetImage (buildImageId, Window.BackingScaleFactor);
					buildResultText.ContentsScale = Window.BackingScaleFactor;
				}
			}));

			AddSubview (imageView);
			AddSubview (textField);
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			if (imageView.Frame.Location == CGPoint.Empty)
				imageView.Frame = new CGRect (6, 0, 16, Frame.Height);
			if (textField.Frame.Location == CGPoint.Empty)
				textField.Frame = new CGRect (imageView.Frame.Right, 0, Frame.Width - 16, Frame.Height);

			base.DrawRect (dirtyRect);
		}

		protected override void Dispose (bool disposing)
		{
			TaskService.Errors.TasksAdded -= updateHandler;
			TaskService.Errors.TasksRemoved -= updateHandler;
			base.Dispose (disposing);
		}

		NSTrackingArea textFieldArea;
		void ReconstructString (bool updateTrackingAreas)
		{
			if (string.IsNullOrEmpty (text)) {
				textField.AttributedStringValue = new NSAttributedString ("");
				imageView.Image = ImageService.GetIcon (Stock.StatusSteady).ToNSImage ();
			} else {
				textField.AttributedStringValue = GetStatusString (text, textColor);
				imageView.Image = image;
			}

			var width = textField.AttributedStringValue.BoundingRectWithSize (new CGSize (nfloat.MaxValue, textField.Frame.Height),
				NSStringDrawingOptions.UsesFontLeading | NSStringDrawingOptions.UsesLineFragmentOrigin).Width;

			if (!updateTrackingAreas)
				return;
			
			if (textFieldArea != null) {
				RemoveTrackingArea (textFieldArea);
				DestroyPopover ();
			}

			if (width > textField.Frame.Width) {
				textFieldArea = new NSTrackingArea (textField.Frame, NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.ActiveInKeyWindow, this, null);
				AddTrackingArea (textFieldArea);
			} else
				textFieldArea = null;
		}

		CALayer ProgressLayer {
			get { return Layer.Sublayers.FirstOrDefault (l => l.Name == ProgressLayerId); }
		}

		readonly Dictionary<string, StatusIcon> layerToStatus = new Dictionary<string, StatusIcon> ();
		internal void RemoveStatusLayer (CALayer statusLayer)
		{
			RemoveTrackingArea (layerToStatus [statusLayer.Name].TrackingArea);
			layerToStatus.Remove (statusLayer.Name);
			RepositionStatusLayers ();
		}

		nfloat LeftMostStatusItemX ()
		{
			if (Layer.Sublayers == null)
				return Layer.Frame.Width;

			var left = Layer.Sublayers.Min<CALayer, nfloat> (layer => {
				if (layer.Name == null)
					return nfloat.PositiveInfinity;

				if (layer.Name.StartsWith (StatusIconPrefixId, StringComparison.Ordinal))
					return layer.Frame.Left;
				return nfloat.PositiveInfinity;
			});
			return left == nfloat.PositiveInfinity ? Layer.Frame.Width : left;
		}

		nfloat DrawSeparatorIfNeeded (nfloat right)
		{
			CALayer layer = Layer.Sublayers.FirstOrDefault (l => l.Name == SeparatorLayerId);
			if (layerToStatus.Count == 0) {
				if (layer != null)
					layer.RemoveFromSuperLayer ();
				return right;
			}

			right -= 9;
			if (layer != null) {
				layer.Frame = new CGRect (right, MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 4 : 3, 1, 16);
				layer.SetNeedsDisplay ();
			} else {
				layer = CALayer.Create ();
				layer.Name = SeparatorLayerId;
				layer.Frame = new CGRect (right, MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 4 : 3, 1, 16);
				layer.BackgroundColor = NSColor.LightGray.CGColor;
				Layer.AddSublayer (layer);
			}
			return right - 3;
		}

		bool buildResultVisible;
		readonly CATextLayer buildResultText = new CATextLayer {
			Name = BuildTextLayerId,
		};
		IconId buildImageId;
		readonly CALayer buildResultIcon = new CALayer {
			Name = BuildIconLayerId,
		};
		nfloat DrawBuildResults ()
		{
			if (!buildResultVisible) {
				if (Layer.Sublayers != null) {
					CALayer layer;
					layer = Layer.Sublayers.FirstOrDefault (l => l.Name != null && l.Name.StartsWith (BuildIconLayerId, StringComparison.Ordinal));
					if (layer != null)
						layer.RemoveFromSuperLayer ();

					layer = Layer.Sublayers.FirstOrDefault (l => l.Name != null && l.Name.StartsWith (BuildTextLayerId, StringComparison.Ordinal));
					if (layer != null)
						layer.RemoveFromSuperLayer ();

					layer = Layer.Sublayers.FirstOrDefault (l => l.Name != null && l.Name.StartsWith (SeparatorLayerId, StringComparison.Ordinal));
					if (layer != null)
						layer.RemoveFromSuperLayer ();
				}
				return nfloat.PositiveInfinity;
			}

			nfloat right = DrawSeparatorIfNeeded (LeftMostStatusItemX ());
			CGSize size = buildResultText.AttributedString.Size;
			right = right - 6 - size.Width;
			buildResultText.Frame = new CGRect (right, MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 6 : 5, size.Width, size.Height);
			if (buildResultText.SuperLayer == null)
				Layer.AddSublayer (buildResultText);
			buildResultText.SetNeedsDisplay ();
			right -= buildResultIcon.Bounds.Width;
			buildResultIcon.Frame = new CGRect (right, MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 4 : 3, buildResultIcon.Bounds.Width, buildResultIcon.Bounds.Height);
			if (buildResultIcon.SuperLayer == null)
				Layer.AddSublayer (buildResultIcon);

			return right;
		}

		internal void RepositionStatusLayers ()
		{
			nfloat right = Layer.Frame.Width;
			CATransaction.DisableActions = true;
			foreach (var item in Layer.Sublayers) {
				if (item.Name != null && item.Name.StartsWith (StatusIconPrefixId, StringComparison.Ordinal)) {
					var icon = layerToStatus [item.Name];
					if (icon.TrackingArea != null)
						RemoveTrackingArea (icon.TrackingArea);

					right -= item.Bounds.Width + 6;
					item.Frame = new CGRect (right, MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 4 : 3, item.Bounds.Width, item.Bounds.Height);

					var area = new NSTrackingArea (item.Frame, NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.ActiveInKeyWindow, this, null);
					AddTrackingArea (area);

					icon.TrackingArea = area;
				}
			}

			nfloat buildResultPosition = DrawBuildResults ();
			CATransaction.DisableActions = false;
			if (buildResultPosition < right) { // We have a build result layer.
				textField.SetFrameSize (new CGSize (buildResultPosition - 6 - textField.Frame.Left, Frame.Height));
			} else
				textField.SetFrameSize (new CGSize (right - 6 - textField.Frame.Left, Frame.Height));
		}

		long statusCounter;
		public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			var layer = CALayer.Create ();
			layer.Name = StatusIconPrefixId + (++statusCounter);
			layer.Bounds = new CGRect (0, 0, (nfloat)pixbuf.Width, (nfloat)pixbuf.Height);
			var statusIcon = new StatusIcon (this, layer, null) {
				Image = pixbuf,
			};
			layerToStatus [layer.Name] = statusIcon;

			Layer.AddSublayer (layer);

			RepositionStatusLayers ();

			return statusIcon;
		}

		public StatusBarContext CreateContext ()
		{
			return ctxHandler.CreateContext ();
		}

		public void ShowReady ()
		{
			ShowMessage (null, "", false, NSColor.DisabledControlText);
		}

		static Pad sourcePad;
		public void SetMessageSourcePad (Pad pad)
		{
			sourcePad = pad;
		}

		public void ShowError (string error)
		{
			ShowMessage (Stock.StatusError, error, false, NSColor.FromDeviceRgba (228f / 255, 84f / 255, 55f / 255, 1));
		}

		public void ShowWarning (string warning)
		{
			ShowMessage (Stock.StatusWarning, warning, false, NSColor.FromDeviceRgba (235f / 255, 161f / 255, 7f / 255, 1));
		}

		public void ShowMessage (string message)
		{
			ShowMessage (null, message, false, NSColor.FromRgba (0.34f, 0.34f, 0.34f, 1));
		}

		public void ShowMessage (string message, bool isMarkup)
		{
			ShowMessage (null, message, true, NSColor.FromRgba (0.34f, 0.34f, 0.34f, 1));
		}

		public void ShowMessage (IconId image, string message)
		{
			ShowMessage (image, message, false, NSColor.FromRgba (0.34f, 0.34f, 0.34f, 1));
		}

		public void ShowMessage (IconId image, string message, bool isMarkup)
		{
			ShowMessage (image, message, isMarkup, NSColor.FromRgba (0.34f, 0.34f, 0.34f, 1));
		}

		public void ShowMessage (IconId image, string message, bool isMarkup, NSColor color)
		{
			DispatchService.AssertGuiThread ();

			LoadText (message, isMarkup, color);
			LoadPixbuf (image);
			ReconstructString (updateTrackingAreas: true);
		}

		void LoadText (string message, bool isMarkup, NSColor color)
		{
			message = message ?? "";

			text = message.Replace (Environment.NewLine, " ").Replace ("\n", " ").Trim ();
			currentTextIsMarkup = isMarkup;
			textColor = color;
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
					ReconstructString (updateTrackingAreas: false);
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
			if (ProgressLayer != null) {
				ProgressLayer.RemoveAnimation (growthAnimationKey);
				if (inProgress == false)
					ProgressLayer.RemoveFromSuperLayer ();
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
		static nfloat verticalOffset = MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 2 : 0;
		CALayer CreateProgressBarLayer (double width)
		{
			CALayer progress = ProgressLayer;
			if (progress == null) {
				progress = CALayer.Create ();
				progress.Name = ProgressLayerId;
				progress.BackgroundColor = xamBlue;
				progress.BorderColor = xamBlue;
				progress.FillMode = CAFillMode.Forwards;
				progress.Frame = new CGRect (0, Frame.Height - barHeight - verticalOffset, (nfloat)width, barHeight);
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
			var oldLayer = ProgressLayer;
			if (oldLayer == null)
				Layer.AddSublayer (progress);

			UpdateLayer ();
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

		bool CreatePopoverForLayer (CALayer layer)
		{
			string tooltip = layerToStatus [layer.Name].ToolTip;
			if (tooltip == null)
				return false;

			CreatePopoverCommon (230, tooltip);
			return true;
		}

		void CreatePopoverForStatusBar ()
		{
			CreatePopoverCommon (Frame.Width, textField.AttributedStringValue.Value);
		}

		void ShowPopoverForLayer (CALayer layer)
		{
			if (popover != null)
				return;

			if (!layerToStatus.ContainsKey (layer.Name))
				return;

			if (!CreatePopoverForLayer (layer))
				return;

			popover.Show (layer.Frame, this, NSRectEdge.MinYEdge);
		}

		void ShowPopoverForStatusBar ()
		{
			if (popover != null)
				return;

			CreatePopoverForStatusBar ();
			popover.Show (textField.Frame, this, NSRectEdge.MinYEdge);
		}

		void DestroyPopover ()
		{
			oldLayer = null;
			if (popover != null)
				popover.Close ();
			popover = null;
		}

		bool InTextField (CGPoint location)
		{
			return textField.IsMouseInRect (location, textField.Frame);
		}

		CALayer LayerForPoint (CGPoint location)
		{
			CALayer layer = Layer.PresentationLayer.HitTest (location);
			return layer != null ? layer.ModelLayer : null;
		}

		string oldLayer;
		public override void MouseEntered (NSEvent theEvent)
		{
			base.MouseEntered (theEvent);

			CGPoint location = ConvertPointFromView (theEvent.LocationInWindow, null);

			if (InTextField (location)) {
				ShowPopoverForStatusBar ();
				return;
			}

			var layer = LayerForPoint (location);
			if (layer == null)
				return;

			if (layer.Name == oldLayer) {
				StatusIcon icon;
				if (!layerToStatus.TryGetValue (layer.Name, out icon))
					return;

				if (string.IsNullOrEmpty (icon.ToolTip))
					return;
			}

			oldLayer = layer.Name;
			ShowPopoverForLayer (layer);
		}

		public override void MouseExited (NSEvent theEvent)
		{
			base.MouseExited (theEvent);

			DestroyPopover ();
		}

		public override void MouseDown (NSEvent theEvent)
		{
			base.MouseDown (theEvent);

			CGPoint location = ConvertPointFromView (theEvent.LocationInWindow, null);
			var layer = LayerForPoint (location);
			if (layer != null && layer.Name != null) {
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

				if (layerToStatus.ContainsKey (layer.Name)) {
					DestroyPopover ();
					layerToStatus [layer.Name].NotifyClicked (button);
					return;
				}

				if (layer.Name == BuildIconLayerId || layer.Name == BuildTextLayerId) { // We clicked error icon.
					IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ().BringToFront ();
					return;
				}
			}

			if (sourcePad != null)
				sourcePad.BringToFront (true);
		}

		public override void ViewDidEndLiveResize ()
		{
			base.ViewDidEndLiveResize ();
			RepositionStatusLayers ();
		}
	}
}
