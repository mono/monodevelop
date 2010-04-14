//
// NRefactoryResolver.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Completion;

namespace MonoDevelop.CSharp.Resolver
{
	public class NRefactoryResolver : IResolver
	{
		ProjectDom dom;
		SupportedLanguage lang;
		MonoDevelop.Ide.Gui.TextEditor editor;
		IType   callingType;
		IMember callingMember;
		ICompilationUnit unit;
		LookupTableVisitor lookupTableVisitor;
		DomLocation resolvePosition;
		string fileName;
		
		public IType CallingType {
			get {
				return callingType;
			}
		}
		
		public IMember CallingMember {
			get {
				return callingMember;
			}
			set {
				callingMember = value;
			}
		}

		public ProjectDom Dom {
			get {
				return dom;
			}
		}
		
		public ICompilationUnit Unit {
			get {
				return unit;
			}
		}

		public static IType GetTypeAtCursor (IType outerType, string fileName, DomLocation position)
		{
			foreach (IType type in outerType.InnerTypes) {
				if (type.BodyRegion.Contains (position))
					return GetTypeAtCursor (type, fileName, position);
			}
			return outerType;
		}
		
		public static IType GetTypeAtCursor (ICompilationUnit unit, string fileName, DomLocation position)
		{
			if (unit == null)
				return null;
			foreach (IType type in unit.Types) {
				if (type.BodyRegion.Contains (position))
					return GetTypeAtCursor (type, fileName, position);
			}
			return null;
		}
		
		public NRefactoryResolver (ProjectDom dom, ICompilationUnit unit, MonoDevelop.Ide.Gui.TextEditor editor, string fileName) : this (dom, unit, SupportedLanguage.CSharp, editor, fileName)
		{
		}
			
		public NRefactoryResolver (ProjectDom dom, ICompilationUnit unit, SupportedLanguage lang, MonoDevelop.Ide.Gui.TextEditor editor, string fileName)
		{
			if (dom == null)
				throw new ArgumentNullException ("dom");
			this.unit = unit;
			
			this.dom = dom;
			this.lang   = lang;
			this.editor = editor;
			this.fileName = fileName;
			this.lookupTableVisitor = new LookupTableVisitor (lang);
			
		}
		
		public IType SearchType (string fullyDecoratedName)
		{
			return dom.SearchType (CallingMember ?? (MonoDevelop.Projects.Dom.INode)CallingType ?? Unit, fullyDecoratedName);
		}
		
		public IType SearchType (IReturnType type)
		{
			return dom.SearchType (CallingMember ?? (MonoDevelop.Projects.Dom.INode)CallingType ?? Unit, type);
		}
		
		ICSharpCode.NRefactory.Ast.CompilationUnit memberCompilationUnit;
		public ICSharpCode.NRefactory.Ast.CompilationUnit MemberCompilationUnit {
			get {
				return this.memberCompilationUnit;
			}
		}
		
		internal static IMember GetMemberAt (IType type, string fileName, DomLocation location)
		{
			foreach (IMember member in type.Members) {
				if (member.DeclaringType.CompilationUnit.FileName != fileName)
					continue;
				if (!(member is IMethod || member is IProperty || member is IEvent || member is IField))
					continue;
				if (member.Location.Line == location.Line || (!member.BodyRegion.IsEmpty && member.BodyRegion.Start <= location && (location < member.BodyRegion.End || member.BodyRegion.End.IsEmpty))) {
					return member;
				}
			}
			return null;
		}
		
