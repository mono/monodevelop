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
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	/// <summary>
	/// A Gnome HIG compliant alert dialog.
	/// </summary>
	internal class GtkAlertDialog : Gtk.Dialog
	{
		AlertButton resultButton = null;
		AlertButton[] buttons;
		
		Gtk.HBox  hbox  = new HBox ();
		Gtk.Image image = new Image ();
		Gtk.Label label = new Label ();
		VBox labelsBox = new VBox (false, 6);
		
		public AlertButton ResultButton {
			get {
				return resultButton;
			}
		}
		
		public bool ApplyToAll { get; set; }
		
		void Init ()
		{
			VBox.PackStart (hbox);
			hbox.PackStart (image, false, false, 0);
			hbox.PackStart (labelsBox, true, true, 0);
			labelsBox.PackStart (label, true, true, 0);
				
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
			this.label.Xalign    = 0.00f;
		}
		
		public GtkAlertDialog (MessageDescription message)
		{
			Init ();
			this.buttons = message.Buttons.ToArray ();
			
			string primaryText;
			string secondaryText;
			
			if (string.IsNullOrEmpty (message.SecondaryText)) {
				secondaryText = message.Text;
				primaryText = null;
			} else {
				primaryText = message.Text;
				secondaryText = message.SecondaryText;
			}
			
			image.Pixbuf = ImageService.GetPixbuf (message.Icon, IconSize.Dialog);
			
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
			label.Selectable = true;
			
			foreach (AlertButton button in message.Buttons) {
				Button newButton = new Button ();
				newButton.Label        = button.Label;
				newButton.UseUnderline = true;
				newButton.UseStock     = button.IsStockButton;
				if (!String.IsNullOrEmpty (button.Icon))
					newButton.Image = new Image (button.Icon, IconSize.Button);
				newButton.Clicked += ButtonClicked;
				ActionArea.Add (newButton);
			}
			
			foreach (var op in message.Options) {
				CheckButton check = new CheckButton (op.Text);
				check.Active = op.Value;
				labelsBox.PackStart (check, false, false, 0);
				check.Toggled += delegate {
					message.SetOptionValue (op.Id, check.Active);
				};
			}
			
			if (message.AllowApplyToAll) {
				CheckButton check = new CheckButton (GettextCatalog.GetString ("Apply to all"));
				labelsBox.PackStart (check, false, false, 0);
				check.Toggled += delegate {
					ApplyToAll = check.Active;
				};
			}
			
			//don't show this yet, let the consumer decide when
			this.Child.ShowAll ();
		}
		
		public void FocusButton (int buttonNumber)
		{
			if (buttonNumber == -1)
				buttonNumber = ActionArea.Children.Length - 1;
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
