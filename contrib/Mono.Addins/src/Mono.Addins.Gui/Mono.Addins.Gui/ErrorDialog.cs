//
// ErrorDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using Gtk;

namespace Mono.Addins.Gui
{
	partial class ErrorDialog : Dialog
	{
		TextTag tagNoWrap;
		TextTag tagWrap;
		
		public ErrorDialog (Window parent)
		{
			Build ();
			TransientFor = parent;
			okButton.Clicked += new EventHandler (OnClose);
			expander.Activated += new EventHandler (OnExpanded);
			descriptionLabel.ModifyBg (StateType.Normal, new Gdk.Color (255,0,0));
			
			tagNoWrap = new TextTag ("nowrap");
			tagNoWrap.WrapMode = WrapMode.None;
			detailsTextView.Buffer.TagTable.Add (tagNoWrap);
			
			tagWrap = new TextTag ("wrap");
			tagWrap.WrapMode = WrapMode.Word;
			detailsTextView.Buffer.TagTable.Add (tagWrap);
			
			expander.Visible = false;
		}
		
		public string Message {
			get { return descriptionLabel.Text; }
			set {
				string message = value;
				while (message.EndsWith ("\r") || message.EndsWith ("\n"))
					message = message.Substring (0, message.Length - 1);
				if (!message.EndsWith (".")) message += ".";
				descriptionLabel.Text = message;
			}
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
		
		void OnClose (object sender, EventArgs args)
		{
			Destroy ();
		}
		
		void OnExpanded (object sender, EventArgs args)
		{
			GLib.Timeout.Add (100, new GLib.TimeoutHandler (UpdateSize));
		}
		
		bool UpdateSize ()
		{
			int w, h;
			GetSize (out w, out h);
			Resize (w, 1);
			return false;
		}
	}
}
