// 
// ResultsEditorExtension.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.QuickTasks;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.AnalysisCore.Gui
{
	class ResultMarker : UnderlineTextSegmentMarker
	{
		Result result;
		
		public ResultMarker (Result result, TextSegment segment) : base ("", segment)
		{
			this.result = result;
		}
		
		static bool IsOneLine (Result result)
		{
			return result.Region.BeginLine == result.Region.EndLine;
		}
		
		public Result Result { get { return result; } }
		
		//utility for debugging
		public int Line { get { return result.Region.BeginLine; } }
		public int ColStart { get { return IsOneLine (result)? (result.Region.BeginColumn) : 0; } }
		public int ColEnd   { get { return IsOneLine (result)? (result.Region.EndColumn) : 0; } }
		public string Message { get { return result.Message; } }
		
		static Cairo.Color GetColor (TextEditor editor, Result result)
		{
			switch (result.Level) {
			case Severity.None:
				return editor.ColorStyle.PlainText.Background;
			case Severity.Error:
				return editor.ColorStyle.UnderlineError.GetColor ("color");
			case Severity.Warning:
				return editor.ColorStyle.UnderlineWarning.GetColor ("color");
			case Severity.Suggestion:
				return editor.ColorStyle.UnderlineSuggestion.GetColor ("color");
			case Severity.Hint:
				return editor.ColorStyle.UnderlineHint.GetColor ("color");
			default:
				throw new System.ArgumentOutOfRangeException ();
			}
		}
		
		public override void Draw (TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
		{
			if (Debugger.DebuggingService.IsDebugging)
				return;
			int markerStart = Segment.Offset;
			int markerEnd = Segment.EndOffset;
			if (markerEnd < startOffset || markerStart > endOffset) 
				return;
			
			bool drawOverlay = result.InspectionMark == IssueMarker.GrayOut;
			
			if (drawOverlay && editor.IsSomethingSelected) {
				var selectionRange = editor.SelectionRange;
				if (selectionRange.Contains (markerStart) && selectionRange.Contains (markerEnd))
					return;
				if (selectionRange.Contains (markerEnd))
					markerEnd = selectionRange.Offset;
				if (selectionRange.Contains (markerStart))
					markerStart = selectionRange.EndOffset;
				if (markerEnd <= markerStart)
					return;
			}
			
			double drawFrom;
			double drawTo;
				
			if (markerStart < startOffset && endOffset < markerEnd) {
				drawFrom = startXPos;
				drawTo = endXPos;
			} else {
				int start = startOffset < markerStart ? markerStart : startOffset;
				int end = endOffset < markerEnd ? endOffset : markerEnd;
				int /*lineNr,*/ x_pos;
				
				x_pos = layout.IndexToPos (start - startOffset).X;
				drawFrom = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
				x_pos = layout.IndexToPos (end - startOffset).X;
	
				drawTo = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
			}
			
			drawFrom = System.Math.Max (drawFrom, editor.TextViewMargin.XOffset);
			drawTo = System.Math.Max (drawTo, editor.TextViewMargin.XOffset);
			if (drawFrom >= drawTo)
				return;
			
			double height = editor.LineHeight / 5;
			cr.Color = GetColor (editor, Result);
			if (drawOverlay) {
				cr.Rectangle (drawFrom, y, drawTo - drawFrom, editor.LineHeight);
				var color = editor.ColorStyle.PlainText.Background;
				color.A = 0.6;
				cr.Color = color;
				cr.Fill ();
			} else if (Wave) {	
				Pango.CairoHelper.ShowErrorUnderline (cr, drawFrom, y + editor.LineHeight - height, drawTo - drawFrom, height);
			} else {
				cr.MoveTo (drawFrom, y + editor.LineHeight - 1);
				cr.LineTo (drawTo, y + editor.LineHeight - 1);
				cr.Stroke ();
			}
		}
	}

	class GrayOutMarker : ResultMarker, IChunkMarker
	{
		public GrayOutMarker (Result result, TextSegment segment) : base (result, segment)
		{
		}

		public override void Draw (TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
		{
		}

		#region IChunkMarker implementation
		void IChunkMarker.ChangeForeColor (TextEditor editor, Chunk chunk, ref Cairo.Color color)
		{
			if (Debugger.DebuggingService.IsDebugging)
				return;
			int markerStart = Segment.Offset;
			int markerEnd = Segment.EndOffset;
			if (!(markerStart <= chunk.Offset && chunk.Offset < markerEnd)) 
				return;

			var bgc = editor.ColorStyle.PlainText.Background;
			double alpha = 0.6;
			color = new Cairo.Color (
				color.R * alpha + bgc.R * (1.0 - alpha),
				color.G * alpha + bgc.G * (1.0 - alpha),
				color.B * alpha + bgc.B * (1.0 - alpha)
			);
		}
		#endregion
	}
}
