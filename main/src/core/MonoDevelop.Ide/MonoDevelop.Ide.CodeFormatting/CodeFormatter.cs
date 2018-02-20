// 
// Formatter.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using MonoDevelop.Projects.Policies;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.CodeFormatting
{
	public sealed class CodeFormatter
	{
		readonly AbstractCodeFormatter formatter;
		readonly string mimeType;

		public bool IsDefault {
			get {
				return formatter is DefaultCodeFormatter;
			}
		}

		public bool SupportsPartialDocumentFormatting { get { return formatter.SupportsPartialDocumentFormatting; }  }

		internal CodeFormatter (string mimeType, AbstractCodeFormatter formatter)
		{
			this.mimeType = mimeType;
			this.formatter = formatter;
		}

		[Obsolete ("Use roslyn instead")]
		public ITextSource Format (PolicyContainer policyParent, ITextSource input, int fromOffset, int toOffset)
		{
			return formatter.Format (policyParent, mimeType, input, fromOffset, toOffset);
		}

		[Obsolete ("Use roslyn instead")]
		public ITextSource Format (PolicyContainer policyParent, ITextSource input, ISegment segment = null)
		{
			try {
				if (segment == null)
					return formatter.Format (policyParent, mimeType, input);
				return formatter.Format (policyParent, mimeType, input, segment.Offset, segment.EndOffset);
			} catch (Exception e) {
				LoggingService.LogError ("Error while formatting text.", e);
				if (segment == null)
					return input;
				return input.CreateSnapshot (segment.Offset, segment.Length);
			}
		}

		[Obsolete ("Use roslyn instead")]
		public string FormatText (PolicyContainer policyParent, string input, int fromOffset, int toOffset)
		{
			return formatter.FormatText (policyParent, mimeType, input, fromOffset, toOffset);
		}

		[Obsolete ("Use roslyn instead")]
		public string FormatText (PolicyContainer policyParent, string input, ISegment segment = null)
		{
			try {
				if (segment == null)
					return formatter.FormatText (policyParent, mimeType, input, 0, input.Length);
				return formatter.FormatText (policyParent, mimeType, input, segment.Offset, segment.EndOffset);
			} catch (Exception e) {
				LoggingService.LogError ("Error while formatting text.", e);
				if (segment == null)
					return input;
				return input.Substring (segment.Offset, segment.Length);
			}
		}

		public bool SupportsOnTheFlyFormatting { get { return formatter.SupportsOnTheFlyFormatting; } }

		[Obsolete("Use OnTheFlyFormat (TextEditor editor, DocumentContext context, ISegment segment) instead. This function is going to be removed.")]
		public void OnTheFlyFormat (TextEditor editor, DocumentContext context, int startOffset, int endOffset)
		{
			formatter.OnTheFlyFormat (editor, context, startOffset, endOffset - startOffset);
		}

		public void OnTheFlyFormat (TextEditor editor, DocumentContext context, ISegment segment = null)
		{
			try {
				if (segment == null) {
					formatter.OnTheFlyFormat (editor, context, 0, editor.Length);
				} else {
					formatter.OnTheFlyFormat (editor, context, segment.Offset, segment.Length);
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while on the fly format.", e);
			}
		}

		[Obsolete("Use OnTheFlyFormat (TextEditor editor, DocumentContext context, ISegment segment) instead. This function is going to be removed.")]
		public void OnTheFlyFormat (Document ideDocument, int startOffset, int endOffset)
		{
			formatter.OnTheFlyFormat (ideDocument.Editor, ideDocument, startOffset, endOffset - startOffset);
		}

		public bool SupportsCorrectingIndent { get { return formatter.SupportsCorrectingIndent; } }

		[Obsolete ("Use roslyn instead")]
		public void CorrectIndenting (PolicyContainer policyParent, TextEditor editor, int line)
		{
			formatter.CorrectIndenting (policyParent, editor, line);
		}

		[Obsolete ("Use roslyn instead")]
		public void CorrectIndenting (PolicyContainer policyParent, TextEditor editor, IDocumentLine line)
		{
			try {
				if (line == null)
					throw new ArgumentNullException (nameof (line));
				formatter.CorrectIndenting (policyParent, editor, line.LineNumber);
			} catch (Exception e) {
				LoggingService.LogError ("Error while indenting.", e);
			}
		}
	}
}