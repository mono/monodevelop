//
// MonoDevelopStatusBar.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Components.MainToolbar;

using StockIcons = MonoDevelop.Ide.Gui.Stock;

namespace MonoDevelop.Ide
{
	class MonoDevelopStatusBar : Gtk.Statusbar
	{
		Label modeLabel;
		Label cursorLabel;
		MiniButton feedbackButton;
		
		HBox statusBox;
		Image currentStatusImage;
		
		readonly StyledProgressBar progressBar = new StyledProgressBar ();
		readonly Label statusLabel = new Label ();
		public readonly static HBox messageBox = new HBox ();
		public readonly static HBox statusIconBox = new HBox ();

		
		List<StatusBarContextImpl> contexts = new List<StatusBarContextImpl> ();
		MainStatusBarContextImpl mainContext;
		StatusBarContextImpl activeContext;
		static Pad sourcePad;
		uint autoPulseTimeoutId;
		
		public StatusBar MainContext {
			get { return mainContext; }
		}
		
		/// <summary>
		/// For small size changes the caret label is grow only. That ensures that normal caret movement doesn't
		/// update the whole status bar all the time. But for big jumps in size a resize is done.
		/// </summary>
		class CaretStatusLabel : Gtk.Label
		{
			public CaretStatusLabel (string label): base (label)
			{
			}

			protected override void OnSizeRequested (ref Requisition requisition)
			{
				base.OnSizeRequested (ref requisition);
				const int upperBound = 20;
				if (Allocation.Width > 0 && Math.Abs (Allocation.Width - requisition.Width) < upperBound)
					requisition.Width = Math.Max (Allocation.Width, requisition.Width);
			}
		}

