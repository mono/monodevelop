// 
// ContextActionExtensions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	static class ContextActionExtensions
	{
		public static int CalcIndentLevel (this MonoDevelop.Ide.Gui.Document doc, string indent)
		{
			int col = GetColumn (indent, 0, doc.Editor.Options.TabSize);
			return System.Math.Max (0, col / doc.Editor.Options.TabSize);
		}
		
		public static int GetColumn (string wrapper, int i, int tabSize)
		{
			int j = i;
			int col = 0;
			for (; j < wrapper.Length && (wrapper[j] == ' ' || wrapper[j] == '\t'); j++) {
				if (wrapper[j] == ' ') {
					col++;
				} else {
					col = GetNextTabstop (col, tabSize);
				}
			}
			return col;
		}
		
		static int GetNextTabstop (int currentColumn, int tabSize)
		{
			int result = currentColumn + tabSize;
			return (result / tabSize) * tabSize;
		}
		
		public static void FormatText (this AstNode node, MonoDevelop.Ide.Gui.Document doc)
		{
			doc.UpdateParseDocument ();
			MonoDevelop.CSharp.Formatting.OnTheFlyFormatter.Format (doc, node.StartLocation);
		}
	}
}

