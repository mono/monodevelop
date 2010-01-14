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
using MonoDevelop.Core.Gui;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Jobs;

namespace MonoDevelop.Ide
{
	public class MonoDevelopStatusBar : Gtk.Statusbar
	{
		ProgressBar progressBar = new ProgressBar ();
		HBox textStatusBarPanel;
		JobBar jobBar = new JobBar ();
		ErrorsStatusPanel errorsStatusPanel;
		
		Label statusLabel;
		Label cursorLabel;
		
		HBox statusBox;
		Image currentStatusImage;
		EventBox eventBox;
		PopupStatusBar currentPopup;
		ExpandedBox expandedBox;
		Widget expandedWidget;
		EventBox mainBox;
		uint popupAnimation, cursorChecker;
		ExpandableStatusBarPanel currentExpandedPanel;
		Gtk.Widget currentExpandedMainWidget;
		int expandCenter;
		
		internal MonoDevelopStatusBar()
		{
			Events |= Gdk.EventMask.LeaveNotifyMask;
			
			Frame originalFrame = (Frame)Children[0];
//			originalFrame.WidthRequest = 8;
//			originalFrame.Shadow = ShadowType.In;
//			originalFrame.BorderWidth = 0;
			
			DefaultWorkbench wb = (DefaultWorkbench) IdeApp.Workbench.RootWindow;
			Gtk.Widget dockBar = wb.WorkbenchLayout.DockFrame.ExtractDockBar (PositionType.Bottom);
			PackStart (dockBar, false, false, 0);

			mainBox = new EventBox ();
			mainBox.Show ();
			PackStart (mainBox, true, true, 0);
			mainBox.Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.PointerMotionMask;
			BorderWidth = 0;
			
			progressBar = new ProgressBar ();
			progressBar.PulseStep = 0.3;
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
			
			statusBox.PackStart (progressBar, false, false, 0);
			statusBox.PackStart (statusLabel, false, false, 0);
			
			jobBar = new JobBar ();
			jobBar.ShowAll ();
			statusBox.PackStart (jobBar, true, true, 0);
			
			cursorLabel = new Label (" ");
			statusBox.PackEnd (cursorLabel, false, false, 0);
			
			VSeparator sep = new VSeparator ();
			sep.Show ();
			statusBox.PackEnd (sep, false, false, 9);
			
			eventBox = new EventBox ();
			eventBox.BorderWidth = 0;
			statusBox.PackEnd (eventBox, false, false, 4);
			
			errorsStatusPanel = new ErrorsStatusPanel ();
			statusBox.PackEnd (errorsStatusPanel, false, false, 0);
			
			textStatusBarPanel = statusBox;
			textStatusBarPanel.BorderWidth = 2;
			mainBox.Add (textStatusBarPanel);
			
			ShowReady ();
			
			this.progressBar.Fraction = 0.0;
			this.ShowAll ();
			eventBox.HideAll ();
			
			originalFrame.HideAll ();
			progressBar.Visible = false;
			
			// the Mac has a resize grip by default, and the GTK+ one breaks it
			if (MonoDevelop.Core.PropertyService.IsMac)
				HasResizeGrip = false;
			
			jobBar.MainOperationStarted += delegate {
				HideMessageBar ();
			};
		}

		int basePopupY;

		internal void ExpandPanel (ExpandableStatusBarPanel panel)
		{
			if (currentPopup != null)
				HidePopup ();
			
			if (!IdeApp.Workbench.RootWindow.IsActive)
				return;
			
			Gdk.Rectangle pos = IdeApp.Workbench.RootWindow.GetCoordinates (panel);
			
			int x,y;
			expandCenter = pos.X;
			panel.GetPointer (out x, out y);
			expandCenter += x;
			
			currentExpandedPanel = panel;

			SetupExpandedPanel (panel);
			
			int newHeight = expandedBox.SizeRequest ().Height;
			basePopupY = pos.Y - (newHeight - pos.Height);
			//textStatusBarPanel.BorderWidth = 0;
			
			Widget content = panel.DetachContent ();
			ExpandableStatusBarPanelTitle newPanel = new ExpandableStatusBarPanelTitle (content);
			currentPopup = new PopupStatusBar (newPanel);
			currentPopup.Show ();
			
			IdeApp.Workbench.RootWindow.AddTopLevelWidget (currentPopup, pos.X, basePopupY);
			
			popupAnimation = GLib.Timeout.Add (25, AnimatePopup);
			cursorChecker = GLib.Timeout.Add (25, CheckPointer);
		}
		
