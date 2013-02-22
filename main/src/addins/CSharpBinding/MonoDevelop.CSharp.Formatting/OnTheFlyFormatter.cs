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
using Mono.TextEditor;
using MonoDevelop.Ide;
using System;
using System.Collections.Generic;
using MonoDevelop.Projects.Policies;
using ICSharpCode.NRefactory.CSharp;
using System.Text;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.CSharp.Completion;

namespace MonoDevelop.CSharp.Formatting
{
	public class OnTheFlyFormatter
	{
		public static void Format (MonoDevelop.Ide.Gui.Document data)
		{
			Format (data, 0, data.Editor.Length);
		}

		public static void Format (MonoDevelop.Ide.Gui.Document data, TextLocation location)
		{
			Format (data, location, location, false);
		} 

		public static void Format (MonoDevelop.Ide.Gui.Document data, TextLocation startLocation, TextLocation endLocation, bool exact = true)
		{
			Format (data, data.Editor.LocationToOffset (startLocation), data.Editor.LocationToOffset (endLocation), exact);
		}
		
		public static void Format (MonoDevelop.Ide.Gui.Document data, int startOffset, int endOffset, bool exact = true)
		{
			var policyParent = data.Project != null ? data.Project.Policies : PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, data, startOffset, endOffset, exact);
		}

		public static void FormatStatmentAt (MonoDevelop.Ide.Gui.Document data, DocumentLocation location)
		{
			var offset = data.Editor.LocationToOffset (location);
			var policyParent = data.Project != null ? data.Project.Policies : PolicyService.DefaultPolicies;
			var mimeTypeChain = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			Format (policyParent, mimeTypeChain, data, offset, offset, false, true);
		}		
		
		/// <summary>
		/// Builds a compileable stub file out of an entity.
		/// </summary>
		/// <returns>
		/// A string representing the stub
		/// </returns>
		/// <param name='memberStartOffset'>
		/// The offset where the member starts in the returned text.
		/// </param>
		static string BuildStub (MonoDevelop.Ide.Gui.Document data, CSharpCompletionTextEditorExtension.TypeSystemTreeSegment seg, int startOffset, int endOffset, out int memberStartOffset)
		{
			var pf = data.ParsedDocument.ParsedFile as CSharpUnresolvedFile;
			if (pf == null) {
				memberStartOffset = 0;
				return null;
			}
			
			var sb = new StringBuilder ();
			
			int closingBrackets = 0;
			// use the member start location to determine the using scope, because this information is in sync, the position in
			// the file may have changed since last parse run (we have up 2 date locations from the type segment tree).
			var scope = pf.GetUsingScope (seg.Entity.Region.Begin);

			while (scope != null && !string.IsNullOrEmpty (scope.NamespaceName)) {
				// Hack: some syntax errors lead to invalid namespace names.
				if (scope.NamespaceName.EndsWith ("<invalid>", StringComparison.Ordinal)) {
					scope = scope.Parent;
					continue;
				}
				sb.Append ("namespace Stub {");
				sb.Append (data.Editor.EolMarker);
				closingBrackets++;
				while (scope.Parent != null && scope.Parent.Region == scope.Region)
					scope = scope.Parent;
				scope = scope.Parent;
			}

			var parent = seg.Entity.DeclaringTypeDefinition;
			while (parent != null) {
				sb.Append ("class " + parent.Name + " {");
				sb.Append (data.Editor.EolMarker);
				closingBrackets++;
				parent = parent.DeclaringTypeDefinition;
			}

			memberStartOffset = sb.Length;
			var text = data.Editor.GetTextBetween (seg.Offset, endOffset);
			sb.Append (text);

			var lex = new CSharpCompletionEngineBase.MiniLexer (text);
			lex.Parse (ch => {
				if (lex.IsInString || lex.IsInChar || lex.IsInVerbatimString || lex.IsInSingleComment || lex.IsInMultiLineComment || lex.IsInPreprocessorDirective)
					return;
				if (ch =='{') {
					closingBrackets++;
				} else if (ch =='}') {
					closingBrackets--;
				}
			});


			// Insert at least caret column eol markers otherwise the reindent of the generated closing bracket
			// could interfere with the current indentation.
			var endLocation = data.Editor.OffsetToLocation (endOffset);
			for (int i = 0; i <= endLocation.Column; i++) {
				sb.Append (data.Editor.EolMarker);
			}
			sb.Append (data.Editor.EolMarker);
			sb.Append (new string ('}', closingBrackets));
			return sb.ToString ();
		}
		
