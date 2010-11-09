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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.CodeCompletion;

using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Dom;
using MonoDevelop.CSharp.Project;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.Components;
using Gtk;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.Completion
{
	public class CSharpTextEditorCompletion : CompletionTextEditorExtension, IPathedDocument
	{
		ProjectDom dom;
		DocumentStateTracker<CSharpIndentEngine> stateTracker;
		CSharpFormattingPolicy policy;
		Mono.TextEditor.TextEditorData textEditorData;
		
		public ProjectDom Dom {
			get { return this.dom; }
			set { this.dom = value; }
		}
		
		public CSharpTextEditorCompletion ()
		{
			IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
		}
		
		public CSharpTextEditorCompletion (Document doc) : this ()
		{
			Initialize (doc);
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			dom = Document.Dom;
			textEditorData = Document.Editor;
			
			InitTracker ();
			IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			if (dom != null && dom.Project != null)
				policy = base.Document.Project.Policies.Get<CSharpFormattingPolicy> (types);
			UpdatePath (null, null);
			textEditorData.Caret.PositionChanged += UpdatePath;
			Document.DocumentParsed += delegate {
				UpdatePath (null, null);
			};
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
		
		ExpressionResult FindExpression (ProjectDom dom, CodeCompletionContext ctx, int offset)
		{
			NewCSharpExpressionFinder expressionFinder = new NewCSharpExpressionFinder (dom);
			try {
				return expressionFinder.FindExpression (textEditorData, Math.Max (ctx.TriggerOffset + offset, 0));
			} catch (Exception ex) {
				LoggingService.LogWarning (ex.Message, ex);
				return null;
			}
		}
		
		ExpressionResult FindExpression (ProjectDom dom, CodeCompletionContext ctx)
		{
			NewCSharpExpressionFinder expressionFinder = new NewCSharpExpressionFinder (dom);
			try {
				return expressionFinder.FindExpression (textEditorData, ctx.TriggerOffset);
			} catch (Exception ex) {
				LoggingService.LogWarning (ex.Message, ex);
				return null;
			}
		}
		
		static bool MatchDelegate (IType delegateType, IMethod method)
		{
			IMethod delegateMethod = delegateType.Methods.First ();
			if (delegateMethod.Parameters.Count != method.Parameters.Count)
				return false;
			for (int i = 0; i < delegateMethod.Parameters.Count; i++) {
				if (delegateMethod.Parameters[i].ReturnType.ToInvariantString () != method.Parameters[i].ReturnType.ToInvariantString ())
					return false;
			}
			return true;
		}
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool result = base.KeyPress (key, keyChar, modifier);
			
			if (PropertyService.Get ("EnableParameterInsight", true) && (keyChar == ',' || keyChar == ')') && CanRunParameterCompletionCommand ())
				base.RunParameterCompletionCommand ();
			
			if (stateTracker.Engine.IsInsideComment) {
				ParameterInformationWindowManager.HideWindow ();
			}/* else {
				int cpos;
				if (key == Gdk.Key.Return && CanRunParameterCompletionCommand () && GetParameterCompletionCommandOffset (out cpos))  {
					base.RunParameterCompletionCommand ();
					ParameterInformationWindowManager.CurrentCodeCompletionContext = Editor.CurrentCodeCompletionContext;
					ParameterInformationWindowManager.PostProcessKeyEvent (key, modifier);
				}
			}*/
			return result;
		}
		
		bool tryToForceCompletion = false;
		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			if (textEditorData.CurrentMode is CompletionTextLinkMode) {
				if (!((CompletionTextLinkMode)textEditorData.CurrentMode).TriggerCodeCompletion)
					return null;
			} else if (textEditorData.CurrentMode is Mono.TextEditor.TextLinkEditMode) {
				return null;
			}

	//		IDisposable timer = null;
		try {
			if (dom == null /*|| Document.CompilationUnit == null*/)
				return null;
			if (completionChar != '#' && stateTracker.Engine.IsInsidePreprocessorDirective)
				return null;
		//	timer = Counters.ResolveTime.BeginTiming ();
			DomLocation location = new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset - 1);
			stateTracker.UpdateEngine ();
			ExpressionResult result;
			int cursor, newCursorOffset = 0;
			switch (completionChar) {
			case ':':
			case '.':
				if (stateTracker.Engine.IsInsideDocLineComment || stateTracker.Engine.IsInsideOrdinaryCommentOrString)
					return null;
				result = FindExpression (dom, completionContext);
				if (result == null || result.Expression == null)
					return null;
				int idx = result.Expression.LastIndexOf ('.');
				if (idx > 0)
					result.Expression = result.Expression.Substring (0, idx);
				// don't parse expressions that end with more than 1 dot - see #646820
				if (result.Expression.EndsWith ("."))
					return null;
				NRefactoryResolver resolver = CreateResolver ();
				ResolveResult resolveResult = resolver.Resolve (result, location);
				if (resolver.ResolvedExpression is ICSharpCode.NRefactory.Ast.PrimitiveExpression) {
					ICSharpCode.NRefactory.Ast.PrimitiveExpression pex = (ICSharpCode.NRefactory.Ast.PrimitiveExpression)resolver.ResolvedExpression;
					if (!tryToForceCompletion && !(pex.Value is string || pex.Value is char || pex.Value is bool))
						return null;
				}
					
				return CreateCompletionData (location, resolveResult, result, resolver);
			case '#':
				if (stateTracker.Engine.IsInsidePreprocessorDirective) 
					return GetDirectiveCompletionData ();
				return null;
			case '>':
				cursor = textEditorData.IsSomethingSelected ? textEditorData.SelectionRange.Offset : textEditorData.Caret.Offset;
				
				if (stateTracker.Engine.IsInsideDocLineComment) {
					string lineText = textEditorData.GetLineText (completionContext.TriggerLine);
					int startIndex = Math.Min (completionContext.TriggerLineOffset - 1, lineText.Length - 1);
					
					while (startIndex >= 0 && lineText[startIndex] != '<') {
						--startIndex;
						if (lineText[startIndex] == '/') { // already closed.
							startIndex = -1;
							break;
						}
					}
					
					if (startIndex >= 0) {
						int endIndex = startIndex;
						while (endIndex <= completionContext.TriggerLineOffset && endIndex < lineText.Length && !Char.IsWhiteSpace (lineText[endIndex])) {
							endIndex++;
						}
						string tag = endIndex - startIndex - 1 > 0 ? lineText.Substring (startIndex + 1, endIndex - startIndex - 2) : null;
						if (!String.IsNullOrEmpty (tag) && commentTags.IndexOf (tag) >= 0) {
							textEditorData.Insert (cursor, "</" + tag + ">");
							textEditorData.Caret.Offset = cursor; 
							return null;
						}
					}
				}
				return null;
/* Disabled because it gives problems when declaring arrays - for example string [] should not pop up code completion.
 			case '[':
				if (stateTracker.Engine.IsInsideDocLineComment || stateTracker.Engine.IsInsideOrdinaryCommentOrString)
					return null;
				result = FindExpression (dom, completionContext);
				if (result.ExpressionContext == ExpressionContext.Attribute)
					return CreateCtrlSpaceCompletionData (completionContext, result);
				return null;*/
			case '<':
				if (stateTracker.Engine.IsInsideDocLineComment) 
					return GetXmlDocumentationCompletionData ();
				return null;
			case '(':
				if (stateTracker.Engine.IsInsideDocLineComment || stateTracker.Engine.IsInsideOrdinaryCommentOrString)
					return null;
				result = FindExpression (dom, completionContext, -1);
				if (result == null || result.Expression == null)
					return null;
				resolver = CreateResolver ();
				resolveResult = resolver.Resolve (result, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset - 2));
				
				if (resolveResult != null && resolver.ResolvedExpression is ICSharpCode.NRefactory.Ast.TypeOfExpression) {
					CompletionDataList completionList = new ProjectDomCompletionDataList ();
					
					CompletionDataCollector col = new CompletionDataCollector (dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
					AddPrimitiveTypes (col);
					foreach (object o in dom.GetNamespaceContents (GetUsedNamespaces (), true, true)) {
						col.Add (o);
					}
					if (resolver.CallingMember is IMethod) {
						foreach (ITypeParameter tp in ((IMethod)resolver.CallingMember).TypeParameters) {
							col.Add (tp.Name, "md-keyword");
						}
					}
					if (resolver.CallingType != null) {
						foreach (ITypeParameter tp in resolver.CallingType.TypeParameters) {
							col.Add (tp.Name, "md-keyword");
						}
					}
					return completionList;
				}
				return null;
			case '/':
				cursor = textEditorData.IsSomethingSelected ? textEditorData.SelectionRange.Offset : textEditorData.Caret.Offset;
				if (cursor < 2)
					break;
					
				if (stateTracker.Engine.IsInsideDocLineComment) {
					string lineText = textEditorData.GetLineText (completionContext.TriggerLine);
					bool startsDocComment = true;
					int slashes = 0;
					for (int i = 0; i < completionContext.TriggerLineOffset && i < lineText.Length; i++) {
						if (lineText[i] == '/') {
							slashes++;
							continue;
						}
						if (!Char.IsWhiteSpace (lineText[i])) {
							startsDocComment = false;
							break;
						}
					}
					// check if lines above already start a doc comment
					for (int i = completionContext.TriggerLine - 2; i >= 1; i--) {
						string text = textEditorData.GetLineText (i);
						if (text.Length == 0)
							continue;
						if (text.StartsWith ("///")) {
							startsDocComment = false;
							break;
						}
						break;
					}
						
					// check if following lines start a doc comment
					for (int i = completionContext.TriggerLine; i <= textEditorData.Document.LineCount; i++) {
						string text = textEditorData.GetLineText (i);
						if (text == null)
							break;
						text = text.Trim ();
						if (text.Length == 0)
							continue;
						if (text.StartsWith ("///")) {
							startsDocComment = false;
							break;
						}
						break;
					}
					
					if (!startsDocComment || slashes != 3)
						break;
					StringBuilder generatedComment = new StringBuilder ();
					bool generateStandardComment = true;
					ParsedDocument currentParsedDocument = Document.UpdateParseDocument ();
					IType insideClass = NRefactoryResolver.GetTypeAtCursor (currentParsedDocument.CompilationUnit, Document.FileName, location);
					if (insideClass != null) {
						string indent = textEditorData.Document.GetLineIndent (completionContext.TriggerLine);
						if (insideClass.ClassType == ClassType.Delegate) {
							AppendSummary (generatedComment, indent, out newCursorOffset);
							IMethod m = null;
							foreach (IMethod method in insideClass.Methods)
								m = method;
							AppendMethodComment (generatedComment, indent, m);
							generateStandardComment = false;
						} else {
							if (!IsInsideClassBody (insideClass, completionContext.TriggerLine, completionContext.TriggerLineOffset))
								break;
							string body = GenerateBody (insideClass, completionContext.TriggerLine, indent, out newCursorOffset);
							if (!String.IsNullOrEmpty (body)) {
								generatedComment.Append (body);
								generateStandardComment = false;
							}
						}
					}
					if (generateStandardComment) {
						string indent = textEditorData.Document.GetLineIndent (completionContext.TriggerLine);
						AppendSummary (generatedComment, indent, out newCursorOffset);
					}
					textEditorData.Document.EndAtomicUndo ();
					textEditorData.Document.BeginAtomicUndo ();
					textEditorData.Insert (cursor, generatedComment.ToString ());
					textEditorData.Caret.Offset = cursor + newCursorOffset;
					return null;
				}
				return null;
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
			case ' ':
				if (stateTracker.Engine.IsInsideDocLineComment || stateTracker.Engine.IsInsideOrdinaryCommentOrString)
					return null;
				result = FindExpression (dom, completionContext);
				if (result == null)
					return null;
					
					
				int tokenIndex = completionContext.TriggerOffset;
				string token = GetPreviousToken (ref tokenIndex, false);
				if (result.ExpressionContext == ExpressionContext.ObjectInitializer) {
					resolver = CreateResolver ();
					ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForObjectInitializer (textEditorData, resolver.Unit, Document.FileName, resolver.CallingType);
					IReturnType objectInitializer = ((ExpressionContext.TypeExpressionContext)exactContext).UnresolvedType;
					if (objectInitializer != null && objectInitializer.ArrayDimensions == 0 && objectInitializer.PointerNestingLevel == 0 && (token == "{" || token == ","))
						return CreateCtrlSpaceCompletionData (completionContext, result); 
				}
				if (token == "=") {
					int j = tokenIndex;
					string prevToken = GetPreviousToken (ref j, false);
					if (prevToken == "=" || prevToken == "+" || prevToken == "-") {
						token = prevToken + token;
						tokenIndex = j;
					}
				}
				switch (token) {
				case "=":
				case "==":
					result = FindExpression (dom, completionContext, tokenIndex - completionContext.TriggerOffset - 1);
					resolver = CreateResolver ();
					resolveResult = resolver.Resolve (result, location);
					if (resolveResult != null) {
						IType resolvedType = dom.GetType (resolveResult.ResolvedType);
						if (resolvedType == null) 
							return null;
						if (resolvedType.ClassType == ClassType.Enum) {
							CompletionDataList completionList = new ProjectDomCompletionDataList ();
							CompletionDataCollector cdc = new CompletionDataCollector (dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
							IReturnType returnType = new DomReturnType (resolvedType);
							bool added = false;
							foreach (IUsing u in Document.CompilationUnit.Usings) {
								foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
									if (alias.Value.ToInvariantString () == returnType.ToInvariantString ()) {
										cdc.Add (alias.Key, "md-class");
										added = true;
									}
								}
							}
							if (!added)
								cdc.Add (returnType);
							foreach (object o in CreateCtrlSpaceCompletionData (completionContext, result)) {
								MemberCompletionData memberData = o as MemberCompletionData;
								if (memberData == null || memberData.Member == null)
									continue;
								if (memberData.Member is IMember) {
									returnType = ((IMember)memberData.Member).ReturnType;
								} else if (memberData.Member is IParameter) {
									returnType = ((IParameter)memberData.Member).ReturnType;
								} else {
									returnType = ((LocalVariable)memberData.Member).ReturnType;
								}
								if (returnType != null && returnType.FullName == resolvedType.FullName)
									completionList.Add (memberData);
							}
							completionList.AutoCompleteEmptyMatch = false;
							return completionList;
						}
						
						if (resolvedType.FullName == DomReturnType.Bool.FullName) {
							CompletionDataList completionList = new ProjectDomCompletionDataList ();
							CompletionDataCollector cdc = new CompletionDataCollector (dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
							completionList.AutoCompleteEmptyMatch = false;
							cdc.Add ("true", "md-keyword");
							cdc.Add ("false", "md-keyword");
							
							foreach (object o in CreateCtrlSpaceCompletionData (completionContext, result)) {
								MemberCompletionData memberData = o as MemberCompletionData;
								if (memberData == null || memberData.Member == null)
									continue;
								IReturnType returnType = null;
								if (memberData.Member is IMember) {
									returnType = ((IMember)memberData.Member).ReturnType;
								} else if (memberData.Member is IParameter) {
									returnType = ((IParameter)memberData.Member).ReturnType;
								} else {
									returnType = ((LocalVariable)memberData.Member).ReturnType;
								}
								if (returnType != null && returnType.FullName == DomReturnType.Bool.FullName)
									completionList.Add (memberData);
							}
							return completionList;
						}
							
						if (resolvedType.ClassType == ClassType.Delegate && token == "=") {
							IMethod delegateMethod = resolvedType.Methods.First ();
							CompletionDataList completionList = new ProjectDomCompletionDataList ();
								
							completionList.Add ("delegate", "md-keyword", GettextCatalog.GetString ("Creates anonymous delegate."), "delegate {" + Document.Editor.EolMarker + stateTracker.Engine.ThisLineIndent  + TextEditorProperties.IndentString + "|" + Document.Editor.EolMarker + stateTracker.Engine.ThisLineIndent +"};");
							StringBuilder sb = new StringBuilder ("(");
							for (int k = 0; k < delegateMethod.Parameters.Count; k++) {
								if (k > 0)
									sb.Append (", ");
								IType parameterType = dom.GetType (delegateMethod.Parameters[k].ReturnType);
								IReturnType returnType = parameterType != null ? new DomReturnType (parameterType) : delegateMethod.Parameters[k].ReturnType;
								sb.Append (CompletionDataCollector.ambience.GetString (Document.CompilationUnit.ShortenTypeName (returnType, textEditorData.Caret.Line, textEditorData.Caret.Column), OutputFlags.ClassBrowserEntries | OutputFlags.UseFullName  | OutputFlags.UseFullInnerTypeName));
								sb.Append (" ");
								sb.Append (delegateMethod.Parameters[k].Name);
							}
							sb.Append (")");
							completionList.Add ("delegate" + sb, "md-keyword", GettextCatalog.GetString ("Creates anonymous delegate."), "delegate" + sb + " {" + Document.Editor.EolMarker + stateTracker.Engine.ThisLineIndent  + TextEditorProperties.IndentString + "|" + Document.Editor.EolMarker + stateTracker.Engine.ThisLineIndent +"};");
							string varName = GetPreviousToken (ref tokenIndex, false);
							varName = GetPreviousToken (ref tokenIndex, false);
							if (varName != ".") {
								varName = null;
							} else {
								List<string> names = new List<string> ();
								while (varName == ".") {
									varName = GetPreviousToken (ref tokenIndex, false);
									if (varName == "this") {
										names.Add ("handle");
									} else if (varName != null) {
										string trimmedName = varName.Trim ();
										if (trimmedName.Length == 0)
											break;
										names.Insert (0, trimmedName);
									}
									varName = GetPreviousToken (ref tokenIndex, false);
								}
								varName = String.Join ("", names.ToArray ());
								foreach (char ch in varName) {
									if (!char.IsLetterOrDigit (ch) && ch != '_') {
										varName = "";
										break;
									}
								}
							}
							completionList.Add (new EventCreationCompletionData (textEditorData, varName, resolvedType, null, sb.ToString (), resolver.CallingMember, resolvedType));
							return completionList;
						}
					}
					return null;
				case "+=":
				case "-=":
					if (stateTracker.Engine.IsInsideDocLineComment || stateTracker.Engine.IsInsideOrdinaryCommentOrString)
						return null;
					result = FindExpression (dom, completionContext, tokenIndex - completionContext.TriggerOffset);
					resolver = CreateResolver ();
					resolveResult = resolver.Resolve (result, location);
					
					if (resolveResult is MemberResolveResult) {
						MemberResolveResult mrr = resolveResult as MemberResolveResult;
						IEvent evt = mrr.ResolvedMember as IEvent;
						if (evt == null)
							return null;
						IType delegateType = resolver.SearchType (evt.ReturnType);
						if (delegateType == null || delegateType.ClassType != ClassType.Delegate)
							return null;
						CompletionDataList completionList = new ProjectDomCompletionDataList ();
						CompletionDataCollector cdc = new CompletionDataCollector (dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
						
						IType declaringType = resolver.CallingType;
						if (Document.LastErrorFreeParsedDocument != null) {
							declaringType = Document.LastErrorFreeParsedDocument.CompilationUnit.GetType (declaringType.FullName, declaringType.TypeParameters.Count);
						}
						IType typeFromDatabase = null;
						if (declaringType != null) {
							typeFromDatabase = dom.GetType (declaringType.FullName, new DomReturnType (declaringType).GenericArguments) ?? declaringType;
							bool includeProtected = DomType.IncludeProtected (dom, typeFromDatabase, resolver.CallingType);
							foreach (IType type in dom.GetInheritanceTree (typeFromDatabase)) {
								foreach (IMethod method in type.Methods) {
									if (method.IsAccessibleFrom (dom, resolver.CallingType, resolver.CallingMember, includeProtected) && MatchDelegate (delegateType, method)) {
										CompletionData data = cdc.Add (method);
										data.SetText (data.CompletionText + ";");
									}
								}
							}
						}
						if (token == "+=") {
							IMethod delegateMethod = delegateType.Methods.First ();
							completionList.Add ("delegate", "md-keyword", GettextCatalog.GetString ("Creates anonymous delegate."), "delegate {" + Document.Editor.EolMarker + stateTracker.Engine.ThisLineIndent  + TextEditorProperties.IndentString + "|" + Document.Editor.EolMarker + stateTracker.Engine.ThisLineIndent +"};");
							StringBuilder sb = new StringBuilder ("(");
							for (int k = 0; k < delegateMethod.Parameters.Count; k++) {
								if (k > 0)
									sb.Append (", ");
								IType parameterType = dom.GetType (delegateMethod.Parameters[k].ReturnType);
								IReturnType returnType = parameterType != null ? new DomReturnType (parameterType) : delegateMethod.Parameters[k].ReturnType;
								sb.Append (CompletionDataCollector.ambience.GetString (Document.CompilationUnit.ShortenTypeName (returnType, textEditorData.Caret.Line, textEditorData.Caret.Column), OutputFlags.ClassBrowserEntries | OutputFlags.UseFullName  | OutputFlags.UseFullInnerTypeName));
								sb.Append (" ");
								sb.Append (delegateMethod.Parameters[k].Name);
							}
							sb.Append (")");
							completionList.Add ("delegate" + sb, "md-keyword", GettextCatalog.GetString ("Creates anonymous delegate."), "delegate" + sb + " {" + Document.Editor.EolMarker + stateTracker.Engine.ThisLineIndent  + TextEditorProperties.IndentString + "|" + Document.Editor.EolMarker + stateTracker.Engine.ThisLineIndent +"};");
							string varName = GetPreviousToken (ref tokenIndex, false);
							varName = GetPreviousToken (ref tokenIndex, false);
							if (varName != ".") {
								varName = null;
							} else {
								List<string> names = new List<string> ();
								while (varName == ".") {
									varName = GetPreviousToken (ref tokenIndex, false);
									if (varName == "this") {
										names.Add ("handle");
									} else if (varName != null) {
										string trimmedName = varName.Trim ();
										if (trimmedName.Length == 0)
											break;
										names.Insert (0, trimmedName);
									}
									varName = GetPreviousToken (ref tokenIndex, false);
								}
								varName = String.Join ("", names.ToArray ());
								foreach (char ch in varName) {
									if (!char.IsLetterOrDigit (ch) && ch != '_') {
										varName = "";
										break;
									}
								}
							}
								
							completionList.Add (new EventCreationCompletionData (textEditorData, varName, delegateType, evt, sb.ToString (), resolver.CallingMember, typeFromDatabase));
						}
						return completionList;
					}
					return null;
				}
				return HandleKeywordCompletion (completionContext, result, tokenIndex, token);
			default:
				if ((Char.IsLetter (completionChar) || completionChar == '_') && TextEditorProperties.EnableAutoCodeCompletion
					    && !stateTracker.Engine.IsInsideDocLineComment
					    && !stateTracker.Engine.IsInsideOrdinaryCommentOrString)
				{
					char prevCh = completionContext.TriggerOffset > 2
							? textEditorData.GetCharAt (completionContext.TriggerOffset - 2)
							: '\0';
					
					char nextCh = completionContext.TriggerOffset < textEditorData.Length
							? textEditorData.GetCharAt (completionContext.TriggerOffset)
							: ' ';
					const string allowedChars = ";,[(){}+-*/%^?:&|~!<>=";
					if (!Char.IsWhiteSpace (nextCh) && allowedChars.IndexOf (nextCh) < 0)
						return null;
					if (Char.IsWhiteSpace (prevCh) || allowedChars.IndexOf (prevCh) >= 0)
					{
						result = FindExpression (dom, completionContext, -1);
						if (result == null)
							return null;
						if (IsInLinqContext (result)) {
							tokenIndex = completionContext.TriggerOffset;
							token = GetPreviousToken (ref tokenIndex, false); // token last typed
							token = GetPreviousToken (ref tokenIndex, false); // possible linq keyword ?
							triggerWordLength = 1;
							
							if (linqKeywords.Contains (token)) {
								if (token == "from") // after from no auto code completion.
									return null;
								result.Expression = "";
								return CreateCtrlSpaceCompletionData (completionContext, result);
							}
							CompletionDataList dataList = new ProjectDomCompletionDataList ();
							CompletionDataCollector col = new CompletionDataCollector (dom, dataList, Document.CompilationUnit, null, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
							foreach (string kw in linqKeywords) {
								col.Add (kw, "md-keyword");
							}
							return dataList;
						} else if (result.ExpressionContext != ExpressionContext.IdentifierExpected) {
							triggerWordLength = 1;
							bool autoSelect = true;
							int cpos;
							if ((prevCh == ',' || prevCh == '(') && GetParameterCompletionCommandOffset (out cpos)) {
								CodeCompletionContext ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
								NRefactoryParameterDataProvider provider = ParameterCompletionCommand (ctx) as NRefactoryParameterDataProvider;
								if (provider != null) {
									int i = provider.GetCurrentParameterIndex (CompletionWidget, ctx) - 1;
									if (i < provider.Methods[0].Parameters.Count) {
										IType returnType = dom.GetType (provider.Methods[0].Parameters[i].ReturnType);
										autoSelect = returnType == null || returnType.ClassType != ClassType.Delegate;
									}
								}
							}
							if (result.ExpressionContext == ExpressionContext.TypeName)
								autoSelect = false;
							CompletionDataList dataList = CreateCtrlSpaceCompletionData (completionContext, result);
							dataList.AutoSelect = autoSelect;
							return dataList;
						}
					}
				}
				break;
			}
			} catch (Exception e) {
				LoggingService.LogError ("Unexpected code completion exception." + Environment.NewLine + 
					"FileName: " + Document.FileName + Environment.NewLine + 
					"Position: line=" + completionContext.TriggerLine + " col=" + completionContext.TriggerLineOffset + Environment.NewLine + 
					"Line text: " + Document.Editor.GetLineText (completionContext.TriggerLine),
					e);
			} finally {
	//			if (timer != null)
	//				timer.Dispose ();
			}
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
		
		
		
		static string[] linqKeywords = new string[] { "from", "where", "select", "group", "into", "orderby", "join", "let", "in", "on", "equals", "by", "ascending", "descending" };
		
		int GetMemberStartPosition (IMember mem)
		{
			if (mem is IField)
				return textEditorData.Document.LocationToOffset (mem.Location.Line, mem.Location.Column);
			if (mem != null)
				return textEditorData.Document.LocationToOffset (mem.BodyRegion.Start.Line, mem.BodyRegion.Start.Column);
			return 0;
		}

		public override bool GetParameterCompletionCommandOffset (out int cpos)
		{
			// Start calculating the parameter offset from the beginning of the
			// current member, instead of the beginning of the file. 
			cpos = textEditorData.Caret.Offset - 1;
			IMember mem = Document.ParsedDocument.CompilationUnit.GetMemberAt (textEditorData.Caret.Line, textEditorData.Caret.Column);
			if (mem == null || (mem is IType))
				return false;
			int startPos = GetMemberStartPosition (mem);
			int parenDepth = 0;
			int chevronDepth = 0;
			while (cpos > startPos) {
				char c = textEditorData.GetCharAt (cpos);
				if (c == ')')
					parenDepth++;
				if (c == '>')
					chevronDepth++;
				if (parenDepth == 0 && c == '(' || chevronDepth == 0 && c == '<') {
					int p = NRefactoryParameterDataProvider.GetCurrentParameterIndex (CompletionWidget, cpos + 1, startPos);
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
		
		public ICSharpCode.NRefactory.Ast.CompilationUnit ParsedUnit { get; set; }
		NRefactoryResolver CreateResolver ()
		{
			NRefactoryResolver result = new NRefactoryResolver (dom, Document.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, textEditorData, Document.FileName);
			if (ParsedUnit != null)
				result.SetupParsedCompilationUnit (ParsedUnit);
			return result;
		}
		
		public override IParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext, char completionChar)
		{
			if (dom == null || (completionChar != '(' && completionChar != '<' && completionChar != '['))
				return null;

			if (stateTracker.Engine.IsInsideDocLineComment || stateTracker.Engine.IsInsideOrdinaryCommentOrString)
				return null;

			ExpressionResult result = FindExpression (dom, completionContext, -1);
			if (result == null)
				return null;
			

			//DomLocation location = new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset - 2);
			NRefactoryResolver resolver = CreateResolver ();

			if (result.ExpressionContext is ExpressionContext.TypeExpressionContext)
				result.ExpressionContext = new NewCSharpExpressionFinder (dom).FindExactContextForNewCompletion (textEditorData, Document.CompilationUnit, Document.FileName, resolver.CallingType) ?? result.ExpressionContext;
			
			switch (completionChar) {
			case '<':
				if (string.IsNullOrEmpty (result.Expression))
					return null;
				return new NRefactoryTemplateParameterDataProvider (textEditorData, resolver, GetUsedNamespaces (), result, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
			case '[': {
				ResolveResult resolveResult = resolver.Resolve (result, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
				if (resolveResult != null && !resolveResult.StaticResolve) {
					IType type = dom.GetType (resolveResult.ResolvedType);
					if (type != null)
						return new NRefactoryIndexerParameterDataProvider (textEditorData, type, result.Expression);
				}
				return null;
			}
			case '(': {
				ResolveResult resolveResult = resolver.Resolve (result, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
				if (resolveResult != null) {
					if (result.ExpressionContext == ExpressionContext.Attribute) {
						IReturnType returnType = resolveResult.ResolvedType;
						
						IType type = resolver.SearchType (result.Expression.Trim () + "Attribute");
						if (type == null) 
							type = resolver.SearchType (returnType);
						if (type != null && returnType != null && returnType.GenericArguments != null)
							type = dom.CreateInstantiatedGenericType (type, returnType.GenericArguments);
						return new NRefactoryParameterDataProvider (textEditorData, resolver, type);
					}
					
//					System.Console.WriteLine("resolveResult:" + resolveResult);
					
					if (result.ExpressionContext is ExpressionContext.TypeExpressionContext) {
						IReturnType returnType = resolveResult.ResolvedType ?? ((ExpressionContext.TypeExpressionContext)result.ExpressionContext).Type;
						
						IType type = resolver.SearchType (returnType);
						if (type != null && returnType.GenericArguments != null)
							type = dom.CreateInstantiatedGenericType (type, returnType.GenericArguments);
						return new NRefactoryParameterDataProvider (textEditorData, resolver, type);
					}
					
					if (resolveResult is MethodResolveResult)
						return new NRefactoryParameterDataProvider (textEditorData, resolver, resolveResult as MethodResolveResult);
					if (result.ExpressionContext == ExpressionContext.BaseConstructorCall) {
						if (resolveResult is ThisResolveResult)
							return new NRefactoryParameterDataProvider (textEditorData, resolver, resolveResult as ThisResolveResult);
						if (resolveResult is BaseResolveResult)
							return new NRefactoryParameterDataProvider (textEditorData, resolver, resolveResult as BaseResolveResult);
					}
					IType resolvedType = resolver.SearchType (resolveResult.ResolvedType);
					if (resolvedType != null && resolvedType.ClassType == ClassType.Delegate) {
						return new NRefactoryParameterDataProvider (textEditorData, result.Expression, resolvedType);
					}
				}
				break;
			} }
			return null;
		}
		
		List<string> GetUsedNamespaces ()
		{
			List<string> result = new List<string> ();
			result.Add ("");
			if (Document.CompilationUnit != null && Document.CompilationUnit.Usings != null) {
				foreach (IUsing u in Document.CompilationUnit.Usings) {
					if (u.Namespaces == null)
						continue;
					foreach (string ns in u.Namespaces) {
						result.Add (ns);
					}
				}
			}
			return result;
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
		
		public ICompletionDataList HandleKeywordCompletion (CodeCompletionContext completionContext, ExpressionResult result, int wordStart, string word)
		{
			if (stateTracker.Engine.IsInsideDocLineComment || stateTracker.Engine.IsInsideOrdinaryCommentOrString)
				return null;
			DomLocation location = new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset);
			switch (word) {
			case "using":
				if (result.ExpressionContext != ExpressionContext.NamespaceNameExcepted)
					return null;
				return CreateCompletionData (location, new NamespaceResolveResult (""), result, null);
			case "namespace":
				result.ExpressionContext = ExpressionContext.NamespaceNameExcepted;
				return CreateCompletionData (location, new NamespaceResolveResult (""), result, null);
			case "case":
				return CreateCaseCompletionData (location, result);
			case ",":
			case ":":
				if (result.ExpressionContext == ExpressionContext.InheritableType) {
					IType cls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
					CompletionDataList completionList = new ProjectDomCompletionDataList ();
					List<string> namespaceList = GetUsedNamespaces ();
					CSharpTextEditorCompletion.CompletionDataCollector col = new CSharpTextEditorCompletion.CompletionDataCollector (dom, completionList, Document.CompilationUnit, null, location);
					bool isInterface = false;
					HashSet<string> baseTypeNames = new HashSet<string> ();
					if (cls != null) {
						baseTypeNames.Add (cls.Name);
						if (cls.ClassType == ClassType.Struct)
							isInterface = true;
					}
					int tokenIndex = completionContext.TriggerOffset;

										// Search base types " : [Type1, ... ,TypeN,] <Caret>"
					string token = null;
					do {
						token = GetPreviousToken (ref tokenIndex, false);
						if (string.IsNullOrEmpty (token))
							break;
						token = token.Trim ();
						if (Char.IsLetterOrDigit (token[0]) || token[0] == '_') {
							IType baseType = dom.SearchType (Document.CompilationUnit, token);
							if (baseType != null) {
								if (baseType.ClassType != ClassType.Interface)
									isInterface = true;
								baseTypeNames.Add (baseType.Name);
							}
						}
					} while (token != ":");
					foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
						IType type = o as IType;
						if (type != null && (type.IsStatic || type.IsSealed || baseTypeNames.Contains (type.Name) || isInterface && type.ClassType != ClassType.Interface)) {
							continue;
						}
						if (o is Namespace && !namespaceList.Any (ns => ns.StartsWith (((Namespace)o).FullName)))
							continue;
						col.Add (o);
					}
					// Add inner classes
					Stack<IType> innerStack = new Stack<IType> ();
					innerStack.Push (cls);
					while (innerStack.Count > 0) {
						IType curType = innerStack.Pop ();
						if (curType == null)
							continue;
						foreach (IType innerType in curType.InnerTypes) {
							if (innerType != cls)
								// don't add the calling class as possible base type
								col.Add (innerType);
						}
						if (curType.DeclaringType != null)
							innerStack.Push (curType.DeclaringType);
					}
					return completionList;
				}
				break;
			case "is":
			case "as":
				
				{
					CompletionDataList completionList = new ProjectDomCompletionDataList ();
					ExpressionResult expressionResult = FindExpression (dom, completionContext, wordStart - textEditorData.Caret.Offset);
					NRefactoryResolver resolver = CreateResolver ();
					ResolveResult resolveResult = resolver.Resolve (expressionResult, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
					if (resolveResult != null && resolveResult.ResolvedType != null) {
						CompletionDataCollector col = new CompletionDataCollector (dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
						IType foundType = null;
						if (word == "as") {
							ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForAsCompletion (textEditorData, Document.CompilationUnit, Document.FileName, resolver.CallingType);
							if (exactContext is ExpressionContext.TypeExpressionContext) {
								foundType = resolver.SearchType (((ExpressionContext.TypeExpressionContext)exactContext).Type);
								AddAsCompletionData (col, foundType);
							}
						}
					
						if (foundType == null)
							foundType = resolver.SearchType (resolveResult.ResolvedType);
					
						if (foundType != null) {
							if (foundType.ClassType == ClassType.Interface)
								foundType = resolver.SearchType (DomReturnType.Object);
						
							foreach (IType type in dom.GetSubclasses (foundType)) {
								if (type.IsSpecialName || type.Name.StartsWith ("<"))
									continue;
								AddAsCompletionData (col, type);
							}
						}
						List<string> namespaceList = GetUsedNamespaces ();
						foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
							if (o is IType) {
								IType type = (IType)o;
								if (type.ClassType != ClassType.Interface || type.IsSpecialName || type.Name.StartsWith ("<"))
									continue;
//								if (foundType != null && !dom.GetInheritanceTree (foundType).Any (x => x.FullName == type.FullName))
//									continue;
								AddAsCompletionData (col, type);
								continue;
							}
							if (o is Namespace)
								continue;
							col.Add (o);
						}
						return completionList;
					}
					result.ExpressionContext = ExpressionContext.TypeName;
					return CreateCtrlSpaceCompletionData (completionContext, result);
				}
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
				IType overrideCls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
				if (overrideCls == null)
					overrideCls = overrideCls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new DomLocation (completionContext.TriggerLine - 1, 1));
				if (overrideCls != null && (overrideCls.ClassType == ClassType.Class || overrideCls.ClassType == ClassType.Struct)) {
					string modifiers = textEditorData.GetTextBetween (firstMod, wordStart);
					return GetOverrideCompletionData (completionContext, overrideCls, modifiers);
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
				overrideCls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
				if (overrideCls != null && (overrideCls.ClassType == ClassType.Class || overrideCls.ClassType == ClassType.Struct)) {
					string modifiers = textEditorData.GetTextBetween (firstMod, wordStart);
					return GetPartialCompletionData (completionContext, overrideCls, modifiers);
				}
				return null;
				
			case "new":
				IType callingType = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new DomLocation (textEditorData.Caret.Line, textEditorData.Caret.Column));
				ExpressionContext newExactContext = new NewCSharpExpressionFinder (dom).FindExactContextForNewCompletion (textEditorData, Document.CompilationUnit, Document.FileName, callingType);
				if (newExactContext is ExpressionContext.TypeExpressionContext)
					return CreateTypeCompletionData (location, callingType, newExactContext, ((ExpressionContext.TypeExpressionContext)newExactContext).Type, ((ExpressionContext.TypeExpressionContext)newExactContext).UnresolvedType);
				if (newExactContext == null) {
					int j = completionContext.TriggerOffset - 4;
					string token = GetPreviousToken (ref j, true);
					string yieldToken = GetPreviousToken (ref j, true);
					if (token == "return") {
						NRefactoryResolver resolver = CreateResolver ();
						resolver.SetupResolver (new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
						IReturnType returnType = resolver.CallingMember.ReturnType;
						if (yieldToken == "yield" && returnType.GenericArguments.Count > 0)
							returnType = returnType.GenericArguments[0];
						if (resolver.CallingMember != null)
							return CreateTypeCompletionData (location, callingType, newExactContext, null, returnType);
					}
				}
				
				return CreateCtrlSpaceCompletionData (completionContext, null);
			case "if":
			case "elif":
				if (stateTracker.Engine.IsInsidePreprocessorDirective) 
					return GetDefineCompletionData ();
				return null;
			case "yield":
				CompletionDataList yieldDataList = new CompletionDataList ();
				yieldDataList.DefaultCompletionString = "return";
				yieldDataList.Add ("break", "md-keyword");
				yieldDataList.Add ("return", "md-keyword");
				return yieldDataList;
			case "where":
				CompletionDataList whereDataList = new CompletionDataList ();
				NRefactoryResolver constraintResolver = CreateResolver ();
				constraintResolver.SetupResolver (new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
				if (constraintResolver.CallingMember is IMethod) {
					foreach (ITypeParameter tp in ((IMethod)constraintResolver.CallingMember).TypeParameters) {
						whereDataList.Add (tp.Name, "md-keyword");
					}
				} else {
					if (constraintResolver.CallingType != null) {
						foreach (ITypeParameter tp in constraintResolver.CallingType.TypeParameters) {
							whereDataList.Add (tp.Name, "md-keyword");
						}
					}
				}

				return whereDataList;
			}
			if (IsInLinqContext (result)) {
				if (linqKeywords.Contains (word)) {
					if (word == "from") // after from no auto code completion.
						return null;
					result.Expression = "";
					return CreateCtrlSpaceCompletionData (completionContext, result);
				}
				CompletionDataList dataList = new ProjectDomCompletionDataList ();
				CompletionDataCollector col = new CompletionDataCollector (dom, dataList, Document.CompilationUnit, null, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
				foreach (string kw in linqKeywords) {
					col.Add (kw, "md-keyword");
				}
				return dataList;
			}
			return null;
		}
		
		string GetPreviousToken (ref int i, bool allowLineChange)
		{
			char c;
			
			if (i <= 0)
				return null;
			
			do {
				c = textEditorData.GetCharAt (--i);
			} while (i > 0 && char.IsWhiteSpace (c) && (allowLineChange ? true : c != '\n'));
			
			if (i == 0)
				return null;
			
			if (!char.IsLetterOrDigit (c))
				return new string (c, 1);
			
			int endOffset = i + 1;
			
			do {
				c = textEditorData.GetCharAt (i - 1);
				if (!(char.IsLetterOrDigit (c) || c == '_'))
					break;
				
				i--;
			} while (i > 0);
			
			return textEditorData.GetTextBetween (i, endOffset);
		}
		
		public override ICompletionDataList CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			if (stateTracker.Engine.IsInsidePreprocessorDirective || stateTracker.Engine.IsInsideOrdinaryCommentOrString || stateTracker.Engine.IsInsideDocLineComment)
				return null;
			int pos = completionContext.TriggerOffset;
			if (pos > 0) {
				char ch = textEditorData.GetCharAt (pos - 1);
				int triggerWordLength = 0; 
				tryToForceCompletion = true;
				ICompletionDataList cp = this.HandleCodeCompletion (completionContext, ch, ref triggerWordLength);
				tryToForceCompletion = false;
				if (cp != null) {
					((CompletionDataList)cp).AutoCompleteUniqueMatch = true;
					return cp;
				}
			}
			ExpressionResult result = FindExpression (dom, completionContext);
						
			if (result == null)
				return null;
			
			CompletionDataList completionList;
/*			if (result.ExpressionContext == ExpressionContext.IdentifierExpected) {
				NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (dom, Document.CompilationUnit,
				                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
				                                                                                Editor,
				                                                                                Document.FileName);
				
				ResolveResult resolveResult = resolver.Resolve (result, new DomLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
				completionList = (CompletionDataList)CreateCompletionData (resolveResult, result, resolver);
			} else {
			}*/
			completionList = CreateCtrlSpaceCompletionData (completionContext, result);
			if (completionList != null)
				completionList.AutoCompleteUniqueMatch = true;
			return completionList;
		}
		
		public class CompletionDataCollector
		{
			public CompletionDataList CompletionList {
				get;
				private set;
			}
			
			Dictionary<string, List<MemberCompletionData>> data = new Dictionary<string, List<MemberCompletionData>> ();
			HashSet<string> namespacesInScope = new HashSet<string> ();
			internal static CSharpAmbience ambience = new CSharpAmbience ();
//			DomLocation location;
			ICompilationUnit unit;
			IType declaringType;
			List<IType> inheritanceTree;
			ProjectDom dom;
			
			bool prefixIsAlias;
			
			string namePrefix = "";
			public string NamePrefix {
				get {
					return namePrefix;
				}
				set {
					namePrefix = value != null ? value.Trim () : string.Empty;
					
					// Check if the name prefix is a type/namespace alias, in which case
					// we don't have to show full names
					prefixIsAlias = false;
					if (unit != null) {
						foreach (IUsing u in unit.Usings) {
							foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
								if (alias.Key == namePrefix || alias.Key + "::" == namePrefix) {
									prefixIsAlias = true;
									break;
								}
							}
						}
					}
				}
			}
			
			public bool FullyQualify { get; set; }
			
			bool hideExtensionParameter = true;
			public bool HideExtensionParameter {
				get {
					return hideExtensionParameter;
				}
				set {
					hideExtensionParameter = value;
				}
			}
			public class NegateKeyHandler : ICompletionKeyHandler
			{
				public bool ProcessKey (CompletionListWindow window, Gdk.Key key, char keyChar, Gdk.ModifierType modifier, out KeyActions keyAction)
				{
					if (keyChar != '!') {
						keyAction = KeyActions.None;
						return false;
					}
					
					keyAction = KeyActions.CloseWindow;
					
					CodeCompletionContext ctx = window.CodeCompletionContext;
					int offset = ctx.TriggerOffset;
					
					while (offset > 0) {
						char ch = window.CompletionWidget.GetChar (offset - 1);
						if (ch != '.') 
							break;
						offset--;
						while (offset > 0) {
							ch = window.CompletionWidget.GetChar (offset - 1);
							if (ch != '_' && !Char.IsLetterOrDigit (ch))
								break;
							offset--;
						}
					}
					
					keyAction = KeyActions.CloseWindow | KeyActions.Ignore | KeyActions.Complete;
					window.CompletionWidget.Replace (offset, 0, "!");
					
					ctx.TriggerOffset++;
					ctx.TriggerLineOffset++;
					
					return true;
				}
			}
			
			public CompletionDataCollector (ProjectDom dom, CompletionDataList completionList, ICompilationUnit unit, IType declaringType, DomLocation location)
			{
				this.CompletionList = completionList;
				this.unit = unit;
				this.dom = dom;
				this.FullyQualify = false;
//				this.location = location;
				this.declaringType = declaringType;
				completionList.AddKeyHandler (new NegateKeyHandler ());
				// Get a list of all namespaces in scope
				if (unit != null) {
					foreach (IUsing u in unit.Usings) {
						if (!u.IsFromNamespace || u.Region.Contains (location)) {
							foreach (string ns in u.Namespaces)
								namespacesInScope.Add (ns);
						}
					}
				}
			}
			
			Dictionary<IType, CompletionCategory> completionCategories = new Dictionary<IType, CompletionCategory> ();
			
			class TypeCompletionCategory : CompletionCategory
			{
				public IType Type {
					get;
					private set;
				}
				
				public TypeCompletionCategory (IType type) : base (type.FullName, type.StockIcon)
				{
					this.Type = type;
				}
				
				public override int CompareTo (CompletionCategory other)
				{
					TypeCompletionCategory compareCategory = other as TypeCompletionCategory;
					if (compareCategory == null)
						return 1;
					
					if (Type.DecoratedFullName == compareCategory.Type.DecoratedFullName)
						return 0;
					
					// System.Object is always the smallest
					if (Type.DecoratedFullName == DomReturnType.Object.DecoratedFullName) 
						return -1;
					if (compareCategory.Type.DecoratedFullName == DomReturnType.Object.DecoratedFullName)
						return 1;
					
					if (Type.SourceProjectDom != null) {
						if (Type.SourceProjectDom.GetInheritanceTree (Type).Any (t => t != null && t.DecoratedFullName == compareCategory.Type.DecoratedFullName))
							return 1;
						return -1;
					}
					
					// source project dom == null - try to make the opposite comparison
					if (compareCategory.Type.SourceProjectDom != null && compareCategory.Type.SourceProjectDom.GetInheritanceTree (Type).Any (t => t != null && t.DecoratedFullName == Type.DecoratedFullName))
						return -1;
					return 1;
				}
			}
			
			internal CompletionCategory GetCompletionCategory (IType type)
			{
				if (type == null)
					return null;
				if (!completionCategories.ContainsKey (type)) {
					completionCategories[type] = new TypeCompletionCategory (type);
				}
				return completionCategories[type];
			}
			
			MemberCompletionData AddMemberCompletionData (object member, OutputFlags flags)
			{
				MemberCompletionData newData = new MemberCompletionData (member as INode, flags);
				newData.HideExtensionParameter = HideExtensionParameter;
				string memberKey = newData.CompletionText;
				if (member is IMember) {
					newData.CompletionCategory = GetCompletionCategory (((IMember)member).DeclaringType);
				}
				List<MemberCompletionData> existingData;
				if (data.TryGetValue (memberKey, out existingData)) {
					if (existingData == null)
						return null;
					IBaseMember a = member as IBaseMember;
					foreach (MemberCompletionData md in existingData) {
						IBaseMember b = md.Member as IBaseMember;
						if (a == null || b == null || a.MemberType == b.MemberType) {
							md.AddOverload (newData);
							newData = null;
							break;
						} 
					}
					if (newData != null) {
						CompletionList.Add (newData);
						data[memberKey].Add (newData);
					}
				} else {
					CompletionList.Add (newData);
					data[memberKey] = new List<MemberCompletionData> ();
					data[memberKey].Add (newData);
				}
				return newData;
			}
			
			public CompletionData Add (string name, string icon)
			{
				if (data.ContainsKey (name))
					return null;
				data.Add (name, null);
				
				return CompletionList.Add (name, icon);
			}
			
			public CompletionData Add (object obj)
			{
				return Add (obj, OutputFlags.None);
			}
			
			public CompletionData Add (object obj, OutputFlags additionalFlags)
			{
				Namespace ns = obj as Namespace;
				if (ns != null) {
					if (data.ContainsKey (ns.Name))
						return null;
					data.Add (ns.Name, null);
					return CompletionList.Add (ns.Name, ns.StockIcon, ns.Documentation);
				}
				
				IReturnType rt = obj as IReturnType;
				if (rt != null) {
					OutputFlags flags = OutputFlags.ClassBrowserEntries | OutputFlags.HideArrayBrackets;
					bool foundNamespace = IsNamespaceInScope (rt.Namespace);
					if (FullyQualify || !foundNamespace && (NamePrefix.Length == 0 || !rt.Namespace.StartsWith (NamePrefix)) && !rt.Namespace.EndsWith ("." + NamePrefix))
						flags |= OutputFlags.UseFullName;
					IType resolvedType = dom.GetType (rt);
					if (inheritanceTree == null && declaringType != null)
						inheritanceTree = new List<IType>(dom.GetInheritanceTree (declaringType));
					if (resolvedType != null && resolvedType.DeclaringType != null && inheritanceTree != null && inheritanceTree.Any (t => resolvedType.DeclaringType.DecoratedFullName == t.DecoratedFullName)) {
						return CompletionList.Add (rt.Name, "md-class");
					}
					string returnTypeString = ambience.GetString (rt, flags);
					if (data.ContainsKey (returnTypeString))
						return null;
					data.Add (returnTypeString, null);
					return CompletionList.Add (returnTypeString, "md-class");
				}
				
				IMember member = obj as IMember;
				if (member != null && !String.IsNullOrEmpty (member.Name)) {
					OutputFlags flags = OutputFlags.IncludeGenerics | OutputFlags.HideArrayBrackets | additionalFlags;
					
					return AddMemberCompletionData (member, flags);
				}
				if (obj is IParameter || obj is LocalVariable)
					AddMemberCompletionData (obj, OutputFlags.IncludeParameterName);
				
				if (obj is string) {
					string str = (string)obj;
					if (data.ContainsKey (str))
						return null;
					data.Add (str, null);
					return CompletionList.Add (str, "md-literal");
				}
				return null;
			}
		
			internal bool IsNamespaceInScope (string ns)
			{
				if (prefixIsAlias)
					return true;
				return namespacesInScope.Contains (ns);
			}
		}
		
		ICompletionDataList CreateCompletionData (DomLocation location, ResolveResult resolveResult, 
		                                          ExpressionResult expressionResult, NRefactoryResolver resolver)
		{
			if (resolveResult == null || expressionResult == null || dom == null)
				return null;
			CompletionDataList result = new ProjectDomCompletionDataList ();
			IEnumerable<object> objects = resolveResult.CreateResolveResult (dom, resolver != null ? resolver.CallingMember : null);
			CompletionDataCollector col = new CompletionDataCollector (dom, result, Document.CompilationUnit, resolver != null ? resolver.CallingType : null, location);
			col.HideExtensionParameter = !resolveResult.StaticResolve;
			col.NamePrefix = expressionResult.Expression;
			if (objects != null) {
				foreach (object obj in objects) {
					if (expressionResult.ExpressionContext != null && expressionResult.ExpressionContext.FilterEntry (obj))
						continue;
					if (expressionResult.ExpressionContext == ExpressionContext.NamespaceNameExcepted && !(obj is Namespace))
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

		void AddVirtuals (CodeCompletionContext ctx, Dictionary<string, bool> alreadyInserted, CompletionDataList completionList, IType type, string modifiers, IReturnType curType)
		{
			if (curType == null)
				return;
			IType searchType = dom.SearchType ((MonoDevelop.Projects.Dom.INode)type ?? Document.CompilationUnit, curType);
			//System.Console.WriteLine("Add Virtuals for:" + searchType + " / " + curType);
			if (searchType == null)
				return;
			bool isInterface      = type.ClassType == ClassType.Interface;
			bool includeOverriden = false;
		
			int declarationBegin = ctx.TriggerOffset;
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
			CompletionDataCollector col = new CompletionDataCollector (dom, completionList, Document.CompilationUnit, searchType, DomLocation.Empty);
			
			List<IType> inheritanceTree = new List<IType> (this.dom.GetInheritanceTree (searchType));
			inheritanceTree.Sort ((l, r) => l.ClassType == r.ClassType ? 0 : (l.ClassType == ClassType.Interface ? 1 : (r.ClassType == ClassType.Interface ? -1 : 0)));
			foreach (IType t in inheritanceTree) {
				foreach (IMember m in t.Members) {
					if (!m.IsAccessibleFrom (dom, type, type, true) || m.IsSpecialName)
						continue;
					//System.Console.WriteLine ("scan:" + m);
					//if (m.IsSpecialName || (m.IsInternal && !m.IsProtectedOrInternal) || && searchType.SourceProject != Document.Project)
					//	continue;
					if (t.ClassType == ClassType.Interface || (isInterface || m.IsVirtual || m.IsAbstract) && !m.IsSealed && (includeOverriden || !type.HasOverriden (m))) {
						// filter out the "Finalize" methods, because finalizers should be done with destructors.
						if (m is IMethod && m.Name == "Finalize")
							continue;
					
						//System.Console.WriteLine("add");
						NewOverrideCompletionData data = new NewOverrideCompletionData (dom, textEditorData, declarationBegin, type, m);
						string text = CompletionDataCollector.ambience.GetString (m, OutputFlags.ClassBrowserEntries);
						// check if the member is already implemented
						bool foundMember = false;
						foreach (IMember member in type.Members) {
							if (text == CompletionDataCollector.ambience.GetString (member, OutputFlags.ClassBrowserEntries)) {
								foundMember = true;
								break;
							}
						}
						
						if (!foundMember && !alreadyInserted.ContainsKey (text)) {
							alreadyInserted[text] = true;
							data.CompletionCategory = col.GetCompletionCategory (t);
							completionList.Add (data);
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
		
		CompletionDataList CreateTypeCompletionData (DomLocation location, IType callingType, ExpressionContext context, IReturnType returnType, IReturnType returnTypeUnresolved)
		{
			CompletionDataList result = new ProjectDomCompletionDataList ();
			// "var o = new " needs special treatment.
			if (returnType == null && returnTypeUnresolved != null && returnTypeUnresolved.FullName == "var")
				returnType = returnTypeUnresolved = DomReturnType.Object;

			//	ExpressionContext.TypeExpressionContext tce = context as ExpressionContext.TypeExpressionContext;

			CompletionDataCollector col = new CompletionDataCollector (dom, result, Document.CompilationUnit, callingType, location);
			IType type = null;
			if (returnType != null)
				type = dom.GetType (returnType);
			if (type == null)
				type = dom.SearchType ((MonoDevelop.Projects.Dom.INode)Document.CompilationUnit ?? callingType, returnTypeUnresolved);
			
			if (type == null || !(type.IsAbstract || type.ClassType == ClassType.Interface)) {
				if (type == null || type.ConstructorCount == 0 || type.Methods.Any (c => c.IsConstructor && c.IsAccessibleFrom (dom, callingType, type, callingType != null && dom.GetInheritanceTree (callingType).Any (x => x.FullName == type.FullName)))) {
					if (returnTypeUnresolved != null) {
						col.FullyQualify = true;
						CompletionData unresovedCompletionData = col.Add (returnTypeUnresolved);
						col.FullyQualify = false;
						result.DefaultCompletionString = StripGenerics (unresovedCompletionData.CompletionText);
					} else {
						CompletionData unresovedCompletionData = col.Add (returnType);
						result.DefaultCompletionString = StripGenerics (unresovedCompletionData.CompletionText);
					}
				}
			}
			//				if (tce != null && tce.Type != null) {
			//					result.DefaultCompletionString = StripGenerics (col.AddCompletionData (result, tce.Type).CompletionString);
			//				} 
			//			else {
			//			}

			if (type == null)
				return result;
			HashSet<string> usedNamespaces = new HashSet<string> (GetUsedNamespaces ());
			if (type.FullName == DomReturnType.Object.FullName) 
				AddPrimitiveTypes (col);
			foreach (IType curType in dom.GetSubclasses (type)) {
				if (context != null && context.FilterEntry (curType))
					continue;
				if ((curType.TypeModifier & TypeModifier.HasOnlyHiddenConstructors) == TypeModifier.HasOnlyHiddenConstructors)
					continue;
				if (usedNamespaces.Contains (curType.Namespace)) {
					if (curType.ConstructorCount > 0) {
						if (!(curType.Methods.Any (c => c.IsConstructor && c.IsAccessibleFrom (dom, curType, callingType, callingType != null && dom.GetInheritanceTree (callingType).Any (x => x.FullName == curType.FullName)))))
							continue;
					}
					col.Add (curType);
				} else {
					string nsName = curType.Namespace;
					int idx = nsName.IndexOf ('.');
					if (idx >= 0)
						nsName = nsName.Substring (0, idx);
					col.Add (new Namespace (nsName));
				}
			}
			
			// add aliases
			if (returnType != null) {
				foreach (IUsing u in Document.CompilationUnit.Usings) {
					foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
						if (alias.Value.ToInvariantString () == returnType.ToInvariantString ())
							result.Add (alias.Key, "md-class");
					}
				}
			}
			return result;
		}
		
		CompletionDataList GetOverrideCompletionData (CodeCompletionContext ctx, IType type, string modifiers)
		{
			CompletionDataList result = new ProjectDomCompletionDataList ();
			Dictionary<string, bool> alreadyInserted = new Dictionary<string, bool> ();
			bool addedVirtuals = false;
			foreach (IReturnType baseType in type.BaseTypes) {
				AddVirtuals (ctx, alreadyInserted, result, type, modifiers, baseType);
				addedVirtuals = true;
			}
			if (!addedVirtuals)
				AddVirtuals (ctx, alreadyInserted, result, type, modifiers, DomReturnType.Object);
			return result;
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
					if (part.Location == type.Location && part.CompilationUnit.FileName == type.CompilationUnit.FileName)
						continue;
					foreach (IMethod method in part.Methods) {
						if (method.IsPartial && method.BodyRegion.End.Line == 0 && !ContainsDeclaration (type, method)) {
							methods.Add (method);
						}
					}
				}

				// now filter all methods that are implemented in the compound class
				foreach (IType part in partialType.Parts) {
					if (part.Location == type.Location && part.CompilationUnit.FileName == type.CompilationUnit.FileName)
						continue;
					for (int i = 0; i < methods.Count; i++) {
						IMethod curMethod = methods[i];
						IMethod method = GetDeclaration (part, curMethod);
						if (method != null && method.BodyRegion.End.Line != 0) {
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
		
		static string[] primitiveTypes = new string [] { "void", "object", "bool", "byte", "sbyte", "char", "short", "int", "long", "ushort", "uint", "ulong", "float", "double", "decimal", "string"};
		static void AddPrimitiveTypes (CompletionDataCollector col)
		{
			foreach (string primitiveType in primitiveTypes) {
				col.Add (primitiveType, "md-keyword");
			}
		}
		
		static void AddNRefactoryKeywords (CompletionDataCollector col, System.Collections.BitArray keywords)
		{
			for (int i = 0; i < keywords.Length; i++) {
				if (keywords[i]) {
					string keyword = ICSharpCode.NRefactory.Parser.CSharp.Tokens.GetTokenString (i);
					if (keyword.IndexOf ('<') >= 0)
						continue;
					col.Add (keyword, "md-keyword");
				}
			}
		}
		
		CompletionDataList CreateCtrlSpaceCompletionData (CodeCompletionContext ctx, ExpressionResult expressionResult)
		{
			NRefactoryResolver resolver = CreateResolver ();
			
			DomLocation cursorLocation = new DomLocation (ctx.TriggerLine, ctx.TriggerLineOffset);
			resolver.SetupResolver (cursorLocation);
			CompletionDataList result = new ProjectDomCompletionDataList ();
			CompletionDataCollector col = new CompletionDataCollector (dom, result, Document.CompilationUnit, resolver.CallingType, cursorLocation);
			if (expressionResult == null) {
				AddPrimitiveTypes (col);
				resolver.AddAccessibleCodeCompletionData (ExpressionContext.Global, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.TypeDeclaration) {
				AddPrimitiveTypes (col);
				AddNRefactoryKeywords (col, ICSharpCode.NRefactory.Parser.CSharp.Tokens.TypeLevel);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.InterfaceDeclaration) {
				AddPrimitiveTypes (col);
				AddNRefactoryKeywords (col, ICSharpCode.NRefactory.Parser.CSharp.Tokens.InterfaceLevel);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.MethodBody) {
				col.Add ("global", "md-keyword");
				col.Add ("var", "md-keyword");
				AddNRefactoryKeywords (col, ICSharpCode.NRefactory.Parser.CSharp.Tokens.StatementStart);
				AddPrimitiveTypes (col);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.InterfacePropertyDeclaration) {
				col.Add ("get", "md-keyword");
				col.Add ("set", "md-keyword");
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
				AddNRefactoryKeywords (col, ICSharpCode.NRefactory.Parser.CSharp.Tokens.InPropertyDeclaration);
			} else if (expressionResult.ExpressionContext == ExpressionContext.EventDeclaration) {
				col.Add ("add", "md-keyword");
				col.Add ("remove", "md-keyword");
			} //else if (expressionResult.ExpressionContext == ExpressionContext.FullyQualifiedType) {} 
			else if (expressionResult.ExpressionContext == ExpressionContext.Default) {
				col.Add ("global", "md-keyword");
				col.Add ("var", "md-keyword");
				AddPrimitiveTypes (col);
				AddNRefactoryKeywords (col, ICSharpCode.NRefactory.Parser.CSharp.Tokens.ExpressionStart);
				AddNRefactoryKeywords (col, ICSharpCode.NRefactory.Parser.CSharp.Tokens.ExpressionContent);
				resolver.AddAccessibleCodeCompletionData (expressionResult.ExpressionContext, col);
			} else if (expressionResult.ExpressionContext == ExpressionContext.Global) {
				AddNRefactoryKeywords (col, ICSharpCode.NRefactory.Parser.CSharp.Tokens.GlobalLevel);
				CodeTemplateService.AddCompletionDataForMime ("text/x-csharp", result);
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
						foreach (IProperty property in type.Properties) {
							col.Add (property);
						}
					}
				}
			} else if (expressionResult.ExpressionContext == ExpressionContext.IdentifierExpected) {
				if (!string.IsNullOrEmpty (expressionResult.Expression))
					expressionResult.Expression = expressionResult.Expression.Trim ();
				MemberResolveResult resolveResult = resolver.Resolve (expressionResult, cursorLocation) as MemberResolveResult;
				if (resolveResult != null && resolveResult.ResolvedMember == null && resolveResult.ResolvedType != null) {
					string name = CSharpAmbience.NetToCSharpTypeName (resolveResult.ResolvedType.FullName);
					if (name != resolveResult.ResolvedType.FullName) {
						col.Add (Char.ToLower (name[0]).ToString (), "md-field");
					} else {
						name = resolveResult.ResolvedType.Name;
						List<string> names = new List<string> ();
						int lastNameStart = 0;
						for (int i = 1; i < name.Length; i++) {
							if (Char.IsUpper (name[i])) {
								names.Add (name.Substring (lastNameStart, i - lastNameStart));
								lastNameStart = i;
							}
						}
						names.Add (name.Substring (lastNameStart, name.Length - lastNameStart));
						
						StringBuilder possibleName = new StringBuilder ();
						for (int i = 0; i < names.Count; i++) {
							possibleName.Length  = 0;
							for (int j = i; j < names.Count; j++) {
								if (string.IsNullOrEmpty (names[j]))
									continue;
								if (j == i) 
									names[j] = Char.ToLower (names[j][0]) + names[j].Substring (1);
								possibleName.Append (names[j]);
							}
							if (possibleName.Length > 0)
								col.Add (possibleName.ToString (), "md-field");
						}
						result.IsSorted = true;
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
		ICompletionDataList CreateCaseCompletionData (DomLocation location, ExpressionResult expressionResult)
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
						if (t.SearchMember (type.Name, true).Any (m => m.MemberType != MemberType.Type)) {
							flags |= OutputFlags.UseFullName;
							break;
						}
					}
				}
//				if (!foundType && (NamePrefix.Length == 0 || !type.Namespace.StartsWith (NamePrefix)) && !type.Namespace.EndsWith ("." + NamePrefix) && type.DeclaringType == null && NamePrefix != null && !NamePrefix.Contains ("::"))
//					flags |= OutputFlags.UseFullName;
				CompletionDataCollector cdc = new CompletionDataCollector (dom, result, Document.CompilationUnit, resolver.CallingType, location);
				cdc.Add (type, flags);
			}
			return result;
		}
		
		class SwitchFinder : ICSharpCode.NRefactory.Visitors.AbstractAstVisitor
		{
			
			ICSharpCode.NRefactory.Ast.SwitchStatement switchStatement = null;
			
			public ICSharpCode.NRefactory.Ast.SwitchStatement SwitchStatement {
				get {
					return this.switchStatement;
				}
			}
			
			public SwitchFinder (DomLocation location)
			{
				//this.location = new ICSharpCode.NRefactory.Location (location.Column, location.Line);
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
		CompletionDataList GetDefineCompletionData ()
		{
			if (Document.Project == null)
				return null;

			Dictionary<string, string> symbols = new Dictionary<string, string> ();
			CompletionDataList cp = new ProjectDomCompletionDataList ();
			foreach (DotNetProjectConfiguration conf in Document.Project.Configurations) {
				CSharpCompilerParameters cparams = conf.CompilationParameters as CSharpCompilerParameters;
				if (cparams != null) {
					string[] syms = cparams.DefineSymbols.Split (';');
					foreach (string s in syms) {
						string ss = s.Trim ();
						if (ss.Length > 0 && !symbols.ContainsKey (ss)) {
							symbols [ss] = ss;
							cp.Add (ss, "md-literal");
						}
					}
				}
			}
			return cp;
		}
		
		CompletionDataList GetDirectiveCompletionData ()
		{
			CompletionDataList cp = new CompletionDataList ();
			string lit = "md-literal";
			cp.Add ("if", lit);
			cp.Add ("else", lit);
			cp.Add ("elif", lit);
			cp.Add ("endif", lit);
			cp.Add ("define", lit);
			cp.Add ("undef", lit);
			cp.Add ("warning", lit);
			cp.Add ("error", lit);
			cp.Add ("pragma", lit);
			cp.Add ("line", lit);
			cp.Add ("line hidden", lit);
			cp.Add ("line default", lit);
			cp.Add ("region", lit);
			cp.Add ("endregion", lit);
			return cp;
		}
		#endregion
		
		#region Xml Comments
		
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
			CSharpAmbience ambience = new CSharpAmbience ();
			if (method.Parameters != null) {
				foreach (IParameter para in method.Parameters) {
					builder.Append (Environment.NewLine);
					builder.Append (indent);
					builder.Append ("/// <param name=\"");
					builder.Append (para.Name);
					builder.Append ("\">\n");
					builder.Append (indent);
					builder.Append ("/// A <see cref=\"");
					builder.Append (ambience.GetString (para.ReturnType, OutputFlags.ClassBrowserEntries | OutputFlags.UseFullName | OutputFlags.UseNETTypeNames));
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
				builder.Append (ambience.GetString (method.ReturnType, OutputFlags.ClassBrowserEntries | OutputFlags.UseFullName | OutputFlags.UseNETTypeNames));
				builder.Append ("\"/>\n");
				builder.Append (indent);
				builder.Append ("/// </returns>");
			}
		}
		
		void AppendPropertyComment (StringBuilder builder, string indent, IProperty property)
		{
			if (property.Parameters != null) {
				CSharpAmbience ambience = new CSharpAmbience ();
				foreach (IParameter para in property.Parameters) {
					builder.Append (Environment.NewLine);
					builder.Append (indent);
					builder.Append ("/// <param name=\"");
					builder.Append (para.Name);
					builder.Append ("\">\n");
					builder.Append (indent);
					builder.Append ("/// A <see cref=\"");
					builder.Append (ambience.GetString (para.ReturnType, OutputFlags.ClassBrowserEntries | OutputFlags.UseFullName | OutputFlags.UseNETTypeNames));
					builder.Append ("\"/>\n");
					builder.Append (indent);
					builder.Append ("/// </param>");
				}
			}
		}
		
		string GenerateBody (IType c, int line, string indent, out int newCursorOffset)
		{
			int startLine = int.MaxValue;
			newCursorOffset = 0;
			StringBuilder builder = new StringBuilder ();
			
			IMember member = null;
			
			foreach (IMember m in c.Members) {
				if (m.Location.Line < startLine && m.Location.Line > line) {
					startLine = m.Location.Line;
					member = m;
				}
			}
			
			if (member is IMethod) {
				AppendSummary (builder, indent, out newCursorOffset);
				AppendMethodComment (builder, indent, (IMethod)member);
			} else if (member is IProperty) {
				AppendSummary (builder, indent, out newCursorOffset);
				AppendPropertyComment (builder, indent, (IProperty)member);
			}
			
			return builder.ToString ();
		}
		
		static readonly List<string> commentTags = new List<string> (new string[] { "c", "code", "example", "exception", "include", "list", "listheader", "item", "term", "description", "para", "param", "paramref", "permission", "remarks", "returns", "see", "seealso", "summary", "value" });
		
		CompletionDataList GetXmlDocumentationCompletionData ()
		{
			CompletionDataList cp = new CompletionDataList ();
			cp.Add ("c", "md-literal", GettextCatalog.GetString ("Set text in a code-like font"));
			cp.Add ("code", "md-literal", GettextCatalog.GetString ("Set one or more lines of source code or program output"));
			cp.Add ("example", "md-literal", GettextCatalog.GetString ("Indicate an example"));
			cp.Add ("exception", "md-literal", GettextCatalog.GetString ("Identifies the exceptions a method can throw"), "exception cref=\"|\"></exception>");
			cp.Add ("include", "md-literal", GettextCatalog.GetString ("Includes comments from a external file"), "include file=\"|\" path=\"\">");
			cp.Add ("list", "md-literal", GettextCatalog.GetString ("Create a list or table"), "list type=\"|\">");
			
			cp.Add ("listheader", "md-literal", GettextCatalog.GetString ("Define the heading row"));
			cp.Add ("item", "md-literal", GettextCatalog.GetString ("Defines list or table item"));
			cp.Add ("term", "md-literal", GettextCatalog.GetString ("A term to define"));
			cp.Add ("description", "md-literal", GettextCatalog.GetString ("Describes a list item"));
			cp.Add ("para", "md-literal", GettextCatalog.GetString ("Permit structure to be added to text"));

			cp.Add ("param", "md-literal", GettextCatalog.GetString ("Describe a parameter for a method or constructor"), "param name=\"|\">");
			cp.Add ("paramref", "md-literal", GettextCatalog.GetString ("Identify that a word is a parameter name"), "paramref name=\"|\"/>");
			
			cp.Add ("permission", "md-literal", GettextCatalog.GetString ("Document the security accessibility of a member"), "permission cref=\"|\"");
			cp.Add ("remarks", "md-literal", GettextCatalog.GetString ("Describe a type"));
			cp.Add ("returns", "md-literal", GettextCatalog.GetString ("Describe the return value of a method"));
			cp.Add ("see", "md-literal", GettextCatalog.GetString ("Specify a link"), "see cref=\"|\"/>");
			cp.Add ("seealso", "md-literal", GettextCatalog.GetString ("Generate a See Also entry"), "seealso cref=\"|\"/>");
			cp.Add ("summary", "md-literal", GettextCatalog.GetString ("Describe a member of a type"));
			cp.Add ("typeparam", "md-literal", GettextCatalog.GetString ("Describe a type parameter for a generic type or method"));
			cp.Add ("typeparamref", "md-literal", GettextCatalog.GetString ("Identify that a word is a type parameter name"));
			cp.Add ("value", "md-literal", GettextCatalog.GetString ("Describe a property"));
			
			return cp;
		}
		#endregion
		
		
		#region IPathedDocument implementation
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;

		protected virtual void OnPathChanged (DocumentPathChangedEventArgs e)
		{
			EventHandler<DocumentPathChangedEventArgs> handler = this.PathChanged;
			if (handler != null)
				handler (this, e);
		}
		
		public class DataProvider : DropDownBoxListWindow.IListDataProvider
		{
			object tag;
			Ambience amb;
			List<IMember> memberList = new List<IMember> ();
			
			Document Document {
				get;
				set;
			}
			
			public DataProvider (Document doc, object tag, Ambience amb)
			{
				this.Document = doc;
				this.tag = ((INode)tag).Parent;
				this.amb = amb;
				Reset ();
			}
			
			#region IListDataProvider implementation
			public void Reset ()
			{
				memberList.Clear ();
				if (tag is ICompilationUnit) {
					Stack<IType> types = new Stack<IType> (((ICompilationUnit)tag).Types);
					while (types.Count > 0) {
						IType type = types.Pop ();
						memberList.Add (type);
						foreach (IType innerType in type.InnerTypes)
							types.Push (innerType);
					}
				} else  if (tag is IType) {
					memberList.AddRange (((IType)tag).Members);
				}
				memberList.Sort ((x, y) => String.Compare (GetString (amb, x), GetString (amb, y), StringComparison.OrdinalIgnoreCase));
			}
			
			string GetString (Ambience amb, IMember x)
			{
				if (tag is ICompilationUnit)
					return amb.GetString (x, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.UseFullInnerTypeName | OutputFlags.ReformatDelegates);
				return amb.GetString (x, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates);
			}
			
			public string GetText (int n)
			{
				return GetString (amb, memberList[n]);
			}

			public Gdk.Pixbuf GetIcon (int n)
			{
				return ImageService.GetPixbuf (memberList[n].StockIcon, IconSize.Menu);
			}

			public object GetTag (int n)
			{
				return memberList[n];
			}

			public void ActivateItem (int n)
			{
				var member = memberList[n];
				MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor extEditor = Document.GetContent<MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor> ();
				if (extEditor != null)
					extEditor.SetCaretTo (Math.Max (1, member.Location.Line), member.Location.Column);
			}

			public int IconCount {
				get {
					return memberList.Count;
				}
			}
			#endregion
		}
		
			
		public class CompilationUnitDataProvider : DropDownBoxListWindow.IListDataProvider
		{
			Document Document {
				get;
				set;
			}
			
			public CompilationUnitDataProvider (Document document)
			{
				this.Document = document;
			}
			
			#region IListDataProvider implementation
			public void Reset ()
			{
			}
			
			public string GetText (int n)
			{
				return Document.ParsedDocument.UserRegions.ElementAt (n).Name;
			}
			
			internal static Gdk.Pixbuf Pixbuf {
				get {
					return ImageService.GetPixbuf (Gtk.Stock.Add, IconSize.Menu);
				}
			}
			
			public Gdk.Pixbuf GetIcon (int n)
			{
				return Pixbuf;
			}
			
			public object GetTag (int n)
			{
				return Document.ParsedDocument.UserRegions.ElementAt (n);
			}
			
			
			public void ActivateItem (int n)
			{
				var reg = Document.ParsedDocument.UserRegions.ElementAt (n);
				MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor extEditor = Document.GetContent<MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor> ();
				if (extEditor != null)
					extEditor.SetCaretTo (Math.Max (1, reg.Region.Start.Line), reg.Region.Start.Column);
			}
			
			public int IconCount {
				get {
					if (Document.ParsedDocument == null)
						return 0;
					return Document.ParsedDocument.UserRegions.Count ();
				}
			}
			
			#endregion
		}
		
		public Gtk.Widget CreatePathWidget (int index)
		{
			PathEntry[] path = CurrentPath;
			if (path == null || index < 0 || index >= path.Length)
				return null;
			var tag = path[index].Tag;
			DropDownBoxListWindow window = new DropDownBoxListWindow (tag is ICompilationUnit ? (DropDownBoxListWindow.IListDataProvider)new CompilationUnitDataProvider (Document) : new DataProvider (Document, tag, GetAmbience ()));
			window.SelectItem (path[index].Tag);
			return window;
		}
		
		public PathEntry[] CurrentPath {
			get;
			private set;
		}
		
		class CustomNode : MonoDevelop.Projects.Dom.AbstractNode
		{
			public CustomNode (INode parent)
			{
				this.Parent = parent;
			}
		}
		
		void UpdatePath (object sender, Mono.TextEditor.DocumentLocationEventArgs e)
		{
			var unit = Document.CompilationUnit;
			if (unit == null)
				return;
			
			var loc = textEditorData.Caret.Location;
			IType type = unit.GetTypeAt (loc.Line, loc.Column);
			IMember member = type != null && type.ClassType != ClassType.Delegate ? type.GetMemberAt (loc.Line, loc.Column) : null;
			
			List<PathEntry> result = new List<PathEntry> ();
			var amb = GetAmbience ();
			INode node = member ?? type ?? (INode)unit;
			while (node != null) {
				PathEntry entry;
				if (node is ICompilationUnit) {
					if (!Document.ParsedDocument.UserRegions.Any ())
						break;
					FoldingRegion reg = Document.ParsedDocument.UserRegions.Where (r => r.Region.Contains (loc.Line, loc.Column)).LastOrDefault ();
					if (reg == null) {
						entry = new PathEntry (GettextCatalog.GetString ("No region"));
					} else {
						entry = new PathEntry (CompilationUnitDataProvider.Pixbuf,
						                       reg.Name);
					}
					entry.Position = EntryPosition.Right;
				} else {
					entry = new PathEntry (ImageService.GetPixbuf (((IMember)node).StockIcon, IconSize.Menu), amb.GetString ((IMember)node, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates));
				}
				entry.Tag = node;
				result.Insert (0, entry);
				node = node.Parent;
			}
			PathEntry noSelection = null;
			if (type == null) {
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = new CustomNode (Document.CompilationUnit) };
			} else if (member == null && type.ClassType != ClassType.Delegate) 
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = new CustomNode (type) };
			if (noSelection != null) {
/*				if (result.Count > 0 && result[result.Count - 1].Tag is ICompilationUnit) {
					result.Insert (result.Count - 1, noSelection);
				} else {*/
					result.Add (noSelection);
//				}
			}
			var prev = CurrentPath;
			CurrentPath = result.ToArray ();
			OnPathChanged (new DocumentPathChangedEventArgs (prev));
		}
		#endregion
	}
}
