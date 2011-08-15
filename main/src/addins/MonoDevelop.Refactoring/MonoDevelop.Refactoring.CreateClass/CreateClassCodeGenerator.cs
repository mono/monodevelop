// 
// CreateClassCodeGenerator.cs
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

using ICSharpCode.NRefactory.CSharp;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
 
using Mono.TextEditor;
using MonoDevelop.Ide.StandardHeader;
using MonoDevelop.Projects;


namespace MonoDevelop.Refactoring.CreateClass
{
	public class CreateClassCodeGenerator : RefactoringOperation
	{
		public CreateClassCodeGenerator ()
		{
			Name = "Create Class";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			if (options.ResolveResult == null || options.ResolveResult.ResolvedExpression == null)
				return false;
			if (options.Dom.GetType (options.ResolveResult.ResolvedType) != null)
				return false;
			createExpression = GetCreateExpression (options);
			return createExpression != null;
		}
		
		ObjectCreateExpression createExpression;
		
		ObjectCreateExpression GetCreateExpression (RefactoringOptions options)
		{
			TextEditorData data = options.GetTextEditorData ();
			if (data == null)
				return null;
			string expression = options.ResolveResult.ResolvedExpression.Expression;
			if (!expression.Contains ("(")) {
				int startPos = data.Document.LocationToOffset (options.ResolveResult.ResolvedExpression.Region.Start.Line, options.ResolveResult.ResolvedExpression.Region.Start.Column);
				if (startPos < 0)
					return null;
				for (int pos = startPos; pos < data.Document.Length; pos++) {
					char ch = data.Document.GetCharAt (pos);
					if (ch == '(') {
						int offset = data.Document.GetMatchingBracketOffset (pos);
						if (offset < startPos)
							return null;
						expression = data.Document.GetTextAt (startPos, offset - startPos + 1);
						break;
					}
				}
			}
			if (!expression.StartsWith ("new ")) {
				int startPos = data.Document.LocationToOffset (options.ResolveResult.ResolvedExpression.Region.Start.Line, options.ResolveResult.ResolvedExpression.Region.Start.Column);
				if (startPos < 0)
					return null;
				for (int pos = startPos; pos >= 0; pos--) {
					char ch = data.Document.GetCharAt (pos);
					if (Char.IsWhiteSpace (ch) && !Char.IsLetterOrDigit (ch) && ch != '_')
						return null;
					if (data.Document.GetTextAt (pos, 4) == "new ") {
						expression = "new " + expression;
						break;
					}
				}
			}
			
			INRefactoryASTProvider provider = options.GetASTProvider ();
			return provider != null ? provider.ParseText (expression) as ObjectCreateExpression : null;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Create Class");
		}
		
		public override void Run (RefactoringOptions options)
		{
			base.Run (options);
		}
		
		static string GetName (string baseFileName)
		{
			int i = 0;
			while (true && i < 999) {
				string curFileName = Path.Combine (Path.GetDirectoryName (baseFileName), Path.GetFileNameWithoutExtension (baseFileName) + (i > 0 ? i.ToString () : "") + Path.GetExtension (baseFileName));
				if (!File.Exists (curFileName))
					return curFileName;
				i++;
			}
			return baseFileName;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			IResolver resolver = options.GetResolver ();
			List<Change> result = new List<Change> ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return result;
			
			var newType = new TypeDeclaration ();
			newType.Name = provider.OutputNode (options.Dom, createExpression.Type);
			newType.ClassType = GetNewTypeType ();

			var constructor = new ConstructorDeclaration ();
			constructor.Modifiers = ICSharpCode.NRefactory.CSharp.Modifiers.Public;
			constructor.Body = new BlockStatement ();
			int i = 0;
			foreach (var expression in createExpression.Arguments) {
				i++;
				string output = provider.OutputNode (options.Dom, expression);
				string parameterName;
				if (Char.IsLetter (output[0]) || output[0] == '_') {
					parameterName = output;
				} else {
					parameterName = "par" + i;
				}

				ResolveResult resolveResult2 = resolver.Resolve (new ExpressionResult (output), options.ResolveResult.ResolvedExpression.Region.Start);
				var typeReference = new SimpleType (resolveResult2.ResolvedType.ToInvariantString ());
				var pde = new ParameterDeclaration (typeReference, parameterName);
				constructor.Parameters.Add (pde);
			}
			AstNode node = newType;
			IType curType = options.Document.CompilationUnit.GetTypeAt (options.Document.Editor.Caret.Line, options.Document.Editor.Caret.Column);
			if (curType != null && !string.IsNullOrEmpty (curType.Namespace)) {
				var namespaceDeclaration = new NamespaceDeclaration (curType.Namespace);
				namespaceDeclaration.Members.Add (newType);
				node = namespaceDeclaration;
			}
			newType.Members.Add (constructor);
			string fileName = GetName (Path.Combine (Path.GetDirectoryName (options.Document.FileName), newType.Name + Path.GetExtension (options.Document.FileName)));
			string header = options.Dom.Project is DotNetProject ? StandardHeaderService.GetHeader (options.Dom.Project, fileName, true) + Environment.NewLine : "";
			CreateFileChange createFile = new CreateFileChange (fileName, header + provider.OutputNode (options.Dom, node));
			result.Add (createFile);
			result.Add (new OpenFileChange (fileName));
			return result;
		}

		protected virtual ICSharpCode.NRefactory.CSharp.ClassType GetNewTypeType ()
		{
			return ICSharpCode.NRefactory.CSharp.ClassType.Class;
		}

	}
}
