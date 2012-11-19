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
	class MonoDevelopStatusBar : Gtk.HBox
	{
		Label modeLabel;
		Label cursorLabel;
		MiniButton feedbackButton;
		Gtk.Widget resizeGrip = new Gtk.Label ("");

		const int ResizeGripWidth = 14;

		HBox statusBox;

		readonly Label statusLabel = new Label ();
		public readonly static HBox messageBox = new HBox ();

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
			BorderWidth = 0;
			Spacing = 0;
			HasResizeGrip = true;

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

			// Resize grip

			resizeGrip.WidthRequest = ResizeGripWidth;
			resizeGrip.HeightRequest = 0;
			mainBox.PackStart (resizeGrip, false, false, 0);

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
			
			this.ShowAll ();

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

		bool hasResizeGrip;
		public bool HasResizeGrip {
			get { return hasResizeGrip; }
			set { hasResizeGrip = value; resizeGrip.Visible = hasResizeGrip; }
		}

		Gdk.Rectangle GetGripRect ()
		{
			Gdk.Rectangle rect = new Gdk.Rectangle (0, 0, ResizeGripWidth, Allocation.Height);
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
