//
// JavaScriptFormatter.cs
//
// Author:
//       Harsimran Bath <harsimranbath@gmail.com>
//
// Copyright (c) 2014 Harsimran
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
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.JavaScript.Formatting
{
	public class JavaScriptFormatter : AbstractAdvancedFormatter
	{
		public override bool SupportsOnTheFlyFormatting
		{
			get { return true; }
		}

		public override bool SupportsCorrectingIndent
		{
			get { return false; }
		}

		public override void CorrectIndenting (Projects.Policies.PolicyContainer policyParent, System.Collections.Generic.IEnumerable<string> mimeTypeChain, Mono.TextEditor.TextEditorData textEditorData, int line)
		{
		}

		public override void OnTheFlyFormat (Ide.Gui.Document doc, int startOffset, int endOffset)
		{
			// TODO: JSBeautify only supports full document formattinh at the moment. The Indent Level has issues.
			if (startOffset != 0 || doc.Editor.GetLineByOffset (endOffset).NextLine != null)
				return;

			string textToFormat = doc.Editor.GetTextBetween (startOffset, endOffset);
			DocumentLine currentLine = doc.Editor.GetLineByOffset (startOffset);
			int indentLevel = 0;
			const int defaultIndentSize = 4;

			if(currentLine != null && currentLine.PreviousLine != null) {
				string indentString = doc.Editor.GetIndentationString (currentLine.PreviousLine.Offset);
				if(indentString.Length % defaultIndentSize == 0) {
					indentLevel = indentString.Length / defaultIndentSize;
				}
			}

			var options = new JSBeautifyOptions {
				IndentSize = defaultIndentSize,
				IndentChar = ' ',
				IndentLevel = indentLevel,
				PreserveNewlines = true
			};
			var jsBeautifier = new JSBeautify (textToFormat, options);

			string formattedText = jsBeautifier.GetResult ();

			using (var undo = doc.Editor.OpenUndoGroup (OperationType.Format)) {
				try {
					doc.Editor.Replace (startOffset, endOffset - startOffset, formattedText);
					doc.Editor.Document.CommitDocumentUpdate ();
				} catch (Exception e) {
					LoggingService.LogError ("Error in on the JS fly formatter", e);
				}
			}
		}

		public override string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, string input, int startOffset, int endOffset)
		{
			var jsBeautifier = new JSBeautify (input, new JSBeautifyOptions {
				IndentSize = 4,
				IndentChar = ' ',
				IndentLevel = 0,
				PreserveNewlines = true,

			});
			return jsBeautifier.GetResult ();
		}
	}
}

