// 
// OnTheFlyFormatter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Ide;
using System;
using System.Collections.Generic;
using MonoDevelop.Projects.Policies;
using ICSharpCode.NRefactory.CSharp;
using System.Text;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.CSharp.Completion;
using MonoDevelop.Ide.Editor;
using ICSharpCode.NRefactory.Editor;
using MonoDevelop.CSharp.NRefactoryWrapper;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.CSharp.Formatting
{
	static class OnTheFlyFormatter
	{
		public static void Format (TextEditor editor, DocumentContext context)
		{
			Format (editor, context, 0, editor.Length);
		}

		public static void Format (TextEditor editor, DocumentContext context, TextLocation location)
		{
			Format (editor, context, location, location, false);
		} 

		public static void Format (TextEditor editor, DocumentContext context, TextLocation startLocation, TextLocation endLocation, bool exact = true)
		{
			Format (editor, context, editor.LocationToOffset (startLocation), editor.LocationToOffset (endLocation), exact);
		}
		
		public static void Format (TextEditor editor, DocumentContext context, int startOffset, int endOffset, bool exact = true)
		{
			var policyParent = context.Project != null ? context.Project.Policies : PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, editor, context, startOffset, endOffset, exact);
		}

		public static void FormatStatmentAt (TextEditor editor, DocumentContext context, MonoDevelop.Ide.Editor.DocumentLocation location)
		{
			var offset = editor.LocationToOffset (location);
			var policyParent = context.Project != null ? context.Project.Policies : PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, editor, context, offset, offset, false, true);
		}		

		public static void Format (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, TextEditor editor, DocumentContext context, int startOffset, int endOffset, bool exact, bool formatLastStatementOnly = false)
		{
			TextSpan span;
			if (exact) {
				span = new TextSpan (startOffset, endOffset - startOffset);
			} else {
				var ext = context.GetContent<CSharpCompletionTextEditorExtension> ();
				if (ext != null) {
					var seg = ext.GetMemberSegmentAt (endOffset) ?? ext.GetMemberSegmentAt (startOffset);
					if (seg != null) {
						span = new TextSpan (seg.Offset, endOffset - seg.Offset);
					} else {
						span = new TextSpan (0, endOffset);
					}
				} else {
					span = new TextSpan (0, endOffset);
				}
			}

			using (var undo = editor.OpenUndoGroup (/*OperationType.Format*/)) {
				try {
					var syntaxTree = context.AnalysisDocument.GetSyntaxTreeAsync ().Result;
					var doc = Formatter.FormatAsync (context.AnalysisDocument, span).Result;
					var newTree = doc.GetSyntaxTreeAsync ().Result;
					foreach (var change in newTree.GetChanges (syntaxTree)) {
						editor.ReplaceText (change.Span.Start, change.Span.Length, change.NewText);
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error in on the fly formatter", e);
				}
			}
		}
	}
}
