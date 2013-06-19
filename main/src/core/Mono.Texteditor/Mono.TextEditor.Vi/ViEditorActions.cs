// 
// ViEditorActions.cs
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

using System;
using Mono.TextEditor;

namespace Mono.TextEditor.Vi
{
	public class ViEditorActions
	{
		public static void CaretToScreenCenter (ViEditor ed)
		{
			var line = ed.Editor.PointToLocation (0, ed.Editor.Allocation.Height/2).Line;
			if (line < 0)
				line = ed.Data.Document.LineCount;
			ed.Data.Caret.Line = line;
		}
		
		public static void CaretToScreenBottom (ViEditor ed)
		{
			int line = ed.Editor.PointToLocation (0, ed.Editor.Allocation.Height - ed.Editor.LineHeight * 2 - 2).Line;
			if (line < 0)
				line = ed.Data.Document.LineCount;
			ed.Data.Caret.Line = line;
		}
		
		public static void CaretToScreenTop (ViEditor ed)
		{
			ed.Data.Caret.Line = System.Math.Max (0, ed.Editor.PointToLocation (0, ed.Editor.LineHeight - 1).Line);
		}

		public static void CaretToLineNumber (int lineNumber, ViEditor ed)
		{
			ed.Data.Caret.Line = System.Math.Max (1, lineNumber);
		}
	}
}
