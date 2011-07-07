// 
// CSharpCompletionTextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using MonoDevelop.Ide.Gui.Content;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.TypeSystem;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.CSharp.Project;
using System.Linq;
using MonoDevelop.CSharp.Formatting;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Text;
using MonoDevelop.Ide.CodeTemplates;

namespace MonoDevelop.CSharp.Completion
{
	
	public class CSharpCompletionTextEditorExtension : CompletionTextEditorExtension
	{
		internal Mono.TextEditor.TextEditorData textEditorData;
		internal ITypeResolveContext ctx;
	
		
		CompilationUnit unit;
		static readonly CompilationUnit emptyUnit = new CompilationUnit ();
		CompilationUnit Unit {
			get {
				return unit ?? emptyUnit;
			}
			set {
				unit = value;
			}
		}
		
		ParsedFile ParsedFile {
			get;
			set;
		}
		
		static bool EnableParameterInsight {
			get {
				return PropertyService.Get ("EnableParameterInsight", true);
			}
		}
		
		CSharpFormattingPolicy policy;
		public CSharpFormattingPolicy FormattingPolicy {
			get {
				if (policy == null) {
					IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
					if (Document.Project != null && Document.Project.Policies != null) {
						policy = base.Document.Project.Policies.Get<CSharpFormattingPolicy> (types);
					} else {
						policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
					}
				}
				return policy;
			}
		}
		
		public CSharpCompletionTextEditorExtension ()
		{
		}
		
		/// <summary>
		/// Used in testing environment.
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public CSharpCompletionTextEditorExtension (MonoDevelop.Ide.Gui.Document doc) : this ()
		{
			Initialize (doc);
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			ctx = Document.TypeResolveContext;
			textEditorData = Document.Editor;
			var parsedDocument = document.ParsedDocument;
			if (parsedDocument != null) {
				this.Unit = parsedDocument.Annotation<CompilationUnit> ();
				this.ParsedFile = parsedDocument.Annotation<ParsedFile> ();
			}
			
			Document.DocumentParsed += delegate {
				var newDocument = Document.ParsedDocument;
				if (newDocument == null) 
					return;
				this.Unit = newDocument.Annotation<CompilationUnit> ();
				this.ParsedFile = newDocument.Annotation<ParsedFile> ();
				var textEditor = Editor.Parent;
				if (textEditor != null) {
					textEditor.TextViewMargin.PurgeLayoutCache ();
					textEditor.RedrawMarginLines (textEditor.TextViewMargin, 1, Editor.LineCount);
				}
			};
		}
		
		bool IsInsideComment ()
		{
			var loc = Document.Editor.Caret.Location;
			return Unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Comment> (loc.Line, loc.Column - 1) != null;
		}
		
		bool IsInsideComment (int offset)
		{
			var loc = Document.Editor.OffsetToLocation (offset);
			return Unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Comment> (loc.Line, loc.Column) != null;
		}
		
		bool IsInsideDocComment ()
		{
			var loc = Document.Editor.Caret.Location;
			var cmt = Unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Comment> (loc.Line, loc.Column - 1);
			return cmt != null && cmt.CommentType == CommentType.Documentation;
		}

		bool IsInsideString ()
		{
			var loc = Document.Editor.Caret.Location;
			var expr = Unit.GetNodeAt<PrimitiveExpression> (loc.Line, loc.Column);
			return expr != null && expr.Value is string;
		}
		
		bool IsInsideString (int offset)
		{
			var loc = Document.Editor.OffsetToLocation (offset);
			var expr = Unit.GetNodeAt<PrimitiveExpression> (loc.Line, loc.Column);
			return expr != null && expr.Value is string;
		}
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool result = base.KeyPress (key, keyChar, modifier);
			
			if (EnableParameterInsight && (keyChar == ',' || keyChar == ')') && CanRunParameterCompletionCommand ())
				base.RunParameterCompletionCommand ();
			
			if (IsInsideComment ())
				ParameterInformationWindowManager.HideWindow (CompletionWidget);
			return result;
		}
		
		Tuple<ResolveResult, CSharpResolver> ResolveExpression (ParsedFile file, AstNode expr, CompilationUnit unit)
		{
			if (expr == null)
				return null;
			AstNode resolveNode;
			if (expr is Expression || expr is AstType) {
				resolveNode = expr;
			} else if (expr is VariableDeclarationStatement) {
				resolveNode = ((VariableDeclarationStatement)expr).Type;
			} else {
				return null;
			}
			
			var csResolver = new CSharpResolver (ctx, System.Threading.CancellationToken.None);
			var navigator = new NodeListResolveVisitorNavigator (new[] { resolveNode });
			var visitor = new ResolveVisitor (csResolver, file, navigator);
			unit.AcceptVisitor (visitor, null);
			var result = visitor.Resolve (resolveNode);

			var state = visitor.GetResolverStateBefore (resolveNode);
			return Tuple.Create (result, state);
		}
		
		ITypeDefinition currentType;
		IMember currentMember;
		
		void AppendMissingClosingBrackets (StringBuilder wrapper, string memberText, bool appendSemicolon)
		{
			var bracketStack = new Stack<char> ();
			
			bool isInString = false, isInChar = false;
			bool isInLineComment = false, isInBlockComment = false;
			
			for (int pos = 0; pos < memberText.Length; pos++) {
				char ch = memberText[pos];
				switch (ch) {
					case '(':
					case '[':
					case '{':
						if (!isInString && !isInChar && !isInLineComment && !isInBlockComment)
							bracketStack.Push (ch);
						break;
					case ')':
					case ']':
					case '}':
						if (!isInString && !isInChar && !isInLineComment && !isInBlockComment)
							if (bracketStack.Count > 0)
								bracketStack.Pop ();
						break;
					case '\r':
					case '\n':
						isInLineComment = false;
						break;
					case '/':
						if (isInBlockComment) {
							if (pos > 0 && memberText [pos - 1] == '*') 
								isInBlockComment = false;
						} else  if (!isInString && !isInChar && pos + 1 < memberText.Length) {
							char nextChar = memberText [pos + 1];
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
						break;
				}
			}
			bool didAppendSemicolon = !appendSemicolon;
			char lastBracket = '\0';
			while (bracketStack.Count > 0) {
				switch (bracketStack.Pop ()) {
					case '(':
						wrapper.Append (')');
						lastBracket = ')';
						break;
					case '[':
						wrapper.Append (']');
						lastBracket = ']';
						break;
					case '<':
						wrapper.Append ('>');
						lastBracket = '>';
						break;
					case '{':
						if (!didAppendSemicolon) {
							didAppendSemicolon = true;
							wrapper.Append (';');
						}
						
						wrapper.Append ('}');
						break;
				}
			}
			if (currentMember == null && lastBracket == ']') {
				// attribute context
				wrapper.Append ("void Target() {}");
			} else {
				if (!didAppendSemicolon)
					wrapper.Append (';');
			}
		}

		CompilationUnit ParseStub (string continuation, bool appendSemicolon = true)
		{
			string memberText = GetMemberTextToCaret ();
			if (memberText == null)
				return null;
			var wrapper = new StringBuilder ();
			wrapper.Append ("class Stub {");
			wrapper.AppendLine ();
			wrapper.Append (memberText);
			wrapper.Append (continuation);
			AppendMissingClosingBrackets (wrapper, memberText, appendSemicolon);
			wrapper.Append ('}'); // open brace from class.
			AstLocation memberLocation;
			if (currentMember != null) {
				memberLocation = currentMember.Region.Begin;
			} else if (currentType != null) {
				memberLocation = currentType.Region.Begin;
			} else {
				memberLocation = new AstLocation (1, 1);
			}
			
			using (var stream = new System.IO.StringReader (wrapper.ToString ())) {
				var parser = new CSharpParser ();
				return parser.Parse (stream, memberLocation.Line - 2);
			}
		}
		
		string GetMemberTextToCaret ()
		{
			int startOffset;
			if (currentMember != null) {
				startOffset = document.Editor.LocationToOffset (currentMember.Region.BeginLine, currentMember.Region.BeginColumn);
			} else if (currentType != null) {
				startOffset = document.Editor.LocationToOffset (currentType.Region.BeginLine, currentType.Region.BeginColumn);
			} else {
				startOffset = 0;
			}
			return Document.Editor.GetTextBetween (startOffset, Document.Editor.Caret.Offset);
		}
		
		Tuple<ParsedFile, AstNode, CompilationUnit> GetExpressionBeforeCursor ()
		{
			CompilationUnit baseUnit;
			if (currentMember == null) {
				baseUnit = ParseStub ("st {}", false);
				var type = baseUnit.GetNodeAt<MemberType> (document.Editor.Caret.Line, document.Editor.Caret.Column);
				if (type != null) {
					// insert target type into compilation unit, to respect the 
					var target = type.Target;
					target.Remove ();
					var node = Unit.GetNodeAt (document.Editor.Caret.Line, document.Editor.Caret.Column) ?? Unit;
					node.AddChild (target, AstNode.Roles.Type);
					return Tuple.Create (ParsedFile, (AstNode)target, Unit);
				}
			}
			
			if (currentMember == null && currentType == null) {
				return null;
			}
			baseUnit = ParseStub ("a()");
			
			var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			var mref = baseUnit.GetNodeAt<MemberReferenceExpression> (document.Editor.Caret.Line, document.Editor.Caret.Column); 
			Expression expr;
			if (mref != null) {
				expr = mref.Target;
			} else {
				var tref = baseUnit.GetNodeAt<TypeReferenceExpression> (document.Editor.Caret.Line, document.Editor.Caret.Column); 
				var memberType = tref != null ? tref.Type as MemberType : null;
				if (memberType == null)
					return null;
				expr = new TypeReferenceExpression (memberType.Target.Clone ());
				tref.ReplaceWith (expr);
			}
			
			var member = Unit.GetNodeAt<AttributedNode> (memberLocation);
			var member2 = baseUnit.GetNodeAt<AttributedNode> (memberLocation);
			member2.Remove ();
			member.ReplaceWith (member2);
			var tsvisitor = new TypeSystemConvertVisitor (Document.GetProjectContext (), Document.FileName);
			Unit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, (AstNode)expr, Unit);
		}
		
