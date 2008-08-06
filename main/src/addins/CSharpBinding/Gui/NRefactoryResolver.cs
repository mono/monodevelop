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
using MonoDevelop.Projects.Gui.Completion;

using CSharpBinding.Parser.SharpDevelopTree;
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
		TextEditor editor;
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
		
		public static IType GetTypeAtCursor (ICompilationUnit unit, string fileName, DomLocation position)
		{
			if (unit == null)
				return null;
			foreach (IType type in unit.Types) {
				if (type.BodyRegion.Contains (position))
					return type;
			}
			return null;
		}
		
		public NRefactoryResolver (ProjectDom dom, ICompilationUnit unit, SupportedLanguage lang, TextEditor editor, string fileName)
		{
			this.unit = unit;
			
			this.dom = dom;
			this.lang   = lang;
			this.editor = editor;
			this.fileName = fileName;
		}
		
		internal void SetupResolver (DomLocation resolvePosition)
		{
			this.resolvePosition = resolvePosition;
			lookupTableVisitor = new LookupTableVisitor (lang);
			
			callingType = GetTypeAtCursor (unit, fileName, resolvePosition);
			
			if (callingType != null) {
				foreach (IMember member in callingType.Members) {
					if (member.Location.Line == resolvePosition.Line || member.BodyRegion.Contains (resolvePosition)) {
						callingMember = member;
						break;
					}
				}
			}
			
			if (callingMember != null) {
				string wrapper = CreateWrapperClassForMember (callingMember);
				
				ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (lang, new StringReader (wrapper));
				parser.Parse ();
				lookupTableVisitor.VisitCompilationUnit (parser.CompilationUnit, null);
			}
		}
		
		void AddContentsFromClassAndMembers (CodeCompletionDataProvider provider)
		{
			IMethod method = callingMember as IMethod;
			if (method != null && method.Parameters != null) {
				foreach (IParameter p in method.Parameters) {
					provider.AddCompletionData (new CodeCompletionData (p.Name, "md-literal"));
				}
			}
			if (CallingType == null)
				return;
			bool isInStatic = CallingMember != null ? CallingMember.IsStatic : false;
			
			if (CallingType.TypeParameters != null) {
				foreach (TypeParameter parameter in CallingType.TypeParameters) {
					provider.AddCompletionData (new CodeCompletionData (parameter.Name, "md-literal"));
				}
			}
			MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector col = new MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector ();
			foreach (IType type in dom.GetInheritanceTree (CallingType)) {
				foreach (IMember member in type.Members) {
//					if (member.IsAccessibleFrom (dom, CallingMember)) {
						col.AddCompletionData (provider, member);
//					}
				}
			}
		}
		
		public void AddAccessibleCodeCompletionData (CodeCompletionDataProvider provider)
		{
			AddContentsFromClassAndMembers (provider);
			
			if (lookupTableVisitor != null && lookupTableVisitor.Variables != null) {
				foreach (KeyValuePair<string, List<LocalLookupVariable>> pair in lookupTableVisitor.Variables) {
					if (pair.Value != null && pair.Value.Count > 0) {
						foreach (LocalLookupVariable v in pair.Value) {
							provider.AddCompletionData (new CodeCompletionData (pair.Key, "md-literal"));
						}
					}
				}
			}
			
			if (CallingMember is IProperty) {
				IProperty property = (IProperty)callingMember;
				if (property.HasSet && property.SetMethod != null && property.SetMethod.BodyRegion.Contains (resolvePosition.Line - 1, editor.CursorColumn - 1)) 
					provider.AddCompletionData (new CodeCompletionData ("value", "md-literal"));
			}
			
			if (CallingMember is IEvent) {
				provider.AddCompletionData (new CodeCompletionData ("value", "md-literal"));
			}
			
			List<string> namespaceList = new List<string> ();
			namespaceList.Add ("");
			if (unit != null && unit.Usings != null) {
				foreach (IUsing u in unit.Usings) {
					if (u.Namespaces == null)
						continue;
					foreach (string ns in u.Namespaces) {
						namespaceList.Add (ns);
					}
				}
			}
			MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector col = new MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector ();
			foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
				col.AddCompletionData (provider, o);
			}
		}
		
		Expression ParseExpression (ExpressionResult expressionResult)
		{
			if (expressionResult == null || String.IsNullOrEmpty (expressionResult.Expression))
				return null;
			string expr = expressionResult.Expression.Trim ();
			System.Console.WriteLine("Parse:" + expr);
			ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (this.lang, new StringReader (expr));
			return parser.ParseExpression();
		}
		
		public ResolveResult Resolve (ExpressionResult expressionResult, DomLocation resolvePosition)
		{
			this.SetupResolver (resolvePosition);
			Expression expr = ParseExpression (expressionResult);
			if (expr == null) {
				System.Console.WriteLine("Can't parse expression");
				return null;
			}
			System.Console.WriteLine("visit:" + expr);
			ResolveVisitor visitor = new ResolveVisitor (this);
			
			ResolveResult result = visitor.Resolve (expr);
			System.Console.WriteLine("resolve result:" + result);
			return result;
		}
		
		public static IReturnType ConvertTypeReference (TypeReference typeRef)
		{
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
			System.Console.WriteLine("converted:" + result + " from:" + typeRef);
			return result;
		}
		
		IReturnType ResolveType (IReturnType type)
		{
			return ResolveType (unit, type);
		}
		
		IReturnType ResolveType (ICompilationUnit unit, IReturnType type)
		{
			SearchTypeResult searchedTypeResult = dom.SearchType (new SearchTypeRequest (unit, -1, -1, type.FullName));
			if (searchedTypeResult != null && searchedTypeResult.Result != null) {
				type.Namespace = searchedTypeResult.Result.Namespace;
				type.Name      = searchedTypeResult.Result.Name;
			}
			return type;
		}
		
		public ResolveResult ResolveLambda (ResolveVisitor visitor, Expression lambdaExpression)
		{
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
				return visitor.CreateResult (varDec.TypeReference);
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
		
		public ResolveResult ResolveIdentifier (ResolveVisitor visitor, string identifier)
		{
			ResolveResult result = null;
			SearchTypeResult searchedTypeResult;
			foreach (KeyValuePair<string, List<LocalLookupVariable>> pair in this.lookupTableVisitor.Variables) {
				if (identifier == pair.Key) {
					LocalLookupVariable var = pair.Value[pair.Value.Count - 1];
					
					IReturnType varType = null;
					if ((var.TypeRef == null || var.TypeRef.Type == "var" || var.TypeRef.IsNull)) {
						if (var.ParentLambdaExpression != null) 
							varType = ResolveLambda (visitor, var.ParentLambdaExpression).ResolvedType;
						if (var.Initializer != null) 
							varType = visitor.Resolve (var.Initializer).ResolvedType;
					} else { 
						varType = ConvertTypeReference (var.TypeRef);
					}
					varType = ResolveType (varType);
					result = new LocalVariableResolveResult (identifier, var.IsLoopVariable);
					result.ResolvedType = varType;
					goto end;
				}
			}
			
			if (this.callingType != null && dom != null) {
				foreach (IType type in dom.GetInheritanceTree (callingType)) {
					List<IMember> members = type.SearchMember (identifier, true);
					if (members != null &&  members.Count > 0) {
						result = new MemberResolveResult (members[0]);
						result.ResolvedType = ResolveType (members[0].ReturnType);
						goto end;
					}
				}
			}
			
			if (this.callingMember != null) {
				if (identifier == "value" && this.callingMember is IProperty) {
					result = new MemberResolveResult (this.callingMember);
					result.ResolvedType = ((IProperty)this.callingMember).ReturnType;
					ResolveType (result.ResolvedType);
					goto end;
				}
				if (this.callingMember is IMethod || this.callingMember is IProperty) {
					ReadOnlyCollection<IParameter> prms = this.callingMember is IMethod ? ((IMethod)this.callingMember).Parameters : ((IProperty)this.callingMember).Parameters;
					if (prms != null) {
						foreach (IParameter para in prms) {
							if (para.Name == identifier) {
								result = new ParameterResolveResult (para);
								result.ResolvedType = para.ReturnType;
								ResolveType (result.ResolvedType);
								goto end;
							}
						}
					}
				}
			}
			
			searchedTypeResult = dom.SearchType (new SearchTypeRequest (unit, -1, -1, identifier));
			if (searchedTypeResult != null) {
				result = new MemberResolveResult (null, true);
				result.ResolvedType = searchedTypeResult.Result;
				goto end;
			}
			
			if (dom.NamespaceExists (identifier, true)) {
				result = new NamespaceResolveResult (identifier);
				goto end;
			}
			
		end:
			if (result != null) {
				result.CallingType   = CallingType;
				result.CallingMember = CallingMember;
			}
			return result;
		}
		
		string CreateWrapperClassForMember (IMember member)
		{
			StringBuilder result = new StringBuilder ();
			int startLine = member.Location.Line;
			int endLine   = member.Location.Line;
			if (!member.BodyRegion.IsEmpty)
				endLine = member.BodyRegion.End.Line;
			result.Append ("class Wrapper {");
			result.Append (this.editor.GetText (this.editor.GetPositionFromLineColumn (startLine, 0),
			                                    this.editor.GetPositionFromLineColumn (endLine, this.editor.GetLineLength (endLine))));
			
			result.Append ("}");
			return result.ToString ();
		}
	}
}
