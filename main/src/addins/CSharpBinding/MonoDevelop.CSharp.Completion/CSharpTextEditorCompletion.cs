//
// CSharpTextEditorCompletion.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Collections;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeTemplates;

using MonoDevelop.Projects;
using MonoDevelop.Ide.CodeCompletion;

using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp.Project;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.Components;
using Gtk;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	public class CSharpTextEditorCompletion : CompletionTextEditorExtension
	{
		ITypeResolveContext dom;
		DocumentStateTracker<CSharpIndentEngine> stateTracker;
		
		public ITypeResolveContext Dom {
			get { return this.dom; }
			set { this.dom = value; }
		}
	
		public CSharpTextEditorCompletion ()
		{
		}
		
		
		public override void Initialize ()
		{
		}
		
		public ICSharpCode.NRefactory.CSharp.CompilationUnit LanguageAST {
			get;
			set;
		}
		
		
		public override bool ExtendsEditor (MonoDevelop.Ide.Gui.Document doc, IEditableTextBuffer editor)
		{
			return System.IO.Path.GetExtension (doc.Name) == ".cs";
		}
		
		#region Sharing the tracker
		
		void InitTracker ()
		{
			//if there's a CSharpTextEditorIndentation in the extension chain, we can reuse its stateTracker
			CSharpTextEditorIndentation c = this.Document.GetContent<CSharpTextEditorIndentation> ();
			if (c != null && c.StateTracker != null) {
				stateTracker = c.StateTracker;
			} else {
				stateTracker = new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (policy), textEditorData);
			}
		}
		
		internal DocumentStateTracker<CSharpIndentEngine> StateTracker { get { return stateTracker; } }
		
		#endregion
		
		ExpressionResult FindExpression (ITypeResolveContext dom, CodeCompletionContext ctx, int offset)
		{
			NewCSharpExpressionFinder expressionFinder = new NewCSharpExpressionFinder (dom);
			try {
				return expressionFinder.FindExpression (textEditorData, Math.Max (ctx.TriggerOffset + offset, 0));
			} catch (Exception ex) {
				LoggingService.LogWarning (ex.Message, ex);
				return null;
			}
		}
		
		ExpressionResult FindExpression (ITypeResolveContext dom, CodeCompletionContext ctx)
		{
			NewCSharpExpressionFinder expressionFinder = new NewCSharpExpressionFinder (dom);
			try {
				return expressionFinder.FindExpression (textEditorData, ctx.TriggerOffset);
			} catch (Exception ex) {
				LoggingService.LogWarning (ex.Message, ex);
				return null;
			}
		}
		
		internal Document GetDocument ()
		{
			return Document;
		}
		
		bool tryToForceCompletion = false;
		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
