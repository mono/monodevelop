// 
// SortImportsRefactoring.cs
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
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using System.Text;

namespace MonoDevelop.Refactoring.RefactorImports
{
	public class SortImportsRefactoring : RefactoringOperation
	{
		public SortImportsRefactoring ()
		{
			Name = "Sort imports";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			return true;
		}
		
		static int UsingComparer (IUsing left, IUsing right)
		{
			if (left.Aliases.Any () && right.Aliases.Any ())
				return left.Aliases.First ().Key.CompareTo (right.Aliases.First ().Key);

			if (left.Aliases.Any ())
				return 1;
			if (right.Aliases.Any ())
				return -1;
			bool leftIsSystem = left.Namespaces.First ().StartsWith ("System");
			bool rightIsSystem = right.Namespaces.First ().StartsWith ("System");
			if (leftIsSystem && !rightIsSystem)
				return -1;
			if (!leftIsSystem && rightIsSystem)
				return 1;
			return left.Namespaces.First ().CompareTo (right.Namespaces.First ());
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			List<Change> result = new List<Change> ();
			ICompilationUnit compilationUnit = options.ParseDocument ().CompilationUnit;
			Mono.TextEditor.TextEditorData textEditorData = options.GetTextEditorData ();
			int minOffset = int.MaxValue;
			foreach (IUsing u in compilationUnit.Usings) {
				if (u.IsFromNamespace)
					continue;
				int offset = textEditorData.Document.LocationToOffset (u.Region.Start.Line - 1, u.Region.Start.Column - 1);
				TextReplaceChange change = new TextReplaceChange () {
					FileName = options.Document.FileName,
					Offset = offset,
					RemovedChars = textEditorData.Document.LocationToOffset (u.Region.End.Line - 1, u.Region.End.Column - 1) - offset
				};
				Mono.TextEditor.LineSegment line = textEditorData.Document.GetLineByOffset (change.Offset);
				if (line != null && line.EditableLength == change.RemovedChars)
					change.RemovedChars += line.DelimiterLength;
				result.Add (change);
				minOffset = Math.Min (minOffset, offset);
			}
			StringBuilder output = new StringBuilder ();
			List<IUsing> usings = new List<IUsing> (compilationUnit.Usings);
			usings.Sort (UsingComparer);
			INRefactoryASTProvider astProvider = options.GetASTProvider ();
			foreach (IUsing u in usings) {
				UsingDeclaration declaration;
				if (u.IsFromNamespace)
					continue;
				if (u.Aliases.Any ()) {
					KeyValuePair<string, IReturnType> alias = u.Aliases.First ();
					declaration = new UsingDeclaration (alias.Key, alias.Value.ConvertToTypeReference ());
				} else {
					declaration = new UsingDeclaration (u.Namespaces.First ());
				}
				output.Append (astProvider.OutputNode (options.Dom, declaration));
			}
			TextReplaceChange insertSortedUsings = new TextReplaceChange () {
				FileName = options.Document.FileName,
				Offset = minOffset,
				InsertedText = output.ToString ()
			};
			result.Add (insertSortedUsings);
			return result;
		}
	}
}
