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
using MonoDevelop.CSharp.Ast;
using System.Text;
using System.Linq;

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
			PolicyContainer policyParent = dom != null && dom.Project != null ? dom.Project.Policies
 : PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, data, dom, location, correctBlankLines, runAferCR);
		}

		public static void Format (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, 
			MonoDevelop.Ide.Gui.Document data, ProjectDom dom, DomLocation location, bool correctBlankLines, bool runAferCR = false)
		{
			var unit = data.ParsedDocument.CompilationUnit.Tag as MonoDevelop.CSharp.Ast.CompilationUnit;
			if (unit == null)
				return;

			AstLocation loc = new AstLocation (location.Line + (runAferCR ? -1 : 0), location.Column);
			AstNode member = null;
			foreach (AstNode node in MonoDevelop.CSharp.Ast.Utils.TreeTraversal.PreOrder (unit.Children, n => n.Parent != null && n.Parent.NodeType == NodeType.TypeDeclaration ? Enumerable.Empty<AstNode> () : n.Children)) {
				if (node.NodeType != NodeType.Member)
					continue;
				if (node.StartLocation < loc && loc <= node.EndLocation) {
					member = node;
					break;
				}
			}
			if (member == null)
				return;
			StringBuilder sb = new StringBuilder ();
			int closingBrackets = 0;
			var parent = member.Parent;
			while (parent != null) {
				if (parent.NodeType == NodeType.TypeDeclaration) {
					sb.Insert (0, "class Stub {");
					closingBrackets++;
				} else if (parent is NamespaceDeclaration) {
					sb.Insert (0, "namespace Stub {");
					closingBrackets++;
				}
				parent = parent.Parent;
			}
			sb.AppendLine ();
			System.Console.WriteLine ("caret offset:" + data.Editor.Caret.Offset);
			System.Console.WriteLine (data.Editor.Length);
			int startOffset = sb.Length;
			sb.Append (data.Editor.GetTextBetween (member.StartLocation.Line, 1, member.EndLocation.Line + (runAferCR ? 1 : 0), member.EndLocation.Column));
			int endOffset = sb.Length;
			sb.AppendLine ();
			sb.Append (new string ('}', closingBrackets));

			TextEditorData stubData = new TextEditorData () { Text = sb.ToString () };
			stubData.Document.FileName = data.FileName;
			var compilationUnit = new MonoDevelop.CSharp.Parser.CSharpParser ().Parse (stubData);

			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
			var domSpacingVisitor = new AstSpacingVisitor (policy, stubData) {
				AutoAcceptChanges = false,
			};
			compilationUnit.AcceptVisitor (domSpacingVisitor, null);

			var domIndentationVisitor = new AstIndentationVisitor (policy, stubData) {
				AutoAcceptChanges = false,
			};
			domIndentationVisitor.CorrectBlankLines = correctBlankLines;
			compilationUnit.AcceptVisitor (domIndentationVisitor, null);

			var changes = new List<Change> ();
			changes.AddRange (domSpacingVisitor.Changes.Cast<TextReplaceChange> ().Where (c => startOffset < c.Offset && c.Offset < endOffset));
			changes.AddRange (domIndentationVisitor.Changes.Cast<TextReplaceChange> ().Where (c => startOffset < c.Offset && c.Offset < endOffset));
			int delta = data.Editor.LocationToOffset (member.StartLocation.Line, 1) - startOffset;
			HashSet<int> lines = new HashSet<int> ();
			foreach (TextReplaceChange change in changes) {
				if (change is AstSpacingVisitor.MyTextReplaceChange) 
					((AstSpacingVisitor.MyTextReplaceChange)change).SetTextEditorData (data.Editor);
				change.Offset += delta;
				lines.Add (data.Editor.OffsetToLineNumber (change.Offset));
			}
			RefactoringService.AcceptChanges (null, null, changes);
			foreach (int line in lines)
				data.Editor.Document.CommitLineUpdate (line);
		}
	}
}