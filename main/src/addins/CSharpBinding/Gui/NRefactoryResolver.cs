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
	public class NRefactoryResolver
	{
		ProjectDom dom;
		SupportedLanguage lang;
		TextEditor editor;
		IType   callingType;
		IMember callingMember;
		ICompilationUnit unit;
		LookupTableVisitor lookupTableVisitor;
		
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
		
		public static IType GetTypeAtCursor (ProjectDom dom, string fileName, TextEditor editor)
		{
			foreach (IType type in dom.GetTypesFrom (fileName)) {
				if (type.BodyRegion.Contains (editor.CursorLine, editor.CursorColumn))
					return type;
			}
			return null;
		}
		
		public NRefactoryResolver (ProjectDom dom, SupportedLanguage lang, TextEditor editor, string fileName)
		{
			this.lang   = lang;
			this.editor = editor;
			
			lookupTableVisitor = new LookupTableVisitor (lang);
			
			this.dom = dom;
			unit = dom.GetCompilationUnit (fileName);
			callingType = GetTypeAtCursor (dom, fileName, editor);
			
			if (callingType != null) {
				foreach (IMember member in callingType.Members) {
					if (member.BodyRegion.Contains (editor.CursorLine, editor.CursorColumn)) {
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
			
			foreach (IType type in dom.GetInheritanceTree (CallingType)) {
				foreach (IMember member in type.Members) {
//					if (member.IsAccessibleFrom (dom, CallingMember)) {
						MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.AddCompletionData (provider, member);
//					}
				}
			}
		}
		
		public void AddAccessibleCodeCompletionData (CodeCompletionDataProvider provider)
		{
			AddContentsFromClassAndMembers (provider);
			
			foreach (KeyValuePair<string, List<LocalLookupVariable>> pair in lookupTableVisitor.Variables) {
				if (pair.Value != null && pair.Value.Count > 0) {
					foreach (LocalLookupVariable v in pair.Value) {
						provider.AddCompletionData (new CodeCompletionData (pair.Key, "md-literal"));
					}
				}
			}
			if (CallingMember is IProperty) {
				IProperty property = (IProperty)callingMember;
				if (property.HasSet && property.SetMethod.BodyRegion.Contains (editor.CursorLine - 1, editor.CursorColumn - 1)) 
					provider.AddCompletionData (new CodeCompletionData ("value", "md-literal"));
			}
			if (CallingMember is IEvent) {
				provider.AddCompletionData (new CodeCompletionData ("value", "md-literal"));
			}
			foreach (object o in dom.GetNamespaceContents ("", true, true)) {
				MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.AddCompletionData (provider, o);
			}
			if (unit != null && unit.Usings != null) {
				foreach (IUsing u in unit.Usings) {
					if (u.Namespaces == null)
						continue;
					foreach (string ns in u.Namespaces) {
						foreach (object o in dom.GetNamespaceContents (ns, true, true)) {
							MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.AddCompletionData (provider, o);
						}
					}
				}
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
		
		public ResolveResult Resolve (ExpressionResult expressionResult)
		{
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
		
		public static DomReturnType ConvertTypeReference (TypeReference typeRef)
		{
			DomReturnType result = new DomReturnType (typeRef.Type);
			foreach (TypeReference genericArgument in typeRef.GenericTypes) {
				result.AddTypeParameter (ConvertTypeReference (genericArgument));
			}
			return result;
		}
		
		void ResolveType (IReturnType type)
		{
			SearchTypeResult searchedTypeResult = dom.SearchType (new SearchTypeRequest (unit, -1, -1, type.FullName));
			if (searchedTypeResult != null && searchedTypeResult.Result != null) 
				type.FullName = searchedTypeResult.Result.FullName;
		}
		
		public ResolveResult ResolveIdentifier (string identifier)
		{
			ResolveResult result = null;
			SearchTypeResult searchedTypeResult;
			foreach (KeyValuePair<string, List<LocalLookupVariable>> pair in this.lookupTableVisitor.Variables) {
				if (identifier == pair.Key) {
					result = new MemberResolveResult ();
					LocalLookupVariable var = pair.Value[pair.Value.Count - 1];
					DomReturnType varType = ConvertTypeReference (var.TypeRef);
					ResolveType (varType);
					result.ResolvedType = varType;
					goto end;
				}
			}
			
			if (this.callingType != null) {
				foreach (IType type in dom.GetInheritanceTree (callingType)) {
					List<IMember> members = type.SearchMember (identifier, true);
					if (members != null &&  members.Count > 0) {
						result = new MemberResolveResult ();
						result.ResolvedType = members[0].ReturnType;
						goto end;
					}
				}
			}
			
			if (this.callingMember != null) {
				if (identifier == "value" && this.callingMember is IProperty) {
					result = new MemberResolveResult ();
					result.ResolvedType = ((IProperty)this.callingMember).ReturnType;
					goto end;
				}
				if (this.callingMember is IMethod || this.callingMember is IProperty) {
					ReadOnlyCollection<IParameter> prms = this.callingMember is IMethod ? ((IMethod)this.callingMember).Parameters : ((IProperty)this.callingMember).Parameters;
					if (prms != null) {
						foreach (IParameter para in prms) {
							if (para.Name == identifier) {
								result = new MemberResolveResult ();
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
				result = new MemberResolveResult (true);
				result.ResolvedType = searchedTypeResult.Result;
				goto end;
			}
			
			if (dom.NamespaceExists (identifier)) {
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
