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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom.Parser;

using CSharpBinding;
using CSharpBinding.FormattingStrategy;
using CSharpBinding.Parser;

namespace MonoDevelop.CSharpBinding.Gui
{
	public class CSharpTextEditorCompletion : CompletionTextEditorExtension
	{
		ProjectDom dom;
		DocumentStateTracker<CSharpIndentEngine> stateTracker;
		DomLocation location;
		
		public CSharpTextEditorCompletion ()
		{
		}
		
		public CSharpTextEditorCompletion (Document doc)
		{
			Initialize (doc);
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			InitTracker ();
			dom = ProjectDomService.GetProjectDom (Document.Project);
			if (dom == null)
				dom = ProjectDomService.GetFileDom (Document.FileName);
		}
		
		public override bool ExtendsEditor (MonoDevelop.Ide.Gui.Document doc, IEditableTextBuffer editor)
		{
			return System.IO.Path.GetExtension (doc.Title) == ".cs";
		}
		
		#region Sharing the tracker
		
		void InitTracker ()
		{
			//if there's a CSharpTextEditorIndentation in the extension chain, we can reuse its stateTracker
			CSharpTextEditorIndentation c = this.Document.GetContent<CSharpTextEditorIndentation> ();
			if (c != null && c.StateTracker != null) {
				stateTracker = c.StateTracker;
			} else {
				stateTracker = new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (), Editor);
			}
		}
		
		internal DocumentStateTracker<CSharpIndentEngine> StateTracker { get { return stateTracker; } }
		
		#endregion
		
		ExpressionResult FindExpression (ProjectDom dom, int offset)
		{
			NewCSharpExpressionFinder expressionFinder = new NewCSharpExpressionFinder (dom);
			try {
				return expressionFinder.FindExpression (Editor.Text, Editor.CursorPosition + offset);
			} catch (Exception ex) {
				LoggingService.LogWarning (ex.Message, ex);
				return null;
			}
		}
		
		ExpressionResult FindFullExpression (ProjectDom dom, int offset)
		{
			NewCSharpExpressionFinder expressionFinder = new NewCSharpExpressionFinder (dom);
			try {
				return expressionFinder.FindFullExpression (Editor.Text, Editor.CursorPosition + offset);
			} catch (Exception ex) {
				LoggingService.LogWarning (ex.Message, ex);
				return null;
			}
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
		try {
			if (dom == null || Document.CompilationUnit == null)
				return null;
			if (completionChar != '#' && stateTracker.Engine.IsInsidePreprocessorDirective)
				return null;
			location = new DomLocation (Editor.CursorLine - 1, Editor.CursorColumn - 1);
			stateTracker.UpdateEngine ();
			ExpressionResult result;
			int cursor, newCursorOffset = 0;
			switch (completionChar) {
			case ':':
			case '.':
				result = FindExpression (dom, 0);
				if (result == null || result.Expression == null)
					return null;
				int idx = result.Expression.LastIndexOf ('.');
				if (idx > 0)
					result.Expression = result.Expression.Substring (0, idx);
				
				NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom,
				                                                                                Document.CompilationUnit,
				                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
				                                                                                Editor,
				                                                                                Document.FileName);
				ResolveResult resolveResult = resolver.Resolve (result, new DomLocation (Editor.CursorLine, Editor.CursorColumn));
				if (resolver.ResolvedExpression is ICSharpCode.NRefactory.Ast.PrimitiveExpression)
					return null;
				return CreateCompletionData (resolveResult, result, resolver);
			case '#':
				if (stateTracker.Engine.IsInsidePreprocessorDirective) 
					return GetDirectiveCompletionData ();
				return null;
			case '>':
				cursor = Editor.SelectionStartPosition;
				
				if (stateTracker.Engine.IsInsideDocLineComment) {
					string lineText = Editor.GetLineText (Editor.CursorLine);
					int startIndex = Math.Min (Editor.CursorColumn - 1, lineText.Length - 1);
					
					while (startIndex >= 0 && lineText[startIndex] != '<') {
						--startIndex;
						if (lineText[startIndex] == '/') { // already closed.
							startIndex = -1;
							break;
						}
					}
					
					if (startIndex >= 0) {
						int endIndex = startIndex;
						while (endIndex <= Editor.CursorColumn && endIndex < lineText.Length && !Char.IsWhiteSpace (lineText[endIndex])) {
							endIndex++;
						}
						string tag = endIndex - startIndex - 1 > 0 ? lineText.Substring (startIndex + 1, endIndex - startIndex - 2) : null;
						if (!String.IsNullOrEmpty (tag) && commentTags.IndexOf (tag) >= 0) {
							Editor.InsertText (cursor, "</" + tag + ">");
							Editor.CursorPosition = cursor; 
							return null;
						}
					}
				}
				return null;
			case '[':
				result = FindExpression (dom, 0);
				if (result.ExpressionContext == ExpressionContext.Attribute)
					return CreateCtrlSpaceCompletionData (result);
				return null;
			case '<':
				if (stateTracker.Engine.IsInsideDocLineComment) 
					return GetXmlDocumentationCompletionData ();
				return null;
			case '(':
				result = FindExpression (dom, -1);
				if (result == null || result.Expression == null)
					return null;
				resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom, Document.CompilationUnit,
				                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
				                                                                                Editor,
				                                                                                Document.FileName);
				resolveResult = resolver.Resolve (result, new DomLocation (Editor.CursorLine, Editor.CursorColumn - 1));
				
				if (resolveResult != null && resolver.ResolvedExpression is ICSharpCode.NRefactory.Ast.TypeOfExpression) {
					CodeCompletionDataProvider provider = new CodeCompletionDataProvider (null, GetAmbience ());
					List<string> namespaceList = new List<string> ();
					namespaceList.Add ("");
					if (Document.CompilationUnit != null && Document.CompilationUnit.Usings != null) {
						foreach (IUsing u in Document.CompilationUnit.Usings) {
							if (u.Namespaces == null)
								continue;
							foreach (string ns in u.Namespaces) {
								namespaceList.Add (ns);
							}
						}
					}
					MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector col = new MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector (Editor, dom, Document.CompilationUnit, location);
					foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
						col.AddCompletionData (provider, o);
					}
					return provider;
				}
				return null;
			case '/':
				cursor = Editor.SelectionStartPosition;
				if (cursor < 2)
					break;
					