		internal void SetupResolver (DomLocation resolvePosition)
		{
			this.resolvePosition = resolvePosition;
			this.resultTable.Clear ();
			callingType = GetTypeAtCursor (unit, fileName, resolvePosition);
			if (callingType != null) {
				callingType = dom.ResolveType (callingType);
				callingMember = GetMemberAt (callingType, fileName, resolvePosition);
				if (callingMember == null) {
					DomLocation posAbove = resolvePosition;
					posAbove.Line--;
					callingMember = GetMemberAt (callingType, fileName, posAbove);
				}
			}
			
			//System.Console.WriteLine("CallingMember: " + callingMember);
			if (callingMember != null && !setupLookupTableVisitor ) {
				string wrapper = CreateWrapperClassForMember (callingMember, fileName, editor);
				using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (lang, new StringReader (wrapper))) {
					parser.Parse ();
					memberCompilationUnit = parser.CompilationUnit;
					lookupTableVisitor.VisitCompilationUnit (parser.CompilationUnit, null);
					lookupVariableLine = CallingMember.Location.Line - 2;
					setupLookupTableVisitor = true;
				}
			} else if (editor != null) {
				string wrapper = editor.Text;
				using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (lang, new StringReader (wrapper))) {
					parser.Parse ();
					memberCompilationUnit = parser.CompilationUnit;
					lookupTableVisitor.VisitCompilationUnit (parser.CompilationUnit, null);
					lookupVariableLine = 0;
					setupLookupTableVisitor = true;
				}
			}
		}
		
		bool setupLookupTableVisitor = false;
		int lookupVariableLine = 0;
		internal void SetupParsedCompilationUnit (ICSharpCode.NRefactory.Ast.CompilationUnit unit)
		{
			lookupVariableLine = 0; // all compilation unit lines are 1 based
			memberCompilationUnit = unit;
			lookupTableVisitor.VisitCompilationUnit (unit, null);
			setupLookupTableVisitor = true;
		}
		
		static void AddParameterList (CSharpTextEditorCompletion.CompletionDataCollector col, IEnumerable<IParameter> parameters)
		{
			foreach (IParameter p in parameters) {
				col.Add (p);
//				completionList.Add (p.Name, "md-literal");
			}
		}
		
		void AddContentsFromClassAndMembers (ExpressionContext context, CSharpTextEditorCompletion.CompletionDataCollector col)
		{
			IMethod method = callingMember as IMethod;
			if (method != null && method.Parameters != null) {
				AddParameterList (col, method.Parameters);
			}
			IProperty property = callingMember as IProperty;
			if (property != null && property.Parameters != null)
				AddParameterList (col, property.Parameters);
			if (CallingType == null)
				return;
			AddContentsFromOuterClass (CallingType.DeclaringType, context, col);
			IType callingType = CallingType is InstantiatedType ? ((InstantiatedType)CallingType).UninstantiatedType : CallingType;
			//bool isInStatic = CallingMember != null ? CallingMember.IsStatic : false;

			if (CallingMember == null || !CallingMember.IsStatic) {
				foreach (TypeParameter parameter in callingType.TypeParameters) {
					col.Add (parameter.Name, "md-literal");
				}
			}
			
			if (context != ExpressionContext.TypeDeclaration && CallingMember != null) {
				bool includeProtected = DomType.IncludeProtected (dom, CallingType, CallingMember.DeclaringType);
				foreach (IType type in dom.GetInheritanceTree (CallingType)) {
					foreach (IMember member in type.Members) {
						if (!(member is IType) && CallingMember.IsStatic && !(member.IsStatic || member.IsConst))
							continue;
						if (member.IsAccessibleFrom (dom, CallingType, CallingMember, includeProtected)) {
							if (context.FilterEntry (member))
								continue;
							col.Add (member);
						}
					}
				}
			}
		}
		
		void AddContentsFromOuterClass (IType outer, ExpressionContext context, CSharpTextEditorCompletion.CompletionDataCollector col)
		{
			if (outer == null)
				return;
			foreach (IMember member in outer.Members) {
				if (member is IType || member.IsStatic || member.IsConst) {
					col.Add (member);
				}
			}
			AddContentsFromOuterClass (outer.DeclaringType, context, col);
		}
		
		static readonly IReturnType attributeType = new DomReturnType ("System.Attribute");
		public CSharpTextEditorCompletion.CompletionDataCollector AddAccessibleCodeCompletionData (ExpressionContext context, CSharpTextEditorCompletion.CompletionDataCollector col)
		{
			if (context != ExpressionContext.Global && context != ExpressionContext.TypeName) {
				AddContentsFromClassAndMembers (context, col);
				
				if (lookupTableVisitor != null && lookupTableVisitor.Variables != null) {
					int callingMemberline = CallingMember != null ? CallingMember.Location.Line : 0;
					// local variables could be outside members (LINQ initializers) 
					foreach (KeyValuePair<string, List<LocalLookupVariable>> pair in lookupTableVisitor.Variables) {
						if (pair.Value != null && pair.Value.Count > 0) {
							foreach (LocalLookupVariable v in pair.Value) {
								if (new DomLocation (callingMemberline + v.StartPos.Line - 2, v.StartPos.Column) <= this.resolvePosition && (v.EndPos.IsEmpty || new DomLocation (callingMemberline + v.EndPos.Line - 2, v.EndPos.Column) >= this.resolvePosition)) {
									col.Add (new LocalVariable (CallingMember, pair.Key, ConvertTypeReference (v.TypeRef), DomRegion.Empty));
								}
							}
						}
					}
				}
				
				if (CallingMember is IProperty) {
					IProperty property = (IProperty)callingMember;
					if (property.HasSet && editor != null && property.SetRegion.Contains (resolvePosition.Line, editor.CursorColumn))
						col.Add ("value");
				}
				
				if (CallingMember is IEvent)
					col.Add ("value");
			}
			
			List<string> namespaceList = new List<string> ();
			namespaceList.Add ("");
			
			List<string> namespaceDeclList = new List<string> ();
			namespaceDeclList.Add ("");
			if (unit != null) {
				foreach (IUsing u in unit.Usings) {
					foreach (string alias in u.Aliases.Keys) {
						col.Add (alias);
					}
					if (u.Namespaces == null)
						continue;
					bool isNamespaceDecl = u.IsFromNamespace && u.Region.Contains (this.resolvePosition);
					if (u.IsFromNamespace && !isNamespaceDecl)
						continue;
					foreach (string ns in u.Namespaces) {
						namespaceList.Add (ns);
						if (isNamespaceDecl)
							namespaceDeclList.Add (ns);
					}
				}
				
				foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
					if (context.FilterEntry (o))
						continue;
					if (o is Namespace) {
						Namespace ns = o as Namespace;
						bool skip = true;
						foreach (string str in namespaceDeclList) {
							if (dom.NamespaceExists (str.Length > 0 ? str + "." + ns.Name : ns.Name)) {
								skip = false;
								break;
							}
						}
						if (skip)
							continue;
					}
					
					//IMember member = o as IMember;
					//if (member != null && completionList.Find (member.Name) != null)
					//	continue;
					if (context == ExpressionContext.Attribute) {
						IType t = o as IType;
						if (t != null && !t.IsBaseType (attributeType))
							continue;
					}
					ICompletionData data = col.Add (o);
					if (data != null && context == ExpressionContext.Attribute && data.CompletionText != null && data.CompletionText.EndsWith ("Attribute")) {
						string newText = data.CompletionText.Substring (0, data.CompletionText.Length - "Attribute".Length);
						data.SetText (newText);
					}
				}
				CodeTemplateService.AddCompletionDataForMime ("text/x-csharp", col.CompletionList);
			}
			return col;
		}
		
		Expression ParseExpression (ExpressionResult expressionResult)
		{
			if (expressionResult == null || String.IsNullOrEmpty (expressionResult.Expression))
				return null;
			string expr = expressionResult.Expression.Trim ();
			if (!expr.EndsWith (";"))
				expr += ";";
			using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (this.lang, new StringReader (expr))) {
				Expression result = parser.ParseExpression();
				if (result is BinaryOperatorExpression) {
					TypeReference typeRef = ParseTypeReference (expressionResult);
					if (typeRef != null) {
						return new TypeReferenceExpression (typeRef);
					}
				}
				return result;
			}
		}
		
		static TypeReference ParseTypeReference (ExpressionResult expressionResult)
		{
			if (expressionResult == null || String.IsNullOrEmpty (expressionResult.Expression))
				return null;
			string expr = expressionResult.Expression.Trim ();
			using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (SupportedLanguage.CSharp, new StringReader ("typeof(" + expr + ");"))) {
				TypeOfExpression typeOfExpression = parser.ParseExpression () as TypeOfExpression;
				if (typeOfExpression != null)
					return typeOfExpression.TypeReference;
			}
			return null;
		}
		
		public static IReturnType ParseReturnType (ExpressionResult expressionResult)
		{
			TypeReference typeReference = ParseTypeReference (expressionResult);
			if (typeReference == null)
				return null;
			return ConvertTypeReference (typeReference);
		}
		
		public ResolveResult ResolveIdentifier (string identifier, DomLocation resolvePosition)
		{
			this.SetupResolver (resolvePosition);
			ResolveVisitor visitor = new ResolveVisitor (this);
			ResolveResult result = this.ResolveIdentifier (visitor, identifier);
			return result;
		}
		
		public ResolveResult ResolveExpression (Expression expr, DomLocation resolvePosition)
		{
			this.expr = expr;
			this.SetupResolver (resolvePosition);
			ResolveVisitor visitor = new ResolveVisitor (this);
			ResolveResult result = visitor.Resolve (expr);
			return result;
		}
		
		Expression expr;
		public Expression ResolvedExpression {
			get {
				return expr;
			}
		}
	
		public DomLocation ResolvePosition {
			get {
				return resolvePosition;
			}
		}
		
		public ResolveResult Resolve (ExpressionResult expressionResult, DomLocation resolvePosition)
		{
			this.SetupResolver (resolvePosition);
			ResolveVisitor visitor = new ResolveVisitor (this);
			ResolveResult result;
//			System.Console.WriteLine("expressionResult:" + expressionResult);

			if (unit != null && expressionResult.ExpressionContext == ExpressionContext.AttributeArguments) {
				string attributeName = NewCSharpExpressionFinder.FindAttributeName (editor, unit, unit.FileName);
				if (attributeName != null) {
					IType type = SearchType (attributeName + "Attribute");
					if (type == null) 
						type = SearchType (attributeName);
					if (type != null) {
						foreach (IProperty property in type.Properties) {
							if (property.Name == expressionResult.Expression) {
								return new MemberResolveResult (property);
							}
						}
					}
				}
			}
			
			TypeReference typeRef;
			if (expressionResult != null && expressionResult.ExpressionContext != null && expressionResult.ExpressionContext.IsObjectCreation) {
				typeRef = ParseTypeReference (expressionResult);
				if (typeRef != null) {
					if (dom.NamespaceExists (typeRef.Type)) {
//						System.Console.WriteLine("namespace resolve result");
						result = new NamespaceResolveResult (typeRef.Type);
					} else {
						result = visitor.CreateResult (ConvertTypeReference (typeRef));
					}
//					System.Console.WriteLine("type reference resolve result");
					result.ResolvedExpression = expressionResult;
					if (dom.GetType (result.ResolvedType) != null)
						return result;
				}
			}
			expr = ParseExpression (expressionResult);
			
//			System.Console.WriteLine("parsed expression:" + expr);
			if (expr == null) {
//				System.Console.WriteLine("Can't parse expression");
				return null;
			}
			
			result = visitor.Resolve (expr);
//			if (CallingMember == null && result != null)
//				result.StaticResolve = true;
//			System.Console.WriteLine("result:" + result + "STATIC" + result.StaticResolve);
			result.ResolvedExpression = expressionResult;
		
			return result;
		}
		
		public static IReturnType ConvertTypeReference (TypeReference typeRef)
		{
			return typeRef.ConvertToReturnType ();
		}
		
		public IReturnType ResolveType (IReturnType type)
		{
			return ResolveType (unit, type);
		}
		
		IReturnType ResolveType (ICompilationUnit unit, IReturnType type)
		{
			if (type == null)
				return DomReturnType.Void;
			if (type.Type != null) // type known (possible anonymous type), no resolving needed
				return type;
			TypeResolverVisitor typeResolverVisitor = new TypeResolverVisitor (dom, unit);
			typeResolverVisitor.SetCurrentMember (this.CallingMember);
			return (IReturnType)typeResolverVisitor.Visit (type, this.CallingType);
		}
		
		
		ResolveResult GetFunctionParameterType (ResolveResult resolveResult)
		{
			if (resolveResult == null || resolveResult.ResolvedType == null)
				return null;
			IReturnType type = resolveResult.ResolvedType;
			while (type.GenericArguments.Count > 0) {
				IType realType = SearchType (type);
				if (realType != null && realType.ClassType == MonoDevelop.Projects.Dom.ClassType.Delegate) {
					IMethod invokeMethod = realType.SearchMember ("Invoke", true) [0] as IMethod;
					if (invokeMethod != null && invokeMethod.Parameters.Count > 0) {
						type = invokeMethod.Parameters[0].ReturnType;
						break;
					}
				}
				if (type.GenericArguments.Count > 0) {
					type = type.GenericArguments[0];
				} else {
					break;
				}
			}
			resolveResult.ResolvedType = type;
			return resolveResult;
		}
		class TypeReplaceVisitor : CopyDomVisitor <object>
		{
			IReturnType replaceType;
			IReturnType replaceWith;
			
			public TypeReplaceVisitor (IReturnType replaceType, IReturnType replaceWith)
			{
				this.replaceType = replaceType;
				this.replaceWith = replaceWith;
			}
			
			public override MonoDevelop.Projects.Dom.INode Visit (IReturnType type, object data)
			{
				if (type.ToInvariantString () == replaceType.ToInvariantString ())
					return base.Visit (replaceWith, data);
				return base.Visit (type, data);
			}

		}
		class LambdaResolver
		{
			HashSet<Expression> expressions = new HashSet<Expression> ();
			Dictionary<string, ResolveResult> returnTypeDictionary = new Dictionary<string, ResolveResult> ();
			NRefactoryResolver resolver;
			
			public LambdaResolver (NRefactoryResolver resolver)
			{
				this.resolver = resolver;
			}
			
			internal ResolveResult ResolveLambda (ResolveVisitor visitor, Expression lambdaExpression)
			{
				if (expressions.Contains (lambdaExpression)) {
					return null;
				}
				expressions.Add (lambdaExpression);
				if (lambdaExpression.Parent is LambdaExpression)
					return ResolveLambda (visitor, lambdaExpression.Parent as Expression);
				if (lambdaExpression.Parent is ParenthesizedExpression)
					return ResolveLambda (visitor, lambdaExpression.Parent as Expression);
				if (lambdaExpression.Parent is AssignmentExpression)
					return visitor.Resolve (((AssignmentExpression)lambdaExpression.Parent).Left);
				if (lambdaExpression.Parent is CastExpression)
					return visitor.Resolve (((CastExpression)lambdaExpression.Parent));
				if (lambdaExpression.Parent is VariableDeclaration) {
					VariableDeclaration varDec = (VariableDeclaration)lambdaExpression.Parent;
					return resolver.GetFunctionParameterType (resolver.ResolveIdentifier (visitor, varDec.Name));
				}
				if (lambdaExpression.Parent is InvocationExpression) {
					LambdaExpression lambda = (LambdaExpression)lambdaExpression; 
					ResolveResult lambdaReturnType = null;
					if (!lambda.ExpressionBody.IsNull) {
						DomLocation old = resolver.resolvePosition;
						try {
							resolver.resolvePosition = new DomLocation (resolver.CallingMember.Location.Line + 
							                                            lambda.ExpressionBody.StartLocation.Line - 2,
							                                            lambda.ExpressionBody.StartLocation.Column - 1);
							lambdaReturnType =  visitor.Resolve (lambda.ExpressionBody);
						} finally {
							resolver.resolvePosition = old;
						}
					}
					
					InvocationExpression invocation = (InvocationExpression)lambdaExpression.Parent;
					MethodResolveResult result = visitor.Resolve (invocation.TargetObject) as MethodResolveResult;
					if (result == null) {
						MonoDevelop.Core.LoggingService.LogWarning ("No compatible method found :" + invocation.TargetObject);
						return null;
					}
					result.ResolveExtensionMethods ();
					
					for (int i = 0; i < invocation.Arguments.Count; i++) {
						if (invocation.Arguments[i] == lambdaExpression && i < result.MostLikelyMethod.Parameters.Count) {
							IParameter parameter = result.MostLikelyMethod.Parameters[i];
							IReturnType returnType = parameter.ReturnType;
							IType type = resolver.Dom.GetType (returnType);
							bool isResolved = false;
							if (type != null && type.ClassType == MonoDevelop.Projects.Dom.ClassType.Delegate) {
								IMethod invocationMethod = type.Methods.First ();
								if (invocationMethod.Parameters.Count > 0) {
									if (lambdaReturnType == null || string.IsNullOrEmpty (lambdaReturnType.ResolvedType.FullName)) {
										returnType = invocationMethod.Parameters[System.Math.Min (i, invocationMethod.Parameters.Count - 1)].ReturnType;
									} else {
										returnType = (IReturnType)new TypeReplaceVisitor (invocationMethod.ReturnType, lambdaReturnType.ResolvedType).Visit (returnType, null);
									}
									isResolved = true;
								}
							}
							if (!isResolved) {
								while (returnType.GenericArguments.Count > 0) {
									returnType = returnType.GenericArguments[0];
								}
							}
							string invariantString = returnType.ToInvariantString ();
							if (returnTypeDictionary.ContainsKey (invariantString))
								return returnTypeDictionary[invariantString];
							ResolveResult createdResult = visitor.CreateResult (returnType);
							returnTypeDictionary[invariantString] = createdResult;
							return createdResult;
						}
					}
					
					if (lambdaReturnType != null && !string.IsNullOrEmpty (lambdaReturnType.ResolvedType.FullName))
						return lambdaReturnType;
					
					foreach (Expression arg in invocation.Arguments) {
						var argType = arg is LambdaExpression ?  DomReturnType.Void : visitor.GetTypeSafe (arg);
						result.AddArgument (argType);
					}
					
					result.ResolveExtensionMethods ();
					//Console.WriteLine ("maybe method:" + result.MostLikelyMethod);
					for (int i = 0; i < invocation.Arguments.Count; i++) {
						if (invocation.Arguments [i] == lambdaExpression && i < result.MostLikelyMethod.Parameters.Count) {
							IParameter parameterType = result.MostLikelyMethod.Parameters [i];
							//Console.WriteLine (i + " par: " + parameterType);
							if (parameterType.ReturnType.Name == "Func" && parameterType.ReturnType.GenericArguments.Count > 0) {
								return visitor.CreateResult (parameterType.ReturnType.GenericArguments[0]);
							}
						}
					}
					return result;
				}
				
				if (lambdaExpression.Parent is ObjectCreateExpression) {
					ObjectCreateExpression objectCreateExpression = (ObjectCreateExpression)lambdaExpression.Parent;
					int index = objectCreateExpression.Parameters.IndexOf (lambdaExpression);
					if (index < 0)
						return null;
					MemberResolveResult resolvedCreateExpression = visitor.Resolve (objectCreateExpression) as MemberResolveResult;
					
					if (resolvedCreateExpression != null) {
						IMethod method = resolvedCreateExpression.ResolvedMember as IMethod;
						if (method!= null && index < method.Parameters.Count) {
							return new ParameterResolveResult (method.Parameters[index]);
						} else {
							return null;
						}
					}
				}
				
				return null;
			}
		}

		public ResolveResult ResolveLambda (ResolveVisitor visitor, Expression lambdaExpression)
		{
			return new LambdaResolver (this).ResolveLambda (visitor, lambdaExpression);
		}
		
		Dictionary<string, ResolveResult> resultTable = new Dictionary<string, ResolveResult> ();
		public ResolveResult ResolveIdentifier (ResolveVisitor visitor, string identifier)
		{
			ResolveResult result = null;
			if (resultTable.TryGetValue (identifier, out result))
				return result;
			resultTable[identifier] = result;
			
			foreach (KeyValuePair<string, List<LocalLookupVariable>> pair in this.lookupTableVisitor.Variables) {
				if (identifier == pair.Key) {
					LocalLookupVariable var = null;
//					Console.WriteLine ("--- RP:" + this.resolvePosition + "/" + pair.Value.Count);
					foreach (LocalLookupVariable v2 in pair.Value) {
						DomLocation varStartPos = new DomLocation (lookupVariableLine + v2.StartPos.Line, v2.StartPos.Column - 1);
						DomLocation varEndPos   = new DomLocation (lookupVariableLine + v2.EndPos.Line, v2.EndPos.Column - 1);
//						Console.WriteLine (v2.Name + ":" + varStartPos + " <> " + varEndPos + " resolve position:" + this.resolvePosition);
						if (varStartPos > this.resolvePosition || (!v2.EndPos.IsEmpty && varEndPos < this.resolvePosition))
							continue;
						var = v2;
					}
//					Console.WriteLine ("var:" + var);
					if (var == null)
						continue;
					IReturnType varType = null;
					IReturnType varTypeUnresolved = null;
					if (var.IsQueryContinuation) {
						QueryExpression query = var.Initializer as QueryExpression;
						
						QueryExpressionGroupClause grouBy = query.SelectOrGroupClause as QueryExpressionGroupClause;
						DomLocation old = resolvePosition;
						try {
							resolvePosition = new DomLocation (lookupVariableLine + grouBy.Projection.StartLocation.Line,
							                                   grouBy.Projection.StartLocation.Column);
							ResolveResult initializerResolve = visitor.Resolve (grouBy.Projection);
							ResolveResult groupByResolve = visitor.Resolve (grouBy.GroupBy);
							DomReturnType resolved = new DomReturnType (dom.GetType ("System.Linq.IGrouping", new IReturnType [] { 
								DomType.GetComponentType (dom, initializerResolve.ResolvedType), groupByResolve.ResolvedType}));
						varTypeUnresolved = varType = resolved;
						} finally {
							resolvePosition = old;
						}
						
					} else if ((var.TypeRef == null || var.TypeRef.Type == "var" || var.TypeRef.IsNull)) {
						if (var.ParentLambdaExpression != null) {
							ResolveResult lambdaResolve = ResolveLambda (visitor, var.ParentLambdaExpression);
							if (lambdaResolve != null) {
								varType           = lambdaResolve.ResolvedType;
								varTypeUnresolved = lambdaResolve.UnresolvedType;
							} else {
								varType = varTypeUnresolved = DomReturnType.Void;
							}
						}
						if (var.Initializer != null) {
							ResolveResult initializerResolve = visitor.Resolve (var.Initializer);
//							Console.WriteLine ("initializer : "+ var.Initializer + " result:" + initializerResolve);
							varType           = var.IsLoopVariable ? DomType.GetComponentType (dom, initializerResolve.ResolvedType) : initializerResolve.ResolvedType;
							varTypeUnresolved = var.IsLoopVariable ? DomType.GetComponentType (dom, initializerResolve.UnresolvedType) : initializerResolve.UnresolvedType;
//							Console.WriteLine ("resolved type:" + initializerResolve.ResolvedType + " is loop : " + var.IsLoopVariable);
//							Console.WriteLine (varType);
//							Console.WriteLine ("----------");
						}
					} else { 
						varTypeUnresolved = varType = ConvertTypeReference (var.TypeRef);
					}
					
					varType = ResolveType (varType);
					result = new LocalVariableResolveResult (
						new LocalVariable (CallingMember, identifier, varType,
							new DomRegion (lookupVariableLine + var.StartPos.Line - 1, var.StartPos.Column - 1, 
							               lookupVariableLine + var.StartPos.Line - 1, var.EndPos.Column - 1)),
							var.IsLoopVariable);
					
					result.ResolvedType = varType;
					result.UnresolvedType = varTypeUnresolved;
					goto end;
				}
			}
			if (this.callingMember != null) {
				// special handling of property or field return types, they can have the same name as the return type
				// ex.: MyType MyType { get; set; }  Type1 Type1;
				if ((callingMember is IProperty || callingMember is IField) && identifier == callingMember.Name) {
					int pos = editor.GetPositionFromLineColumn (resolvePosition.Line, resolvePosition.Column);
					while (pos < editor.TextLength && !Char.IsWhiteSpace (editor.GetCharAt (pos)))
						pos++;
					while (pos < editor.TextLength && Char.IsWhiteSpace (editor.GetCharAt (pos)))
						pos++;
					StringBuilder memberName = new StringBuilder ();
					while (pos < editor.TextLength && (Char.IsLetterOrDigit (editor.GetCharAt (pos)) || editor.GetCharAt (pos) == '_') ) {
						memberName.Append (editor.GetCharAt (pos));
						pos++;
					}
					//Console.WriteLine ("id: '" + identifier + "' : '" + memberName.ToString () +"'" + (memberName.ToString () == identifier));
					if (memberName.ToString () == identifier) {
						result = visitor.CreateResult (callingMember.ReturnType);
						goto end;
					}
				}
				
				if (identifier == "value" && this.callingMember is IProperty) {
					result = new MemberResolveResult (this.callingMember);
					result.UnresolvedType = ((IProperty)this.callingMember).ReturnType;
					result.ResolvedType = ResolveType (((IProperty)this.callingMember).ReturnType);
					goto end;
				}
				if (this.callingMember is IMethod || this.callingMember is IProperty) {
					ReadOnlyCollection<IParameter> prms = this.callingMember is IMethod
						? ((IMethod)this.callingMember).Parameters
						: ((IProperty)this.callingMember).Parameters;
					if (prms != null) {
						foreach (IParameter para in prms) {
							if (para.Name == identifier) {
								result = new ParameterResolveResult (para);
								result.UnresolvedType = para.ReturnType;
								result.ResolvedType = ResolveType (para.ReturnType);
								goto end;
							}
						}
					}
				}
			}
			IType searchedType = SearchType (identifier);
			if (this.callingType != null && dom != null) {
				List<IMember> members = new List <IMember> ();
				foreach (IType type in dom.GetInheritanceTree (callingType)) {
					members.AddRange (type.SearchMember (identifier, true));
				}
				bool includeProtected = true;
				// filter members
				if (this.CallingMember != null) {
					for (int i = 0; i < members.Count; i++) {
						if (this.CallingMember.IsStatic && !members[i].IsStatic
						    || !members[i].IsAccessibleFrom (dom, callingType, this.CallingMember, includeProtected))
						{
							members.RemoveAt (i);
							i--;
							continue;
						}
					}
				}
				
				if (members.Count > 0) {
					if (members[0] is IMethod) {
						result = new MethodResolveResult (members);
						if (CallingMember != null)
							result.StaticResolve = CallingMember.IsStatic;
					} else if (members[0] is IType) {
						result = new MemberResolveResult (null, true);
						result.UnresolvedType = result.ResolvedType = new DomReturnType ((IType)members[0]);
						goto end;
					} else {
						result = new MemberResolveResult (members[0]);
					}
					result.UnresolvedType = members[0].ReturnType;
					result.ResolvedType = ResolveType (members[0].ReturnType);
					
					if (members[0] is IProperty && searchedType != null && result.ResolvedType.FullName == searchedType.FullName) {
						result = new AggregatedResolveResult (result, new MemberResolveResult (null, true) {
							UnresolvedType = new DomReturnType (searchedType),
							ResolvedType = new DomReturnType (searchedType)
						});
					}
					goto end;
				}
			}
			
			if (searchedType != null) {
				result = new MemberResolveResult (null, true);
				result.UnresolvedType = result.ResolvedType = new DomReturnType (searchedType);
				goto end;
			}
			
			if (dom.NamespaceExists (identifier, true)) {
				result = new NamespaceResolveResult (identifier);
				goto end;
			}

			if (unit != null && unit.Usings != null) {
				foreach (IUsing u in unit.Usings) {
					if (u.IsFromNamespace && u.Region.Contains (resolvePosition)) {
						foreach (string ns in u.Namespaces) {
							if (dom.NamespaceExists (ns + "."  + identifier, true)) {
								result = new NamespaceResolveResult (ns + "."  + identifier);
								goto end;
							}
						}
					}
					foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
						if (alias.Key == identifier || alias.Key + ".?" == identifier) {
							result = new NamespaceResolveResult (alias.Value.FullName);
							goto end;
						}
					}
				}
			}
		end:
			if (result != null) {
				result.CallingType   = CallingType;
				result.CallingMember = CallingMember;
			}
			resultTable[identifier] = result;
			return result;
		}
		
		internal static string CreateWrapperClassForMember (IMember member, string fileName, TextEditor editor)
		{
			if (member == null)
				return "";
			StringBuilder result = new StringBuilder ();
			int startLine = member.Location.Line;
			int endLine   = member.Location.Line;
			if (!member.BodyRegion.IsEmpty) 
				endLine = member.BodyRegion.End.Line + 1;
			
			string text;
			result.Append ("class " + member.DeclaringType.Name + " {");
			if (editor != null) {
				int col, maxLine;
				editor.GetLineColumnFromPosition (editor.TextLength - 1, out col, out maxLine);
				endLine = System.Math.Max (endLine, maxLine);
				
				int endPos = editor.GetPositionFromLineColumn (endLine, editor.GetLineLength (endLine));
				if (endPos < 0)
					endPos = editor.TextLength;
				int startPos = Math.Max (0, editor.GetPositionFromLineColumn (startLine, 0));
				text = editor.GetText (startPos, endPos);
			} else {
				Mono.TextEditor.Document doc = new Mono.TextEditor.Document ();
				doc.Text = File.ReadAllText (fileName) ?? "";
				startLine = Math.Min (doc.LineCount, Math.Max (1, startLine));
				endLine   = Math.Min (doc.LineCount, Math.Max (1, endLine));
				int startOffset = doc.LocationToOffset (startLine - 1, 0);
				text = doc.GetTextAt (startOffset, doc.LocationToOffset (endLine  - 1, doc.GetLine (endLine - 1).EditableLength) - startOffset);
			}
			if (!string.IsNullOrEmpty (text))
				result.Append (text);
			result.Append ("}");
			
			return result.ToString ();
		}
	}
	static class HelperMethods
	{
		public static void SetText (this ICompletionData data, string text)
		{
			if (data is CompletionData) {
				((CompletionData)data).CompletionText = text;
			} else if (data is MemberCompletionData) {
				((MemberCompletionData)data).CompletionText = text;
			} else {
				System.Console.WriteLine("Unknown completion data:" + data);
			}
		}
	}
	
}
