// 
// StatusArea.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Diagnostics;
using Gtk;
using MonoDevelop.Components;
using Cairo;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;

using StockIcons = MonoDevelop.Ide.Gui.Stock;

namespace MonoDevelop.Components.MainToolbar
{
	interface Easing
	{
		float Ease (float val);
	}

	class LinearEasing : Easing
	{
		public float Ease (float val)
		{
			return val;
		}
	}

	class SinInOutEasing : Easing
	{
		public float Ease (float val)
		{
			return (float)Math.Sin (val * Math.PI * 0.5f);
		}
	}

	class Tweener
	{
		public uint Length { get; private set; }
		public uint Rate { get; private set; }
		public float Value { get; private set; }
		public Easing Easing { get; set; }

		public bool IsRunning {
			get { return runningTime.IsRunning; }
		}

		public event EventHandler ValueUpdated;
		public event EventHandler Finished;

		Stopwatch runningTime;
		uint timeoutHandle;

		public Tweener (uint length, uint rate)
		{
			Value = 0.0f;
			Length = length;
			Rate = rate;
			runningTime = new Stopwatch ();
			Easing = new LinearEasing ();
		}

		~Tweener ()
		{
			if (timeoutHandle > 0)
				GLib.Source.Remove (timeoutHandle);
		}

		public void Start ()
		{
			// reset any current running instance and reset the stopwatch.
			Pause ();
			runningTime.Reset ();
			Value = 0.0f;

			runningTime.Start ();
			timeoutHandle = GLib.Timeout.Add (Rate, () => { 
				Value = Math.Min (1.0f, runningTime.ElapsedMilliseconds / (float) Length);
				if (ValueUpdated != null)
					ValueUpdated (this, EventArgs.Empty);

				if (Value >= 1.0f)
				{
					runningTime.Stop ();
					timeoutHandle = 0;
					if (Finished != null)
						Finished (this, EventArgs.Empty);
					return false;
				}
				return true;
			});
		}

		public void Stop ()
		{
			Pause ();
			runningTime.Reset ();
			Value = 1.0f;
			if (Finished != null)
				Finished (this, EventArgs.Empty);
		}

		public void Pause ()
		{
			runningTime.Stop ();

			if (timeoutHandle > 0) {
				GLib.Source.Remove (timeoutHandle);
				timeoutHandle = 0;
			}
		}
	}

	class MouseTracker
	{
		public bool Hovered { get; private set; }
		public Gdk.Point MousePosition { get; private set; }

		public event EventHandler MouseMoved;
		public event EventHandler HoveredChanged;

		public MouseTracker (Gtk.Widget owner)
		{
			Hovered = false;
			MousePosition = new Gdk.Point(0, 0);

			owner.Events = owner.Events | Gdk.EventMask.PointerMotionMask;

			owner.MotionNotifyEvent += (object o, MotionNotifyEventArgs args) => {
				MousePosition = new Gdk.Point ((int)args.Event.X, (int)args.Event.Y);
				if (MouseMoved != null)
					MouseMoved (this, EventArgs.Empty);
			};

			owner.EnterNotifyEvent += (o, args) => {
				Hovered = true;
				if (HoveredChanged != null)
					HoveredChanged (this, EventArgs.Empty);
			};

			owner.LeaveNotifyEvent += (o, args) => {
				Hovered = false;
				if (HoveredChanged != null)
					HoveredChanged (this, EventArgs.Empty);
			};
		}
	}

	class StatusArea : EventBox, StatusBar
	{
		struct Message
		{
			public string Text;
			public IconId Icon;
			public bool IsMarkup;

			public Message (IconId icon, string text, bool markup)
			{
				Text = text;
				Icon = icon;
				IsMarkup = markup;
			}
		}

		const int PaddingLeft = 10;

		HBox contentBox = new HBox (false, 8);

		StatusAreaSeparator statusIconSeparator;
		Gtk.Widget buildResultWidget;

		readonly HBox messageBox = new HBox ();
		internal readonly HBox statusIconBox = new HBox ();
		Alignment mainAlign;

		string lastText;
		string currentText;

		Tweener textAnimTweener;
		Tweener mouseHoverTweener;
		MouseTracker tracker;

		bool textIsMarkup;
		bool lastTextIsMarkup;