				if (stateTracker.Engine.IsInsideDocLineComment) {
					string lineText = Editor.GetLineText (Editor.CursorLine);
					bool startsDocComment = true;
					int slashes = 0;
					for (int i = 0; i < Editor.CursorColumn && i < lineText.Length; i++) {
						if (lineText[i] == '/') {
							slashes++;
							continue;
						}
						if (!Char.IsWhiteSpace (lineText[i])) {
							startsDocComment = false;
							break;
						}
					}
					if (!startsDocComment || slashes != 3)
						break;
					StringBuilder generatedComment = new StringBuilder ();
					bool generateStandardComment = true;
					IType insideClass = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new DomLocation (Editor.CursorLine, Editor.CursorColumn));
					if (insideClass != null) {
						string indent = GetLineWhiteSpace (lineText);
						if (insideClass.ClassType == ClassType.Delegate) {
							AppendSummary (generatedComment, indent, out newCursorOffset);
							IMethod m = null;
							foreach (IMethod method in insideClass.Methods)
								m = method;
							AppendMethodComment (generatedComment, indent, m);
							generateStandardComment = false;
						} else {
							if (!IsInsideClassBody (insideClass, Editor.CursorLine, Editor.CursorColumn))
								break;
							string body = GenerateBody (insideClass, Editor.CursorLine, indent, out newCursorOffset);
							if (!String.IsNullOrEmpty (body)) {
								generatedComment.Append (body);
								generateStandardComment = false;
							}
						}
					}
					if (generateStandardComment) {
						string indent = GetLineWhiteSpace (Editor.GetLineText (Editor.CursorLine));
						AppendSummary (generatedComment, indent, out newCursorOffset);
					}
					
