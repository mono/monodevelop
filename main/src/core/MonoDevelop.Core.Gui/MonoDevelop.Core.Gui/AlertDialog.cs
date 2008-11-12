//
// AlertDialog.cs
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
using System.Text;
using Gtk;

namespace MonoDevelop.Core.Gui
{
	/// <summary>
	/// A Gnome HIG compliant alert dialog.
	/// </summary>
	internal class AlertDialog : Gtk.Dialog
	{
		AlertButton resultButton = null;
		AlertButton[] buttons;
		
		Gtk.HBox  hbox  = new HBox ();
		Gtk.Image image = new Image ();
		Gtk.Label label = new Label ();
		
		public AlertButton ResultButton {
			get {
				return resultButton;
			}
		}
		
		void Init ()
		{
			VBox.PackStart (hbox);
			hbox.PackStart (image);
			hbox.PackStart (label);
				
			// Table 3.1
			this.Title        = "";
			this.BorderWidth  = 6;
			//this.Type         = WindowType.Toplevel;
			this.Resizable    = false;
			this.HasSeparator = false;
			
			// Table 3.2
			this.VBox.Spacing = 12;
			
			// Table 3.3
			this.hbox.Spacing     = 12;
			this.hbox.BorderWidth = 6;
			
			// Table 3.4
			this.image.Yalign   = 0.00f;
			//this.image.IconSize = Gtk.IconSize.Dialog;
			
			// Table 3.5
			this.label.UseMarkup = true;
			this.label.Wrap      = true;
			this.label.Yalign    = 0.00f;
		}
		
		public AlertDialog (string icon, string primaryText, string secondaryText, AlertButton[] buttons)
		{
			Init ();
			this.buttons = buttons;
			image.Pixbuf = Services.Resources.GetBitmap (icon, IconSize.Dialog);
			
			StringBuilder markup = new StringBuilder (@"<span weight=""bold"" size=""larger"">");
			markup.Append (GLib.Markup.EscapeText (primaryText));
			markup.Append ("</span>");
			if (!String.IsNullOrEmpty (secondaryText)) {
				if (!String.IsNullOrEmpty (primaryText)) {
					markup.AppendLine ();
					markup.AppendLine ();
				}
				markup.Append (GLib.Markup.EscapeText (secondaryText));
			}
			label.Markup = markup.ToString ();
			
			foreach (AlertButton button in buttons) {
				Button newButton = new Button ();
				newButton.Label        = button.Label;
				newButton.UseUnderline = true;
				newButton.UseStock     = button.IsStockButton;
				if (!String.IsNullOrEmpty (button.Icon))
					newButton.Image = new Image (button.Icon, IconSize.Button);
				newButton.Clicked += ButtonClicked;
				ActionArea.Add (newButton);
			}
			this.ShowAll ();
		}
		
		public void FocusButton (int buttonNumber)
		{
			ActionArea.Children[buttonNumber].GrabFocus ();
		}
			
		
		void ButtonClicked (object sender, EventArgs e) 
		{
			Gtk.Button clickButton = (Gtk.Button)sender;
			foreach (AlertButton alertButton in buttons) {
				if (clickButton.Label == alertButton.Label) {
					resultButton = alertButton;
					break;
				}
			}
			this.Destroy ();
		}
	}
}
