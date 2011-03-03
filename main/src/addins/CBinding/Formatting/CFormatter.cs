// 
// CFormatter.cs
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Mono.TextEditor;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Policies;
using System.Linq;

namespace CBinding.Formatting
{
	public class CFormatter : AbstractAdvancedFormatter
	{
		static internal readonly string MimeType = "text/x-csrc";

		public override bool SupportsOnTheFlyFormatting { get { return false; } }

		public override bool SupportsCorrectingIndent { get { return true; } }

		public override void CorrectIndenting (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, 
			TextEditorData data, int line)
		{
			LineSegment lineSegment = data.Document.GetLine (line);
			if (lineSegment == null)
				return;

			var policy = policyParent.Get<CFormattingPolicy> (mimeTypeChain);
			var tracker = new DocumentStateTracker<CIndentEngine> (new CIndentEngine (policy), data);
			tracker.UpdateEngine (lineSegment.Offset);
			for (int i = lineSegment.Offset; i < lineSegment.Offset + lineSegment.EditableLength; i++) {
				tracker.Engine.Push (data.Document.GetCharAt (i));
			}

			string curIndent = lineSegment.GetIndentation (data.Document);

			int nlwsp = curIndent.Length;
			if (!tracker.Engine.LineBeganInsideMultiLineComment || (nlwsp < lineSegment.Length && data.Document.GetCharAt (lineSegment.Offset + nlwsp) == '*')) {
				// Possibly replace the indent
				string newIndent = tracker.Engine.ThisLineIndent;
				if (newIndent != curIndent) 
					data.Replace (lineSegment.Offset, nlwsp, newIndent);
			}
			tracker.Dispose ();
		}

		public override void OnTheFlyFormat (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, 
			TextEditorData data, IType type, IMember member, ProjectDom dom, ICompilationUnit unit, DomLocation caretLocation)
		{
			// NotImplemented
		}

		public override void OnTheFlyFormat (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			TextEditorData data, int startOffset, int endOffset)
		{
			// NotImplemented
		}

		public override string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			string input, int startOffset, int endOffset)
		{
			//var data = new TextEditorData ();
			//data.Document.SuppressHighlightUpdate = true;
			//data.Document.MimeType = mimeTypeChain.First ();
			//data.Document.FileName = "toformat.cpp";
			//var textPolicy = policyParent.Get<TextStylePolicy> (mimeTypeChain);
			//data.Options.TabsToSpaces = textPolicy.TabsToSpaces;
			//data.Options.TabSize = textPolicy.TabWidth;
			//data.Options.OverrideDocumentEolMarker = true;
			//data.Options.DefaultEolMarker = textPolicy.GetEolMarker ();
			//data.Text = input;
			
			return input;
		}
	}
}
