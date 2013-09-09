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


using Mono.TextEditor;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Policies;
using System.Linq;
using MonoDevelop.Ide.CodeFormatting;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Refactoring;

namespace MonoDevelop.CSharp.Formatting
{
	class CSharpFormatter : AbstractAdvancedFormatter
	{
		static internal readonly string MimeType = "text/x-csharp";

		public override bool SupportsOnTheFlyFormatting { get { return true; } }

		public override bool SupportsCorrectingIndent { get { return true; } }

		public override void CorrectIndenting (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, 
			TextEditorData data, int line)
		{
			DocumentLine lineSegment = data.Document.GetLine (line);
			if (lineSegment == null)
				return;

			try {
				var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
				var tracker = new CSharpIndentEngine (data.Document, data.CreateNRefactoryTextEditorOptions (),  policy.CreateOptions ());

				tracker.Update (lineSegment.Offset);
				for (int i = lineSegment.Offset; i < lineSegment.Offset + lineSegment.Length; i++) {
					tracker.Push (data.Document.GetCharAt (i));
				}

				string curIndent = lineSegment.GetIndentation (data.Document);

				int nlwsp = curIndent.Length;
				if (!tracker.LineBeganInsideMultiLineComment || (nlwsp < lineSegment.LengthIncludingDelimiter && data.Document.GetCharAt (lineSegment.Offset + nlwsp) == '*')) {
					// Possibly replace the indent
					string newIndent = tracker.ThisLineIndent;
					if (newIndent != curIndent) 
						data.Replace (lineSegment.Offset, nlwsp, newIndent);
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while indenting", e);
			}
		}

		public override void OnTheFlyFormat (MonoDevelop.Ide.Gui.Document doc, int startOffset, int endOffset)
		{
			OnTheFlyFormatter.Format (doc, startOffset, endOffset);
		}


		public static string FormatText (CSharpFormattingPolicy policy, TextStylePolicy textPolicy, string mimeType, string input, int startOffset, int endOffset)
		{
			var data = new TextEditorData ();
			data.Document.SuppressHighlightUpdate = true;
			data.Document.MimeType = mimeType;
			data.Document.FileName = "toformat.cs";
			if (textPolicy != null) {
				data.Options.TabsToSpaces = textPolicy.TabsToSpaces;
				data.Options.TabSize = textPolicy.TabWidth;
				data.Options.IndentationSize = textPolicy.IndentWidth;
				data.Options.IndentStyle = textPolicy.RemoveTrailingWhitespace ? IndentStyle.Virtual : IndentStyle.Smart;
			}
			data.Text = input;

			// System.Console.WriteLine ("-----");
			// System.Console.WriteLine (data.Text.Replace (" ", ".").Replace ("\t", "->"));
			// System.Console.WriteLine ("-----");

			var parser = new CSharpParser ();
			var compilationUnit = parser.Parse (data);
			bool hadErrors = parser.HasErrors;
			
			if (hadErrors) {
				//				foreach (var e in parser.ErrorReportPrinter.Errors)
				//					Console.WriteLine (e.Message);
				return input.Substring (startOffset, Math.Max (0, Math.Min (endOffset, input.Length) - startOffset));
			}

			var originalVersion = data.Document.Version;

			var textEditorOptions = data.CreateNRefactoryTextEditorOptions ();
			var formattingVisitor = new ICSharpCode.NRefactory.CSharp.CSharpFormatter (
				policy.CreateOptions (),
				textEditorOptions
			) {
				FormattingMode = FormattingMode.Intrusive
			};

			var changes = formattingVisitor.AnalyzeFormatting (data.Document, compilationUnit);
			try {
				changes.ApplyChanges (startOffset, endOffset - startOffset);
			} catch (Exception e) {
				LoggingService.LogError ("Error in code formatter", e);
				return input.Substring (startOffset, Math.Max (0, Math.Min (endOffset, input.Length) - startOffset));
			}

			// check if the formatter has produced errors
			parser = new CSharpParser ();
			parser.Parse (data);
			if (parser.HasErrors) {
				LoggingService.LogError ("C# formatter produced source code errors. See console for output.");
				return input.Substring (startOffset, Math.Max (0, Math.Min (endOffset, input.Length) - startOffset));
			}

			var currentVersion = data.Document.Version;

			string result = data.GetTextBetween (startOffset, originalVersion.MoveOffsetTo (currentVersion, endOffset));
			data.Dispose ();
			return result;
		}

		public override string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, string input, int startOffset, int endOffset)
		{
			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
			var textPolicy = policyParent.Get<TextStylePolicy> (mimeTypeChain);

			return FormatText (policy, textPolicy, mimeTypeChain.First (), input, startOffset, endOffset);

		}
	}
}
