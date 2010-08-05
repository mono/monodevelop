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
using Gdk;
using System.Collections.Generic;
using Mono.TextEditor;
using Mono.TextEditor.Utils;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.ComponentModel;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Views
{
	[ToolboxItem (true)]
	public class MergeWidget : EditorCompareWidgetBase
	{
		protected override TextEditor MainEditor {
			get {
				return editors[1];
			}
		}

		protected MergeWidget (IntPtr ptr) : base (ptr)
		{
		}
		
		public MergeWidget () : base (null)
		{
		}

		public void Load (VersionControlDocumentInfo info)
		{
			base.info = info;
			
			SetLocal (MainEditor.GetTextEditorData ());
			
			this.CreateDiff ();
			MainEditor.Document.TextReplaced += delegate {
				this.UpdateDiff ();
			};
			Show ();
		}

		public void Load (string fileName)
		{
			MainEditor.Document.MimeType = DesktopService.GetMimeTypeForUri (fileName);
			MainEditor.Document.Text = System.IO.File.ReadAllText (fileName);
			
			this.CreateDiff ();
			MainEditor.Document.TextReplaced += delegate {
				this.UpdateDiff ();
			};
			Show ();
		}

		protected override void CreateComponents ()
		{
			this.editors = new [] { new TextEditor (), new TextEditor (), new TextEditor () };
			
			Label myVersion = new Label (GettextCatalog.GetString ("My"));
			Label currentVersion = new Label (GettextCatalog.GetString ("Current"));
			Label conflictedVersion = new Label (GettextCatalog.GetString ("Conflict"));
			
			this.headerWidgets = new [] { myVersion, currentVersion, conflictedVersion };
		}
		
		// todo: move to version control backend
		IEnumerable<Conflict> Conflicts (Mono.TextEditor.Document doc)
		{
			foreach (int mergeStart in doc.SearchForward ("<<<<<<<", 0)) {
				LineSegment start = doc.GetLineByOffset (mergeStart);
				if (start.Offset != mergeStart)
					continue;
				int dividerOffset = doc.SearchForward ("=======", mergeStart).First ();
				LineSegment divider = doc.GetLineByOffset (dividerOffset);
				
				int endOffset      = doc.SearchForward (">>>>>>>", dividerOffset).First (); 
				LineSegment end = doc.GetLineByOffset (endOffset);
				
				yield return new Conflict (new Mono.TextEditor.Segment (start.EndOffset, divider.Offset - start.EndOffset),
					new Mono.TextEditor.Segment (divider.EndOffset, end.Offset - divider.EndOffset), start.Offset, end.EndOffset);
			}
		}

		class Conflict
		{
			public readonly Mono.TextEditor.Segment MySegment;
			public readonly Mono.TextEditor.Segment TheirSegment;
			public readonly int StartOffset;
			public readonly int EndOffset;
			
			public Conflict (Mono.TextEditor.Segment mySegment, Mono.TextEditor.Segment theirSegment, int startOffset, int endOffset)
			{
				this.MySegment = mySegment;
				this.TheirSegment = theirSegment;
				this.StartOffset = startOffset;
				this.EndOffset = endOffset;
			}
		}

		List<Conflict> currentConflicts = new List<Conflict> ();
		Dictionary<Conflict, Mono.TextEditor.Utils.Hunk> leftConflicts = new Dictionary<Conflict, Mono.TextEditor.Utils.Hunk> ();
		Dictionary<Conflict, Mono.TextEditor.Utils.Hunk> rightConflicts = new Dictionary<Conflict, Mono.TextEditor.Utils.Hunk> ();

		public override void UpdateDiff ()
		{
			var conflicts = new List<Conflict> (Conflicts (MainEditor.Document));
			
			leftDiff  = new List<Mono.TextEditor.Utils.Hunk> (editors[0].Document.Diff (MainEditor.Document));
			rightDiff = new List<Mono.TextEditor.Utils.Hunk> (editors[2].Document.Diff (MainEditor.Document));
			
			LineSegment line;
			leftDiff.RemoveAll (item => null != (line = MainEditor.Document.GetLine (item.InsertStart)) && 
				conflicts.Any (c => c.StartOffset <= line.Offset && line.Offset < c.EndOffset));
			rightDiff.RemoveAll (item => null != (line = MainEditor.Document.GetLine (item.InsertStart)) && 
				conflicts.Any (c => c.StartOffset <= line.Offset && line.Offset < c.EndOffset));
			
			int j = 0;
			for (int i = 0; i < currentConflicts.Count && j < conflicts.Count;) {
				var curConflict = currentConflicts[i];
				var newConflict = conflicts[j];
				
				if (curConflict.EndOffset - curConflict.StartOffset == newConflict.EndOffset - newConflict.StartOffset) {
					var left = leftConflicts[curConflict];
					var right = rightConflicts[curConflict];
					
					int middleA = MainEditor.Document.OffsetToLineNumber (newConflict.StartOffset);
					int middleB = MainEditor.Document.OffsetToLineNumber (newConflict.EndOffset);
				
					leftDiff.Add (new Mono.TextEditor.Utils.Hunk (left.RemoveStart, middleA, left.Removed, middleB - middleA));
					rightDiff.Add (new Mono.TextEditor.Utils.Hunk (right.RemoveStart, middleA, right.Removed, middleB - middleA));
					i++;j++;
				} else {
					j++;
				}
			}
			QueueDraw ();
		}
		
		public override void CreateDiff ()
		{
			int curOffset = 0;
			var conflicts = new List<Conflict> (Conflicts (MainEditor.Document));
			currentConflicts = conflicts;
			leftConflicts.Clear ();
			rightConflicts.Clear ();
			
			editors[0].Document.Text = "";
			editors[2].Document.Text = "";
			foreach (Conflict conflict in currentConflicts) {
				string above = MainEditor.Document.GetTextBetween (curOffset, conflict.StartOffset);
				editors[0].Insert (editors[0].Document.Length, above);
				int leftA = editors[0].Document.LineCount - 1;
				editors[0].Insert (editors[0].Document.Length, MainEditor.Document.GetTextAt (conflict.MySegment));
				int leftB = editors[0].Document.LineCount - 1;
				
				editors[2].Insert (editors[2].Document.Length, above);
				int rightA = editors[2].Document.LineCount - 1;
				editors[2].Insert (editors[2].Document.Length, MainEditor.Document.GetTextAt (conflict.TheirSegment));
				int rightB = editors[2].Document.LineCount - 1;
				
				int middleA = MainEditor.Document.OffsetToLineNumber (conflict.StartOffset);
				int middleB = MainEditor.Document.OffsetToLineNumber (conflict.EndOffset);
				
				leftConflicts[conflict] = new Mono.TextEditor.Utils.Hunk (leftA, middleA, leftB - leftA, middleB - middleA);
				rightConflicts[conflict] = new Mono.TextEditor.Utils.Hunk (rightA, middleA, rightB - rightA, middleB - middleA);
				
				curOffset = conflict.EndOffset;
			}
			
			string lastPart = MainEditor.Document.GetTextBetween (curOffset, MainEditor.Document.Length);
			editors[0].Insert (editors[0].Document.Length, lastPart);
			editors[2].Insert (editors[2].Document.Length, lastPart);

			UpdateDiff ();
		}
	
	}
}