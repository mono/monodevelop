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

namespace MonoDevelop.Ide
{
	class MonoDevelopStatusBar : Gtk.Statusbar, StatusBar, IShadedWidget
	{
		ProgressBar progressBar = new ProgressBar ();
		Frame textStatusBarPanel = new Frame ();
		
		Label statusLabel;
		Label modeLabel;
		Label cursorLabel;
		
		HBox statusBox;
		HBox messageBox;
		Image currentStatusImage;
		EventBox eventBox;
		List<StatusBarContextImpl> contexts = new List<StatusBarContextImpl> ();
		MainStatusBarContextImpl mainContext;
		StatusBarContextImpl activeContext;
		Pad sourcePad;
		uint autoPulseTimeoutId;
		
		public StatusBar MainContext {
			get { return mainContext; }
		}
		
		internal MonoDevelopStatusBar()
		{
			mainContext = new MainStatusBarContextImpl (this);
			activeContext = mainContext;
			contexts.Add (mainContext);
			
			Frame originalFrame = (Frame)Children[0];
//			originalFrame.WidthRequest = 8;
//			originalFrame.Shadow = ShadowType.In;
//			originalFrame.BorderWidth = 0;
			
			BorderWidth = 0;
			
			DefaultWorkbench wb = (DefaultWorkbench) IdeApp.Workbench.RootWindow;
			wb.DockFrame.ShadedContainer.Add (this);
			Gtk.Widget dockBar = wb.DockFrame.ExtractDockBar (PositionType.Bottom);
			dockBar.NoShowAll = true;
			PackStart (dockBar, false, false, 0);
			
			progressBar = new ProgressBar ();
			progressBar.PulseStep = 0.1;
			progressBar.SizeRequest ();
			progressBar.HeightRequest = 1;
			
			statusBox = new HBox (false, 0);
			statusBox.BorderWidth = 0;
			
			statusLabel = new Label ();
			statusLabel.SetAlignment (0, 0.5f);
			statusLabel.Wrap = false;
			int w, h;
			Gtk.Icon.SizeLookup (IconSize.Menu, out w, out h);
			statusLabel.HeightRequest = h;
			statusLabel.SetPadding (0, 0);
			
			EventBox eventMessageBox = new EventBox ();
			messageBox = new HBox ();
			messageBox.PackStart (progressBar, false, false, 0);
			messageBox.PackStart (statusLabel, true, true, 0);
			eventMessageBox.Add (messageBox);
			statusBox.PackStart (eventMessageBox, true, true, 0);
			eventMessageBox.ButtonPressEvent += HandleEventMessageBoxButtonPressEvent;
			
			textStatusBarPanel.BorderWidth = 0;
			textStatusBarPanel.ShadowType = ShadowType.None;
			textStatusBarPanel.Add (statusBox);
			Label fillerLabel = new Label ();
			fillerLabel.WidthRequest = 8;
			statusBox.PackEnd (fillerLabel, false, false, 0);
			
			modeLabel = new Label (" ");
			statusBox.PackEnd (modeLabel, false, false, 8);
			
			cursorLabel = new Label (" ");
			statusBox.PackEnd (cursorLabel, false, false, 0);
			
			eventBox = new EventBox ();
			eventBox.BorderWidth = 0;
			statusBox.PackEnd (eventBox, false, false, 4);
			
			this.PackStart (textStatusBarPanel, true, true, 0);
			
			ShowReady ();
			Gtk.Box.BoxChild boxChild = (Gtk.Box.BoxChild)this[textStatusBarPanel];
			boxChild.Position = 0;
			boxChild.Expand = boxChild.Fill = true;
			
	//		boxChild = (Gtk.Box.BoxChild)this[originalFrame];
	//		boxChild.Padding = 0;
	//		boxChild.Expand = boxChild.Fill = false;
			
			this.progressBar.Fraction = 0.0;
			this.ShowAll ();
			eventBox.HideAll ();
			
			originalFrame.HideAll ();
			progressBar.Visible = false;
			
			StatusBarContext completionStatus = null;
			
			// todo: Move this to the CompletionWindowManager when it's possible.
			CompletionWindowManager.WindowShown += delegate {
				CompletionListWindow wnd = CompletionWindowManager.Wnd;
				if (wnd != null && wnd.List != null && wnd.List.CategoryCount > 1) {
					if (completionStatus == null)
						completionStatus = CreateContext ();
					completionStatus.ShowMessage (string.Format (GettextCatalog.GetString ("To toggle categorized completion mode press {0}."), IdeApp.CommandService.GetCommandInfo (Commands.TextEditorCommands.ShowCompletionWindow, null).AccelKey));
				}
			};
			
			CompletionWindowManager.WindowClosed += delegate {
				if (completionStatus != null) {
					completionStatus.Dispose ();
					completionStatus = null;
				}
			};
		}