//		IDisposable timer = null;
				if (dom == null /*|| Document.CompilationUnit == null*/)
					return null;
				if (completionChar != '#' && stateTracker.Engine.IsInsidePreprocessorDirective)
					return null;
				
				AstLocation location = new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset - 1);
				stateTracker.UpdateEngine ();
				ExpressionResult result;
				int cursor, newCursorOffset = 0, cpos;
				IType resolvedType;
				CodeCompletionContext ctx;
				NRefactoryParameterDataProvider provider;
				
				
				switch (completionChar) {
/* Disabled because it gives problems when declaring arrays - for example string [] should not pop up code completion.
 			case '[':
				if (stateTracker.Engine.IsInsideDocLineComment || stateTracker.Engine.IsInsideOrdinaryCommentOrString)
					return null;
				result = FindExpression (dom, completionContext);
				if (result.ExpressionContext == ExpressionContext.Attribute)
					return CreateCtrlSpaceCompletionData (completionContext, result);
				return null;*/
//			case '\n':
//			case '\r': {
//				if (stateTracker.Engine.IsInsideDocLineComment || stateTracker.Engine.IsInsideOrdinaryCommentOrString)
//					return null;
//				result = FindExpression (dom, completionContext);
//				if (result == null)
//					return null;
//					
//					
//				int tokenIndex = completionContext.TriggerOffset;
//				string token = GetPreviousToken (ref tokenIndex, false);
//				if (result.ExpressionContext == ExpressionContext.ObjectInitializer) {
//					if (token == "{" || token == ",")
//						return CreateCtrlSpaceCompletionData (completionContext, result); 
//				} 
//				return null;
//				}
			return null;
		}
		public bool IsInLinqContext (ExpressionResult result)
		{
			if (result.Contexts == null)
				return false;
			var ctx = (ExpressionContext.LinqContext)result.Contexts.FirstOrDefault (c => c is ExpressionContext.LinqContext);
			if (ctx == null)
				return false;
			int offset = this.textEditorData.Document.LocationToOffset (ctx.Line, ctx.Column);
			return !GetTextWithoutCommentsAndStrings (this.textEditorData.Document, offset, textEditorData.Caret.Offset).Any (p => p.Key == ';');
		}
		
		static IEnumerable<KeyValuePair <char, int>> GetTextWithoutCommentsAndStrings (Mono.TextEditor.Document doc, int start, int end) 
		{
			bool isInString = false, isInChar = false;
			bool isInLineComment = false, isInBlockComment = false;
			
			for (int pos = start; pos < end; pos++) {
				char ch = doc.GetCharAt (pos);
				switch (ch) {
					case '\r':
					case '\n':
						isInLineComment = false;
						break;
					case '/':
						if (isInBlockComment) {
							if (pos > 0 && doc.GetCharAt (pos - 1) == '*') 
								isInBlockComment = false;
						} else  if (!isInString && !isInChar && pos + 1 < doc.Length) {
							char nextChar = doc.GetCharAt (pos + 1);
							if (nextChar == '/')
								isInLineComment = true;
							if (!isInLineComment && nextChar == '*')
								isInBlockComment = true;
						}
						break;
					case '"':
						if (!(isInChar || isInLineComment || isInBlockComment)) 
							isInString = !isInString;
						break;
					case '\'':
						if (!(isInString || isInLineComment || isInBlockComment)) 
							isInChar = !isInChar;
						break;
					default :
						if (!(isInString || isInChar || isInLineComment || isInBlockComment))
							yield return new KeyValuePair<char, int> (ch, pos);
						break;
				}
			}
		}
		
		int GetMemberStartPosition (IMember mem)
		{
			if (mem is IField)
				return textEditorData.Document.LocationToOffset (mem.Location.Line, mem.Location.Column);
			if (mem != null)
				return textEditorData.Document.LocationToOffset (mem.BodyRegion.BeginLine, mem.BodyRegion.BeginColumn);
			return 0;
		}

		public ICSharpCode.OldNRefactory.Ast.CompilationUnit ParsedUnit { get; set; }
		NRefactoryResolver CreateResolver ()
		{
			NRefactoryResolver result = new NRefactoryResolver (dom, Document.CompilationUnit, ICSharpCode.OldNRefactory.SupportedLanguage.CSharp, textEditorData, Document.FileName);
			if (ParsedUnit != null)
				result.SetupParsedCompilationUnit (ParsedUnit);
			return result;
		}
		
		public override IParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext, char completionChar)
		{
			//AstLocation location = new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset - 2);
			NRefactoryResolver resolver = CreateResolver ();
			if (result.ExpressionContext is ExpressionContext.TypeExpressionContext)
				result.ExpressionContext = new NewCSharpExpressionFinder (dom).FindExactContextForNewCompletion (textEditorData, Document.CompilationUnit, Document.FileName, resolver.CallingType) ?? result.ExpressionContext;
			
			switch (completionChar) {
			 }
			return null;
		}
		
		/// <summary>
		/// Adds a type to completion list. If it's a simple type like System.String it adds the simple
		/// C# type name "string" as well.
		/// </summary>
		static void AddAsCompletionData (CompletionDataCollector col, IType type)
		{
			if (type == null)
				return;
			string netName = CSharpAmbience.NetToCSharpTypeName (type.FullName);
			if (!string.IsNullOrEmpty (netName) && netName != type.FullName)
				col.Add (netName);
			
			if (!String.IsNullOrEmpty (type.Namespace) && !col.IsNamespaceInScope (type.Namespace)) {
				string[] ns = type.Namespace.Split ('.');
				for (int i = 0; i < ns.Length; i++) {
					col.Add (new Namespace (ns[i]));
					if (!col.IsNamespaceInScope (ns[i]))
						return;
				}
			}
			
			col.Add (type);
		}

		ICompletionDataList CreateCompletionData (AstLocation location, ResolveResult resolveResult, 
		                                          ExpressionResult expressionResult, NRefactoryResolver resolver)
		{
			if (resolveResult == null || expressionResult == null || dom == null)
				return null;
			CompletionDataList result = new ProjectDomCompletionDataList ();
			IEnumerable<object> objects = resolveResult.CreateResolveResult (dom, resolver != null ? resolver.CallingMember : null);
			CompletionDataCollector col = new CompletionDataCollector (this, dom, result, Document.CompilationUnit, resolver != null ? resolver.CallingType : null, location);
			col.HideExtensionParameter = !resolveResult.StaticResolve;
			col.NamePrefix = expressionResult.Expression;
			bool showOnlyTypes = expressionResult.Contexts.Any (ctx => ctx == ExpressionContext.InheritableType || ctx == ExpressionContext.Constraints);
			if (objects != null) {
				foreach (object obj in objects) {
					if (expressionResult.ExpressionContext != null && expressionResult.ExpressionContext.FilterEntry (obj))
						continue;
					if (expressionResult.ExpressionContext == ExpressionContext.NamespaceNameExcepted && !(obj is Namespace))
						continue;
					if (showOnlyTypes && !(obj is IType))
						continue;
					CompletionData data = col.Add (obj);
					if (data != null && expressionResult.ExpressionContext == ExpressionContext.Attribute && data.CompletionText != null && data.CompletionText.EndsWith ("Attribute")) {
						string newText = data.CompletionText.Substring (0, data.CompletionText.Length - "Attribute".Length);
						data.SetText (newText);
					}
				}
			}
			
			return result;
		}

		
		static string StripGenerics (string str)
		{
			int idx = str.IndexOf ('<');
			if (idx > 0)
				return str.Substring (0, idx);
			return str;
		}
		
		
		static bool ContainsDeclaration (IType type, IMethod method)
		{
			return GetDeclaration (type, method) != null;
		}
		
		static IMethod GetDeclaration (IType type, IMethod method)
		{
			foreach (IMethod cur in type.Methods) {
				if (cur.Name == method.Name && cur.Parameters.Count == method.Parameters.Count) {
					bool equal = true;
					for (int i = 0; i < cur.Parameters.Count; i++) {
						if (cur.Parameters[i].ReturnType.ToInvariantString () != method.Parameters[i].ReturnType.ToInvariantString ()) {
							equal = false;
							break;
						}
					}
					if (equal)
						return cur;
				}
			}
			return null;
		}
		
		CompletionDataList GetPartialCompletionData (CodeCompletionContext ctx, IType type, string modifiers)
		{
			CompletionDataList result = new ProjectDomCompletionDataList ();

			CompoundType partialType = dom.GetType (type.FullName) as CompoundType;
			if (partialType != null) {
				List<IMethod> methods = new List<IMethod> ();
				// gather all partial methods without implementation
				foreach (IType part in partialType.Parts) {
					if (part.Location == type.Location && part.GetDefinition ().Region.FileName == type.GetDefinition ().Region.FileName)
						continue;
					foreach (IMethod method in part.Methods) {
						if (method.IsPartial && method.BodyRegion.EndLine == 0 && !ContainsDeclaration (type, method)) {
							methods.Add (method);
						}
					}
				}

				// now filter all methods that are implemented in the compound class
				foreach (IType part in partialType.Parts) {
					if (part.Location == type.Location && part.GetDefinition ().Region.FileName == type.GetDefinition ().Region.FileName)
						continue;
					for (int i = 0; i < methods.Count; i++) {
						IMethod curMethod = methods[i];
						IMethod method = GetDeclaration (part, curMethod);
						if (method != null && method.BodyRegion.EndLine != 0) {
							methods.RemoveAt (i);
							i--;
							continue;
						}
					}
				}

				foreach (IMethod method in methods) {
					NewOverrideCompletionData data = new NewOverrideCompletionData (dom, textEditorData, ctx.TriggerOffset, type, method);
					data.GenerateBody = false;
					result.Add (data);
				}
				
			}
			return result;
		}
		
	
		static void AddNRefactoryKeywords (CompletionDataCollector col, System.Collections.BitArray keywords)
		{
			for (int i = 0; i < keywords.Length; i++) {
				if (keywords[i]) {
					string keyword = ICSharpCode.OldNRefactory.Parser.CSharp.Tokens.GetTokenString (i);
					if (keyword.IndexOf ('<') >= 0)
						continue;
					col.Add (keyword, "md-keyword");
				}
			}
		}
		
		CompletionDataList CreateCtrlSpaceCompletionData (CodeCompletionContext ctx, ExpressionResult expressionResult)
		{
			NRefactoryResolver resolver = CreateResolver ();
			AstLocation cursorLocation = new AstLocation (ctx.TriggerLine, ctx.TriggerLineOffset);
			resolver.SetupResolver (cursorLocation);
			CompletionDataList result = new ProjectDomCompletionDataList ();
			CompletionDataCollector col = new CompletionDataCollector (this, dom, result, Document.CompilationUnit, resolver.CallingType, cursorLocation);
			
			if (expressionResult == null) {
				AddPrimitiveTypes (col);
				resolver.AddAccessibleCodeCompletionData (ExpressionContext.Global, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.TypeDeclaration) {
				AddPrimitiveTypes (col);
				AddNRefactoryKeywords (col, ICSharpCode.OldNRefactory.Parser.CSharp.Tokens.TypeLevel);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.InterfaceDeclaration) {
				AddPrimitiveTypes (col);
				AddNRefactoryKeywords (col, ICSharpCode.OldNRefactory.Parser.CSharp.Tokens.InterfaceLevel);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.MethodBody) {
				col.Add ("global", "md-keyword");
				col.Add ("var", "md-keyword");
				AddNRefactoryKeywords (col, ICSharpCode.OldNRefactory.Parser.CSharp.Tokens.StatementStart);
				AddPrimitiveTypes (col);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.InterfacePropertyDeclaration) {
				col.Add ("get", "md-keyword");
				col.Add ("set", "md-keyword");
			} else if (expressionResult.ExpressionContext == ExpressionContext.ConstraintsStart) {
				col.Add ("where", "md-keyword");
			} else if (expressionResult.ExpressionContext == ExpressionContext.Constraints) {
				col.Add ("new", "md-keyword");
				col.Add ("class", "md-keyword");
				col.Add ("struct", "md-keyword");
				AddPrimitiveTypes (col);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.Attribute) {
				col.Add ("assembly", "md-keyword");
				col.Add ("module", "md-keyword");
				col.Add ("type", "md-keyword");
				col.Add ("method", "md-keyword");
				col.Add ("field", "md-keyword");
				col.Add ("property", "md-keyword");
				col.Add ("event", "md-keyword");
				col.Add ("param", "md-keyword");
				col.Add ("return", "md-keyword");
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.BaseConstructorCall) {
				col.Add ("this", "md-keyword");
				col.Add ("base", "md-keyword");
			} else if (expressionResult.ExpressionContext == ExpressionContext.ParameterType || expressionResult.ExpressionContext == ExpressionContext.FirstParameterType) {
				col.Add ("ref", "md-keyword");
				col.Add ("out", "md-keyword");
				col.Add ("params", "md-keyword");
				// C# 3.0 extension method
				if (expressionResult.ExpressionContext == ExpressionContext.FirstParameterType)
					col.Add ("this", "md-keyword");
				AddPrimitiveTypes (col);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.PropertyDeclaration) {
				AddNRefactoryKeywords (col, ICSharpCode.OldNRefactory.Parser.CSharp.Tokens.InPropertyDeclaration);
			} else if (expressionResult.ExpressionContext == ExpressionContext.EventDeclaration) {
				col.Add ("add", "md-keyword");
				col.Add ("remove", "md-keyword");
			} //else if (expressionResult.ExpressionContext == ExpressionContext.FullyQualifiedType) {} 
			else if (expressionResult.ExpressionContext == ExpressionContext.Default) {
				col.Add ("global", "md-keyword");
				col.Add ("var", "md-keyword");
				AddPrimitiveTypes (col);
				AddNRefactoryKeywords (col, ICSharpCode.OldNRefactory.Parser.CSharp.Tokens.ExpressionStart);
				AddNRefactoryKeywords (col, ICSharpCode.OldNRefactory.Parser.CSharp.Tokens.ExpressionContent);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.Global) {
				AddNRefactoryKeywords (col, ICSharpCode.OldNRefactory.Parser.CSharp.Tokens.GlobalLevel);
			} else if (expressionResult.ExpressionContext == ExpressionContext.ObjectInitializer) {
				ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForObjectInitializer (textEditorData, resolver.Unit, Document.FileName, resolver.CallingType);
				if (exactContext is ExpressionContext.TypeExpressionContext) {
					IReturnType objectInitializer = ((ExpressionContext.TypeExpressionContext)exactContext).UnresolvedType;
					if (objectInitializer.ArrayDimensions > 0 || objectInitializer.PointerNestingLevel > 0) {
						col.Add ("global", "md-keyword");
						col.Add ("new", "md-keyword");
						AddPrimitiveTypes (col);
						resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
						return result;
					}
						
					IType foundType = resolver.SearchType (objectInitializer);
					if (foundType == null)
						foundType = dom.GetType (objectInitializer);
					
					if (foundType != null) {
						bool includeProtected = DomType.IncludeProtected (dom, foundType, resolver.CallingType);
						foreach (IType type in dom.GetInheritanceTree (foundType)) {
							foreach (IProperty property in type.Properties) {
								if (property.IsAccessibleFrom (dom, resolver.CallingType, resolver.CallingMember, includeProtected)) {
									col.Add (property);
								}
							}
							foreach (var field in type.Fields) {
								if (field.IsAccessibleFrom (dom, resolver.CallingType, resolver.CallingMember, includeProtected)) {
									col.Add (field);
								}
							}
						}
					}
				}
//				result.Add ("global", "md-literal");
//				AddPrimitiveTypes (result);
//				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.AttributeArguments) {
				col.Add ("global", "md-keyword");
				AddPrimitiveTypes (col);
				string attributeName = NewCSharpExpressionFinder.FindAttributeName (textEditorData, Document.CompilationUnit, Document.FileName);
				if (attributeName != null) {
					IType type = resolver.SearchType (attributeName + "Attribute");
					if (type == null) 
						type = resolver.SearchType (attributeName);
					if (type != null) {
						foreach (var property in type.Properties) {
							col.Add (property);
						}
						foreach (var field in type.Fields) {
							if (field.IsPublic)
								col.Add (field);
						}
					}
				}
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.IdentifierExpected) {
				if (!string.IsNullOrEmpty (expressionResult.Expression))
					expressionResult.Expression = expressionResult.Expression.Trim ();
				MemberResolveResult resolveResult = resolver.Resolve (expressionResult, cursorLocation) as MemberResolveResult;
				if (resolveResult != null && resolveResult.ResolvedMember == null && resolveResult.ResolvedType != null) {
					string name = CSharpAmbience.NetToCSharpTypeName (resolveResult.ResolvedType.FullName);
					if (name != resolveResult.ResolvedType.FullName) {
						col.Add (Char.ToLower (name[0]).ToString (), "md-field");
					} else {
					}
				} else {
					col.Add ("global", "md-keyword");
					AddPrimitiveTypes (col);
					resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
					if (expressionResult.ExpressionContext == ExpressionContext.Constraints) {
						col.Add ("struct", "md-keyword");
						col.Add ("class", "md-keyword");
						col.Add ("new()", "md-keyword");
					} else {
						col.Add ("var", "md-keyword");
					}
				}
			} else if (expressionResult.ExpressionContext == ExpressionContext.TypeName) {
				col.Add ("global", "md-keyword");
				AddPrimitiveTypes (col);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.ForeachInToken) {
				col.Add ("in", "md-keyword");
			} else {
				col.Add ("global", "md-keyword");
				col.Add ("var", "md-keyword");
				AddPrimitiveTypes (col);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			}
			
			if (resolver.CallingMember is IMethod) {
				foreach (ITypeParameter tp in ((IMethod)resolver.CallingMember).TypeParameters) {
					col.Add (tp.Name, "md-keyword");
				}
			}
			
			return result;
		}
		
		#region case completion
		ICompletionDataList CreateCaseCompletionData (AstLocation location, ExpressionResult expressionResult)
		{
			NRefactoryResolver resolver = CreateResolver ();
			
			resolver.SetupResolver (location);
			
			SwitchFinder switchFinder = new SwitchFinder (location);
			if (resolver.MemberCompilationUnit != null)
				switchFinder.VisitCompilationUnit (resolver.MemberCompilationUnit, null);
			CompletionDataList result = new ProjectDomCompletionDataList ();
			if (switchFinder.SwitchStatement == null)
				return result;
			ResolveResult resolveResult = resolver.ResolveExpression (switchFinder.SwitchStatement.SwitchExpression, location);
			IType type = dom.GetType (resolveResult.ResolvedType);
			if (type != null && type.ClassType == ClassType.Enum) {
				OutputFlags flags = OutputFlags.None;
				var declaringType = resolver.CallingType;
				if (declaringType != null && dom != null) {
					foreach (IType t in new List<IType>(dom.GetInheritanceTree (declaringType))) {
						if (t.SearchMember (type.Name, true).Any (m => m.MemberType != MonoDevelop.Projects.Dom.MemberType.Type)) {
							flags |= OutputFlags.UseFullName;
							break;
						}
					}
				}
//				if (!foundType && (NamePrefix.Length == 0 || !type.Namespace.StartsWith (NamePrefix)) && !type.Namespace.EndsWith ("." + NamePrefix) && type.DeclaringType == null && NamePrefix != null && !NamePrefix.Contains ("::"))
//					flags |= OutputFlags.UseFullName;
				CompletionDataCollector cdc = new CompletionDataCollector (this, dom, result, Document.CompilationUnit, resolver.CallingType, location);
				cdc.Add (type, flags);
			}
			return result;
		}
		
		class SwitchFinder : ICSharpCode.OldNRefactory.Visitors.AbstractAstVisitor
		{
			
			ICSharpCode.OldNRefactory.Ast.SwitchStatement switchStatement = null;
			
			public ICSharpCode.OldNRefactory.Ast.SwitchStatement SwitchStatement {
				get {
					return this.switchStatement;
				}
			}
			
			public SwitchFinder (AstLocation location)
			{
				//this.location = new ICSharpCode.OldNRefactory.Location (location.Column, location.Line);
			}
			
			public override object VisitSwitchStatement (ICSharpCode.OldNRefactory.Ast.SwitchStatement switchStatement, object data)
			{
//				if (switchStatement.StartLocation < caretLocation && caretLocation < switchStatement.EndLocation)
					this.switchStatement = switchStatement;
				return base.VisitSwitchStatement(switchStatement, data);
			}

		}
		#endregion
	
	}
}
