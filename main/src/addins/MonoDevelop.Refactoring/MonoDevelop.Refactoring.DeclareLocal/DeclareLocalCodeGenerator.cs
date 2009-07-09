// 
// DeclareLocalCodeGenerator.cs
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
using System.Text;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using Mono.TextEditor;

namespace MonoDevelop.Refactoring.DeclareLocal
{
	public class DeclareLocalCodeGenerator : RefactoringOperation
	{
		public override string AccelKey {
			get {
				return IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.RefactoryCommands.DeclareLocal, null).AccelKey.Replace ("dead_circumflex", "^");
			}
		}
		public DeclareLocalCodeGenerator ()
		{
			Name = "Declare Local";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return false;

			TextEditorData data = options.GetTextEditorData ();
			LineSegment lineSegment = data.Document.GetLine (data.Caret.Line);
			string line = data.Document.GetTextAt (lineSegment);
			Expression expression = provider.ParseExpression (line);
			if (expression == null)
				return false;
			ResolveResult resolveResult = resolver.Resolve (new ExpressionResult (line), DomLocation.Empty);
			return resolveResult.ResolvedType != null &&  !string.IsNullOrEmpty (resolveResult.ResolvedType.FullName);
		}
		
		public string GetSimpleTypeName (RefactoringOptions options, string fullTypeName)
		{
			IType foundType = null;

			string curType = fullTypeName;
			while (foundType == null) {
				foundType = options.Dom.GetType (curType);
				int idx = curType.LastIndexOf ('.');
				if (idx < 0)
					break;
				curType = fullTypeName.Substring (0, idx);
			}

			if (foundType == null)
				foundType = new DomType (fullTypeName);
			if (options.Document.ParsedDocument != null) {
				foreach (IUsing u in options.Document.ParsedDocument.CompilationUnit.Usings) {
					foreach (string includedNamespace in u.Namespaces) {
						if (includedNamespace == foundType.Namespace)
							return fullTypeName.Substring (includedNamespace.Length + 1);
					}
				}
			}
			return fullTypeName;
		}
		
		public override void Run (RefactoringOptions options)
		{
			base.Run (options);
			options.Document.TextEditor.Select (selectionStart, selectionEnd);
		}

		int selectionStart;
		int selectionEnd;
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return result;
			TextEditorData data = options.GetTextEditorData ();
			LineSegment lineSegment = data.Document.GetLine (data.Caret.Line);
			string line = data.Document.GetTextAt (lineSegment);
			Expression expression = provider.ParseExpression (line);
			if (expression == null)
				return result;

			ResolveResult resolveResult = resolver.Resolve (new ExpressionResult (line), DomLocation.Empty);
			if (resolveResult.ResolvedType != null && !string.IsNullOrEmpty (resolveResult.ResolvedType.FullName)) {
				Change insert = new Change ();
				insert.FileName = options.Document.FileName;
				insert.Description = GettextCatalog.GetString ("Insert variable declaration");
				insert.Offset = lineSegment.Offset + options.GetWhitespaces (lineSegment.Offset).Length;
				string varName = "a" + resolveResult.ResolvedType.Name;
				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (new TypeReference (GetSimpleTypeName (options, resolveResult.ResolvedType.FullName)));
				varDecl.Variables.Add (new VariableDeclaration (varName, expression));
				insert.RemovedChars = expression.EndLocation.Column - 1;
				insert.InsertedText = provider.OutputNode (options.Dom, varDecl);
				result.Add (insert);
				selectionStart = insert.Offset + insert.InsertedText.IndexOf (varName);
				selectionEnd = selectionStart + varName.Length;
			}
			
			return result;
		}
	}
}
