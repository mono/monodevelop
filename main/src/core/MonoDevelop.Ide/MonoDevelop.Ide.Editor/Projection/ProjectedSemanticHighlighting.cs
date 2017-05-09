//
// ProjectedSemanticHighlighting.cs
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
using System.Linq;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor.Projection
{
	sealed class ProjectedSemanticHighlighting : SemanticHighlighting
	{
		List<Projection> projections;

		public ProjectedSemanticHighlighting (TextEditor editor, DocumentContext documentContext, IEnumerable<Projection> projections) : base (editor, documentContext)
		{
			this.projections = new List<Projection> (projections);
			foreach (var p in this.projections) {
				if (p.ProjectedEditor.SemanticHighlighting == null)
					continue;
				p.ProjectedEditor.SemanticHighlighting.SemanticHighlightingUpdated += HandleSemanticHighlightingUpdated;
			}
		}

		public void UpdateProjection (IEnumerable<Projection>  projections)
		{
			foreach (var p in this.projections) {
				if (p.ProjectedEditor.SemanticHighlighting == null)
					continue;
				p.ProjectedEditor.SemanticHighlighting.SemanticHighlightingUpdated -= HandleSemanticHighlightingUpdated;
			}
			this.projections = new List<Projection> (projections);
			foreach (var p in this.projections) {
				if (p.ProjectedEditor.SemanticHighlighting == null)
					continue;
				p.ProjectedEditor.SemanticHighlighting.SemanticHighlightingUpdated += HandleSemanticHighlightingUpdated;
			}
		}

		void HandleSemanticHighlightingUpdated (object sender, EventArgs e)
		{
			NotifySemanticHighlightingUpdate ();
		}

		protected override void DocumentParsed ()
		{
			NotifySemanticHighlightingUpdate ();
		}

		public override IEnumerable<ColoredSegment> GetColoredSegments (MonoDevelop.Core.Text.ISegment segment)
		{
			foreach (Projection p in projections) {
				foreach (var seg in p.ProjectedSegments) {
					if (seg.ContainsOriginal (segment.Offset) || 
					    seg.ContainsOriginal (segment.EndOffset) || 
					    segment.Offset <= seg.Offset && seg.Offset + seg.Length <= segment.EndOffset) {
						if (p.ProjectedEditor.SemanticHighlighting == null)
							continue;
						if (segment.Offset < seg.Offset) {
							foreach (var cs in GetColoredSegments (MonoDevelop.Core.Text.TextSegment.FromBounds (segment.Offset, seg.Offset - 1)))
								yield return cs;
						}


						var v = Math.Max (seg.Offset, segment.Offset);
						var projectedStartOffset = seg.FromOriginalToProjected (v);
						var projectedEndOffset = Math.Min (seg.FromOriginalToProjected (segment.EndOffset), seg.ProjectedOffset + seg.Length);
						var originalEndOffset = seg.FromProjectedToOriginal (projectedEndOffset);

						foreach (var cs in p.ProjectedEditor.SemanticHighlighting.GetColoredSegments (MonoDevelop.Core.Text.TextSegment.FromBounds (projectedStartOffset, projectedEndOffset))) {
							yield return new ColoredSegment (cs.Offset - projectedStartOffset + v, cs.Length, cs.ScopeStack);
						}

						if (originalEndOffset < segment.EndOffset) {
							foreach (var cs in GetColoredSegments (MonoDevelop.Core.Text.TextSegment.FromBounds (originalEndOffset, segment.EndOffset)))
								yield return cs;
						}
						yield break;
					}
				}
			}
		}
	}
}