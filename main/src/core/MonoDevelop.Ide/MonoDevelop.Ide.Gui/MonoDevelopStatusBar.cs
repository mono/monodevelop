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

namespace MonoDevelop.Ide
{
	public class MonoDevelopStatusBar : Gtk.HBox
	{
		ProgressBar progress       = new ProgressBar ();
		Frame textStatusBarPanel    = new Frame ();
		Frame cursorStatusBarPanel = new Frame ();
		Frame modeStatusBarPanel   = new Frame ();
		
		Label statusLabel;
		Label modeLabel;
		Label cursorLabel;
		
		HBox iconStatusBarPanel = new HBox ();
		
		HBox statusBox = new HBox ();
		Image currentStatusImage;
		
		public MonoDevelopStatusBar()
		{
			Spacing = 3;
			BorderWidth = 1;
			
			progress = new ProgressBar ();
			progress.PulseStep = 0.3;
			this.PackStart (progress, false, false, 0);
			
			this.PackStart (textStatusBarPanel, true, true, 0);
			statusBox = new HBox ();
			statusLabel = new Label ();
			statusLabel.SetAlignment (0, 0.5f);
			statusLabel.Wrap = false;
			statusBox.PackEnd (statusLabel, true, true, 0);
			textStatusBarPanel.Add (statusBox);
			
			this.PackStart (cursorStatusBarPanel, false, false, 0);
			cursorLabel = new Label ("  ");
			cursorStatusBarPanel.Add (cursorLabel);
				
			this.PackStart (modeStatusBarPanel, false, false, 0);
			modeLabel = new Label ("  ");
			modeStatusBarPanel.Add (modeLabel);

			this.PackStart (iconStatusBarPanel, false, false, 0);
			
			int w, h;
			Gtk.Icon.SizeLookup (IconSize.Menu, out w, out h);
			statusLabel.HeightRequest = h;
			
			ShowReady ();
			this.progress.Fraction = 0.0;
			this.ShowAll ();
			progress.Visible = false;
		}
		
		public void ShowCaretState (int line, int column, int selectedChars, bool isInInsertMode)
		{
			DispatchService.AssertGuiThread ();
			cursorStatusBarPanel.ShowAll ();
			if (selectedChars > 0) {
				cursorLabel.Text = String.Format ("{0,3} : {1,-3} - {2}", line, column, selectedChars);
			} else {
				cursorLabel.Text = String.Format ("{0,3} : {1,-3}", line, column);
			}
			modeStatusBarPanel.ShowAll ();
			string status = isInInsertMode ? GettextCatalog.GetString ("INS") : GettextCatalog.GetString ("OVR");
			modeLabel.Text = " " + status + " ";
		}
		
		public void ShowReady ()
		{
			ShowMessage (GettextCatalog.GetString ("Ready"));	
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
			DispatchService.AssertGuiThread ();
			if (currentStatusImage != null) {
				statusBox.Remove (currentStatusImage);
				currentStatusImage = null;
			}
			statusLabel.Markup = !String.IsNullOrEmpty (message) ? " " + message.Replace ("\n", " ") : "";
		}
		
		public void ShowMessage (Image image, string message)
		{
			DispatchService.AssertGuiThread ();
			if (currentStatusImage != image) {
				if (currentStatusImage != null) 
					statusBox.Remove (currentStatusImage);
				currentStatusImage = image;
				statusBox.PackStart (image, false, false, 3);
				image.Show ();
			}
			
			statusLabel.Markup = !String.IsNullOrEmpty (message) ? " " + message.Replace ("\n", " ") : "";
		}
		
		public StatusIcon ShowStatusIcon (Gdk.Pixbuf image)
		{
			DispatchService.AssertGuiThread ();
			EventBox eventBox = new EventBox ();
			eventBox.Child = new Gtk.Image (image);
			statusBox.PackEnd (eventBox, false, false, 2);
			statusBox.ReorderChild (eventBox, 0);
			eventBox.ShowAll ();
			return new StatusIcon (this, eventBox, image);
		}
		
		void HideStatusIcon (StatusIcon icon)
		{
			statusBox.Remove (((StatusIcon)icon).EventBox);
		}
		
		#region Progress Monitor implementation
		public void BeginProgress (string name)
		{
			ShowMessage (name);
			this.progress.Visible = true;
		}
		
		public void BeginProgress (Image image, string name)
		{
			ShowMessage (image, name);
			this.progress.Visible = true;
		}

		public void SetProgressFraction (double work)
		{
			DispatchService.AssertGuiThread ();
			this.progress.Fraction = work;
		}
		
		public void EndProgress ()
		{
			ShowMessage ("");
			this.progress.Fraction = 0.0;
			this.progress.Visible = false;
		}

		public void Pulse ()
		{
			DispatchService.AssertGuiThread ();
			this.progress.Visible = true;
			this.progress.Pulse ();
		}		
		#endregion
		
		public class StatusIcon : IDisposable
		{
			MonoDevelopStatusBar statusBar;
			internal EventBox box;
			string tip;
			Tooltips tips;
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
				if (tips != null) {
					tips.Destroy ();
					tips = null;
				}
				if (icon != null) {
					icon.Dispose ();
					icon = null;
				}
			}
			
			public string ToolTip {
				get { return tip; }
				set {
					if (tips == null) 
						tips = new Tooltips ();	
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
}
