// 
// FeedbackDialog.cs
//  
// Author:
//       lluis <${AuthorEmail}>
// 
// Copyright (c) 2011 lluis
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
using MonoDevelop.Core;
using System.Net;
using System.IO;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public class FeedbackDialog: Gtk.Window
	{
		Gtk.VBox mainBox;
		EntryWithEmptyMessage mailEntry;
		TextViewWithEmptyMessage textEntry;
		Gtk.Label mailLabel;
		Gtk.Label mailWarningLabel;
		Gtk.Label bodyWarningLabel;
		Gtk.HBox headerBox;
		bool closed;
		bool sent;
		Gtk.Frame mainFrame;
		
		public FeedbackDialog (int x, int y): base (Gtk.WindowType.Toplevel)
		{
			SetDefaultSize (350, 200);
			Move (x - 350, y - 200);
			mainFrame = new Gtk.Frame ();
			
			mainBox = new Gtk.VBox ();
			mainBox.BorderWidth = 12;
			mainBox.Spacing = 6;
			headerBox = new Gtk.HBox ();
			mailEntry = new EntryWithEmptyMessage ();
			mailEntry.EmptyMessage = GettextCatalog.GetString ("email address");
			Decorated = false;
			mainFrame.ShadowType = Gtk.ShadowType.Out;
			
			// Header
			
			headerBox.Spacing = 6;
			mailLabel = new Gtk.Label ();
			headerBox.PackStart (mailLabel, false, false, 0);
			Gtk.Button changeButton = new Gtk.Button ("(Change)");
			changeButton.Relief = Gtk.ReliefStyle.None;
			headerBox.PackStart (changeButton, false, false, 0);
			changeButton.Clicked += HandleChangeButtonClicked;
			mainBox.PackStart (headerBox, false, false, 0);
			mainBox.PackStart (mailEntry, false, false, 0);
			mailWarningLabel = new Gtk.Label (GettextCatalog.GetString ("Please enter a valid e-mail address"));
			mailWarningLabel.Xalign = 0;
			mainBox.PackStart (mailWarningLabel, false, false, 0);
			
			// Body
			
			textEntry = new TextViewWithEmptyMessage ();
			textEntry.EmptyMessage = GettextCatalog.GetString (
				"Tell us how we can make {0} better.",
				BrandingService.SuiteName
			);
			textEntry.AcceptsTab = false;
			textEntry.WrapMode = Gtk.WrapMode.Word;
			textEntry.WidthRequest = 300;
			var sw = new Gtk.ScrolledWindow ();
			sw.ShadowType = Gtk.ShadowType.In;
			sw.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.HscrollbarPolicy = Gtk.PolicyType.Never;
			sw.Add (textEntry);
			mainBox.PackStart (sw, true, true, 0);
			bodyWarningLabel = new Gtk.Label (GettextCatalog.GetString ("Please enter some feedback"));
			bodyWarningLabel.Xalign = 0;
			mainBox.PackStart (bodyWarningLabel, false, false, 0);
			
			// Bottom
			
			Gtk.HBox bottomBox = new Gtk.HBox (false, 6);
			Gtk.Label countLabel = new Gtk.Label ();
			countLabel.Xalign = 0;
			bottomBox.PackStart (countLabel, false, false, 0);
			
			Gtk.Button sendButton = new Gtk.Button (GettextCatalog.GetString ("Send Feedback"));
			sendButton.Clicked += HandleSendButtonClicked;
			bottomBox.PackEnd (sendButton, false, false, 0);
			mainBox.PackStart (bottomBox, false, false, 0);
			
			// Init
			
			mainBox.ShowAll ();
			
			mailWarningLabel.Hide ();
			bodyWarningLabel.Hide ();
			
			string mail = FeedbackService.ReporterEMail;
			if (string.IsNullOrEmpty (mail))
				mail = AuthorInformation.Default.Email;
			
			if (string.IsNullOrEmpty (mail)) {
				headerBox.Hide ();
				mailEntry.GrabFocus ();
			}
			else {
				mailEntry.Text = mail;
				mailEntry.Hide ();
				mailLabel.Text = GettextCatalog.GetString ("From: {0}", mail);
				textEntry.GrabFocus ();
			}
			if (FeedbackService.FeedbacksSent > 0)
				countLabel.Text = GettextCatalog.GetString ("Your feedbacks: {0}", FeedbackService.FeedbacksSent);
			else
				countLabel.Hide ();
			
			mainFrame.Show ();
			mainFrame.Add (mainBox);
			Add (mainFrame);
		}

		void HandleChangeButtonClicked (object sender, EventArgs e)
		{
			headerBox.Hide ();
			mailEntry.Show ();
			mailEntry.GrabFocus ();
		}

		void HandleSendButtonClicked (object sender, EventArgs e)
		{
			string email = mailEntry.Text;
			
			if (!ValidateEmail (email)) {
				mailWarningLabel.Show ();
				mailEntry.GrabFocus ();
				return;
			}
			mailWarningLabel.Hide ();
			
			if (textEntry.Buffer.Text.Length == 0) {
				bodyWarningLabel.Show ();
				textEntry.GrabFocus ();
				return;
			}
			
			FeedbackService.SendFeedback (email, textEntry.Buffer.Text);
			
			mainFrame.Remove (mainBox);
			mainBox.Destroy ();
			
			Gtk.VBox box = new Gtk.VBox (false, 18);
			box.PackStart (new Gtk.Label (), true, true, 0); // Filler
			string txt = "<big>" + GettextCatalog.GetString ("Thank you for your feedback!") + "</big>";
			Gtk.Label lab = new Gtk.Label ();
			lab.Markup = txt;
			box.PackStart (lab, false, false, 0);
			
			lab = new Gtk.Label (GettextCatalog.GetString ("Feedbacks sent: {0}", FeedbackService.FeedbacksSent));
			box.PackStart (lab, false, false, 0);
			
			box.PackStart (new Gtk.Label (), true, true, 0); // Filler
			box.ShowAll ();

			mainFrame.Add (box);
			GLib.Timeout.Add (1000, delegate {
				Close ();
				return false;
			});
			sent = true;
		}
		
		bool ValidateEmail (string email)
		{
			int i = email.IndexOf ('@');
			if (i <= 0)
				return false;
			int p = email.IndexOf ('.', i + 1);
			return (p > i + 1 && p < email.Length - 1);
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape || sent) {
				Close ();
				return true;
			}
			return base.OnKeyPressEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (sent) {
				Close ();
				return true;
			}
			return base.OnButtonPressEvent (evnt);
		}
		
		void Close ()
		{
			if (!closed) {
				closed = true;
				Destroy ();
			}
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			bool res = base.OnFocusOutEvent (evnt);
			Hide ();
			return res;
		}
	}
	
	class TextViewWithEmptyMessage: Gtk.TextView
	{
		private Pango.Layout layout;
		private Gdk.GC text_gc;
		
		public string EmptyMessage { get; set; }
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			bool res = base.OnExposeEvent (evnt);
			if (Buffer.Text.Length == 0 && !string.IsNullOrEmpty (EmptyMessage)) {
				if (text_gc == null) {
					text_gc = new Gdk.GC (evnt.Window);
					text_gc.Copy (Style.TextGC (Gtk.StateType.Normal));
					Gdk.Color color_a = Style.Base (Gtk.StateType.Normal);
					Gdk.Color color_b = Style.Text (Gtk.StateType.Normal);
					text_gc.RgbFgColor = EntryWithEmptyMessage.ColorBlend (color_a, color_b);
				}
				
				if (layout == null) {
					layout = new Pango.Layout (PangoContext);
					layout.FontDescription = PangoContext.FontDescription.Copy ();
				}
				
				int width, height;
				layout.SetMarkup (EmptyMessage);
				layout.GetPixelSize (out width, out height);
				evnt.Window.DrawLayout (text_gc, 2, 2, layout);
			}
			return res;
		}
	}
	
	class EntryWithEmptyMessage: Gtk.Entry
	{
		private Pango.Layout layout;
		private Gdk.GC text_gc;
		
		public string EmptyMessage { get; set; }
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			bool res = base.OnExposeEvent (evnt);
			if (Text.Length == 0 && !string.IsNullOrEmpty (EmptyMessage)) {
				if (text_gc == null) {
					text_gc = new Gdk.GC (evnt.Window);
					text_gc.Copy (Style.TextGC (Gtk.StateType.Normal));
					Gdk.Color color_a = Style.Base (Gtk.StateType.Normal);
					Gdk.Color color_b = Style.Text (Gtk.StateType.Normal);
					text_gc.RgbFgColor = ColorBlend (color_a, color_b);
				}
				
				if (layout == null) {
					layout = new Pango.Layout (PangoContext);
					layout.FontDescription = PangoContext.FontDescription.Copy ();
				}
				
				int width, height;
				layout.SetMarkup (EmptyMessage);
				layout.GetPixelSize (out width, out height);
				evnt.Window.DrawLayout (text_gc, 2, 2, layout);
			}
			return res;
		}
		
		public static Gdk.Color ColorBlend (Gdk.Color a, Gdk.Color b)
		{
			// at some point, might be nice to allow any blend?
			double blend = 0.5;
			
			if (blend < 0.0 || blend > 1.0) {
				throw new ApplicationException ("blend < 0.0 || blend > 1.0");
			}
			
			double blendRatio = 1.0 - blend;
			
			int aR = a.Red >> 8;
			int aG = a.Green >> 8;
			int aB = a.Blue >> 8;
			
			int bR = b.Red >> 8;
			int bG = b.Green >> 8;
			int bB = b.Blue >> 8;
			
			double mR = aR + bR;
			double mG = aG + bG;
			double mB = aB + bB;
			
			double blR = mR * blendRatio;
			double blG = mG * blendRatio;
			double blB = mB * blendRatio;
			
			Gdk.Color color = new Gdk.Color ((byte)blR, (byte)blG, (byte)blB);
			Gdk.Colormap.System.AllocColor (ref color, true, true);
			return color;
		}
	}
}

