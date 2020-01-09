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
using MonoDevelop.Components.AtkCocoaHelper;
using System.ComponentModel;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl.Views
{
	[ToolboxItem (true)]
	class ComparisonWidget : EditorCompareWidgetBase
	{
		internal ComboBox originalComboBox, diffComboBox;
		ListStore revisionStore;

		public MonoTextEditor OriginalEditor {
			get {
				return editors[1];
			}
		}

		public MonoTextEditor DiffEditor {
			get {
				return editors[0];
			}
		}

		public ComboBox OriginalCombo {
			get {
				return originalComboBox;
			}
		}

		public ComboBox DiffCombo {
			get {
				return diffComboBox;
			}
		}

		internal override MonoTextEditor MainEditor {
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
			var options = GetTextEditorOptions ();
			this.editors = new [] {
				new MonoTextEditor (new TextDocument (), options),
				new MonoTextEditor (new TextDocument (), options),
			};
			editors [0].Accessible.SetShouldIgnore (false);
			editors [0].Accessible.SetRole (AtkCocoa.Roles.AXGroup);
			editors [0].Accessible.SetTitle (GettextCatalog.GetString ("Comparing Revision Editor"));
			editors [1].Accessible.SetShouldIgnore (false);
			editors [1].Accessible.SetRole (AtkCocoa.Roles.AXGroup);
			editors [1].Accessible.SetTitle (GettextCatalog.GetString ("Original Revision Editor"));

			if (!viewOnly) {
				revisionStore = new ListStore (typeof(Revision), typeof (string), typeof (string), typeof (string));
				revisionStore.AppendValues (GettextCatalog.GetString ("Loading…"), "", "");

				originalComboBox = new ComboBox (revisionStore);
				originalComboBox.Changed += OriginalComboBox_Changed;
				var revRenderer = new CellRendererText ();
				revRenderer.WidthChars = 62;
				originalComboBox.PackStart (revRenderer, false);
				originalComboBox.AddAttribute (revRenderer, "text", 1);

				var timeRenderer = new CellRendererText ();
				timeRenderer.WidthChars = 21;

				originalComboBox.PackStart (timeRenderer, false);
				originalComboBox.AddAttribute (timeRenderer, "text", 2);

				var authorRenderer = new CellRendererText ();
				originalComboBox.PackStart (authorRenderer, true);
				originalComboBox.AddAttribute (authorRenderer, "text", 3);

				originalComboBox.Accessible.SetTitle (GettextCatalog.GetString ("Original Revision"));

				originalComboBox.Active = 0;
				originalComboBox.Sensitive = false;

				diffComboBox = new ComboBox (revisionStore);
				diffComboBox.Changed += DiffComboBox_Changed;
				diffComboBox.PackStart (revRenderer, false);
				diffComboBox.AddAttribute (revRenderer, "text", 1);
				diffComboBox.PackStart (timeRenderer, false);
				diffComboBox.AddAttribute (timeRenderer, "text", 2);
				diffComboBox.PackStart (authorRenderer, true);
				diffComboBox.AddAttribute (authorRenderer, "text", 3);

				diffComboBox.Accessible.SetTitle (GettextCatalog.GetString ("Compared Revision"));

				diffComboBox.Active = 0;
				diffComboBox.Sensitive = false;
				this.headerWidgets = new [] { diffComboBox, originalComboBox };
			}
		}

		void DiffComboBox_Changed (object sender, EventArgs e)
		{
			Change (DiffEditor, diffComboBox.Active);
		}

		void OriginalComboBox_Changed (object sender, EventArgs e)
		{
			Change (MainEditor, originalComboBox.Active);
		}

		void Change (MonoTextEditor textEditor, int n)
		{
			if (n == 0) {
				SetLocal (textEditor.GetTextEditorData ());
				return;
			}

			RemoveLocal (textEditor.GetTextEditorData ());
			textEditor.Document.IsReadOnly = true;

			if (n == 1) {
				if (textEditor == editors [0]) {
					diffRevision = null;
				} else {
					originalRevision = null;
				}
				Task.Run (async () => {
					try {
						return await info.Item.Repository.GetBaseTextAsync (info.Item.Path);
					} catch (Exception ex) {
						var text = string.Format (GettextCatalog.GetString ("Error while getting the base text of {0}:\n{1}"), info.Item.Path, ex.ToString ());
						await Runtime.RunInMainThread (() => MessageService.ShowError (text));
						return text;
					}
				}).ContinueWith (t => {
					var editor = textEditor;
					if (editor.IsDisposed)
						return;
					editor.Document.Text = t.Result;
					CreateDiff ();
				}, Runtime.MainTaskScheduler);
				return;
			}

			var rev = info.History [n - 2];
			SetRevision (textEditor, rev);
		}

		protected override void OnSetVersionControlInfo (VersionControlDocumentInfo info)
		{
			info.Updated += OnInfoUpdated;
			MainEditor.Document.IsReadOnly = false;
			base.OnSetVersionControlInfo (info);
		}

		const int LocalIndex = 0;

		const int BaseIndex = 1;

		void OnInfoUpdated (object sender, EventArgs args)
		{
			revisionStore.Clear ();
			revisionStore.AppendValues (null, GettextCatalog.GetString ("Local"), "", "");
			revisionStore.AppendValues (null, GettextCatalog.GetString ("Base"), "", "");
			foreach (var revision in info.History) {
				revisionStore.AppendValues (revision, revision.ToString (), revision.Time.ToString (), revision.Author);
			}
			originalComboBox.Active = LocalIndex;
			diffComboBox.Active = BaseIndex;
			originalComboBox.Sensitive = diffComboBox.Sensitive = true;
		}

		protected override void OnDestroyed ()
		{
			originalComboBox.Changed -= OriginalComboBox_Changed;
			diffComboBox.Changed -= DiffComboBox_Changed;
			info.Updated -= OnInfoUpdated;
			base.OnDestroyed ();
		}

		public ComparisonWidget ()
		{
		}

		public ComparisonWidget (bool viewOnly) : base (viewOnly)
		{
			Intialize ();
		}


		public void GotoNext ()
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

		public void GotoPrev ()
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
			Diff = new List<Mono.TextEditor.Utils.Hunk> (DiffEditor.Document.Diff (OriginalEditor.Document, includeEol: false));
			ClearDiffCache ();
			QueueDraw ();
		}

		public void SetRevision (MonoTextEditor toEditor, Revision rev)
		{
			IdeApp.Workbench.StatusBar.BeginProgress (string.Format (GettextCatalog.GetString ("Retrieving revision {0}..."), rev.ToString ()));
			IdeApp.Workbench.StatusBar.AutoPulse = true;

			if (toEditor == editors[0]) {
				diffRevision = rev;
			} else {
				originalRevision = rev;
			}

			var box2 = toEditor == editors[0] ? diffComboBox : originalComboBox;
			box2.Sensitive = false;

			Task.Run (async delegate {
				string text = null;
				try {
					text = await info.Item.Repository.GetTextAtRevisionAsync ((await info.Item.GetVersionInfoAsync ()).LocalPath, rev);
				} catch (Exception ex) {
					text = string.Format (GettextCatalog.GetString ("Error while getting the text of revision {0}:\n{1}"), rev, ex.ToString ());
					MessageService.ShowError (text);
					return;
				}

				Runtime.RunInMainThread (() => {
					var box = toEditor == editors [0] ? diffComboBox : originalComboBox;
					RemoveLocal (toEditor.GetTextEditorData ());
					for (int i = 0; i < info.History.Length; i++) {
						if (info.History [i].Time == rev.Time) {
							box.Active = 2 + i;
							break;
						}
					}
					toEditor.Text = text;
					IdeApp.Workbench.StatusBar.AutoPulse = false;
					IdeApp.Workbench.StatusBar.EndProgress ();
					IdeApp.Workbench.StatusBar.ShowReady ();
					box.Sensitive = true;
					UpdateDiff ();
				}).Ignore ();
			}).Ignore ();
		}

		internal Revision originalRevision, diffRevision;
	}
}