		void HandleEventMessageBoxButtonPressEvent (object o, ButtonPressEventArgs args)
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
		
		public StatusBarContext CreateContext ()
		{
			StatusBarContextImpl ctx = new StatusBarContextImpl (this);
			contexts.Add (ctx);
			return ctx;
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
			ShowMessage (new Image (MonoDevelop.Ide.Gui.Stock.Error, IconSize.Menu), error);
		}
		
		public void ShowWarning (string warning)
		{
			DispatchService.AssertGuiThread ();
			ShowMessage (new Gtk.Image (MonoDevelop.Ide.Gui.Stock.Warning, IconSize.Menu), warning);
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
			
			Gtk.Image image = new Gtk.Image (pixbuf);
			image.SetPadding (0, 0);
			if (eventBox.Child != null)
				eventBox.Remove (eventBox.Child);
			eventBox.Child = image;
			
			eventBox.ShowAll ();
			return new StatusIcon (this, eventBox, pixbuf);
		}
		
		void HideStatusIcon (StatusIcon icon)
		{
			Widget child = icon.EventBox.Child; 
			if (child != null) {
				icon.EventBox.Remove (child);
				child.Destroy ();
			}
			eventBox.HideAll ();
		}
		
		#region Progress Monitor implementation
		public void BeginProgress (string name)
		{
			ShowMessage (name);
			this.progressBar.Visible = true;
		}
		
		public void BeginProgress (Image image, string name)
		{
			ShowMessage (image, name);
			this.progressBar.Visible = true;
		}

		public void SetProgressFraction (double work)
		{
			DispatchService.AssertGuiThread ();
			this.progressBar.Fraction = work;
		}
		
		public void EndProgress ()
		{
			ShowMessage ("");
			this.progressBar.Fraction = 0.0;
			this.progressBar.Visible = false;
			AutoPulse = false;
		}

		public void Pulse ()
		{
			DispatchService.AssertGuiThread ();
			this.progressBar.Visible = true;
			this.progressBar.Pulse ();
		}

