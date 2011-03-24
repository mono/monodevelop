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
using System.IO;
using System.Reflection;
using System.Text;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

using Mono.TextEditor;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using System.Linq;
using MonoDevelop.Ide.CodeFormatting;

namespace MonoDevelop.CSharp.Formatting
{
	public class CSharpFormatter : AbstractAdvancedFormatter
	{
		static internal readonly string MimeType = "text/x-csharp";

		public override bool SupportsOnTheFlyFormatting { get { return true; } }

		public override bool SupportsCorrectingIndent { get { return true; } }

		public override void CorrectIndenting (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, 
			TextEditorData data, int line)
		{
			LineSegment lineSegment = data.Document.GetLine (line);
			if (lineSegment == null)
				return;

			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
			var tracker = new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (policy), data);
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
			//		OnTheFlyFormatter.Format (policyParent, mimeTypeChain, data, dom, caretLocation, true);
		}

		public override void OnTheFlyFormat (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, 
			TextEditorData data, int startOffset, int endOffset)
		{
			var parser = new MonoDevelop.CSharp.Parser.CSharpParser ();
			var compilationUnit = parser.Parse (data);
			bool hadErrors = parser.ErrorReportPrinter.ErrorsCount + parser.ErrorReportPrinter.FatalCounter > 0;
			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
			var formattingVisitor = new AstFormattingVisitor (policy, data) {
				AutoAcceptChanges = false,
			};
			compilationUnit.AcceptVisitor (formattingVisitor, null);
			
			
			var changes = new List<Change> ();

			changes.AddRange (formattingVisitor.Changes.
				Where (c => c is TextReplaceChange && (startOffset <= ((TextReplaceChange)c).Offset && ((TextReplaceChange)c).Offset < endOffset)));

			RefactoringService.AcceptChanges (null, null, changes);
		}

		public override string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, string input, int startOffset, int endOffset)
		{
			var data = new TextEditorData ();
			data.Document.SuppressHighlightUpdate = true;
			data.Document.MimeType = mimeTypeChain.First ();
			data.Document.FileName = "toformat.cs";
			var textPolicy = policyParent.Get<TextStylePolicy> (mimeTypeChain);
			data.Options.TabsToSpaces = textPolicy.TabsToSpaces;
			data.Options.TabSize = textPolicy.TabWidth;
			data.Options.OverrideDocumentEolMarker = true;
			data.Options.DefaultEolMarker = textPolicy.GetEolMarker ();
			data.Text = input;

			//System.Console.WriteLine ("-----");
			//System.Console.WriteLine (data.Text.Replace (" ", ".").Replace ("\t", "->"));
			//System.Console.WriteLine ("-----");

			MonoDevelop.CSharp.Parser.CSharpParser parser = new MonoDevelop.CSharp.Parser.CSharpParser ();
			var compilationUnit = parser.Parse (data);
			bool hadErrors = parser.ErrorReportPrinter.ErrorsCount + parser.ErrorReportPrinter.FatalCounter > 0;
			if (hadErrors)
				return null;
			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);

			var formattingVisitor = new AstFormattingVisitor (policy, data) {
				AutoAcceptChanges = false
			};
			compilationUnit.AcceptVisitor (formattingVisitor, null);

			var changes = new List<Change> ();

			changes.AddRange (formattingVisitor.Changes.
				Where (c => c is TextReplaceChange && (startOffset <= ((TextReplaceChange)c).Offset && ((TextReplaceChange)c).Offset < endOffset)));

			RefactoringService.AcceptChanges (null, null, changes);
			int end = endOffset;
			foreach (TextReplaceChange c in changes) {
				end -= c.RemovedChars;
				if (c.InsertedText != null)
					end += c.InsertedText.Length;
			}
			/*			System.Console.WriteLine ("-----");
			System.Console.WriteLine (data.Text.Replace (" ", "^").Replace ("\t", "->"));
			System.Console.WriteLine ("-----");*/
			string result = data.GetTextBetween (startOffset, Math.Min (data.Length, end));
			data.Dispose ();
			return result;
		}
	}
}
