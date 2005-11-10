//
// ProgressDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using Gtk;
using Glade;

namespace MonoDevelop.Core.Gui.Dialogs
{
	class ProgressDialog : IDisposable
	{
		[Glade.Widget ("ProgressDialog")] Dialog dialog;
		[Glade.Widget] Button btnCancel;
		[Glade.Widget] Button btnClose;
		[Glade.Widget] Label label;
		[Glade.Widget] Gtk.TextView detailsTextView;
		[Glade.Widget] Gtk.Expander expander;
		[Glade.Widget] Gtk.ProgressBar progressBar;
		
		Gtk.TextBuffer buffer;
		
		TextTag tag;
		TextTag bold;
		int ident = 0;
		ArrayList tags = new ArrayList ();
		Stack indents = new Stack ();
		IAsyncOperation asyncOperation;
		
		public ProgressDialog (bool allowCancel, bool showDetails): this (null, allowCancel, showDetails)
		{
		}
		
		public ProgressDialog (Window parent, bool allowCancel, bool showDetails)
		{
			new Glade.XML (null, "Base.glade", "ProgressDialog", null).Autoconnect (this);
			dialog.TransientFor = parent;
			
			btnCancel.Clicked += new EventHandler (OnCancel);
			btnCancel.Visible = allowCancel;

			btnClose.Clicked += new EventHandler (OnClose);
			
			expander.Activated += new EventHandler (OnExpanded);
			expander.Visible = showDetails;
			
			buffer = detailsTextView.Buffer;
			detailsTextView.Editable = false;
			
			bold = new TextTag ("bold");
			bold.Weight = Pango.Weight.Bold;
			buffer.TagTable.Add (bold);
			
			tag = new TextTag ("0");
			tag.Indent = 10;
			buffer.TagTable.Add (tag);
			tags.Add (tag);
		}
		
		public IAsyncOperation AsyncOperation {
			get { return asyncOperation; }
			set { asyncOperation = value; }
		}
		
		public string Message {
			get { return label.Text; }
			set { label.Text = value; }
		}
		
		public double Progress {
			get { return progressBar.Fraction; }
			set { progressBar.Fraction = value; }
		}
		
		public void BeginTask (string name)
		{
			if (name != null && name.Length > 0) {
				Indent ();
				indents.Push (name);
				Message = name;
			} else
				indents.Push (null);

			if (name != null) {
				TextIter it = buffer.EndIter;
				string txt = name + "\n";
				buffer.InsertWithTags (ref it, txt, tag, bold);
				detailsTextView.ScrollMarkOnscreen (buffer.InsertMark);
			}
		}
		
		public void EndTask ()
		{
			if (indents.Count > 0) {
				string msg = (string) indents.Pop ();
				if (msg != null) {
					Unindent ();
					Message = msg;
				}
			}
		}
		
		public void WriteText (string text)
		{
			AddText (text);
			if (text.EndsWith ("\n"))
				detailsTextView.ScrollMarkOnscreen (buffer.InsertMark);
		}
		
		void AddText (string s)
		{
			TextIter it = buffer.EndIter;
			buffer.InsertWithTags (ref it, s, tag);
		}
		
		void Indent ()
		{
			ident++;
			if (ident >= tags.Count) {
				tag = new TextTag (ident.ToString ());
				tag.Indent = 10 + 15 * (ident - 1);
				buffer.TagTable.Add (tag);
				tags.Add (tag);
			} else {
				tag = (TextTag) tags [ident];
			}
		}
		
		void Unindent ()
		{
			if (ident >= 0) {
				ident--;
				tag = (TextTag) tags [ident];
			}
		}
		
		public void Show ()
		{
			dialog.Show ();
		}
		
		public void Run ()
		{
			dialog.Show ();
			dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
			dialog.Dispose ();
		}
		
		public void ShowDone (bool warnings, bool errors)
		{
			btnCancel.Hide ();
			btnClose.Show ();

			if (errors)
				label.Text = GettextCatalog.GetString ("Operation completed with errors.");
			else if (warnings)
				label.Text = GettextCatalog.GetString ("Operation completed with warnings.");
			else
				label.Text = GettextCatalog.GetString ("Operation successfully completed.");
		}
		
		void OnCancel (object sender, EventArgs args)
		{
			if (asyncOperation != null)
				asyncOperation.Cancel ();
		}
		
		void OnClose (object sender, EventArgs args)
		{
			Dispose ();
		}
		
		void OnExpanded (object sender, EventArgs args)
		{
			GLib.Timeout.Add (100, new GLib.TimeoutHandler (UpdateSize));
		}
		
		bool UpdateSize ()
		{
			int w, h;
			dialog.GetSize (out w, out h);
			dialog.Resize (w, 1);
			return false;
		}
	}
}