		AnimatedIcon iconAnimation;
		IconId currentIcon;
		Gdk.Pixbuf currentPixbuf;
		static Pad sourcePad;
		IDisposable currentIconAnimation;
		double progressFraction;
		bool showingProgress;

		IDisposable progressFadeAnimation;
		double progressDisplayAlpha;

		MainStatusBarContextImpl mainContext;
		StatusBarContextImpl activeContext;

		Queue<Message> messageQueue;
		
		public StatusBar MainContext {
			get { return mainContext; }
		}

		public int MaxWidth { get; set; }

		public StatusArea ()
		{
			mainContext = new MainStatusBarContextImpl (this);
			activeContext = mainContext;
			contexts.Add (mainContext);

			VisibleWindow = false;
			NoShowAll = true;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;

			statusIconBox.BorderWidth = 0;
			statusIconBox.Spacing = 3;
			
			ProgressBegin += delegate {
				showingProgress = true;
				progressFraction = 0;
				QueueDraw ();
			};
			
			ProgressEnd += delegate {
				showingProgress = false;
				if (progressFadeAnimation != null)
					progressFadeAnimation.Dispose ();
				progressFadeAnimation = DispatchService.RunAnimation (delegate {
					progressDisplayAlpha -= 0.2;
					QueueDraw ();
					if (progressDisplayAlpha <= 0) {
						progressDisplayAlpha = 0;
						return 0;
					}
					else
						return 100;
				});
				QueueDraw ();
			};
			
			ProgressFraction += delegate(object sender, FractionEventArgs e) {
				progressFraction = e.Work;
				if (progressFadeAnimation != null)
					progressFadeAnimation.Dispose ();
				progressFadeAnimation = DispatchService.RunAnimation (delegate {
					progressDisplayAlpha += 0.2;
					QueueDraw ();
					if (progressDisplayAlpha >= 1) {
						progressDisplayAlpha = 1;
						return 0;
					}
					else
						return 100;
				});
				QueueDraw ();
			};

			contentBox.PackStart (messageBox, true, true, 0);
			contentBox.PackEnd (statusIconBox, false, false, 0);
			contentBox.PackEnd (statusIconSeparator = new StatusAreaSeparator (), false, false, 0);
			contentBox.PackEnd (buildResultWidget = CreateBuildResultsWidget (Orientation.Horizontal), false, false, 0);

			mainAlign = new Alignment (0, 0.5f, 1, 0);
			mainAlign.LeftPadding = 12;
			mainAlign.RightPadding = 8;
			mainAlign.Add (contentBox);
			Add (mainAlign);

			mainAlign.ShowAll ();
			statusIconBox.Hide ();
			statusIconSeparator.Hide ();
			buildResultWidget.Hide ();
			Show ();

			this.ButtonPressEvent += delegate {
				if (sourcePad != null)
					sourcePad.BringToFront (true);
			};

			statusIconBox.Shown += delegate {
				UpdateSeparators ();
			};

			statusIconBox.Hidden += delegate {
				UpdateSeparators ();
			};

			messageQueue = new Queue<Message> ();

			textAnimTweener = new Tweener(250, 16);
			textAnimTweener.Easing = new SinInOutEasing ();
			textAnimTweener.ValueUpdated += (o, a) => QueueDraw ();

			textAnimTweener.Finished += (o, a) => {
				if (messageQueue.Count > 0)
				{
					Message message = messageQueue.Dequeue();
					ShowMessageInner (message.Icon, message.Text, message.IsMarkup);
				}
			};

			mouseHoverTweener = new Tweener (250, 16);
			mouseHoverTweener.Easing = new SinInOutEasing ();
			mouseHoverTweener.ValueUpdated += (sender, e) => QueueDraw ();
			mouseHoverTweener.Finished += (sender, e) => QueueDraw ();

			tracker = new MouseTracker(this);
			tracker.HoveredChanged += (sender, e) => {
				mouseHoverTweener.Start ();
				QueueDraw ();
			};
			tracker.MouseMoved += (sender, e) => QueueDraw ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (MaxWidth > 0 && allocation.Width > MaxWidth) {
				allocation = new Gdk.Rectangle (allocation.X + (allocation.Width - MaxWidth) / 2, allocation.Y, MaxWidth, allocation.Height);
			}
			base.OnSizeAllocated (allocation);
		}

		void UpdateSeparators ()
		{
			statusIconSeparator.Visible = statusIconBox.Visible && buildResultWidget.Visible;
		}

