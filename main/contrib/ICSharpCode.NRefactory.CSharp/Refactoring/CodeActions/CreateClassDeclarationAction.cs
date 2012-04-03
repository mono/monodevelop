// 
// CreateClassDeclarationAction.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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

using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Create class", Description = "Creates a class declaration out of an object creation.")]
	public class CreateClassDeclarationAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var createExpression = context.GetNode<ObjectCreateExpression>();
			if (createExpression != null) 
				return GetActions(context, createExpression);
			
			var simpleType = context.GetNode<SimpleType>();
			if (simpleType != null && !(simpleType.Parent is EventDeclaration || simpleType.Parent is CustomEventDeclaration)) 
				return GetActions(context, simpleType);

			return Enumerable.Empty<CodeAction>();
		}

		static IEnumerable<CodeAction> GetActions(RefactoringContext context, AstNode node)
		{
			var resolveResult = context.Resolve(node) as UnknownIdentifierResolveResult;
			if (resolveResult == null)
				yield break;

			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
			if (service != null && !service.IsValidName(resolveResult.Identifier, AffectedEntity.Class)) { 
				yield break;
			}

			yield return new CodeAction(context.TranslateString("Create class"), script => {
				script.CreateNewType(CreateType(context, service, node));
			});

			yield return new CodeAction(context.TranslateString("Create nested class"), script => {
				script.InsertWithCursor(context.TranslateString("Create nested class"), CreateType(context, service, node), Script.InsertPosition.Before);
			});
		}

		static TypeDeclaration CreateType(RefactoringContext context, NamingConventionService service, AstNode node)
		{
			var result = node is SimpleType ?
				CreateClassFromType(context, (SimpleType)node) : 
				CreateClassFromObjectCreation(context, (ObjectCreateExpression)node);
			
			return AddBaseTypesAccordingToNamingRules(context, service, result);
		}

		static TypeDeclaration CreateClassFromType(RefactoringContext context, SimpleType simpleType)
		{
			TypeDeclaration result;
			string className = simpleType.Identifier;

			if (simpleType.Parent is Attribute) {
				if (!className.EndsWith("Attribute"))
					className += "Attribute";
			}

			result = new TypeDeclaration() { Name = className };
			var entity = simpleType.GetParent<EntityDeclaration>();
			if (entity != null)
				result.Modifiers |= entity.Modifiers & ~Modifiers.Internal;

			return result;
		}

		static TypeDeclaration CreateClassFromObjectCreation(RefactoringContext context, ObjectCreateExpression createExpression)
		{
			TypeDeclaration result;
			string className = createExpression.Type.GetText();
			if (!createExpression.Arguments.Any()) {
				result = new TypeDeclaration() { Name = className };
			} else {
				var decl = new ConstructorDeclaration() {
					Name = className,
					Modifiers = Modifiers.Public,
					Body = new BlockStatement() {
						new ThrowStatement(new ObjectCreateExpression(context.CreateShortType("System", "NotImplementedException")))
					}
				};
				result = new TypeDeclaration() {
					Name = className,
					Members = {
						decl
					}
				};
				decl.Parameters.AddRange(CreateMethodDeclarationAction.GenerateParameters(context, createExpression.Arguments));
			}
			return result;
		}
		
		static TypeDeclaration AddBaseTypesAccordingToNamingRules(RefactoringContext context, NamingConventionService service, TypeDeclaration result)
		{
			if (service.HasValidRule(result.Name, AffectedEntity.CustomAttributes, Modifiers.Public)) {
				result.BaseTypes.Add(context.CreateShortType("System", "Attribute"));
			} else if (service.HasValidRule(result.Name, AffectedEntity.CustomEventArgs, Modifiers.Public)) {
				result.BaseTypes.Add(context.CreateShortType("System", "EventArgs"));
			} else if (service.HasValidRule(result.Name, AffectedEntity.CustomExceptions, Modifiers.Public)) {
				result.BaseTypes.Add(context.CreateShortType("System", "Exception"));
			}
			return result;
		}
	}
}