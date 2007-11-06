//  SdStatusBar.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Ide.Gui
{
	public class SdStatusBar : HBox
	{
		ProgressBar progress = new ProgressBar ();
		Frame txtStatusBarPanel    = new Frame ();
		Frame cursorStatusBarPanel = new Frame ();
		Frame modeStatusBarPanel   = new Frame ();
		
		Label statusLabel;
		Label modeLabel;
		Label cursorLabel;
		
		HBox iconsStatusBarPanel = new HBox ();
		
		HBox statusBox = new HBox ();
		Image currentStatusImage;
		
		bool cancelEnabled;
		private static GLib.GType gtype;
		
		/*
		public Statusbar CursorStatusBarPanel
		{
			get {
				return cursorStatusBarPanel;
			}
		}*/
		
		public bool CancelEnabled
		{
			get { return cancelEnabled; }
			set { cancelEnabled = value; }
		}

		public ProgressBar Progress
		{
			get { return progress; }
		}

		public static new GLib.GType GType
		{
			get {
				if (gtype == GLib.GType.Invalid)
					gtype = RegisterGType (typeof (SdStatusBar));
				return gtype;
			}
		}
		
		public SdStatusBar ()
		{
			Spacing = 3;
			BorderWidth = 1;

			progress = new ProgressBar ();
			this.PackStart (progress, false, false, 0);
			
			this.PackStart (txtStatusBarPanel, true, true, 0);
			statusBox = new HBox ();
			statusLabel = new Label ();
			statusLabel.SetAlignment (0, 0.5f);
			statusLabel.Wrap = false;
			statusBox.PackEnd (statusLabel, true, true, 0);
			txtStatusBarPanel.Add (statusBox);

			this.PackStart (cursorStatusBarPanel, false, false, 0);
			cursorLabel = new Label ("  ");
			cursorStatusBarPanel.Add (cursorLabel);
				
			this.PackStart (modeStatusBarPanel, false, false, 0);
			modeLabel = new Label ("  ");
			modeStatusBarPanel.Add (modeLabel);

			this.PackStart (iconsStatusBarPanel, false, false, 0);
			txtStatusBarPanel.ShowAll ();
			
			Progress.Hide ();
			Progress.PulseStep = 0.3;
			
			int w, h;
			Gtk.Icon.SizeLookup (IconSize.Menu, out w, out h);
			statusLabel.HeightRequest = h;
		}
		
		public void SetModeStatus (string status)
		{
			modeStatusBarPanel.ShowAll ();
			modeLabel.Text = " " + status + " ";
		}
		
		public void ShowErrorMessage(string message)
		{
			SetMessage (GettextCatalog.GetString ("Error : {0}", message));
		}
		
		public void ShowErrorMessage(Image image, string message)
		{
			SetMessage (GettextCatalog.GetString ("Error : {0}", message));
		}
		
		public void SetCursorPosition (int ln, int col, int ch)
		{
			cursorStatusBarPanel.ShowAll ();
			cursorLabel.Markup = GettextCatalog.GetString (" ln <span font_family='fixed'>{0,-4}</span>  col <span font_family='fixed'>{1,-3}</span>  ch <span font_family='fixed'>{2,-3}</span> ", ln, col, ch);
		}
		
		public void SetMessage (string message)
		{
			if (currentStatusImage != null) {
				statusBox.Remove (currentStatusImage);
				currentStatusImage = null;
			}
			if (message != null)
				statusLabel.Text = " " + message.Replace ("\n", " ");
			else
				statusLabel.Text = "";
		}
		
		public void SetMessage (Image image, string message)
		{
			if (currentStatusImage != image) {
				if (currentStatusImage != null) statusBox.Remove (currentStatusImage);
				currentStatusImage = image;
				statusBox.PackStart (image, false, false, 3);
				image.Show ();
			}
			
			if (message != null)
				statusLabel.Text = message.Replace ("\n", " ");
			else
				statusLabel.Text = "";
		}
		
		public IStatusIcon ShowStatusIcon (Gdk.Pixbuf image)
		{
			EventBox ebox = new EventBox ();
			ebox.Child = new Gtk.Image (image);
			statusBox.PackEnd (ebox, false, false, 2);
			statusBox.ReorderChild (ebox, 0);
			ebox.ShowAll ();
			return new StatusIcon (this, ebox, image);
		}
		
		internal void HideStatusIcon (IStatusIcon icon)
		{
			statusBox.Remove (((StatusIcon)icon).EventBox);
		}
		
		// Progress Monitor implementation
		public void BeginProgress (string name)
		{
			SetMessage (name);
			this.Progress.Visible = true;
		}

		public void SetProgressFraction (double work)
		{
			this.Progress.Fraction = work;
		}
		
		public void EndProgress ()
		{
			SetMessage ("");
			this.Progress.Fraction = 0.0;
			this.Progress.Visible = false;
		}

		public void Pulse ()
		{
			this.Progress.Visible = true;
			this.Progress.Pulse ();
		}
	}
	
	class StatusIcon: IStatusIcon
	{
		SdStatusBar statusBar;
		internal EventBox box;
		string tip;
		Tooltips tips;
		DateTime alertEnd;
		Gdk.Pixbuf icon;
		
		int astep;
		Gtk.Image[] images;
		
		public StatusIcon (SdStatusBar statusBar, EventBox box, Gdk.Pixbuf icon)
		{
			this.statusBar = statusBar;
			this.box = box;
			this.icon = icon;
		}
		
		public void Dispose ()
		{
			statusBar.HideStatusIcon (this);
		}
		
		public string ToolTip {
			get { return tip; }
			set {
				if (tips == null) tips = new Tooltips ();
				tip = value;
				if (tip == null)
					tips.Disable ();
				else {
					tips.Enable ();
					tips.SetTip (box, tip, tip);
				}
			}
		}
		
		public EventBox EventBox {
			get { return box; }
		}
		
		public Gdk.Pixbuf Image {
			get { return icon; }
			set {
				icon = value;
				box.Child = new Gtk.Image (icon);
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
				images [n] = new Image (Services.Icons.MakeTransparent (icon, ((double)(9-n))/10.0));
				images [n].Show ();
			}
		}
		
		public bool AnimateIcon ()
		{
			box.Remove (box.Child);
			
			if (DateTime.Now >= alertEnd && astep == 0) {
				box.Child = new Gtk.Image (icon);
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
