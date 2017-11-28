//
// LineSeparatorMarker.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using Mono.TextEditor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;

namespace MonoDevelop.SourceEditor
{
	class LineSeparatorMarker : TextLineMarker, ITextLineMarker
	{
		IDocumentLine ITextLineMarker.Line {
			get {
				return LineSegment;
			}
		}

		public override void Draw (MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics)
		{
			var color = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.FoldLine);
			cr.SetSourceColor (color);

			cr.LineWidth = 1 * editor.Options.Zoom;
			var y = metrics.LineYRenderStartPosition;
			cr.MoveTo (metrics.TextRenderStartPosition, y + metrics.LineHeight - 1.5);
			cr.LineTo (editor.Allocation.Width, y + metrics.LineHeight - 1.5);
			cr.Stroke ();
		}
	}
}
