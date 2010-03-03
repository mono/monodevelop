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
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide
{
	public class MonoDevelopStatusBar : Gtk.Statusbar
	{
		ProgressBar progressBar = new ProgressBar ();
		Frame textStatusBarPanel = new Frame ();
		
		
		Label statusLabel;
		Label modeLabel;
		Label cursorLabel;
		
		HBox statusBox;
		Image currentStatusImage;
		EventBox eventBox;
		internal MonoDevelopStatusBar()
		{
			Frame originalFrame = (Frame)Children[0];
//			originalFrame.WidthRequest = 8;
//			originalFrame.Shadow = ShadowType.In;
//			originalFrame.BorderWidth = 0;
			
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
			statusBox.PackStart (statusLabel, true, true, 0);
			
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
			
			// the Mac has a resize grip by default, and the GTK+ one breaks it
			if (MonoDevelop.Core.PropertyService.IsMac)
				HasResizeGrip = false;
			
			// todo: Move this to the CompletionWindowManager when it's possible.
			CompletionWindowManager.WindowShown += delegate {
				if (CompletionWindowManager.Wnd.List.CategoryCount > 1)
					ShowMessage (string.Format (GettextCatalog.GetString ("To toggle categorized completion mode press {0}."), IdeApp.CommandService.GetCommandInfo (Commands.TextEditorCommands.ShowCompletionWindow, null).AccelKey));
			};
			
			CompletionWindowManager.WindowClosed += delegate {
				ShowReady ();
			};
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
			cursorLabel.Text = "";
			modeLabel.Text = "";
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
					statusBox.PackStart (image, false, false, 3);
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
}