		static AstFormattingVisitor GetFormattingChanges (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, MonoDevelop.Ide.Gui.Document document, string input, DomRegion formattingRegion, ref int formatStartOffset, ref int formatLength, bool formatLastStatementOnly)
		{
			using (var stubData = TextEditorData.CreateImmutable (input)) {
				stubData.Document.FileName = document.FileName;
				var parser = document.HasProject ? new CSharpParser (TypeSystemParser.GetCompilerArguments (document.Project)) : new CSharpParser ();
				var compilationUnit = parser.Parse (stubData);
				bool hadErrors = parser.HasErrors;
				if (hadErrors) {
					using (var stubData2 = TextEditorData.CreateImmutable (input + "}")) {
						compilationUnit = parser.Parse (stubData2);
						hadErrors = parser.HasErrors;
					}
				}
				// try it out, if the behavior is better when working only with correct code.
				if (hadErrors) {
					return null;
				}
				
				var policy = policyParent.Get<CSharpFormattingPolicy> (mimeTypeChain);
				
				var formattingVisitor = new AstFormattingVisitor (policy.CreateOptions (), stubData.Document, document.Editor.CreateNRefactoryTextEditorOptions ()) {
					HadErrors = hadErrors,
				};
				formattingVisitor.AddFormattingRegion (formattingRegion);

				compilationUnit.AcceptVisitor (formattingVisitor);

				if (formatLastStatementOnly) {
					AstNode node = compilationUnit.GetAdjacentNodeAt<Statement> (stubData.OffsetToLocation (formatStartOffset + formatLength - 1));
					if (node != null) {
						while (node.Role == Roles.EmbeddedStatement || node.Role == IfElseStatement.TrueRole || node.Role == IfElseStatement.FalseRole)
							node = node.Parent;
						var start = stubData.LocationToOffset (node.StartLocation);
						if (start > formatStartOffset) {
							var end = stubData.LocationToOffset (node.EndLocation);
							formatStartOffset = start;
							formatLength = end - start;
						}
					}
				}
				return formattingVisitor;
			}
		}
		
		public static void Format (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, MonoDevelop.Ide.Gui.Document data, int startOffset, int endOffset, bool exact, bool formatLastStatementOnly = false)
		{
			if (data.ParsedDocument == null)
				return;
			var ext = data.GetContent<CSharpCompletionTextEditorExtension> ();
			if (ext == null)
				return;
			string text;
			int formatStartOffset, formatLength, realTextDelta;
			DomRegion formattingRegion = DomRegion.Empty;
			int startDelta = 1;
			if (exact) {
				text = data.Editor.Text;
				var seg = ext.typeSystemSegmentTree.GetMemberSegmentAt (startOffset);
				var seg2 = ext.typeSystemSegmentTree.GetMemberSegmentAt (endOffset);
				if (seg != null && seg == seg2) {
					var member = seg.Entity;
					if (member == null || member.Region.IsEmpty || member.BodyRegion.End.IsEmpty)
						return;

					text = BuildStub (data, seg, startOffset, endOffset, out formatStartOffset);
					startDelta = startOffset - seg.Offset;
					formatLength = endOffset - startOffset + startDelta;
					realTextDelta = seg.Offset - formatStartOffset;
				} else {
					formatStartOffset = startOffset;
					formatLength = endOffset - startOffset;
					realTextDelta = 0;
					formattingRegion = new DomRegion (data.Editor.OffsetToLocation (startOffset), data.Editor.OffsetToLocation (endOffset));
				}
			} else {
				var seg = ext.typeSystemSegmentTree.GetMemberSegmentAt (startOffset - 1);
				if (seg == null)
					return;
				var member = seg.Entity;
				if (member == null || member.Region.IsEmpty || member.BodyRegion.End.IsEmpty)
					return;
	
				// Build stub
				text = BuildStub (data, seg, startOffset, endOffset, out formatStartOffset);

				formatLength = endOffset - seg.Offset;
				realTextDelta = seg.Offset - formatStartOffset;
			}
			// Get changes from formatting visitor
			var changes = GetFormattingChanges (policyParent, mimeTypeChain, data, text, formattingRegion, ref formatStartOffset, ref formatLength, formatLastStatementOnly);
			if (changes == null)
				return;

			// Do the actual formatting
//			var originalVersion = data.Editor.Document.Version;

			using (var undo = data.Editor.OpenUndoGroup ()) {
				try {
					changes.ApplyChanges (formatStartOffset + startDelta, Math.Max (0, formatLength - startDelta - 1), delegate (int replaceOffset, int replaceLength, string insertText) {
						int translatedOffset = realTextDelta + replaceOffset;
						data.Editor.Document.CommitLineUpdate (data.Editor.OffsetToLineNumber (translatedOffset));
						data.Editor.Replace (translatedOffset, replaceLength, insertText);
					}, (replaceOffset, replaceLength, insertText) => {
						int translatedOffset = realTextDelta + replaceOffset;
						if (translatedOffset < 0 || translatedOffset + replaceLength > data.Editor.Length)
							return true;
						return data.Editor.GetTextAt (translatedOffset, replaceLength) == insertText;
					});
				} catch (Exception e) {
					LoggingService.LogError ("Error in on the fly formatter", e);
				}

//				var currentVersion = data.Editor.Document.Version;
//				data.Editor.Caret.Offset = originalVersion.MoveOffsetTo (currentVersion, caretOffset, ICSharpCode.NRefactory.Editor.AnchorMovementType.Default);
			}
		}
	}
}