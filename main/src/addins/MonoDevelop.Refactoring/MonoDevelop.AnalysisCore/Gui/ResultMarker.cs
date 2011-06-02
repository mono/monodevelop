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

namespace MonoDevelop.AnalysisCore.Gui
{
	class ResultMarker : UnderlineMarker
	{
		Result result;
		
		public ResultMarker (Result result) : base (
				GetColor (result),
				IsOneLine (result)? (result.Region.Start.Column) : 0,
				IsOneLine (result)? (result.Region.End.Column) : 0)
		{
			this.result = result;
		}
		
		static bool IsOneLine (Result result)
		{
			return result.Region.Start.Line == result.Region.End.Line;
		}
		
		public Result Result { get { return result; } }
		
		//utility for debugging
		public int Line { get { return result.Region.Start.Line; } }
		public int ColStart { get { return IsOneLine (result)? (result.Region.Start.Column) : 0; } }
		public int ColEnd   { get { return IsOneLine (result)? (result.Region.End.Column) : 0; } }
		public string Message { get { return result.Message; } }
		
		static string GetColor (Result result)
		{
			switch (result.Level) {
			case QuickTaskSeverity.Error:
				return Mono.TextEditor.Highlighting.ColorSheme.ErrorUnderlineString;
			case QuickTaskSeverity.Warning:
				return Mono.TextEditor.Highlighting.ColorSheme.WarningUnderlineString;
			case QuickTaskSeverity.Suggestion:
				return Mono.TextEditor.Highlighting.ColorSheme.SuggestionUnderlineString;
			case QuickTaskSeverity.Hint:
				return Mono.TextEditor.Highlighting.ColorSheme.HintUnderlineString;
			default:
				throw new System.ArgumentOutOfRangeException ();
			}
		}
		
		public override void Draw (TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
		{
			int markerStart = LineSegment.Offset + System.Math.Max (StartCol - 1, 0);
			int markerEnd = LineSegment.Offset + (EndCol < 1 ? LineSegment.EditableLength : EndCol - 1);
			if (markerEnd < startOffset || markerStart > endOffset) 
				return; 
	
			double @from;
			double to;
				
			if (markerStart < startOffset && endOffset < markerEnd) {
					@from = startXPos;
				to = endXPos;
			} else {
				int start = startOffset < markerStart ? markerStart : startOffset;
				int end = endOffset < markerEnd ? endOffset : markerEnd;
				int /*lineNr,*/ x_pos;
				
				x_pos = layout.IndexToPos (start - startOffset).X;
					@from = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
	
				x_pos = layout.IndexToPos (end - startOffset).X;
	
				to = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
			}
				@from = System.Math.Max (@from, editor.TextViewMargin.XOffset);
			to = System.Math.Max (to, editor.TextViewMargin.XOffset);
			if (@from >= to) {
				return;
			}
			double height = editor.LineHeight / 5;
			cr.Color = ColorName == null ? Color : editor.ColorStyle.GetColorFromDefinition (ColorName);
			if (result.Level == QuickTaskSeverity.Warning && result.Importance == ResultImportance.Low) {
				cr.Rectangle (@from, y, to - from, editor.LineHeight);
				var color = editor.ColorStyle.Default.CairoBackgroundColor;
				color.A = 0.6;
				cr.Color = color;
				cr.Fill ();
			} else if (Wave) {	
				Pango.CairoHelper.ShowErrorUnderline (cr, @from, y + editor.LineHeight - height, to - @from, height);
			} else {
				cr.MoveTo (@from, y + editor.LineHeight - 1);
				cr.LineTo (to, y + editor.LineHeight - 1);
				cr.Stroke ();
			}
		}
	}
}
