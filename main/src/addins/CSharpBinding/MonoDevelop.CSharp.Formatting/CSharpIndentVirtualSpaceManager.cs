// 
// CSharpIndentVirtualSpaceManager.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects;
using MonoDevelop.Ide.CodeCompletion;

using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using Mono.TextEditor;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharp.Formatting
{
	class IndentVirtualSpaceManager : Mono.TextEditor.IIndentationTracker
	{
		Mono.TextEditor.TextEditorData data;
		DocumentStateTracker<CSharpIndentEngine> stateTracker;

		public IndentVirtualSpaceManager (Mono.TextEditor.TextEditorData data, DocumentStateTracker<CSharpIndentEngine> stateTracker)
		{
			this.data = data;
			this.stateTracker = stateTracker;
		}

		string GetIndentationString (int offset, DocumentLocation loc)
		{
			stateTracker.UpdateEngine (Math.Min (data.Length, offset + 1));
			DocumentLine line = data.Document.GetLine (loc.Line);
			if (line == null)
				return "";
			// Get context to the end of the line w/o changing the main engine's state
			var ctx = (CSharpIndentEngine)stateTracker.Engine.Clone ();
			for (int max = offset; max < line.Offset + line.Length; max++) {
				ctx.Push (data.Document.GetCharAt (max));
			}
//			int pos = line.Offset;
			string curIndent = line.GetIndentation (data.Document);
			int nlwsp = curIndent.Length;
//			int o = offset > pos + nlwsp ? offset - (pos + nlwsp) : 0;
			if (!stateTracker.Engine.LineBeganInsideMultiLineComment || (nlwsp < line.LengthIncludingDelimiter && data.Document.GetCharAt (line.Offset + nlwsp) == '*')) {
				return ctx.ThisLineIndent;
			}
			return curIndent;
		}
		public string GetIndentationString (int lineNumber, int column)
		{
			return GetIndentationString (data.LocationToOffset (lineNumber, column), new DocumentLocation (lineNumber, column));
		}
		
		public string GetIndentationString (int offset)
		{
			return GetIndentationString (offset, data.OffsetToLocation (offset));
		}

		string GetIndent (int lineNumber, int column)
		{
			var line = data.GetLine (lineNumber);
			if (line == null)
				return "";
			int offset = line.Offset + Math.Min (line.Length, column - 1);
 
			stateTracker.UpdateEngine (offset);
			return stateTracker.Engine.NewLineIndent;
		}

		public int GetVirtualIndentationColumn (int offset)
		{
			return 1 + GetIndentationString (offset).Length;
		}
		
		public int GetVirtualIndentationColumn (int lineNumber, int column)
		{
			return 1 + GetIndentationString (lineNumber, column).Length;
		}
	}
}

