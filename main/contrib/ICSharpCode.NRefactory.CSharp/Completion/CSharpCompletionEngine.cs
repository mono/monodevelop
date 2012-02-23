﻿// 
// CSharpCompletionEngine.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Completion
{
	public class CSharpCompletionEngine : CSharpCompletionEngineBase
	{
		internal ICompletionDataFactory factory;
		
		#region Additional input properties
		public CSharpFormattingOptions FormattingPolicy { get; set; }

		public string EolMarker { get; set; }

		public string IndentString { get; set; }
		#endregion
		
		#region Result properties
		public bool AutoCompleteEmptyMatch;
		public bool AutoSelect;
		public string DefaultCompletionString;
		public bool CloseOnSquareBrackets;
		#endregion
		
		public CSharpCompletionEngine (IDocument document, ICompletionDataFactory factory, IProjectContent content, CSharpTypeResolveContext ctx, CompilationUnit unit, CSharpParsedFile parsedFile) : base (content, ctx, unit, parsedFile)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (factory == null)
				throw new ArgumentNullException ("factory");
			this.document = document;
			this.factory = factory;
			// Set defaults for additional input properties
			this.FormattingPolicy = new CSharpFormattingOptions();
			this.EolMarker = Environment.NewLine;
			this.IndentString = "\t";
		}

		public IEnumerable<ICompletionData> GetCompletionData (int offset, bool controlSpace)
		{
			this.AutoCompleteEmptyMatch = true;
			this.AutoSelect = true;
			this.DefaultCompletionString = null;
			SetOffset (offset);
			if (offset > 0) {
				char lastChar = document.GetCharAt (offset - 1);
				var result = MagicKeyCompletion (lastChar, controlSpace) ?? Enumerable.Empty<ICompletionData> ();
				if (controlSpace && char.IsWhiteSpace (lastChar)) {
					offset -= 2;
					while (offset >= 0 && char.IsWhiteSpace (document.GetCharAt (offset)))
						offset--;
					if (offset > 0) {
						var nonWsResult = MagicKeyCompletion (document.GetCharAt (offset), controlSpace);
						if (nonWsResult != null) {
							var text = new HashSet<string> (result.Select (r => r.CompletionText));
							result = result.Concat (nonWsResult.Where (r => !text.Contains (r.CompletionText)));
						}
					}
				}
				
				return result;
			}
			return Enumerable.Empty<ICompletionData> ();
		}

		IEnumerable<string> GenerateNameProposals (AstType type)
		{
			if (type is PrimitiveType) {
				var pt = (PrimitiveType)type;
				switch (pt.Keyword) {
				case "object":
					yield return "o";
					yield return "obj";
					break;
				case "bool":
					yield return "b";
					yield return "pred";
					break;
				case "double":
				case "float":
				case "decimal":
					yield return "d";
					yield return "f";
					yield return "m";
					break;
				default:
					yield return "i";
					yield return "j";
					yield return "k";
					break;
				}
				yield break;
			}
			
			var names = new List<string> ();
			int offset1 = document.GetOffset (type.StartLocation);
			int offset2 = document.GetOffset (type.EndLocation);
			
			string name = document.GetText (offset1, offset2 - offset1);
			int lastNameStart = 0;
			for (int i = 1; i < name.Length; i++) {
				if (Char.IsUpper (name [i])) {
					names.Add (name.Substring (lastNameStart, i - lastNameStart));
					lastNameStart = i;
				}
			}
			
			names.Add (name.Substring (lastNameStart, name.Length - lastNameStart));
			
			var possibleName = new StringBuilder ();
			for (int i = 0; i < names.Count; i++) {
				possibleName.Length = 0;
				for (int j = i; j < names.Count; j++) {
					if (string.IsNullOrEmpty (names [j]))
						continue;
					if (j == i) 
						names [j] = Char.ToLower (names [j] [0]) + names [j].Substring (1);
					possibleName.Append (names [j]);
				}
				yield return possibleName.ToString ();
			}
		}

		IEnumerable<ICompletionData> MagicKeyCompletion (char completionChar, bool controlSpace)
		{
			switch (completionChar) {
			// Magic key completion
			case ':':
			case '.':
				if (IsInsideCommentOrString ())
					return Enumerable.Empty<ICompletionData> ();
				var expr = GetExpressionBeforeCursor ();
				if (expr == null)
					return null;
				// do not complete <number>. (but <number>.<number>.)
				if (expr.Node is PrimitiveExpression) {
					var pexpr = (PrimitiveExpression)expr.Node;
					if (!(pexpr.Value is string || pexpr.Value is char) && !pexpr.LiteralValue.Contains ('.'))
						return null;
				}
				
				var resolveResult = ResolveExpression (expr);
				if (resolveResult == null)
					return null;
				if (expr.Node is AstType)
					return CreateTypeAndNamespaceCompletionData (location, resolveResult.Item1, expr.Node, resolveResult.Item2);
				return CreateCompletionData (location, resolveResult.Item1, expr.Node, resolveResult.Item2);
			case '#':
				if (IsInsideCommentOrString ())
					return null;
				return GetDirectiveCompletionData ();
			
// XML doc completion
			case '<':
				if (IsInsideDocComment ())
					return GetXmlDocumentationCompletionData ();
				if (controlSpace)
					return DefaultControlSpaceItems ();
				return null;
			case '>':
				if (!IsInsideDocComment ())
					return null;
				string lineText = document.GetText (document.GetLineByNumber (location.Line));
				int startIndex = Math.Min (location.Column - 1, lineText.Length - 1);
				
				while (startIndex >= 0 && lineText [startIndex] != '<') {
					--startIndex;
					if (lineText [startIndex] == '/') { // already closed.
						startIndex = -1;
						break;
					}
				}
				
				if (startIndex >= 0) {
					int endIndex = startIndex;
					while (endIndex <= location.Column && endIndex < lineText.Length && !Char.IsWhiteSpace (lineText [endIndex])) {
						endIndex++;
					}
					string tag = endIndex - startIndex - 1 > 0 ? lineText.Substring (startIndex + 1, endIndex - startIndex - 2) : null;
					if (!string.IsNullOrEmpty (tag) && commentTags.IndexOf (tag) >= 0)
						document.Insert (offset, "</" + tag + ">");
				}
				return null;
			
			// Parameter completion
			case '(':
				if (IsInsideCommentOrString ())
					return null;
				var invoke = GetInvocationBeforeCursor (true);
				if (invoke == null)
					return null;
				if (invoke.Node is TypeOfExpression)
					return CreateTypeList ();
				var invocationResult = ResolveExpression (invoke);
				if (invocationResult == null)
					return null;
				var methodGroup = invocationResult.Item1 as MethodGroupResolveResult;
				if (methodGroup != null)
					return CreateParameterCompletion (methodGroup, invocationResult.Item2, invoke.Node, 0, controlSpace);
				
				if (controlSpace)
					return DefaultControlSpaceItems (invoke);
				
				return null;
			case '=':
				return controlSpace ? DefaultControlSpaceItems () : null;
			case ',':
				int cpos2;
				if (!GetParameterCompletionCommandOffset (out cpos2)) 
					return null;
			//	completionContext = CompletionWidget.CreateCodeCompletionContext (cpos2);
			//	int currentParameter2 = MethodParameterDataProvider.GetCurrentParameterIndex (CompletionWidget, completionContext) - 1;
//				return CreateParameterCompletion (CreateResolver (), location, ExpressionContext.MethodBody, provider.Methods, currentParameter);	
				break;
				
			// Completion on space:
			case ' ':
				if (IsInsideCommentOrString ())
					return null;
				
				int tokenIndex = offset;
				string token = GetPreviousToken (ref tokenIndex, false);
				// check propose name, for context <variable name> <ctrl+space> (but only in control space context)
				//IType isAsType = null;
				var isAsExpression = GetExpressionAt (offset);
				if (controlSpace && isAsExpression != null && isAsExpression.Node is VariableDeclarationStatement && token != "new") {
					var parent = isAsExpression.Node as VariableDeclarationStatement;
					var proposeNameList = new CompletionDataWrapper (this);
					
					foreach (var possibleName in GenerateNameProposals (parent.Type)) {
						if (possibleName.Length > 0)
							proposeNameList.Result.Add (factory.CreateLiteralCompletionData (possibleName.ToString ()));
					}
					
					AutoSelect = false;
					AutoCompleteEmptyMatch = false;
					return proposeNameList.Result;
				}
//				int tokenIndex = offset;
//				string token = GetPreviousToken (ref tokenIndex, false);
//				if (result.ExpressionContext == ExpressionContext.ObjectInitializer) {
//					resolver = CreateResolver ();
//					ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForObjectInitializer (document, resolver.Unit, Document.FileName, resolver.CallingType);
//					IReturnType objectInitializer = ((ExpressionContext.TypeExpressionContext)exactContext).UnresolvedType;
//					if (objectInitializer != null && objectInitializer.ArrayDimensions == 0 && objectInitializer.PointerNestingLevel == 0 && (token == "{" || token == ","))
//						return CreateCtrlSpaceCompletionData (completionContext, result); 
//				}
				if (token == "=") {
					int j = tokenIndex;
					string prevToken = GetPreviousToken (ref j, false);
					if (prevToken == "=" || prevToken == "+" || prevToken == "-") {
						token = prevToken + token;
						tokenIndex = j;
					}
				}
				switch (token) {
				case "(":
				case ",":
					int cpos;
					if (!GetParameterCompletionCommandOffset (out cpos)) 
						break;
					int currentParameter = GetCurrentParameterIndex (cpos, 0) - 1;
					if (currentParameter < 0)
						return null;
					invoke = GetInvocationBeforeCursor (token == "(");
					if (invoke == null)
						return null;
					invocationResult = ResolveExpression (invoke);
					if (invocationResult == null)
						return null;
					methodGroup = invocationResult.Item1 as MethodGroupResolveResult;
					if (methodGroup != null)
						return CreateParameterCompletion (methodGroup, invocationResult.Item2, invoke.Node, currentParameter, controlSpace);
					return null;
				case "=":
				case "==":
					GetPreviousToken (ref tokenIndex, false);
					var expressionOrVariableDeclaration = GetExpressionAt (tokenIndex);
					if (expressionOrVariableDeclaration == null)
						return null;
					
					resolveResult = ResolveExpression (expressionOrVariableDeclaration);
					
					if (resolveResult == null)
						return null;
					if (resolveResult.Item1.Type.Kind == TypeKind.Enum) {
						var wrapper = new CompletionDataWrapper (this);
						AddContextCompletion (wrapper, resolveResult.Item2, expressionOrVariableDeclaration.Node);
						AddEnumMembers (wrapper, resolveResult.Item1.Type, resolveResult.Item2);
						AutoCompleteEmptyMatch = false;
						return wrapper.Result;
					}
//				
//					if (resolvedType.FullName == DomReturnType.Bool.FullName) {
//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
//						CompletionDataCollector cdc = new CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
//						completionList.AutoCompleteEmptyMatch = false;
//						cdc.Add ("true", "md-keyword");
//						cdc.Add ("false", "md-keyword");
//						resolver.AddAccessibleCodeCompletionData (result.ExpressionContext, cdc);
//						return completionList;
//					}
//					if (resolvedType.ClassType == ClassType.Delegate && token == "=") {
//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
//						string parameterDefinition = AddDelegateHandlers (completionList, resolvedType);
//						string varName = GetPreviousMemberReferenceExpression (tokenIndex);
//						completionList.Add (new EventCreationCompletionData (document, varName, resolvedType, null, parameterDefinition, resolver.CallingMember, resolvedType));
//						
//						CompletionDataCollector cdc = new CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
//						resolver.AddAccessibleCodeCompletionData (result.ExpressionContext, cdc);
//						foreach (var data in completionList) {
//							if (data is MemberCompletionData) 
//								((MemberCompletionData)data).IsDelegateExpected = true;
//						}
//						return completionList;
//					}
					return null;
				case "+=":
				case "-=":
					GetPreviousToken (ref tokenIndex, false);
					
					expressionOrVariableDeclaration = GetExpressionAt (tokenIndex);
					if (expressionOrVariableDeclaration == null)
						return null;
				
					resolveResult = ResolveExpression (expressionOrVariableDeclaration);
					if (resolveResult == null)
						return null;
					
					
					var mrr = resolveResult.Item1 as MemberResolveResult;
					if (mrr != null) {
						var evt = mrr.Member as IEvent;
						if (evt == null)
							return null;
						var delegateType = evt.ReturnType;
						if (delegateType.Kind != TypeKind.Delegate)
							return null;
						
						var wrapper = new CompletionDataWrapper (this);
						if (currentType != null) {
//							bool includeProtected = DomType.IncludeProtected (dom, typeFromDatabase, resolver.CallingType);
							foreach (var method in currentType.Methods) {
								if (MatchDelegate (delegateType, method) /*&& method.IsAccessibleFrom (dom, resolver.CallingType, resolver.CallingMember, includeProtected) &&*/) {
									wrapper.AddMember (method);
//									data.SetText (data.CompletionText + ";");
								}
							}
						}
						if (token == "+=") {
							string parameterDefinition = AddDelegateHandlers (wrapper, delegateType);
							string varName = GetPreviousMemberReferenceExpression (tokenIndex);
							wrapper.Result.Add (factory.CreateEventCreationCompletionData (varName, delegateType, evt, parameterDefinition, currentMember, currentType));
						}
					
						return wrapper.Result;
					}
					return null;
				case ":":
					
/* Breaks constructor initializer case:
					 * if (currentMember == null) {
						var wrapper = new CompletionDataWrapper (this);
						AddTypesAndNamespaces (wrapper, GetState (), null, t => currentType != null ? !currentType.Equals (t) : true);
						return wrapper.Result;
					}*/
					return null;
				}
				
				var keywordCompletion = HandleKeywordCompletion (tokenIndex, token);
				if (keywordCompletion == null && controlSpace)
					goto default;
				return keywordCompletion;
			// Automatic completion
			default:
				if (IsInsideCommentOrString ())
					return null;
				if (IsInLinqContext (offset)) {
					if (!controlSpace && !(char.IsLetter (completionChar) || completionChar == '_'))
						return null;
					tokenIndex = offset;
					token = GetPreviousToken (ref tokenIndex, false); // token last typed
					if (!char.IsWhiteSpace (completionChar) && !linqKeywords.Contains (token))
						token = GetPreviousToken (ref tokenIndex, false); // token last typed
					
					if (linqKeywords.Contains (token)) {
						if (token == "from") // after from no auto code completion.
							return null;
						return DefaultControlSpaceItems ();
					}
					var dataList = new CompletionDataWrapper (this);
					AddKeywords (dataList, linqKeywords);
					return dataList.Result;
				}
				if (currentType != null && currentType.Kind == TypeKind.Enum)
					return HandleEnumContext ();
				
				var contextList = new CompletionDataWrapper (this);
				var identifierStart = GetExpressionAtCursor ();
				
				if (identifierStart != null && identifierStart.Node is TypeParameterDeclaration)
					return null;
				
				if (identifierStart != null && identifierStart.Node is VariableInitializer && location <= ((VariableInitializer)identifierStart.Node).NameToken.EndLocation) {
					return controlSpace ? HandleAccessorContext () ?? DefaultControlSpaceItems (identifierStart) : null;
				}
				if (!(char.IsLetter (completionChar) || completionChar == '_') && (!controlSpace || identifierStart == null || !(identifierStart.Node is ArrayInitializerExpression))) {
					return controlSpace ? HandleAccessorContext () ?? DefaultControlSpaceItems (identifierStart) : null;
				}
				char prevCh = offset > 2 ? document.GetCharAt (offset - 2) : ';';
				char nextCh = offset < document.TextLength ? document.GetCharAt (offset) : ' ';
				const string allowedChars = ";,[](){}+-*/%^?:&|~!<>=";
				if (!Char.IsWhiteSpace (nextCh) && allowedChars.IndexOf (nextCh) < 0)
					return null;
				if (!(Char.IsWhiteSpace (prevCh) || allowedChars.IndexOf (prevCh) >= 0))
					return null;
				// Do not pop up completion on identifier identifier (should be handled by keyword completion).
				tokenIndex = offset - 1;
				token = GetPreviousToken (ref tokenIndex, false);
				if (token == "class" || token == "interface" || token == "struct" || token == "enum" || token == "namespace") // after these always follows a name
					return null;
				var keywordresult = HandleKeywordCompletion (tokenIndex, token);
				if (keywordresult != null)
					return keywordresult;
				int prevTokenIndex = tokenIndex;
				var prevToken2 = GetPreviousToken (ref prevTokenIndex, false);
				if (prevToken2 == "delegate") // after these always follows a name
					return null;
				if (identifierStart == null && !string.IsNullOrEmpty (token) && !(IsInsideComment (tokenIndex) || IsInsideString (tokenIndex)) && (prevToken2 == ";" || prevToken2 == "{" || prevToken2 == "}")) {
					char last = token [token.Length - 1];
					if (char.IsLetterOrDigit (last) || last == '_' || token == ">") {
						return HandleKeywordCompletion (tokenIndex, token);
					}
				}
				
				if (identifierStart == null) {
					var accCtx = HandleAccessorContext ();
					if (accCtx != null)
						return accCtx;
					return DefaultControlSpaceItems (null, controlSpace);
				}
				CSharpResolver csResolver;
				AstNode n = identifierStart.Node;
				
				if (n != null && n.Parent is AnonymousTypeCreateExpression) {
					AutoSelect = false;
				}
				
				// Handle foreach (type name _
				if (n is IdentifierExpression) {
					var prev = n.GetPrevNode () as ForeachStatement;
					if (prev != null && prev.InExpression.IsNull) {
						if (controlSpace) {
							contextList.AddCustom ("in");
							return contextList.Result;
						}
						return null;
					}
				}
				if (n != null && n.Parent is InvocationExpression) {
					var invokeParent = (InvocationExpression)n.Parent;
					var invokeResult = ResolveExpression (invokeParent.Target, identifierStart.Unit);
					var mgr = invokeResult != null ? invokeResult.Item1 as MethodGroupResolveResult : null;
					if (mgr != null) {
						int idx = 0;
						foreach (var arg in invokeParent.Arguments) {
							if (arg == n)
								break;
							idx++;
						}
						
						foreach (var method in mgr.Methods) {
							if (idx < method.Parameters.Count && method.Parameters [idx].Type.Kind == TypeKind.Delegate) {
								AutoSelect = false;
								AutoCompleteEmptyMatch = false;
							}
							foreach (var p in method.Parameters) {
								contextList.AddVariable (p);
							}
						}
						idx++;
						foreach (var list in mgr.GetExtensionMethods ()) {
							foreach (var method in list) {
								if (idx < method.Parameters.Count && method.Parameters [idx].Type.Kind == TypeKind.Delegate) {
									AutoSelect = false;
									AutoCompleteEmptyMatch = false;
								}
								
							}
						}
					}
				}
				
				if (n != null && n.Parent is ObjectCreateExpression) {
					var invokeResult = ResolveExpression (n.Parent, identifierStart.Unit);
					var mgr = invokeResult != null ? invokeResult.Item1 as ResolveResult : null;
					
					if (mgr != null) {
						foreach (var constructor in mgr.Type.GetConstructors ()) {
							foreach (var p in constructor.Parameters) {
								contextList.AddVariable (p);
							}
						}
					}
				}
				
				if (n is IdentifierExpression) {
					var bop = n.Parent as BinaryOperatorExpression;
					Expression evaluationExpr = null;
					
					if (bop != null && bop.Right == n && (bop.Operator == BinaryOperatorType.Equality || bop.Operator == BinaryOperatorType.InEquality)) {
						evaluationExpr = bop.Left;
					}
					// check for compare to enum case 
					if (evaluationExpr != null) {
						resolveResult = ResolveExpression (evaluationExpr, identifierStart.Unit);
						if (resolveResult != null && resolveResult.Item1.Type.Kind == TypeKind.Enum) {
							var wrapper = new CompletionDataWrapper (this);
							AddContextCompletion (wrapper, resolveResult.Item2, evaluationExpr);
							AddEnumMembers (wrapper, resolveResult.Item1.Type, resolveResult.Item2);
							AutoCompleteEmptyMatch = false;
							return wrapper.Result;
						}
					}
				}
				
				
				if (n is Identifier && n.Parent is ForeachStatement) {
					if (controlSpace)
						return DefaultControlSpaceItems ();
					return null;
				}
				if (n is ArrayInitializerExpression) {
					// check for new [] {...} expression -> no need to resolve the type there
					var parent = n.Parent as ArrayCreateExpression;
					if (parent != null && parent.Type.IsNull)
						return DefaultControlSpaceItems ();
					
					var initalizerResult = ResolveExpression (n.Parent, identifierStart.Unit);
					
					var concreteNode = identifierStart.Unit.GetNodeAt<IdentifierExpression> (location);
					// check if we're on the right side of an initializer expression
					if (concreteNode != null && concreteNode.Parent != null && concreteNode.Parent.Parent != null && concreteNode.Identifier != "a" && concreteNode.Parent.Parent is NamedExpression) {
						return DefaultControlSpaceItems ();
					}
					
					if (initalizerResult != null && initalizerResult.Item1.Type.Kind != TypeKind.Unknown) { 
						
						foreach (var property in initalizerResult.Item1.Type.GetProperties ()) {
							if (!property.IsPublic)
								continue;
							contextList.AddMember (property);
						}
						foreach (var field in initalizerResult.Item1.Type.GetFields ()) {       
							if (!field.IsPublic)
								continue;
							contextList.AddMember (field);
						}
						return contextList.Result;
					}
					return DefaultControlSpaceItems ();
				}
				if (n != null/* && !(identifierStart.Item2 is TypeDeclaration)*/) {
					csResolver = new CSharpResolver (ctx);
					var nodes = new List<AstNode> ();
					nodes.Add (n);
					if (n.Parent is ICSharpCode.NRefactory.CSharp.Attribute)
						nodes.Add (n.Parent);
					var astResolver = new CSharpAstResolver (csResolver, identifierStart.Unit, CSharpParsedFile);
					astResolver.ApplyNavigator (new NodeListResolveVisitorNavigator (nodes));
					try {
						csResolver = astResolver.GetResolverStateBefore (n);
					} catch (Exception) {
						csResolver = GetState ();
					}
					// add attribute properties.
					if (n.Parent is ICSharpCode.NRefactory.CSharp.Attribute) {
						var resolved = astResolver.Resolve (n.Parent);
						if (resolved != null && resolved.Type != null) {
							foreach (var property in resolved.Type.GetProperties (p => p.Accessibility == Accessibility.Public)) {
								contextList.AddMember (property);
							}
							foreach (var field in resolved.Type.GetFields (p => p.Accessibility == Accessibility.Public)) {
								contextList.AddMember (field);
							}
						}
					}
				} else {
					csResolver = GetState ();
				}
				// identifier has already started with the first letter
				offset--;
				AddContextCompletion (contextList, csResolver, identifierStart.Node);
				return contextList.Result;
//				if (stub.Parent is BlockStatement)
				
//				result = FindExpression (dom, completionContext, -1);
//				if (result == null)
//					return null;
//				 else if (result.ExpressionContext != ExpressionContext.IdentifierExpected) {
//					triggerWordLength = 1;
//					bool autoSelect = true;
//					IType returnType = null;
//					if ((prevCh == ',' || prevCh == '(') && GetParameterCompletionCommandOffset (out cpos)) {
//						ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
//						NRefactoryParameterDataProvider dataProvider = ParameterCompletionCommand (ctx) as NRefactoryParameterDataProvider;
//						if (dataProvider != null) {
//							int i = dataProvider.GetCurrentParameterIndex (CompletionWidget, ctx) - 1;
//							foreach (var method in dataProvider.Methods) {
//								if (i < method.Parameters.Count) {
//									returnType = dom.GetType (method.Parameters [i].ReturnType);
//									autoSelect = returnType == null || returnType.ClassType != ClassType.Delegate;
//									break;
//								}
//							}
//						}
//					}
//					// Bug 677531 - Auto-complete doesn't always highlight generic parameter in method signature
//					//if (result.ExpressionContext == ExpressionContext.TypeName)
//					//	autoSelect = false;
//					CompletionDataList dataList = CreateCtrlSpaceCompletionData (completionContext, result);
//					AddEnumMembers (dataList, returnType);
//					dataList.AutoSelect = autoSelect;
//					return dataList;
//				} else {
//					result = FindExpression (dom, completionContext, 0);
//					tokenIndex = offset;
//					
//					// check foreach case, unfortunately the expression finder is too dumb to handle full type names
//					// should be overworked if the expression finder is replaced with a mcs ast based analyzer.
//					var possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // starting letter
//					possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // varname
//				
//					// read return types to '(' token
//					possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // varType
//					if (possibleForeachToken == ">") {
//						while (possibleForeachToken != null && possibleForeachToken != "(") {
//							possibleForeachToken = GetPreviousToken (ref tokenIndex, false);
//						}
//					} else {
//						possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // (
//						if (possibleForeachToken == ".")
//							while (possibleForeachToken != null && possibleForeachToken != "(")
//								possibleForeachToken = GetPreviousToken (ref tokenIndex, false);
//					}
//					possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // foreach
//				
//					if (possibleForeachToken == "foreach") {
//						result.ExpressionContext = ExpressionContext.ForeachInToken;
//					} else {
//						return null;
//						//								result.ExpressionContext = ExpressionContext.IdentifierExpected;
//					}
//					result.Expression = "";
//					result.Region = DomRegion.Empty;
//				
//					return CreateCtrlSpaceCompletionData (completionContext, result);
//				}
//				break;
			}
			return null;
		}
		
		IEnumerable<ICompletionData> HandleEnumContext ()
		{
			var cu = ParseStub ("a", false);
			if (cu == null)
				return null;
			var member = cu.GetNodeAt<EnumMemberDeclaration> (location);
			if (member != null && member.NameToken.EndLocation < location)
				return DefaultControlSpaceItems ();
			return null;
		}
		
		bool IsInLinqContext (int offset)
		{
			string token;
			while (null != (token = GetPreviousToken (ref offset, true)) && !IsInsideComment (offset) && !IsInsideString (offset)) {
				if (token == "from")
					return true;
				if (token == ";" || token == "{")
					return false;
			}
			return false;
		}
		
		IEnumerable<ICompletionData> HandleAccessorContext ()
		{
			var unit = ParseStub ("get; }", false);
			var node = unit.GetNodeAt (location, cn => !(cn is CSharpTokenNode));
			if (node is Accessor)
				node = node.Parent;
			var contextList = new CompletionDataWrapper (this);
			if (node is PropertyDeclaration) {
				contextList.AddCustom ("get");
				contextList.AddCustom ("set");
				AddKeywords (contextList, accessorModifierKeywords);
			} else if (node is CustomEventDeclaration) {
				contextList.AddCustom ("add");
				contextList.AddCustom ("remove");
			} else {
				return null;
			}
			
			return contextList.Result;
		}
		
		IEnumerable<ICompletionData> DefaultControlSpaceItems (ExpressionResult xp = null, bool controlSpace = true)
		{
			var wrapper = new CompletionDataWrapper (this);
			if (offset >= document.TextLength)
				offset = document.TextLength - 1;
			while (offset > 1 && char.IsWhiteSpace (document.GetCharAt (offset))) {
				offset--;
			}
			location = document.GetLocation (offset);
			
			if (xp == null)
				xp = GetExpressionAtCursor ();
			AstNode node;
			Tuple<ResolveResult, CSharpResolver> rr;
			if (xp != null) {
				node = xp.Node;
				rr = ResolveExpression (node, xp.Unit);
			} else {
				var unit = ParseStub ("a");
				node = unit.GetNodeAt (location);
				rr = ResolveExpression (node, unit);
			}
			
			if (node is Identifier && node.Parent is ForeachStatement) {
				var foreachStmt = (ForeachStatement)node.Parent;
				foreach (var possibleName in GenerateNameProposals (foreachStmt.VariableType)) {
					if (possibleName.Length > 0)
						wrapper.Result.Add (factory.CreateLiteralCompletionData (possibleName.ToString ()));
				}
					
				AutoSelect = false;
				AutoCompleteEmptyMatch = false;
				return wrapper.Result;
			}
			
			if (node is Identifier && node.Parent is ParameterDeclaration) {
				if (!controlSpace)
					return null;
				// Try Parameter name case 
				var param = node.Parent as ParameterDeclaration;
				if (param != null) {
					foreach (var possibleName in GenerateNameProposals (param.Type)) {
						if (possibleName.Length > 0)
							wrapper.Result.Add (factory.CreateLiteralCompletionData (possibleName.ToString ()));
					}
					AutoSelect = false;
					AutoCompleteEmptyMatch = false;
					return wrapper.Result;
				}
			}
			if (Unit != null && (node == null || node is TypeDeclaration)) {
				var constructor = Unit.GetNodeAt<ConstructorDeclaration> (location.Line, location.Column - 3);
				if (constructor != null && !constructor.ColonToken.IsNull && constructor.Initializer.IsNull) {
					wrapper.AddCustom ("this");
					wrapper.AddCustom ("base");
					return wrapper.Result;
				}
			}
			CSharpResolver csResolver = null;
			if (rr != null)
				csResolver = rr.Item2;
			if (csResolver == null) {
				if (node != null) {
					csResolver =  GetState ();
					//var astResolver = new CSharpAstResolver (csResolver, node, xp != null ? xp.Item1 : CSharpParsedFile);
					
					try {
						//csResolver = astResolver.GetResolverStateBefore (node);
						Console.WriteLine (csResolver.LocalVariables.Count ());
					} catch (Exception  e)  {
						Console.WriteLine ("E!!!" + e);
					}
					
				} else {
					csResolver = GetState ();
				}
			}
			
			AddContextCompletion (wrapper, csResolver, node);
			
			return wrapper.Result;
		}
		
		void AddContextCompletion (CompletionDataWrapper wrapper, CSharpResolver state, AstNode node)
		{
			if (state != null && !(node is AstType)) {
				foreach (var variable in state.LocalVariables) {
					if (variable.Region.IsInside (location.Line, location.Column - 1))
						continue;
					wrapper.AddVariable (variable);
				}
			}
			
			if (currentMember is IUnresolvedParameterizedMember && !(node is AstType)) {
				var param = (IParameterizedMember)currentMember.CreateResolved (ctx);
				foreach (var p in param.Parameters) {
					wrapper.AddVariable (p);
				}
			}
			
			if (currentMember is IUnresolvedMethod) {
				var method = (IUnresolvedMethod)currentMember;
				foreach (var p in method.TypeParameters) {
					wrapper.AddTypeParameter (p);
				}
			}
			
			Predicate<IType> typePred = null;
			if (node is Attribute) {
				var attribute = Compilation.FindType (KnownTypeCode.Attribute);
				typePred = t => {
					return t.GetAllBaseTypeDefinitions ().Any (bt => bt.Equals (attribute));
				};
			}
			AddTypesAndNamespaces (wrapper, state, node, typePred);
			
			wrapper.Result.Add (factory.CreateLiteralCompletionData ("global"));
			
			if (!(node is AstType)) {
				if (currentMember != null) {
					AddKeywords (wrapper, statementStartKeywords);
					AddKeywords (wrapper, expressionLevelKeywords);
				} else if (currentType != null) {
					AddKeywords (wrapper, typeLevelKeywords);
				} else {
					AddKeywords (wrapper, globalLevelKeywords);
				}
				var prop = currentMember as IUnresolvedProperty;
				if (prop != null && prop.Setter != null && prop.Setter.Region.IsInside (location))
					wrapper.AddCustom ("value"); 
				if (currentMember is IUnresolvedEvent)
					wrapper.AddCustom ("value"); 
				
				if (IsInSwitchContext (node)) {
					wrapper.AddCustom ("case"); 
				}
				
			}
			AddKeywords (wrapper, primitiveTypesKeywords);
			if (currentMember != null)
				wrapper.AddCustom ("var"); 
			wrapper.Result.AddRange (factory.CreateCodeTemplateCompletionData ());
			
			if (node != null && node.Role == AstNode.Roles.Argument) {
				var resolved = ResolveExpression (node.Parent, Unit);
				var invokeResult = resolved != null ? resolved.Item1 as CSharpInvocationResolveResult : null;
				if (invokeResult != null) {
					int argNum = 0;
					foreach (var arg in node.Parent.Children.Where (c => c.Role == AstNode.Roles.Argument)) {
						if (arg == node)
							break;
						argNum++;
					}
					var param = argNum < invokeResult.Member.Parameters.Count ? invokeResult.Member.Parameters [argNum] : null;
					if (param != null && param.Type.Kind == TypeKind.Enum) {
						AddEnumMembers (wrapper, param.Type, state);
					}
				}
			}
			
		}
		
		static bool IsInSwitchContext (AstNode node)
		{
			var n = node;
			while (n != null && !(n is MemberDeclaration)) {
				if (n is SwitchStatement)
					return true;
				if (n is BlockStatement)
					return false;
				n = n.Parent;
			}
			return false;
		}
		
		void AddTypesAndNamespaces (CompletionDataWrapper wrapper, CSharpResolver state, AstNode node, Predicate<IType> typePred = null, Predicate<IMember> memberPred = null)
		{
			if (currentType != null) {
				for (var ct = currentType; ct != null; ct = ct.DeclaringTypeDefinition) {
					foreach (var nestedType in ct.NestedTypes) {
						if (typePred == null || typePred (nestedType.Resolve (ctx))) {
							string name = nestedType.Name;
							if (node is Attribute && name.EndsWith ("Attribute") && name.Length > "Attribute".Length)
								name = name.Substring (0, name.Length - "Attribute".Length);
							wrapper.AddType (nestedType, name);
						}
					}
				}
				if (this.currentMember != null && !(node is AstType)) {
					var def = ctx.CurrentTypeDefinition ?? Compilation.MainAssembly.GetTypeDefinition (currentType);
					if (def != null) {
						foreach (var member in def.GetMembers ()) {
							if (member is IMethod && ((IMethod)member).FullName == "System.Object.Finalize")
								continue;
							if (member.EntityType == EntityType.Operator)
								continue;
							if (memberPred == null || memberPred (member))
								wrapper.AddMember (member);
						}
						var declaring = def.DeclaringTypeDefinition;
						while (declaring != null) {
							foreach (var member in declaring.GetMembers (m => m.IsStatic)) {
								if (memberPred == null || memberPred (member))
									wrapper.AddMember (member);
							}
							declaring = declaring.DeclaringTypeDefinition;
						}
					}
				}
				foreach (var p in currentType.TypeParameters) {
					wrapper.AddTypeParameter (p);
				}
			}
			var scope = CSharpParsedFile.GetUsingScope (location).Resolve (Compilation);
			
			for (var n = scope; n != null; n = n.Parent) {
				foreach (var pair in n.UsingAliases) {
					wrapper.AddNamespace (pair.Key);
				}
				foreach (var u in n.Usings) {
					foreach (var type in u.Types) {
						if (typePred == null || typePred (type)) {
							string name = type.Name;
							if (node is Attribute && name.EndsWith ("Attribute") && name.Length > "Attribute".Length)
								name = name.Substring (0, name.Length - "Attribute".Length);
							wrapper.AddType (type, name);
						}
					}
				}
				
				foreach (var type in n.Namespace.Types) {
					if (typePred == null || typePred (type))
						wrapper.AddType (type, type.Name);
				}
				
				foreach (var curNs in n.Namespace.ChildNamespaces) {
					wrapper.AddNamespace (curNs.Name);
				}
			}
		}
		
		IEnumerable<ICompletionData> HandleKeywordCompletion (int wordStart, string word)
		{
			if (IsInsideCommentOrString ())
				return null;
			switch (word) {
			case "using":
			case "namespace":
				if (currentType != null)
					return null;
				var wrapper = new CompletionDataWrapper (this);
				AddTypesAndNamespaces (wrapper, GetState (), null, t => false);
				return wrapper.Result;
			case "case":
				return CreateCaseCompletionData (location);
//				case ",":
//				case ":":
//					if (result.ExpressionContext == ExpressionContext.InheritableType) {
//						IType cls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
//						List<string > namespaceList = GetUsedNamespaces ();
//						var col = new CSharpTextEditorCompletion.CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, null, location);
//						bool isInterface = false;
//						HashSet<string > baseTypeNames = new HashSet<string> ();
//						if (cls != null) {
//							baseTypeNames.Add (cls.Name);
//							if (cls.ClassType == ClassType.Struct)
//								isInterface = true;
//						}
//						int tokenIndex = offset;
//	
//						// Search base types " : [Type1, ... ,TypeN,] <Caret>"
//						string token = null;
//						do {
//							token = GetPreviousToken (ref tokenIndex, false);
//							if (string.IsNullOrEmpty (token))
//								break;
//							token = token.Trim ();
//							if (Char.IsLetterOrDigit (token [0]) || token [0] == '_') {
//								IType baseType = dom.SearchType (Document.CompilationUnit, cls, result.Region.Start, token);
//								if (baseType != null) {
//									if (baseType.ClassType != ClassType.Interface)
//										isInterface = true;
//									baseTypeNames.Add (baseType.Name);
//								}
//							}
//						} while (token != ":");
//						foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
//							IType type = o as IType;
//							if (type != null && (type.IsStatic || type.IsSealed || baseTypeNames.Contains (type.Name) || isInterface && type.ClassType != ClassType.Interface)) {
//								continue;
//							}
//							if (o is Namespace && !namespaceList.Any (ns => ns.StartsWith (((Namespace)o).FullName)))
//								continue;
//							col.Add (o);
//						}
//						// Add inner classes
//						Stack<IType > innerStack = new Stack<IType> ();
//						innerStack.Push (cls);
//						while (innerStack.Count > 0) {
//							IType curType = innerStack.Pop ();
//							if (curType == null)
//								continue;
//							foreach (IType innerType in curType.InnerTypes) {
//								if (innerType != cls)
//									// don't add the calling class as possible base type
//									col.Add (innerType);
//							}
//							if (curType.DeclaringType != null)
//								innerStack.Push (curType.DeclaringType);
//						}
//						return completionList;
//					}
//					break;
			case "is":
			case "as":
				if (currentType == null)
					return null;
				IType isAsType = null;
				var isAsExpression = GetExpressionAt (wordStart);
				if (isAsExpression != null) {
					var parent = isAsExpression.Node.Parent;
					if (parent is VariableInitializer)
						parent = parent.Parent;
					if (parent is VariableDeclarationStatement) {
						var resolved = ResolveExpression (parent, isAsExpression.Unit);
						if (resolved != null)
							isAsType = resolved.Item1.Type;
					}
				}
				var isAsWrapper = new CompletionDataWrapper (this);
				AddTypesAndNamespaces (isAsWrapper, GetState (), null, t => isAsType == null || t.GetDefinition ().IsDerivedFrom (isAsType.GetDefinition ()), m => false);
				return isAsWrapper.Result;
//					{
//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
//						ExpressionResult expressionResult = FindExpression (dom, completionContext, wordStart - document.Caret.Offset);
//						NRefactoryResolver resolver = CreateResolver ();
//						ResolveResult resolveResult = resolver.Resolve (expressionResult, new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//						if (resolveResult != null && resolveResult.ResolvedType != null) {
//							CompletionDataCollector col = new CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
//							IType foundType = null;
//							if (word == "as") {
//								ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForAsCompletion (document, Document.CompilationUnit, Document.FileName, resolver.CallingType);
//								if (exactContext is ExpressionContext.TypeExpressionContext) {
//									foundType = resolver.SearchType (((ExpressionContext.TypeExpressionContext)exactContext).Type);
//									AddAsCompletionData (col, foundType);
//								}
//							}
//						
//							if (foundType == null)
//								foundType = resolver.SearchType (resolveResult.ResolvedType);
//						
//							if (foundType != null) {
//								if (foundType.ClassType == ClassType.Interface)
//									foundType = resolver.SearchType (DomReturnType.Object);
//							
//								foreach (IType type in dom.GetSubclasses (foundType)) {
//									if (type.IsSpecialName || type.Name.StartsWith ("<"))
//										continue;
//									AddAsCompletionData (col, type);
//								}
//							}
//							List<string > namespaceList = GetUsedNamespaces ();
//							foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
//								if (o is IType) {
//									IType type = (IType)o;
//									if (type.ClassType != ClassType.Interface || type.IsSpecialName || type.Name.StartsWith ("<"))
//										continue;
//	//								if (foundType != null && !dom.GetInheritanceTree (foundType).Any (x => x.FullName == type.FullName))
//	//									continue;
//									AddAsCompletionData (col, type);
//									continue;
//								}
//								if (o is Namespace)
//									continue;
//								col.Add (o);
//							}
//							return completionList;
//						}
//						result.ExpressionContext = ExpressionContext.TypeName;
//						return CreateCtrlSpaceCompletionData (completionContext, result);
//					}
			case "override":
				// Look for modifiers, in order to find the beginning of the declaration
				int firstMod = wordStart;
				int i = wordStart;
				for (int n = 0; n < 3; n++) {
					string mod = GetPreviousToken (ref i, true);
					if (mod == "public" || mod == "protected" || mod == "private" || mod == "internal" || mod == "sealed") {
						firstMod = i;
					} else if (mod == "static") {
						// static methods are not overridable
						return null;
					} else
						break;
				}
				if (!IsLineEmptyUpToEol ())
					return null;
				if (currentType != null && (currentType.Kind == TypeKind.Class || currentType.Kind == TypeKind.Struct)) {
					string modifiers = document.GetText (firstMod, wordStart - firstMod);
					return GetOverrideCompletionData (currentType, modifiers);
				}
				return null;
			case "partial":
				// Look for modifiers, in order to find the beginning of the declaration
				firstMod = wordStart;
				i = wordStart;
				for (int n = 0; n < 3; n++) {
					string mod = GetPreviousToken (ref i, true);
					if (mod == "public" || mod == "protected" || mod == "private" || mod == "internal" || mod == "sealed") {
						firstMod = i;
					} else if (mod == "static") {
						// static methods are not overridable
						return null;
					} else
						break;
				}
				if (!IsLineEmptyUpToEol ())
					return null;
				var state = GetState ();
				
				if (state.CurrentTypeDefinition != null && (state.CurrentTypeDefinition.Kind == TypeKind.Class || state.CurrentTypeDefinition.Kind == TypeKind.Struct)) {
					string modifiers = document.GetText (firstMod, wordStart - firstMod);
					return GetPartialCompletionData (state.CurrentTypeDefinition, modifiers);
				}
				return null;
				
			case "public":
			case "protected":
			case "private":
			case "internal":
			case "sealed":
			case "static":
				var accessorContext = HandleAccessorContext ();
				if (accessorContext != null)
					return accessorContext;
				wrapper = new CompletionDataWrapper (this);
				state = GetState ();
				if (currentType != null) {
					AddTypesAndNamespaces (wrapper, state, null, null, m => false);
					AddKeywords (wrapper, primitiveTypesKeywords);
				}
				AddKeywords (wrapper, typeLevelKeywords);
				return wrapper.Result;
			case "new":
				int j = offset - 4;
//				string token = GetPreviousToken (ref j, true);
				
				IType hintType = null;
				var expressionOrVariableDeclaration = GetNewExpressionAt (j);
				AstNode newParentNode = null;
				AstType hintTypeAst = null;
				if (expressionOrVariableDeclaration != null) {
					newParentNode = expressionOrVariableDeclaration.Node.Parent;
					if (newParentNode is VariableInitializer)
						newParentNode = newParentNode.Parent;
				}
				if (newParentNode is InvocationExpression) {
					var invoke = (InvocationExpression)newParentNode;
					var resolved = ResolveExpression (invoke, expressionOrVariableDeclaration.Unit);
					if (resolved != null) {
						var mgr = resolved.Item1 as CSharpInvocationResolveResult;
						if (mgr != null) {
							int i1 = 0;
							foreach (var a in invoke.Arguments) {
								if (a == expressionOrVariableDeclaration.Node) {
									if (mgr.Member.Parameters.Count > i1)
										hintType = mgr.Member.Parameters [i1].Type;
									break;
								}
								i1++;
							}
						}
					}
				}
				
				if (newParentNode is ObjectCreateExpression) {
					var invoke = (ObjectCreateExpression)newParentNode;
					var resolved = ResolveExpression (invoke, expressionOrVariableDeclaration.Unit);
					if (resolved != null) {
						var mgr = resolved.Item1 as CSharpInvocationResolveResult;
						if (mgr != null) {
							int i1 = 0;
							foreach (var a in invoke.Arguments) {
								if (a == expressionOrVariableDeclaration.Node) {
									if (mgr.Member.Parameters.Count > i1)
										hintType = mgr.Member.Parameters [i1].Type;
									break;
								}
								i1++;
							}
						}
					}
				}
				
				if (newParentNode is AssignmentExpression) {
					var assign = (AssignmentExpression)newParentNode;
					var resolved = ResolveExpression (assign.Left, expressionOrVariableDeclaration.Unit);
					if (resolved != null) {
						hintType = resolved.Item1.Type;
					}
				}
				
				if (newParentNode is VariableDeclarationStatement) {
					var varDecl = (VariableDeclarationStatement)newParentNode;
					hintTypeAst = varDecl.Type;
					var resolved = ResolveExpression (varDecl.Type, expressionOrVariableDeclaration.Unit);
					if (resolved != null) {
						hintType = resolved.Item1.Type;
					}
				}
				
				if (newParentNode is FieldDeclaration) {
					var varDecl = (FieldDeclaration)newParentNode;
					hintTypeAst = varDecl.ReturnType;
					var resolved = ResolveExpression (varDecl.ReturnType, expressionOrVariableDeclaration.Unit);
					if (resolved != null)
						hintType = resolved.Item1.Type;
				}
				return CreateTypeCompletionData (hintType, hintTypeAst);
//					IType callingType = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new TextLocation (document.Caret.Line, document.Caret.Column));
//					ExpressionContext newExactContext = new NewCSharpExpressionFinder (dom).FindExactContextForNewCompletion (document, Document.CompilationUnit, Document.FileName, callingType);
//					if (newExactContext is ExpressionContext.TypeExpressionContext)
//						return CreateTypeCompletionData (location, callingType, newExactContext, ((ExpressionContext.TypeExpressionContext)newExactContext).Type, ((ExpressionContext.TypeExpressionContext)newExactContext).UnresolvedType);
//					if (newExactContext == null) {
//						int j = offset - 4;
//						
//						string yieldToken = GetPreviousToken (ref j, true);
//						if (token == "return") {
//							NRefactoryResolver resolver = CreateResolver ();
//							resolver.SetupResolver (new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//							IReturnType returnType = resolver.CallingMember.ReturnType;
//							if (yieldToken == "yield" && returnType.GenericArguments.Count > 0)
//								returnType = returnType.GenericArguments [0];
//							if (resolver.CallingMember != null)
//								return CreateTypeCompletionData (location, callingType, newExactContext, null, returnType);
//						}
//					}
//					return CreateCtrlSpaceCompletionData (completionContext, null);
			case "if":
			case "elif":
				if (wordStart > 0 && document.GetCharAt (wordStart - 1) == '#') 
					return factory.CreatePreProcessorDefinesCompletionData ();
				return null;
			case "yield":
				var yieldDataList = new CompletionDataWrapper (this);
				DefaultCompletionString = "return";
				yieldDataList.AddCustom ("break");
				yieldDataList.AddCustom ("return");
				return yieldDataList.Result;
			case "in":
				var inList = new CompletionDataWrapper (this);
				var node = Unit.GetNodeAt (location);
				var rr = ResolveExpression (node, Unit);
				AddContextCompletion (inList, rr != null ? rr.Item2 : GetState (), node);
				return inList.Result;
//				case "where":
//					CompletionDataList whereDataList = new CompletionDataList ();
//					NRefactoryResolver constraintResolver = CreateResolver ();
//					constraintResolver.SetupResolver (new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//					if (constraintResolver.CallingMember is IMethod) {
//						foreach (ITypeParameter tp in ((IMethod)constraintResolver.CallingMember).TypeParameters) {
//							whereDataList.Add (tp.Name, "md-keyword");
//						}
//					} else {
//						if (constraintResolver.CallingType != null) {
//							foreach (ITypeParameter tp in constraintResolver.CallingType.TypeParameters) {
//								whereDataList.Add (tp.Name, "md-keyword");
//							}
//						}
//					}
//	
//					return whereDataList;
			}
//				if (IsInLinqContext (result)) {
//					if (linqKeywords.Contains (word)) {
//						if (word == "from") // after from no auto code completion.
//							return null;
//						result.Expression = "";
//						return CreateCtrlSpaceCompletionData (completionContext, result);
//					}
//					CompletionDataList dataList = new ProjectDomCompletionDataList ();
//					CompletionDataCollector col = new CompletionDataCollector (this, dom, dataList, Document.CompilationUnit, null, new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//					foreach (string kw in linqKeywords) {
//						col.Add (kw, "md-keyword");
//					}
//					return dataList;
//				}
			return null;
		}
		
		bool IsLineEmptyUpToEol ()
		{
			var line = document.GetLineByNumber (location.Line);
			for (int j = offset; j < line.EndOffset; j++) {
				char ch = document.GetCharAt (j);
				if (!char.IsWhiteSpace (ch))
					return false;
			}
			return true;
		}

		string GetLineIndent (int lineNr)
		{
			var line = document.GetLineByNumber (lineNr);
			for (int j = offset; j < line.EndOffset; j++) {
				char ch = document.GetCharAt (j);
				if (!char.IsWhiteSpace (ch))
					return document.GetText (line.Offset, j - line.Offset - 1);
			}
			return "";
		}
		
		IEnumerable<ICompletionData> CreateTypeCompletionData (IType hintType, AstType hintTypeAst)
		{
			var wrapper = new CompletionDataWrapper (this);
			var state = GetState ();
			Predicate<IType> pred = null;
			if (hintType != null) {
				
				if (hintType.Kind != TypeKind.Unknown) {
					var lookup = new MemberLookup (ctx.CurrentTypeDefinition, Compilation.MainAssembly);
					pred = t => {
						// check if type is in inheritance tree.
						if (hintType.GetDefinition () != null && !t.GetDefinition ().IsDerivedFrom (hintType.GetDefinition ()))
							return false;
						
						// check for valid constructors
						if (t.GetConstructors ().Count () == 0)
							return true;
						bool isProtectedAllowed = currentType != null ? currentType.Resolve (ctx).GetDefinition ().IsDerivedFrom (t.GetDefinition ()) : false;
						return t.GetConstructors ().Any (m => lookup.IsAccessible (m, isProtectedAllowed));
					};
					DefaultCompletionString = GetShortType (hintType, GetState ());
					wrapper.AddType (hintType, DefaultCompletionString);
				} else {
					DefaultCompletionString = hintTypeAst.ToString ();
					wrapper.AddType (hintType, DefaultCompletionString);
				}
			} 
			AddTypesAndNamespaces (wrapper, state, null, pred, m => false);
			AddKeywords (wrapper, primitiveTypesKeywords.Where (k => k != "void"));
			CloseOnSquareBrackets = true;
			AutoCompleteEmptyMatch = true;
			return wrapper.Result;
		}
		
		IEnumerable<ICompletionData> GetOverrideCompletionData (IUnresolvedTypeDefinition type, string modifiers)
		{
			var wrapper = new CompletionDataWrapper (this);
			var alreadyInserted = new Dictionary<string, bool> ();
			bool addedVirtuals = false;
			
			int declarationBegin = offset;
			int j = declarationBegin;
			for (int i = 0; i < 3; i++) {
				switch (GetPreviousToken (ref j, true)) {
				case "public":
				case "protected":
				case "private":
				case "internal":
				case "sealed":
				case "override":
					declarationBegin = j;
					break;
				case "static":
					return null; // don't add override completion for static members
				}
			}
			AddVirtuals (alreadyInserted, wrapper, modifiers, type.Resolve (ctx), declarationBegin);
			return wrapper.Result;
		}
		
		IEnumerable<ICompletionData> GetPartialCompletionData (ITypeDefinition type, string modifiers)
		{
			var wrapper = new CompletionDataWrapper (this);
			int declarationBegin = offset;
			int j = declarationBegin;
			for (int i = 0; i < 3; i++) {
				switch (GetPreviousToken (ref j, true)) {
				case "public":
				case "protected":
				case "private":
				case "internal":
				case "sealed":
				case "override":
					declarationBegin = j;
					break;
				case "static":
					return null; // don't add override completion for static members
				}
			}
			
			var methods = new List<IUnresolvedMethod> ();
			
			foreach (var part in type.Parts) {
				foreach (var method in part.Methods) {
					if (method.BodyRegion.IsEmpty) {
						if (GetImplementation (type, method) != null)
							continue;
						methods.Add (method);
					}
				}	
			}
			
			foreach (var method in methods) {
				wrapper.Add (factory.CreateNewPartialCompletionData (declarationBegin, method.DeclaringTypeDefinition, method));
			} 
			
			return wrapper.Result;
		}
		
		IMethod GetImplementation (ITypeDefinition type, IUnresolvedMethod method)
		{
			foreach (var cur in type.Methods) {
				if (cur.Name == method.Name && cur.Parameters.Count == method.Parameters.Count && !cur.BodyRegion.IsEmpty) {
					bool equal = true;
					/*for (int i = 0; i < cur.Parameters.Count; i++) {
						if (!cur.Parameters [i].Type.Equals (method.Parameters [i].Type)) {
							equal = false;
							break;
						}
					}*/
					if (equal)
						return cur;
				}
			}
			return null;
		}
		
		static string GetNameWithParamCount (IMember member)
		{
			var e = member as IMethod;
			if (e == null || e.TypeParameters.Count == 0)
				return member.Name;
			return e.Name + "`" + e.TypeParameters.Count;
		}
		
		void AddVirtuals (Dictionary<string, bool> alreadyInserted, CompletionDataWrapper col, string modifiers, IType curType, int declarationBegin)
		{
			if (curType == null)
				return;
			foreach (var m in curType.GetMethods (m => !m.IsConstructor && !m.IsDestructor).Cast<IMember> ().Concat (curType.GetProperties ().Cast<IMember> ()).Reverse ()) {
				if (m.IsSynthetic || curType.Kind != TypeKind.Interface && !m.IsOverridable)
					continue;
				// filter out the "Finalize" methods, because finalizers should be done with destructors.
				if (m is IMethod && m.Name == "Finalize")
					continue;
				
				var data = factory.CreateNewOverrideCompletionData (declarationBegin, currentType, m);
				string text = GetNameWithParamCount (m);
				
				// check if the member is already implemented
				bool foundMember = curType.GetMembers ().Any (cm => GetNameWithParamCount (cm) == text && cm.DeclaringTypeDefinition == curType.GetDefinition ());
				if (foundMember)
					continue;
				if (alreadyInserted.ContainsKey (text))
					continue;
				alreadyInserted [text] = true;
				data.CompletionCategory = col.GetCompletionCategory (curType);
				col.Add (data);
			}
		}
		
		static void AddKeywords (CompletionDataWrapper wrapper, IEnumerable<string> keywords)
		{
			foreach (string keyword in keywords) {
				wrapper.AddCustom (keyword);
			}
		}
		
		public string GetPreviousMemberReferenceExpression (int tokenIndex)
		{
			string result = GetPreviousToken (ref tokenIndex, false);
			result = GetPreviousToken (ref tokenIndex, false);
			if (result != ".") {
				result = null;
			} else {
				var names = new List<string> ();
				while (result == ".") {
					result = GetPreviousToken (ref tokenIndex, false);
					if (result == "this") {
						names.Add ("handle");
					} else if (result != null) {
						string trimmedName = result.Trim ();
						if (trimmedName.Length == 0)
							break;
						names.Insert (0, trimmedName);
					}
					result = GetPreviousToken (ref tokenIndex, false);
				}
				result = String.Join ("", names.ToArray ());
				foreach (char ch in result) {
					if (!char.IsLetterOrDigit (ch) && ch != '_') {
						result = "";
						break;
					}
				}
			}
			return result;
		}
		
		bool MatchDelegate (IType delegateType, IUnresolvedMethod method)
		{
			var delegateMethod = delegateType.GetDelegateInvokeMethod ();
			if (delegateMethod == null || delegateMethod.Parameters.Count != method.Parameters.Count)
				return false;
			
			for (int i = 0; i < delegateMethod.Parameters.Count; i++) {
				if (!delegateMethod.Parameters [i].Type.Equals (method.Parameters [i].Type.Resolve (ctx)))
					return false;
			}
			return true;
		}

		string AddDelegateHandlers (CompletionDataWrapper completionList, IType delegateType, bool addSemicolon = true, bool addDefault = true)
		{
			IMethod delegateMethod = delegateType.GetDelegateInvokeMethod ();
			var thisLineIndent = GetLineIndent (location.Line);
			string delegateEndString = EolMarker + thisLineIndent + "}" + (addSemicolon ? ";" : "");
			bool containsDelegateData = completionList.Result.Any (d => d.DisplayText.StartsWith ("delegate("));
			if (addDefault)
				completionList.AddCustom ("delegate", "Creates anonymous delegate.", "delegate {" + EolMarker + thisLineIndent + IndentString + "|" + delegateEndString);
			var sb = new StringBuilder ("(");
			var sbWithoutTypes = new StringBuilder ("(");
			for (int k = 0; k < delegateMethod.Parameters.Count; k++) {
				if (k > 0) {
					sb.Append (", ");
					sbWithoutTypes.Append (", ");
				}
				var parameterType = delegateMethod.Parameters [k].Type;
				sb.Append (GetShortType (parameterType, GetState ()));
				sb.Append (" ");
				sb.Append (delegateMethod.Parameters [k].Name);
				sbWithoutTypes.Append (delegateMethod.Parameters [k].Name);
			}
			sb.Append (")");
			sbWithoutTypes.Append (")");
			completionList.AddCustom ("delegate" + sb, "Creates anonymous delegate.", "delegate" + sb + " {" + EolMarker + thisLineIndent + IndentString + "|" + delegateEndString);
			if (!completionList.Result.Any (data => data.DisplayText == sbWithoutTypes.ToString ()))
				completionList.AddCustom (sbWithoutTypes.ToString (), "Creates lambda expression.", sbWithoutTypes + " => |" + (addSemicolon ? ";" : ""));
			/* TODO:Make factory method out of it.
			// It's  needed to temporarly disable inserting auto matching bracket because the anonymous delegates are selectable with '('
			// otherwise we would end up with () => )
			if (!containsDelegateData) {
				var savedValue = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket;
				MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = false;
				completionList.Result.CompletionListClosed += delegate {
					MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = savedValue;
				};
			}*/
			return sb.ToString ();
		}
		
		bool IsAccessibleFrom (IEntity member, ITypeDefinition calledType, IMember currentMember, bool includeProtected)
		{
			if (currentMember == null)
				return member.IsStatic || member.IsPublic;
//			if (currentMember is MonoDevelop.Projects.Dom.BaseResolveResult.BaseMemberDecorator) 
//				return member.IsPublic | member.IsProtected;
			//		if (member.IsStatic && !IsStatic)
			//			return false;
			if (member.IsPublic || calledType != null && calledType.Kind == TypeKind.Interface && !member.IsProtected)
				return true;
			if (member.DeclaringTypeDefinition != null) {
				if (member.DeclaringTypeDefinition.Kind == TypeKind.Interface) 
					return IsAccessibleFrom (member.DeclaringTypeDefinition, calledType, currentMember, includeProtected);
			
				if (member.IsProtected && !(member.DeclaringTypeDefinition.IsProtectedOrInternal && !includeProtected))
					return includeProtected;
			}
			if (member.IsInternal || member.IsProtectedAndInternal || member.IsProtectedOrInternal) {
				var type1 = member is ITypeDefinition ? (ITypeDefinition)member : member.DeclaringTypeDefinition;
				var type2 = currentMember is ITypeDefinition ? (ITypeDefinition)currentMember : currentMember.DeclaringTypeDefinition;
				bool result = true;
				// easy case, projects are the same
/*//				if (type1.ProjectContent == type2.ProjectContent) {
//					result = true; 
//				} else 
				if (type1.ProjectContent != null) {
					// maybe type2 hasn't project dom set (may occur in some cases), check if the file is in the project
					//TODO !!
//					result = type1.ProjectContent.Annotation<MonoDevelop.Projects.Project> ().GetProjectFile (type2.Region.FileName) != null;
					result = false;
				} else if (type2.ProjectContent != null) {
					//TODO!!
//					result = type2.ProjectContent.Annotation<MonoDevelop.Projects.Project> ().GetProjectFile (type1.Region.FileName) != null;
					result = false;
				} else {
					// should never happen !
					result = true;
				}*/
				return member.IsProtectedAndInternal ? includeProtected && result : result;
			}
			
			if (!(currentMember is IType) && (currentMember.DeclaringTypeDefinition == null || member.DeclaringTypeDefinition == null))
				return false;
			
			// inner class 
			var declaringType = currentMember.DeclaringTypeDefinition;
			while (declaringType != null) {
				if (declaringType.ReflectionName == currentMember.DeclaringType.ReflectionName)
					return true;
				declaringType = declaringType.DeclaringTypeDefinition;
			}
			
			
			return currentMember.DeclaringTypeDefinition != null && member.DeclaringTypeDefinition.FullName == currentMember.DeclaringTypeDefinition.FullName;
		}
		
		IEnumerable<ICompletionData> CreateTypeAndNamespaceCompletionData (TextLocation location, ResolveResult resolveResult, AstNode resolvedNode, CSharpResolver state)
		{
			if (resolveResult == null || resolveResult.IsError)
				return null;
			var result = new CompletionDataWrapper (this);
			if (resolveResult is NamespaceResolveResult) {
				var nr = (NamespaceResolveResult)resolveResult;
				foreach (var cl in nr.Namespace.Types) {
					result.AddType (cl, cl.Name);
				}
				foreach (var ns in nr.Namespace.ChildNamespaces) {
					result.AddNamespace (ns.Name);
				}
			} else if (resolveResult is TypeResolveResult) {
				var type = resolveResult.Type;
				foreach (var nested in type.GetNestedTypes ()) {
					result.AddType (nested, nested.Name);
				}
			}
			return result.Result;
		}
		
		IEnumerable<ICompletionData> CreateTypeList ()
		{
			foreach (var cl in Compilation.RootNamespace.Types) {
				yield return factory.CreateTypeCompletionData (cl, cl.Name);
			}
			
			foreach (var ns in Compilation.RootNamespace.ChildNamespaces) {
				yield return factory.CreateNamespaceCompletionData (ns.Name);
			}
		}
		
		IEnumerable<ICompletionData> CreateParameterCompletion (MethodGroupResolveResult resolveResult, CSharpResolver state, AstNode invocation, int parameter, bool controlSpace)
		{
			var result = new CompletionDataWrapper (this);
			var addedEnums = new HashSet<string> ();
			var addedDelegates = new HashSet<string> ();
			
			foreach (var method in resolveResult.Methods) {
				if (method.Parameters.Count <= parameter)
					continue;
				var resolvedType = method.Parameters [parameter].Type;
				if (resolvedType.Kind == TypeKind.Enum) {
					if (addedEnums.Contains (resolvedType.ReflectionName))
						continue;
					addedEnums.Add (resolvedType.ReflectionName);
					AddEnumMembers (result, resolvedType, state);
				} else if (resolvedType.Kind == TypeKind.Delegate) {
//					if (addedDelegates.Contains (resolvedType.DecoratedFullName))
//						continue;
//					addedDelegates.Add (resolvedType.DecoratedFullName);
//					string parameterDefinition = AddDelegateHandlers (completionList, resolvedType, false, addedDelegates.Count == 1);
//					string varName = "Handle" + method.Parameters [parameter].ReturnType.Name + method.Parameters [parameter].Name;
//					result.Add (new EventCreationCompletionData (document, varName, resolvedType, null, parameterDefinition, resolver.Unit.GetMemberAt (location), resolvedType) { AddSemicolon = false });
				
				}
			}
			if (!controlSpace) {
				if (addedEnums.Count + addedDelegates.Count == 0)
					return Enumerable.Empty<ICompletionData> ();
				AutoCompleteEmptyMatch = false;
				AutoSelect = false;
			}
			AddContextCompletion (result, state, invocation);
			
//			resolver.AddAccessibleCodeCompletionData (ExpressionContext.MethodBody, cdc);
//			if (addedDelegates.Count > 0) {
//				foreach (var data in result.Result) {
//					if (data is MemberCompletionData) 
//						((MemberCompletionData)data).IsDelegateExpected = true;
//				}
//			}
			return result.Result;
		}
		
		string GetShortType (IType type, CSharpResolver state)
		{
			var builder = new TypeSystemAstBuilder (state);
			var dt = state.CurrentTypeDefinition;
			var declaring = type.DeclaringType != null ? type.DeclaringType.GetDefinition () : null;
			if (declaring != null) {
				while (dt != null) {
					if (dt.Equals (declaring)) {
						builder.AlwaysUseShortTypeNames = true;
						break;
					}
					dt = dt.DeclaringTypeDefinition;
				}
			}
			var shortType = builder.ConvertType (type);
			using (var w = new System.IO.StringWriter ()) {
				var visitor = new CSharpOutputVisitor (w, FormattingPolicy);
				shortType.AcceptVisitor (visitor, null);
				return w.ToString ();
			}
		}
		
		void AddEnumMembers (CompletionDataWrapper completionList, IType resolvedType, CSharpResolver state)
		{
			if (resolvedType.Kind != TypeKind.Enum)
				return;
			string typeString = GetShortType (resolvedType, state);
			if (typeString.Contains ("."))
				completionList.AddType (resolvedType, typeString);
			foreach (var field in resolvedType.GetFields ()) {
				if (field.IsConst || field.IsStatic)
					completionList.Result.Add (factory.CreateEntityCompletionData (field, typeString + "." + field.Name));
			}
			DefaultCompletionString = typeString;
		}
		
		IEnumerable<ICompletionData> CreateCompletionData (TextLocation location, ResolveResult resolveResult, AstNode resolvedNode, CSharpResolver state)
		{
			if (resolveResult == null /*|| resolveResult.IsError*/)
				return null;
			
			if (resolveResult is NamespaceResolveResult) {
				var nr = (NamespaceResolveResult)resolveResult;
				var namespaceContents = new CompletionDataWrapper (this);
				
				foreach (var cl in nr.Namespace.Types) {
					namespaceContents.AddType (cl, cl.Name);
				}
				
				foreach (var ns in nr.Namespace.ChildNamespaces) {
					namespaceContents.AddNamespace (ns.Name);
				}
				return namespaceContents.Result;
			}
			
			IType type = resolveResult.Type;
			var typeDef = resolveResult.Type.GetDefinition ();
			var result = new CompletionDataWrapper (this);
			bool includeStaticMembers = false;
			
			if (resolveResult is LocalResolveResult) {
				if (resolvedNode is IdentifierExpression) {
					var mrr = (LocalResolveResult)resolveResult;
					includeStaticMembers = mrr.Variable.Name == mrr.Type.Name;
				}
			}
			if (resolveResult is TypeResolveResult && type.Kind == TypeKind.Enum) {
				foreach (var field in type.GetFields ()) {
					result.AddMember (field);
				}
				foreach (var m in type.GetMethods ()) {
					if (m.Name == "TryParse")
						result.AddMember (m);
				}
				return result.Result;
			}
			
			var lookup = new MemberLookup (ctx.CurrentTypeDefinition, Compilation.MainAssembly);
			bool isProtectedAllowed = resolveResult is ThisResolveResult ? true : lookup.IsProtectedAccessAllowed (type);
			bool skipNonStaticMembers = (resolveResult is TypeResolveResult);
			
			if (resolveResult is MemberResolveResult && resolvedNode is IdentifierExpression) {
				var mrr = (MemberResolveResult)resolveResult;
				includeStaticMembers = mrr.Member.Name == mrr.Type.Name;
				
				TypeResolveResult trr;
				if (state.IsVariableReferenceWithSameType (resolveResult, ((IdentifierExpression)resolvedNode).Identifier, out trr)) {
					if (currentMember != null && mrr.Member.IsStatic ^ currentMember.IsStatic) {
						skipNonStaticMembers = true;
						
						if (trr.Type.Kind == TypeKind.Enum) {
							foreach (var field in trr.Type.GetFields ()) {
								result.AddMember (field);
							}
							foreach (var m in trr.Type.GetMethods ()) {
								if (m.Name == "TryParse" && m.IsStatic) {
									result.AddMember (m);
								}
							}
							return result.Result;
						}
					}
				}
				// ADD Aliases
				var scope = CSharpParsedFile.GetUsingScope (location).Resolve (Compilation);
			
				for (var n = scope; n != null; n = n.Parent) {
					foreach (var pair in n.UsingAliases) {
						if (pair.Key == mrr.Member.Name) {
							foreach (var r in CreateCompletionData (location, pair.Value, resolvedNode, state)) {
								if (r is IEntityCompletionData && ((IEntityCompletionData)r).Entity is IMember) {
									result.AddMember ((IMember)((IEntityCompletionData)r).Entity);
								} else {
									result.Add (r);
								}
							}
						}
					}
				}				
				
				
			}
			if (resolveResult is TypeResolveResult && (resolvedNode is IdentifierExpression || resolvedNode is MemberReferenceExpression)) {
				includeStaticMembers = true;
			}
			
//			Console.WriteLine ("type:" + type +"/"+type.GetType ());
//			Console.WriteLine ("current:" + ctx.CurrentTypeDefinition);
//			Console.WriteLine ("IS PROT ALLOWED:" + isProtectedAllowed + " static: "+ includeStaticMembers);
//			Console.WriteLine (resolveResult);
//			Console.WriteLine ("node:" + resolvedNode);
//			Console.WriteLine (currentMember !=  null ? currentMember.IsStatic : "currentMember == null");
			
			if (resolvedNode.Annotation<ObjectCreateExpression> () == null) { //tags the created expression as part of an object create expression.
				
				var filteredList = new List<IMember> ();
				foreach (var member in type.GetMembers ()) {
//					Console.WriteLine ("member:" + member + member.IsShadowing);
					if (!lookup.IsAccessible (member, isProtectedAllowed)) {
//						Console.WriteLine ("skip access: " + member.FullName);
						continue;
					}
					if (resolvedNode is BaseReferenceExpression && member.IsAbstract)
						continue;
					bool memberIsStatic = member.IsStatic;
					if (!includeStaticMembers && memberIsStatic && !(resolveResult is TypeResolveResult)) {
//						Console.WriteLine ("skip static member: " + member.FullName);
						continue;
					}
					var field = member as IField;
					if (field != null)
						memberIsStatic |= field.IsConst;
					
					if (!memberIsStatic && skipNonStaticMembers) {
						continue;
					}
					
					if (member is IMethod && ((IMethod)member).FullName == "System.Object.Finalize")
						continue;
					if (member.EntityType == EntityType.Operator)
						continue;
					if (member.IsShadowing)
						filteredList.RemoveAll (m => m.Name == member.Name);
					
					filteredList.Add (member);
				}
				
				foreach (var member in filteredList) {
//					Console.WriteLine ("add:" + member + "/" + member.IsStatic);
					result.AddMember (member);
				}
			}
			
			if (resolveResult is TypeResolveResult || includeStaticMembers) {
				foreach (var nested in type.GetNestedTypes ()) {
					result.AddType (nested, nested.Name);
				}
				
			} else {
				foreach (var meths in state.GetExtensionMethods (type)) {
					foreach (var m in meths) {
						result.AddMember (m);
					}
				}
			}
			
//			IEnumerable<object> objects = resolveResult.CreateResolveResult (dom, resolver != null ? resolver.CallingMember : null);
//			CompletionDataCollector col = new CompletionDataCollector (this, dom, result, Document.CompilationUnit, resolver != null ? resolver.CallingType : null, location);
//			col.HideExtensionParameter = !resolveResult.StaticResolve;
//			col.NamePrefix = expressionResult.Expression;
//			bool showOnlyTypes = expressionResult.Contexts.Any (ctx => ctx == ExpressionContext.InheritableType || ctx == ExpressionContext.Constraints);
//			if (objects != null) {
//				foreach (object obj in objects) {
//					if (expressionResult.ExpressionContext != null && expressionResult.ExpressionContext.FilterEntry (obj))
//						continue;
//					if (expressionResult.ExpressionContext == ExpressionContext.NamespaceNameExcepted && !(obj is Namespace))
//						continue;
//					if (showOnlyTypes && !(obj is IType))
//						continue;
//					CompletionData data = col.Add (obj);
//					if (data != null && expressionResult.ExpressionContext == ExpressionContext.Attribute && data.CompletionText != null && data.CompletionText.EndsWith ("Attribute")) {
//						string newText = data.CompletionText.Substring (0, data.CompletionText.Length - "Attribute".Length);
//						data.SetText (newText);
//					}
//				}
//			}
			
			return result.Result;
		}

		IEnumerable<ICompletionData> CreateCaseCompletionData (TextLocation location)
		{
			var unit = ParseStub ("a: break;");
			if (unit == null)
				return null;
			var s = unit.GetNodeAt<SwitchStatement> (location);
			if (s == null)
				return null;
			
			var offset = document.GetOffset (s.Expression.StartLocation);
			var expr = GetExpressionAt (offset);
			if (expr == null)
				return null;
			
			var resolveResult = ResolveExpression (expr);
			if (resolveResult == null || resolveResult.Item1.Type.Kind != TypeKind.Enum) 
				return null;
			var wrapper = new CompletionDataWrapper (this);
			AddEnumMembers (wrapper, resolveResult.Item1.Type, resolveResult.Item2);
			AutoCompleteEmptyMatch = false;
			return wrapper.Result;
		}
		
		#region Parsing methods
		ExpressionResult GetExpressionBeforeCursor ()
		{
			CompilationUnit baseUnit;
			if (currentMember == null) {
				baseUnit = ParseStub ("st {}", false);
				var type = baseUnit.GetNodeAt<MemberType> (location);
				if (type == null) {
					baseUnit = ParseStub ("a;", false);
					type = baseUnit.GetNodeAt<MemberType> (location);
				}
				
				if (type == null) {
					baseUnit = ParseStub ("A a;", false);
					type = baseUnit.GetNodeAt<MemberType> (location);
				}
				if (type != null) {
					if (currentType == null) {
						return new ExpressionResult ((AstNode)type.Target, baseUnit);
					}
					
					var target = type.Target;
					target.Remove ();
					var node = Unit.GetNodeAt (location) ?? Unit;
					node.AddChild (target, AstNode.Roles.Type);
					return new ExpressionResult ((AstNode)target, Unit);
				}
			}
			
			baseUnit = ParseStub ("a()", false);
			var curNode = baseUnit.GetNodeAt (location);
			
			// hack for local variable declaration missing ';' issue - remove that if it works.
			if (curNode is AttributedNode || baseUnit.GetNodeAt<Expression> (location) == null) {
				baseUnit = ParseStub ("a()");
				curNode = baseUnit.GetNodeAt (location);
			}
			
			// Hack for handle object initializer continuation expressions
			if (curNode is AttributedNode || baseUnit.GetNodeAt<Expression> (location) == null) {
				baseUnit = ParseStub ("a()};");
			}
			var mref = baseUnit.GetNodeAt<MemberReferenceExpression> (location); 
			if (currentMember == null && currentType == null) {
				if (mref != null)
					return new ExpressionResult ((AstNode)mref.Target, baseUnit);
				return null;
			}
			var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			if (mref == null) {
				var invoke = baseUnit.GetNodeAt<InvocationExpression> (location); 
				if (invoke != null)
					mref = invoke.Target as MemberReferenceExpression;
			}
			Expression expr = null;
			if (mref != null) {
				expr = mref.Target.Clone ();
				mref.Parent.ReplaceWith (expr);
			} else {
				Expression tref = baseUnit.GetNodeAt<TypeReferenceExpression> (location); 
				MemberType memberType = tref != null ? ((TypeReferenceExpression)tref).Type as MemberType : null;
				if (memberType == null) {
					memberType = baseUnit.GetNodeAt<MemberType> (location); 
					if (memberType != null) {
						tref = baseUnit.GetNodeAt<Expression> (location); 
						if (tref == null) {
							tref = new TypeReferenceExpression (memberType.Clone ());
							memberType.Parent.AddChild (tref, AstNode.Roles.Expression);
						}
					}
					if (tref is ObjectCreateExpression) {
						expr = new TypeReferenceExpression (memberType.Target.Clone ());
						expr.AddAnnotation (new ObjectCreateExpression ());
					}
				}
					
				if (memberType == null)
					return null;
				if (expr == null)
					expr = new TypeReferenceExpression (memberType.Target.Clone ());
				tref.ReplaceWith (expr);
			}
			
			var member = Unit.GetNodeAt<AttributedNode> (memberLocation);
			var member2 = baseUnit.GetNodeAt<AttributedNode> (memberLocation);
			member2.Remove ();
			member.ReplaceWith (member2);
			return new ExpressionResult ((AstNode)expr, Unit);
		}
		
		ExpressionResult GetExpressionAtCursor ()
		{
//			if (currentMember == null && currentType == null)
//				return null;
			
			TextLocation memberLocation;
			if (currentMember != null) {
				memberLocation = currentMember.Region.Begin;
			} else if (currentType != null) {
				memberLocation = currentType.Region.Begin;
			} else {
				memberLocation = location;
			}
			var baseUnit = ParseStub ("a");
			var tmpUnit = baseUnit;
			AstNode expr = baseUnit.GetNodeAt (location, n => n is IdentifierExpression || n is MemberReferenceExpression);
			if (expr == null) {
				expr = baseUnit.GetNodeAt<AstType> (location.Line, location.Column - 1);
				if (expr is AstType && expr.Parent is Attribute)
					expr = expr.Parent;
			}
			
			// try insertStatement
			if (expr == null && baseUnit.GetNodeAt<EmptyStatement> (location.Line, location.Column) != null) {
				tmpUnit = baseUnit = ParseStub ("a();", false);
				expr = baseUnit.GetNodeAt<InvocationExpression> (location.Line, location.Column + 1); 
			}
			
			if (expr == null) {
				baseUnit = ParseStub ("()");
				expr = baseUnit.GetNodeAt<IdentifierExpression> (location.Line, location.Column - 1); 
			}
			
			// try initializer expression
			if (expr == null) {
				baseUnit = ParseStub ("a = b};", false);
				expr = baseUnit.GetNodeAt<ArrayInitializerExpression> (location.Line, location.Column - 1); 
			}
			
			// try statement 
			if (expr == null) {
				expr = tmpUnit.GetNodeAt<SwitchStatement> (location.Line, location.Column - 1); 
				baseUnit = tmpUnit;
			}
			
			if (expr == null) {
				var block = tmpUnit.GetNodeAt<BlockStatement> (location); 
				var node = block != null ? block.Statements.LastOrDefault () : null;
				
				var forStmt = node != null ? node.PrevSibling as ForStatement : null;
				if (forStmt != null && forStmt.EmbeddedStatement.IsNull) {
					expr = forStmt;
					var id = new IdentifierExpression ("stub");
					forStmt.EmbeddedStatement = new BlockStatement () { Statements = { new ExpressionStatement (id) }};
					expr = id;
					baseUnit = tmpUnit;
				}
			}
			
			if (expr == null) {
				var forStmt = tmpUnit.GetNodeAt<ForeachStatement> (location.Line, location.Column - 3); 
				if (forStmt != null && forStmt.EmbeddedStatement.IsNull) {
					forStmt.VariableNameToken = Identifier.Create ("stub");
					expr = forStmt.VariableNameToken;
					baseUnit = tmpUnit;
				}
			}
			if (expr == null) {
				expr = tmpUnit.GetNodeAt<VariableInitializer> (location.Line, location.Column - 1);
				baseUnit = tmpUnit;
			}
			
			// try parameter declaration type
			if (expr == null) {
				baseUnit = ParseStub (">", false, "{}");
				expr = baseUnit.GetNodeAt<TypeParameterDeclaration> (location.Line, location.Column - 1); 
			}
			
			// try parameter declaration method
			if (expr == null) {
				baseUnit = ParseStub ("> ()", false, "{}");
				expr = baseUnit.GetNodeAt<TypeParameterDeclaration> (location.Line, location.Column - 1); 
			}
			
			// try expression in anonymous type "new { sample = x$" case
			if (expr == null) {
				baseUnit = ParseStub ("a", false);
				expr = baseUnit.GetNodeAt<AnonymousTypeCreateExpression> (location.Line, location.Column); 
				if (expr != null)
					expr = baseUnit.GetNodeAt<Expression> (location.Line, location.Column) ?? expr; 
				if (expr == null)
					expr = baseUnit.GetNodeAt<AstType> (location.Line, location.Column); 
				if (expr is AstType && expr.Parent is Attribute)
					expr = expr.Parent;
			}
			if (expr == null)
				return null;
			var member = Unit.GetNodeAt<AttributedNode> (memberLocation);
			var member2 = baseUnit.GetNodeAt<AttributedNode> (memberLocation);
			if (member != null && member2 != null) {
				member2.Remove ();
				
				if (member is TypeDeclaration) {
					member.AddChild (member2, TypeDeclaration.MemberRole);
				} else {
					member.ReplaceWith (member2);
				}
			} else {
				return new ExpressionResult (expr, baseUnit);
			}
			return new ExpressionResult (expr, Unit);
		}
		
		ExpressionResult GetExpressionAt (int offset)
		{
			var parser = new CSharpParser ();
			string text = this.document.GetText (0, this.offset); 
			var sb = new StringBuilder (text);
			sb.Append ("a;");
			AppendMissingClosingBrackets (sb, text, false);
			var stream = new System.IO.StringReader (sb.ToString ());
			var completionUnit = parser.Parse (stream, CSharpParsedFile.FileName, 0);
			stream.Close ();
			var loc = document.GetLocation (offset);
			
			var expr = completionUnit.GetNodeAt (loc, n => n is Expression || n is VariableDeclarationStatement);
			if (expr == null)
				return null;
			return new ExpressionResult (expr, completionUnit);
		}
		
		ExpressionResult GetNewExpressionAt (int offset)
		{
			var parser = new CSharpParser ();
			string text = this.document.GetText (0, this.offset); 
			var sb = new StringBuilder (text);
			sb.Append ("a ();");
			AppendMissingClosingBrackets (sb, text, false);
			
			var stream = new System.IO.StringReader (sb.ToString ());
			var completionUnit = parser.Parse (stream, CSharpParsedFile.FileName, 0);
			stream.Close ();
			var loc = document.GetLocation (offset);
			
			var expr = completionUnit.GetNodeAt (loc, n => n is Expression);
			if (expr == null) {
				// try without ";"
				sb = new StringBuilder (text);
				sb.Append ("a ()");
				AppendMissingClosingBrackets (sb, text, false);
				stream = new System.IO.StringReader (sb.ToString ());
				completionUnit = parser.Parse (stream, CSharpParsedFile.FileName, 0);
				stream.Close ();
				loc = document.GetLocation (offset);
				
				expr = completionUnit.GetNodeAt (loc, n => n is Expression);
				if (expr == null)
					return null;
			}
			return new ExpressionResult (expr, completionUnit);
		}
		
		
		#endregion
		
		#region Helper methods
		string GetPreviousToken (ref int i, bool allowLineChange)
		{
			char c;
			if (i <= 0)
				return null;
			
			do {
				c = document.GetCharAt (--i);
			} while (i > 0 && char.IsWhiteSpace (c) && (allowLineChange ? true : c != '\n'));
			
			if (i == 0)
				return null;
			
			if (!char.IsLetterOrDigit (c))
				return new string (c, 1);
			
			int endOffset = i + 1;
			
			do {
				c = document.GetCharAt (i - 1);
				if (!(char.IsLetterOrDigit (c) || c == '_'))
					break;
				
				i--;
			} while (i > 0);
			
			return document.GetText (i, endOffset - i);
		}
		
		bool GetParameterCompletionCommandOffset (out int cpos)
		{
			// Start calculating the parameter offset from the beginning of the
			// current member, instead of the beginning of the file. 
			cpos = offset - 1;
			var mem = currentMember;
			if (mem == null || (mem is IType))
				return false;
			int startPos = document.GetOffset (mem.Region.BeginLine, mem.Region.BeginColumn);
			int parenDepth = 0;
			int chevronDepth = 0;
			while (cpos > startPos) {
				char c = document.GetCharAt (cpos);
				if (c == ')')
					parenDepth++;
				if (c == '>')
					chevronDepth++;
				if (parenDepth == 0 && c == '(' || chevronDepth == 0 && c == '<') {
					int p = GetCurrentParameterIndex (cpos + 1, startPos);
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
		}
		
		int GetCurrentParameterIndex (int offset, int memberStart)
		{
			int cursor = this.offset;
			int i = offset;
			
			if (i > cursor)
				return -1;
			if (i == cursor) 
				return 1; // parameters are 1 based
			int index = memberStart + 1;
			int parentheses = 0;
			int bracket = 0;
			bool insideQuote = false, insideString = false, insideSingleLineComment = false, insideMultiLineComment = false;
			do {
				char c = document.GetCharAt (i - 1);
				switch (c) {
				case '\\':
					if (insideString || insideQuote)
						i++;
					break;
				case '\'':
					if (!insideString && !insideSingleLineComment && !insideMultiLineComment)
						insideQuote = !insideQuote;
					break;
				case '"':
					if (!insideQuote && !insideSingleLineComment && !insideMultiLineComment)
						insideString = !insideString;
					break;
				case '/':
					if (!insideQuote && !insideString && !insideMultiLineComment) {
						if (document.GetCharAt (i) == '/')
							insideSingleLineComment = true;
						if (document.GetCharAt (i) == '*')
							insideMultiLineComment = true;
					}
					break;
				case '*':
					if (insideMultiLineComment && document.GetCharAt (i) == '/')
						insideMultiLineComment = false;
					break;
				case '\n':
				case '\r':
					insideSingleLineComment = false;
					break;
				case '{':
					if (!insideQuote && !insideString && !insideSingleLineComment && !insideMultiLineComment)
						bracket++;
					break;
				case '}':
					if (!insideQuote && !insideString && !insideSingleLineComment && !insideMultiLineComment)
						bracket--;
					break;
				case '(':
					if (!insideQuote && !insideString && !insideSingleLineComment && !insideMultiLineComment)
						parentheses++;
					break;
				case ')':
					if (!insideQuote && !insideString && !insideSingleLineComment && !insideMultiLineComment)
						parentheses--;
					break;
				case ',':
					if (!insideQuote && !insideString && !insideSingleLineComment && !insideMultiLineComment && parentheses == 1 && bracket == 0)
						index++;
					break;
				}
				i++;
			} while (i <= cursor && parentheses >= 0);
			
			return parentheses != 1 || bracket > 0 ? -1 : index;
		}
		
		#endregion
		
		#region Preprocessor
		
		IEnumerable<ICompletionData> GetDirectiveCompletionData ()
		{
			yield return factory.CreateLiteralCompletionData ("if");
			yield return factory.CreateLiteralCompletionData ("else");
			yield return factory.CreateLiteralCompletionData ("elif");
			yield return factory.CreateLiteralCompletionData ("endif");
			yield return factory.CreateLiteralCompletionData ("define");
			yield return factory.CreateLiteralCompletionData ("undef");
			yield return factory.CreateLiteralCompletionData ("warning");
			yield return factory.CreateLiteralCompletionData ("error");
			yield return factory.CreateLiteralCompletionData ("pragma");
			yield return factory.CreateLiteralCompletionData ("line");
			yield return factory.CreateLiteralCompletionData ("line hidden");
			yield return factory.CreateLiteralCompletionData ("line default");
			yield return factory.CreateLiteralCompletionData ("region");
			yield return factory.CreateLiteralCompletionData ("endregion");
		}
		#endregion
		
		#region Xml Comments
		static readonly List<string> commentTags = new List<string> (new string[] { "c", "code", "example", "exception", "include", "list", "listheader", "item", "term", "description", "para", "param", "paramref", "permission", "remarks", "returns", "see", "seealso", "summary", "value" });
		
		IEnumerable<ICompletionData> GetXmlDocumentationCompletionData ()
		{
			yield return factory.CreateLiteralCompletionData ("c", "Set text in a code-like font");
			yield return factory.CreateLiteralCompletionData ("code", "Set one or more lines of source code or program output");
			yield return factory.CreateLiteralCompletionData ("example", "Indicate an example");
			yield return factory.CreateLiteralCompletionData ("exception", "Identifies the exceptions a method can throw", "exception cref=\"|\"></exception>");
			yield return factory.CreateLiteralCompletionData ("include", "Includes comments from a external file", "include file=\"|\" path=\"\">");
			yield return factory.CreateLiteralCompletionData ("list", "Create a list or table", "list type=\"|\">");
			yield return factory.CreateLiteralCompletionData ("listheader", "Define the heading row");
			yield return factory.CreateLiteralCompletionData ("item", "Defines list or table item");
			
			yield return factory.CreateLiteralCompletionData ("term", "A term to define");
			yield return factory.CreateLiteralCompletionData ("description", "Describes a list item");
			yield return factory.CreateLiteralCompletionData ("para", "Permit structure to be added to text");
			
			yield return factory.CreateLiteralCompletionData ("param", "Describe a parameter for a method or constructor", "param name=\"|\">");
			yield return factory.CreateLiteralCompletionData ("paramref", "Identify that a word is a parameter name", "paramref name=\"|\"/>");
			
			yield return factory.CreateLiteralCompletionData ("permission", "Document the security accessibility of a member", "permission cref=\"|\"");
			yield return factory.CreateLiteralCompletionData ("remarks", "Describe a type");
			yield return factory.CreateLiteralCompletionData ("returns", "Describe the return value of a method");
			yield return factory.CreateLiteralCompletionData ("see", "Specify a link", "see cref=\"|\"/>");
			yield return factory.CreateLiteralCompletionData ("seealso", "Generate a See Also entry", "seealso cref=\"|\"/>");
			yield return factory.CreateLiteralCompletionData ("summary", "Describe a member of a type");
			yield return factory.CreateLiteralCompletionData ("typeparam", "Describe a type parameter for a generic type or method");
			yield return factory.CreateLiteralCompletionData ("typeparamref", "Identify that a word is a type parameter name");
			yield return factory.CreateLiteralCompletionData ("value", "Describe a property");
		}
		#endregion
		
		#region Keywords
		static string[] expressionLevelKeywords = new string [] { "as", "is", "else", "out", "ref", "null", "delegate", "default"};
		static string[] primitiveTypesKeywords = new string [] { "void", "object", "bool", "byte", "sbyte", "char", "short", "int", "long", "ushort", "uint", "ulong", "float", "double", "decimal", "string"};
		static string[] statementStartKeywords = new string [] { "base", "new", "sizeof", "this", 
			"true", "false", "typeof", "checked", "unchecked", "from", "break", "checked",
			"unchecked", "const", "continue", "do", "finally", "fixed", "for", "foreach",
			"goto", "if", "lock", "return", "stackalloc", "switch", "throw", "try", "unsafe", 
			"using", "while", "yield", "dynamic", "var", "dynamic"
		};
		static string[] globalLevelKeywords = new string [] {
			"namespace", "using", "extern", "public", "internal", 
			"class", "interface", "struct", "enum", "delegate",
			"abstract", "sealed", "static", "unsafe", "partial"
		};
		static string[] accessorModifierKeywords = new string [] {
			"public", "internal", "protected", "private"
		};
		static string[] typeLevelKeywords = new string [] {
			"public", "internal", "protected", "private",
			"class", "interface", "struct", "enum", "delegate",
			"abstract", "sealed", "static", "unsafe", "partial",
			"const", "event", "extern", "fixed","new", 
			"operator", "explicit", "implicit", 
			"override", "readonly", "virtual", "volatile"
		};
		static string[] linqKeywords = new string[] { "from", "where", "select", "group", "into", "orderby", "join", "let", "in", "on", "equals", "by", "ascending", "descending" };
		#endregion
	}
}

