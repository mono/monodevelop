// 
// ErrorDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class GtkErrorDialog : Gtk.Dialog
	{
		TextTag tagWrap, tagNoWrap;
		TextView detailsTextView;
		Expander expander;
		
		public GtkErrorDialog (Window parent, string title, string message, AlertButton[] buttons)
		{
			if (string.IsNullOrEmpty (title))
				throw new ArgumentException ();
			if (buttons == null)
				throw new ArgumentException ();
			
			Title = BrandingService.ApplicationName;
			TransientFor = parent;
			Modal = true;
			WindowPosition = Gtk.WindowPosition.CenterOnParent;
			DefaultWidth = 624;
			DefaultHeight = 142;
			
			this.VBox.BorderWidth = 2;
			
			var hbox = new HBox () {
				Spacing = 6,
				BorderWidth = 12,
			};
			
			var errorImage = new Image (Gtk.Stock.DialogError, IconSize.Dialog) {
				Yalign = 0F,
			};
			hbox.PackStart (errorImage, false, false, 0);
			this.VBox.Add (hbox);
			
			var vbox = new VBox () {
				Spacing = 6,
			};
			hbox.PackEnd (vbox, true, true, 0);
			
			var titleLabel = new Label () {
				Markup = "<b>" + GLib.Markup.EscapeText (title) + "</b>",
				Xalign = 0F,
			};
			vbox.PackStart (titleLabel, false, false, 0);
			
			if (!string.IsNullOrWhiteSpace (message)) {
				message = message.Trim ();
				var descriptionLabel = new Label (message) {
					Xalign = 0F,
					Selectable = true,
				};
				descriptionLabel.LineWrap = true;
				descriptionLabel.WidthRequest = 500;
				descriptionLabel.ModifyBg (StateType.Normal, new Gdk.Color (255,0,0));
				vbox.PackStart (descriptionLabel, false, false, 0);
			}
			
			expander = new Expander (GettextCatalog.GetString ("Details")) {
				CanFocus = true,
				Visible = false,
			};
			vbox.PackEnd (expander, true, true, 0);
			
			var sw = new ScrolledWindow () {
				HeightRequest = 180,
				ShadowType = ShadowType.Out,
			};
			expander.Add (sw);
			
			detailsTextView = new TextView () {
				CanFocus = true,
			};
			detailsTextView.KeyPressEvent += TextViewKeyPressed;
			sw.Add (detailsTextView);
			
			var aa = this.ActionArea;
			aa.Spacing = 10;
			aa.LayoutStyle = ButtonBoxStyle.End;
			aa.BorderWidth = 5;
			aa.Homogeneous = true;
			
			expander.Activated += delegate {
				this.AllowGrow = expander.Expanded;
				GLib.Timeout.Add (100, delegate {
					Resize (DefaultWidth, 1);
					return false;
				});
			};
			
			tagNoWrap = new TextTag ("nowrap");
			tagNoWrap.WrapMode = WrapMode.None;
			detailsTextView.Buffer.TagTable.Add (tagNoWrap);
			
			tagWrap = new TextTag ("wrap");
			tagWrap.WrapMode = WrapMode.Word;
			detailsTextView.Buffer.TagTable.Add (tagWrap);
			
			this.Buttons = buttons;
			for (int i = 0; i < Buttons.Length; i++) {
				Gtk.Button button;
				button = new Gtk.Button (Buttons[i].Label);
				button.ShowAll ();
				AddActionWidget (button, i);
			}
			
			Child.ShowAll ();
			Hide ();
		}
		
		[GLib.ConnectBefore]
		void TextViewKeyPressed (object sender, KeyPressEventArgs args)
		{
			if (args.Event.State.HasFlag (Gdk.ModifierType.ControlMask) &&
			    (args.Event.Key == Gdk.Key.c || args.Event.Key == Gdk.Key.C)) {
				TextView tv = (TextView) sender;
				
				Gtk.Clipboard clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
				Gtk.TextIter start, end;
				string text;
				
				if (!tv.Buffer.GetSelectionBounds (out start, out end) || start.Offset == end.Offset) {
					start = tv.Buffer.StartIter;
					end = tv.Buffer.EndIter;
				}
				
				text = tv.Buffer.GetText (start, end, true);
				
				if (Platform.IsWindows) {
					// Windows specific hack
					text = text.Replace ("\r\n", "\n");
				}
				
				clipboard.Text = text;
				
				args.RetVal = true;
			}
		}
		
		public AlertButton[] Buttons {
			get; private set;
		}

		public void AddDetails (string text, bool wrapped)
		{
			TextIter it = detailsTextView.Buffer.EndIter;
			if (wrapped)
				detailsTextView.Buffer.InsertWithTags (ref it, text, tagWrap);
			else
				detailsTextView.Buffer.InsertWithTags (ref it, text, tagNoWrap);
			expander.Visible = true;
		}
	}
}