		public Widget CreateBuildResultsWidget (Orientation orientation)
		{
			EventBox ebox = new EventBox ();

			Gtk.Box box;
			if (orientation == Orientation.Horizontal)
				box = new HBox ();
			else
				box = new VBox ();
			box.Spacing = 3;
			
			Gdk.Pixbuf errorIcon = ImageService.GetPixbuf (StockIcons.Error, IconSize.Menu);
			Gdk.Pixbuf noErrorIcon = ImageService.MakeGrayscale (errorIcon); // creates a new pixbuf instance
			Gdk.Pixbuf warningIcon = ImageService.GetPixbuf (StockIcons.Warning, IconSize.Menu);
			Gdk.Pixbuf noWarningIcon = ImageService.MakeGrayscale (warningIcon); // creates a new pixbuf instance
			
			Gtk.Image errorImage = new Gtk.Image (errorIcon);
			Gtk.Image warningImage = new Gtk.Image (warningIcon);
			
			box.PackStart (errorImage, false, false, 0);
			Label errors = new Gtk.Label ();
			box.PackStart (errors, false, false, 0);
			
			box.PackStart (warningImage, false, false, 0);
			Label warnings = new Gtk.Label ();
			box.PackStart (warnings, false, false, 0);
			box.NoShowAll = true;
			box.Show ();
			
			TaskEventHandler updateHandler = delegate {
				int ec=0, wc=0;
				foreach (Task t in TaskService.Errors) {
					if (t.Severity == TaskSeverity.Error)
						ec++;
					else if (t.Severity == TaskSeverity.Warning)
						wc++;
				}
				errors.Visible = ec > 0;
				errors.Text = ec.ToString ();
				errorImage.Visible = ec > 0;

				warnings.Visible = wc > 0;
				warnings.Text = wc.ToString ();
				warningImage.Visible = wc > 0;
				ebox.Visible = ec > 0 || wc > 0;
				UpdateSeparators ();
			};
			
			updateHandler (null, null);
			
			TaskService.Errors.TasksAdded += updateHandler;
			TaskService.Errors.TasksRemoved += updateHandler;
			
			box.Destroyed += delegate {
				noErrorIcon.Dispose ();
				noWarningIcon.Dispose ();
				TaskService.Errors.TasksAdded -= updateHandler;
				TaskService.Errors.TasksRemoved -= updateHandler;
			};

			ebox.VisibleWindow = false;
			ebox.Add (box);
			ebox.ShowAll ();
			ebox.ButtonReleaseEvent += delegate {
				var pad = IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ();
				pad.BringToFront ();
			};

			errors.Visible = false;
			errorImage.Visible = false;
			warnings.Visible = false;
			warningImage.Visible = false;

			return ebox;
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			ModifyText (StateType.Normal, Styles.StatusBarTextColor.ToGdkColor ());
			ModifyFg (StateType.Normal, Styles.StatusBarTextColor.ToGdkColor ());
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Height = 32;
			base.OnSizeRequested (ref requisition);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {

				CairoExtensions.RoundedRectangle (context, Allocation.X + 0.5, Allocation.Y + 1.5, Allocation.Width - 1, Allocation.Height - 1, 3);
				context.LineWidth = 1;
				context.Color = Styles.StatusBarShadowColor1;
				context.Stroke ();
				
				CairoExtensions.RoundedRectangle (context, Allocation.X + 0.5, Allocation.Y + 2.5, Allocation.Width - 1, Allocation.Height - 1, 3);
				context.LineWidth = 1;
				context.Color = Styles.StatusBarShadowColor2;
				context.Stroke ();
				
				CairoExtensions.RoundedRectangle (context, Allocation.X + 1, Allocation.Y + 1, Allocation.Width - 2, Allocation.Height - 2, 3);
				using (LinearGradient lg = new LinearGradient (Allocation.X + 1, Allocation.Y + 1, Allocation.X + 1, Allocation.Y + Allocation.Height - 1)) {
					lg.AddColorStop (0, Styles.StatusBarFill1Color);
					lg.AddColorStop (0.5, Styles.StatusBarFill2Color);
					lg.AddColorStop (0.5, Styles.StatusBarFill3Color);
					lg.AddColorStop (1, Styles.StatusBarFill4Color);
					context.Pattern = lg;
				}
				context.Fill ();

				CairoExtensions.RoundedRectangle (context, Allocation.X + 1.5, Allocation.Y + 1.5, Allocation.Width - 3, Allocation.Height - 3, 3);
				context.LineWidth = 1;
				context.Color = Styles.StatusBarInnerColor;
				context.Stroke ();

				CairoExtensions.RoundedRectangle (context, Allocation.X + 0.5, Allocation.Y + 0.5, Allocation.Width - 1, Allocation.Height - 1, 3);
				context.LineWidth = 1;
				context.Color = Styles.StatusBarBorderColor;
				context.StrokePreserve ();

				if (tracker.Hovered || mouseHoverTweener.IsRunning)
				{
					context.Clip ();
					int x1 = Allocation.X + tracker.MousePosition.X - 200;
					int x2 = x1 + 400;
					using (Cairo.LinearGradient gradient = new LinearGradient (x1, 0, x2, 0))
					{
						Cairo.Color targetColor = Styles.StatusBarFill1Color;
						Cairo.Color transparentColor = targetColor;
						targetColor.A = .7;
						transparentColor.A = 0;

						if (tracker.Hovered)
							targetColor.A = .7 * mouseHoverTweener.Value;
						else
							targetColor.A = .7 * (1.0 - mouseHoverTweener.Value);

						gradient.AddColorStop (0.0, transparentColor);
						gradient.AddColorStop (0.5, targetColor);
						gradient.AddColorStop (1.0, transparentColor);

						context.Pattern = gradient;

						context.Rectangle (x1, Allocation.Y, x2 - x1, Allocation.Height);
						context.Fill ();
					}
					context.ResetClip ();
				} else {
					context.NewPath ();
				}

				int progress_bar_x = messageBox.Allocation.X;
				int progress_bar_width = messageBox.Allocation.Width;

				if (currentPixbuf != null) {
					int y = Allocation.Y + (Allocation.Height - currentPixbuf.Height) / 2;
					Gdk.CairoHelper.SetSourcePixbuf (context, currentPixbuf, messageBox.Allocation.X, y);
					context.Paint ();
					progress_bar_x += currentPixbuf.Width + Styles.ProgressBarOuterPadding;
					progress_bar_width -= currentPixbuf.Width + Styles.ProgressBarOuterPadding;
				}

				int center = Allocation.Y + Allocation.Height / 2;

				if (showingProgress || progressDisplayAlpha > 0) {
					DrawProgressBar (context, progressFraction, new Gdk.Rectangle (progress_bar_x, center - Styles.ProgressBarHeight / 2, progress_bar_width, Styles.ProgressBarHeight));
				}

				int text_x = progress_bar_x + Styles.ProgressBarInnerPadding;
				int text_width = progress_bar_width - (Styles.ProgressBarInnerPadding * 2);

				if (lastText != null) {
					double opacity = 1.0f - textAnimTweener.Value;
					DrawString (lastText, lastTextIsMarkup, context, text_x, center - (int)(textAnimTweener.Value * Allocation.Height * 0.5), text_width, opacity);
				}

				if (currentText != null) {
					DrawString (currentText, textIsMarkup, context, text_x, center + (int)((1.0f - textAnimTweener.Value) * Allocation.Height * 0.5), text_width, textAnimTweener.Value);
				}
			}
			return base.OnExposeEvent (evnt);
		}

