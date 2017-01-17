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
using System.Linq;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.Gui.Content;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Options;

namespace MonoDevelop.CSharp.Formatting
{
	static class OnTheFlyFormatter
	{
		public static void Format (TextEditor editor, DocumentContext context)
		{
			Format (editor, context, 0, editor.Length);
		}

		public static void Format (TextEditor editor, DocumentContext context, int startOffset, int endOffset, bool exact = true, OptionSet optionSet = null)
		{
			var policyParent = context.Project != null ? context.Project.Policies : PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, editor, context, startOffset, endOffset, exact, optionSet: optionSet);
		}

		public static void FormatStatmentAt (TextEditor editor, DocumentContext context, MonoDevelop.Ide.Editor.DocumentLocation location, OptionSet optionSet = null)
		{
			var offset = editor.LocationToOffset (location);
			var policyParent = context.Project != null ? context.Project.Policies : PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, editor, context, offset, offset, false, true, optionSet: optionSet);
		}

		static void Format (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, TextEditor editor, DocumentContext context, int startOffset, int endOffset, bool exact, bool formatLastStatementOnly = false, OptionSet optionSet = null)
		{
			TextSpan span;
			if (exact) {
				span = new TextSpan (startOffset, endOffset - startOffset);
			} else {
				span = new TextSpan (0, endOffset);
			}

			var analysisDocument = context.AnalysisDocument;
			if (analysisDocument == null)
				return;
			using (var undo = editor.OpenUndoGroup (/*OperationType.Format*/)) {
				try {
					var syntaxTree = analysisDocument.GetSyntaxTreeAsync ().Result;

					if (formatLastStatementOnly) {
						var root = syntaxTree.GetRoot ();
						var token = root.FindToken (endOffset);
						var tokens = ICSharpCode.NRefactory6.CSharp.FormattingRangeHelper.FindAppropriateRange (token);
						if (tokens.HasValue) {
							span = new TextSpan (tokens.Value.Item1.SpanStart, tokens.Value.Item2.Span.End - tokens.Value.Item1.SpanStart);
						} else {
							var parent = token.Parent;
							if (parent != null)
								span = parent.FullSpan;
						}
					}

					if (optionSet == null) {
						var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
						var textPolicy = policyParent.Get<TextStylePolicy> (mimeTypeChain);
						optionSet = policy.CreateOptions (textPolicy);
					}
					var doc = Formatter.FormatAsync (analysisDocument, span, optionSet).Result;
					var newTree = doc.GetSyntaxTreeAsync ().Result;
					var caretOffset = editor.CaretOffset;
					var caretEndOffset = caretOffset;

					int delta = 0;
					foreach (var change in newTree.GetChanges (syntaxTree)) {
						if (!exact && change.Span.Start >= caretOffset)
							continue;
						if (exact && !span.Contains (change.Span.Start))
							continue;
						var newText = change.NewText;
						var length = change.Span.Length;
						var changeEnd = delta + change.Span.End - 1;
						if (changeEnd < editor.Length && changeEnd >= 0 && editor.GetCharAt (changeEnd) == '\r')
							length--;
						var replaceOffset = delta + change.Span.Start;
						editor.ReplaceText (replaceOffset, length, newText);
						delta = delta - length + newText.Length;
						if (change.Span.Start < caretOffset) {
							if (change.Span.End < caretOffset) {
								caretEndOffset += newText.Length - length;
							} else {
								caretEndOffset = replaceOffset;
							}
						}
					}
					if (startOffset < caretOffset) {
						if (0 <= caretEndOffset && caretEndOffset < editor.Length)
							editor.CaretOffset = caretEndOffset;
						if (editor.CaretColumn == 1) {
							if (editor.CaretLine > 1 && editor.GetLine (editor.CaretLine - 1).Length == 0)
								editor.CaretLine--;
							editor.CaretColumn = editor.GetVirtualIndentationColumn (editor.CaretLine);
						}
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error in on the fly formatter", e);
				}
			}
		}
	}
}