					Editor.InsertText (cursor, generatedComment.ToString ());
					Editor.CursorPosition = cursor + newCursorOffset;
					return null;
				}
				return null;
			case ' ':
				result = FindExpression (dom, 0);
				if (result == null)
					return null;
				
				int i = completionContext.TriggerOffset;
				string token = GetPreviousToken (ref i, false);
				if (token == "=" || token == "==") {
					result = FindExpression (dom, i - completionContext.TriggerOffset - 1);
					resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom,
						                                                         Document.CompilationUnit,
						                                                         ICSharpCode.NRefactory.SupportedLanguage.CSharp,
						                                                         Editor,
						                                                         Document.FileName);
					
					resolveResult = resolver.Resolve (result, new DomLocation (Editor.CursorLine, Editor.CursorColumn));
					if (resolveResult != null) {
						IType resolvedType = dom.GetType (resolveResult.ResolvedType);
						if (resolvedType != null && resolvedType.ClassType == ClassType.Enum) {
							CodeCompletionDataProvider provider = new CodeCompletionDataProvider (null, GetAmbience ());
							CompletionDataCollector cdc = new CompletionDataCollector (Editor, dom, Document.CompilationUnit, location);
							cdc.AddCompletionData (provider, resolvedType);
							
	//						foreach (IField field in resolvedType.Fields) {
	//							cdc.AddCompletionData (provider, field);
	//						}
							return provider;
						}
					}
					return null;
				}
				return HandleKeywordCompletion (result, i, token);
			default:
				if (Char.IsLetter (completionChar) && !stateTracker.Engine.IsInsideDocLineComment && !stateTracker.Engine.IsInsideOrdinaryCommentOrString) {
					char prevCh = Editor.CursorPosition > 2 ? Editor.GetCharAt (Editor.CursorPosition - 2) : '\0';
					if (prevCh != '.' && !Char.IsLetterOrDigit (prevCh)) {
						NewCSharpExpressionFinder expressionFinder = new NewCSharpExpressionFinder (dom);
						try {
							result = expressionFinder.FindExpression (Editor.Text, Math.Max (Editor.CursorPosition - 1, 0));
						} catch (Exception ex) {
							LoggingService.LogWarning (ex.Message, ex);
							result = null;
						}
						if (result == null)
							return null;
						
						if (result.ExpressionContext != ExpressionContext.IdentifierExpected) {
							triggerWordLength = 1;
							return CreateCtrlSpaceCompletionData (result);
						}
					}
				}
				break;
			}
			} catch (Exception e) {
				System.Console.WriteLine("cce: " +e);
			}
			return null;
		}
		
		public override IParameterDataProvider HandleParameterCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			if (dom == null)
				return null;
			ExpressionResult result = FindExpression (dom , -1);
			if (result == null)
				return null;
			if (result.ExpressionContext is ExpressionContext.TypeExpressionContext)
				result.ExpressionContext = new NewCSharpExpressionFinder (dom).FindExactContextForNewCompletion(Editor, Document.CompilationUnit, Document.FileName) ?? result.ExpressionContext;
				
			location = new DomLocation (Editor.CursorLine - 1, Editor.CursorColumn - 1);
			NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom, Document.CompilationUnit,
			                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
			                                                                                Editor,
			                                                                                Document.FileName);
			switch (completionChar) {
			case '(':
				ResolveResult resolveResult = resolver.Resolve (result, new DomLocation (Editor.CursorLine, Editor.CursorColumn));
				if (result.ExpressionContext is ExpressionContext.TypeExpressionContext) {
					IReturnType returnType = ((ExpressionContext.TypeExpressionContext)result.ExpressionContext).Type;
					IType type = dom.SearchType (new SearchTypeRequest (resolver.Unit, returnType));
					if (type != null && returnType.GenericArguments != null)
						type = DomType.CreateInstantiatedGenericType (type, returnType.GenericArguments);
					return new NRefactoryParameterDataProvider (Editor, resolver, type);
				}
				if (resolveResult != null) {
					if (resolveResult is MethodResolveResult)
						return new NRefactoryParameterDataProvider (Editor, resolver, resolveResult as MethodResolveResult);
					if (result.ExpressionContext == ExpressionContext.BaseConstructorCall) {
						if (resolveResult is ThisResolveResult)
							return new NRefactoryParameterDataProvider (Editor, resolver, resolveResult as ThisResolveResult);
						if (resolveResult is BaseResolveResult)
							return new NRefactoryParameterDataProvider (Editor, resolver, resolveResult as BaseResolveResult);
					}
					IType resolvedType = dom.SearchType (new SearchTypeRequest (resolver.Unit, resolveResult.ResolvedType));
					if (resolvedType != null && resolvedType.ClassType == ClassType.Delegate) {
						return new NRefactoryParameterDataProvider (Editor, resolver, result.Expression, resolvedType);
					}
				}
				break;
			}
			return null;
		}
		
		public ICompletionDataProvider HandleKeywordCompletion (ExpressionResult result, int wordStart, string word)
		{
			if (stateTracker.Engine.IsInsideDocLineComment || stateTracker.Engine.IsInsideOrdinaryCommentOrString)
				return null;
				
			location = new DomLocation (Editor.CursorLine - 1, Editor.CursorColumn - 1);
			switch (word) {
			case "namespace":
			case "using":
				result.ExpressionContext = ExpressionContext.NamespaceNameExcepted;
				return CreateCompletionData (new NamespaceResolveResult (""), result, null);
			case "case":
				return CreateCaseCompletionData (result);
			case ",":
			case ":":
				if (result.ExpressionContext == ExpressionContext.InheritableType) {
					IType cls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new DomLocation (Editor.CursorLine, Editor.CursorColumn));
					CodeCompletionDataProvider provider = new CodeCompletionDataProvider (null, GetAmbience ());
					List<string> namespaceList = new List<string> ();
					namespaceList.Add ("");
					if (Document.CompilationUnit != null && Document.CompilationUnit.Usings != null) {
						foreach (IUsing u in Document.CompilationUnit.Usings) {
							if (u.Namespaces == null)
								continue;
							foreach (string ns in u.Namespaces) {
								namespaceList.Add (ns);
							}
						}
					}
					MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector col = new MonoDevelop.CSharpBinding.Gui.CSharpTextEditorCompletion.CompletionDataCollector (Editor, dom, Document.CompilationUnit, location);
					foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
						if (cls != null && o is IType && ((IType)o).Name == cls.Name) {
							continue;
						}
						col.AddCompletionData (provider, o);
					}
					return provider;
					
				}
				break;
			case "is":
			case "as": {
				ExpressionResult expressionResult = FindExpression (dom, -3);
				NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom, 
				                                                                                Document.CompilationUnit,
				                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
				                                                                                Editor,
				                                                                                Document.FileName);
				ResolveResult resolveResult = resolver.Resolve (expressionResult, new DomLocation (Editor.CursorLine, Editor.CursorColumn));
				if (resolveResult != null && resolveResult.ResolvedType != null) {
					CodeCompletionDataProvider provider = new CodeCompletionDataProvider (null, GetAmbience ());
					CompletionDataCollector col = new CompletionDataCollector (Editor, dom, Document.CompilationUnit, location);
					foreach (IType type in dom.GetSubclasses (dom.SearchType (new SearchTypeRequest (resolver.Unit, resolveResult.ResolvedType)))) {
						col.AddCompletionData (provider, type);
					}
					List<string> namespaceList = new List<string> ();
					namespaceList.Add ("");
					if (resolver.Unit != null && resolver.Unit.Usings != null) {
						foreach (IUsing u in resolver.Unit.Usings) {
							if (u.Namespaces == null)
								continue;
							foreach (string ns in u.Namespaces) {
								namespaceList.Add (ns);
							}
						}
					}
					foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
						if (o is IType) {
							IType type = (IType)o;
							if (type.ClassType != ClassType.Interface)
								continue;
						}
						col.AddCompletionData (provider, o);
					}
					
					return provider;
				}
				result.ExpressionContext = ExpressionContext.Type;
				return CreateCtrlSpaceCompletionData (result);
			}
			case "override":
				// Look for modifiers, in order to find the beginning of the declaration
				int firstMod = wordStart;
				int i        = wordStart;
				for (int n=0; n<3; n++) {
					string mod = GetPreviousToken (ref i, true);
					if (mod == "public" || mod == "protected" || mod == "private" || mod == "internal" || mod == "sealed") {
						firstMod = i;
					} else if (mod == "static") {
						// static methods are not overridable
						return null;
					} else
						break;
				}
				IType cls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new DomLocation (Editor.CursorLine, Editor.CursorColumn));
				if (cls != null && (cls.ClassType == ClassType.Class || cls.ClassType == ClassType.Struct)) {
					string modifiers = Editor.GetText (firstMod, wordStart);
					return GetOverrideCompletionData (cls, modifiers);
				}
				return null;
			case "new":
				ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForNewCompletion (Editor, Document.CompilationUnit, Document.FileName);
				
				if (exactContext is ExpressionContext.TypeExpressionContext)
					return CreateTypeCompletionData (exactContext, ((ExpressionContext.TypeExpressionContext)exactContext).Type);
				return CreateTypeCompletionData (exactContext, null);
			case "if":
			case "elif":
				if (stateTracker.Engine.IsInsidePreprocessorDirective) 
					return GetDefineCompletionData ();
				return null;
			}
			return null;
		}
		
		string GetPreviousToken (ref int i, bool allowLineChange)
		{
			char c;
			
			if (i <= 0)
				return null;
			
			do {
				c = Editor.GetCharAt (--i);
			} while (i > 0 && char.IsWhiteSpace (c) && (allowLineChange ? true : c != '\n'));
			
			if (i == 0)
				return null;
			
			if (!char.IsLetterOrDigit (c))
				return new string (c, 1);
			
			int endOffset = i + 1;
			
			do {
				c = Editor.GetCharAt (i - 1);
				if (!(char.IsLetterOrDigit (c) || c == '_'))
					break;
				
				i--;
			} while (i > 0);
			
			return Editor.GetText (i, endOffset);
		}
		
		public override ICompletionDataProvider CodeCompletionCommand (ICodeCompletionContext completionContext)
		{
			if (stateTracker.Engine.IsInsidePreprocessorDirective || stateTracker.Engine.IsInsideOrdinaryCommentOrString || stateTracker.Engine.IsInsideDocLineComment)
				return null;
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetText (pos - 1, pos);
			if (txt.Length > 0) {
				int triggerWordLength = 0; 
				ICompletionDataProvider cp = this.HandleCodeCompletion (completionContext, txt[0], ref triggerWordLength);
				if (cp != null) {
					((CodeCompletionDataProvider)cp).AutoCompleteUniqueMatch = true;
					return cp;
				}
			}

			NewCSharpExpressionFinder expressionFinder = new NewCSharpExpressionFinder (dom);
			ExpressionResult result;
			try {
				result = expressionFinder.FindExpression (Editor.Text, Editor.CursorPosition);
			} catch (Exception ex) {
				LoggingService.LogWarning (ex.Message, ex);
				result = null;
			}
			
			if (result == null)
				return null;
			
			CodeCompletionDataProvider provider;
/*			if (result.ExpressionContext == ExpressionContext.IdentifierExpected) {
				NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom, Document.CompilationUnit,
				                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
				                                                                                Editor,
				                                                                                Document.FileName);
				
				ResolveResult resolveResult = resolver.Resolve (result, new DomLocation (Editor.CursorLine, Editor.CursorColumn));
				provider = (CodeCompletionDataProvider)CreateCompletionData (resolveResult, result, resolver);
			} else {
			}*/
			provider = CreateCtrlSpaceCompletionData (result);
			if (provider != null)
				provider.AutoCompleteUniqueMatch = true;
			return provider;
		}
		
		public class CompletionDataCollector
		{
			Dictionary<string, MemberCompletionData> data = new Dictionary<string, MemberCompletionData> ();
			Dictionary<string, bool> namespaces = new Dictionary<string,bool> ();
			internal static CSharpAmbience ambience = new CSharpAmbience ();
			ProjectDom dom;
			ICompilationUnit unit;
			DomLocation location;
			TextEditor editor;
			
			string namePrefix = "";
			public string NamePrefix {
				get {
					return namePrefix;
				}
				set {
					namePrefix = value ?? "";
				}
			}
			
			bool hideExtensionParameter = true;
			public bool HideExtensionParameter {
				get {
					return hideExtensionParameter;
				}
				set {
					hideExtensionParameter = value;
				}
			}
			
			public CompletionDataCollector (TextEditor editor, ProjectDom dom, ICompilationUnit unit, DomLocation location)
			{
				this.editor = editor;
				this.dom  = dom;
				this.unit = unit;
				this.location = location;
			}
			
			public ICompletionData AddCompletionData (CodeCompletionDataProvider provider, object obj)
			{
				Namespace ns = obj as Namespace;
				if (ns != null) {
					if (namespaces.ContainsKey(ns.Name))
						return null;
					namespaces[ns.Name] = true;
					CodeCompletionData data = new CodeCompletionData (ns.Name, ns.StockIcon, ns.Documentation);
					provider.AddCompletionData (data);
					return data;
				}
				IReturnType rt = obj as IReturnType;
				if (rt != null) {
					OutputFlags flags = OutputFlags.ClassBrowserEntries;
					bool foundNamespace = false;
					foreach (IUsing u in unit.Usings) {
						foreach (string n in u.Namespaces) {
							if (n == rt.Namespace) {
								foundNamespace = true;
								break;
							}
						}
						foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
							if (alias.Key == namePrefix.Trim () || alias.Key + "::" == namePrefix.Trim ()) {
								foundNamespace = true;
								break;
							}
						}
						
					}
					if (!foundNamespace && (NamePrefix.Length == 0 || !rt.Namespace.StartsWith (NamePrefix)) && !rt.Namespace.EndsWith ("." + NamePrefix))
						flags |= OutputFlags.UseFullName;
					
					CodeCompletionData cd = new CodeCompletionData (ambience.GetString (rt, flags | OutputFlags.EmitMarkup), "md-class", "");
					cd.CompletionString = ambience.GetString (rt, flags);
					provider.AddCompletionData (cd);
					return cd;
				}
				IMember member = obj as IMember;
				if (member != null && !String.IsNullOrEmpty (member.Name)) {
					OutputFlags flags = OutputFlags.IncludeGenerics;
					if (member is IType) {
						IType type = member as IType;
						bool foundType = false;
						foreach (IUsing u in unit.Usings) {
							if (!u.IsFromNamespace || u.Region.Contains (location)) {
								foreach (string n in u.Namespaces) {
									if (type.Namespace == n)
										foundType = true;
								}
							}
							foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
									if (alias.Key == namePrefix.Trim () || alias.Key + "::" == namePrefix.Trim ()) {
									foundType = true;
									break;
								}
							}
							
						}
						
						if (!foundType && (NamePrefix.Length == 0 || !type.Namespace.StartsWith (NamePrefix)) && !type.Namespace.EndsWith ("." + NamePrefix))
							flags |= OutputFlags.UseFullName;
					}
					MemberCompletionData newData = new MemberCompletionData (member, flags);
					newData.HideExtensionParameter = HideExtensionParameter;
					
					if (data.ContainsKey (member.Name)) {
						data [member.Name].AddOverload (newData);
					} else {
						provider.AddCompletionData (newData);
						data [member.Name] = newData;
					}
					return newData;
				}
				return null;
			}
		}
		
		ICompletionDataProvider CreateCompletionData (ResolveResult resolveResult, ExpressionResult expressionResult, NRefactoryResolver resolver)
		{
			if (resolveResult == null || expressionResult == null)
				return null;
			CodeCompletionDataProvider result = new CodeCompletionDataProvider (null, null);
			ProjectDom dom = ProjectDomService.GetProjectDom (Document.Project);
			if (dom == null)
				dom = ProjectDomService.GetFileDom (Document.FileName);
			if (dom == null)
				return null;
			IEnumerable<object> objects = resolveResult.CreateResolveResult (dom, resolver != null ? resolver.CallingMember : null);
			CompletionDataCollector col = new CompletionDataCollector (Editor, dom, Document.CompilationUnit, location);
			col.HideExtensionParameter = !resolveResult.StaticResolve;
			col.NamePrefix = expressionResult.Expression;
			
			if (objects != null) {
				foreach (object obj in objects) {
					if (expressionResult.ExpressionContext != null && expressionResult.ExpressionContext.FilterEntry (obj))
						continue;
					if (expressionResult.ExpressionContext == ExpressionContext.NamespaceNameExcepted && !(obj is Namespace))
						continue;
					col.AddCompletionData (result, obj);
				}
			}
			
			return result;
		}
		
		static string GetLineWhiteSpace (string line)
		{
			int trimmedLength = line.TrimStart ().Length;
			return line.Substring (0, line.Length - trimmedLength);
		}
		
		void AddVirtuals (Dictionary<string, bool> alreadyInserted, CodeCompletionDataProvider provider, IType type, string modifiers, IReturnType curType)
		{
			if (curType == null)
				return;
			IType searchType = dom.SearchType (new SearchTypeRequest (Document.CompilationUnit, curType));
			//System.Console.WriteLine("Add Virtuals for:" + searchType + " / " + curType);
			if (searchType == null)
				return;
			bool isInterface      = type.ClassType == ClassType.Interface;
			bool includeOverriden = false;
		
			int declarationBegin = Editor.CursorPosition;
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
						return; // don't add override completion for static members
				}
			}
			foreach (IType t in this.dom.GetInheritanceTree (searchType)) {
				//System.Console.WriteLine("t:" + t);
				foreach (IMember m in t.Members) {
					//System.Console.WriteLine ("scan:" + m);
					if (m.IsInternal && searchType.SourceProject != Document.Project)
						continue;
					
					if ((isInterface || m.IsVirtual || m.IsAbstract) && !m.IsSealed && (includeOverriden || !type.HasOverriden (m))) {
						//System.Console.WriteLine("add");
						NewOverrideCompletionData data = new NewOverrideCompletionData (Editor, declarationBegin, type, m);
						if (!alreadyInserted.ContainsKey (data.CompletionString)) {
							alreadyInserted[data.CompletionString] = true;
							provider.AddCompletionData (data);
						}
					}
				}
			}
		}
		static string StripGenerics (string str)
		{
			int idx = str.IndexOf ('<');
			if (idx > 0)
				return str.Substring (0, idx);
			return str;
		}
		CodeCompletionDataProvider CreateTypeCompletionData (ExpressionContext context, IReturnType returnType)
		{
			CodeCompletionDataProvider result = new CodeCompletionDataProvider (null, GetAmbience ());
			
			IType type = null;
			if (returnType != null)
				type = dom.SearchType (new SearchTypeRequest (Document.CompilationUnit, returnType));
			ExpressionContext.TypeExpressionContext tce = context as ExpressionContext.TypeExpressionContext;
			
			CompletionDataCollector col = new CompletionDataCollector (Editor, dom, Document.CompilationUnit, location);
			
			if (type == null) {
				result.DefaultCompletionString = StripGenerics (col.AddCompletionData (result, returnType).CompletionString);
				return result;
			}
			
			if (tce != null && tce.Type != null) {
				result.DefaultCompletionString = StripGenerics (col.AddCompletionData (result, tce.Type).CompletionString);
			} else {
				if (context == null || !context.FilterEntry (type))
					col.AddCompletionData (result, type);
			}
			
			foreach (IType curType in dom.GetSubclasses (type)) {
				if (context != null && context.FilterEntry (curType))
					continue;
				col.AddCompletionData (result, curType);
			}
			// add aliases
			foreach (IUsing u in Document.CompilationUnit.Usings) {
				foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
					if (alias.Value.ToInvariantString () == returnType.ToInvariantString ())
						result.AddCompletionData (new CodeCompletionData (alias.Key, "md-class"));
				}
			}
			return result;
		}
		
		CodeCompletionDataProvider GetOverrideCompletionData (IType type, string modifiers)
		{
			CodeCompletionDataProvider result = new CodeCompletionDataProvider (null, GetAmbience ());
			Dictionary<string, bool> alreadyInserted = new Dictionary<string, bool> ();
			bool addedVirtuals = false;
			foreach (IReturnType baseType in type.BaseTypes) {
				AddVirtuals (alreadyInserted, result, type, modifiers, baseType);
				addedVirtuals = true;
			}
			if (!addedVirtuals)
				AddVirtuals (alreadyInserted, result, type, modifiers, DomReturnType.Object);
			return result;
		}
		
		static string[] primitiveTypes = new string [] { "void", "object", "bool", "byte", "sbyte", "char", "short", "int", "long", "ushort", "uint", "ulong", "float", "double", "decimal", "string"};
		static void AddPrimitiveTypes (CodeCompletionDataProvider provider)
		{
			foreach (string pimitiveType in primitiveTypes) {
				provider.AddCompletionData (new CodeCompletionData (pimitiveType, "md-literal"));
			}
		}
		
		static void AddNRefactoryKeywords (CodeCompletionDataProvider provider, System.Collections.BitArray keywords)
		{
			for (int i = 0; i < keywords.Length; i++) {
				if (keywords[i]) 
					provider.AddCompletionData (new CodeCompletionData (ICSharpCode.NRefactory.Parser.CSharp.Tokens.GetTokenString (i), "md-literal"));
			}
		}
		
		CodeCompletionDataProvider CreateCtrlSpaceCompletionData (ExpressionResult expressionResult)
		{
			NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom, Document.CompilationUnit,
			                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
			                                                                                Editor,
			                                                                                Document.FileName);
			
			resolver.SetupResolver (new DomLocation (Editor.CursorLine, Editor.CursorColumn));
			//System.Console.WriteLine(expressionResult.ExpressionContext );
			CodeCompletionDataProvider result = new CodeCompletionDataProvider (null, GetAmbience ());
			
			if (expressionResult.ExpressionContext == ExpressionContext.TypeDeclaration) {
				AddPrimitiveTypes (result);
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.TypeLevel);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.InterfaceDeclaration) {
				AddPrimitiveTypes (result);
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.InterfaceLevel);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.MethodBody) {
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.StatementStart);
				AddPrimitiveTypes (result);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.InterfacePropertyDeclaration) {
				result.AddCompletionData (new CodeCompletionData ("get", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("set", "md-literal"));
			} else if (expressionResult.ExpressionContext == ExpressionContext.Attribute) {
				result.AddCompletionData (new CodeCompletionData ("assembly", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("module", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("type", "md-literal"));
			
				result.AddCompletionData (new CodeCompletionData ("method", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("field", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("property", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("event", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("param", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("return", "md-literal"));
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.BaseConstructorCall) {
				result.AddCompletionData (new CodeCompletionData ("this", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("base", "md-literal"));
			} else  if (expressionResult.ExpressionContext == ExpressionContext.ParameterType || expressionResult.ExpressionContext == ExpressionContext.FirstParameterType) {
				result.AddCompletionData (new CodeCompletionData ("ref", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("out", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("params", "md-literal"));
				// C# 3.0 extension method
				if (expressionResult.ExpressionContext == ExpressionContext.FirstParameterType)
					result.AddCompletionData (new CodeCompletionData ("this", "md-literal"));
				AddPrimitiveTypes (result);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.PropertyDeclaration) {
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.InPropertyDeclaration);
			} else if (expressionResult.ExpressionContext == ExpressionContext.EventDeclaration) {
				result.AddCompletionData (new CodeCompletionData ("add", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("remove", "md-literal"));
			} //else if (expressionResult.ExpressionContext == ExpressionContext.FullyQualifiedType) {} 
			else if (expressionResult.ExpressionContext == ExpressionContext.Default) {
				AddPrimitiveTypes (result);
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.ExpressionStart);
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.ExpressionContent);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.Global) {
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.GlobalLevel);
			} else {
				AddPrimitiveTypes (result);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, result);
			}
			
			if (resolver.CallingMember is IMethod && ((IMethod)resolver.CallingMember).GenericParameters != null) {
				foreach (IReturnType returnType in ((IMethod)resolver.CallingMember).GenericParameters) {
					result.AddCompletionData (new CodeCompletionData (returnType.Name, "md-literal"));
				}
			}
			return result;
		}

		#region case completion
		ICompletionDataProvider CreateCaseCompletionData (ExpressionResult expressionResult)
		{
			NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom,
			                                                                                Document.CompilationUnit,
			                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
			                                                                                Editor,
			                                                                                Document.FileName);
			
			resolver.SetupResolver (location);
			
			SwitchFinder switchFinder = new SwitchFinder (location);
			if (resolver.MemberCompilationUnit != null)
				switchFinder.VisitCompilationUnit (resolver.MemberCompilationUnit, null);
			CodeCompletionDataProvider result = new CodeCompletionDataProvider (null, GetAmbience ());
			if (switchFinder.SwitchStatement == null)
				return result;
			ResolveResult resolveResult = resolver.ResolveExpression (switchFinder.SwitchStatement.SwitchExpression, location);
			IType type = dom.GetType (resolveResult.ResolvedType);
			if (type != null && type.ClassType == ClassType.Enum) {
				CompletionDataCollector cdc = new CompletionDataCollector (Editor, dom, Document.CompilationUnit, location);
				cdc.AddCompletionData (result, type);
			}
			return result;
		}
		
		class SwitchFinder : ICSharpCode.NRefactory.Visitors.AbstractAstVisitor
		{
			ICSharpCode.NRefactory.Location location;
			ICSharpCode.NRefactory.Ast.SwitchStatement switchStatement = null;
			
			public ICSharpCode.NRefactory.Ast.SwitchStatement SwitchStatement {
				get {
					return this.switchStatement;
				}
			}
			
			public SwitchFinder (DomLocation location)
			{
				this.location = new ICSharpCode.NRefactory.Location (location.Column, location.Line);
			}
			
			public override object VisitSwitchStatement (ICSharpCode.NRefactory.Ast.SwitchStatement switchStatement, object data)
			{
//				if (switchStatement.StartLocation < caretLocation && caretLocation < switchStatement.EndLocation)
					this.switchStatement = switchStatement;
				return base.VisitSwitchStatement(switchStatement, data);
			}

		}
		#endregion
		
		#region Preprocessor
		CodeCompletionDataProvider GetDefineCompletionData ()
		{
			if (Document.Project == null)
				return null;

			Dictionary<string, string> symbols = new Dictionary<string, string> ();
			CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
			foreach (DotNetProjectConfiguration conf in Document.Project.Configurations) {
				CSharpCompilerParameters cparams = conf.CompilationParameters as CSharpCompilerParameters;
				if (cparams != null) {
					string[] syms = cparams.DefineSymbols.Split (';');
					foreach (string s in syms) {
						string ss = s.Trim ();
						if (ss.Length > 0 && !symbols.ContainsKey (ss)) {
							symbols [ss] = ss;
							cp.AddCompletionData (new CodeCompletionData (ss, "md-literal"));
						}
					}
				}
			}
			return cp;
		}
		
		CodeCompletionDataProvider GetDirectiveCompletionData ()
		{
			CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
			cp.AddCompletionData (new CodeCompletionData ("if", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("else", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("elif", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("endif", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("define", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("undef", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("warning", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("error", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("pragma", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("line", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("line hidden", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("line default", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("region", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("endregion", "md-literal"));
			return cp;
		}
		#endregion
		
		#region Xml Comments
		bool IsInsideDocumentationComment (int cursor)
		{
			int lin, col;
			Editor.GetLineColumnFromPosition (cursor, out lin, out col);
			return Editor.GetLineText (lin).Trim ().StartsWith ("///");
		}
		
		bool IsInsideClassBody (IType insideClass, int line, int column)
		{
			if (insideClass.Members != null) {
				foreach (IMember m in insideClass.Methods) {
					if (m.BodyRegion.Contains (line, column)) 
						return false;
				}
			}
			return true;
		}
		
		void AppendSummary (StringBuilder sb, string indent, out int newCursorOffset)
		{
			Debug.Assert (sb != null);
			int length = sb.Length;
			sb.Append (" <summary>\n");
			sb.Append (indent);
			sb.Append ("/// \n");
			newCursorOffset = sb.Length - length - 1;
			sb.Append (indent);
			sb.Append ("/// </summary>");
		}
		
		void AppendMethodComment (StringBuilder builder, string indent, IMethod method)
		{
			if (method.Parameters != null) {
				foreach (IParameter para in method.Parameters) {
					builder.Append (Environment.NewLine);
					builder.Append (indent);
					builder.Append ("/// <param name=\"");
					builder.Append (para.Name);
					builder.Append ("\">\n");
					builder.Append (indent);
					builder.Append ("/// A <see cref=\"");
					builder.Append (para.ReturnType.FullName);
					builder.Append ("\"/>\n");
					builder.Append (indent);
					builder.Append ("/// </param>");
				}
			}
			if (method.ReturnType != null && method.ReturnType.FullName != "System.Void") {
				builder.Append (Environment.NewLine);
				builder.Append (indent);
				builder.Append("/// <returns>\n");
				builder.Append (indent);
				builder.Append ("/// A <see cref=\"");
				builder.Append (method.ReturnType.FullName);
				builder.Append ("\"/>\n");
				builder.Append (indent);
				builder.Append ("/// </returns>");
			}
		}
		
		string GenerateBody (IType c, int line, string indent, out int newCursorOffset)
		{
			int startLine = int.MaxValue;
			newCursorOffset = 0;
			StringBuilder builder = new StringBuilder ();
			
			IMethod method = null;
			IProperty property = null;
			foreach (IMethod m in c.Methods) {
				if (m.Location.Line < startLine && m.Location.Line > line) {
					startLine = m.Location.Line;
					method = m;
				}
			}
			foreach (IProperty p in c.Properties) {
				if (p.Location.Line < startLine && p.Location.Line > line) {
					startLine = p.Location.Line;
					property = p;
					method = null;
				}
			}
			
			if (method != null) {
				AppendSummary (builder, indent, out newCursorOffset);
				AppendMethodComment (builder, indent, method);
			} else if (property != null) {
				builder.Append ("/ <value>\n");
				builder.Append (indent);
				builder.Append ("/// \n");
				builder.Append (indent);
				builder.Append ("/// </value>");
				newCursorOffset = ("/ <value>\n/// " + indent).Length;
			}
			
			return builder.ToString ();
		}
		
		static readonly List<string> commentTags = new List<string> (new string[] { "c", "code", "example", "exception", "include", "list", "listheader", "item", "term", "description", "para", "param", "paramref", "permission", "remarks", "returns", "see", "seealso", "summary", "value" });
		
		CodeCompletionDataProvider GetXmlDocumentationCompletionData ()
		{
			CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
			cp.AddCompletionData (new CodeCompletionData ("c", "md-literal", GettextCatalog.GetString ("Set text in a code-like font")));
			cp.AddCompletionData (new CodeCompletionData ("code", "md-literal", GettextCatalog.GetString ("Set one or more lines of source code or program output")));
			cp.AddCompletionData (new CodeCompletionData ("example", "md-literal", GettextCatalog.GetString ("Indicate an example")));
			cp.AddCompletionData (new CodeCompletionData ("exception", "md-literal", GettextCatalog.GetString ("Identifies the exceptions a method can throw"), "exception cref=\"|\"></exception>"));
			cp.AddCompletionData (new CodeCompletionData ("include", "md-literal", GettextCatalog.GetString ("Includes comments from a external file"), "include file=\"|\" path=\"\">"));
			cp.AddCompletionData (new CodeCompletionData ("list", "md-literal", GettextCatalog.GetString ("Create a list or table"), "list type=\"|\">"));
			
			cp.AddCompletionData (new CodeCompletionData ("listheader", "md-literal", GettextCatalog.GetString ("Define the heading row")));
			cp.AddCompletionData (new CodeCompletionData ("item", "md-literal", GettextCatalog.GetString ("Defines list or table item")));
			cp.AddCompletionData (new CodeCompletionData ("term", "md-literal", GettextCatalog.GetString ("A term to define")));
			cp.AddCompletionData (new CodeCompletionData ("description", "md-literal", GettextCatalog.GetString ("Describes a list item")));
			cp.AddCompletionData (new CodeCompletionData ("para", "md-literal", GettextCatalog.GetString ("Permit structure to be added to text")));

			cp.AddCompletionData (new CodeCompletionData ("param", "md-literal", GettextCatalog.GetString ("Describe a parameter for a method or constructor"), "param name=\"|\">"));
			cp.AddCompletionData (new CodeCompletionData ("paramref", "md-literal", GettextCatalog.GetString ("Identify that a word is a parameter name"), "paramref name=\"|\"/>"));
			
			cp.AddCompletionData (new CodeCompletionData ("permission", "md-literal", GettextCatalog.GetString ("Document the security accessibility of a member"), "permission cref=\"|\""));
			cp.AddCompletionData (new CodeCompletionData ("remarks", "md-literal", GettextCatalog.GetString ("Describe a type")));
			cp.AddCompletionData (new CodeCompletionData ("returns", "md-literal", GettextCatalog.GetString ("Describe the return value of a method")));
			cp.AddCompletionData (new CodeCompletionData ("see", "md-literal", GettextCatalog.GetString ("Specify a link"), "see cref=\"|\"/>"));
			cp.AddCompletionData (new CodeCompletionData ("seealso", "md-literal", GettextCatalog.GetString ("Generate a See Also entry"), "seealso cref=\"|\"/>"));
			cp.AddCompletionData (new CodeCompletionData ("summary", "md-literal", GettextCatalog.GetString ("Describe a member of a type")));
			cp.AddCompletionData (new CodeCompletionData ("typeparam", "md-literal", GettextCatalog.GetString ("Describe a type parameter for a generic type or method")));
			cp.AddCompletionData (new CodeCompletionData ("typeparamref", "md-literal", GettextCatalog.GetString ("Identify that a word is a type parameter name")));
			cp.AddCompletionData (new CodeCompletionData ("value", "md-literal", GettextCatalog.GetString ("Describe a property")));
			
			return cp;
		}
		#endregion
	}
}
