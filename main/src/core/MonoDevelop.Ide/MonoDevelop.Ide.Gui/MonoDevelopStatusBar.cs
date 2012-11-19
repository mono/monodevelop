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
		
		readonly Label statusLabel = new Label ();
		public readonly static HBox messageBox = new HBox ();
		public readonly static HBox statusIconBox = new HBox ();

		static Pad sourcePad;
		uint autoPulseTimeoutId;
		
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
			
			messageBox.PackStart (statusLabel, true, true, 0);

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
}