		void DrawProgressBar (Cairo.Context context, double progress, Gdk.Rectangle bounding)
		{
			CairoExtensions.RoundedRectangle (context, bounding.X + 0.5, bounding.Y + 0.5, (bounding.Width - 1) * progress, bounding.Height - 1, 3);
			context.Clip ();

			CairoExtensions.RoundedRectangle (context, bounding.X + 0.5, bounding.Y + 0.5, bounding.Width - 1, bounding.Height - 1, 3);
			context.Color = new Cairo.Color (Styles.StatusBarProgressBackgroundColor.R,
			                                 Styles.StatusBarProgressBackgroundColor.G,
			                                 Styles.StatusBarProgressBackgroundColor.B,
			                                 Styles.StatusBarProgressBackgroundColor.A * progressDisplayAlpha);
			context.FillPreserve ();

			context.ResetClip ();

			context.Color = new Cairo.Color (Styles.StatusBarProgressOutlineColor.R,
			                                 Styles.StatusBarProgressOutlineColor.G,
			                                 Styles.StatusBarProgressOutlineColor.B,
			                                 Styles.StatusBarProgressOutlineColor.A * progressDisplayAlpha);
			context.LineWidth = 1;
			context.Stroke ();
		}

		void DrawString (string text, bool isMarkup, Cairo.Context context, int x, int y, int width, double opacity)
		{
			Pango.Layout pl = new Pango.Layout (this.PangoContext);
			if (isMarkup)
				pl.SetMarkup (text);
			else
				pl.SetText (text);
			pl.FontDescription = Styles.StatusFont;
			pl.Ellipsize = Pango.EllipsizeMode.End;
			pl.Width = Pango.Units.FromPixels(width);

			int w, h;
			pl.GetPixelSize (out w, out h);

			context.Save ();
			// use widget height instead of message box height as message box does not have a true height when no widgets are packed in it
			// also ensures animations work properly instead of getting clipped
			context.Rectangle (new Rectangle (x, Allocation.Y, width, Allocation.Height));
			context.Clip ();

			// Subtract off remainder instead of drop to prefer higher centering when centering an odd number of pixels
			context.MoveTo (x, y - h / 2 - (h % 2));

			Cairo.Color finalColor = Styles.StatusBarTextColor;
			finalColor.A = opacity;
			context.Color = finalColor;

			Pango.CairoHelper.ShowLayout (context, pl);
			pl.Dispose ();
			context.Restore ();
		}