		static void Print (AstNode node)
		{
			var v = new OutputVisitor (Console.Out, new CSharpFormattingOptions ());
			node.AcceptVisitor (v, null);
		}
		
		Tuple<ParsedFile, Expression, CompilationUnit> GetExpressionAtCursor ()
		{
			if (currentMember == null && currentType == null)
				return null;
			
			var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			var baseUnit = ParseStub ("");
			
			Expression expr = baseUnit.GetNodeAt<IdentifierExpression> (document.Editor.Caret.Line, document.Editor.Caret.Column - 1); 
			if (expr == null) {
				baseUnit = ParseStub ("()");
				expr = baseUnit.GetNodeAt<IdentifierExpression> (document.Editor.Caret.Line, document.Editor.Caret.Column - 1); 
				if (expr == null)
					return null;
			}
			var member = Unit.GetNodeAt<AttributedNode> (memberLocation);
			var member2 = baseUnit.GetNodeAt<AttributedNode> (memberLocation);
			member2.Remove ();
			
			if (member is TypeDeclaration) {
				member.AddChild (member2, TypeDeclaration.MemberRole);
			} else {
				member.ReplaceWith (member2);
			}

			var tsvisitor = new TypeSystemConvertVisitor (Document.GetProjectContext (), Document.FileName);
			Unit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, expr, Unit);
		}
		
		Tuple<ParsedFile, AstNode, CompilationUnit> GetExpressionAt (int offset)
		{
			CSharpParser parser = new CSharpParser ();
			string text = Document.Editor.GetTextAt (0, Document.Editor.Caret.Offset) + "a; } } } }";
			var stream = new System.IO.StringReader (text);
			var completionUnit = parser.Parse (stream, 0);
			stream.Close ();
			var loc = document.Editor.OffsetToLocation (offset);
			
			var expr = completionUnit.GetNodeAt (new AstLocation (loc.Line, loc.Column), n => n is Expression || n is VariableDeclarationStatement);
			
			if (expr == null)
				return null;
			var tsvisitor = new TypeSystemConvertVisitor (Document.GetProjectContext (), Document.FileName);
			completionUnit.AcceptVisitor (tsvisitor, null);
			
			return Tuple.Create (tsvisitor.ParsedFile, expr, completionUnit);
		}
		
		Tuple<ParsedFile, Expression, CompilationUnit> GetInvocationBeforeCursor (bool afterBracket)
		{
			///////
			if (currentMember == null && currentType == null)
				return null;
			
			CSharpParser parser = new CSharpParser ();
			int startOffset;
			if (currentMember != null) {
				startOffset = document.Editor.LocationToOffset (currentMember.Region.BeginLine, currentMember.Region.BeginColumn);
			} else {
				startOffset = document.Editor.LocationToOffset (currentType.Region.BeginLine, currentType.Region.BeginColumn);
			}
			string memberText = Document.Editor.GetTextBetween (startOffset, Document.Editor.Caret.Offset - 1);
			
			var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			StringBuilder wrapper = new StringBuilder ();
			wrapper.Append ("class Stub {");
			wrapper.AppendLine ();
			wrapper.Append (memberText);
			
			if (afterBracket) {
				wrapper.Append ("();");
			} else {
				wrapper.Append ("x);");
			}
			
			wrapper.Append (" SomeCall (); } } }");
			var stream = new System.IO.StringReader (wrapper.ToString ());
			var baseUnit = parser.Parse (stream, memberLocation.Line - 2);
			stream.Close ();
			var expr = baseUnit.GetNodeAt<Expression> (document.Editor.Caret.Line, document.Editor.Caret.Column);
			if (expr is InvocationExpression) {
				expr = ((InvocationExpression)expr).Target;
			}
			if (expr == null)
				return null;
			var member = Unit.GetNodeAt<AttributedNode> (memberLocation);
			var member2 = baseUnit.GetNodeAt<AttributedNode> (memberLocation);
			member2.Remove ();
			member.ReplaceWith (member2);
			
			var tsvisitor = new TypeSystemConvertVisitor (Document.GetProjectContext (), Document.FileName);
			Unit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, expr, Unit);
		}
		
		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			//	var timer = Counters.ResolveTime.BeginTiming ();
			try {
				return InternalHandleCodeCompletion (completionContext, completionChar, ref triggerWordLength);
			} catch (Exception e) {
				LoggingService.LogError ("Unexpected code completion exception." + Environment.NewLine + 
					"FileName: " + Document.FileName + Environment.NewLine + 
					"Position: line=" + completionContext.TriggerLine + " col=" + completionContext.TriggerLineOffset + Environment.NewLine + 
					"Line text: " + Document.Editor.GetLineText (completionContext.TriggerLine), 
					e);
				return null;
			} finally {
				//			if (timer != null)
				//				timer.Dispose ();
			}
		}
		
		ICompletionDataList InternalHandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			if (textEditorData.CurrentMode is CompletionTextLinkMode) {
				if (!((CompletionTextLinkMode)textEditorData.CurrentMode).TriggerCodeCompletion)
					return null;
			} else if (textEditorData.CurrentMode is Mono.TextEditor.TextLinkEditMode) {
				return null;
			}
			var loc = new AstLocation (Document.Editor.Caret.Location.Line, Document.Editor.Caret.Location.Column);
			this.currentType = ParsedFile.GetTypeDefinition (loc);
			this.currentMember = ParsedFile.GetMember (loc);
			switch (completionChar) {
			// Magic key completion
			case ':':
			case '.':
				if (IsInsideComment () || IsInsideString ())
					return null;
				var expr = GetExpressionBeforeCursor ();
				if (expr == null)
					return null;
				// do not complete <number>. (but <number>.<number>.)
				if (expr.Item2 is PrimitiveExpression) {
					var pexpr = (PrimitiveExpression)expr.Item2;
					if (!(pexpr.Value is string || pexpr.Value is char) && !pexpr.LiteralValue.Contains ('.'))
						return null;
				}
				
				
				var resolveResult = ResolveExpression (expr.Item1, expr.Item2, expr.Item3);
				
				Console.WriteLine (resolveResult + "/" + expr.Item2.GetType ());
				if (resolveResult == null)
					return null;
				if (expr.Item2 is AstType)
					return CreateTypeAndNamespaceCompletionData (loc, resolveResult.Item1, expr.Item2, resolveResult.Item2);
				
				return CreateCompletionData (loc, resolveResult.Item1, expr.Item2, resolveResult.Item2);
			case '#':
				if (IsInsideComment () || IsInsideString ())
					return null;
				return GetDirectiveCompletionData ();
			
			// XML doc completion
			case '<':
				if (IsInsideDocComment ())
					return GetXmlDocumentationCompletionData ();
				return null;
			case '>':
				if (!IsInsideDocComment ())
					return null;
				string lineText = Document.Editor.GetLineText (completionContext.TriggerLine);
				int startIndex = Math.Min (completionContext.TriggerLineOffset - 1, lineText.Length - 1);
				
				while (startIndex >= 0 && lineText [startIndex] != '<') {
					--startIndex;
					if (lineText [startIndex] == '/') { // already closed.
						startIndex = -1;
						break;
					}
				}
				
				if (startIndex >= 0) {
					int endIndex = startIndex;
					while (endIndex <= completionContext.TriggerLineOffset && endIndex < lineText.Length && !Char.IsWhiteSpace (lineText [endIndex])) {
						endIndex++;
					}
					string tag = endIndex - startIndex - 1 > 0 ? lineText.Substring (startIndex + 1, endIndex - startIndex - 2) : null;
					if (!String.IsNullOrEmpty (tag) && commentTags.IndexOf (tag) >= 0)
						Document.Editor.Insert (Document.Editor.Caret.Offset, "</" + tag + ">");
				}
				return null;
			
			// Parameter completion
			case '(':
				if (IsInsideComment () || IsInsideString ())
					return null;
				var invoke = GetInvocationBeforeCursor (true);
				
				if (invoke == null)
					return null;
				if (invoke.Item2 is TypeOfExpression)
					return CreateTypeList ();
				var invocationResult = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
				Console.WriteLine ("result:" + invocationResult);
				if (invocationResult == null)
					return null;
				var methodGroup = invocationResult.Item1 as MethodGroupResolveResult;
				if (methodGroup != null)
					return CreateParameterCompletion (methodGroup, invocationResult.Item2, invoke.Item2, 0);
				return null;
			case ',':
				int cpos2;
				if (!GetParameterCompletionCommandOffset (out cpos2)) 
					return null;
			//	completionContext = CompletionWidget.CreateCodeCompletionContext (cpos2);
			//	int currentParameter2 = MethodParameterDataProvider.GetCurrentParameterIndex (CompletionWidget, completionContext) - 1;
				Console.WriteLine ("cp:" + cpos2);
//				return CreateParameterCompletion (CreateResolver (), location, ExpressionContext.MethodBody, provider.Methods, currentParameter);	
				break;
				
			// Completion on space:
			case ' ':
				if (IsInsideComment () || IsInsideString ())
					return null;
				
				int tokenIndex = completionContext.TriggerOffset;
				string token = GetPreviousToken (ref tokenIndex, false);
//				int tokenIndex = completionContext.TriggerOffset;
//				string token = GetPreviousToken (ref tokenIndex, false);
//				if (result.ExpressionContext == ExpressionContext.ObjectInitializer) {
//					resolver = CreateResolver ();
//					ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForObjectInitializer (textEditorData, resolver.Unit, Document.FileName, resolver.CallingType);
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
					int currentParameter = MethodParameterDataProvider.GetCurrentParameterIndex (CompletionWidget, cpos, 0) - 1;
					if (currentParameter < 0)
						return null;
					invoke = GetInvocationBeforeCursor (token == "(");
					if (invoke == null)
						return null;
					invocationResult = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
					if (invocationResult == null)
						return null;
					methodGroup = invocationResult.Item1 as MethodGroupResolveResult;
					if (methodGroup != null)
						return CreateParameterCompletion (methodGroup, invocationResult.Item2, invoke.Item2, currentParameter);
					return null;
				case "=":
				case "==":
					GetPreviousToken (ref tokenIndex, false);
					
					var expressionOrVariableDeclaration = GetExpressionAt (tokenIndex);
					if (expressionOrVariableDeclaration == null)
						return null;
					
					resolveResult = ResolveExpression (expressionOrVariableDeclaration.Item1, expressionOrVariableDeclaration.Item2, expressionOrVariableDeclaration.Item3);
					if (resolveResult == null)
						return null;
					
					if (resolveResult.Item1.Type.IsEnum ()) {
						var wrapper = new CompletionDataWrapper (this);
						AddContextCompletion (wrapper, resolveResult.Item2, expressionOrVariableDeclaration.Item2);
						AddEnumMembers (wrapper, resolveResult.Item1.Type, resolveResult.Item2);
						wrapper.Result.AutoCompleteEmptyMatch = false;
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
//						completionList.Add (new EventCreationCompletionData (textEditorData, varName, resolvedType, null, parameterDefinition, resolver.CallingMember, resolvedType));
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
					Console.WriteLine (expressionOrVariableDeclaration);
					if (expressionOrVariableDeclaration == null)
						return null;
				
					resolveResult = ResolveExpression (expressionOrVariableDeclaration.Item1, expressionOrVariableDeclaration.Item2, expressionOrVariableDeclaration.Item3);
					Console.WriteLine (resolveResult);
					if (resolveResult == null)
						return null;
					
					
					var mrr = resolveResult.Item1 as MemberResolveResult;
					if (mrr != null) {
						var evt = mrr.Member as IEvent;
						if (evt == null)
							return null;
						var delegateType = evt.ReturnType.Resolve (ctx);
						if (!delegateType.IsDelegate ())
							return null;
						
						var wrapper = new CompletionDataWrapper (this);
						if (currentType != null) {
//							bool includeProtected = DomType.IncludeProtected (dom, typeFromDatabase, resolver.CallingType);
							foreach (var method in currentType.GetMethods (ctx)) {
								if (MatchDelegate (delegateType, method) /*&& method.IsAccessibleFrom (dom, resolver.CallingType, resolver.CallingMember, includeProtected) &&*/) {
									wrapper.AddMember (method);
//									data.SetText (data.CompletionText + ";");
								}
							}
						}
						if (token == "+=") {
							string parameterDefinition = AddDelegateHandlers (wrapper, delegateType);
							string varName = GetPreviousMemberReferenceExpression (tokenIndex);
							wrapper.Result.Add (new EventCreationCompletionData (this, varName, delegateType, evt, parameterDefinition, currentMember, currentType));
						}
					
						return wrapper.Result;
					}
					return null;
				case ":":
					if (currentMember == null) {
						var wrapper = new CompletionDataWrapper (this);
						AddTypesAndNamespaces (wrapper, GetState (), t => currentType != null ? !currentType.Equals (t) : true);
						return wrapper.Result;
					}
					return null;
				}
				
				return HandleKeywordCompletion (completionContext, tokenIndex, token);
			// Automatic completion
			default:
				if (!TextEditorProperties.EnableAutoCodeCompletion ||
					IsInsideComment () || IsInsideString () ||
					!(char.IsLetter (completionChar) || completionChar == '_'))
					return null;
				char prevCh = completionContext.TriggerOffset > 2 ? textEditorData.GetCharAt (completionContext.TriggerOffset - 2) : '\0';
				char nextCh = completionContext.TriggerOffset < textEditorData.Length ? textEditorData.GetCharAt (completionContext.TriggerOffset) : ' ';
				const string allowedChars = ";,[(){}+-*/%^?:&|~!<>=";
				if (!Char.IsWhiteSpace (nextCh) && allowedChars.IndexOf (nextCh) < 0)
					return null;
				if (!(Char.IsWhiteSpace (prevCh) || allowedChars.IndexOf (prevCh) >= 0))
					return null;
				// Do not pop up completion on identifier identifier (should be handled by keyword completion).
				tokenIndex = completionContext.TriggerOffset - 1;
				token = GetPreviousToken (ref tokenIndex, false);
				if (string.IsNullOrEmpty (token) && !(IsInsideComment (tokenIndex) || IsInsideString (tokenIndex))) {
					char last = token [token.Length - 1];
					if (!char.IsLetterOrDigit (last) && last != '_')
						return null;
				}
				
				var identifierStart = GetExpressionAtCursor ();
				if (identifierStart == null)
					return null;
				CSharpResolver csResolver;
				AstNode n = identifierStart.Item2;
				var contextList = new CompletionDataWrapper (this);
				
				if (n != null/* && !(identifierStart.Item2 is TypeDeclaration)*/) {
					csResolver = new CSharpResolver (ctx, System.Threading.CancellationToken.None);
					var nodes = new List<AstNode> ();
					nodes.Add (n);
					if (n.Parent is ICSharpCode.NRefactory.CSharp.Attribute)
						nodes.Add (n.Parent);
					var navigator = new NodeListResolveVisitorNavigator (nodes);
					var visitor = new ResolveVisitor (csResolver, identifierStart.Item1, navigator);
					identifierStart.Item3.AcceptVisitor (visitor, null);
					csResolver = visitor.GetResolverStateBefore (n);
					
					// add attribute properties.
					if (n.Parent is ICSharpCode.NRefactory.CSharp.Attribute) {
						var resolved = visitor.Resolve (n.Parent);
						Console.WriteLine (resolved);
						if (!resolved.IsError)  {
							foreach (var property in resolved.Type.GetProperties (ctx).Where (p => p.Accessibility == Accessibility.Public)) {
								contextList.AddMember (property);
							}
							foreach (var field in resolved.Type.GetFields (ctx).Where (p => p.Accessibility == Accessibility.Public)) {
								contextList.AddMember (field);
							}
						}
					}
				} else {
					csResolver = GetState ();
				}
				
				// identifier has already started with the first letter
				completionContext.TriggerOffset--;
				completionContext.TriggerLineOffset--;
				completionContext.TriggerWordLength = 1;
				
				
				
				AddContextCompletion (contextList, csResolver, identifierStart.Item2);
				return contextList.Result;
//				if (stub.Parent is BlockStatement)
				
//				result = FindExpression (dom, completionContext, -1);
//				if (result == null)
//					return null;
//				if (IsInLinqContext (result)) {
//					tokenIndex = completionContext.TriggerOffset;
//					token = GetPreviousToken (ref tokenIndex, false); // token last typed
//					token = GetPreviousToken (ref tokenIndex, false); // possible linq keyword ?
//					triggerWordLength = 1;
//				
//					if (linqKeywords.Contains (token)) {
//						if (token == "from") // after from no auto code completion.
//							return null;
//						result.Expression = "";
//						return CreateCtrlSpaceCompletionData (completionContext, result);
//					}
//					CompletionDataList dataList = new ProjectDomCompletionDataList ();
//					CompletionDataCollector col = new CompletionDataCollector (this, dom, dataList, Document.CompilationUnit, null, new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//					foreach (string kw in linqKeywords) {
//						col.Add (kw, "md-keyword");
//					}
//					return dataList;
//				} else if (result.ExpressionContext != ExpressionContext.IdentifierExpected) {
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
//					tokenIndex = completionContext.TriggerOffset;
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
				break;
			}
			return null;
		}
		
		public string GetPreviousMemberReferenceExpression (int tokenIndex)
		{
			string result = GetPreviousToken (ref tokenIndex, false);
			result = GetPreviousToken (ref tokenIndex, false);
			if (result != ".") {
				result = null;
			} else {
				List<string > names = new List<string> ();
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
		
		bool MatchDelegate (IType delegateType, IMethod method)
		{
			var delegateMethod = delegateType.GetDelegateInvokeMethod ();
			if (delegateMethod == null || delegateMethod.Parameters.Count != method.Parameters.Count)
				return false;
			
			for (int i = 0; i < delegateMethod.Parameters.Count; i++) {
				if (!delegateMethod.Parameters[i].Type.Resolve (ctx).Equals (method.Parameters[i].Type.Resolve (ctx)))
					return false;
			}
			return true;
		}
		
		string AddDelegateHandlers (CompletionDataWrapper completionList, IType delegateType, bool addSemicolon = true, bool addDefault = true)
		{
			var ambience = GetAmbience ();
			IMethod delegateMethod = delegateType.GetDelegateInvokeMethod ();
			var thisLineIndent = Document.Editor.GetLineIndent (Document.Editor.Caret.Line);
			string delegateEndString = Document.Editor.EolMarker + thisLineIndent + "}" + (addSemicolon ? ";" : "");
			bool containsDelegateData = completionList.Result.Any (d => d.DisplayText.StartsWith ("delegate("));
			if (addDefault)
				completionList.AddCustom ("delegate", "md-keyword", GettextCatalog.GetString ("Creates anonymous delegate."), "delegate {" + Document.Editor.EolMarker + thisLineIndent + TextEditorProperties.IndentString + "|" + delegateEndString);
			var generator = new MonoDevelop.CSharp.Refactoring.CSharpCodeGenerator ();
			var sb = new StringBuilder ("(");
			var sbWithoutTypes = new StringBuilder ("(");
			for (int k = 0; k < delegateMethod.Parameters.Count; k++) {
				if (k > 0) {
					sb.Append (", ");
					sbWithoutTypes.Append (", ");
				}
				var parameterType = delegateMethod.Parameters [k].Type.Resolve (ctx);
				sb.Append (generator.GetShortTypeString (Document, parameterType));
				sb.Append (" ");
				sb.Append (delegateMethod.Parameters [k].Name);
				sbWithoutTypes.Append (delegateMethod.Parameters [k].Name);
			}
			sb.Append (")");
			sbWithoutTypes.Append (")");
			completionList.AddCustom ("delegate" + sb, "md-keyword", GettextCatalog.GetString ("Creates anonymous delegate."), "delegate" + sb + " {" + Document.Editor.EolMarker + thisLineIndent + TextEditorProperties.IndentString + "|" + delegateEndString);
			if (!completionList.Result.Any (data => data.DisplayText == sbWithoutTypes.ToString ()))
				completionList.AddCustom (sbWithoutTypes.ToString (), "md-keyword", GettextCatalog.GetString ("Creates lambda expression."), sbWithoutTypes + " => |" + (addSemicolon ? ";" : ""));
			
			// It's  needed to temporarly disable inserting auto matching bracket because the anonymous delegates are selectable with '('
			// otherwise we would end up with () => )
			if (!containsDelegateData) {
				var savedValue = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket;
				MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = false;
				completionList.Result.CompletionListClosed += delegate {
					MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = savedValue;
				};
			}
			return sb.ToString ();
		}
		
		public override ICompletionDataList CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			ICompletionDataList result = base.CodeCompletionCommand (completionContext);
			if (result != null)
				return result;
			
			// check propose name, for context <variable name> <ctrl+space>
			int tokenIndex = completionContext.TriggerOffset;
			string token = GetPreviousToken (ref tokenIndex, false);
			IType isAsType = null;
			var isAsExpression = GetExpressionAt (tokenIndex);
			if (isAsExpression != null && isAsExpression.Item2 is VariableDeclarationStatement) {
				var parent = isAsExpression.Item2 as VariableDeclarationStatement;
				string name = Editor.GetTextBetween (parent.Type.StartLocation.Line, parent.Type.StartLocation.Column, parent.Type.EndLocation.Line, parent.Type.EndLocation.Column);
				
				var names = new List<string> ();
				int lastNameStart = 0;
				for (int i = 1; i < name.Length; i++) {
					if (Char.IsUpper (name[i])) {
						names.Add (name.Substring (lastNameStart, i - lastNameStart));
						lastNameStart = i;
					}
				}
				names.Add (name.Substring (lastNameStart, name.Length - lastNameStart));
				var proposeNameList = new CompletionDataWrapper (this);
				var possibleName = new StringBuilder ();
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
						proposeNameList.Result.Add (possibleName.ToString (), "md-field");
				}
				proposeNameList.Result.IsSorted = true;
				return proposeNameList.Result;
			}
			
			// Ctrl+Space context
			if (IsInsideComment () || IsInsideString ())
				return null;
			
			var wrapper = new CompletionDataWrapper (this);
			var state = GetState ();
			
			AddContextCompletion (wrapper, state, null);
			return wrapper.Result;
		}
		
		CSharpResolver GetState ()
		{
			var state = new CSharpResolver (ctx, System.Threading.CancellationToken.None);
			var pf = document.ParsedDocument.Annotation<ParsedFile> ();
			var loc = new AstLocation (Editor.Caret.Line, Editor.Caret.Column);
			state.CurrentMember =  pf.GetMember (loc);
			state.CurrentTypeDefinition =  pf.GetTypeDefinition (loc);
			state.UsingScope = pf.GetUsingScope (loc);
			if (state.CurrentMember != null) {
				var unit = document.ParsedDocument.Annotation<CompilationUnit> ();
				var node = unit.GetNodeAt (Editor.Caret.Line, Editor.Caret.Column);
				if (node == null)
					return state;
				var navigator = new NodeListResolveVisitorNavigator (new[] { node });
				var visitor = new ResolveVisitor (state, pf, navigator);
				unit.AcceptVisitor (visitor, null);
				state = visitor.GetResolverStateBefore (node) ?? state;
				Print (unit);
				foreach (var v in state.LocalVariables) {
					Console.WriteLine (v);
				}
			}
			
			return state;
		}

		static bool ContainsPublicConstructors (ITypeDefinition t)
		{
			if (t.Methods.Count (m => m.IsConstructor) == 0)
				return true;
			return t.Methods.Any (m => m.IsConstructor && m.IsPublic);
		}

		ICompletionDataList CreateTypeCompletionData (IType hintType)
		{
			var wrapper = new CompletionDataWrapper (this);
			var state = GetState ();
			Predicate<ITypeDefinition> pred = null;
			if (hintType != null && !hintType.Equals (SharedTypes.UnknownType)) {
				var lookup = new MemberLookup (ctx, currentType, document.GetProjectContext ());
				pred = t => {
					// check if type is in inheritance tree.
					if (hintType.GetDefinition () != null && !t.IsDerivedFrom (hintType.GetDefinition (), ctx))
						return false;
					// check for valid constructors
					if (t.Methods.Count (m => m.IsConstructor) == 0)
						return true;
					bool isProtectedAllowed = currentType != null ? currentType.IsDerivedFrom (t, ctx) : false;
					return t.Methods.Any (m => m.IsConstructor && lookup.IsAccessible (m, isProtectedAllowed));
				};
				var generator = new MonoDevelop.CSharp.Refactoring.CSharpCodeGenerator ();
				wrapper.Result.DefaultCompletionString = generator.GetShortTypeString (Document, hintType);
				wrapper.AddType (hintType, wrapper.Result.DefaultCompletionString);
			}
			AddTypesAndNamespaces (wrapper, state, pred);
			AddKeywords (wrapper, primitiveTypes.Where (k => k != "void"));
			wrapper.Result.AutoCompleteEmptyMatch = true;
			return wrapper.Result;
		}

