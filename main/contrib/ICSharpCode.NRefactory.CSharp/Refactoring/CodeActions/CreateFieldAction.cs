// 
// CreateField.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.PatternMatching;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Create field", Description = "Creates a field for a undefined variable.")]
	public class CreateFieldAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var identifier = GetIdentifier(context);
			if (identifier == null) {
				yield break;
			}
			
			if (!(context.Resolve(identifier).IsError && GuessType(context, identifier) != null)) {
				yield break;
			}
			
			yield return new CodeAction (context.TranslateString("Create field"), script => {
				script.InsertWithCursor(context.TranslateString("Create field"), GenerateFieldDeclaration(context, identifier), Script.InsertPosition.Before);
			});
		}
		

		static AstNode GenerateFieldDeclaration (RefactoringContext context, IdentifierExpression identifier)
		{
			return new FieldDeclaration () {
				ReturnType = GuessType (context, identifier),
				Variables = { new VariableInitializer (identifier.Identifier) }
			};
		}
		
		internal static AstType GuessType (RefactoringContext context, IdentifierExpression identifier)
		{
			if (identifier.Parent is AssignmentExpression) {
				var assign = (AssignmentExpression)identifier.Parent;
				var other = assign.Left == identifier ? assign.Right : assign.Left;
				return context.CreateShortType (context.Resolve (other).Type);
			}
			return null;
		}
		
		public static IdentifierExpression GetIdentifier (RefactoringContext context)
		{
			return context.GetNode<IdentifierExpression> ();
		}
	}
}

