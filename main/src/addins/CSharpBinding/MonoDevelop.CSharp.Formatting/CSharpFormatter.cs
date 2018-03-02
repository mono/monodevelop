// 
// CSharpFormatter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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


using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Policies;
using System.Linq;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide;
using MonoDevelop.Core.Text;
using Mono.Options;

namespace MonoDevelop.CSharp.Formatting
{
	class CSharpFormatter : AbstractCodeFormatter
	{
		static internal readonly string MimeType = "text/x-csharp";

		public override bool SupportsOnTheFlyFormatting { get { return true; } }

		public override bool SupportsCorrectingIndent { get { return true; } }

		public override bool SupportsPartialDocumentFormatting { get { return true; } }

		protected override void CorrectIndentingImplementation (PolicyContainer policyParent, TextEditor editor, int line)
		{
			var lineSegment = editor.GetLine (line);
			if (lineSegment == null)
				return;

			try {
				var policy = policyParent.Get<CSharpFormattingPolicy> (MimeType);
				var textpolicy = policyParent.Get<TextStylePolicy> (MimeType);
				var tracker = new CSharpIndentEngine (policy.CreateOptions (textpolicy));

				tracker.Update (IdeApp.Workbench.ActiveDocument.Editor, lineSegment.Offset);
				for (int i = lineSegment.Offset; i < lineSegment.Offset + lineSegment.Length; i++) {
					tracker.Push (editor.GetCharAt (i));
				}

				string curIndent = lineSegment.GetIndentation (editor);

				int nlwsp = curIndent.Length;
				if (!tracker.LineBeganInsideMultiLineComment || (nlwsp < lineSegment.LengthIncludingDelimiter && editor.GetCharAt (lineSegment.Offset + nlwsp) == '*')) {
					// Possibly replace the indent
					string newIndent = tracker.ThisLineIndent;
					if (newIndent != curIndent) 
						editor.ReplaceText (lineSegment.Offset, nlwsp, newIndent);
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while indenting", e);
			}
		}

		protected override void OnTheFlyFormatImplementation (TextEditor editor, DocumentContext context, int startOffset, int length)
		{
			OnTheFlyFormatter.Format (editor, context, startOffset, startOffset + length);
		}

		public static string FormatText (Microsoft.CodeAnalysis.Options.OptionSet optionSet, string input, int startOffset, int endOffset)
		{
			var inputTree = CSharpSyntaxTree.ParseText (input);

			var root = inputTree.GetRoot ();
			var doc = Formatter.Format (root, new TextSpan (startOffset, endOffset - startOffset), TypeSystemService.Workspace, optionSet);
			var result = doc.ToFullString ();
			return result.Substring (startOffset, endOffset + result.Length - input.Length - startOffset);
		}

		protected override ITextSource FormatImplementation (PolicyContainer policyParent, string mimeType, ITextSource input, int startOffset, int length)
		{
			var chain = DesktopService.GetMimeTypeInheritanceChain (mimeType);
			var policy = policyParent.Get<CSharpFormattingPolicy> (chain);
			var textPolicy = policyParent.Get<TextStylePolicy> (chain);

			return new StringTextSource (FormatText (policy.CreateOptions (textPolicy), input.Text, startOffset, startOffset + length));
		}
	}
}