		internal MonoDevelopStatusBar ()
		{
	/*		mainContext = new MainStatusBarContextImpl (this);
			activeContext = mainContext;
			contexts.Add (mainContext);*/
			
			Frame originalFrame = (Frame)Children [0];
			originalFrame.Hide ();
			originalFrame.NoShowAll = true;

			BorderWidth = 0;
			Spacing = 0;

			HeaderBox hb = new HeaderBox (1, 0, 0, 0);
			hb.BorderColor = Styles.DockSeparatorColor;
			var mainBox = new HBox ();
			mainBox.PackStart (new Label (""), true, true, 0);
			hb.Add (mainBox);
			hb.ShowAll ();
			PackStart (hb, true, true, 0);
			
			// Feedback button
			
			if (FeedbackService.Enabled) {
				CustomFrame fr = new CustomFrame (0, 0, 1, 1);
				Gdk.Pixbuf px = Gdk.Pixbuf.LoadFromResource ("balloon.png");
				HBox b = new HBox (false, 3);
				b.PackStart (new Gtk.Image (px));
				b.PackStart (new Gtk.Label ("Feedback"));
				Gtk.Alignment al = new Gtk.Alignment (0f, 0f, 1f, 1f);
				al.RightPadding = 5;
				al.LeftPadding = 3;
				al.Add (b);
				feedbackButton = new MiniButton (al);
				//feedbackButton.BackroundColor = new Gdk.Color (200, 200, 255);
				fr.Add (feedbackButton);
				mainBox.PackStart (fr, false, false, 0);
				feedbackButton.Clicked += HandleFeedbackButtonClicked;
				feedbackButton.ButtonPressEvent += HandleFeedbackButtonButtonPressEvent;
				;
				feedbackButton.ClickOnRelease = true;
				FeedbackService.FeedbackPositionGetter = delegate {
					int x, y;
					feedbackButton.GdkWindow.GetOrigin (out x, out y);
					x += feedbackButton.Allocation.Width;
					y -= 6;
					return new Gdk.Point (x, y);
				};
			}
			
			// Dock area
			
			DefaultWorkbench wb = (DefaultWorkbench)IdeApp.Workbench.RootWindow;
			var dockBar = wb.DockFrame.ExtractDockBar (PositionType.Bottom);
			dockBar.AlignToEnd = true;
			dockBar.ShowBorder = false;
			dockBar.NoShowAll = true;
			mainBox.PackStart (dockBar, false, false, 0);
			
			// Status panels
			

			statusBox = new HBox (false, 0);
			statusBox.BorderWidth = 0;
			
			statusLabel.SetAlignment (0, 0.5f);
			statusLabel.Wrap = false;
			int w, h;
			Gtk.Icon.SizeLookup (IconSize.Menu, out w, out h);
			statusLabel.HeightRequest = h;
			statusLabel.SetPadding (0, 0);
			statusLabel.ShowAll ();
			
//			EventBox eventMessageBox = new EventBox ();
			messageBox.PackEnd (progressBar, false, false, 0);
			messageBox.PackStart (statusLabel, true, true, 0);
//			eventMessageBox.Add (messageBox);
//			statusBox.PackStart (eventMessageBox, true, true, 0);
//			eventMessageBox.ButtonPressEvent += HandleEventMessageBoxButtonPressEvent;
			
			var eventCaretBox = new EventBox ();
			var caretStatusBox = new HBox ();
			modeLabel = new Label (" ");
			caretStatusBox.PackEnd (modeLabel, false, false, 8);
			
			cursorLabel = new CaretStatusLabel (" ");
			caretStatusBox.PackEnd (cursorLabel, false, false, 0);
			
			caretStatusBox.GetSizeRequest (out w, out h);
			caretStatusBox.WidthRequest = w;
			caretStatusBox.HeightRequest = h;
			eventCaretBox.Add (caretStatusBox);
			statusBox.PackEnd (eventCaretBox, false, false, 0);
			

			statusIconBox.BorderWidth = 0;
			statusIconBox.Spacing = 3;

			ShowReady ();

			this.ShowAll ();
			statusIconBox.HideAll ();
			
			originalFrame.HideAll ();


//			// todo: Move this to the CompletionWindowManager when it's possible.
//			StatusBarContext completionStatus = null;
//			CompletionWindowManager.WindowShown += delegate {
//				CompletionListWindow wnd = CompletionWindowManager.Wnd;
//				if (wnd != null && wnd.List != null && wnd.List.CategoryCount > 1) {
//					if (completionStatus == null)
//						completionStatus = CreateContext ();
//					completionStatus.ShowMessage (string.Format (GettextCatalog.GetString ("To toggle categorized completion mode press {0}."), IdeApp.CommandService.GetCommandInfo (Commands.TextEditorCommands.ShowCompletionWindow).AccelKey));
//				}
//			};
//			
//			CompletionWindowManager.WindowClosed += delegate {
//				if (completionStatus != null) {
//					completionStatus.Dispose ();
//					completionStatus = null;
//				}
//			};
//
//			MonoDevelopStatusBar.ProgressBegin += delegate {
//				progressBar.ShowProgress = true;
//				progressBar.Visible = true;
//				progressBar.Fraction = 0;
//			};
//			
//			MonoDevelopStatusBar.ProgressEnd += delegate {
//				progressBar.ShowProgress = false;
//				progressBar.Visible = false;
//				progressBar.Fraction = 0;
//			};
//			
//			MonoDevelopStatusBar.ProgressFraction += delegate(object sender, MonoDevelopStatusBar.FractionEventArgs e) {
//				progressBar.Fraction = e.Work;
//				QueueDraw ();
//			};
		}
		
		[System.Runtime.InteropServices.DllImport ("libc")]
		static extern void abort ();
		
		static readonly bool FeedbackButtonThrowsException = Environment.GetEnvironmentVariable ("MONODEVELOP_TEST_CRASH_REPORTING") != null;
		void HandleFeedbackButtonButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			if (FeedbackService.IsFeedbackWindowVisible)
				ignoreFeedbackButtonClick = true;

