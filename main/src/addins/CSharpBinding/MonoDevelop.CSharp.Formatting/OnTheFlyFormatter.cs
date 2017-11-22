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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Policies;
using Roslyn.Utilities;

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

		static async void Format (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, TextEditor editor, DocumentContext context, int startOffset, int endOffset, bool exact, bool formatLastStatementOnly = false, OptionSet optionSet = null)
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
					var syntaxTree = await analysisDocument.GetSyntaxTreeAsync ();
					var root = syntaxTree.GetRoot ();
					if (formatLastStatementOnly) {
						var token = root.FindToken (endOffset);
						var tokens = Microsoft.CodeAnalysis.CSharp.Utilities.FormattingRangeHelper.FindAppropriateRange (token);
						if (tokens.HasValue) {
							span = new TextSpan (tokens.Value.Item1.SpanStart, editor.CaretOffset - tokens.Value.Item1.SpanStart);
						} else {
							var parent = token.Parent;
							if (parent != null)
								span = new TextSpan (parent.FullSpan.Start, editor.CaretOffset - parent.FullSpan.Start);
						}
					}

					if (optionSet == null) {
						var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
						var textPolicy = policyParent.Get<TextStylePolicy> (mimeTypeChain);
						optionSet = policy.CreateOptions (textPolicy);
					}
					var rules = Formatter.GetDefaultFormattingRules (analysisDocument);
					var changes = Formatter.GetFormattedTextChanges (root, SpecializedCollections.SingletonEnumerable (span), context.RoslynWorkspace, optionSet, rules, default(CancellationToken));
					editor.ApplyTextChanges (changes.Where(c => {
						if (!exact)
							return true;
						return span.Contains (c.Span.Start);
					}));
				} catch (Exception e) {
					LoggingService.LogError ("Error in on the fly formatter", e);
				}
			}
		}
	}
}
