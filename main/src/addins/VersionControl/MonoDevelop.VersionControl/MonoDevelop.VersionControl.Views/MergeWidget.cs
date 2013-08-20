//
// MergeWidget.cs
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
using System.Collections.Generic;
using Mono.TextEditor;
using Mono.TextEditor.Utils;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl.Views
{
	public class MergeWidget : EditorCompareWidgetBase
	{
		protected override TextEditor MainEditor {
			get {
				return editors != null && editors.Length >= 2 ? editors[1] : null;
			}
		}

		public MergeWidget ()
		{
			MainEditor.Document.TextReplaced += UpdateConflictsOnTextReplace;
		}

		protected override void UndoChange (TextEditor fromEditor, TextEditor toEditor, Hunk hunk)
		{
			base.UndoChange (fromEditor, toEditor, hunk);
			int i = leftConflicts.IndexOf (hunk);
			if (i < 0)
				i = rightConflicts.IndexOf (hunk);
			// no conflicting change
			if (i < 0)
				return;
			currentConflicts.RemoveAt (i);
/*			var startLine = MainEditor.Document.GetLineByOffset (hunk.InsertStart);
			var endline   = MainEditor.Document.GetLineByOffset (hunk.InsertStart + hunk.Inserted);
			
			currentConflicts[i].StartSegment.Offset = startLine.EndOffset;
			currentConflicts[i].EndSegment.Offset = endline.EndOffset;
						 */
			UpdateDiff ();
		}

		public void Load (VersionControlDocumentInfo info)
		{
			base.info = info;
			// SetLocal calls create diff & sets UpdateDiff handler -> should be connected after diff is created
			SetLocal (MainEditor.GetTextEditorData ());
			Show ();
		}

		public void Load (string fileName)
		{
			MainEditor.Document.MimeType = DesktopService.GetMimeTypeForUri (fileName);
			MainEditor.Document.Text = TextFileUtility.ReadAllText (fileName);

			this.CreateDiff ();
			Show ();
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (MainEditor != null && MainEditor.Document != null)
				MainEditor.Document.TextReplaced -= UpdateConflictsOnTextReplace;
		}
		
		public string GetResultText ()
		{
			return MainEditor.Text;
		}

		protected override void CreateComponents ()
		{
			this.editors = new [] {
				new TextEditor (new TextDocument (), new CommonTextEditorOptions ()),
				new TextEditor (new TextDocument (), new CommonTextEditorOptions ()),
				new TextEditor (new TextDocument (), new CommonTextEditorOptions ()),
			};
			
			this.editors[0].Document.ReadOnly = true;
			this.editors[2].Document.ReadOnly = true;
			
			Label myVersion = new Label (GettextCatalog.GetString ("My"));
			Label currentVersion = new Label (GettextCatalog.GetString ("Current"));
			Label conflictedVersion = new Label (GettextCatalog.GetString ("Theirs"));

			this.headerWidgets = new [] { myVersion, currentVersion, conflictedVersion };
		}

		// todo: move to version control backend
		static IEnumerable<Conflict> Conflicts (TextDocument doc)
		{
			int mergeStart = 0;
			while ((mergeStart = doc.IndexOf ("<<<<<<<", mergeStart, doc.TextLength - mergeStart, StringComparison.Ordinal)) >= 0) {
				DocumentLine start = doc.GetLineByOffset (mergeStart);
				if (start.Offset != mergeStart)
					continue;
				int dividerOffset = doc.IndexOf ("=======", mergeStart, doc.TextLength - mergeStart, StringComparison.Ordinal);
				DocumentLine divider = doc.GetLineByOffset (dividerOffset);

				int endOffset = doc.IndexOf (">>>>>>>", dividerOffset, doc.TextLength - dividerOffset, StringComparison.Ordinal);
				DocumentLine end = doc.GetLineByOffset (endOffset);
				mergeStart = dividerOffset + 1;

				yield return new Conflict (new TextSegment (start.EndOffsetIncludingDelimiter, divider.Offset - start.EndOffsetIncludingDelimiter),
					new TextSegment (divider.EndOffsetIncludingDelimiter, end.Offset - divider.EndOffsetIncludingDelimiter),
					start,
					divider,
					end);
			}
		}

		class Conflict
		{
			public TextSegment MySegment;
			public TextSegment TheirSegment;

			public TextSegment StartSegment;
			public TextSegment DividerSegment;
			public TextSegment EndSegment;

			public Conflict (TextSegment mySegment, TextSegment theirSegment, TextSegment startSegment, TextSegment dividerSegment, TextSegment endSegment)
			{
				this.MySegment = mySegment;
				this.TheirSegment = theirSegment;
				this.StartSegment = startSegment;
				this.DividerSegment = dividerSegment;
				this.EndSegment = endSegment;
			}
		}

		List<Conflict> currentConflicts = new List<Conflict> ();
		List<Hunk> leftConflicts = new List<Hunk> ();
		List<Hunk> rightConflicts = new List<Hunk> ();
		
		public override void UpdateDiff ()
		{
			LeftDiff  = new List<Hunk> (editors[0].Document.Diff (MainEditor.Document));
			RightDiff = new List<Hunk> (editors[2].Document.Diff (MainEditor.Document));

			DocumentLine line;
			LeftDiff.RemoveAll (item => null != (line = MainEditor.Document.GetLine (item.InsertStart)) &&
				currentConflicts.Any (c => c.StartSegment.Offset <= line.Offset && line.Offset < c.EndSegment.EndOffset));
			RightDiff.RemoveAll (item => null != (line = MainEditor.Document.GetLine (item.InsertStart)) &&
				currentConflicts.Any (c => c.StartSegment.Offset <= line.Offset && line.Offset < c.EndSegment.EndOffset));

			for (int i = 0; i < currentConflicts.Count; i++) {
				var curConflict = currentConflicts[i];
				int idx = i;// currentConflicts.Count - i - 1; // stored reverse

				var left = leftConflicts[idx];
				var right = rightConflicts[idx];

				int middleA = MainEditor.Document.OffsetToLineNumber (curConflict.StartSegment.Offset);
				int middleB = MainEditor.Document.OffsetToLineNumber (curConflict.EndSegment.Offset) + 1;

				LeftDiff.Add (new Hunk (left.RemoveStart, middleA, left.Removed, middleB - middleA));
				RightDiff.Add (new Hunk (right.RemoveStart, middleA, right.Removed, middleB - middleA));
			}
			base.UpdateDiff ();
			QueueDraw ();
		}

		public override void CreateDiff ()
		{
			int curOffset = 0;
			currentConflicts = new List<Conflict> (Conflicts (MainEditor.Document));
			leftConflicts.Clear ();
			rightConflicts.Clear ();
			editors[0].Document.Text = "";
			editors[2].Document.Text = "";

			for (int i = 0; i < currentConflicts.Count; i++) {
				Conflict conflict = currentConflicts[i];

				string above = MainEditor.Document.GetTextBetween (curOffset, conflict.StartSegment.Offset);
				editors[0].Insert (editors[0].Document.TextLength, above);
				int leftA = editors[0].Document.LineCount;
				editors[0].Insert (editors[0].Document.TextLength, MainEditor.Document.GetTextAt (conflict.MySegment));
				int leftB = editors[0].Document.LineCount;

				editors[2].Insert (editors[2].Document.TextLength, above);
				int rightA = editors[2].Document.LineCount;
				editors[2].Insert (editors[2].Document.TextLength, MainEditor.Document.GetTextAt (conflict.TheirSegment));
				int rightB = editors[2].Document.LineCount;

				int middleA = MainEditor.Document.OffsetToLineNumber (conflict.StartSegment.Offset);
				int middleB = MainEditor.Document.OffsetToLineNumber (conflict.EndSegment.EndOffset);

				leftConflicts.Add (new Hunk (leftA, middleA, leftB - leftA, middleB - middleA));
				rightConflicts.Add (new Hunk (rightA, middleA, rightB - rightA, middleB - middleA));
			}
			int endOffset = 0;
			if (currentConflicts.Count > 0)
				endOffset = currentConflicts.Last ().EndSegment.EndOffset;

			string lastPart = MainEditor.Document.GetTextBetween (endOffset, MainEditor.Document.TextLength);
			editors[0].Insert (editors[0].Document.TextLength, lastPart);
			editors[2].Insert (editors[2].Document.TextLength, lastPart);

			UpdateDiff ();
		}

		void UpdateConflictsOnTextReplace (object sender, DocumentChangeEventArgs e)
		{
			this.UpdateDiff ();
			foreach (var conflict in currentConflicts) {
				conflict.StartSegment = conflict.StartSegment.AdjustSegment (e);
				conflict.DividerSegment = conflict.DividerSegment.AdjustSegment (e);
				conflict.EndSegment = conflict.EndSegment.AdjustSegment (e);
				conflict.MySegment = conflict.MySegment.AdjustSegment (e);
				conflict.TheirSegment = conflict.TheirSegment.AdjustSegment (e);
			}
		}
	}
}