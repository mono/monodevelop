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

namespace MonoDevelop.MacIntegration
{
	class StatusIcon : StatusBarIcon
	{
		StatusBar bar;
		CALayer layer;

		public StatusIcon (StatusBar bar, CALayer layer)
		{
			this.bar = bar;
			this.layer = layer;
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

		Xwt.Drawing.Image image;
		public Xwt.Drawing.Image Image {
			get { return image; }
			set {
				image = value;
				layer.Contents = value.ToNSImage ().CGImage;
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

		static NSAttributedString GetAttributedString (string text, string imageResource, CGSize size, NSColor color)
		{
			return GetAttributedString (text, ImageService.GetIcon (imageResource, Gtk.IconSize.Menu).ToNSImage (), size, color);
		}

		static NSAttributedString GetAttributedString (string text, NSImage image, CGSize size, NSColor color)
		{
			var attrString = new NSMutableAttributedString ("");
			if (image != null) {
				// FIXME: Use the size parameter.
				// Center image with frame.
				if (!size.IsEmpty)
					image.AlignmentRect = new CGRect (-2, -3, image.Size.Width, image.Size.Height);
					attrString.Append (NSAttributedString.FromAttachment (new NSTextAttachment { AttachmentCell = new NSTextAttachmentCell (image) }));
				}

			attrString.Append (new NSAttributedString ("  "));
			attrString.Append (new NSAttributedString (text, new NSStringAttributes {
				BaselineOffset = 6,
				ForegroundColor = color,
				ParagraphStyle = new NSMutableParagraphStyle { LineBreakMode = NSLineBreakMode.TruncatingMiddle, Alignment = NSTextAlignment.Center }
			}));

			return attrString;
		}

		TaskEventHandler updateHandler;
		public StatusBar ()
		{
			Cell.PlaceholderAttributedString = GetAttributedString (BrandingService.ApplicationName, Stock.StatusSteady,
				new CGSize (360, 23), NSColor.DisabledControlText);

			AllowsEditingTextAttributes = WantsLayer = true;
			Editable = Selectable = false;
			Layer.CornerRadius = 4;
			ctxHandler = new StatusBarContextHandler (this);

			updateHandler = delegate {
				int ec=0, wc=0;

				foreach (Task t in TaskService.Errors) {
					if (t.Severity == TaskSeverity.Error)
						ec++;
					else if (t.Severity == TaskSeverity.Warning)
						wc++;
				}

				if (ec > 0) {
					buildResultVisible = true;
					buildResultText.AttributedString = new NSAttributedString (ec.ToString (), foregroundColor: NSColor.Text,
						font: NSFont.SystemFontOfSize (NSFont.SmallSystemFontSize));
					buildResultIcon.Contents = ImageService.GetIcon ("md-status-error-count", Gtk.IconSize.Menu).ToNSImage ().CGImage;
				} else if (wc > 0) {
					buildResultVisible = true;
					buildResultText.AttributedString = new NSAttributedString (wc.ToString (), foregroundColor: NSColor.Text,
						font: NSFont.SystemFontOfSize (NSFont.SmallSystemFontSize));
					buildResultIcon.Contents = ImageService.GetIcon ("md-status-warning-count", Gtk.IconSize.Menu).ToNSImage ().CGImage;
				} else
					buildResultVisible = false;

				DrawBuildResults ();
			};

			updateHandler (null, null);

			TaskService.Errors.TasksAdded += updateHandler;
			TaskService.Errors.TasksRemoved += updateHandler;
		}

		protected override void Dispose (bool disposing)
		{
			TaskService.Errors.TasksAdded -= updateHandler;
			TaskService.Errors.TasksRemoved -= updateHandler;
			base.Dispose (disposing);
		}

		void ReconstructString ()
		{
			if (string.IsNullOrEmpty (text))
				AttributedStringValue = new NSAttributedString ("");
			else
				AttributedStringValue = GetAttributedString (text, image, new CGSize (350, 22), textColor);
		}

		CALayer ProgressLayer {
			get { return Layer.Sublayers.FirstOrDefault (l => l.Name == ProgressLayerId); }
		}

		readonly Dictionary<CALayer, StatusIcon> layerToStatus = new Dictionary<CALayer, StatusIcon> ();
		internal void RemoveStatusLayer (CALayer statusLayer)
		{
			layerToStatus.Remove (statusLayer);
			RepositionStatusLayers ();
		}

		nfloat LeftMostItemX ()
		{
			if (Layer.Sublayers == null)
				return Frame.Width;

			return Layer.Sublayers.Min<CALayer, nfloat> (layer => {
				if (layer.Name == null)
					return nfloat.PositiveInfinity;

				if (layer.Name == SeparatorLayerId || layer.Name.StartsWith (StatusIconPrefixId, StringComparison.Ordinal))
					return layer.Frame.Left;
				return nfloat.PositiveInfinity;
			});
		}

		nfloat DrawSeparatorIfNeeded (nfloat right)
		{
			if (layerToStatus.Count == 0)
				return right;

			right -= 6;
			var layer = Layer.Sublayers.FirstOrDefault (l => l.Name == SeparatorLayerId);
			if (layer != null) {
				layer.Frame = new CGRect (right, 3, 1, 16);
				layer.SetNeedsDisplay ();
			} else {
				layer = CALayer.Create ();
				layer.Name = SeparatorLayerId;
				layer.Frame = new CGRect (right, 3, 1, 16);
				layer.BackgroundColor = NSColor.LightGray.CGColor;
				Layer.AddSublayer (layer);
			}
			return right;
		}

		bool buildResultVisible;
		readonly CATextLayer buildResultText = new CATextLayer ();
		readonly CALayer buildResultIcon = new CALayer ();
		void DrawBuildResults ()
		{
			if (buildResultText.Name != BuildTextLayerId)
				buildResultText.Name = BuildTextLayerId;
			if (buildResultIcon.Name != BuildIconLayerId)
				buildResultIcon.Name = BuildIconLayerId;

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
				return;
			}

			nfloat right = DrawSeparatorIfNeeded (LeftMostItemX ());
			CGSize size = buildResultText.AttributedString.Size;
			right = right - 6 - size.Width;
			buildResultText.Frame = new CGRect (right - 0.5f, 4f, size.Width, size.Height);
			if (buildResultText.SuperLayer == null)
				Layer.AddSublayer (buildResultText);
			buildResultText.SetNeedsDisplay ();
			buildResultIcon.Frame = new CGRect (right - 0.5f - buildResultIcon.Contents.Width, 3, buildResultIcon.Contents.Width, buildResultIcon.Contents.Height);
			if (buildResultIcon.SuperLayer == null)
				Layer.AddSublayer (buildResultIcon);
		}

		class TooltipOwner : NSObject
		{
			readonly StatusBar bar;
			readonly CALayer layer;

			public TooltipOwner (StatusBar bar, CALayer layer)
			{
				this.bar = bar;
				this.layer = layer;
			}

			public override string Description { get { return bar.layerToStatus [layer].ToolTip; } }
		}

		List<TooltipOwner> tooltips = new List<TooltipOwner> ();
		void AddTooltip (CALayer layer)
		{
			var tooltip = new TooltipOwner (this, layer);
			tooltips.Add (tooltip);
			AddToolTip (layer.Frame, tooltip, IntPtr.Zero);
		}

		void RepositionStatusLayers ()
		{
			RemoveAllToolTips ();
			foreach (var tooltip in tooltips)
				tooltip.Dispose ();
			tooltips.Clear ();

			nfloat right = Frame.Width;
			foreach (var item in Layer.Sublayers) {
				if (item.Name.StartsWith (StatusIconPrefixId, StringComparison.Ordinal)) {
					right -= item.Contents.Width + 6;
					item.Frame = new CGRect (right, 3, item.Contents.Width, item.Contents.Height);
					AddTooltip (item);
					item.SetNeedsDisplay ();
				}
			}

			DrawBuildResults ();
		}

		long statusCounter;
		public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			nfloat right = layerToStatus.Count == 0 ?
				Frame.Width :
				Layer.Sublayers.Last (i => i.Name.StartsWith (StatusIconPrefixId, StringComparison.Ordinal)).Frame.Left;

			var layer = CALayer.Create ();
			layer.Name = StatusIconPrefixId + (++statusCounter);
			layer.Contents = pixbuf.ToNSImage ().CGImage;
			layer.Frame = new CGRect (right - (nfloat)pixbuf.Width - 9, 3, (nfloat)pixbuf.Width, (nfloat)pixbuf.Height);
			var statusIcon = new StatusIcon (this, layer);
			layerToStatus [layer] = statusIcon;
			AddTooltip (layer);

			Layer.AddSublayer (layer);

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
			ReconstructString ();
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
				iconId = "md-status-steady";

			// load image now
			if (ImageService.IsAnimation (iconId, Gtk.IconSize.Menu)) {
				iconAnimation = ImageService.GetAnimatedIcon (iconId, Gtk.IconSize.Menu);
				image = iconAnimation.FirstFrame.ToNSImage ();
				xwtAnimation = iconAnimation.StartAnimation (p => {
					var dummy = p.ToPixbuf ();
					dummy.Dispose ();
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
			inProgress = false;
			if (ProgressLayer != null)
				ProgressLayer.RemoveAnimation (growthAnimationKey);
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
		CALayer CreateProgressBarLayer (double width)
		{
			CALayer progress = ProgressLayer;
			if (progress == null) {
				progress = CALayer.Create ();
				progress.Name = ProgressLayerId;
				progress.BackgroundColor = xamBlue;
				progress.BorderColor = xamBlue;
				progress.FillMode = CAFillMode.Forwards;
				progress.Frame = new CGRect (0, Frame.Height - barHeight, (nfloat)width, barHeight);
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
			move.To = NSNumber.FromDouble (Frame.Width + frameAutoPulseWidth);
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
			var progress = CreateProgressBarLayer (Frame.Width);
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

		CALayer LayerForEvent (NSEvent theEvent)
		{
			CGPoint location = ConvertPointFromView (theEvent.LocationInWindow, null);
			CALayer layer = Layer.PresentationLayer.HitTest (location);
			return layer != null ? layer.ModelLayer : null;
		}

		public override void MouseMoved (NSEvent theEvent)
		{
			// Take care of tooltip.
			base.MouseMoved (theEvent);
		}

		public override void MouseDown (NSEvent theEvent)
		{
			base.MouseDown (theEvent);

			var layer = LayerForEvent (theEvent);
			if (layer != null) {
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

				if (layerToStatus.ContainsKey (layer)) {
					layerToStatus [layer].NotifyClicked (button);
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
	}
}