		#region StatusBar implementation

		public void ShowCaretState (int line, int column, int selectedChars, bool isInInsertMode)
		{
			throw new NotImplementedException ();
		}

		public void ClearCaretState ()
		{
			throw new NotImplementedException ();
		}

		public StatusBarIcon ShowStatusIcon (Gdk.Pixbuf pixbuf)
		{
			DispatchService.AssertGuiThread ();
			StatusIcon icon = new StatusIcon (this, pixbuf);
			statusIconBox.PackEnd (icon.box);
			statusIconBox.ShowAll ();
			return icon;
		}
		
		void HideStatusIcon (StatusIcon icon)
		{
			statusIconBox.Remove (icon.EventBox);
			if (statusIconBox.Children.Length == 0)
				statusIconBox.Hide ();
			icon.EventBox.Destroy ();
		}

		List<StatusBarContextImpl> contexts = new List<StatusBarContextImpl> ();
		public StatusBarContext CreateContext ()
		{
			StatusBarContextImpl ctx = new StatusBarContextImpl (this);
			contexts.Add (ctx);
			return ctx;
		}

		public void ShowReady ()
		{
			ShowMessage ("");
		}

		public void SetMessageSourcePad (Pad pad)
		{
			sourcePad = pad;
		}

		public bool HasResizeGrip {
			get;
			set;
		}

		public class StatusIcon : StatusBarIcon
		{
			StatusArea statusBar;
			internal EventBox box;
			string tip;
			DateTime alertEnd;
			Gdk.Pixbuf icon;
			uint animation;
			Gtk.Image image;
			
			int astep;
			Gdk.Pixbuf[] images;
			TooltipPopoverWindow tooltipWindow;
			bool mouseOver;
			
			public StatusIcon (StatusArea statusBar, Gdk.Pixbuf icon)
			{
				this.statusBar = statusBar;
				this.icon = icon;
				box = new EventBox ();
				box.VisibleWindow = false;
				image = new Image (icon);
				image.SetPadding (0, 0);
				box.Child = image;
				box.Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
				box.EnterNotifyEvent += HandleEnterNotifyEvent;
				box.LeaveNotifyEvent += HandleLeaveNotifyEvent;
			}
			
			[GLib.ConnectBefore]
			void HandleLeaveNotifyEvent (object o, LeaveNotifyEventArgs args)
			{
				mouseOver = false;
				HideTooltip ();
			}
			
			[GLib.ConnectBefore]
			void HandleEnterNotifyEvent (object o, EnterNotifyEventArgs args)
			{
				mouseOver = true;
				ShowTooltip ();
			}
			
			void ShowTooltip ()
			{
				if (!string.IsNullOrEmpty (tip)) {
					HideTooltip ();
					tooltipWindow = new TooltipPopoverWindow ();
					tooltipWindow.ShowArrow = true;
					tooltipWindow.Text = tip;
					tooltipWindow.ShowPopup (box, PopupPosition.Top);
				}
			}
			
