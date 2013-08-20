
// 
// ComparisonWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using Gdk;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.ComponentModel;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl.Views
{
	[ToolboxItem (true)]
	public class ComparisonWidget : EditorCompareWidgetBase
	{
		internal DropDownBox originalComboBox, diffComboBox;
		
		public TextEditor OriginalEditor {
			get {
				return editors[1];
			}
		}

		public TextEditor DiffEditor {
			get {
				return editors[0];
			}
		}

		protected override TextEditor MainEditor {
			get {
				return editors[1];
			}
		}

		public List<Mono.TextEditor.Utils.Hunk> Diff {
			get { return LeftDiff; }
			set { LeftDiff = value; }
		}
		
		protected override void CreateComponents ()
		{
			this.editors = new [] {
				new TextEditor (new TextDocument (), new CommonTextEditorOptions ()),
				new TextEditor (new TextDocument (), new CommonTextEditorOptions ()),
			};

			if (!viewOnly) {
				originalComboBox = new DropDownBox ();
				originalComboBox.WindowRequestFunc = CreateComboBoxSelector;
				originalComboBox.Text = "Local";
				originalComboBox.Tag = editors[1];
			
				diffComboBox = new DropDownBox ();
				diffComboBox.WindowRequestFunc = CreateComboBoxSelector;
				diffComboBox.Text = "Base";
				diffComboBox.Tag = editors[0];
			
				this.headerWidgets = new [] { diffComboBox, originalComboBox };
			}
		}
		
		public ComparisonWidget ()
		{
		}
		
		public ComparisonWidget (bool viewOnly) : base (viewOnly)
		{
			Intialize ();
		}
		
		
		public void GotoPrev () 
		{
			if (this.Diff == null)
				return;
			MainEditor.GrabFocus ();
			
			int line = MainEditor.Caret.Line;
			int max  = -1, searched = -1;
			foreach (var hunk in LeftDiff) {
				max = System.Math.Max (hunk.InsertStart, max);
				if (hunk.InsertStart < line)
					searched = System.Math.Max (hunk.InsertStart, searched);
			}
			if (max >= 0) {
				MainEditor.Caret.Line = searched < 0 ? max : searched;
				MainEditor.CenterToCaret ();
			}
		}
		
		public void GotoNext ()
		{
			if (this.Diff == null)
				return;
			MainEditor.GrabFocus ();
				
			int line = MainEditor.Caret.Line;
			int min  = Int32.MaxValue, searched = Int32.MaxValue;
			foreach (var hunk in this.LeftDiff) {
				min = System.Math.Min (hunk.InsertStart, min);
				if (hunk.InsertStart > line)
					searched = System.Math.Min (hunk.InsertStart, searched);
			}
			if (min < Int32.MaxValue) {
				MainEditor.Caret.Line = searched == Int32.MaxValue ? min : searched;
				MainEditor.CenterToCaret ();
			}
		}
		
		public override void UpdateDiff ()
		{
			CreateDiff ();
		}
		
		public override void CreateDiff () 
		{
			Diff = new List<Mono.TextEditor.Utils.Hunk> (DiffEditor.Document.Diff (OriginalEditor.Document));
			ClearDiffCache ();
			QueueDraw ();
		}
		
		public void SetRevision (TextEditor toEditor, Revision rev)
		{
			BackgroundWorker worker = new BackgroundWorker ();
			worker.DoWork += delegate(object sender, DoWorkEventArgs e) {
				Revision workingRevision = (Revision)e.Argument;
				string text = null;
				try {
					text = info.Item.Repository.GetTextAtRevision (info.VersionInfo.LocalPath, workingRevision);
				} catch (Exception ex) {
					text = string.Format (GettextCatalog.GetString ("Error while getting the text of revision {0}:\n{1}"), workingRevision, ex.ToString ());
					MessageService.ShowError (text);
				}
				e.Result = new KeyValuePair<Revision, string> (workingRevision, text);
			};
			
			worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e) {
				Application.Invoke (delegate {
					var result = (KeyValuePair<Revision, string>)e.Result;
					var box = toEditor == editors[0] ? diffComboBox : originalComboBox;
					RemoveLocal (toEditor.GetTextEditorData ());
					box.SetItem (string.Format (GettextCatalog.GetString ("Revision {0}\t{1}\t{2}"), result.Key, result.Key.Time, result.Key.Author), null, result.Key);
					toEditor.Text = result.Value;
					IdeApp.Workbench.StatusBar.AutoPulse = false;
					IdeApp.Workbench.StatusBar.EndProgress ();
					box.Sensitive = true;
					UpdateDiff ();
				});
			};
			
			worker.RunWorkerAsync (rev);
			IdeApp.Workbench.StatusBar.BeginProgress (string.Format (GettextCatalog.GetString ("Retrieving revision {0}..."), rev.ToString ()));
			IdeApp.Workbench.StatusBar.AutoPulse = true;
			
			if (toEditor == editors[0]) {
				diffRevision = rev;
			} else {
				originalRevision = rev;
			}
	
			var box2 = toEditor == editors[0] ? diffComboBox : originalComboBox;
			box2.Sensitive = false;
		}
		
		internal Revision originalRevision, diffRevision;
		
		class ComboBoxSelector : DropDownBoxListWindow.IListDataProvider
		{
			ComparisonWidget widget;
			DropDownBox box;
			
			public ComboBoxSelector (ComparisonWidget widget, DropDownBox box)
			{
				this.widget = widget;
				this.box = box;
				
			}

			#region IListDataProvider implementation
			public void Reset ()
			{
			}
	
			public string GetMarkup (int n)
			{
				if (n == 0)
					return "Local";
				if (n == 1)
					return "Base";
				Revision rev = widget.info.History[n - 2];
				return GLib.Markup.EscapeText (rev.ToString () + "\t" + rev.Time.ToString () + "\t" + rev.Author);
			}

			public Pixbuf GetIcon (int n)
			{
				return null;
			}

			public object GetTag (int n)
			{
				if (n < 2)
					return null;
				return widget.info.History[n - 2];
			}
			
			public void ActivateItem (int n)
			{
				if (n == 0) {
					box.SetItem ("Local", null, new object());
					widget.SetLocal (((TextEditor)box.Tag).GetTextEditorData ());
					return;
				}
				widget.RemoveLocal (((TextEditor)box.Tag).GetTextEditorData ());
				((TextEditor)box.Tag).Document.ReadOnly = true;
				if (n == 1) {
					box.SetItem ("Base", null, new object());
					if (((TextEditor)box.Tag) == widget.editors[0]) {
						widget.diffRevision = null;
					} else {
						widget.originalRevision = null;
					}
					string text;
					try {
						text = widget.info.Item.Repository.GetBaseText (widget.info.Item.Path);
					} catch (Exception ex) {
						text = string.Format (GettextCatalog.GetString ("Error while getting the base text of {0}:\n{1}"), widget.info.Item.Path, ex.ToString ());
						MessageService.ShowError (text);
					}
					
					((TextEditor)box.Tag).Document.Text = text;
					widget.CreateDiff ();
					return;
				}
				
				Revision rev = widget.info.History[n - 2];
				widget.SetRevision ((TextEditor)box.Tag, rev);
			}

			public int IconCount {
				get {
					return widget.info.History == null ? 2 : widget.info.History.Length + 2;
				}
			}
			#endregion
		}
		
		Gtk.Window CreateComboBoxSelector (DropDownBox box)
		{
			DropDownBoxListWindow window = new DropDownBoxListWindow (new ComboBoxSelector (this, box));
			return window;
		}
	}
}