//			CompletionDataList result = new ProjectDomCompletionDataList ();
//			// "var o = new " needs special treatment.
//			if (returnType == null && returnTypeUnresolved != null && returnTypeUnresolved.FullName == "var")
//				returnType = returnTypeUnresolved = DomReturnType.Object;
//
//			//	ExpressionContext.TypeExpressionContext tce = context as ExpressionContext.TypeExpressionContext;
//
//			CompletionDataCollector col = new CompletionDataCollector (this, dom, result, Document.CompilationUnit, callingType, location);
//			IType type = null;
//			if (returnType != null)
//				type = dom.GetType (returnType);
//			if (type == null)
//				type = dom.SearchType (Document.CompilationUnit, callingType, location, returnTypeUnresolved);
//			
//			// special handling for nullable types: Bug 674516 - new completion for nullables should not include "Nullable"
//			if (type is InstantiatedType && ((InstantiatedType)type).UninstantiatedType.FullName == "System.Nullable" && ((InstantiatedType)type).GenericParameters.Count == 1) {
//				var genericParameter = ((InstantiatedType)type).GenericParameters [0];
//				returnType = returnTypeUnresolved = Document.CompilationUnit.ShortenTypeName (genericParameter, location);
//				type = dom.SearchType (Document.CompilationUnit, callingType, location, genericParameter);
//			}
//			
//			if (type == null || !(type.IsAbstract || type.ClassType == ClassType.Interface)) {
//				if (type == null || type.ConstructorCount == 0 || type.Methods.Any (c => c.IsConstructor && c.IsAccessibleFrom (dom, callingType, type, callingType != null && dom.GetInheritanceTree (callingType).Any (x => x.FullName == type.FullName)))) {
//					if (returnTypeUnresolved != null) {
//						col.FullyQualify = true;
//						CompletionData unresovedCompletionData = col.Add (returnTypeUnresolved);
//						col.FullyQualify = false;
//						// don't set default completion string for arrays, since it interferes with: 
//						// string[] arr = new string[] vs new { "a"}
//						if (returnTypeUnresolved.ArrayDimensions == 0)
//							result.DefaultCompletionString = StripGenerics (unresovedCompletionData.CompletionText);
//					} else {
//						CompletionData unresovedCompletionData = col.Add (returnType);
//						if (returnType.ArrayDimensions == 0)
//							result.DefaultCompletionString = StripGenerics (unresovedCompletionData.CompletionText);
//					}
//				}
//			}
//			
//			//				if (tce != null && tce.Type != null) {
//			//					result.DefaultCompletionString = StripGenerics (col.AddCompletionData (result, tce.Type).CompletionString);
//			//				} 
//			//			else {
//			//			}
//			if (type == null)
//				return result;
//			HashSet<string > usedNamespaces = new HashSet<string> (GetUsedNamespaces ());
//			if (type.FullName == DomReturnType.Object.FullName) 
//				AddPrimitiveTypes (col);
//			
//			foreach (IType curType in dom.GetSubclasses (type)) {
//				if (context != null && context.FilterEntry (curType))
//					continue;
//				if ((curType.TypeModifier & TypeModifier.HasOnlyHiddenConstructors) == TypeModifier.HasOnlyHiddenConstructors)
//					continue;
//				if (usedNamespaces.Contains (curType.Namespace)) {
//					if (curType.ConstructorCount > 0) {
//						if (!(curType.Methods.Any (c => c.IsConstructor && c.IsAccessibleFrom (dom, curType, callingType, callingType != null && dom.GetInheritanceTree (callingType).Any (x => x.FullName == curType.FullName)))))
//							continue;
//					}
//					col.Add (curType);
//				} else {
//					string nsName = curType.Namespace;
//					int idx = nsName.IndexOf ('.');
//					if (idx >= 0)
//						nsName = nsName.Substring (0, idx);
//					col.Add (new Namespace (nsName));
//				}
//			}
//			
//			// add aliases
//			if (returnType != null) {
//				foreach (IUsing u in Document.CompilationUnit.Usings) {
//					foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
//						if (alias.Value.ToInvariantString () == returnType.ToInvariantString ())
//							result.Add (alias.Key, "md-class");
//					}
//				}
//			}
//			
//			return result;
//		}
		
		public ICompletionDataList HandleKeywordCompletion (CodeCompletionContext completionContext, int wordStart, string word)
		{
			if (IsInsideComment () || IsInsideString ())
				return null;
			var location = new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset);
			switch (word) {
			case "using":
			case "namespace":
				if (currentType != null)
					return null;
				var wrapper = new CompletionDataWrapper (this);
				AddTypesAndNamespaces (wrapper, GetState (), t => false);
				return wrapper.Result;
//				case "case":
//					return CreateCaseCompletionData (location, result);
//				case ",":
//				case ":":
//					if (result.ExpressionContext == ExpressionContext.InheritableType) {
//						IType cls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
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
//						int tokenIndex = completionContext.TriggerOffset;
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
					var parent = isAsExpression.Item2.Parent;
					if (parent is VariableInitializer)
						parent = parent.Parent;
					if (parent is VariableDeclarationStatement) {
						var resolved = ResolveExpression (isAsExpression.Item1, parent, isAsExpression.Item3);
						if (resolved != null)
							isAsType = resolved.Item1.Type;
					}
				}
				
				var isAsWrapper = new CompletionDataWrapper (this);
				AddTypesAndNamespaces (isAsWrapper, GetState (), t => isAsType == null || t.IsDerivedFrom (isAsType.GetDefinition (), ctx));
				return isAsWrapper.Result;
