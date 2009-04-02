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
using MonoDevelop.Projects.Gui.Completion;

using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharpBinding
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
		
		ICSharpCode.NRefactory.Ast.CompilationUnit memberCompilationUnit;
		public ICSharpCode.NRefactory.Ast.CompilationUnit MemberCompilationUnit {
			get {
				return this.memberCompilationUnit;
			}
		}
		
		static IMember GetMemberAt (IType type, DomLocation location)
		{
			foreach (IMember member in type.Members) {
				if (!(member is IMethod || member is IProperty || member is IEvent))
					continue;
				if (member.Location.Line == location.Line || member.BodyRegion.Contains (location)) {
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
				callingMember = GetMemberAt (callingType, resolvePosition);
				if (callingMember == null) {
					DomLocation posAbove = resolvePosition;
					posAbove.Line--;
					callingMember = GetMemberAt (callingType, posAbove);
				}
				callingType = dom.ResolveType (callingType);
			}
			//System.Console.WriteLine("CallingMember: " + callingMember);
			if (callingMember != null && !setupLookupTableVisitor ) {
				string wrapper = CreateWrapperClassForMember (callingMember);
				using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (lang, new StringReader (wrapper))) {
					parser.Parse ();
					memberCompilationUnit = parser.CompilationUnit;
					lookupTableVisitor.VisitCompilationUnit (parser.CompilationUnit, null);
					lookupVariableLine = CallingMember.Location.Line - 2;
					setupLookupTableVisitor = true;
				}
			} else {
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
			lookupVariableLine = 0;
			memberCompilationUnit = unit;
			lookupTableVisitor.VisitCompilationUnit (unit, null);
			setupLookupTableVisitor = true;
		}
		
		static void AddParameterList (CompletionDataList completionList, MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector col, IEnumerable<IParameter> parameters)
		{
			foreach (IParameter p in parameters) {
				col.AddCompletionData (completionList, p);
//				completionList.Add (p.Name, "md-literal");
			}
		}
				
		void AddContentsFromClassAndMembers (ExpressionContext context, CompletionDataList completionList, MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector col)
		{
			IMethod method = callingMember as IMethod;
			if (method != null && method.Parameters != null) {
				AddParameterList (completionList, col, method.Parameters);
			}
			IProperty property = callingMember as IProperty;
			if (property != null && property.Parameters != null) 
				AddParameterList (completionList, col, property.Parameters);
			if (CallingType == null)
				return;
			AddContentsFromOuterClass (CallingType.DeclaringType, context, completionList, col);
			IType callingType = CallingType is InstantiatedType ? ((InstantiatedType)CallingType).UninstantiatedType : CallingType;
			//bool isInStatic = CallingMember != null ? CallingMember.IsStatic : false;

			if (CallingMember == null || !CallingMember.IsStatic) {
				foreach (TypeParameter parameter in callingType.TypeParameters) {
					completionList.Add (parameter.Name, "md-literal");
				}
			}
			
			if (CallingMember != null) {
				bool includeProtected = DomType.IncludeProtected (dom, CallingType, CallingMember.DeclaringType);
				foreach (IType type in dom.GetInheritanceTree (CallingType)) {
					foreach (IMember member in type.Members) {
						if (!(member is IType) && CallingMember.IsStatic && !(member.IsStatic || member.IsConst))
							continue;
						if (member.IsAccessibleFrom (dom, CallingType, CallingMember, includeProtected)) {
							if (context.FilterEntry (member))
								continue;
							col.AddCompletionData (completionList, member);
						}
					}
				}
			}
		}
		
		void AddContentsFromOuterClass (IType outer, ExpressionContext context, CompletionDataList completionList, MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector col)
		{
			if (outer == null)
				return;
			foreach (IMember member in outer.Members) {
				if (member is IType || member.IsStatic || member.IsConst) {
					col.AddCompletionData (completionList, member);
				}
			}
			AddContentsFromOuterClass (outer.DeclaringType, context, completionList, col);
		}
		
		static readonly IReturnType attributeType = new DomReturnType ("System.Attribute");
		public MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector AddAccessibleCodeCompletionData (ExpressionContext context, CompletionDataList completionList)
		{
			MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector col = new MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector (unit, new DomLocation (editor.CursorLine - 1, editor.CursorColumn - 1));
			if (context != ExpressionContext.Global) {
				AddContentsFromClassAndMembers (context, completionList, col);
				
				if (lookupTableVisitor != null && lookupTableVisitor.Variables != null) {
					foreach (KeyValuePair<string, List<LocalLookupVariable>> pair in lookupTableVisitor.Variables) {
						if (pair.Value != null && pair.Value.Count > 0) {
							foreach (LocalLookupVariable v in pair.Value) {
								if (new DomLocation (CallingMember.Location.Line + v.StartPos.Line - 2, v.StartPos.Column) <= this.resolvePosition && (v.EndPos.IsEmpty || new DomLocation (CallingMember.Location.Line + v.EndPos.Line - 2, v.EndPos.Column) >= this.resolvePosition)) {
									col.AddCompletionData (completionList, new LocalVariable (CallingMember, pair.Key, ConvertTypeReference (v.TypeRef) , DomRegion.Empty));
								}
							}
						}
					}
				}
			
				if (CallingMember is IProperty) {
					IProperty property = (IProperty)callingMember;
					if (property.HasSet && editor != null && property.SetRegion.Contains (resolvePosition.Line, editor.CursorColumn)) 
						col.AddCompletionData (completionList, "value");
				}
			
				if (CallingMember is IEvent) 
					col.AddCompletionData (completionList, "value");
			}
			
			List<string> namespaceList = new List<string> ();
			namespaceList.Add ("");
			
			List<string> namespaceDeclList = new List<string> ();
			namespaceDeclList.Add ("");
			if (unit != null) {
				foreach (IUsing u in unit.Usings) {
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
					ICompletionData data = col.AddCompletionData (completionList, o);
					if (data != null && context == ExpressionContext.Attribute && data.CompletionText != null && data.CompletionText.EndsWith ("Attribute")) {
						string newText = data.CompletionText.Substring (0, data.CompletionText.Length - "Attribute".Length);
						data.SetText (newText);
					}
				}
				CodeTemplateService.AddCompletionDataForMime ("text/x-csharp", completionList);
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
			System.Console.WriteLine("expressionResult:" + expressionResult);
			
			if (expressionResult.ExpressionContext == ExpressionContext.AttributeArguments) {
				string attributeName = MonoDevelop.CSharpBinding.Gui.NewCSharpExpressionFinder.FindAttributeName (editor, unit, unit.FileName);
				if (attributeName != null) {
					IType type = dom.SearchType (new SearchTypeRequest (unit, new DomReturnType (attributeName + "Attribute"), CallingType));
					if (type == null) 
						type = dom.SearchType (new SearchTypeRequest (unit, new DomReturnType (attributeName), CallingType));
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
						result =  new NamespaceResolveResult (typeRef.Type);
					} else {
						result = visitor.CreateResult (ConvertTypeReference (typeRef));
					}
//					System.Console.WriteLine("type reference resolve result");
					result.ResolvedExpression = expressionResult.Expression;
					return result;
				}
			}
			expr = ParseExpression (expressionResult);
			
			System.Console.WriteLine("parsed expression:" + expr);
			if (expr == null) {
//				System.Console.WriteLine("Can't parse expression");
				return null;
			}
			
			result = visitor.Resolve (expr);
//			if (CallingMember == null && result != null)
//				result.StaticResolve = true;
//			System.Console.WriteLine("result:" + result + "STATIC" + result.StaticResolve);
			result.ResolvedExpression = expressionResult.Expression;
			return result;
		}
		
		public static IReturnType ConvertTypeReference (TypeReference typeRef)
		{
			if (typeRef == null)
				return null;
			DomReturnType result = new DomReturnType (typeRef.SystemType ?? typeRef.Type);
			foreach (TypeReference genericArgument in typeRef.GenericTypes) {
				result.AddTypeParameter (ConvertTypeReference (genericArgument));
			}
			result.PointerNestingLevel = typeRef.PointerNestingLevel;
			if (typeRef.IsArrayType) {
				result.ArrayDimensions = typeRef.RankSpecifier.Length;
				for (int i = 0; i < typeRef.RankSpecifier.Length; i++) {
					result.SetDimension (i, typeRef.RankSpecifier[i]);
				}
			}
			return result;
		}
		
		IReturnType ResolveType (IReturnType type)
		{
			return ResolveType (unit, type);
		}

		bool TryResolve (ICompilationUnit unit, IReturnType type, out DomReturnType result)
		{
			IType resolvedType = dom.SearchType (new SearchTypeRequest (unit, type, callingType));
			//System.Console.WriteLine(type +" resolved to: " +resolvedType);
			if (resolvedType != null) {
				result = new DomReturnType (resolvedType);
				result.ArrayDimensions = type.ArrayDimensions;
				for (int i = 0; i < result.ArrayDimensions; i++) {
					result.SetDimension (i, type.GetDimension (i));
				}
				result.IsNullable = type.IsNullable;
				return true;
			}
			result = null;
			return false;
		}
		
		IReturnType ResolveType (ICompilationUnit unit, IReturnType type)
		{
			if (type == null)
				return DomReturnType.Void;
			if (type.Type != null) // type known (possible anonymous type), no resolving needed
				return type;
			DomReturnType result = null;
			if (TryResolve (unit, type, out result))
				return result;
			
			if (this.CallingType != null) {
				DomReturnType possibleInnerType = new DomReturnType (this.CallingType.FullName + "." + type.Name, type.IsNullable, type.GenericArguments);
				possibleInnerType.ArrayDimensions = type.ArrayDimensions;
				for (int i = 0; i < type.ArrayDimensions; i++) {
					possibleInnerType.SetDimension (i, type.GetDimension (i));
				}
				if (TryResolve (unit, possibleInnerType, out result))
					return result;
				
				if (this.CallingType.DeclaringType != null) {
					possibleInnerType = new DomReturnType (this.CallingType.DeclaringType.FullName + "." + type.Name, type.IsNullable, type.GenericArguments);
					possibleInnerType.ArrayDimensions = type.ArrayDimensions;
					for (int i = 0; i < type.ArrayDimensions; i++) {
						possibleInnerType.SetDimension (i, type.GetDimension (i));
					}
					if (TryResolve (unit, possibleInnerType, out result))
						return result;
				}
			}
			
			return type;
		}
		
		ResolveResult GetFunctionParameterType (ResolveResult resolveResult)
		{
			if (resolveResult == null || resolveResult.ResolvedType == null)
				return null;
			IReturnType type = resolveResult.ResolvedType;
			while (type.GenericArguments.Count > 0) {
				IType realType = dom.SearchType (new SearchTypeRequest (Unit, type, CallingType));
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

		public ResolveResult ResolveLambda (ResolveVisitor visitor, Expression lambdaExpression)
		{
			//Console.WriteLine ("oOoOoOoOoO");
			//Console.WriteLine ("lambda expr:" + lambdaExpression);
			//Console.WriteLine ("Parent:" + lambdaExpression.Parent);
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
				return GetFunctionParameterType (ResolveIdentifier (visitor, varDec.Name));
			}
			if (lambdaExpression.Parent is InvocationExpression) {
				InvocationExpression invocation = (InvocationExpression)lambdaExpression.Parent;
				MethodResolveResult result = visitor.Resolve (invocation.TargetObject) as MethodResolveResult;
				if (result == null) {
					MonoDevelop.Core.LoggingService.LogWarning ("No compatible method found :" + invocation.TargetObject);
					return null;
				} 
				result.ResolveExtensionMethods ();
				for (int i = 0; i < invocation.Arguments.Count; i++) {
					if (invocation.Arguments [i] == lambdaExpression && i < result.MostLikelyMethod.Parameters.Count) {
						IParameter parameterType = result.MostLikelyMethod.Parameters [i];
						if (parameterType.ReturnType.Name == "Func" && parameterType.ReturnType.GenericArguments.Count > 0) {
							return visitor.CreateResult (parameterType.ReturnType.GenericArguments[0]);
						}
					}
				}
				
				LambdaExpression lambda = (LambdaExpression)lambdaExpression;
				//Console.WriteLine ("lambda:" + lambda);
				if (!lambda.ExpressionBody.IsNull) {
					DomLocation old = resolvePosition;
					try {
						resolvePosition = new DomLocation (lookupVariableLine + lambda.ExpressionBody.StartLocation.Line,
						                                   lambda.ExpressionBody.StartLocation.Column);
						//result.AddArgument (visitor.GetTypeSafe (lambda.ExpressionBody));
						ResolveResult res =  visitor.Resolve (lambda.ExpressionBody);
						if (!string.IsNullOrEmpty (res.ResolvedType.FullName))
							return res;
					} finally {
						resolvePosition = old;
					}
				} 
				
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
		
		IReturnType GetEnumerationMember (IReturnType returnType)
		{
			if (returnType == null)
				return null;
			if (returnType.FullName == DomReturnType.String.FullName)
				return DomReturnType.Char;
			IType type = dom.SearchType (new SearchTypeRequest (Unit, returnType, CallingType));
			//System.Console.WriteLine("!!!! " + type);
			if (type != null) {
				foreach (IType baseType in dom.GetInheritanceTree (type)) {
					if (baseType.FullName == "System.Collections.IEnumerable") {
						InstantiatedType instantiated = type as InstantiatedType;
						if (instantiated != null && instantiated.GenericParameters != null && instantiated.GenericParameters.Count > 0) 
							return instantiated.GenericParameters[0];
					}
				}
			}
			
			if (returnType.ArrayDimensions > 0) {
				DomReturnType elementType = new DomReturnType (returnType.FullName);
				elementType.ArrayDimensions = returnType.ArrayDimensions - 1;
				return elementType;
			}
			
			return returnType;
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
					foreach (LocalLookupVariable v2 in pair.Value) {
						if (new DomLocation (lookupVariableLine + v2.StartPos.Line, v2.StartPos.Column) > this.resolvePosition || (!v2.EndPos.IsEmpty && new DomLocation (lookupVariableLine + v2.EndPos.Line, v2.EndPos.Column) < this.resolvePosition))
							continue;
						var = v2;
					}
					if (var != null) {
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
								GetEnumerationMember (initializerResolve.ResolvedType), groupByResolve.ResolvedType}));
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
								varType           = var.IsLoopVariable ? GetEnumerationMember (initializerResolve.ResolvedType)   : initializerResolve.ResolvedType;
								varTypeUnresolved = var.IsLoopVariable ? GetEnumerationMember (initializerResolve.UnresolvedType) : initializerResolve.UnresolvedType;
								Console.WriteLine ("resolved type:" + initializerResolve.ResolvedType + " is loop : " + var.IsLoopVariable);
								Console.WriteLine (varType);
								Console.WriteLine ("----------");
							}
						} else { 
							varTypeUnresolved = varType = ConvertTypeReference (var.TypeRef);
						}
						//Console.WriteLine ("-----");
						//Console.WriteLine (varType);
						varType = ResolveType (varType);
						result = new LocalVariableResolveResult (
							new LocalVariable (CallingMember, identifier, varType,
								new DomRegion (lookupVariableLine + var.StartPos.Line,  var.StartPos.Column - 1, 
								               lookupVariableLine + var.StartPos.Line, var.EndPos.Column - 1)),
								var.IsLoopVariable);
						
						result.ResolvedType = varType;
						result.UnresolvedType = varTypeUnresolved;
						goto end;
					}
				}
			}
			IType searchedType = dom.SearchType (new SearchTypeRequest (unit, this.CallingType, identifier));
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
			
			if (this.callingMember != null) {
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
		
		string CreateWrapperClassForMember (IMember member)
		{
			if (member == null)
				return "";
			StringBuilder result = new StringBuilder ();
			int startLine = member.Location.Line;
			int endLine   = member.Location.Line;
			if (!member.BodyRegion.IsEmpty) 
				endLine = member.BodyRegion.End.Line + 1;
			string text;
			result.Append ("class Wrapper {");
			if (editor != null) {
				int col, maxLine;
				editor.GetLineColumnFromPosition (editor.TextLength - 1, out col, out maxLine);
				endLine = System.Math.Max (endLine, maxLine);
				
				int endPos = this.editor.GetPositionFromLineColumn (endLine, this.editor.GetLineLength (endLine));
				if (endPos < 0)
					endPos = this.editor.TextLength;
				text = this.editor.GetText (this.editor.GetPositionFromLineColumn (startLine, 0), endPos);
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
