// 
// ProgressDialog.cs
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
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using System.Threading;
#if MAC
using AppKit;
#endif

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class ProgressDialog : Gtk.Dialog
	{
		Gtk.TextBuffer buffer;
		
		TextTag tag;
		TextTag bold;
		int ident = 0;
		List<TextTag> tags = new List<TextTag> ();
		Stack<string> indents = new Stack<string> ();
		CancellationTokenSource cancellationTokenSource;
		private MonoDevelop.Components.Window componentsWindowParent;

		public ProgressDialog (bool allowCancel, bool showDetails): this (null, allowCancel, showDetails)
		{
		}
		
		public ProgressDialog (MonoDevelop.Components.Window parent, bool allowCancel, bool showDetails)
		{
			MonoDevelop.Components.IdeTheme.ApplyTheme (this);
			this.Build ();
			this.Title = BrandingService.ApplicationName;
			this.componentsWindowParent = parent; 
			HasSeparator = false;
			ActionArea.Hide ();
			DefaultHeight = 5;
			
			btnCancel.Visible = allowCancel;

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
		
		public CancellationTokenSource CancellationTokenSource {
			get { return cancellationTokenSource; }
			set { cancellationTokenSource = value; }
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
		
		public void ShowDone (bool warnings, bool errors)
		{
			progressBar.Fraction = 1;
			
			btnCancel.Hide ();
			btnClose.Show ();

			if (errors)
				label.Text = GettextCatalog.GetString ("Operation completed with errors.");
			else if (warnings)
				label.Text = GettextCatalog.GetString ("Operation completed with warnings.");
			else
				label.Text = GettextCatalog.GetString ("Operation successfully completed.");
		}
		
		protected void OnBtnCancelClicked (object sender, EventArgs e)
		{
			if (cancellationTokenSource != null)
				cancellationTokenSource.Cancel ();
		}
		
		bool UpdateSize ()
		{
			int w, h;
			GetSize (out w, out h);
			Resize (w, 1);
			return false;
		}
		
		protected virtual void OnExpander1Activated (object sender, System.EventArgs e)
		{
			GLib.Timeout.Add (100, new GLib.TimeoutHandler (UpdateSize));
		}
		
		protected virtual void OnBtnCloseClicked (object sender, System.EventArgs e)
		{
			Destroy ();
		}

		protected override void OnShown ()
		{
			base.OnShown ();
			this.SetParentToWindow (componentsWindowParent);
		}
	}
}