		void SetupExpandedPanel (ExpandableStatusBarPanel panel)
		{
			panel.Expanded = true;
			expandedWidget = panel.ExpandedPanel;
			expandedWidget.Show ();
			currentExpandedMainWidget = panel.MainPanelWidget;
			currentExpandedMainWidget.SizeAllocated += HandleCurrentExpandedMainWidgetSizeAllocated;
			expandedBox = new ExpandedBox (expandedWidget);
			Gdk.Rectangle pos = IdeApp.Workbench.RootWindow.GetCoordinates (panel);
//			expandedBox.WidthRequest = IdeApp.Workbench.RootWindow.Allocation.Width;
			expandedBox.Show ();
			IdeApp.Workbench.RootWindow.AddTopLevelWidget (expandedBox, pos.X, pos.Bottom - expandedBox.SizeRequest ().Height);
			// For some weird reason size request may change after adding the widget
			IdeApp.Workbench.RootWindow.MoveTopLevelWidget (expandedBox, pos.X, pos.Bottom - expandedBox.SizeRequest ().Height);
		}

		void HandleCurrentExpandedMainWidgetSizeAllocated (object o, SizeAllocatedArgs args)
		{
			Gdk.Rectangle pos = IdeApp.Workbench.RootWindow.GetCoordinates (currentExpandedMainWidget);
			int shiftSize = expandCenter - (pos.X + (pos.Width/2));
			if (shiftSize == 0)
				return;
			pos = IdeApp.Workbench.RootWindow.GetCoordinates (expandedBox);
			Gdk.Rectangle headerPos = IdeApp.Workbench.RootWindow.GetTopLevelPosition (currentPopup);
			int newX = pos.X + shiftSize;
			if (newX > headerPos.X) {
				expandedBox.Shift (newX - headerPos.X);
				newX = headerPos.X;
			}
			int w = expandedBox.SizeRequest ().Width;
			if (newX + w < headerPos.Right)
				expandedBox.WidthRequest = headerPos.Right - newX;
			IdeApp.Workbench.RootWindow.MoveTopLevelWidget (expandedBox, newX, pos.Y);
//			expandedBox.Shift (shiftSize);
			currentExpandedMainWidget.SizeAllocated -= HandleCurrentExpandedMainWidgetSizeAllocated; // Shift only once
		}
		
		bool AnimatePopup ()
		{
			int targetPopupY = basePopupY - currentPopup.SizeRequest ().Height;
			
			Gdk.Rectangle pos = IdeApp.Workbench.RootWindow.GetCoordinates (currentPopup);
			
			int newY = targetPopupY + (int)((double) (pos.Y - targetPopupY) * 0.4);
			IdeApp.Workbench.RootWindow.MoveTopLevelWidget (currentPopup, pos.X, newY);
			currentPopup.Show ();
			if (newY == targetPopupY) {
				popupAnimation = 0;
				return false;
			} else
				return true;
		}
		
		bool CheckPointer ()
		{
			int x, y;
			IdeApp.Workbench.RootWindow.GetPointer (out x, out y);
			if (!IdeApp.Workbench.RootWindow.GetCoordinates (currentPopup).Contains (x, y) && !IdeApp.Workbench.RootWindow.GetCoordinates (expandedBox).Contains (x,y))
				HidePopup ();
			return true;
		}
		
		void RemoveExpandedBox ()
		{
			((Gtk.Container)expandedWidget.Parent).Remove (expandedWidget);
			IdeApp.Workbench.RootWindow.RemoveTopLevelWidget (expandedBox);
			expandedBox.Destroy ();
			currentExpandedMainWidget.SizeAllocated -= HandleCurrentExpandedMainWidgetSizeAllocated;
			expandedWidget = null;
			currentExpandedMainWidget = null;
		}

