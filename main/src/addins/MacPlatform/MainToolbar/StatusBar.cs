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
using AppKit;
using Foundation;
using CoreAnimation;
using CoreGraphics;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Components;

// TODO:
// Autopulse - Smaller bar moving.
// Hook logic.
// Status Icon CALayer.
namespace MonoDevelop.MacIntegration
{
	class StatusIcon : StatusBarIcon
	{
		public void SetAlertMode (int seconds)
		{
		}

		public void Dispose ()
		{
		}

		public string ToolTip {
			get;
			set;
		}

		public Gtk.EventBox EventBox {
			get {
				return new Gtk.EventBox ();
				//throw new NotImplementedException ();
			}
		}

		public Xwt.Drawing.Image Image {
			get;
			set;
		}
	}

	[Register]
	class StatusBar : NSTextField, MonoDevelop.Ide.StatusBar
	{
		const string growthAnimationKey = "bounds";
		StatusBarContextHandler ctxHandler;
		Queue<double> progressMarks = new Queue<double> ();
		bool currentTextIsMarkup;
		string text;
		NSColor textColor;
		NSImage image;
		IconId icon;
		AnimatedIcon iconAnimation;
		IDisposable xwtAnimation;

		NSAttributedString GetAttributedString (string text, string imageResource, CGSize size, NSColor color)
		{
			return GetAttributedString (text, ImageService.GetIcon (imageResource, Gtk.IconSize.Menu).ToNSImage (), size, color);
		}

		NSAttributedString GetAttributedString (string text, NSImage image, CGSize size, NSColor color)
		{
			var attrString = new NSMutableAttributedString ("");
			if (image != null) {
				// FIXME: Use the size parameter.
				// Center image with frame.
				if (!size.IsEmpty)
					image.AlignmentRect = new CGRect (0, -3, image.Size.Width, image.Size.Height);
					attrString.Append (NSAttributedString.FromAttachment (new NSTextAttachment { AttachmentCell = new NSTextAttachmentCell (image) }));
				}

			attrString.Append (new NSAttributedString (" "));
			attrString.Append (new NSAttributedString (text, new NSStringAttributes {
				BaselineOffset = 6,
				ForegroundColor = color,
				ParagraphStyle = new NSMutableParagraphStyle { LineBreakMode = NSLineBreakMode.TruncatingMiddle, Alignment = NSTextAlignment.Center }
			}));

			return attrString;
		}

		public StatusBar ()
		{
			Cell.PlaceholderAttributedString = GetAttributedString (BrandingService.ApplicationName, Stock.StatusSteady,
				new CGSize (350, 25), NSColor.DisabledControlText);

			AllowsEditingTextAttributes = WantsLayer = true;
			Editable = Selectable = false;
			Layer.CornerRadius = 4;
			ctxHandler = new StatusBarContextHandler (this);
		}

		void ReconstructString ()
		{
			if (string.IsNullOrEmpty (text))
				AttributedStringValue = new NSAttributedString ("");
			else
				AttributedStringValue = GetAttributedString (text, image, new CGSize (350, 25), textColor);
		}

		CALayer TextLayer {
			get { return Layer.Sublayers [0]; }
		}

		CALayer ProgressLayer {
			get {
				// Ignore the TextLayer.
				return Layer.Sublayers.Length > 1 ? Layer.Sublayers [1] : null;
			}
		}

		public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			var statusIcon = new StatusIcon ();
			// TODO: Add CALayer for update button.
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
			ShowMessage (Stock.StatusError, error, false, NSColor.Red);
		}

		public void ShowWarning (string warning)
		{
			ShowMessage (Stock.StatusWarning, warning, false, NSColor.Yellow);
		}

		public void ShowMessage (string message)
		{
			ShowMessage (null, message, false, NSColor.Black);
		}

		public void ShowMessage (string message, bool isMarkup)
		{
			ShowMessage (null, message, true, NSColor.Black);
		}

