//
// TypeGuessing.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class TypeGuessing
	{
		static int GetArgumentIndex(IEnumerable<ArgumentSyntax> arguments, SyntaxNode parameter)
		{
			//Console.WriteLine("arg:" +parameter);
			int argumentNumber = 0;
			foreach (var arg in arguments) {
				//Console.WriteLine(arg +"/"+parameter);
				if (arg == parameter) {
					return argumentNumber;
				}
				argumentNumber++;
			}
			return -1;
		}

		static IEnumerable<ITypeSymbol> GetAllValidTypesFromInvocation(SemanticModel resolver, InvocationExpressionSyntax invoke, SyntaxNode parameter)
		{
			int index = GetArgumentIndex(invoke.ArgumentList.Arguments, parameter);
			if (index < 0)
				yield break;
			var targetResult = resolver.GetSymbolInfo(invoke.Expression);

//			var targetResult = resolver.Resolve(invoke.Target) as MethodGroupResolveResult;
//			if (targetResult != null) {
			foreach (var method in targetResult.CandidateSymbols) {
				var parameters = method.GetParameters();
				if (index < parameters.Length) {
					if (parameters [index].IsParams) {
						var arrayType = parameters[index].Type as IArrayTypeSymbol;
						if (arrayType != null)
							yield return arrayType.ElementType;
					}
					yield return parameters[index].Type;
				}
			}

//				foreach (var extMethods in targetResult.GetExtensionMethods ()) {
//					foreach (var extMethod in extMethods) {
//						ITypeSymbol[] inferredTypes;
//						var m = extMethod;
//						if (CSharpResolver.IsEligibleExtensionMethod(targetResult.TargetType, extMethod, true, out inferredTypes)) {
//							if (inferredTypes != null)
//								m = extMethod.Specialize(new TypeParameterSubstitution(null, inferredTypes));
//						}
//
//						int correctedIndex = index + 1;
//						if (correctedIndex < m.Parameters.Count) {
//							if (m.Parameters [correctedIndex].IsParams) {
//								var arrayType = m.Parameters [correctedIndex].Type as ArrayType;
//								if (arrayType != null)
//									yield return arrayType.ElementType;
//							}
//							yield return m.Parameters [correctedIndex].Type;
//						}
//					}
//				}
//			}
		}

//		static IEnumerable<ITypeSymbol> GetAllValidTypesFromObjectCreation(SemanticModel resolver, ObjectCreateExpressionSyntax invoke, SyntaxNode parameter)
//		{
//			int index = GetArgumentIndex(invoke.Arguments, parameter);
//			if (index < 0)
//				yield break;
//
//			var targetResult = resolver.Resolve(invoke.Type);
//			if (targetResult is TypeResolveResult) {
//				var type = ((TypeResolveResult)targetResult).Type;
//				if (type.Kind == TypeKind.Delegate && index == 0) {
//					yield return type;
//					yield break;
//				}
//				foreach (var constructor in type.GetConstructors ()) {
//					if (index < constructor.Parameters.Count)
//						yield return constructor.Parameters [index].Type;
//				}
//			}
//		}
//
//		public static ITypeSymbol GetElementType(SemanticModel resolver, ITypeSymbol type)
//		{
//			// TODO: A better get element type method.
//			if (type.Kind == TypeKind.Array || type.Kind == TypeKind.Dynamic) {
//				if (type.Kind == TypeKind.Array)
//					return ((ArrayType)type).ElementType;
//				return resolver.Compilation.FindType(KnownTypeCode.Object);
//			}
//
//
//			foreach (var method in type.GetMethods (m => m.Name == "GetEnumerator")) {
//				ITypeSymbol returnType = null;
//				foreach (var prop in method.ReturnType.GetProperties(p => p.Name == "Current")) {
//					if (returnType != null && prop.ReturnType.IsKnownType (KnownTypeCode.Object))
//						continue;
//					returnType = prop.ReturnType;
//				}
//				if (returnType != null)
//					return returnType;
//			}
//
//			return resolver.Compilation.FindType(KnownTypeCode.Object);
//		}
//
//		static IEnumerable<ITypeSymbol> GuessFromConstructorInitializer(SemanticModel resolver, SyntaxNode expr)
//		{
//			var init = expr.Parent as ConstructorInitializer;
//			var rr = resolver.Resolve(expr.Parent);
//			int index = GetArgumentIndex(init.Arguments, expr);
//			if (index >= 0) {
//				foreach (var constructor in rr.Type.GetConstructors()) {
//					if (index < constructor.Parameters.Count) {
//						yield return constructor.Parameters[index].Type;
//					}
//				}
//			}
//		}

		public static IEnumerable<ITypeSymbol> GetValidTypes(SemanticModel model, SyntaxNode expr, CancellationToken cancellationToken = default(CancellationToken))
		{
//			if (expr.Role == Roles.Condition) {
//				return new [] { model.Compilation.FindType (KnownTypeCode.Boolean) };
//			}
//
			var mref = expr.Parent as MemberAccessExpressionSyntax;
			if (mref != null && mref.Name != expr) {
				mref = null;
			}
			if (mref != null) {
				// case: guess enum when trying to access not existent enum member
				var rr = model.GetTypeInfo(mref.Expression);
				if (rr.Type != null && rr.Type.TypeKind == TypeKind.Enum)
					return new [] { rr.Type };
			}

//			if (expr.Parent is ParenthesizedExpressionSyntax || expr.Parent is NamedArgumentExpressionSyntax) {
//				return GetValidTypes(model, expr.Parent);
//			}
//			if (expr.Parent is DirectionExpressionSyntax) {
//				var parent = expr.Parent.Parent;
//				if (parent is InvocationExpressionSyntax) {
//					var invoke = (InvocationExpressionSyntax)parent;
//					return GetAllValidTypesFromInvocation(model, invoke, expr.Parent);
//				}
//			}
//
//			if (expr.Parent is ArrayInitializerExpressionSyntax) {
//				if (expr is NamedExpressionSyntax)
//					return new [] { model.Resolve(((NamedExpressionSyntax)expr).ExpressionSyntax).Type };
//
//				var aex = expr.Parent as ArrayInitializerExpressionSyntax;
//				if (aex.IsSingleElement)
//					aex = aex.Parent as ArrayInitializerExpressionSyntax;
//				var type = GetElementType(model, model.Resolve(aex.Parent).Type);
//				if (type.Kind != TypeKind.Unknown)
//					return new [] { type };
//			}
//
//			if (expr.Parent is ObjectCreateExpressionSyntax) {
//				var invoke = (ObjectCreateExpressionSyntax)expr.Parent;
//				return GetAllValidTypesFromObjectCreation(model, invoke, expr);
//			}
//
//			if (expr.Parent is ArrayCreateExpressionSyntax) {
//				var ace = (ArrayCreateExpressionSyntax)expr.Parent;
//				if (!ace.Type.IsNull) {
//					return new [] { model.Resolve(ace.Type).Type };
//				}
//			}
//
//			if (expr.Parent is VariableInitializer) {
//				var initializer = (VariableInitializer)expr.Parent;
//				var field = initializer.GetParent<FieldDeclaration>();
//				if (field != null) {
//					var rr = model.Resolve(field.ReturnType);
//					if (!rr.IsError)
//						return new [] { rr.Type };
//				}
//				var varStmt = initializer.GetParent<VariableDeclarationStatement>();
//				if (varStmt != null) {
//					var rr = model.Resolve(varStmt.Type);
//					if (!rr.IsError)
//						return new [] { rr.Type };
//				}
//				return new [] { model.Resolve(initializer).Type };
//			}
//
//			if (expr.Parent is CastExpressionSyntax) {
//				var cast = (CastExpressionSyntax)expr.Parent;
//				return new [] { model.Resolve(cast.Type).Type };
//			}
//
//			if (expr.Parent is AsExpressionSyntax) {
//				var cast = (AsExpressionSyntax)expr.Parent;
//				return new [] { model.Resolve(cast.Type).Type };
//			}

			if (expr.Parent is AssignmentExpressionSyntax || mref != null && mref.Parent is AssignmentExpressionSyntax ) {
				var assign = expr.Parent as AssignmentExpressionSyntax ?? mref.Parent as AssignmentExpressionSyntax;
				var other = assign.Left == expr || assign.Left == mref ? assign.Right : assign.Left;
				return new [] { model.GetTypeInfo(other).Type };
			}

//			if (expr.Parent is BinaryOperatorExpressionSyntax) {
//				var assign = (BinaryOperatorExpressionSyntax)expr.Parent;
//				var other = assign.Left == expr ? assign.Right : assign.Left;
//				return new [] { model.Resolve(other).Type };
//			}
//
//			if (expr.Parent is ReturnStatement) {
//				var parent = expr.Ancestors.FirstOrDefault(n => n is EntityDeclaration || n is AnonymousMethodExpressionSyntax|| n is LambdaExpressionSyntax);
//				if (parent != null) {
//					var rr = model.Resolve(parent);
//					if (!rr.IsError)
//						return new [] { rr.Type };
//				}
//				var e = parent as EntityDeclaration;
//				if (e != null) {
//					var rt = model.Resolve(e.ReturnType);
//					if (!rt.IsError)
//						return new [] { rt.Type };
//				}
//			}
//
//			if (expr.Parent is YieldReturnStatement) {
//				var state = model.GetResolverStateBefore(expr);
//				if (state != null && (state.CurrentMember.ReturnType is ParameterizedType)) {
//					var pt = (ParameterizedType)state.CurrentMember.ReturnType;
//					if (pt.FullName == "System.Collections.Generic.IEnumerable") {
//						return new [] { pt.TypeArguments.First() };
//					}
//				}
//			}
//
//			if (expr.Parent is UnaryOperatorExpressionSyntax) {
//				var uop = (UnaryOperatorExpressionSyntax)expr.Parent;
//				switch (uop.Operator) {
//					case UnaryOperatorType.Not:
//						return new [] { model.Compilation.FindType(KnownTypeCode.Boolean) };
//						case UnaryOperatorType.Minus:
//						case UnaryOperatorType.Plus:
//						case UnaryOperatorType.Increment:
//						case UnaryOperatorType.Decrement:
//						case UnaryOperatorType.PostIncrement:
//						case UnaryOperatorType.PostDecrement:
//						return new [] { model.Compilation.FindType(KnownTypeCode.Int32) };
//				}
//			}
//
//			if (expr.Parent is ConstructorInitializer)
//				return GuessFromConstructorInitializer(model, expr);
//
//			if (expr.Parent is NamedExpressionSyntax) {
//				var rr = model.Resolve(expr.Parent);
//				if (!rr.IsError) {
//					return new [] { rr.Type };
//				}
//			}

			if (expr.IsKind(SyntaxKind.Argument)) {
				var parent = expr.Parent.Parent;
				var invocationParent = parent as InvocationExpressionSyntax;
				if (invocationParent != null) {
					return GetAllValidTypesFromInvocation(model, invocationParent, expr);
				}
			}

			var types = typeInferenceService.InferTypes(model, expr, cancellationToken).ToList();

			return types;
		}

		public static readonly CSharpTypeInferenceService typeInferenceService = new CSharpTypeInferenceService ();

		public static TypeSyntax GuessAstType(SemanticModel context, SyntaxNode expr, CancellationToken cancellationToken = default(CancellationToken))
		{
			var types = GetValidTypes(context, expr,cancellationToken).ToList();
			/*var typeInference = new TypeInference(context.Compilation);
			typeInference.Algorithm = TypeInferenceAlgorithm.Improved;
			var inferedType = typeInference.FindTypeInBounds(type, emptyTypes);*/

			if (types.Count == 0)
				return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));

			var resultType = types[0];

			foreach (var type in types) {
				if (type.SpecialType == SpecialType.System_Object) {
					resultType = type;
					break;
				}
			}

			return resultType.GenerateTypeSyntax ();
		}


		public static ITypeSymbol GuessType(SemanticModel context, SyntaxNode expr, CancellationToken cancellationToken = default(CancellationToken))
		{
			var types = GetValidTypes(context, expr,cancellationToken).ToList();
			/*var typeInference = new TypeInference(context.Compilation);
			typeInference.Algorithm = TypeInferenceAlgorithm.Improved;
			var inferedType = typeInference.FindTypeInBounds(type, emptyTypes);*/

			if (types.Count == 0)
				return context.Compilation.GetTypeSymbol ("System", "Object", 0, cancellationToken);

			var resultType = types[0];

			foreach (var type in types) {
				if (type.SpecialType == SpecialType.System_Object) {
					resultType = type;
					break;
				}
			}

			return resultType;
		}