			if (FeedbackButtonThrowsException) {
				// Control == hard crash
				if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
					abort ();
				}
				//Alt = terminating exception
				var ex = new Exception ("Feedback Button is throwing an exception", new Exception (Environment.StackTrace));
				if ((args.Event.State & Gdk.ModifierType.Mod1Mask) != 0) {
					throw ex;
				}
				// None: Nonterminating exception
				GLib.ExceptionManager.RaiseUnhandledException (new Exception ("Feedback Button is throwing an exception", new Exception (Environment.StackTrace)), false);
				ignoreFeedbackButtonClick = true;
			}
		}

		bool ignoreFeedbackButtonClick;
		void HandleFeedbackButtonClicked (object sender, EventArgs e)
		{
			if (!ignoreFeedbackButtonClick)
				FeedbackService.ShowFeedbackWindow ();
			ignoreFeedbackButtonClick = false;
		}

		internal static void HandleEventMessageBoxButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			if (sourcePad != null)
				sourcePad.BringToFront (true);
		}
		
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
		
	/*	public StatusBarContext CreateContext ()
		{
	/		StatusBarContextImpl ctx = new StatusBarContextImpl (this);
			contexts.Add (ctx);
			return ctx;
		}*/
		
		public void ShowCaretState (int line, int column, int selectedChars, bool isInInsertMode)
		{
			DispatchService.AssertGuiThread ();
			string cursorText = selectedChars > 0 ? String.Format ("{0,3} : {1,-3} - {2}", line, column, selectedChars) : String.Format ("{0,3} : {1,-3}", line, column);
			if (cursorLabel.Text != cursorText)
				cursorLabel.Text = cursorText;
			
			string modeStatusText = isInInsertMode ? GettextCatalog.GetString ("INS") : GettextCatalog.GetString ("OVR");
			if (modeLabel.Text != modeStatusText)
				modeLabel.Text = modeStatusText;
		}
		
		public void ClearCaretState ()
		{
			if (cursorLabel.Text != "")
				cursorLabel.Text = "";
			if (modeLabel.Text != "")
				modeLabel.Text = "";
		}
		
		public void ShowReady ()
		{
			ShowMessage (GettextCatalog.GetString ("Ready"));
		}
		
		public void ShowError (string error)
		{
			ShowMessage (new Image (StockIcons.StatusError, IconSize.Menu), error);
		}
		
		public void ShowWarning (string warning)
		{
			DispatchService.AssertGuiThread ();
			ShowMessage (new Gtk.Image (StockIcons.StatusWarning, IconSize.Menu), warning);
		}
		
		public void ShowMessage (string message)
		{
			ShowMessage (null, message, false);
		}
		
		public void ShowMessage (string message, bool isMarkup)
		{
			ShowMessage (null, message, isMarkup);
		}
		
		public void ShowMessage (Image image, string message)
		{
			ShowMessage (image, message, false);
		}
		string lastText = null;
		public void ShowMessage (Image image, string message, bool isMarkup)
		{
			if (message == lastText)
				return;
			sourcePad = null;
			lastText = message;
			DispatchService.AssertGuiThread ();
			if (currentStatusImage != image) {
				if (currentStatusImage != null) 
					messageBox.Remove (currentStatusImage);
				currentStatusImage = image;
				if (image != null) {
					image.SetPadding (0, 0);
					messageBox.PackStart (image, false, false, 3);
					messageBox.ReorderChild (image, 1);
					image.Show ();
				}
			}
			
			string txt = !String.IsNullOrEmpty (message) ? " " + message.Replace ("\n", " ") : "";
			if (isMarkup) {
				statusLabel.Markup = txt;
			} else {
				statusLabel.Text = txt;
			}
		}
		
		public void SetMessageSourcePad (Pad pad)
		{
			sourcePad = pad;
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
		
		public void BeginProgress (Image image, string name)
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
		
		public class StatusIcon : StatusBarIcon
		{
			MonoDevelopStatusBar statusBar;
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

			public StatusIcon (MonoDevelopStatusBar statusBar, Gdk.Pixbuf icon)
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
		
		Gdk.Rectangle GetGripRect ()
		{
			Gdk.Rectangle rect = new Gdk.Rectangle (0, 0, 18, Allocation.Height);
			if (rect.Width > Allocation.Width)
				rect.Width = Allocation.Width;
			rect.Y = Allocation.Y + Allocation.Height - rect.Height;
			if (Direction == TextDirection.Ltr)
				rect.X = Allocation.X + Allocation.Width - rect.Width;
			else
				rect.X = Allocation.X + Style.XThickness;
			return rect;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			bool ret = base.OnExposeEvent (evnt);
			if (HasResizeGrip) {
				Gdk.Rectangle rect = GetGripRect ();
				int w = rect.Width - Style.Xthickness;
				int h = Allocation.Height - Style.YThickness;
				if (h < 18 - Style.YThickness) h = 18 - Style.YThickness;
				Gdk.WindowEdge edge = Direction == TextDirection.Ltr ? Gdk.WindowEdge.SouthEast : Gdk.WindowEdge.SouthWest;
				Gtk.Style.PaintResizeGrip (Style, GdkWindow, State, evnt.Area, this, "statusbar", edge, rect.X, rect.Y, w, h);
			}
			return ret;
		}
	}
	
	/// <summary>
	/// The MonoDevelop status bar.
	/// </summary>
	public interface StatusBar: StatusBarContextBase
	{
		StatusBar MainContext { get; }
		/// <summary>
		/// Shows a status icon in the toolbar. The icon can be removed by disposing
		/// the StatusBarIcon instance.
		/// </summary>
		StatusBarIcon ShowStatusIcon (Gdk.Pixbuf pixbuf);
		
		/// <summary>
		/// Creates a status bar context. The returned context can be used to show status information
		/// which will be cleared when the context is disposed. When several contexts are created,
		/// the status bar will show the status of the latest created context.
		/// </summary>
		StatusBarContext CreateContext ();
		
		// Clears the status bar information
		void ShowReady ();
		
		/// <summary>
		/// Sets a pad which has detailed information about the status message. When clicking on the
		/// status bar, this pad will be activated. This source pad is reset at every ShowMessage call.
		/// </summary>
		void SetMessageSourcePad (Pad pad);
		
		/// <summary>
		/// When set to true, the resize grip is shown
		/// </summary>
		bool HasResizeGrip { get; set; }
	}
	
	public interface StatusBarContextBase: IDisposable
	{
		/// <summary>
		/// Shows a message with an error icon
		/// </summary>
		void ShowError (string error);
		
		/// <summary>
		/// Shows a message with a warning icon
		/// </summary>
		void ShowWarning (string warning);
		
		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		void ShowMessage (string message);
		
		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		void ShowMessage (string message, bool isMarkup);
		
		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		void ShowMessage (IconId image, string message);
		
		/// <summary>
		/// Shows a progress bar, with the provided label next to it
		/// </summary>
		void BeginProgress (string name);
		
		/// <summary>
		/// Shows a progress bar, with the provided label and icon next to it
		/// </summary>
		void BeginProgress (IconId image, string name);
		
		/// <summary>
		/// Sets the progress fraction. It can only be used after calling BeginProgress.
		/// </summary>
		void SetProgressFraction (double work);

		/// <summary>
		/// Hides the progress bar shown with BeginProgress
		/// </summary>
		void EndProgress ();
		
		/// <summary>
		/// Pulses the progress bar shown with BeginProgress
		/// </summary>
		void Pulse ();
		
		/// <summary>
		/// When set, the status bar progress will be automatically pulsed at short intervals
		/// </summary>
		bool AutoPulse { get; set; }
	}
	
	public interface StatusBarContext: StatusBarContextBase
	{
		Pad StatusSourcePad { get; set; }
	}
	
	public interface StatusBarIcon : IDisposable
	{
		/// <summary>
		/// Tooltip of the status icon
		/// </summary>
		string ToolTip { get; set; }
		
		/// <summary>
		/// Event box which can be used to subscribe mouse events on the icon
		/// </summary>
		EventBox EventBox { get; }
		
		/// <summary>
		/// The icon
		/// </summary>
		Gdk.Pixbuf Image { get; set; }
		
		/// <summary>
		/// Sets alert mode. The icon will flash for the provided number of seconds.
		/// </summary>
		void SetAlertMode (int seconds);
	}
	
	class StatusBarContextImpl: StatusBarContext
	{
		IconId image;
		string message;
		bool isMarkup;
		double progressFraction;
		bool showProgress;
		Pad sourcePad;
		bool autoPulse;
		protected StatusArea statusBar;

		// The last time any status bar context changed the status area message
		static DateTime globalLastChangeTime;
		static bool lastMessageIsTransient;

		// The last time this context changed the status area message
		DateTime lastChangeTime;
		bool messageShownAfterProgress;

		internal bool StatusChanged { get; set; }

		internal StatusBarContextImpl (StatusArea statusBar)
		{
			this.statusBar = statusBar;
		}
		
		public void Dispose ()
		{
			statusBar.Remove (this);
		}
		
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
		
		bool InitialSetup ()
		{
			if (!StatusChanged) {
				StatusChanged = true;
				statusBar.UpdateActiveContext ();
				return true;
			} else
				return false;
		}
		
		public void ShowMessage (IconId image, string message, bool isMarkup)
		{
			if (!showProgress)
				messageShownAfterProgress = true;
			this.image = image;
			this.message = message;
			this.isMarkup = isMarkup;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this)) {
				OnMessageChanged ();
				globalLastChangeTime = DateTime.Now;
				statusBar.ShowMessage (image, message, isMarkup);
				statusBar.SetMessageSourcePad (sourcePad);
				if (showProgress)
					lastMessageIsTransient = true;
				else {
					lastMessageIsTransient = false;
					ResetMessage (); // Once the message is shown, don't show it again
				}
			} else
				lastChangeTime = DateTime.Now;
		}
		
		public void BeginProgress (string name)
		{
			messageShownAfterProgress = false;
			image = null;
			isMarkup = false;
			progressFraction = 0;
			message = name;
			showProgress = true;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this)) {
				OnMessageChanged ();
				statusBar.BeginProgress (name);
				statusBar.SetMessageSourcePad (sourcePad);
				lastMessageIsTransient = true;
			}
		}
		
		public void BeginProgress (IconId image, string name)
		{
			messageShownAfterProgress = false;
			this.image = image;
			isMarkup = false;
			progressFraction = 0;
			message = name;
			showProgress = true;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this)) {
				OnMessageChanged ();
				statusBar.BeginProgress (name);
				statusBar.SetMessageSourcePad (sourcePad);
				lastMessageIsTransient = true;
			}
		}
		
		public void SetProgressFraction (double work)
		{
			progressFraction = work;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this))
				statusBar.SetProgressFraction (work);
		}
		
		public void EndProgress ()
		{
			showProgress = false;
			ResetMessage ();
			progressFraction = 0;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this))
				statusBar.EndProgress ();
		}
		
		public void Pulse ()
		{
			showProgress = true;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this))
				statusBar.Pulse ();
		}
		
		public bool AutoPulse {
			get {
				return autoPulse;
			}
			set {
				if (value)
					showProgress = true;
				autoPulse = value;
				if (InitialSetup ())
					return;
				if (statusBar.IsCurrentContext (this))
					statusBar.AutoPulse = value;
			}
		}
		
		
		internal void Update ()
		{
			if (showProgress) {
				statusBar.BeginProgress (image, message);
				statusBar.SetProgressFraction (progressFraction);
				statusBar.AutoPulse = autoPulse;
				statusBar.SetMessageSourcePad (sourcePad);
				lastMessageIsTransient = true;
			} else {
				statusBar.EndProgress ();
				if (globalLastChangeTime < lastChangeTime && messageShownAfterProgress) {
					globalLastChangeTime = lastChangeTime;
					statusBar.ShowMessage (image, message, isMarkup);
					statusBar.SetMessageSourcePad (sourcePad);
					ResetMessage (); // Once the message is shown, don't show it again
					lastMessageIsTransient = !messageShownAfterProgress;
				} else if (lastMessageIsTransient)
					statusBar.ShowReady ();
			}
		}

		void ResetMessage ()
		{
			image = IconId.Null;
			message = string.Empty;
			sourcePad = null;
		}
		
		public Pad StatusSourcePad {
			get {
				return sourcePad;
			}
			set {
				sourcePad = value;
			}
		}
		
		protected virtual void OnMessageChanged ()
		{
		}
	}
	
	class MainStatusBarContextImpl: StatusBarContextImpl, StatusBar
	{
		public StatusBar MainContext {
			get {
				return this;
			}
		}
		public MainStatusBarContextImpl (StatusArea statusBar): base (statusBar)
		{
			StatusChanged = true;
		}
		
		public void ShowCaretState (int line, int column, int selectedChars, bool isInInsertMode)
		{
			statusBar.ShowCaretState (line, column, selectedChars, isInInsertMode);
		}
		
		public void ClearCaretState ()
		{
			statusBar.ClearCaretState ();
		}
		
		public StatusBarIcon ShowStatusIcon (Gdk.Pixbuf pixbuf)
		{
			return statusBar.ShowStatusIcon (pixbuf);
		}
		
		public StatusBarContext CreateContext ()
		{
			return statusBar.CreateContext ();
		}
		
		public void ShowReady ()
		{
			statusBar.ShowReady ();
		}
		
		public void SetMessageSourcePad (Pad pad)
		{
			StatusSourcePad = pad;
			if (statusBar.IsCurrentContext (this))
				statusBar.SetMessageSourcePad (pad);
		}
		
		protected override void OnMessageChanged ()
		{
			StatusSourcePad = null;
		}
		
		public bool HasResizeGrip {
			get {
				return statusBar.HasResizeGrip;
			}
			set {
				statusBar.HasResizeGrip = value;
			}
		}
		
	}
}
