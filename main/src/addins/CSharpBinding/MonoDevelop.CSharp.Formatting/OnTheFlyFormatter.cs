// 
// OnTheFlyFormatter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using Mono.CSharp;
using Mono.TextEditor;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Refactoring;
using System;
using System.Collections.Generic;
using MonoDevelop.Projects.Policies;
using ICSharpCode.NRefactory.CSharp;
using System.Text;
using System.Linq;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using MonoDevelop.CSharp.ContextAction;

namespace MonoDevelop.CSharp.Formatting
{
	public class OnTheFlyFormatter
	{
		public static void Format (MonoDevelop.Ide.Gui.Document data, ProjectDom dom)
		{
			Format (data, dom, DomLocation.Empty, false);
		}

		public static void Format (MonoDevelop.Ide.Gui.Document data, ProjectDom dom, DomLocation location, bool runAferCR = false)
		{
			Format (data, dom, location, false, runAferCR);
		}

		public static void Format (MonoDevelop.Ide.Gui.Document data, ProjectDom dom, DomLocation location, bool correctBlankLines, bool runAferCR = false)
		{
			PolicyContainer policyParent = dom != null && dom.Project != null ? dom.Project.Policies  : PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, data, dom, location, correctBlankLines, runAferCR);
		}

		public static void Format (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, MonoDevelop.Ide.Gui.Document data, ProjectDom dom, DomLocation location, bool correctBlankLines, bool runAferCR/* = false*/)
		{
			if (data.ParsedDocument == null || data.ParsedDocument.CompilationUnit == null)
				return;
			var member = data.ParsedDocument.CompilationUnit.GetMemberAt (location.Line + (runAferCR ? -1 : 0), location.Column);
			if (member == null || member.Location.IsEmpty || member.BodyRegion.End.IsEmpty)
				return;
			
			StringBuilder sb = new StringBuilder ();
			int closingBrackets = 0;
			DomRegion validRegion = DomRegion.Empty;
			foreach (var u in data.ParsedDocument.CompilationUnit.Usings.Where (us => us.IsFromNamespace)) {
				// the dom parser breaks A.B.C into 3 namespaces with the same region, this is filtered here
				if (u.ValidRegion == validRegion || !u.ValidRegion.Contains (location))
					continue;
				// indicates a parser error on namespace level.
				if (u.Namespaces.FirstOrDefault () == "<invalid>")
					continue;
				validRegion = u.ValidRegion;
				sb.Append ("namespace Stub {");
				closingBrackets++;
			}
			
			var parent = member.DeclaringType;
			while (parent != null) {
				sb.Append ("class Stub {");
				closingBrackets++;
				parent = parent.DeclaringType;
			}
			sb.AppendLine ();
			int startOffset = sb.Length;
			int memberStart = data.Editor.LocationToOffset (member.Location.Line, 1);
			int memberEnd = data.Editor.LocationToOffset (member.BodyRegion.End.Line + (runAferCR ? 1 : 0), member.BodyRegion.End.Column);
			if (memberEnd < 0)
				memberEnd = data.Editor.Length;
			sb.Append (data.Editor.GetTextBetween (memberStart, memberEnd));
			int endOffset = sb.Length;
			sb.AppendLine ();
			sb.Append (new string ('}', closingBrackets));
			TextEditorData stubData = new TextEditorData () { Text = sb.ToString () };
			stubData.Document.FileName = data.FileName;
			var parser = new ICSharpCode.NRefactory.CSharp.CSharpParser ();
			var compilationUnit = parser.Parse (stubData);
			bool hadErrors = parser.HasErrors;
			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
			var adapter = new TextEditorDataAdapter (stubData);
			
			var domSpacingVisitor = new AstFormattingVisitor (policy.CreateOptions (), adapter, new FormattingActionFactory (data.Editor)) {
				HadErrors = hadErrors
			};
			compilationUnit.AcceptVisitor (domSpacingVisitor, null);
			
			var changes = new List<ICSharpCode.NRefactory.CSharp.Refactoring.Action> ();
			changes.AddRange (domSpacingVisitor.Changes.Cast<TextReplaceAction> ().Where (c => startOffset < c.Offset && c.Offset < endOffset));
			
			int delta = data.Editor.LocationToOffset (member.Location.Line, 1) - startOffset;
			HashSet<int > lines = new HashSet<int> ();
			foreach (TextReplaceAction change in changes) {
				change.Offset += delta;
				lines.Add (data.Editor.OffsetToLineNumber (change.Offset));
			}
			// be sensible in documents with parser errors - only correct up to the caret position.
			if (hadErrors || data.ParsedDocument.Errors.Any (e => e.ErrorType == ErrorType.Error)) {
				var lastOffset = data.Editor.Caret.Offset;
				changes.RemoveAll (c => ((TextReplaceAction)c).Offset > lastOffset);
			}
			try {
				data.Editor.Document.BeginAtomicUndo ();
				MDRefactoringContext.MdScript.RunActions (changes, null);
				
				foreach (int line in lines)
					data.Editor.Document.CommitLineUpdate (line);
			} finally {
				data.Editor.Document.EndAtomicUndo ();
			}
			stubData.Dispose ();
		}
	}
}