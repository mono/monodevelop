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
			string allText = doc.Editor.Text;

			var jsBeautifier = new JSBeautify (allText, new JSBeautifyOptions {
				IndentSize = 4,
				IndentChar = ' ',
				IndentLevel = 0,
				PreserveNewlines = true,

			});

			string formattedText = jsBeautifier.GetResult ();
			string offsetText = formattedText.Substring (startOffset, endOffset - startOffset);

			using (var undo = doc.Editor.OpenUndoGroup (OperationType.Format)) {
				try {
					//changes.ApplyChanges (formatStartOffset + startDelta, Math.Max (0, formatLength - startDelta - 1), delegate (int replaceOffset, int replaceLength, string insertText) {
					//	int translatedOffset = realTextDelta + replaceOffset;
					//	data.Editor.Document.CommitLineUpdate (data.Editor.OffsetToLineNumber (translatedOffset));
					//	data.Editor.Replace (translatedOffset, replaceLength, insertText);
					//}, (replaceOffset, replaceLength, insertText) => {
					//	int translatedOffset = realTextDelta + replaceOffset;
					//	if (translatedOffset < 0 || translatedOffset + replaceLength > data.Editor.Length || replaceLength < 0)
					//		return true;
					//	return data.Editor.GetTextAt (translatedOffset, replaceLength) == insertText;
					//});
					// doc.Editor.Text = doc.Editor.Text.Replace (oldText, formattedText);
					doc.Editor.Replace (startOffset, endOffset - startOffset, offsetText);
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