		void HidePopup ()
		{
			if (currentPopup != null) {
				RemoveExpandedBox ();
				currentExpandedPanel.ReattachContent ();
				currentExpandedPanel.Expanded = false;
				currentExpandedPanel = null;
				IdeApp.Workbench.RootWindow.RemoveTopLevelWidget (currentPopup);
				currentPopup.Destroy ();
				currentPopup = null;
				if (popupAnimation != 0) {
					GLib.Source.Remove (popupAnimation);
					popupAnimation = 0;
				}
				GLib.Source.Remove (cursorChecker);
			}
		}
		
		void HideMessageBar ()
		{
			progressBar.Visible = false;
			if (currentStatusImage != null)
				currentStatusImage.Visible = false;
			statusLabel.Visible = false;
		}
		
		public void ShowCaretState (int line, int column, int selectedChars, bool isInInsertMode)
		{
			DispatchService.AssertGuiThread ();
			
			string cursorText = selectedChars > 0 ? String.Format ("{0,3} : {1,-3} - {2}", line, column, selectedChars) : String.Format ("{0,3} : {1,-3}", line, column);
			if (cursorLabel.Text != cursorText)
				cursorLabel.Text = cursorText;
		}
		
		public void ClearCaretState ()
		{
			cursorLabel.Text = "";
		}
		
		public void ShowReady ()
		{
			HideMessageBar ();
			jobBar.RestoreMainOperation ();
		}
		
		public void ShowError (string error)
		{
			ShowMessage (new Image (MonoDevelop.Core.Gui.Stock.Error, IconSize.Menu), error);
		}
		
		public void ShowWarning (string warning)
		{
			DispatchService.AssertGuiThread ();
			ShowMessage (new Gtk.Image (MonoDevelop.Core.Gui.Stock.Warning, IconSize.Menu), warning);
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
		
		void ShowMessage (Image image, string message, bool isMarkup)
		{
			DispatchService.AssertGuiThread ();
			if (currentStatusImage != image) {
				if (currentStatusImage != null) 
					statusBox.Remove (currentStatusImage);
				currentStatusImage = image;
				if (image != null) {
					image.SetPadding (0, 0);
					statusBox.PackStart (image, false, false, 0);
					statusBox.ReorderChild (image, 1);
					image.Show ();
				}
			}
			string txt = !String.IsNullOrEmpty (message) ? " " + message.Replace ("\n", " ") : "";
			if (isMarkup) {
				statusLabel.Markup = txt;
			} else {
				statusLabel.Text = txt;
			}
			
			if (!jobBar.IsMainOperationRunning) {
				jobBar.ShiftMainOperation ();
				if (currentStatusImage != null)
					currentStatusImage.Show ();
				statusLabel.Show ();
			}
		}
		
		public StatusIcon ShowStatusIcon (Gdk.Pixbuf pixbuf)
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
		}

		public void Pulse ()
		{
			DispatchService.AssertGuiThread ();
			this.progressBar.Visible = true;
			this.progressBar.Pulse ();
		}		
		#endregion
		
		public class StatusIcon : IDisposable
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
			
			public bool AnimateIcon ()
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
	}
	
	class PopupStatusBar: Gtk.EventBox
	{
		public PopupStatusBar (Gtk.Widget child)
		{
			Alignment al = new Alignment (0,0,1,1);
			al.TopPadding = 2;
			Add (al);
			ShowAll ();
			al.Add (child);
		}
	}
	
	abstract class ExpandableStatusBarPanel: EventBox
	{
		bool expanded;
		HBox mainBox;
		const int expandedMargin = 3;
		
		public ExpandableStatusBarPanel ()
		{
			Events |= Gdk.EventMask.EnterNotifyMask;
			mainBox = new HBox ();
			mainBox.Show ();
			Add (mainBox);
			Expandable = true;
		}
		
		public Gtk.Widget DetachContent ()
		{
			Requisition req = SizeRequest ();
			WidthRequest = req.Width;
			HeightRequest = req.Height;
			Remove (mainBox);
			mainBox.BorderWidth = 2;
			return mainBox;
		}
		
		public void ReattachContent ()
		{
			if (mainBox.Parent != null)
				((Gtk.Container)mainBox.Parent).Remove (mainBox);
			mainBox.BorderWidth = 0;
			Add (mainBox);
			WidthRequest = -1;
			HeightRequest = -1;
		}
		
		public HBox MainBox {
			get { return mainBox; }
		}
		
		public bool Expandable { get; set; }
		
		public abstract Gtk.Widget ExpandedPanel { get; }
		
