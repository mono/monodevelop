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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Gui.Completion;

using CSharpBinding;
using CSharpBinding.FormattingStrategy;
using CSharpBinding.Parser;

namespace MonoDevelop.CSharpBinding.Gui
{
	public class CSharpTextEditorCompletion : CompletionTextEditorExtension
	{
		ProjectDom dom;
		DocumentStateTracker<CSharpIndentEngine> stateTracker;
		
		public CSharpTextEditorCompletion ()
		{
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			stateTracker = new DocumentStateTracker<CSharpIndentEngine> (Editor);
			dom = ProjectDomService.GetDom (Document.Project);
		}
		
		ExpressionResult FindExpression (ProjectDom dom, int offset)
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
			if (dom == null)
				return null;
			stateTracker.UpdateEngine ();
			ExpressionResult result;
			int cursor, newCursorOffset = 0;
			
			switch (completionChar) {
			case '.':
				result = FindExpression (dom, 0);
				if (result == null)
					return null;
				
				NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom,
				                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
				                                                                                Editor,
				                                                                                Document.FileName);
				
				ResolveResult resolveResult = resolver.Resolve (result);
				return CreateCompletionData (resolveResult, result);
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
			case '<':
				if (stateTracker.Engine.IsInsideDocLineComment) 
					return GetXmlDocumentationCompletionData ();
				return null;
			case '/':
				cursor = Editor.SelectionStartPosition;
				if (cursor < 2)
					break;
				if (stateTracker.Engine.IsInsideDocLineComment) {
					StringBuilder generatedComment = new StringBuilder ();
					bool generateStandardComment = true;
					IType insideClass = NRefactoryResolver.GetTypeAtCursor (dom, Document.FileName, Editor);
					if (insideClass != null) {
						string indent = GetLineWhiteSpace (Editor.GetLineText (Editor.CursorLine));
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
				return HandleKeywordCompletion (result, i, token);
			default:
				if (Char.IsLetter (completionChar) && !stateTracker.Engine.IsInsideDocLineComment && !stateTracker.Engine.IsInsideOrdinaryCommentOrString) {
					char prevCh = Editor.CursorPosition > 2 ? Editor.GetCharAt (Editor.CursorPosition - 2) : '\0';
					if (prevCh != '.') {
						NewCSharpExpressionFinder expressionFinder = new NewCSharpExpressionFinder (dom);
						try {
							result = expressionFinder.FindExpression (Editor.Text, Editor.CursorPosition - 1);
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
/*					expressionFinder = new NewCSharpExpressionFinder (dom);
					try {
						result = expressionFinder.FindFullExpression (Editor.Text, Editor.CursorPosition);
					} catch (Exception ex) {
						LoggingService.LogWarning (ex.Message, ex);
						return null;
					}*/
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
			ExpressionResult result = FindExpression (dom , -2);
			if (result == null)
				return null;
			NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom,
			                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
			                                                                                Editor,
			                                                                                Document.FileName);
			
			switch (completionChar) {
			case '(':
				ResolveResult resolveResult = resolver.Resolve (result);
				if (resolveResult != null) {
					if (resolveResult is MethodResolveResult)
						return new NRefactoryParameterDataProvider (Editor, resolver.Dom, resolveResult as MethodResolveResult);
					if (result.ExpressionContext == ExpressionContext.BaseConstructorCall) {
						if (resolveResult is ThisResolveResult)
							return new NRefactoryParameterDataProvider (Editor, resolver.Dom, resolveResult as ThisResolveResult);
						if (resolveResult is BaseResolveResult)
							return new NRefactoryParameterDataProvider (Editor, resolver.Dom, resolveResult as BaseResolveResult);
					}
				}
				break;
			}
			return null;
		}
		
		
		public ICompletionDataProvider HandleKeywordCompletion (ExpressionResult result, int wordStart, string word)
		{
			switch (word) {
			case "using":
				result.ExpressionContext = ExpressionContext.Using;
				return CreateCompletionData (new NamespaceResolveResult (""), result);
			case "is":
			case "as":
				System.Console.WriteLine("IsAs");
				return null;
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
				IType cls = NRefactoryResolver.GetTypeAtCursor (dom, Document.FileName, Editor);
				if (cls != null && (cls.ClassType == ClassType.Class || cls.ClassType == ClassType.Struct)) {
					string modifiers = Editor.GetText (firstMod, wordStart);
					return GetOverrideCompletionData (cls, modifiers);
				}
				return null;
			case "new":
				ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForNewCompletion(Editor, Document.FileName);
				System.Console.WriteLine("New:" + exactContext);
				return null;
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
			return CreateCtrlSpaceCompletionData (result);
		}
		
		public static void AddCompletionData (CodeCompletionDataProvider provider, object obj)
		{
			Namespace ns = obj as Namespace;
			if (ns != null) {
				provider.AddCompletionData (new CodeCompletionData (ns.Name, ns.StockIcon, ns.Documentation));
				return;
			}
		
			IMember member = obj as IMember;
			if (member != null) {
				provider.AddCompletionData (new CodeCompletionData (member.Name, member.StockIcon, member.Documentation));
				return;
			}
		}
		
		
		ICompletionDataProvider CreateCompletionData (ResolveResult resolveResult, ExpressionResult expressionResult)
		{
			if (resolveResult == null || expressionResult == null)
				return null;
			CodeCompletionDataProvider result = new CodeCompletionDataProvider (null, null);
			ProjectDom dom = ProjectDomService.GetDom (Document.Project);
			if (dom == null)
				return null;
			IEnumerable<object> objects = resolveResult.CreateResolveResult (dom);
			if (objects != null) {
				foreach (object obj in objects) {
					if (expressionResult.ExpressionContext != null && expressionResult.ExpressionContext.FilterEntry (obj))
						continue;
					AddCompletionData (result, obj);
				}
			}
			
			return result;
		}
		
		static string GetLineWhiteSpace (string line)
		{
			int trimmedLength = line.TrimStart ().Length;
			return line.Substring (0, line.Length - trimmedLength);
		}
		
		void AddVirtuals (CodeCompletionDataProvider provider, IType type, string modifiers, IReturnType curType)
		{
			IType searchType = dom.GetType (curType);
			if (searchType == null)
				return;
			bool isInterface      = type.ClassType == ClassType.Interface;
			bool includeOverriden = false;
			foreach (IMember m in searchType.Members) {
				System.Console.WriteLine ("scan:" + m);
				if (m.IsInternal && searchType.SourceProject != Document.Project)
					continue;
				
				if ((isInterface || m.IsVirtual || m.IsAbstract) && !m.IsSealed && (includeOverriden || !type.HasOverriden (m))) {
				System.Console.WriteLine("add");
					provider.AddCompletionData (new NewOverrideCompletionData (Editor, m));
				}
			}
			if (searchType.BaseType == null) {
				if (searchType.FullName != "System.Object")
					AddVirtuals (provider, type, modifiers, new DomReturnType ("System.Object"));
			} else {
				AddVirtuals (provider, type, modifiers, searchType.BaseType);
			}
			
		}
		
		CodeCompletionDataProvider GetOverrideCompletionData (IType type, string modifiers)
		{
			CodeCompletionDataProvider result = new CodeCompletionDataProvider (null, GetAmbience ());
			AddVirtuals (result, type, modifiers, type.BaseType);
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
		
		ICompletionDataProvider CreateCtrlSpaceCompletionData (ExpressionResult expressionResult)
		{
			NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom,
			                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
			                                                                                Editor,
			                                                                                Document.FileName);
			//System.Console.WriteLine(expressionResult.ExpressionContext );
			CodeCompletionDataProvider result = new CodeCompletionDataProvider (null, GetAmbience ());
			if (expressionResult.ExpressionContext == ExpressionContext.TypeDeclaration) {
				AddPrimitiveTypes (result);
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.TypeLevel);
				result.AddCompletionData (new CodeCompletionData ("partial", "md-literal")); // maybe obsolete in newer nrefactory versions.
				resolver.AddAccessibleCodeCompletionData (result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.InterfaceDeclaration) {
				AddPrimitiveTypes (result);
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.InterfaceLevel);
				resolver.AddAccessibleCodeCompletionData (result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.MethodBody) {
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.StatementStart);
				result.AddCompletionData (new CodeCompletionData ("yield", "md-literal")); // maybe obsolete in newer nrefactory versions.
				AddPrimitiveTypes (result);
				resolver.AddAccessibleCodeCompletionData (result);
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
				resolver.AddAccessibleCodeCompletionData (result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.BaseConstructorCall) {
				result.AddCompletionData (new CodeCompletionData ("this", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("base", "md-literal"));
			} else  if (expressionResult.ExpressionContext == ExpressionContext.ParameterType || expressionResult.ExpressionContext == ExpressionContext.FirstParameterType) {
				result.AddCompletionData (new CodeCompletionData ("ref", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("out", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("params", "md-literal"));
				// TODO: Extension methods for C# 3.0
				AddPrimitiveTypes (result);
				resolver.AddAccessibleCodeCompletionData (result);
			} else if (expressionResult.ExpressionContext == ExpressionContext.PropertyDeclaration) {
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.InPropertyDeclaration);
				result.AddCompletionData (new CodeCompletionData ("get", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("set", "md-literal"));
			} else if (expressionResult.ExpressionContext == ExpressionContext.EventDeclaration) {
				result.AddCompletionData (new CodeCompletionData ("add", "md-literal"));
				result.AddCompletionData (new CodeCompletionData ("remove", "md-literal"));
			} else if (expressionResult.ExpressionContext == ExpressionContext.FullyQualifiedType) {
				//cu.ProjectContent.AddNamespaceContents(result, "", languageProperties, true);
			} else if (expressionResult.ExpressionContext == ExpressionContext.Default) {
				AddPrimitiveTypes (result);
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.ExpressionStart);
				AddNRefactoryKeywords (result, ICSharpCode.NRefactory.Parser.CSharp.Tokens.ExpressionContent);
				resolver.AddAccessibleCodeCompletionData (result);
			} else {
				AddPrimitiveTypes (result);
				resolver.AddAccessibleCodeCompletionData (result);
			}
			return result;
		}
		
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
			cp.AddCompletionData (new CodeCompletionData ("c", "md-literal", GettextCatalog.GetString ("Marks text as code.")));
			cp.AddCompletionData (new CodeCompletionData ("code", "md-literal", GettextCatalog.GetString ("Marks text as code.")));
			cp.AddCompletionData (new CodeCompletionData ("example", "md-literal", GettextCatalog.GetString ("A description of the code sample.\nCommonly, this would involve use of the &lt;code&gt; tag.")));
			cp.AddCompletionData (new CodeCompletionData ("exception", "md-literal", GettextCatalog.GetString ("This tag lets you specify which exceptions can be thrown."), "exception cref=\"|\"></exception>"));
			cp.AddCompletionData (new CodeCompletionData ("include", "md-literal", GettextCatalog.GetString ("The &lt;include&gt; tag lets you refer to comments in another file that describe the types and members in your source code.\nThis is an alternative to placing documentation comments directly in your source code file."), "include file=\"|\" path=\"\">"));
			cp.AddCompletionData (new CodeCompletionData ("list", "md-literal", GettextCatalog.GetString ("Defines a list or table."), "list type=\"|\">"));
			cp.AddCompletionData (new CodeCompletionData ("listheader", "md-literal", GettextCatalog.GetString ("Defines a header for a list or table.")));
			cp.AddCompletionData (new CodeCompletionData ("item", "md-literal", GettextCatalog.GetString ("Defines an item for a list or table.")));
			cp.AddCompletionData (new CodeCompletionData ("term", "md-literal", GettextCatalog.GetString ("A term to define.")));
			cp.AddCompletionData (new CodeCompletionData ("description", "md-literal", GettextCatalog.GetString ("Describes a term in a list or table.")));
			cp.AddCompletionData (new CodeCompletionData ("para", "md-literal", GettextCatalog.GetString ("A text paragraph.")));

			cp.AddCompletionData (new CodeCompletionData ("param", "md-literal", GettextCatalog.GetString ("Describes a method parameter."), "param name=\"|\">"));
			cp.AddCompletionData (new CodeCompletionData ("paramref", "md-literal", GettextCatalog.GetString ("The &lt;paramref&gt; tag gives you a way to indicate that a word is a parameter."), "paramref name=\"|\"/>"));
			
			cp.AddCompletionData (new CodeCompletionData ("permission", "md-literal", GettextCatalog.GetString ("The &lt;permission&gt; tag lets you document the access of a member."), "permission cref=\"|\""));
			cp.AddCompletionData (new CodeCompletionData ("remarks", "md-literal", GettextCatalog.GetString ("The &lt;remarks&gt; tag is used to add information about a type, supplementing the information specified with &lt;summary&gt;.")));
			cp.AddCompletionData (new CodeCompletionData ("returns", "md-literal", GettextCatalog.GetString ("The &lt;returns&gt; tag should be used in the comment for a method declaration to describe the return value.")));
			cp.AddCompletionData (new CodeCompletionData ("see", "md-literal", GettextCatalog.GetString ("The &lt;see&gt; tag lets you specify a link from within text."), "see cref=\"|\"/>"));
			cp.AddCompletionData (new CodeCompletionData ("seealso", "md-literal", GettextCatalog.GetString ("The &lt;seealso&gt; tag lets you specify the text that you might want to appear in a See Also section."), "seealso cref=\"|\"/>"));
			cp.AddCompletionData (new CodeCompletionData ("summary", "md-literal", GettextCatalog.GetString ("The &lt;summary&gt; tag should be used to describe a type or a type member.")));
			cp.AddCompletionData (new CodeCompletionData ("value", "md-literal", GettextCatalog.GetString ("The &lt;value&gt; tag lets you describe a property.")));
			
			return cp;
		}
		#endregion
	}
}