			void HideTooltip ()
			{
				if (tooltipWindow != null) {
					tooltipWindow.Destroy ();
					tooltipWindow = null;
				}
			}
			
			public void Dispose ()
			{
				HideTooltip ();
				statusBar.HideStatusIcon (this);
				if (images != null) {
					foreach (Gdk.Pixbuf img in images) {
						img.Dispose ();
					}
				}
				if (animation != 0) {
					GLib.Source.Remove (animation);
					animation = 0;
				}
			}
			
			public string ToolTip {
				get { return tip; }
				set {
					tip = value;
					if (tooltipWindow != null) {
						if (!string.IsNullOrEmpty (tip))
							tooltipWindow.Text = value;
						else
							HideTooltip ();
					} else if (!string.IsNullOrEmpty (tip) && mouseOver)
						ShowTooltip ();
				}
			}
			
			public EventBox EventBox {
				get { return box; }
			}
			
			public Gdk.Pixbuf Image {
				get { return icon; }
				set {
					icon = value;
					image.Pixbuf = icon;
				}
			}
			
			public void SetAlertMode (int seconds)
			{
				astep = 0;
				alertEnd = DateTime.Now.AddSeconds (seconds);
				
				if (animation != 0)
					GLib.Source.Remove (animation);
				
				animation = GLib.Timeout.Add (60, new GLib.TimeoutHandler (AnimateIcon));
				
				if (images == null) {
					images = new Gdk.Pixbuf [10];
					for (int n=0; n<10; n++)
						images [n] = ImageService.MakeTransparent (icon, ((double)(9-n))/10.0);
				}
			}
			
			bool AnimateIcon ()
			{
				if (DateTime.Now >= alertEnd && astep == 0) {
					image.Pixbuf = icon;
					animation = 0;
					return false;
				}
				if (astep < 10)
					image.Pixbuf = images [astep];
				else
					image.Pixbuf = images [20 - astep - 1];
				
				astep = (astep + 1) % 20;
				return true;
			}
		}
		
		#endregion

		#region StatusBarContextBase implementation

		public void ShowError (string error)
		{
			ShowMessage (StockIcons.StatusError, error);
		}

		public void ShowWarning (string warning)
		{
			DispatchService.AssertGuiThread ();
			ShowMessage (StockIcons.StatusWarning, warning);
		}

		public void ShowMessage (string message)
		{
			ShowMessage (null, message, false);
		}

		public void ShowMessage (string message, bool isMarkup)
		{
			ShowMessage (null, message, isMarkup);
		}

		public void ShowMessage (IconId image, string message)
		{
			ShowMessage (image, message, false);
		}

		public void ShowMessage (IconId image, string message, bool isMarkup)
		{
			if (textAnimTweener.IsRunning) {
				messageQueue.Clear ();
				messageQueue.Enqueue (new Message (image, message, isMarkup));
			} else {
				ShowMessageInner (image, message, isMarkup);
			}
		}

		void ShowMessageInner (IconId image, string message, bool isMarkup)
		{
			DispatchService.AssertGuiThread ();

			LoadText (message, isMarkup);
			LoadPixbuf (image);

			textAnimTweener.Start ();

			if (currentText == lastText)
				textAnimTweener.Stop ();
			
			QueueDraw ();
		}

		void LoadText (string message, bool isMarkup)
		{
			if (string.IsNullOrEmpty(message))
				message = GettextCatalog.GetString("Welcome");
			message = message ?? "";

			lastText = currentText;
			currentText = message.Replace ("\n", " ").Trim ();

			lastTextIsMarkup = textIsMarkup;
			textIsMarkup = isMarkup;
		}

		static bool iconLoaded = false;
		void LoadPixbuf (IconId image)
		{
			// We dont need to load the same image twice
			if (currentIcon == image && iconLoaded)
				return;

			currentIcon = image;
			iconAnimation = null;

			// clean up previous running animation
			if (currentIconAnimation != null) {
				currentIconAnimation.Dispose ();
				currentIconAnimation = null;
			}

			// if we have nothing, use the default icon
			if (image == IconId.Null)
				image = "md-status-steady";

			// load image now
			if (ImageService.IsAnimation (image, Gtk.IconSize.Menu)) {
				iconAnimation = ImageService.GetAnimatedIcon (image, Gtk.IconSize.Menu);
				currentPixbuf = iconAnimation.FirstFrame;
				currentIconAnimation = iconAnimation.StartAnimation (delegate (Gdk.Pixbuf p) {
					currentPixbuf = p;
					QueueDraw ();
				});
			} else {
				currentPixbuf = ImageService.GetPixbuf (image, Gtk.IconSize.Menu);
			}

			iconLoaded = true;
		}
		#endregion


