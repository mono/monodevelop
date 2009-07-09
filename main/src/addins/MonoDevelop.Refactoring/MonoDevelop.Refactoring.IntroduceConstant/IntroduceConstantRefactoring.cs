// 
// IntroduceConstantRefactoring.cs
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
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Core;
using Mono.TextEditor;



namespace MonoDevelop.Refactoring.IntroduceConstant
{
	public class IntroduceConstantRefactoring : RefactoringOperation
	{
		public class Parameters
		{
			public string Name {
				get;
				set;
			}
			
			public ICSharpCode.NRefactory.Ast.Modifiers Modifiers {
				get;
				set;
			}
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			if (options.ResolveResult == null)
				return false;
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (provider == null)
				return false;
			Expression expression = provider.ParseExpression (options.ResolveResult.ResolvedExpression.Expression);
			Console.WriteLine (expression);
			return expression is PrimitiveExpression;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Introduce Constant...");
		}

		public override void Run (RefactoringOptions options)
		{
			IntroduceConstantDialog dialog = new IntroduceConstantDialog (this, options, new Parameters ());
			dialog.Show ();
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			List<Change> result = new List<Change> ();
			Parameters param = properties as Parameters;
			if (param == null)
				return result;
			TextEditorData data = options.GetTextEditorData ();

			INRefactoryASTProvider provider = options.GetASTProvider ();

			FieldDeclaration fieldDeclaration = new FieldDeclaration (null);
			VariableDeclaration varDecl = new VariableDeclaration (param.Name);
			varDecl.Initializer = provider.ParseExpression (options.ResolveResult.ResolvedExpression.Expression);
			fieldDeclaration.Fields.Add (varDecl);
			fieldDeclaration.Modifier = param.Modifiers;
			fieldDeclaration.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Const;
			fieldDeclaration.TypeReference = new TypeReference (options.ResolveResult.ResolvedType.ToInvariantString ());
			fieldDeclaration.TypeReference.IsKeyword = true;

			Change insertConstant = new Change ();
			insertConstant.FileName = options.Document.FileName;
			insertConstant.Description = string.Format (GettextCatalog.GetString ("Generate constant '{0}'"), param.Name);
			insertConstant.Offset = data.Document.LocationToOffset (options.ResolveResult.CallingMember.Location.Line - 1, 0);
			insertConstant.InsertedText = provider.OutputNode (options.Dom, fieldDeclaration, options.GetIndent (options.ResolveResult.CallingMember)) + Environment.NewLine;
			result.Add (insertConstant);
			
			Change replaceConstant = new Change ();
			replaceConstant.FileName = options.Document.FileName;
			replaceConstant.Description = string.Format (GettextCatalog.GetString ("Replace expression with constant '{0}'"), param.Name);
			replaceConstant.Offset = data.Document.LocationToOffset (options.ResolveResult.ResolvedExpression.Region.Start.Line - 1, options.ResolveResult.ResolvedExpression.Region.Start.Column - 1);
			replaceConstant.RemovedChars = data.Document.LocationToOffset (options.ResolveResult.ResolvedExpression.Region.End.Line - 1, options.ResolveResult.ResolvedExpression.Region.End.Column - 1) - replaceConstant.Offset;
			replaceConstant.InsertedText = param.Name;
			result.Add (replaceConstant);

			return result;
		}
	}
}
