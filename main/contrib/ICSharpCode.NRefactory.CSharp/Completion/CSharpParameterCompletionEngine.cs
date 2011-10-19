// 
// CSharpParameterCompletionEngine.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Completion;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Completion
{
	public class CSharpParameterCompletionEngine : CSharpCompletionEngineBase
	{
		internal IParameterCompletionDataFactory factory;
		
		public CSharpParameterCompletionEngine (IDocument document, IParameterCompletionDataFactory factory)
		{
			this.document = document;
			this.factory = factory;
		}
		
		public IParameterDataProvider GetParameterDataProvider (int offset)
		{
			if (offset <= 0)
				return null;
			SetOffset (offset);
			
			char completionChar = document.GetCharAt (offset - 1);
			if (completionChar != '(' && completionChar != '<' && completionChar != '[')
				return null;
			if (IsInsideComment () || IsInsideString ())
				return null;
			
			var invoke = GetInvocationBeforeCursor (true);
			if (invoke == null)
				return null;
			
			ResolveResult resolveResult;
			switch (completionChar) {
			case '(':
				if (invoke.Item2 is ObjectCreateExpression) {
					var createType = ResolveExpression (invoke.Item1, ((ObjectCreateExpression)invoke.Item2).Type, invoke.Item3);
					return factory.CreateConstructorProvider (createType.Item1.Type);
				}
				
				if (invoke.Item2 is ICSharpCode.NRefactory.CSharp.Attribute) {
					var attribute = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
					if (attribute == null || attribute.Item1 == null)
						return null;
					return factory.CreateConstructorProvider (attribute.Item1.Type);
				}
				
				var invocationExpression = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
				
				if (invocationExpression == null || invocationExpression.Item1 == null || invocationExpression.Item1.IsError)
					return null;
				resolveResult = invocationExpression.Item1;
				if (resolveResult is MethodGroupResolveResult)
					return factory.CreateMethodDataProvider (resolveResult as MethodGroupResolveResult);
				if (resolveResult is MemberResolveResult) {
					if (resolveResult.Type.Kind == TypeKind.Delegate)
						return factory.CreateDelegateDataProvider (resolveResult.Type);
					var mr = resolveResult as MemberResolveResult;
					if (mr.Member is IMethod)
						return factory.CreateMethodDataProvider ((IMethod)mr.Member);
				}
				
//				
//				if (result.ExpressionContext == ExpressionContext.BaseConstructorCall) {
//					if (resolveResult is ThisResolveResult)
//						return new NRefactoryParameterDataProvider (textEditorData, resolver, resolveResult as ThisResolveResult);
//					if (resolveResult is BaseResolveResult)
//						return new NRefactoryParameterDataProvider (textEditorData, resolver, resolveResult as BaseResolveResult);
//				}
//				IType resolvedType = resolver.SearchType (resolveResult.ResolvedType);
//				if (resolvedType != null && resolvedType.ClassType == ClassType.Delegate) {
//					return new NRefactoryParameterDataProvider (textEditorData, result.Expression, resolvedType);
//				}
				break;
				
//			case '<':
//				if (string.IsNullOrEmpty (result.Expression))
//					return null;
//				return new NRefactoryTemplateParameterDataProvider (textEditorData, resolver, GetUsedNamespaces (), result, new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//			case '[': {
//				ResolveResult resolveResult = resolver.Resolve (result, new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//				if (resolveResult != null && !resolveResult.StaticResolve) {
//					IType type = dom.GetType (resolveResult.ResolvedType);
//					if (type != null)
//						return new NRefactoryIndexerParameterDataProvider (textEditorData, type, result.Expression);
//				}
//				return null;
//			}
				
			}
			return null;
		}
		
		List<string> GetUsedNamespaces ()
		{
			var scope = CSharpParsedFile.GetUsingScope (location);
			var result = new List<string> ();
			while (scope != null) {
				result.Add (scope.NamespaceName);
				foreach (var u in scope.Usings) {
					var ns = u.ResolveNamespace (ctx);
					if (ns == null)
						continue;
					result.Add (ns.NamespaceName);
				}
				scope = scope.Parent;
			}
			return result;
		}
		/*
		public override bool GetParameterCompletionCommandOffset (out int cpos)
		{
			// Start calculating the parameter offset from the beginning of the
			// current member, instead of the beginning of the file. 
			cpos = textEditorData.Caret.Offset - 1;
			var parsedDocument = Document.ParsedDocument;
			if (parsedDocument == null)
				return false;
			IMember mem = currentMember;
			if (mem == null || (mem is IType))
				return false;
			int startPos = textEditorData.LocationToOffset (mem.Region.BeginLine, mem.Region.BeginColumn);
			int parenDepth = 0;
			int chevronDepth = 0;
			while (cpos > startPos) {
				char c = textEditorData.GetCharAt (cpos);
				if (c == ')')
					parenDepth++;
				if (c == '>')
					chevronDepth++;
				if (parenDepth == 0 && c == '(' || chevronDepth == 0 && c == '<') {
					int p = MethodParameterDataProvider.GetCurrentParameterIndex (CompletionWidget, cpos + 1, startPos);
					if (p != -1) {
						cpos++;
						return true;
					} else {
						return false;
					}
				}
				if (c == '(')
					parenDepth--;
				if (c == '<')
					chevronDepth--;
				cpos--;
			}
			return false;
		}*/
	}
}

