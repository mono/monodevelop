// MultiTaskProgressDialog.cs: Dialog for displaying the progress of multiple tasks.
//     N.B. Much of this code for this dialog is adapted from ProgressDialog.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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

using System;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class MultiTaskProgressDialog : Gtk.Dialog
	{
		ListStore statusStore = new ListStore (typeof(string), typeof(string), typeof (int));
		const int STORE_TaskName = 0; 
		const int STORE_TaskLabel = 1;
		const int STORE_TaskProgress = 2;
		TreeIter currentTaskIter;
		
		TextBuffer buffer;
		TextTag tag;
		TextTag bold;
		int ident = 0;
		List<TextTag> tags = new List<TextTag> ();
		Stack<string> indents = new Stack<string> ();
		IAsyncOperation asyncOperation;
		
		CellRendererText textRenderer;
		CellRendererProgress progressRenderer;
		
		IDictionary<string, string> taskLabelAliases = null;
		
		bool completed = false;
		bool allowCancel;
		                                
		public MultiTaskProgressDialog (bool allowCancel, bool showDetails, IDictionary<string, string> taskLabelAliases)
		{
			DispatchService.AssertGuiThread ();
			this.Build();
			this.allowCancel = allowCancel;
			
			this.taskLabelAliases = taskLabelAliases;
			detailsScroll.Visible = showDetails;
			
			buttonCancel.Visible = allowCancel;
			buttonClose.Sensitive = false;
			
			progressTreeView.Model = statusStore;
			progressTreeView.HeadersVisible = false;			
			textRenderer = new CellRendererText ();
			textRenderer.WrapMode = Pango.WrapMode.WordChar;
			TreeViewColumn textColumn = new TreeViewColumn ("Task", textRenderer, "markup", STORE_TaskLabel);
			textColumn.MinWidth = 292; // total width 400 with progressColumn width
			progressTreeView.AppendColumn (textColumn);
			
			progressRenderer = new CellRendererProgress ();
			progressRenderer.Xpad = 4;
			progressRenderer.Ypad = 4;
			TreeViewColumn progressColumn = new TreeViewColumn ("Progress", progressRenderer, "value", STORE_TaskProgress);
			progressColumn.MinWidth = 108; // 1 pixel per step, plus padding
			progressTreeView.AppendColumn (progressColumn);
			
			buffer = detailsTextView.Buffer;
			
			bold = new TextTag ("bold");
			bold.Weight = Pango.Weight.Bold;
			buffer.TagTable.Add (bold);
			
			tag = new TextTag ("0");
			tag.Indent = 10;
			buffer.TagTable.Add (tag);
			tags.Add (tag);
			
			int w,h;
			GetSize (out w, out h);
			Resize (w, 1);
		}
		
		public IAsyncOperation AsyncOperation {
			get { return asyncOperation; }
			set { asyncOperation = value; }
		}
		
		public string OperationTitle {
			get { return title.Text; }
			set {
				if (string.IsNullOrEmpty (value)) {
					if (title.Visible) {
						title.Hide ();
						title.Visible = false;
					}
				} else {
					title.Markup = "<big><b>" + value + "</b></big>";
					if (!title.Visible) {
						title.Visible = true;
						title.Show ();
					}
				}
			}
		}
		
		public void WriteText (string text)
		{
			AddText (text);
			if (text.EndsWith ("\n"))
				detailsTextView.ScrollMarkOnscreen (buffer.InsertMark);
		}
		
		public bool SetProgress (double fraction)
		{
			if (statusStore.IterIsValid (currentTaskIter)) {
				statusStore.SetValue (currentTaskIter, STORE_TaskProgress, System.Convert.ToInt32 (fraction * 100));
				return true;
			}
			return false;
		}
		
		public bool SetProgress (string taskName, double fraction)
		{
			TreeIter iter;
			if (statusStore.GetIterFirst (out iter)) {
				do {
					if (((string) statusStore.GetValue (iter, STORE_TaskName)) == taskName) {
						statusStore.SetValue (iter, STORE_TaskProgress, System.Convert.ToInt32 (fraction * 100));
						return true;
					}
				} while (statusStore.IterNext (ref iter));
			}
			return false;
		}
		
		public void BeginTask (string name)
		{
			if (name != null && name.Length > 0) {
				Indent ();
				indents.Push (name);
				if (taskLabelAliases != null && taskLabelAliases.ContainsKey (name))
					currentTaskIter = statusStore.AppendValues (name, taskLabelAliases [name], 0);
				else
					currentTaskIter = statusStore.AppendValues (name, name, 0);
			} else {
				indents.Push (null);
				currentTaskIter = TreeIter.Zero;
			}
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
				}
				currentTaskIter = TreeIter.Zero;
			}
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
		
		public void AllDone ()
		{
			completed = true;
			buttonCancel.Sensitive = false;
			buttonClose.Sensitive = true;
		}
		
		void OnCancel (object sender, EventArgs args)
		{
			if (asyncOperation != null)
				asyncOperation.Cancel ();
		}
		
		bool destroyed = false;
		void OnClose (object sender, EventArgs args)
		{
			if (!destroyed) {
				destroyed = true;
				Destroy ();
			}
		}
		
		[GLib.ConnectBefore]
		protected virtual void DeleteActivated (object o, DeleteEventArgs args)
		{
			args.RetVal = true;
			if (completed)
				OnClose (null, null);
			else if (allowCancel)
				OnCancel (null, null);
		}
	}
}