		public abstract Gtk.Widget MainPanelWidget { get; }
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			if (!expanded && Expandable)
				IdeApp.Workbench.StatusBar.ExpandPanel (this);
			return base.OnEnterNotifyEvent (evnt);
		}
		
		internal bool Expanded {
			get {
				return expanded;
			}
			set {
				expanded = value;
			}
		}
	}
	
	class ExpandableStatusBarPanelTitle: EventBox
	{
		public ExpandableStatusBarPanelTitle (Gtk.Widget content)
		{
			Show ();
			Add (content);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			int w, h;
			GdkWindow.GetSize (out w, out h);
			int x=0, y=0, r=5;
			using (Cairo.Context ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				HslColor c1 = new HslColor (Style.Background (Gtk.StateType.Normal));
				HslColor c2 = c1;
				c1.L *= 0.7;
				c2.L *= 0.9;
				Cairo.Gradient pat = new Cairo.LinearGradient (x, y, x, y+h);
				pat.AddColorStop (0, c1);
				pat.AddColorStop (1, c2);
				ctx.NewPath ();
				ctx.Arc (x+r, y+r, r, 180 * (Math.PI / 180), 270 * (Math.PI / 180));
				ctx.LineTo (x+w-r, y);
				ctx.Arc (x+w-r, y+r, r, 270 * (Math.PI / 180), 360 * (Math.PI / 180));
				ctx.LineTo (x+w, y+h);
				ctx.LineTo (x, y+h);
				ctx.ClosePath ();
				ctx.Pattern = pat;
				ctx.Fill ();
			}
			PropagateExpose (Child, evnt);
			return true;
		}
	}
	
	class ExpandedBox: EventBox
	{
		Label filler;
		ExpandedPanelContainer expanded;
		HBox mainBox;
		
		public ExpandedBox (Gtk.Widget w)
		{
			mainBox = new HBox ();
			Add (mainBox);
			mainBox.Show ();
			filler = new Label ();
			filler.WidthRequest = 0;
			filler.Show ();
			mainBox.PackStart (filler, false, false, 0);
			expanded = new ExpandedPanelContainer (w);
			expanded.Show ();
			mainBox.PackStart (expanded, false, false, 0);
			Show ();
		}
		
		public void Shift (int x)
		{
//			if (x + expanded.SizeRequest ().Width > Allocation.Width)
//				x = Allocation.Width - expanded.SizeRequest ().Width - 1;
			if (x >= 0)
				filler.WidthRequest = x;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Gdk.Rectangle r = new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height);
			using (Cairo.Context ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				HslColor c1 = new HslColor (Style.Background (Gtk.StateType.Normal));
				HslColor c2 = c1;
				c1.L *= 0.9;
				c2.L *= 0.95;
				Cairo.Gradient pat = new Cairo.LinearGradient (r.X, r.Y, r.X, r.Bottom);
				pat.AddColorStop (0, c1);
				pat.AddColorStop (0.3, c2);
				pat.AddColorStop (1, c2);
				ctx.Rectangle (r.X, r.Y, r.Width, r.Height);
				ctx.Pattern = pat;
				ctx.Fill ();
			}
			foreach (Gtk.Widget w in Children)
				PropagateExpose (w, evnt);
			return true;
		}

	}
	
	class ExpandedPanelContainer: HBox
	{
		HBox mainBox = new HBox ();
		
		public ExpandedPanelContainer (Gtk.Widget w)
		{
			mainBox.BorderWidth = 3;
			Add (mainBox);
			ShowAll ();
			mainBox.Add (w);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Gdk.Rectangle r = Allocation;
			using (Cairo.Context ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				HslColor c1 = new HslColor (Style.Background (Gtk.StateType.Normal));
				HslColor c2 = c1;
				c1.L *= 0.9;
				c2.L *= 0.7;
				Cairo.Gradient pat = new Cairo.LinearGradient (r.X, r.Y, r.X, r.Bottom);
				pat.AddColorStop (0, c1);
				pat.AddColorStop (1, c2);
				ctx.Rectangle (r.X, r.Y, r.Width, r.Height);
				ctx.Pattern = pat;
				ctx.Fill ();
			}
			foreach (Gtk.Widget w in Children)
				PropagateExpose (w, evnt);
			return true;
		}

	}
}
