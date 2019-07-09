// 
// TextFileNavigationPoint.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//   Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;
using MonoDevelop.Core.Text;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Ide.Composition;
using Microsoft.VisualStudio.Text.Operations;

namespace MonoDevelop.Ide.Navigation
{
	public class TextFileNavigationPoint : DocumentNavigationPoint
	{
		int line;
		int column;

		SnapshotPoint? offset;

		public TextFileNavigationPoint (Document doc, ITextView textView)
			: base (doc)
		{
			offset = textView.Caret.Position.BufferPosition;
		}

		protected override void OnDocumentClosing ()
		{
			try {
				// text source version becomes invalid on document close.
				if (this.offset is SnapshotPoint point) {
					var currentSnapshot = point.Snapshot.TextBuffer.CurrentSnapshot;
					var translatedOffset = point.TranslateTo (currentSnapshot, PointTrackingMode.Positive);
					var snapshotLine = currentSnapshot.GetLineFromPosition (translatedOffset);
					line = snapshotLine.LineNumber;
					column = translatedOffset - snapshotLine.Start.Position;
					offset = null;
				}
			} catch (Exception e) {
				LoggingService.LogInternalError (e);
			}
		}

		public TextFileNavigationPoint (FilePath file, int line, int column)
			: base (file)
		{
			this.line = line;
			this.column = column;
		}

		public override bool ShouldReplace (NavigationPoint oldPoint)
		{
			var tf = oldPoint as TextFileNavigationPoint;
			if (tf == null)
				return false;
			var line1 = this.line;
			if (this.offset is SnapshotPoint sp1) {
				var currentSnapshot = sp1.Snapshot.TextBuffer.CurrentSnapshot;
				var translatedOffset1 = sp1.TranslateTo (currentSnapshot, PointTrackingMode.Positive);
				line1 = currentSnapshot.GetLineNumberFromPosition (translatedOffset1);
			}
			var line2 = tf.line;
			if (tf.offset is SnapshotPoint sp2) {
				var currentSnapshot = sp2.Snapshot.TextBuffer.CurrentSnapshot;
				var translatedOffset2 = sp2.TranslateTo (currentSnapshot, PointTrackingMode.Positive);
				line2 = currentSnapshot.GetLineNumberFromPosition (translatedOffset2);
			}
			return base.Equals (tf) && Math.Abs (line1 - line2) < 5;
		}
		
		
		public int Line {
			get { return line; }
		}
		
		public int Column {
			get { return this.column; }
		}
		
		public override string DisplayName {
			get {
				return string.Format ("{0} : {1}", base.DisplayName, Line);
			}
		}
		
		protected override async Task<Document> DoShow ()
		{
			var doc = await base.DoShow ();
			if (doc != null) {
				doc.RunWhenContentAdded<ITextView> (textView => {
					doc.DisableAutoScroll ();
					JumpToCurrentLocation (textView);
				});
			}
			return doc;
		}

		protected void JumpToCurrentLocation (ITextView textView)
		{
			var editorOperationsFactoryService = CompositionManager.Instance.GetExportedValue<IEditorOperationsFactoryService> ();
			var editorOperations = editorOperationsFactoryService.GetEditorOperations (textView);
			VirtualSnapshotPoint point;
			if (offset is SnapshotPoint sp1 && sp1.Snapshot.TextBuffer == textView.TextBuffer) {
				var currentSnapshot = textView.TextBuffer.CurrentSnapshot;
				var sp = sp1.TranslateTo (currentSnapshot, PointTrackingMode.Positive);
				point = new VirtualSnapshotPoint (currentSnapshot, sp);
			} else {
				var snapshotLine = textView.TextSnapshot.GetLineFromLineNumber (Math.Min (textView.TextSnapshot.LineCount - 1, this.line));
				point = new VirtualSnapshotPoint (textView.TextSnapshot, Math.Min (textView.TextSnapshot.Length - 1, snapshotLine.Start.Position + column));
				offset = point.Position;
			}
			editorOperations.SelectAndMoveCaret (point, point, TextSelectionMode.Stream, EnsureSpanVisibleOptions.AlwaysCenter);
		}

		public override bool Equals (object o)
		{
			var other = o as TextFileNavigationPoint;
			if (other == null)
				return false;
			if (this.offset.HasValue != other.offset.HasValue)
				return false;
			if (this.offset.HasValue && !other.offset.Value.Equals (offset.Value))
				return false;
			return base.Equals (other);
		}
		
		public override int GetHashCode ()
		{
			unchecked {
				return line + base.GetHashCode ();
			}
		}
	}
}