		public void ShowMessage (IconId image, string message)
		{
			ShowMessage (image, message, false, NSColor.Black);
		}

		public void ShowMessage (IconId image, string message, bool isMarkup)
		{
			ShowMessage (image, message, isMarkup, NSColor.Black);
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
		}

		public void BeginProgress (IconId image, string name)
		{
			ShowMessage (image, name);
			oldFraction = 0;
		}

		bool inProgress;
		double oldFraction;
		public void SetProgressFraction (double work)
		{
			progressMarks.Enqueue (work);
			if (!inProgress) {
				inProgress = true;
				StartProgress (progressMarks.Dequeue ());
			}
		}

		public void EndProgress ()
		{
			progressMarks.Clear ();
			inProgress = false;
			ProgressLayer.RemoveAnimation (growthAnimationKey);
			AutoPulse = false;
		}

		public void Pulse ()
		{
			// TODO: Hook in animation.
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

		static CGColor xamBlue = new CGColor ((nfloat)52 / 255, (nfloat)152 / 255, (nfloat)219.0 / 255);
		const int barHeight = 4;
		void StartProgress (double newFraction)
		{
			var progress = CALayer.Create ();
			progress.BackgroundColor = xamBlue;
			progress.BorderColor = xamBlue;
			progress.BorderWidth = 2;
			progress.FillMode = CAFillMode.Forwards;
			progress.Frame = new CGRect (Frame.Left - 4, Frame.Bottom - 5 - barHeight, Frame.Width, barHeight);

			CAAnimationGroup grp = CAAnimationGroup.CreateAnimation ();
			grp.Duration = 0.2;
			grp.FillMode = CAFillMode.Forwards;
			grp.RemovedOnCompletion = false;

			CABasicAnimation move = CABasicAnimation.FromKeyPath ("position.x");
			double oldOffset = (progress.Frame.Width / 2) * oldFraction;
			double newOffset = (progress.Frame.Width / 2) * newFraction;
			move.From = NSNumber.FromDouble (oldOffset);
			move.To = NSNumber.FromDouble (newOffset);

			CABasicAnimation grow = CABasicAnimation.FromKeyPath ("bounds");
			grow.From = NSValue.FromCGRect (new CGRect (0, 0, progress.Frame.Width * (nfloat)oldFraction, barHeight));
			grow.To = NSValue.FromCGRect (new CGRect (0, 0, progress.Frame.Width * (nfloat)newFraction, barHeight));

			oldFraction = newFraction;

			grp.Animations = new [] {
				move,
				grow,
			};
			grp.AnimationStopped += (sender, e) => {
				if (oldFraction < 1 && inProgress) {
					if (progressMarks.Count != 0)
						StartProgress (progressMarks.Dequeue ());
					else {
						inProgress = false;
					}
					return;
				}

				AutoPulse = false;
				inProgress = false;
				ShowReady ();

				CABasicAnimation fadeout = CABasicAnimation.FromKeyPath ("opacity");
				fadeout.From = NSNumber.FromDouble (1);
				fadeout.To = NSNumber.FromDouble (0);
				fadeout.Duration = 0.5;
				fadeout.FillMode = CAFillMode.Forwards;
				fadeout.RemovedOnCompletion = false;
				fadeout.AnimationStopped += (sender2, e2) => {
					if (!e2.Finished)
						return;

					progress.Opacity = 0;
					progress.RemoveFromSuperLayer ();
				};
				progress.AddAnimation (fadeout, "opacity");
			};
			progress.AddAnimation (grp, growthAnimationKey);

			if (Layer.Sublayers.Length == 2)
				Layer.ReplaceSublayer (Layer.Sublayers [1], progress);
			else
				Layer.AddSublayer (progress);

			UpdateLayer ();
		}

		public override void MouseDown (NSEvent theEvent)
		{
			if (sourcePad != null)
				sourcePad.BringToFront (true);
			base.MouseDown (theEvent);
		}
	}
}