		public bool AutoPulse {
			get { return autoPulseTimeoutId != 0; }
			set {
				DispatchService.AssertGuiThread ();
				if (value) {
					this.progressBar.Visible = true;
					if (autoPulseTimeoutId == 0) {
						autoPulseTimeoutId = GLib.Timeout.Add (100, delegate {
							this.progressBar.Pulse ();
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
			
			int astep;
			Gtk.Image[] images;
			
			public StatusIcon (MonoDevelopStatusBar statusBar, EventBox box, Gdk.Pixbuf icon)
			{
				this.statusBar = statusBar;
				this.box = box;
				this.icon = icon;
			}
			
			public void Dispose ()
			{
				statusBar.HideStatusIcon (this);
				if (images != null) {
					foreach (Gtk.Image img in images) {
						img.Dispose ();
					}
				}
			}
			
			public string ToolTip {
				get { return tip; }
				set {
					box.TooltipText = tip = value;
				}
			}
			
			public EventBox EventBox {
				get { return box; }
			}
			
			public Gdk.Pixbuf Image {
				get { return icon; }
				set {
					icon = value;
					Gtk.Image i = new Gtk.Image (icon);
					i.SetPadding (0, 0);
					box.Child = i;
				}
			}
			
			public void SetAlertMode (int seconds)
			{
				astep = 0;
				alertEnd = DateTime.Now.AddSeconds (seconds);
				
				if (images == null)
					GLib.Timeout.Add (60, new GLib.TimeoutHandler (AnimateIcon));
				
				images = new Gtk.Image [10];
				for (int n=0; n<10; n++) {
					images [n] = new Image (ImageService.MakeTransparent (icon, ((double)(9-n))/10.0));
					images [n].SetPadding (0, 0);
					images [n].Show ();
				}
			}
			
			bool AnimateIcon ()
			{
				box.Remove (box.Child);
				
				if (DateTime.Now >= alertEnd && astep == 0) {
					Gtk.Image i = new Gtk.Image (icon);
					i.SetPadding (0, 0);
					box.Child = i;
					images = null;
					box.Child.Show ();
					return false;
				}
				if (astep < 10)
					box.Child = images [astep];
				else
					box.Child = images [20 - astep - 1];
					
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
				DefaultWorkbench wb = (DefaultWorkbench) IdeApp.Workbench.RootWindow;
				wb.DockFrame.ShadedContainer.DrawBackground (this, GetGripRect ());
				Gdk.Rectangle rect = GetGripRect ();
				int w = rect.Width - Style.Xthickness;
				int h = Allocation.Height - Style.YThickness;
				if (h < 18 - Style.YThickness) h = 18 - Style.YThickness;
				Gdk.WindowEdge edge = Direction == TextDirection.Ltr ? Gdk.WindowEdge.SouthEast : Gdk.WindowEdge.SouthWest;
				Gtk.Style.PaintResizeGrip (Style, GdkWindow, State, evnt.Area, this, "statusbar", edge, rect.X, rect.Y, w, h);
			}
			return ret;
		}
		
		public void NotifyShadedAreasChanged ()
		{
			if (AreasChanged != null)
				AreasChanged (this, EventArgs.Empty);
		}
		
		public IEnumerable<Gdk.Rectangle> GetShadedAreas ()
		{
			if (HasResizeGrip)
				yield return GetGripRect ();
		}
		
		public event EventHandler AreasChanged;
	}
	
	/// <summary>
	/// The MonoDevelop status bar.
	/// </summary>
	public interface StatusBar: StatusBarContextBase
	{
		/// <summary>
		/// Show caret state information
		/// </summary>
		void ShowCaretState (int line, int column, int selectedChars, bool isInInsertMode);
		
		/// <summary>
		/// Hides the caret state information
		/// </summary>
		void ClearCaretState ();
		
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
		void ShowMessage (Image image, string message);
		
		/// <summary>
		/// Shows a progress bar, with the provided label next to it
		/// </summary>
		void BeginProgress (string name);
		
		/// <summary>
		/// Shows a progress bar, with the provided label and icon next to it
		/// </summary>
		void BeginProgress (Image image, string name);
		
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
		Image image;
		string message;
		bool isMarkup;
		double progressFraction;
		bool showProgress;
		Pad sourcePad;
		bool autoPulse;
		protected MonoDevelopStatusBar statusBar;
		
		internal bool StatusChanged { get; set; }
		
		internal StatusBarContextImpl (MonoDevelopStatusBar statusBar)
		{
			this.statusBar = statusBar;
		}
		
		public void Dispose ()
		{
			statusBar.Remove (this);
		}
		
		public void ShowError (string error)
		{
			ShowMessage (new Image (MonoDevelop.Ide.Gui.Stock.Error, IconSize.Menu), error);
		}
		
		public void ShowWarning (string warning)
		{
			ShowMessage (new Gtk.Image (MonoDevelop.Ide.Gui.Stock.Warning, IconSize.Menu), warning);
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
		
		bool InitialSetup ()
		{
			if (!StatusChanged) {
				StatusChanged = true;
				statusBar.UpdateActiveContext ();
				return true;
			} else
				return false;
		}
		
		public void ShowMessage (Image image, string message, bool isMarkup)
		{
			this.image = image;
			this.message = message;
			this.isMarkup = isMarkup;
			if (InitialSetup ())
				return;
			if (statusBar.IsCurrentContext (this)) {
				OnMessageChanged ();
				statusBar.ShowMessage (image, message, isMarkup);
				statusBar.SetMessageSourcePad (sourcePad);
			}
		}
		
		public void BeginProgress (string name)
		{
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
			}
		}
		
		public void BeginProgress (Image image, string name)
		{
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
			message = string.Empty;
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
			} else {
				statusBar.EndProgress ();
				statusBar.ShowMessage (image, message, isMarkup);
				statusBar.SetMessageSourcePad (sourcePad);
			}
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
		public MainStatusBarContextImpl (MonoDevelopStatusBar statusBar): base (statusBar)
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
				statusBar.NotifyShadedAreasChanged ();
			}
		}
		
	}
}
