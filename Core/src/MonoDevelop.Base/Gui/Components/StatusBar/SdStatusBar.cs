// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using MonoDevelop.Services;
using Gtk;
using Gnome;

namespace MonoDevelop.Gui.Components
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
		
		public SdStatusBar (IStatusBarService manager)
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
		}
		
		public void SetModeStatus (string status)
		{
			modeStatusBarPanel.ShowAll ();
			modeLabel.Text = " " + status + " ";
		}
		
		public void ShowErrorMessage(string message)
		{
			SetMessage (String.Format (GettextCatalog.GetString ("Error : {0}"), message));
		}
		
		public void ShowErrorMessage(Image image, string message)
		{
			SetMessage (String.Format (GettextCatalog.GetString ("Error : {0}"), message));
		}
		
		public void SetCursorPosition (int ln, int col, int ch)
		{
			cursorStatusBarPanel.ShowAll ();
			cursorLabel.Markup = String.Format (GettextCatalog.GetString (" ln <span font_family='fixed'>{0,-4}</span>  col <span font_family='fixed'>{1,-3}</span>  ch <span font_family='fixed'>{2,-3}</span> "), ln, col, ch);
		}
		
		public void SetMessage (string message)
		{
			if (currentStatusImage != null) {
				statusBox.Remove (currentStatusImage);
				currentStatusImage = null;
			}
			if (message != null)
				statusLabel.Text = " " + message;
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
				statusLabel.Text = message;
			else
				statusLabel.Text = "";
		}
		
		public IStatusIcon ShowStatusIcon (Gtk.Image image)
		{
			EventBox ebox = new EventBox ();
			ebox.Child = image;
			statusBox.PackEnd (ebox, false, false, 2);
			statusBox.ReorderChild (ebox, 0);
			ebox.ShowAll ();
			return new StatusIcon (ebox);
		}
		
		public void HideStatusIcon (IStatusIcon icon)
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
		internal EventBox box;
		string tip;
		Tooltips tips;
		
		public StatusIcon (EventBox box)
		{
			this.box = box;
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
		
		public Image Image {
			get { return (Image) box.Child; }
			set { box.Child = value; }
		}
	}
}
