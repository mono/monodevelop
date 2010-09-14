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

namespace MonoDevelop.CSharp.Formatting
{
	public class CSharpFormatter : AbstractPrettyPrinter
	{
		static internal readonly string MimeType = "text/x-csharp";
		
		public override bool SupportsOnTheFlyFormatting {
			get {
				return true;
			}
		}
		
		public override bool CanFormat (string mimeType)
		{
			return mimeType == MimeType;
		}
		
		public override void CorrectIndenting (object textEditorData, int line)
		{
			TextEditorData data = (TextEditorData)textEditorData;
			LineSegment lineSegment = data.Document.GetLine (line);
			if (lineSegment == null)
				return;
			IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			var policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
			DocumentStateTracker<CSharpIndentEngine> tracker = new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (policy), data);
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
		
		public override void OnTheFlyFormat (object textEditorData, IType type, IMember member, ProjectDom dom, ICompilationUnit unit, DomLocation caretLocation)
		{
			OnTheFlyFormatter.Format ((TextEditorData)textEditorData, dom, caretLocation);
		}
		
		public override void OnTheFlyFormat (PolicyContainer policyParent, object textEditorData, int startOffset, int endOffset)
		{
			TextEditorData data = (TextEditorData)textEditorData;
			CSharp.Dom.CompilationUnit compilationUnit = new MonoDevelop.CSharp.Parser.CSharpParser ().Parse (data);
			IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			CSharpFormattingPolicy policy = policyParent != null ? policyParent.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
			DomSpacingVisitor domSpacingVisitor = new DomSpacingVisitor (policy, data);
			domSpacingVisitor.AutoAcceptChanges = false;
			compilationUnit.AcceptVisitor (domSpacingVisitor, null);
			
			DomIndentationVisitor domIndentationVisitor = new DomIndentationVisitor (policy, data);
			domIndentationVisitor.AutoAcceptChanges = false;
			compilationUnit.AcceptVisitor (domIndentationVisitor, null);
			
			List<Change> changes = new List<Change> ();
			
			changes.AddRange (domSpacingVisitor.Changes.
				Concat (domIndentationVisitor.Changes).
				Where (c => c is TextReplaceChange && (startOffset <= ((TextReplaceChange)c).Offset && ((TextReplaceChange)c).Offset < endOffset)));
			
			RefactoringService.AcceptChanges (null, null, changes);
		}
				
		protected override string InternalFormat (PolicyContainer policyParent, string mimeType, string input, int startOffset, int endOffset)
		{
			TextEditorData data = new TextEditorData ();
			data.Text = input;
			data.Document.MimeType = mimeType;
			data.Document.FileName = "toformat.cs";
			CSharp.Dom.CompilationUnit compilationUnit = new MonoDevelop.CSharp.Parser.CSharpParser ().Parse (data);
			IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			CSharpFormattingPolicy policy = policyParent != null ? policyParent.Get<CSharpFormattingPolicy> (types) : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
			DomSpacingVisitor domSpacingVisitor = new DomSpacingVisitor (policy, data);
			domSpacingVisitor.AutoAcceptChanges = false;
			compilationUnit.AcceptVisitor (domSpacingVisitor, null);
			
			DomIndentationVisitor domIndentationVisitor = new DomIndentationVisitor (policy, data);
			domIndentationVisitor.AutoAcceptChanges = false;
			compilationUnit.AcceptVisitor (domIndentationVisitor, null);
			
			List<Change> changes = new List<Change> ();
			
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
			return data.Text.Substring (startOffset, end - startOffset + 1);
		}
	}
}
