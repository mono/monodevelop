// 
// ExpandSelectionHandler.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.CSharp
{
	enum ExpandCommands
	{
		ExpandSelection,
		ShrinkSelection
	}
	
	static class ExpandSelectionHandler
	{
		internal class ExpandSelectionAnnotation
		{
			readonly MonoDevelop.Ide.Editor.TextEditor editor;

			public Stack<SyntaxNode> Stack = new Stack<SyntaxNode> ();

			public ExpandSelectionAnnotation (MonoDevelop.Ide.Editor.TextEditor editor)
			{
				this.editor = editor;
				editor.CaretPositionChanged += Editor_CaretPositionChanged;
			}

			void Editor_CaretPositionChanged (object sender, EventArgs e)
			{
				editor.CaretPositionChanged -= Editor_CaretPositionChanged;
				Stack = null;
				editor.RemoveAnnotations<ExpandSelectionAnnotation> ();
			}
		}

		internal static ExpandSelectionAnnotation GetSelectionAnnotation (MonoDevelop.Ide.Editor.TextEditor editor)
		{
			var result = editor.Annotation<ExpandSelectionAnnotation> ();
			if (result == null) {
				result = new ExpandSelectionAnnotation (editor);
				editor.AddAnnotation (result);
			}
			return result;
		}

		internal static void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			var selectionRange = doc.Editor.SelectionRange;
			var parsedDocument = doc.ParsedDocument;
			if (parsedDocument == null)
				return;
			var model = parsedDocument.GetAst<SemanticModel> ();
			if (model == null)
				return;
			var unit = model.SyntaxTree.GetRoot ();
			var node = unit.FindNode (Microsoft.CodeAnalysis.Text.TextSpan.FromBounds (selectionRange.Offset, selectionRange.EndOffset));
			if (node == null)
				return;
			
			if (doc.Editor.IsSomethingSelected) {
				while (node != null && ShrinkSelectionHandler.IsSelected (doc.Editor, node.Span)) {
					node = node.Parent;
				}
			}

			if (node != null) {
				var selectionAnnotation = GetSelectionAnnotation (doc.Editor);
				selectionAnnotation.Stack.Push (node);
				doc.Editor.SetSelection (node.SpanStart, node.Span.End);
			}
		}
	}
	
	static class ShrinkSelectionHandler
	{
		internal static bool IsSelected (MonoDevelop.Ide.Editor.TextEditor editor, Microsoft.CodeAnalysis.Text.TextSpan span)
		{
			var selection = editor.SelectionRange;
			return selection.Offset == span.Start && selection.Length == span.Length;
		}

		internal static async void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			var selectionRange = doc.Editor.CaretOffset;
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return;
			var parsedDocument = await analysisDocument.GetSyntaxTreeAsync ();
			if (parsedDocument == null)
				return;
			var unit = parsedDocument.GetRoot ();
			if (unit == null)
				return;

			var selectionAnnotation = ExpandSelectionHandler.GetSelectionAnnotation (doc.Editor);
			if (selectionAnnotation.Stack.Count == 0)
				return;
			selectionAnnotation.Stack.Pop ();
			if (selectionAnnotation.Stack.Count > 0) {
				var node = selectionAnnotation.Stack.Peek ();
				doc.Editor.SetSelection (node.SpanStart, node.Span.End);
			} else {
				doc.Editor.ClearSelection ();
			}
		}
	}
}