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
using Xwt.Motion;
using MonoDevelop.Ide.Fonts;
using System.Threading;

namespace MonoDevelop.Components.MainToolbar
{
	class StatusArea : EventBox, StatusBar, Xwt.Motion.IAnimatable
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

		public struct RenderArg
		{
			public Gdk.Rectangle Allocation { get; set; }
			public double        BuildAnimationProgress { get; set; }
			public double        BuildAnimationOpacity { get; set; }
			public Gdk.Rectangle ChildAllocation { get; set; }
			public Xwt.Drawing.Image CurrentPixbuf { get; set; }
			public string        CurrentText { get; set; }
			public bool          CurrentTextIsMarkup { get; set; }
			public double        ErrorAnimationProgress { get; set; }
			public double        HoverProgress { get; set; }
			public string        LastText { get; set; }
			public bool          LastTextIsMarkup { get; set; }
			public Xwt.Drawing.Image LastPixbuf { get; set; }
			public Gdk.Point     MousePosition { get; set; }
			public Pango.Context Pango { get; set; }
			public double        ProgressBarAlpha { get; set; }
			public float         ProgressBarFraction { get; set; }
			public bool          ShowProgressBar { get; set; }
			public double        TextAnimationProgress { get; set; }
		}

		StatusAreaTheme theme;
		RenderArg renderArg;

		HBox contentBox = new HBox (false, 8);

		StatusAreaSeparator statusIconSeparator;
		Gtk.Widget buildResultWidget;

		readonly HBox messageBox = new HBox ();
		internal readonly HBox statusIconBox = new HBox ();
		Alignment mainAlign;

		uint animPauseHandle;

		MouseTracker tracker;

		AnimatedIcon iconAnimation;
		IconId currentIcon;
		static Pad sourcePad;
		IDisposable currentIconAnimation;

		bool errorAnimPending;

		StatusBarContextHandler ctxHandler;
		bool progressBarVisible;

		string currentApplicationName = String.Empty;

		Queue<Message> messageQueue;

		public StatusBar MainContext {
			get { return ctxHandler.MainContext; }
		}

		public int MaxWidth { get; set; }

		void messageBoxToolTip (object o, QueryTooltipArgs e)
		{
			if (theme.IsEllipsized && (e.X < messageBox.Allocation.Width)) {
				var label = new Label ();
				if (renderArg.CurrentTextIsMarkup) {
					label.Markup = renderArg.CurrentText;
				} else {
					label.Text = renderArg.CurrentText;
				}

				label.Wrap = true;
				label.WidthRequest = messageBox.Allocation.Width;

				e.Tooltip.Custom = label;
				e.RetVal = true;
			} else {
				e.RetVal = false;
			}
		}

		public StatusArea ()
		{
			theme = new StatusAreaTheme ();
			renderArg = new RenderArg ();

			ctxHandler = new StatusBarContextHandler (this);
			VisibleWindow = false;
			NoShowAll = true;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;

			statusIconBox.BorderWidth = 0;
			statusIconBox.Spacing = 3;

			Action<bool> animateProgressBar =
				showing => this.Animate ("ProgressBarFade",
				                         val => renderArg.ProgressBarAlpha = val,
				                         renderArg.ProgressBarAlpha,
				                         showing ? 1.0f : 0.0f,
				                         easing: Easing.CubicInOut);

			ProgressBegin += delegate {
				renderArg.ShowProgressBar = true;
//				StartBuildAnimation ();
				renderArg.ProgressBarFraction = 0;
				QueueDraw ();
				animateProgressBar (true);
			};

			ProgressEnd += delegate {
				renderArg.ShowProgressBar = false;
//				StopBuildAnimation ();
				QueueDraw ();
				animateProgressBar (false);
			};

			ProgressFraction += delegate(object sender, FractionEventArgs e) {
				renderArg.ProgressBarFraction = (float)e.Work;
				QueueDraw ();
			};

			contentBox.PackStart (messageBox, true, true, 0);
			contentBox.PackEnd (statusIconBox, false, false, 0);
			contentBox.PackEnd (statusIconSeparator = new StatusAreaSeparator (), false, false, 0);
			contentBox.PackEnd (buildResultWidget = CreateBuildResultsWidget (Orientation.Horizontal), false, false, 0);

			HasTooltip = true;
			QueryTooltip += messageBoxToolTip;

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

			tracker = new MouseTracker(this);
			tracker.MouseMoved += (sender, e) => QueueDraw ();
			tracker.HoveredChanged += (sender, e) => {
				this.Animate ("Hovered",
				              x => renderArg.HoverProgress = x,
				              renderArg.HoverProgress,
				              tracker.Hovered ? 1.0f : 0.0f,
				              easing: Easing.SinInOut);
			};

			IdeApp.FocusIn += delegate {
				// If there was an error while the application didn't have the focus,
				// trigger the error animation again when it gains the focus
				if (errorAnimPending) {
					errorAnimPending = false;
					TriggerErrorAnimation ();
				}
			};
		}