		#region Progress Monitor implementation
		public static event EventHandler ProgressBegin, ProgressEnd, ProgressPulse;
		public static event EventHandler<FractionEventArgs> ProgressFraction;
		
		public sealed class FractionEventArgs : EventArgs
		{
			public double Work { get; private set; }
			
			public FractionEventArgs (double work)
			{
				this.Work = work;
			}
		}
		
		static void OnProgressBegin (EventArgs e)
		{
			var handler = ProgressBegin;
			if (handler != null)
				handler (null, e);
		}
		
		static void OnProgressEnd (EventArgs e)
		{
			var handler = ProgressEnd;
			if (handler != null)
				handler (null, e);
		}
		
		static void OnProgressPulse (EventArgs e)
		{
			var handler = ProgressPulse;
			if (handler != null)
				handler (null, e);
		}
		
		static void OnProgressFraction (FractionEventArgs e)
		{
			var handler = ProgressFraction;
			if (handler != null)
				handler (null, e);
		}
		
		public void BeginProgress (string name)
		{
			ShowMessage (name);
			OnProgressBegin (EventArgs.Empty);
		}
		
		public void BeginProgress (IconId image, string name)
		{
			ShowMessage (image, name);
			OnProgressBegin (EventArgs.Empty);
		}
		
		public void SetProgressFraction (double work)
		{
			DispatchService.AssertGuiThread ();
			OnProgressFraction (new FractionEventArgs (work));
		}
		
		public void EndProgress ()
		{
			ShowMessage ("");
			OnProgressEnd (EventArgs.Empty);
			AutoPulse = false;
		}
		
		public void Pulse ()
		{
			DispatchService.AssertGuiThread ();
			OnProgressPulse (EventArgs.Empty);
		}
		
		uint autoPulseTimeoutId;
		public bool AutoPulse {
			get { return autoPulseTimeoutId != 0; }
			set {
				DispatchService.AssertGuiThread ();
				if (value) {
					if (autoPulseTimeoutId == 0) {
						autoPulseTimeoutId = GLib.Timeout.Add (100, delegate {
							Pulse ();
							return true;
						});
					}
				} else {
					if (autoPulseTimeoutId != 0) {
						GLib.Source.Remove (autoPulseTimeoutId);
						autoPulseTimeoutId = 0;
					}
				}
			}
		}
		#endregion
	
		internal bool IsCurrentContext (StatusBarContextImpl ctx)
		{
			return ctx == activeContext;
		}
		
		internal void Remove (StatusBarContextImpl ctx)
		{
			if (ctx == mainContext)
				return;
			
			StatusBarContextImpl oldActive = activeContext;
			contexts.Remove (ctx);
			UpdateActiveContext ();
			if (oldActive != activeContext) {
				// Removed the active context. Update the status bar.
				activeContext.Update ();
			}
		}
		
		internal void UpdateActiveContext ()
		{
			for (int n = contexts.Count - 1; n >= 0; n--) {
				StatusBarContextImpl ctx = contexts [n];
				if (ctx.StatusChanged) {
					if (ctx != activeContext) {
						activeContext = ctx;
						activeContext.Update ();
					}
					return;
				}
			}
			throw new InvalidOperationException (); // There must be at least the main context
		}
	}

	class StatusAreaSeparator: HBox
	{
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (this.GdkWindow)) {
				var alloc = Allocation;
				//alloc.Inflate (0, -2);
				ctx.Rectangle (alloc.X, alloc.Y, 1, alloc.Height);
				using (Cairo.LinearGradient gr = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Y + alloc.Height)) {
					gr.AddColorStop (0, new Cairo.Color (0, 0, 0, 0));
					gr.AddColorStop (0.5, new Cairo.Color (0, 0, 0, 0.2));
					gr.AddColorStop (1, new Cairo.Color (0, 0, 0, 0));
					ctx.Pattern = gr;
					ctx.Fill ();
				}
			}
			return true;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = 1;
		}
	}
}