//					{
//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
//						ExpressionResult expressionResult = FindExpression (dom, completionContext, wordStart - textEditorData.Caret.Offset);
//						NRefactoryResolver resolver = CreateResolver ();
//						ResolveResult resolveResult = resolver.Resolve (expressionResult, new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//						if (resolveResult != null && resolveResult.ResolvedType != null) {
//							CompletionDataCollector col = new CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
//							IType foundType = null;
//							if (word == "as") {
//								ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForAsCompletion (textEditorData, Document.CompilationUnit, Document.FileName, resolver.CallingType);
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
					var overrideCls = ParsedFile.GetTypeDefinition (location);
								
					if (overrideCls != null && (overrideCls.ClassType == ClassType.Class || overrideCls.ClassType == ClassType.Struct)) {
						string modifiers = textEditorData.GetTextBetween (firstMod, wordStart);
						return GetOverrideCompletionData (completionContext, overrideCls, modifiers);
					}
					return null;
//				case "partial":
//					// Look for modifiers, in order to find the beginning of the declaration
//					firstMod = wordStart;
//					i = wordStart;
//					for (int n = 0; n < 3; n++) {
//						string mod = GetPreviousToken (ref i, true);
//						if (mod == "public" || mod == "protected" || mod == "private" || mod == "internal" || mod == "sealed") {
//							firstMod = i;
//						} else if (mod == "static") {
//							// static methods are not overridable
//							return null;
//						} else
//							break;
//					}
//					if (!IsLineEmptyUpToEol ())
//						return null;
//					
//					overrideCls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//					if (overrideCls != null && (overrideCls.ClassType == ClassType.Class || overrideCls.ClassType == ClassType.Struct)) {
//						string modifiers = textEditorData.GetTextBetween (firstMod, wordStart);
//						return GetPartialCompletionData (completionContext, overrideCls, modifiers);
//					}
//					return null;
//					
			case "public":
			case "protected":
			case "private":
			case "internal":
			case "sealed":
			case "static":
				wrapper = new CompletionDataWrapper (this);
				var state = GetState ();
				AddTypesAndNamespaces (wrapper, state, null, m => false);
				AddKeywords (wrapper, typeLevel);
				AddKeywords (wrapper, primitiveTypes);
				return wrapper.Result;
			case "new":
				int j = completionContext.TriggerOffset - 4;