//		public static ITypeSymbol GuessType(BaseRefactoringContext context, SyntaxNode expr)
//		{
//			if (expr is SimpleType && expr.Role == Roles.TypeArgument) {
//				if (expr.Parent is MemberReferenceExpressionSyntax || expr.Parent is IdentifierExpressionSyntax) {
//					var rr = context.Resolve (expr.Parent);
//					var argumentNumber = expr.Parent.GetChildrenByRole (Roles.TypeArgument).TakeWhile (c => c != expr).Count ();
//
//					var mgrr = rr as MethodGroupResolveResult;
//					if (mgrr != null && mgrr.Methods.Any () && mgrr.Methods.First ().TypeArguments.Count > argumentNumber)
//						return mgrr.Methods.First ().TypeParameters[argumentNumber]; 
//				} else if (expr.Parent is MemberType || expr.Parent is SimpleType) {
//					var rr = context.Resolve (expr.Parent);
//					var argumentNumber = expr.Parent.GetChildrenByRole (Roles.TypeArgument).TakeWhile (c => c != expr).Count ();
//					var mgrr = rr as TypeResolveResult;
//					if (mgrr != null &&  mgrr.Type.TypeParameterCount > argumentNumber) {
//						return mgrr.Type.GetDefinition ().TypeParameters[argumentNumber]; 
//					}
//				}
//			}
//
//			var type = GetValidTypes(context.Resolver, expr).ToArray();
//			var typeInference = new TypeInference(context.Compilation);
//			typeInference.Algorithm = TypeInferenceAlgorithm.Improved;
//			var inferedType = typeInference.FindTypeInBounds(type, emptyTypes);
//			return inferedType;
//		}
	}
}

