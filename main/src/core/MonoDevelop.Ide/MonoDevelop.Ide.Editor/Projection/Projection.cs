//
// Projection.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Editor;
using System.Collections.Immutable;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor.Projection
{
	public sealed class Projection
	{
		public ITextDocument Document { get; private set; }

		SegmentTree<ProjectedTreeSegment> originalProjections  = new SegmentTree<ProjectedTreeSegment> ();
		SegmentTree<ProjectedTreeSegment> projectedProjections = new SegmentTree<ProjectedTreeSegment> ();

		class ProjectedTreeSegment : TreeSegment
		{
			public ProjectedTreeSegment LinkedTo { get; set; }

			public ProjectedTreeSegment (int offset, int length) : base (offset, length)
			{
			}
		}

		public IEnumerable<ProjectedSegment> ProjectedSegments {
			get {
				foreach (var treeSeg in originalProjections) {
					yield return new ProjectedSegment (treeSeg.Offset, treeSeg.LinkedTo.Offset, treeSeg.Length);
				}
			}
		}

		TextEditor projectedEditor;

		internal TextEditor ProjectedEditor
		{
			get
			{
				return projectedEditor;
			}
		}

		ProjectedDocumentContext projectedDocumentContext;
		TextEditor attachedEditor;

		internal DocumentContext ProjectedContext {
			get {
				return projectedDocumentContext;
			}
		}

		public TextEditor CreateProjectedEditor (DocumentContext originalContext)
		{
			if (projectedEditor == null) {
				projectedEditor = TextEditorFactory.CreateNewEditor (Document, TextEditorType.Projection);
				projectedDocumentContext = new ProjectedDocumentContext (projectedEditor, originalContext);
				projectedEditor.InitializeExtensionChain (projectedDocumentContext);
				projectedProjections.InstallListener (projectedEditor);
			}
			return projectedEditor;
		}

		public Projection (ITextDocument document, IReadOnlyList<ProjectedSegment> projectedSegments)
		{
			if (document == null)
				throw new ArgumentNullException (nameof (document));
			this.Document = document;

			for (int i = 0; i < projectedSegments.Count; i++) {
				var p = projectedSegments [i];
				var original = new ProjectedTreeSegment (p.Offset, p.Length);
				var projected =  new ProjectedTreeSegment (p.ProjectedOffset, p.Length);
				original.LinkedTo = projected;
				projected.LinkedTo = original;
				originalProjections.Add (original);
				projectedProjections.Add (projected);
			}
		}

		internal void Dettach ()
		{
			attachedEditor.TextChanging -= HandleTextChanging;
		}

		internal void Attach (TextEditor textEditor)
		{
			attachedEditor = textEditor;
			attachedEditor.TextChanging += HandleTextChanging;
		}

		void HandleTextChanging (object sender, TextChangeEventArgs e)
		{
			foreach (var change in e.TextChanges) {
				foreach (var segment in originalProjections) {
					if (segment.Contains (change.Offset)) {
						var projectedOffset = change.Offset - segment.Offset + segment.LinkedTo.Offset;
						projectedEditor.ReplaceText (projectedOffset, change.RemovalLength, change.InsertedText);
					}
				}
			}

			originalProjections.UpdateOnTextReplace (sender, e);
		}

		public bool TryConvertFromProjectionToOriginal (int projectedOffset, out int originalOffset)
		{
			foreach (var pseg in ProjectedSegments) {
				if (pseg.ContainsProjected (projectedOffset)) {
					originalOffset = pseg.FromProjectedToOriginal (projectedOffset);
					return true;
				}
			}
			originalOffset = -1;
			return false;
		}

		public bool TryConvertFromOriginalToProjection (int originalOffset, out int projectedOffset)
		{
			foreach (var pseg in ProjectedSegments) {
				if (pseg.ContainsOriginal (originalOffset)) {
					projectedOffset = pseg.FromOriginalToProjected (originalOffset);
					return true;
				}
			}
			projectedOffset = -1;
			return false;
		}
	}
}