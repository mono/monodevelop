// 
// DiffWidget.cs
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
using System.Linq;
using Gtk;
using Gdk;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Components.Diff;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.ComponentModel;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Views
{
	class ComparisonWidget : EditorCompareWidgetBase
	{
		VersionControlDocumentInfo info;
		
		DropDownBox originalComboBox, diffComboBox;
		
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
			get { return leftDiff; }
			set { leftDiff = value; }
		}
		
		protected ComparisonWidget (IntPtr ptr) : base (ptr)
		{
		}
		
		protected override void CreateComponents ()
		{
			this.editors = new [] { new TextEditor (), new TextEditor ()};
			
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
		
		public ComparisonWidget (VersionControlDocumentInfo info)
		{
			this.info = info;
		
		/*	prev = new Button ();
			prev.Add (new Arrow (ArrowType.Up, ShadowType.None));
			AddChild (prev);
			prev.ShowAll ();
			prev.Clicked += delegate {
				if (this.Diff == null)
					return;
				originalEditor.GrabFocus ();
				
				int line = originalEditor.Caret.Line;
				int max  = -1, searched = -1;
				foreach (Diff.Hunk hunk in this.Diff) {
					if (hunk.Same)
						continue;
					max = System.Math.Max (hunk.Right.Start, max);
					if (hunk.Right.Start < line)
						searched = System.Math.Max (hunk.Right.Start, searched);
				}
				if (max >= 0) {
					originalEditor.Caret.Line = searched < 0 ? max : searched;
					originalEditor.CenterToCaret ();
				}
			};
			
			next = new Button ();
			next.BorderWidth = 0;
			next.Add (new Arrow (ArrowType.Down, ShadowType.None));
			next.Clicked += delegate {
				if (this.Diff == null)
					return;
				originalEditor.GrabFocus ();
				
				int line = originalEditor.Caret.Line;
				int min  = Int32.MaxValue, searched = Int32.MaxValue;
				foreach (Diff.Hunk hunk in this.Diff) {
					if (hunk.Same)
						continue;
					min = System.Math.Min (hunk.Right.Start, min);
					if (hunk.Right.Start > line)
						searched = System.Math.Min (hunk.Right.Start, searched);
				}
				if (min < Int32.MaxValue) {
					originalEditor.Caret.Line = searched == Int32.MaxValue ? min : searched;
					originalEditor.CenterToCaret ();
				}
			};
			AddChild (next);
			next.ShowAll ();*/
		}
		
		
		List<TextEditorData> localUpdate = new List<TextEditorData> ();

		void HandleInfoDocumentTextEditorDataDocumentTextReplaced (object sender, ReplaceEventArgs e)
		{
			foreach (var data in localUpdate.ToArray ()) {
				data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
				data.Replace (e.Offset, e.Count, e.Value);
				data.Document.TextReplaced += HandleDataDocumentTextReplaced;
				data.Document.CommitUpdateAll ();
			}
		}
		
		public override void UpdateDiff ()
		{
			 CreateDiff ();
		}
		
		public override void CreateDiff () 
		{
			Diff = new List<Mono.TextEditor.Utils.Hunk> (OriginalEditor.Document.Diff (DiffEditor.Document));
			QueueDraw ();
		}
		
		Dictionary<Mono.TextEditor.Document, TextEditorData> dict = new Dictionary<Mono.TextEditor.Document, TextEditorData> ();
		public void SetLocal (TextEditorData data)
		{
			dict[data.Document] = data;
			data.Document.Text = info.Document.Editor.Document.Text;
			data.Document.ReadOnly = false;
			data.Document.TextReplaced += HandleDataDocumentTextReplaced;
			CreateDiff ();
		}
		
		void HandleDataDocumentTextReplaced (object sender, ReplaceEventArgs e)
		{
			var data = dict[(Document)sender];
			localUpdate.Remove (data);
			info.Document.Editor.Replace (e.Offset, e.Count, e.Value);
			localUpdate.Add (data);
			CreateDiff ();
		}
		
		public void RemoveLocal (TextEditorData data)
		{
			localUpdate.Remove (data);
			data.Document.ReadOnly = true;
			data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
		}
		
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
	
			public string GetText (int n)
			{
				if (n == 0)
					return "Local";
				if (n == 1)
					return "Base";
				Revision rev = widget.info.History[n - 2];
				return rev.ToString () + "\t" + rev.Time.ToString () + "\t" + rev.Author;
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
					((TextEditor)box.Tag).Document.Text = widget.info.Item.Repository.GetBaseText (widget.info.Item.Path);
					widget.CreateDiff ();
					return;
				}
				
				BackgroundWorker worker = new BackgroundWorker ();
				worker.DoWork += HandleWorkerDoWork;
				worker.RunWorkerCompleted += HandleWorkerRunWorkerCompleted;
				Revision rev = widget.info.History[n - 2];
				worker.RunWorkerAsync (rev);
				IdeApp.Workbench.StatusBar.BeginProgress (string.Format (GettextCatalog.GetString ("Retrieving revision {0}..."), rev.ToString ()));
				IdeApp.Workbench.StatusBar.AutoPulse = true;
				box.Sensitive = false;
			}

			void HandleWorkerRunWorkerCompleted (object sender, RunWorkerCompletedEventArgs e)
			{
				Application.Invoke (delegate {
					var result = (KeyValuePair<Revision, string>)e.Result;
					box.SetItem (string.Format (GettextCatalog.GetString ("Revision {0}\t{1}\t{2}"), result.Key, result.Key.Time, result.Key.Author), null, result.Key);
					((TextEditor)box.Tag).Document.Text = result.Value;
					widget.CreateDiff ();
					IdeApp.Workbench.StatusBar.AutoPulse = false;
					IdeApp.Workbench.StatusBar.EndProgress ();
					box.Sensitive = true;
				});
			}

			void HandleWorkerDoWork (object sender, DoWorkEventArgs e)
			{
				Revision rev = (Revision)e.Argument;
				string text = null;
				try {
					Console.WriteLine (widget.info.VersionInfo.RepositoryPath);
					text = widget.info.Item.Repository.GetTextAtRevision (widget.info.VersionInfo.LocalPath, rev);
				} catch (Exception ex) {
					text = "Error retrieving revision " + rev + Environment.NewLine + ex.ToString ();
				}
				e.Result = new KeyValuePair<Revision, string> (rev, text);
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

