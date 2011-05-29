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
			case ResultLevel.Error:
				return Mono.TextEditor.Highlighting.ColorSheme.ErrorUnderlineString;
			case ResultLevel.Warning:
				return Mono.TextEditor.Highlighting.ColorSheme.WarningUnderlineString;
			case ResultLevel.Suggestion:
				return Mono.TextEditor.Highlighting.ColorSheme.SuggestionUnderlineString;
			case ResultLevel.Todo:
				return Mono.TextEditor.Highlighting.ColorSheme.CaretString;
			default:
				throw new System.ArgumentOutOfRangeException ();
			}
		}
	}
}
