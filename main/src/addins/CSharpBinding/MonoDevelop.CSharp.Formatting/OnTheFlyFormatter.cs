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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.CSharp.Completion;

namespace MonoDevelop.CSharp.Formatting
{
	public class OnTheFlyFormatter
	{
		public static void Format (MonoDevelop.Ide.Gui.Document data)
		{
			Format (data, TextLocation.Empty, false);
		}

		public static void Format (MonoDevelop.Ide.Gui.Document data, TextLocation location)
		{
			Format (data, location, false);
		} 

		public static void Format (MonoDevelop.Ide.Gui.Document data, TextLocation location, bool correctBlankLines)
		{
			var policyParent = data.Project != null ? data.Project.Policies : PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, data, location, data.Editor.Caret.Location, correctBlankLines);
		}
		
		public static void Format (MonoDevelop.Ide.Gui.Document data, int startOffset, int endOffset)
		{
			var policyParent = data.Project != null ? data.Project.Policies : PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, data, data.Editor.OffsetToLocation (startOffset), data.Editor.OffsetToLocation (endOffset), true);
		}

		public static void Format (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, MonoDevelop.Ide.Gui.Document data, TextLocation location, TextLocation endLocation, bool correctBlankLines)
		{
			if (data.ParsedDocument == null)
				return;
			var ext = data.GetContent<CSharpCompletionTextEditorExtension> ();
			if (ext == null)
				return;
			var offset = data.Editor.LocationToOffset (location);
			var seg = ext.typeSystemSegmentTree.GetMemberSegmentAt (offset);
			if (seg == null)
				return;

			var member = seg.Entity;
			if (member == null || member.Region.IsEmpty || member.BodyRegion.End.IsEmpty)
				return;

//			var unit = data.ParsedDocument.Annotation<CompilationUnit> ();
			var pf = data.ParsedDocument.ParsedFile as CSharpParsedFile;

			StringBuilder sb = new StringBuilder ();
			int closingBrackets = 0;
			// use the member start location to determine the using scope, because this information is in sync, the position in
			// the file may have changed since last parse run (we have up 2 date locations from the type segment tree).
			var scope = pf.GetUsingScope (member.Region.Begin);

			while (scope != null && !string.IsNullOrEmpty (scope.NamespaceName)) {
				sb.Append ("namespace Stub {");
				sb.Append (data.Editor.EolMarker);
				closingBrackets++;
				while (scope.Parent != null && scope.Parent.Region == scope.Region)
					scope = scope.Parent;
				scope = scope.Parent;
			}

			var parent = member.DeclaringTypeDefinition;
			while (parent != null) {
				sb.Append ("class " + parent.Name + " {");
				sb.Append (data.Editor.EolMarker);
				closingBrackets++;
				parent = parent.DeclaringTypeDefinition;
			}

			int startOffset = sb.Length;
			sb.Append (data.Editor.GetTextBetween (seg.Offset, seg.EndOffset));
			int endOffset = startOffset + data.Editor.LocationToOffset (endLocation) - seg.Offset;
			// Insert at least caret column eol markers otherwise the reindent of the generated closing bracket
			// could interfere with the current indentation.
			for (int i = 0; i <= endLocation.Column; i++) {
				sb.Append (data.Editor.EolMarker);
			}
			sb.Append (data.Editor.EolMarker);
			sb.Append (new string ('}', closingBrackets));
			var stubData = new TextEditorData () { Text = sb.ToString () };
			stubData.Document.FileName = data.FileName;
			var parser = new ICSharpCode.NRefactory.CSharp.CSharpParser ();
			var compilationUnit = parser.Parse (stubData);
			bool hadErrors = parser.HasErrors;
			// try it out, if the behavior is better when working only with correct code.
//			if (hadErrors) {
/*				Console.WriteLine (sb);
				parser.ErrorPrinter.Errors.ForEach (e => Console.WriteLine (e.Message));*/
//				return;
//			}
			
			var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
			
			var domSpacingVisitor = new AstFormattingVisitor (policy.CreateOptions (), stubData.Document, new FormattingActionFactory (data.Editor), data.Editor.Options.TabsToSpaces, data.Editor.Options.TabSize) {
				HadErrors = hadErrors,
				EolMarker = stubData.EolMarker
			};
			compilationUnit.AcceptVisitor (domSpacingVisitor);
			var changes = new List<ICSharpCode.NRefactory.CSharp.Refactoring.Action> ();
			changes.AddRange (domSpacingVisitor.Changes.Cast<TextReplaceAction> ());
			changes.Sort ((x, y) => ((TextReplaceAction)x).Offset.CompareTo (((TextReplaceAction)y).Offset));
			
			var newList = new List<ICSharpCode.NRefactory.CSharp.Refactoring.Action> (); 
			for (int i = 0; i < changes.Count; i++) {
				var c = (TextReplaceAction)changes [i];
				if (startOffset < c.Offset && c.Offset < endOffset || c.DependsOn != null && newList.Contains (c.DependsOn)) {
					newList.Add (c);
				}
			}
			changes = newList;
			int delta = seg.Offset - startOffset;
			
			HashSet<int> lines = new HashSet<int> ();
			foreach (TextReplaceAction change in changes) {
				change.Offset += delta;
				lines.Add (data.Editor.OffsetToLineNumber (change.Offset));
			}
			// be sensible in documents with parser errors - only correct up to the caret position.
			if (hadErrors || data.ParsedDocument.Errors.Any (e => e.ErrorType == ErrorType.Error)) {
				var lastOffset = data.Editor.Caret.Offset;
				newList = new List<ICSharpCode.NRefactory.CSharp.Refactoring.Action> (); 

				for (int i = 0; i < changes.Count; i++) {
					var tra = (TextReplaceAction)changes [i];
					if (tra.Offset < lastOffset || tra.DependsOn != null && newList.Contains (tra.DependsOn)) {
						newList.Add (tra);
					} else {
						Console.WriteLine (tra);
					}
				}
				changes = newList;
			}
			
			var caretEndOffset = data.Editor.Caret.Offset;
			int caretDelta = 0;
			foreach (TextReplaceAction act in changes) {
				if (act.Offset < caretEndOffset)
					caretDelta += -act.RemovedChars + (act.InsertedText != null ? act.InsertedText.Length : 0);
			}
			caretEndOffset += caretDelta;
			
			using (var undo = data.Editor.OpenUndoGroup ()) {
				MDRefactoringContext.MdScript.RunActions (changes, null);
				foreach (int line in lines)
					data.Editor.Document.CommitLineUpdate (line);
			}
			data.Editor.Caret.Offset = caretEndOffset;
			stubData.Dispose ();
		}
	}
}