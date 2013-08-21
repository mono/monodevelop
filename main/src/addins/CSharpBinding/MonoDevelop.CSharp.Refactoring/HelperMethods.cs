// 
// HelperMethods.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.Refactoring
{
	static class HelperMethods
	{
		public static TextReplaceChange GetRemoveNodeChange (this TextEditorData editor, AstNode n)
		{
			var change = new TextReplaceChange ();
			change.FileName = editor.FileName;
			change.Offset = editor.LocationToOffset (n.StartLocation);
			change.RemovedChars = editor.LocationToOffset (n.EndLocation) - change.Offset;
			
			// remove EOL, when line is empty
			var line = editor.GetLineByOffset (change.Offset);
			if (line != null && line.Length == change.RemovedChars)
				change.RemovedChars += line.DelimiterLength;
			return change;
		}

		public static ICSharpCode.NRefactory.CSharp.TextEditorOptions CreateNRefactoryTextEditorOptions (this TextEditorData doc)
		{
			return new ICSharpCode.NRefactory.CSharp.TextEditorOptions () {
				TabsToSpaces = doc.TabsToSpaces,
				TabSize = doc.Options.TabSize,
				IndentSize = doc.Options.IndentationSize,
				ContinuationIndent = doc.Options.IndentationSize,
				LabelIndent = -doc.Options.IndentationSize,
				EolMarker = doc.EolMarker,
				IndentBlankLines = doc.Options.IndentStyle != IndentStyle.Virtual,
				WrapLineLength = doc.Options.RulerColumn
			};
		}

		public static void RemoveNode (this TextEditorData editor, AstNode n)
		{
			var change = editor.GetRemoveNodeChange (n);
			editor.Remove (change.Offset, change.RemovedChars);
		}
		public static void Replace (this TextEditorData editor, AstNode n, AstNode replaceWith)
		{
			var change = editor.GetRemoveNodeChange (n);
			editor.Replace (change.Offset, change.RemovedChars, replaceWith.ToString ());
		}
	}
}