		protected override void OnDestroyed ()
		{
			if (theme != null)
				theme.Dispose ();
			base.OnDestroyed ();
		}

		void IAnimatable.BatchBegin () { }
		void IAnimatable.BatchCommit () { QueueDraw (); }

		void StartBuildAnimation ()
		{
			this.Animate ("Build",
			              val => renderArg.BuildAnimationProgress = val,
			              length: 5000,
			              repeat: () => true);

			this.Animate ("BuildOpacity",
			              start: renderArg.BuildAnimationOpacity,
			              end: 1.0f,
			              callback: x => renderArg.BuildAnimationOpacity = x);
		}

		void StopBuildAnimation ()
		{
			this.Animate ("BuildOpacity",
			              x => renderArg.BuildAnimationOpacity = x,
			              renderArg.BuildAnimationOpacity,
			              0.0f,
			              finished: (val, aborted) => { if (!aborted) this.AbortAnimation ("Build"); });
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (MaxWidth > 0 && allocation.Width > MaxWidth) {
				allocation = new Gdk.Rectangle (allocation.X + (allocation.Width - MaxWidth) / 2, allocation.Y, MaxWidth, allocation.Height);
			}
			base.OnSizeAllocated (allocation);
		}

		void TriggerErrorAnimation ()
		{
/* Hack for a compiler error - csc crashes on this:
 			this.Animate (name: "statusAreaError",
			              length: 700,
			              callback: val => renderArg.ErrorAnimationProgress = val);
*/
			this.Animate ("statusAreaError",
			              val => renderArg.ErrorAnimationProgress = val,
			              length: 900);
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

			var errorIcon = ImageService.GetIcon (StockIcons.Error).WithSize (Xwt.IconSize.Small);
			var warningIcon = ImageService.GetIcon (StockIcons.Warning).WithSize (Xwt.IconSize.Small);

			var errorImage = new Xwt.ImageView (errorIcon);
			var warningImage = new Xwt.ImageView (warningIcon);

			box.PackStart (errorImage.ToGtkWidget (), false, false, 0);
			Label errors = new Gtk.Label ();
			box.PackStart (errors, false, false, 0);

			box.PackStart (warningImage.ToGtkWidget (), false, false, 0);
			Label warnings = new Gtk.Label ();
			box.PackStart (warnings, false, false, 0);
			box.NoShowAll = true;
			box.Show ();

			TaskEventHandler updateHandler = delegate {
				int ec=0, wc=0;

				foreach (TaskListEntry t in TaskService.Errors) {
					if (t.Severity == TaskSeverity.Error)
						ec++;
					else if (t.Severity == TaskSeverity.Warning)
						wc++;
				}


				using (var font = FontService.SansFont.CopyModified (MonoDevelop.Ide.Gui.Styles.FontScale11)) {
					errors.Visible = ec > 0;
					errors.ModifyFont (font);
					errors.Text = ec.ToString ();
					errorImage.Visible = ec > 0;

					warnings.Visible = wc > 0;
					warnings.ModifyFont (font);
					warnings.Text = wc.ToString ();
					warningImage.Visible = wc > 0;
				}

				ebox.Visible = ec > 0 || wc > 0;

				UpdateSeparators ();
			};

			updateHandler (null, null);

			TaskService.Errors.TasksAdded += updateHandler;
			TaskService.Errors.TasksRemoved += updateHandler;

			currentApplicationName = BrandingService.ApplicationLongName;
			BrandingService.ApplicationNameChanged += ApplicationNameChanged;
			
			box.Destroyed += delegate {
				TaskService.Errors.TasksAdded -= updateHandler;
				TaskService.Errors.TasksRemoved -= updateHandler;
				BrandingService.ApplicationNameChanged -= ApplicationNameChanged;
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

		void ApplicationNameChanged (object sender, EventArgs e)
		{
			if (renderArg.CurrentText == currentApplicationName) {
				LoadText (BrandingService.ApplicationLongName, false);
				LoadPixbuf (null);
				QueueDraw ();
			}
			currentApplicationName = BrandingService.ApplicationLongName;
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
				renderArg.Allocation            = Allocation;
				renderArg.ChildAllocation       = messageBox.Allocation;
				renderArg.MousePosition         = tracker.MousePosition;
				renderArg.Pango                 = PangoContext;

				theme.Render (context, renderArg, this);
			}
			return base.OnExposeEvent (evnt);
		}


		#region StatusBar implementation

		public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			Runtime.AssertMainThread ();
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

		public StatusBarContext CreateContext ()
		{
			return ctxHandler.CreateContext ();
		}

		public void ShowReady ()
		{
			ShowMessage ("");
			SetMessageSourcePad (null);
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
			Xwt.Drawing.Image icon;
			uint animation;
			Xwt.ImageView image;

			int astep;
			Xwt.Drawing.Image[] images;
			TooltipPopoverWindow tooltipWindow;
			bool mouseOver;
			uint tipShowTimeoutId;
			DateTime scheduledTipTime;
			const int TooltipTimeout = 350;

			public StatusIcon (StatusArea statusBar, Xwt.Drawing.Image icon)
			{
				if (!icon.HasFixedSize)
					icon = icon.WithSize (IconSize.Menu);

				this.statusBar = statusBar;
				this.icon = icon;
				box = new EventBox ();
				box.VisibleWindow = false;
				image = new Xwt.ImageView (icon);
				box.Child = image.ToGtkWidget ();
				box.Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
				box.EnterNotifyEvent += HandleEnterNotifyEvent;
				box.LeaveNotifyEvent += HandleLeaveNotifyEvent;
				box.ButtonPressEvent += (o, e) => {
					// TODO: Refactor this in Xwt as an extension method.
					var m = Xwt.ModifierKeys.None;
					if ((e.Event.State & Gdk.ModifierType.ShiftMask) != 0)
						m |= Xwt.ModifierKeys.Shift;
					if ((e.Event.State & Gdk.ModifierType.ControlMask) != 0)
						m |= Xwt.ModifierKeys.Control;
					if ((e.Event.State & Gdk.ModifierType.Mod1Mask) != 0)
						m |= Xwt.ModifierKeys.Alt;
					// TODO: Backport this one.
					if ((e.Event.State & Gdk.ModifierType.Mod2Mask) != 0)
						m |= Xwt.ModifierKeys.Command;

					Clicked?.Invoke (o, new StatusBarIconClickedEventArgs {
						Button = (Xwt.PointerButton)e.Event.Button,
						Modifiers = m,
					});
				};
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
				scheduledTipTime = DateTime.Now + TimeSpan.FromMilliseconds (TooltipTimeout);
				if (tipShowTimeoutId == 0)
					tipShowTimeoutId = GLib.Timeout.Add (TooltipTimeout, ShowTooltipEvent);
			}

			bool ShowTooltipEvent ()
			{
				tipShowTimeoutId = 0;

				if (!mouseOver)
					return false;

				int remainingMs = (int) (scheduledTipTime - DateTime.Now).TotalMilliseconds;
				if (remainingMs > 50) {
					// Still some significant time left. Re-schedule the timer
					tipShowTimeoutId = GLib.Timeout.Add ((uint) remainingMs, ShowTooltipEvent);
					return false;
				}

				if (!string.IsNullOrEmpty (tip)) {
					HideTooltip ();
					tooltipWindow = TooltipPopoverWindow.Create ();
					tooltipWindow.ShowArrow = true;
					tooltipWindow.Text = tip;
					tooltipWindow.ShowPopup (box, PopupPosition.Top);
				}
				return false;
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
					foreach (Xwt.Drawing.Image img in images) {
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

			public string Title {
				get;
				set;
			}

			public string Help {
				get;
				set;
			}

			public EventBox EventBox {
				get { return box; }
			}

			public Xwt.Drawing.Image Image {
				get { return icon; }
				set {
					icon = value;
					if (!icon.HasFixedSize)
						icon = icon.WithSize (IconSize.Menu);
					image.Image = icon;
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
					images = new Xwt.Drawing.Image [10];
					for (int n=0; n<10; n++)
						images [n] = icon.WithAlpha (((double)(9-n))/10.0);
				}
			}

			bool AnimateIcon ()
			{
				if (DateTime.Now >= alertEnd && astep == 0) {
					image.Image = icon;
					animation = 0;
					return false;
				}
				if (astep < 10)
					image.Image = images [astep];
				else
					image.Image = images [20 - astep - 1];

				astep = (astep + 1) % 20;
				return true;
			}

			public event EventHandler<StatusBarIconClickedEventArgs> Clicked;
		}

		#endregion

		#region StatusBarContextBase implementation

		public void ShowError (string error)
		{
			ShowMessage (StockIcons.StatusError, error);
		}

		public void ShowWarning (string warning)
		{
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
			if (this.AnimationIsRunning("Text") || animPauseHandle > 0) {
				messageQueue.Clear ();
				messageQueue.Enqueue (new Message (image, message, isMarkup));
			} else {
				ShowMessageInner (image, message, isMarkup);
			}
		}

		void ShowMessageInner (IconId image, string message, bool isMarkup)
		{
			Runtime.AssertMainThread ();

			if (image == StockIcons.StatusError) {
				// If the application doesn't have the focus, trigger the animation
				// again when it gains the focus
				if (!IdeApp.CommandService.ApplicationHasFocus)
					errorAnimPending = true;
				TriggerErrorAnimation ();
			}

			LoadText (message, isMarkup);
			LoadPixbuf (image);
			/* Hack for a compiler error - csc crashes on this:
			this.Animate ("Text", easing: Easing.SinInOut,
			              callback: x => renderArg.TextAnimationProgress = x,
			              finished: x => { animPauseHandle = GLib.Timeout.Add (1000, () => {
					if (messageQueue.Count > 0) {
						Message m = messageQueue.Dequeue();
						ShowMessageInner (m.Icon, m.Text, m.IsMarkup);
					}
					animPauseHandle = 0;
					return false;
				});
			});
			*/
			this.Animate ("Text",
			              x => renderArg.TextAnimationProgress = x,
			              easing: Easing.SinInOut,
			              finished: (x, b) => { animPauseHandle = GLib.Timeout.Add (1000, () => {
					if (messageQueue.Count > 0) {
						Message m = messageQueue.Dequeue();
						ShowMessageInner (m.Icon, m.Text, m.IsMarkup);
					}
					animPauseHandle = 0;
					return false;
				});
			});


			if (renderArg.CurrentText == renderArg.LastText)
				this.AbortAnimation ("Text");

			QueueDraw ();
		}

		void LoadText (string message, bool isMarkup)
		{
			if (string.IsNullOrEmpty(message))
				message = BrandingService.ApplicationLongName;
			message = message ?? "";

			renderArg.LastText = renderArg.CurrentText;
			renderArg.CurrentText = message.Replace (System.Environment.NewLine, " ").Replace ("\n", " ").Trim ();

			renderArg.LastTextIsMarkup = renderArg.CurrentTextIsMarkup;
			renderArg.CurrentTextIsMarkup = isMarkup;
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
				renderArg.CurrentPixbuf = iconAnimation.FirstFrame.WithSize (16,16);
				currentIconAnimation = iconAnimation.StartAnimation (delegate (Xwt.Drawing.Image p) {
					renderArg.CurrentPixbuf = p.WithSize (16,16);
					QueueDraw ();
				});
			} else {
				renderArg.CurrentPixbuf = ImageService.GetIcon (image).WithSize (16,16);
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
			if (!progressBarVisible) {
				progressBarVisible = true;
				OnProgressBegin (EventArgs.Empty);
			}
		}

		public void BeginProgress (IconId image, string name)
		{
			ShowMessage (image, name);
			if (!progressBarVisible) {
				progressBarVisible = true;
				OnProgressBegin (EventArgs.Empty);
			}
		}

		public void SetProgressFraction (double work)
		{
			Runtime.AssertMainThread ();
			OnProgressFraction (new FractionEventArgs (work));
		}

		public void EndProgress ()
		{
			if (!progressBarVisible)
				return;

			progressBarVisible = false;
			OnProgressEnd (EventArgs.Empty);
			AutoPulse = false;
		}

		public void Pulse ()
		{
			Runtime.AssertMainThread ();
			OnProgressPulse (EventArgs.Empty);
		}

		uint autoPulseTimeoutId;
		public bool AutoPulse {
			get { return autoPulseTimeoutId != 0; }
			set {
				Runtime.AssertMainThread ();
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

		public void SetCancellationTokenSource (CancellationTokenSource source)
		{
		}
		#endregion
	}

	class StatusAreaSeparator: HBox
	{
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (this.GdkWindow)) {
				var alloc = Allocation;
				//alloc.Inflate (0, -2);
				ctx.Rectangle (alloc.X, alloc.Y, 1, alloc.Height);

				// FIXME: VV: Remove gradient features
				using (Cairo.LinearGradient gr = new LinearGradient (alloc.X, alloc.Y, alloc.X, alloc.Y + alloc.Height)) {
					gr.AddColorStop (0, new Cairo.Color (0, 0, 0, 0));
					gr.AddColorStop (0.5, new Cairo.Color (0, 0, 0, 0.2));
					gr.AddColorStop (1, new Cairo.Color (0, 0, 0, 0));
					ctx.SetSource (gr);
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