//				string token = GetPreviousToken (ref j, true);
				
				IType hintType = null;
				var expressionOrVariableDeclaration = GetExpressionAt (j);
				if (expressionOrVariableDeclaration != null && expressionOrVariableDeclaration.Item2 is VariableDeclarationStatement) {
					var varDecl = (VariableDeclarationStatement)expressionOrVariableDeclaration.Item2;
					var resolved = ResolveExpression (expressionOrVariableDeclaration.Item1, varDecl.Type, expressionOrVariableDeclaration.Item3);
					if (resolved != null)
						hintType = resolved.Item1.Type;
				}
				return CreateTypeCompletionData (hintType);
//					IType callingType = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new AstLocation (textEditorData.Caret.Line, textEditorData.Caret.Column));
//					ExpressionContext newExactContext = new NewCSharpExpressionFinder (dom).FindExactContextForNewCompletion (textEditorData, Document.CompilationUnit, Document.FileName, callingType);
//					if (newExactContext is ExpressionContext.TypeExpressionContext)
//						return CreateTypeCompletionData (location, callingType, newExactContext, ((ExpressionContext.TypeExpressionContext)newExactContext).Type, ((ExpressionContext.TypeExpressionContext)newExactContext).UnresolvedType);
//					if (newExactContext == null) {
//						int j = completionContext.TriggerOffset - 4;
//						
//						string yieldToken = GetPreviousToken (ref j, true);
//						if (token == "return") {
//							NRefactoryResolver resolver = CreateResolver ();
//							resolver.SetupResolver (new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//							IReturnType returnType = resolver.CallingMember.ReturnType;
//							if (yieldToken == "yield" && returnType.GenericArguments.Count > 0)
//								returnType = returnType.GenericArguments [0];
//							if (resolver.CallingMember != null)
//								return CreateTypeCompletionData (location, callingType, newExactContext, null, returnType);
//						}
//					}
//					return CreateCtrlSpaceCompletionData (completionContext, null);
//				case "if":
//				case "elif":
//					if (stateTracker.Engine.IsInsidePreprocessorDirective) 
//						return GetDefineCompletionData ();
//					return null;
				case "yield":
					var yieldDataList = new CompletionDataWrapper (this);
					yieldDataList.Result.DefaultCompletionString = "return";
					yieldDataList.Result.Add ("break", "md-keyword");
					yieldDataList.Result.Add ("return", "md-keyword");
					return yieldDataList.Result;
