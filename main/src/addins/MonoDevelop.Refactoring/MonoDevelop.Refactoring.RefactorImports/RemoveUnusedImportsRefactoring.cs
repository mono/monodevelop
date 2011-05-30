// 
// RemoveUnusedUsingsRefactoring.cs
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
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.Refactoring.RefactorImports
{
	public class RemoveUnusedImportsRefactoring : RefactoringOperation
	{
		public RemoveUnusedImportsRefactoring ()
		{
			Name = "Remove unused imports";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			return true;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			List<Change> result = new List<Change> ();
			ICSharpCode.NRefactory.CSharp.CompilationUnit unit = options.GetASTProvider ().ParseFile (options.Document.Editor.Text);
			FindTypeReferencesVisitor visitor = new FindTypeReferencesVisitor (options.GetTextEditorData (), options.GetResolver ());
			visitor.VisitCompilationUnit (unit, null);

			ProjectDom dom = options.Dom;
			
			ICompilationUnit compilationUnit = options.ParseDocument ().CompilationUnit;
			HashSet<string> usedUsings = new HashSet<string> ();
			foreach (var r in visitor.PossibleTypeReferences) {
				if (r is PrimitiveType)
					continue;
				IType type = dom.SearchType (compilationUnit, options.ResolveResult != null ? options.ResolveResult.CallingType : null, new DomLocation (options.Document.Editor.Caret.Line, options.Document.Editor.Caret.Column), r.ConvertToReturnType ());
				if (type != null) {
					usedUsings.Add (type.Namespace);
				}
			}
			
			Mono.TextEditor.TextEditorData textEditorData = options.GetTextEditorData ();
			HashSet<string> currentUsings = new HashSet<string> ();
			foreach (IUsing u in compilationUnit.Usings) {
				if (u.IsFromNamespace)
					continue;
				if (!u.Aliases.Any () && u.Namespaces.All (name => currentUsings.Contains (name) || !usedUsings.Contains (name)) ) {
					TextReplaceChange change = new TextReplaceChange ();
					change.FileName = options.Document.FileName;
					change.Offset = textEditorData.Document.LocationToOffset (u.Region.Start.Line, u.Region.Start.Column);
					change.RemovedChars = textEditorData.Document.LocationToOffset (u.Region.End.Line, u.Region.End.Column) - change.Offset;
					Mono.TextEditor.LineSegment line = textEditorData.Document.GetLineByOffset (change.Offset);
					if (line != null && line.EditableLength == change.RemovedChars)
						change.RemovedChars += line.DelimiterLength;
					result.Add (change);
				}
				foreach (string nspace in u.Namespaces)
					currentUsings.Add (nspace);
			}
			
			return result;
		}
	}
}
