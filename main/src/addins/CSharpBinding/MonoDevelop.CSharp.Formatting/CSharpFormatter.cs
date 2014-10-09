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
using MonoDevelop.CSharp.NRefactoryWrapper;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.Formatting
{
	class CSharpFormatter : AbstractAdvancedFormatter
	{
		static internal readonly string MimeType = "text/x-csharp";

		public override bool SupportsOnTheFlyFormatting { get { return true; } }

		public override bool SupportsCorrectingIndent { get { return true; } }

		public override void CorrectIndenting (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, 
			TextEditor data, int line)
		{
			var lineSegment = data.GetLine (line);
			if (lineSegment == null)
				return;

			try {
				var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
				var textpolicy = policyParent.Get<TextStylePolicy> (mimeTypeChain);
				var tracker = new CSharpIndentEngine (IdeApp.Workbench.ActiveDocument.AnalysisDocument, policy.CreateOptions (textpolicy));

				tracker.Update (lineSegment.Offset);
				for (int i = lineSegment.Offset; i < lineSegment.Offset + lineSegment.Length; i++) {
					tracker.Push (data.GetCharAt (i));
				}

				string curIndent = lineSegment.GetIndentation (data);

				int nlwsp = curIndent.Length;
				if (!tracker.LineBeganInsideMultiLineComment || (nlwsp < lineSegment.LengthIncludingDelimiter && data.GetCharAt (lineSegment.Offset + nlwsp) == '*')) {
					// Possibly replace the indent
					string newIndent = tracker.ThisLineIndent;
					if (newIndent != curIndent) 
						data.ReplaceText (lineSegment.Offset, nlwsp, newIndent);
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while indenting", e);
			}
		}

		public override void OnTheFlyFormat (TextEditor editor, DocumentContext context, int startOffset, int endOffset)
		{
			OnTheFlyFormatter.Format (editor, context, startOffset, endOffset);
		}


		public static string FormatText (CSharpFormattingPolicy policy, TextStylePolicy textPolicy, string mimeType, string input, int startOffset, int endOffset)
		{
			var inputTree = CSharpSyntaxTree.ParseText (SourceText.From (input));

			var doc = Formatter.Format (inputTree.GetRoot (), new TextSpan (startOffset, endOffset - startOffset), RoslynTypeSystemService.Workspace, policy.CreateOptions (textPolicy));
			var result = doc.ToFullString ();
			return result.Substring (startOffset, endOffset + result.Length - input.Length);
		}

		public override string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, string input, int startOffset, int endOffset)
		{
			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
			var textPolicy = policyParent.Get<TextStylePolicy> (mimeTypeChain);

			return FormatText (policy, textPolicy, mimeTypeChain.First (), input, startOffset, endOffset);

		}
	}
}
