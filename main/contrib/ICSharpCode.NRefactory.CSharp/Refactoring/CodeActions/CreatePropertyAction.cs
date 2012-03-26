// 
// CreateProperty.cs
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
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Create property", Description = "Creates a property for a undefined variable.")]
	public class CreatePropertyAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var identifier = context.GetNode<IdentifierExpression>();
			if (identifier == null) {
				yield break;
			}
			if (CreateFieldAction.IsInvocationTarget(identifier)) {
				yield break;
			}
			var statement = context.GetNode<Statement>();
			if (statement == null) {
				yield break;
			}

			if (!(context.Resolve(identifier).IsError)) {
				yield break;
			}
			var guessedType = CreateFieldAction.GuessAstType(context, identifier);
			if (guessedType == null) {
				yield break;
			}
			var state = context.GetResolverStateBefore(identifier);
			bool isStatic = state.CurrentMember.IsStatic;

			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
			if (service != null && !service.IsValidName(identifier.Identifier, AffectedEntity.Property, Modifiers.Private, isStatic)) { 
				yield break;
			}

			yield return new CodeAction(context.TranslateString("Create property"), script => {
				var decl = new PropertyDeclaration() {
					ReturnType = guessedType,
					Name = identifier.Identifier,
					Getter = new Accessor(),
					Setter = new Accessor()
				};
				if (isStatic) {
					decl.Modifiers |= Modifiers.Static;
				}
				script.InsertWithCursor(context.TranslateString("Create property"), decl, Script.InsertPosition.Before);
			});
		}
	}
}

