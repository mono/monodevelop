//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Navigation;

namespace MonoDevelop.TextEditor
{
	partial class TextViewContent<TView, TImports>
	{
		public NavigationPoint BuildNavigationPoint ()
		{
			var document = TextView.TryGetParentDocument ();
			if (document == null)
				return null;
			return new TextViewNavigationPoint (document, TextView);
		}

		void TryLogNavPoint (bool transient)
		{
			if (TextView.Properties.TryGetProperty<Document> (typeof (Document), out var doc) && doc == Ide.IdeApp.Workbench.ActiveDocument) {
				NavigationHistoryService.LogNavigationPoint (new TextViewNavigationPoint (doc, TextView), transient);
			}
		}

		class TextViewNavigationPoint : DocumentNavigationPoint
		{
			ITextView textView;
			SnapshotPoint position;

			public TextViewNavigationPoint (Document document, ITextView textView) : base (document)
			{
				//FIXME should we use tracking points instead of SnapshotPoint & TranslateTo
				Initialize (textView, textView.Caret.Position.BufferPosition);
			}

			void Initialize (ITextView textView, SnapshotPoint position)
			{
				this.textView = textView;
				this.position = textView.Caret.Position.BufferPosition;
				CaptureLine ();
			}

			void CaptureLine ()
			{
				Offset = position;
				var line = position.Snapshot.GetLineFromPosition (position);
				Line = line.LineNumber;
			}

			void MakePositionCurrent () => position = position.TranslateTo (textView.TextBuffer.CurrentSnapshot, PointTrackingMode.Positive);

			protected override void OnDocumentClosing ()
			{
				// when the document is closed, update the position and capture it as a line/offset
				MakePositionCurrent ();
				CaptureLine ();

				// drop reference to the editor and snapshot so as not to leak them
				textView = null;
				position = default;
			}

			public int Line { get; private set; }
			public int Offset { get; private set; }

			// editor's line numbers are zero based, hence we add 1
			// FIXME: add a snippet of text
			public override string DisplayName => string.Format ("{0} : {1}", base.DisplayName, Line + 1);

			protected override async Task<Document> DoShow ()
			{
				var doc = await base.DoShow ();
				if (doc == null) {
					return doc;
				}

				var view = doc.GetContent<ITextView> ();
				if (view == null) {
					return doc;
				}

				var point = new SnapshotPoint (view.TextBuffer.CurrentSnapshot, Offset);
				view.Caret.MoveTo (point);
				view.Caret.EnsureVisible ();

				return doc;
			}

			public override bool ShouldReplace (NavigationPoint oldPoint)
			{
				//we can replace textview navpoints from the same file
				if (!(oldPoint is TextViewNavigationPoint tf) || tf.FileName != FileName) {
					return false;
				}

				// if it's detached (i.e the view that created it closed, but that file is now open again), reattach it
				if (tf.textView == null) {
					tf.SetDocument (Document);
					tf.Initialize (
						textView,
						new SnapshotPoint (
							textView.TextBuffer.CurrentSnapshot,
							Math.Min (textView.TextBuffer.CurrentSnapshot.Length, tf.Offset)
						)
					);
				}

				//replace the point if it's within five lines of this one
				return Math.Abs (Line - tf.Line) < 5;
			}

			public override bool Equals (object o) => o is TextViewNavigationPoint other && other.Offset == Offset && base.Equals (other);

			public override int GetHashCode () => Offset ^ base.GetHashCode ();
		}
	}
}