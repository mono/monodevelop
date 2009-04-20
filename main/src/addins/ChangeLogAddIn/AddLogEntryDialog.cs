// AddLogEntryDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.IO;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.ChangeLogAddIn
{
	internal partial class AddLogEntryDialog : Gtk.Dialog
	{
		ListStore store;
		Dictionary<ChangeLogEntry,string> changes = new Dictionary<ChangeLogEntry,string> ();
		TextMark editMark;
		TextTag oldTextTag;
		bool loading;
		
		public AddLogEntryDialog (Dictionary<string,ChangeLogEntry> entries)
		{
			Build ();
			
			Pango.FontDescription font = Pango.FontDescription.FromString (
				MonoDevelop.Core.Gui.Services.PlatformService.DefaultMonospaceFont);
			textview.ModifyFont (font);
			textview.WrapMode = WrapMode.None;
			textview.AcceptsTab = true;
			Pango.TabArray tabs = new Pango.TabArray (1, true);
			tabs.SetTab (0, Pango.TabAlign.Left, GetStringWidth (" ") * 4);
			textview.Tabs = tabs;
			textview.SizeRequested += delegate (object o, SizeRequestedArgs args) {
				textview.WidthRequest = GetStringWidth (string.Empty.PadRight (80));
			};
			font.Dispose ();
			
			store = new ListStore (typeof(ChangeLogEntry), typeof(Gdk.Pixbuf), typeof(string));
			fileList.Model = store;
			
			fileList.AppendColumn (string.Empty, new CellRendererPixbuf (), "pixbuf", 1);
			fileList.AppendColumn (string.Empty, new CellRendererText (), "text", 2);
			
			foreach (ChangeLogEntry ce in entries.Values) {
				Gdk.Pixbuf pic;
				if (ce.CantGenerate)
					pic = ImageService.GetPixbuf (Gtk.Stock.DialogWarning, Gtk.IconSize.Menu);
				else if (ce.IsNew)
					pic = ImageService.GetPixbuf (Gtk.Stock.New, Gtk.IconSize.Menu);
				else
					pic = null;
				store.AppendValues (ce, pic, ce.File);
			}
			fileList.Selection.Changed += OnSelectionChanged;
			textview.Buffer.Changed += OnTextChanged;
			TreeIter it;
			
			editMark = textview.Buffer.CreateMark (null, textview.Buffer.EndIter, false);
			oldTextTag = new Gtk.TextTag ("readonly");
			oldTextTag.Foreground = "gray";
			oldTextTag.Editable = false;
			textview.Buffer.TagTable.Add (oldTextTag);
			
			if (store.GetIterFirst (out it))
				fileList.Selection.SelectIter (it);
		}
		
		private int GetStringWidth (string str)
		{
			int width, height;
			Pango.Layout layout = new Pango.Layout (textview.PangoContext);
			layout.SetText (str);
			layout.GetPixelSize (out width, out height);
			layout.Dispose ();
			return width;
		}
		
		public void OnSelectionChanged (object s, EventArgs a)
		{
			TreeIter it;
			if (!fileList.Selection.GetSelected (out it)) {
				textview.Buffer.Text = "";
				textview.Sensitive = false;
			} else {
				textview.Sensitive = true;
				ChangeLogEntry ce = (ChangeLogEntry) store.GetValue (it, 0);
				boxNewFile.Visible = ce.IsNew && !ce.CantGenerate;
				boxNoFile.Visible = ce.CantGenerate;
				loading = true;
				string msg;
				if (changes.TryGetValue (ce, out msg))
					textview.Buffer.Text = msg;
				else
					textview.Buffer.Text = ce.Message;
				int eoffset = textview.Buffer.EndIter.Offset;
				if (!ce.IsNew && File.Exists (ce.File)) {
					textview.Buffer.Text += File.ReadAllText (ce.File);
					TextIter eiter = textview.Buffer.GetIterAtOffset (eoffset);
					textview.Buffer.ApplyTag (oldTextTag, eiter, textview.Buffer.EndIter);
				}
				textview.Buffer.MoveMark (editMark, textview.Buffer.GetIterAtOffset (eoffset));
				loading = false;
			}
		}
		
		public void OnTextChanged (object s, EventArgs a)
		{
			if (loading)
				return;
			TreeIter it;
			if (!fileList.Selection.GetSelected (out it))
				return;
			ChangeLogEntry ce = (ChangeLogEntry) store.GetValue (it, 0);
			changes [ce] = textview.Buffer.GetText (textview.Buffer.StartIter, textview.Buffer.GetIterAtMark (editMark), true);
		}
		
		protected override void OnResponse (ResponseType response_id)
		{
			if (response_id == ResponseType.Ok) {
				foreach (KeyValuePair<ChangeLogEntry,string> val in changes) {
					val.Key.Message = val.Value;
				}
			}
			base.OnResponse (response_id);
		}

	}
}
