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
			OnTheFlyFormatter.Format (policyParent, mimeTypeChain, data, dom, caretLocation, true);
		}
		
		public override void OnTheFlyFormat (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			TextEditorData data, int startOffset, int endOffset)
		{
			var compilationUnit = new MonoDevelop.CSharp.Parser.CSharpParser ().Parse (data);
			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
			var domSpacingVisitor = new DomSpacingVisitor (policy, data) {
				AutoAcceptChanges = false,
			};
			compilationUnit.AcceptVisitor (domSpacingVisitor, null);
			
			var domIndentationVisitor = new DomIndentationVisitor (policy, data) {
				AutoAcceptChanges = false,
			};
			compilationUnit.AcceptVisitor (domIndentationVisitor, null);
			
			var changes = new List<Change> ();
			
			changes.AddRange (domSpacingVisitor.Changes.
				Concat (domIndentationVisitor.Changes).
				Where (c => c is TextReplaceChange && (startOffset <= ((TextReplaceChange)c).Offset && ((TextReplaceChange)c).Offset < endOffset)));
			
			RefactoringService.AcceptChanges (null, null, changes);
			CorrectFormatting (data, startOffset, endOffset);
		}
		
		int CorrectFormatting (TextEditorData data, int start, int end)
		{
			int delta = 0;
			int lineNumber = data.OffsetToLineNumber (start);
			LineSegment line = data.GetLine (lineNumber);
			if (line.Offset < start)
				lineNumber++;
			line = data.GetLine (lineNumber);
			if (line == null)
				return 0;
			bool wholeDocument = end >= data.Document.Length;
			do {
				string indent = line.GetIndentation (data.Document);
				StringBuilder newIndent = new StringBuilder ();
				int col = 1;
				if (data.Options.TabsToSpaces) {
					foreach (char ch in indent) {
						if (ch == '\t') {
							int tabWidth = TextViewMargin.GetNextTabstop (data, col) - col;
							newIndent.Append (new string (' ', tabWidth));
							col += tabWidth;
						} else {
							newIndent.Append (ch);
						}
					}
				} else {
					for (int i = 0; i < indent.Length; i++) {
						char ch = indent[i];
						if (ch == '\t') {
							int tabWidth = TextViewMargin.GetNextTabstop (data, col) - col;
							newIndent.Append (ch);
							col += tabWidth;
						} else {
							int tabWidth = TextViewMargin.GetNextTabstop (data, col) - col;
							newIndent.Append ('\t');
							col += tabWidth;
							while (tabWidth-- > 0 && i + 1 < indent.Length) {
								if (indent[i + 1] != ' ')
									break;
								i++;
							}
						}
					}
				}
				
				if (line.DelimiterLength != 0) {
					delta -= line.DelimiterLength;
					delta += data.EolMarker.Length;
					data.Replace (line.Offset + line.EditableLength, line.DelimiterLength, data.EolMarker);
					if (!wholeDocument) {
						end -= line.DelimiterLength;
						end += data.EolMarker.Length;
					}
				}
				
				string replaceWith = newIndent.ToString ();
				if (indent != replaceWith) {
					int count = (indent ?? "").Length;
					delta -= count;
					delta += data.Replace (line.Offset, count, replaceWith);
					if (!wholeDocument)
						end = end - count + replaceWith.Length;
				}
				
				lineNumber++;
				line = data.GetLine (lineNumber);
			} while (line != null && (wholeDocument || line.EndOffset <= end));
			return delta;
		}
		
		public override string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			string input, int startOffset, int endOffset)
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
			
//			System.Console.WriteLine ("TABS:" + textPolicy.TabsToSpaces);
			endOffset += CorrectFormatting (data, startOffset, endOffset);
			
/*			System.Console.WriteLine ("-----");
			System.Console.WriteLine (data.Text.Replace (" ", ".").Replace ("\t", "->"));
			System.Console.WriteLine ("-----");*/
			
			var compilationUnit = new MonoDevelop.CSharp.Parser.CSharpParser ().Parse (data);
			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
			
			var domSpacingVisitor = new DomSpacingVisitor (policy, data) {
				AutoAcceptChanges = false,
			};
			compilationUnit.AcceptVisitor (domSpacingVisitor, null);
			
			var domIndentationVisitor = new DomIndentationVisitor (policy, data) {
				AutoAcceptChanges = false,
			};
			compilationUnit.AcceptVisitor (domIndentationVisitor, null);
			
			var changes = new List<Change> ();
			
			changes.AddRange (domSpacingVisitor.Changes.
				Concat (domIndentationVisitor.Changes).
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
			return result;
		}
	}
}