//				case "where":
//					CompletionDataList whereDataList = new CompletionDataList ();
//					NRefactoryResolver constraintResolver = CreateResolver ();
//					constraintResolver.SetupResolver (new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
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
//					CompletionDataCollector col = new CompletionDataCollector (this, dom, dataList, Document.CompilationUnit, null, new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//					foreach (string kw in linqKeywords) {
//						col.Add (kw, "md-keyword");
//					}
//					return dataList;
//				}
			return null;
		}
		
		bool IsLineEmptyUpToEol ()
		{
			var line = Editor.GetLine (Editor.Caret.Line);
			for (int j = Editor.Caret.Offset; j < line.EndOffset; j++) {
				char ch = Editor.GetCharAt (j);
				if (!char.IsWhiteSpace (ch))
					return false;
			}
			return true;
		}
		
		CompletionDataList GetOverrideCompletionData (CodeCompletionContext completionCtx, ITypeDefinition type, string modifiers)
		{
			CompletionDataWrapper wrapper = new CompletionDataWrapper (this);
			Dictionary<string, bool> alreadyInserted = new Dictionary<string, bool> ();
			bool addedVirtuals = false;
			
			int declarationBegin = completionCtx.TriggerOffset;
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
			
			foreach (var baseType in type.GetAllBaseTypeDefinitions (ctx)) {
				AddVirtuals (completionCtx, alreadyInserted, wrapper, type, modifiers, baseType, declarationBegin);
				addedVirtuals = true;
			}
			if (!addedVirtuals)
				AddVirtuals (completionCtx, alreadyInserted, wrapper, type, modifiers, ctx.GetTypeDefinition (typeof(object)), declarationBegin);
			return wrapper.Result;
		}
		
		void AddVirtuals (CodeCompletionContext completionCtx, Dictionary<string, bool> alreadyInserted, CompletionDataWrapper col, ITypeDefinition type, string modifiers, ITypeDefinition curType, int declarationBegin)
		{
			if (curType == null)
				return;
			var amb = new CSharpAmbience ();
			foreach (var m in curType.Methods.Where (m => !m.IsConstructor && !m.IsDestructor).Cast<IMember> ().Concat (curType.Properties.Cast<IMember> ())) {
				if (m.IsSynthetic || curType.ClassType != ClassType.Interface && !(m.IsVirtual || m.IsOverride || m.IsAbstract))
					continue;
				
				// filter out the "Finalize" methods, because finalizers should be done with destructors.
				if (m is IMethod && m.Name == "Finalize")
					continue;
				
				var data = new NewOverrideCompletionData (this, declarationBegin, type, m);
				string text = amb.GetString (ctx, m, OutputFlags.ClassBrowserEntries);
				// check if the member is already implemented
				bool foundMember = type.Members.Any (cm => amb.GetString (ctx, cm, OutputFlags.ClassBrowserEntries) == text);
				
				if (!foundMember && !alreadyInserted.ContainsKey (text)) {
					alreadyInserted[text] = true;
					data.CompletionCategory = col.GetCompletionCategory (curType);
					col.Result.Add (data);
				}
			}
		}
		
		void AddContextCompletion (CompletionDataWrapper wrapper, CSharpResolver state, AstNode node)
		{
			if (state == null) 
				return;
			foreach (var variable in state.LocalVariables) {
				wrapper.AddVariable (variable);
			}
			if (state.CurrentMember is IParameterizedMember) {
				var param = (IParameterizedMember)state.CurrentMember;
				foreach (var p in param.Parameters) {
					wrapper.AddVariable (p);
				}
			}
			
			if (state.CurrentMember is IMethod) {
				var method = (IMethod)state.CurrentMember;
				foreach (var p in method.TypeParameters) {
					wrapper.AddTypeParameter (p);
				}
			}
			
			AddTypesAndNamespaces (wrapper, state);
			
			wrapper.Result.Add (new CompletionData ("global", "md-keyword"));
			if (state.CurrentMember != null) {
				AddKeywords (wrapper, statementStart);
				AddKeywords (wrapper, expressionLevel);
			} else if (state.CurrentTypeDefinition != null) {
				AddKeywords (wrapper, typeLevel);
			} else {
				AddKeywords (wrapper, globalLevel);
			}
			
			AddKeywords (wrapper, primitiveTypes);
			
			CodeTemplateService.AddCompletionDataForMime ("text/x-csharp", wrapper.Result);
		}

		void AddTypesAndNamespaces (CompletionDataWrapper wrapper, CSharpResolver state, Predicate<ITypeDefinition> typePred = null, Predicate<IMember> memberPred = null)
		{
			if (state.CurrentTypeDefinition != null) {
				
				for (var ct = state.CurrentTypeDefinition; ct != null; ct = ct.DeclaringTypeDefinition) {
					foreach (var nestedType in ct.NestedTypes) {
						if (typePred == null || typePred (nestedType))
							wrapper.AddType (nestedType, nestedType.Name);
					}
				}
				
				foreach (var member in state.CurrentTypeDefinition.Resolve (ctx).GetMembers (ctx)) {
					if (memberPred == null || memberPred (member))
						wrapper.AddMember (member);
				}
				
				foreach (var p in state.CurrentTypeDefinition.TypeParameters) {
					wrapper.AddTypeParameter (p);
				}
			}
			
			for (var n = state.UsingScope; n != null; n = n.Parent) {
				foreach (var pair in n.UsingAliases) {
					wrapper.AddNamespace ("", pair.Key);
				}
				
				foreach (var u in n.Usings) {
					var ns = u.ResolveNamespace (ctx);
					if (ns == null)
						continue;
					foreach (var type in ctx.GetTypes (ns.NamespaceName, StringComparer.Ordinal)) {
						if (typePred == null || typePred (type))
							wrapper.AddType (type, type.Name);
					}
				}
				
				foreach (var type in ctx.GetTypes (n.NamespaceName, StringComparer.Ordinal)) {
					if (typePred == null || typePred (type))
						wrapper.AddType (type, type.Name);
				}
				
				foreach (var curNs in ctx.GetNamespaces ().Where (sn => sn.StartsWith (n.NamespaceName) && sn != n.NamespaceName)) {
					wrapper.AddNamespace (n.NamespaceName, curNs);
				}
			}
		}
		static string[] expressionLevel = new string [] { "as", "is", "else", "out", "ref", "null", "delegate", "default"};
		static string[] primitiveTypes = new string [] { "void", "object", "bool", "byte", "sbyte", "char", "short", "int", "long", "ushort", "uint", "ulong", "float", "double", "decimal", "string"};
		static string[] statementStart = new string [] { "base", "new", "sizeof", "this", 
			"true", "false", "typeof", "checked", "unchecked", "from", "break", "checked",
			"unchecked", "const", "continue", "do", "finally", "fixed", "for", "foreach",
			"goto", "if", "lock", "return", "stackalloc", "switch", "throw", "try", "unsafe", 
			"using", "while", "yield", "dynamic", "var" };
		static string[] globalLevel = new string [] {
			"namespace", "using", "extern", "public", "internal", 
			"class", "interface", "struct", "enum", "delegate",
			"abstract", "sealed", "static", "unsafe", "partial"
		};
		
		static string[] typeLevel = new string [] {
			"public", "internal", "protected", "private",
			"class", "interface", "struct", "enum", "delegate",
			"abstract", "sealed", "static", "unsafe", "partial",
			"const", "event", "extern", "fixed","new", 
			"operator", "explicit", "implicit", 
			"override", "readonly", "virtual", "volatile"
		};
		static string[] linqKeywords = new string[] { "from", "where", "select", "group", "into", "orderby", "join", "let", "in", "on", "equals", "by", "ascending", "descending" };
		
		static void AddKeywords (CompletionDataWrapper wrapper, IEnumerable<string> keywords)
		{
			foreach (string keyword in keywords) {
				wrapper.Result.Add (new CompletionData (keyword, "md-keyword"));
			}
		}

		
		ICompletionDataList CreateTypeList ()
		{
			var ctx = Document.TypeResolveContext;
			var result = new ProjectDomCompletionDataList ();
				
			foreach (var cl in ctx.GetTypes ("", StringComparer.Ordinal)) {
				result.Add (new CompletionData (cl.Name, cl.GetStockIcon ()));
			}
			foreach (var ns in ctx.GetNamespaces ()) {
				string name = ns;
				int idx = name.IndexOf (".");
				if (idx >= 0)
					name = name.Substring (0, idx);
				result.Add (new CompletionData (name, Stock.Namespace));
			}
			
			return result;
		}
		
		class CompletionDataWrapper
		{
			CSharpCompletionTextEditorExtension completion;
			CompletionDataList result = new ProjectDomCompletionDataList ();
			
			public CompletionDataList Result {
				get {
					return result;
				}
			}
			
			public CompletionDataWrapper (CSharpCompletionTextEditorExtension completion)
			{
				this.completion = completion;
			}

			public void AddCustom (string displayText, string icon, string description, string completionText)
			{
				result.Add (displayText, icon, description, completionText);
			}
			
			HashSet<string> usedNamespaces = new HashSet<string> ();
			
			public void AddNamespace (string curNamespace, string fullNamespace)
			{
				string name;
				// crop prefix
				if (!string.IsNullOrEmpty (curNamespace)) {
					name = fullNamespace.Substring (curNamespace.Length + 1);
				} else {
					name = fullNamespace;
				}
				
				// crop suffix
				int idx = name.IndexOf (".");
				if (idx >= 0)
					name = name.Substring (0, idx);
				
				if (string.IsNullOrEmpty (name) || usedNamespaces.Contains (name))
					return;
				usedNamespaces.Add (name);
				result.Add (new CompletionData (name, Stock.Namespace));
			}
			
			HashSet<string> usedTypes = new HashSet<string> ();
			public void AddType (IType type, string shortType)
			{
				if (type == null || string.IsNullOrEmpty (shortType) || usedTypes.Contains (shortType))
					return;
				usedTypes.Add (shortType);
				result.Add (new CompletionData (shortType, type.GetStockIcon ()));
			}
			
			Dictionary<string, List<MemberCompletionData>> data = new Dictionary<string, List<MemberCompletionData>> ();
			
			public void AddVariable (IVariable variable)
			{
				if (data.ContainsKey (variable.Name))
					return;
				data[variable.Name] = new List<MemberCompletionData> ();
				result.Add (new CompletionData (variable.Name, variable.GetStockIcon ()));
			}
			
			public void AddTypeParameter (ITypeParameter variable)
			{
				if (data.ContainsKey (variable.Name))
					return;
				data[variable.Name] = new List<MemberCompletionData> ();
				result.Add (new CompletionData (variable.Name, variable.GetStockIcon ()));
			}
			
			public MemberCompletionData AddMember (IMember member)
			{
				return AddMember (member, OutputFlags.IncludeGenerics | OutputFlags.HideArrayBrackets /* | additionalFlags*/);
			}
			
			public MemberCompletionData AddMember (IMember member, OutputFlags flags)
			{
				var newData = new MemberCompletionData (completion, member, flags);
//				newData.HideExtensionParameter = HideExtensionParameter;
				string memberKey = newData.CompletionText;
				if (memberKey == null)
					return null;
				if (member is IMember) {
					newData.CompletionCategory = GetCompletionCategory (member.DeclaringTypeDefinition);
				}
				List<MemberCompletionData> existingData;
				data.TryGetValue (memberKey, out existingData);
				
				if (existingData != null) {
					var a = member as IEntity;
					foreach (MemberCompletionData md in existingData) {
						var b = md.Member as IEntity;
						if (a == null || b == null || a.EntityType == b.EntityType) {
							md.AddOverload (newData);
							newData = null;
							break;
						} 
					}
					if (newData != null) {
						result.Add (newData);
						data[memberKey].Add (newData);
					}
				} else {
					result.Add (newData);
					data[memberKey] = new List<MemberCompletionData> ();
					data[memberKey].Add (newData);
				}
				return newData;
			}
			
			internal CompletionCategory GetCompletionCategory (ITypeDefinition type)
			{
				if (type == null)
					return null;
				if (!completionCategories.ContainsKey (type))
					completionCategories[type] = new TypeCompletionCategory (type);
				return completionCategories[type];
			}
			
			Dictionary<ITypeDefinition, CompletionCategory> completionCategories = new Dictionary<ITypeDefinition, CompletionCategory> ();
			class TypeCompletionCategory : CompletionCategory
			{
				public ITypeDefinition Type {
					get;
					private set;
				}
				
				public TypeCompletionCategory (ITypeDefinition type) : base (type.FullName, type.GetStockIcon ())
				{
					this.Type = type;
				}
				
				public override int CompareTo (CompletionCategory other)
				{
					TypeCompletionCategory compareCategory = other as TypeCompletionCategory;
					if (compareCategory == null)
						return 1;
					
					if (Type.ReflectionName == compareCategory.Type.ReflectionName)
						return 0;
					
					// System.Object is always the smallest
					if (Type.ReflectionName == "System.Object") 
						return -1;
					if (compareCategory.Type.ReflectionName == "System.Object")
						return 1;
					
/*					if (Type.GetProjectContent () != null) {
						if (Type.GetProjectContent ().GetInheritanceTree (Type).Any (t => t != null && t.DecoratedFullName == compareCategory.Type.DecoratedFullName))
							return 1;
						return -1;
					}
					
					// source project dom == null - try to make the opposite comparison
					if (compareCategory.Type.GetProjectContent () != null && compareCategory.Type.GetProjectContent ().GetInheritanceTree (Type).Any (t => t != null && t.DecoratedFullName == Type.DecoratedFullName))
						return -1;*/
					return 1;
				}
			}
		}
		
		bool IsAccessibleFrom (IEntity member, ITypeDefinition calledType, IMember currentMember, bool includeProtected)
		{
			if (currentMember == null)
				return member.IsStatic || member.IsPublic;
//			if (currentMember is MonoDevelop.Projects.Dom.BaseResolveResult.BaseMemberDecorator) 
//				return member.IsPublic | member.IsProtected;
	//		if (member.IsStatic && !IsStatic)
	//			return false;
			if (member.IsPublic || calledType != null && calledType.ClassType == ClassType.Interface && !member.IsProtected)
				return true;
			if (member.DeclaringTypeDefinition != null) {
				if (member.DeclaringTypeDefinition.ClassType == ClassType.Interface) 
					return IsAccessibleFrom (member.DeclaringTypeDefinition, calledType, currentMember, includeProtected);
			
				if (member.IsProtected && !(member.DeclaringTypeDefinition.IsProtectedOrInternal && !includeProtected))
					return includeProtected;
			}
			if (member.IsInternal || member.IsProtectedAndInternal || member.IsProtectedOrInternal) {
				var type1 = member is ITypeDefinition ? (ITypeDefinition)member : member.DeclaringTypeDefinition;
				var type2 = currentMember is ITypeDefinition ? (ITypeDefinition)currentMember : currentMember.DeclaringTypeDefinition;
				bool result;
				// easy case, projects are the same
				if (type1.ProjectContent == type2.ProjectContent) {
					result = true; 
				} else if (type1.ProjectContent != null && type1.ProjectContent.Annotation<MonoDevelop.Projects.Project> () != null) {
					// maybe type2 hasn't project dom set (may occur in some cases), check if the file is in the project
					result = type1.ProjectContent.Annotation<MonoDevelop.Projects.Project> ().GetProjectFile (type2.Region.FileName) != null;
				} else if (type2.ProjectContent != null && type2.ProjectContent.Annotation<MonoDevelop.Projects.Project> () != null) {
					result = type2.ProjectContent.Annotation<MonoDevelop.Projects.Project> ().GetProjectFile (type1.Region.FileName) != null;
				} else {
					// should never happen !
					result = true;
				}
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
		
		ICompletionDataList CreateTypeAndNamespaceCompletionData (AstLocation location, ResolveResult resolveResult, AstNode resolvedNode, CSharpResolver state)
		{
			if (resolveResult == null || resolveResult.IsError)
				return null;
			var ctx = Document.TypeResolveContext;
			var result = new CompletionDataWrapper (this);
			
			if (resolveResult is NamespaceResolveResult) {
				var nr = (NamespaceResolveResult)resolveResult;
				foreach (var cl in ctx.GetTypes (nr.NamespaceName, StringComparer.Ordinal)) {
					result.AddType (cl, cl.Name);
				}
				foreach (var ns in ctx.GetNamespaces ().Where (n => n.Length > nr.NamespaceName.Length && n.StartsWith (nr.NamespaceName))) {
					result.AddNamespace (nr.NamespaceName, ns);
				}
			} else if (resolveResult is TypeResolveResult) {
				var type = resolveResult.Type.Resolve (ctx);
				foreach (var nested in type.GetNestedTypes (ctx)) {
					result.AddType (nested, nested.Name);
				}
			}
			return result.Result;
		}
		
		ICompletionDataList CreateCompletionData (AstLocation location, ResolveResult resolveResult, AstNode resolvedNode, CSharpResolver state)
		{
			if (resolveResult == null || resolveResult.IsError)
				return null;
			var ctx = Document.TypeResolveContext;
			if (resolveResult is NamespaceResolveResult) {
				var nr = (NamespaceResolveResult)resolveResult;
				var namespaceContents = new CompletionDataWrapper (this);
				foreach (var cl in ctx.GetTypes (nr.NamespaceName, StringComparer.Ordinal)) {
					namespaceContents.AddType (cl, cl.Name);
				}
				foreach (var ns in ctx.GetNamespaces ().Where (n => n.Length > nr.NamespaceName.Length && n.StartsWith (nr.NamespaceName))) {
					namespaceContents.AddNamespace (nr.NamespaceName, ns);
				}
				
				return namespaceContents.Result;
			}
			var type = resolveResult.Type.Resolve (ctx);
			var typeDef = type.GetDefinition ();
			var lookup = new MemberLookup (ctx, currentType, document.GetProjectContext ());
			var result = new CompletionDataWrapper (this);
			bool isProtectedAllowed = false;
			bool includeStaticMembers = false;
			
			if (resolveResult is LocalResolveResult) {
				isProtectedAllowed = currentType != null && typeDef != null ? typeDef.GetAllBaseTypeDefinitions (ctx).Any (bt => bt.Equals (currentType)) : false;
				if (resolvedNode is IdentifierExpression) {
					var mrr = (LocalResolveResult)resolveResult;
					includeStaticMembers  = mrr.Variable.Name == mrr.Type.Name;
				}
			} else {
				isProtectedAllowed = currentType != null && typeDef != null ? currentType.GetAllBaseTypeDefinitions (ctx).Any (bt => bt.Equals (typeDef)) : false;
			}
			if (resolveResult is TypeResolveResult && type.IsEnum ()) {
				foreach (var field in type.GetFields (ctx)) {
					result.AddMember (field);
				}
				foreach (var m in type.GetMethods (ctx)) {
					if (m.Name == "TryParse")
						result.AddMember (m);
				}
				return result.Result;
			}
			
			if (resolveResult is MemberResolveResult && resolvedNode is IdentifierExpression) {
				var mrr = (MemberResolveResult)resolveResult;
				includeStaticMembers  = mrr.Member.Name == mrr.Type.Name;
			}
			
//			Console.WriteLine ("type:" + type);
//			Console.WriteLine ("IS PROT ALLOWED:" + isProtectedAllowed);
//			Console.WriteLine (resolveResult);
//			Console.WriteLine (currentMember.IsStatic);
			foreach (var member in type.GetMembers (ctx)) {
				if (!lookup.IsAccessible (member, isProtectedAllowed)) {
//					Console.WriteLine ("skip access: " + member.FullName);
					continue;
				}
				if (resolvedNode is BaseReferenceExpression && member.IsAbstract)
					continue;
				
				if (!includeStaticMembers && member.IsStatic && !(resolveResult is TypeResolveResult)) {
//					Console.WriteLine ("skip static member: " + member.FullName);
					continue;
				}
				if (!member.IsStatic && (resolveResult is TypeResolveResult)) {
//					Console.WriteLine ("skip non static member: " + member.FullName);
					continue;
				}
//				Console.WriteLine ("add : "+ member.FullName + " --- " + member.IsStatic);
				result.AddMember (member);
			}
			
			if (resolveResult is TypeResolveResult || includeStaticMembers) {
				foreach (var nested in type.GetNestedTypes (ctx)) {
					result.AddType (nested, nested.Name);
				}
				
			} else {
				var baseTypes = new List<IType> (type.GetAllBaseTypes (ctx));
				var conv = new Conversions (ctx);
				for (var n = state.UsingScope; n != null; n = n.Parent) {
					AddExtensionMethods (result, conv, baseTypes, n.NamespaceName);
					foreach (var u in n.Usings) {
						var ns = u.ResolveNamespace (ctx);
						AddExtensionMethods (result, conv, baseTypes, ns.NamespaceName);
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

		void AddExtensionMethods (CompletionDataWrapper result, Conversions conv, List<IType> baseTypes, string namespaceName)
		{
			foreach (var typeDefinition in ctx.GetTypes (namespaceName, StringComparer.Ordinal).Where (t => t.IsStatic && t.HasExtensionMethods)) {
				foreach (var m in typeDefinition.Methods.Where (m => m.IsExtensionMethod )) {
					var pt= m.Parameters.First ().Type.Resolve (ctx);
					string reflectionName = pt is ParameterizedType ?  ((ParameterizedType)pt).GenericType.ReflectionName : pt.ReflectionName;
					if (baseTypes.Any (bt => (bt is ParameterizedType ?  ((ParameterizedType)bt).GenericType.ReflectionName : bt.ReflectionName) == reflectionName)) {
						result.AddMember (m);
					}
				}
			}
		}
						
		ICompletionDataList CreateParameterCompletion (MethodGroupResolveResult resolveResult, CSharpResolver state, AstNode invocation, int parameter)
		{
			var result = new CompletionDataWrapper (this);
			var addedEnums = new HashSet<string> ();
			var addedDelegates = new HashSet<string> ();
			
			foreach (var method in resolveResult.Methods) {
				if (method.Parameters.Count <= parameter)
					continue;
				var resolvedType = method.Parameters [parameter].Type.Resolve (ctx);
				if (resolvedType.IsEnum ()) {
					if (addedEnums.Contains (resolvedType.ReflectionName))
						continue;
					addedEnums.Add (resolvedType.ReflectionName);
					AddEnumMembers (result, resolvedType, state);
				} else if (resolvedType.IsDelegate ()) {
//					if (addedDelegates.Contains (resolvedType.DecoratedFullName))
//						continue;
//					addedDelegates.Add (resolvedType.DecoratedFullName);
//					string parameterDefinition = AddDelegateHandlers (completionList, resolvedType, false, addedDelegates.Count == 1);
//					string varName = "Handle" + method.Parameters [parameter].ReturnType.Name + method.Parameters [parameter].Name;
//					result.Add (new EventCreationCompletionData (textEditorData, varName, resolvedType, null, parameterDefinition, resolver.Unit.GetMemberAt (location), resolvedType) { AddSemicolon = false });
				
				}
			}
			if (addedEnums.Count + addedDelegates.Count == 0)
				return null;
			result.Result.AutoCompleteEmptyMatch = false;
			result.Result.AutoSelect = false;
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
			var shortType = builder.ConvertType (type);
			using (var w = new System.IO.StringWriter ()) {
				var visitor = new OutputVisitor (w, FormattingPolicy.CreateOptions ());
				shortType.AcceptVisitor (visitor, null);
				return w.ToString ();
			}
		}
		
		void AddEnumMembers (CompletionDataWrapper completionList, IType resolvedType, CSharpResolver state)
		{
			if (!resolvedType.IsEnum ())
				return;
			string typeString = GetShortType (resolvedType, state);
			if (typeString.Contains ("."))
				completionList.Result.Add (typeString, resolvedType.GetStockIcon ());
			foreach (var field in resolvedType.GetFields (ctx)) {
				if (field.IsConst || field.IsStatic)
					completionList.Result.Add (typeString + "." + field.Name, field.GetStockIcon ());
			}
			completionList.Result.DefaultCompletionString = typeString;
		}
		
		public override IParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext, char completionChar)
		{
			if (completionChar != '(' && completionChar != '<' && completionChar != '[')
				return null;
			if (IsInsideComment () || IsInsideString ())
				return null;
			this.currentType = Document.ParsedDocument.GetTypeDefinition (Editor.Caret.Line, Editor.Caret.Column);
			this.currentMember = Document.ParsedDocument.GetMember (Editor.Caret.Line, Editor.Caret.Column);
			
			var invoke = GetInvocationBeforeCursor (true);
			if (invoke == null)
				return null;
			
			ResolveResult resolveResult;
			switch (completionChar) {
			case '(':
				if (invoke.Item2 is ObjectCreateExpression) {
					var createType = ResolveExpression (invoke.Item1, ((ObjectCreateExpression)invoke.Item2).Type, invoke.Item3);
					return new ConstructorParameterDataProvider (this, createType.Item1.Type);
				}
				
				var invocationExpression = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
				
				if (invocationExpression == null || invocationExpression.Item1 == null || invocationExpression.Item1.IsError)
					return null;
				resolveResult = invocationExpression.Item1;
				if (resolveResult is MethodGroupResolveResult)
					return new MethodParameterDataProvider (this, resolveResult as MethodGroupResolveResult);
				if (resolveResult is MemberResolveResult )
				if (resolveResult.Type.IsDelegate ()) {
					return new DelegateDataProvider (this, resolveResult.Type);
				}
				
//				if (result.ExpressionContext == ExpressionContext.Attribute) {
//					IReturnType returnType = resolveResult.ResolvedType;
//					
//					IType type = resolver.SearchType (result.Expression.Trim () + "Attribute");
//					if (type == null) 
//						type = resolver.SearchType (returnType);
//					if (type != null && returnType != null && returnType.GenericArguments != null)
//						type = dom.CreateInstantiatedGenericType (type, returnType.GenericArguments);
//					return new NRefactoryParameterDataProvider (textEditorData, resolver, type);
//				}
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
//				return new NRefactoryTemplateParameterDataProvider (textEditorData, resolver, GetUsedNamespaces (), result, new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
//			case '[': {
//				ResolveResult resolveResult = resolver.Resolve (result, new AstLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
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
			var scope = ParsedFile.GetUsingScope (new AstLocation (document.Editor.Caret.Line, document.Editor.Caret.Column));
			List<string> result = new List<string> ();
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
		
		#region Preprocessor
		CompletionDataList GetDefineCompletionData ()
		{
			if (Document.Project == null)
				return null;
			
			var symbols = new Dictionary<string, string> ();
			var cp = new ProjectDomCompletionDataList ();
			foreach (DotNetProjectConfiguration conf in Document.Project.Configurations) {
				var cparams = conf.CompilationParameters as CSharpCompilerParameters;
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
			var cp = new CompletionDataList ();
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
		static readonly List<string> commentTags = new List<string> (new string[] { "c", "code", "example", "exception", "include", "list", "listheader", "item", "term", "description", "para", "param", "paramref", "permission", "remarks", "returns", "see", "seealso", "summary", "value" });
		
		CompletionDataList GetXmlDocumentationCompletionData ()
		{
			var result = new CompletionDataList ();
			result.Add ("c", "md-literal", GettextCatalog.GetString ("Set text in a code-like font"));
			result.Add ("code", "md-literal", GettextCatalog.GetString ("Set one or more lines of source code or program output"));
			result.Add ("example", "md-literal", GettextCatalog.GetString ("Indicate an example"));
			result.Add ("exception", "md-literal", GettextCatalog.GetString ("Identifies the exceptions a method can throw"), "exception cref=\"|\"></exception>");
			result.Add ("include", "md-literal", GettextCatalog.GetString ("Includes comments from a external file"), "include file=\"|\" path=\"\">");
			result.Add ("list", "md-literal", GettextCatalog.GetString ("Create a list or table"), "list type=\"|\">");
			
			result.Add ("listheader", "md-literal", GettextCatalog.GetString ("Define the heading row"));
			result.Add ("item", "md-literal", GettextCatalog.GetString ("Defines list or table item"));
			result.Add ("term", "md-literal", GettextCatalog.GetString ("A term to define"));
			result.Add ("description", "md-literal", GettextCatalog.GetString ("Describes a list item"));
			result.Add ("para", "md-literal", GettextCatalog.GetString ("Permit structure to be added to text"));

			result.Add ("param", "md-literal", GettextCatalog.GetString ("Describe a parameter for a method or constructor"), "param name=\"|\">");
			result.Add ("paramref", "md-literal", GettextCatalog.GetString ("Identify that a word is a parameter name"), "paramref name=\"|\"/>");
			
			result.Add ("permission", "md-literal", GettextCatalog.GetString ("Document the security accessibility of a member"), "permission cref=\"|\"");
			result.Add ("remarks", "md-literal", GettextCatalog.GetString ("Describe a type"));
			result.Add ("returns", "md-literal", GettextCatalog.GetString ("Describe the return value of a method"));
			result.Add ("see", "md-literal", GettextCatalog.GetString ("Specify a link"), "see cref=\"|\"/>");
			result.Add ("seealso", "md-literal", GettextCatalog.GetString ("Generate a See Also entry"), "seealso cref=\"|\"/>");
			result.Add ("summary", "md-literal", GettextCatalog.GetString ("Describe a member of a type"));
			result.Add ("typeparam", "md-literal", GettextCatalog.GetString ("Describe a type parameter for a generic type or method"));
			result.Add ("typeparamref", "md-literal", GettextCatalog.GetString ("Identify that a word is a type parameter name"));
			result.Add ("value", "md-literal", GettextCatalog.GetString ("Describe a property"));
			
			return result;
		}		
		#endregion
		
	}
}
