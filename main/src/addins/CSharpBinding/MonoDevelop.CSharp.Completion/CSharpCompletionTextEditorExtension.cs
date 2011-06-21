// 
// CSharpCompletionTextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
			
			Document.DocumentParsed += delegate {
				var unit = Document.ParsedDocument;
				if (unit == null) 
					return;
				this.Unit = document.ParsedDocument.Annotation<CompilationUnit> ();
				this.ParsedFile = document.ParsedDocument.Annotation<ParsedFile> ();
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
			return Unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Comment> (loc.Line, loc.Column) != null;
		}
		
		bool IsInsideDocComment ()
		{
			var loc = Document.Editor.Caret.Location;
			var cmt = Unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Comment> (loc.Line, loc.Column);
			return cmt != null && cmt.CommentType == CommentType.Documentation;
		}

		bool IsInsideString ()
		{
			var loc = Document.Editor.Caret.Location;
			var expr = Unit.GetNodeAt<PrimitiveExpression> (loc.Line, loc.Column );
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
		
		Tuple<ResolveResult, CSharpResolver> ResolveExpression (ParsedFile file, Expression expr, CompilationUnit unit)
		{
			if (expr == null)
				return null;
			var csResolver = new CSharpResolver (ctx, System.Threading.CancellationToken.None);
			var navigator = new NodeListResolveVisitorNavigator (new[] { expr });
			var visitor = new ResolveVisitor (csResolver, file, navigator);
			unit.AcceptVisitor (visitor, null);
			var result = visitor.Resolve (expr);

			var state = visitor.GetResolverStateBefore (expr);
			return Tuple.Create (result, state);
		}
		
		ITypeDefinition currentType;
		IMember currentMember;
		
		Tuple<ParsedFile, Expression, CompilationUnit> GetExpressionBeforeCursor ()
		{
			CSharpParser parser = new CSharpParser ();
			string text = Document.Editor.GetTextAt (0, Document.Editor.Caret.Offset);
			var stream = new System.IO.StringReader (text);
			var completionUnit = parser.Parse (stream, 0);
			stream.Close ();
			var expr = completionUnit.TopExpression as Expression;
			if (expr == null) {
				/// try to get this. or base.
				text += "a ();  } } }";
				stream = new System.IO.StringReader (text);
				completionUnit = parser.Parse (stream, 0);
				stream.Close ();
				
				var mref = completionUnit.GetNodeAt<MemberReferenceExpression> (document.Editor.Caret.Line, document.Editor.Caret.Column);
				if (mref != null) {
					expr = mref.Target.Clone ();
				} else {
					return null;
				}
			} else {
				text += "Console.WriteLine (\"a\"); } } }";
				stream = new System.IO.StringReader (text);
				parser = new CSharpParser ();
				completionUnit = parser.Parse (stream, 0);
				stream.Close ();
			}
			
			var type = completionUnit.GetNodeAt<TypeDeclaration> (expr.StartLocation);
			if (type == null)
				return null;
			
			bool nodeInserted = false;
			
			AstNode node = completionUnit.GetNodeAt<Statement> (document.Editor.Caret.Line, document.Editor.Caret.Column);
			if (node != null) {
				if (node is BlockStatement) {
					node.ReplaceWith (n => new BlockStatement () { Statements = { new ExpressionStatement (expr) } });
				} else {
					node.ReplaceWith (n => new ExpressionStatement (expr));
				}
				nodeInserted = true;
			} else {
				node = completionUnit.GetNodeAt<Expression> (document.Editor.Caret.Line, document.Editor.Caret.Column);
				if (node != null) {
					node.ReplaceWith (n => expr);
					nodeInserted = true;
				}
			}
			
			if (!nodeInserted) {
				var member = type.Members.LastOrDefault ();
				if (member == null)
					return null;
				if (member is MethodDeclaration) {
					((MethodDeclaration)member).Body.Add (new ExpressionStatement (expr));
				} else if (member is ConstructorDeclaration) {
					((ConstructorDeclaration)member).Body.Add (new ExpressionStatement (expr));
				} else if (member is DestructorDeclaration) {
					((DestructorDeclaration)member).Body.Add (new ExpressionStatement (expr));
				} else {
					return null;
				}
			}
//			
//			Console.WriteLine ("Parsed AST:");
//			var v = new OutputVisitor (Console.Out, new CSharpFormattingOptions ());
//			completionUnit.AcceptVisitor (v, null);
//			
			var tsvisitor = new TypeSystemConvertVisitor (Document.GetProjectContext (), Document.FileName);
			completionUnit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, expr, completionUnit);
		}
		
		Tuple<ParsedFile, Expression, CompilationUnit> GetExpressionAtCursor ()
		{
			CSharpParser parser = new CSharpParser ();
			string text = Document.Editor.GetTextAt (0, Document.Editor.Caret.Offset);
			text += "a(); } } }";
			var stream = new System.IO.StringReader (text);
			var completionUnit = parser.Parse (stream, 0);
			stream.Close ();
			
			var expr = completionUnit.GetNodeAt<Expression> (document.Editor.Caret.Line, document.Editor.Caret.Column);
			if (expr == null)
				return null;
			var tsvisitor = new TypeSystemConvertVisitor (Document.GetProjectContext (), Document.FileName);
			completionUnit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, expr, completionUnit);
		}
		
		Tuple<ParsedFile, Expression, CompilationUnit> GetInvocationBeforeCursor ()
		{
			CSharpParser parser = new CSharpParser ();
			string text = Document.Editor.GetTextAt (0, Document.Editor.Caret.Offset);
			text += "a); } } }";
			var stream = new System.IO.StringReader (text);
			var completionUnit = parser.Parse (stream, 0);
			stream.Close ();
			
			var expr = completionUnit.GetNodeAt<Expression> (document.Editor.Caret.Line, document.Editor.Caret.Column - 1);
			
			if (expr is InvocationExpression)
				expr = ((InvocationExpression)expr).Target;
			
			var tsvisitor = new TypeSystemConvertVisitor (Document.GetProjectContext (), Document.FileName);
			completionUnit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, expr, completionUnit);
		}
		
		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			if (textEditorData.CurrentMode is CompletionTextLinkMode) {
				if (!((CompletionTextLinkMode)textEditorData.CurrentMode).TriggerCodeCompletion)
					return null;
			} else if (textEditorData.CurrentMode is Mono.TextEditor.TextLinkEditMode) {
				return null;
			}
			this.currentType = Document.ParsedDocument.GetTypeDefinition (Editor.Caret.Line, Editor.Caret.Column);
			this.currentMember = Document.ParsedDocument.GetMember (Editor.Caret.Line, Editor.Caret.Column);

			
			
			var loc = new AstLocation (Document.Editor.Caret.Location.Line, Document.Editor.Caret.Location.Column);
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
					if (pexpr.Value is string || pexpr.Value is char || !pexpr.LiteralValue.Contains ('.'))
						return null;
				}
				var resolveResult = ResolveExpression (expr.Item1, expr.Item2, expr.Item3);
				if (resolveResult == null)
					return null;
				return CreateCompletionData (loc, resolveResult.Item1);
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
			
			case '(':
				if (IsInsideComment () || IsInsideString ())
					return null;
				var invoke = GetInvocationBeforeCursor ();
				if (invoke == null)
					return null;
				if (invoke.Item2 is TypeOfExpression)
					return CreateTypeList ();
				
				var invocationResult = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
				if (invocationResult == null)
					return null;
				var methodGroup = invocationResult.Item1 as MethodGroupResolveResult;
				if (methodGroup != null)
					return CreateParameterCompletion (methodGroup, invocationResult.Item2, invoke.Item2, 0);
				return null;
				
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
				var identifierStart = GetExpressionAtCursor ();
				if (identifierStart == null)
					return null;
				
				var csResolver = new CSharpResolver (ctx, System.Threading.CancellationToken.None);
				var navigator = new NodeListResolveVisitorNavigator (new[] { identifierStart.Item2 });
				var visitor = new ResolveVisitor (csResolver, identifierStart.Item1, navigator);
				identifierStart.Item3.AcceptVisitor (visitor, null);
				csResolver = visitor.GetResolverStateBefore (identifierStart.Item2);
				
				// identifier has already started with the first letter
				completionContext.TriggerOffset--;
				completionContext.TriggerLineOffset--;
				completionContext.TriggerWordLength = 1;
				
				var wrapper = new CompletionDataWrapper (this);
				AddContextCompletion (wrapper, csResolver, identifierStart.Item2);
				return wrapper.Result;
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
			
			if (state.CurrentTypeDefinition != null) {
				foreach (var member in state.CurrentTypeDefinition.Resolve (ctx).GetMembers (ctx)) {
					wrapper.AddMember (member);
				}
				
				foreach (var p in state.CurrentTypeDefinition.TypeParameters) {
					wrapper.AddTypeParameter (p);
				}
			}
			
			for (var n = state.UsingScope; n != null; n = n.Parent) {
				foreach (var pair in n.UsingAliases) {
					wrapper.AddNamespace (pair.Key);
				}
				foreach (var u in n.Usings) {
					var ns = u.ResolveNamespace (ctx);
					foreach (var type in ctx.GetTypes (ns.NamespaceName, StringComparer.Ordinal)) {
						wrapper.AddType (type);
					}
				}
			}
			
			foreach (var ns in ctx.GetNamespaces ()) {
				string name = ns;
				int idx = name.IndexOf (".");
				if (idx >= 0)
					name = name.Substring (0, idx);
				wrapper.AddNamespace (name);
			}

			
			wrapper.Result.Add (new CompletionData ("global", "md-keyword"));
			wrapper.Result.Add (new CompletionData ("var", "md-keyword"));
			
			AddKeywords (wrapper, primitiveTypes);
			AddKeywords (wrapper, statementStart);
		}
		
		static string[] primitiveTypes = new string [] { "void", "object", "bool", "byte", "sbyte", "char", "short", "int", "long", "ushort", "uint", "ulong", "float", "double", "decimal", "string"};
		static string[] statementStart = new string [] { "base", "new", "sizeof", "this", 
			"true", "false", "typeof", "checked", "unchecked", "from", "break", "checked",
			"unchecked", "const", "continue", "do", "finally", "fixed", "for", "foreach",
			"goto", "if", "lock", "return", "stackalloc", "switch", "throw", "try", "unsafe", 
			"using", "while", "yield", "dynamic" };

		
		static void AddKeywords (CompletionDataWrapper wrapper, string[] keywords)
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
			
			HashSet<string> usedNamespaces = new HashSet<string> ();
			
			public void AddNamespace (string name)
			{
				if (string.IsNullOrEmpty (name) || usedNamespaces.Contains (name))
					return;
				usedNamespaces.Add (name);
				result.Add (new CompletionData (name, Stock.Namespace));
			}
			
			HashSet<string> usedTypes = new HashSet<string> ();
			public void AddType (ITypeDefinition type)
			{
				if (type == null || usedTypes.Contains (type.Name))
					return;
				usedTypes.Add (type.Name);
				result.Add (new CompletionData (type.Name, type.GetStockIcon ()));
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
					var a = member;
					foreach (MemberCompletionData md in existingData) {
						var b = md.Member;
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
				
		
		ICompletionDataList CreateCompletionData (AstLocation location, ResolveResult resolveResult)
		{
			if (resolveResult == null || resolveResult.IsError)
				return null;
			var ctx = Document.TypeResolveContext;
			if (resolveResult is NamespaceResolveResult) {
				var nr = (NamespaceResolveResult)resolveResult;
				var namespaceContents = new CompletionDataWrapper (this);
				foreach (var cl in ctx.GetTypes (nr.NamespaceName, StringComparer.Ordinal)) {
					namespaceContents.AddType (cl);
				}
				foreach (var ns in ctx.GetNamespaces ().Where (n => n.Length > nr.NamespaceName.Length && n.StartsWith (nr.NamespaceName))) {
					string name = ns.Substring (nr.NamespaceName.Length + 1);
					int idx = name.IndexOf (".");
					if (idx >= 0)
						name = name.Substring (0, idx);
					namespaceContents.AddNamespace (name);
				}
				
				return namespaceContents.Result;
			}
			var type = resolveResult.Type.Resolve (ctx);
			var typeDef = type.GetDefinition ();
			var lookup = new MemberLookup (ctx, currentType, document.GetProjectContext ());
			var result = new CompletionDataWrapper (this);
			
			bool isProtectedAllowed = currentType != null ? currentType.GetAllBaseTypeDefinitions (ctx).Any (bt => bt.FullName == typeDef.FullName && bt.TypeParameterCount == typeDef.TypeParameterCount) : false;
			
			foreach (var member in type.GetMembers (ctx)) {
				if (!lookup.IsAccessible (member, isProtectedAllowed)) {
					Console.WriteLine ("skip:" + member.FullName);
					continue;
				}
				if ((currentMember.IsStatic || resolveResult is TypeResolveResult) ^ member.IsStatic) 
					continue;
				Console.WriteLine ("add: "+ member.FullName);
				result.AddMember (member);
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
					AddEnumMembers (result, resolvedType);
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
		
		void AddEnumMembers (CompletionDataWrapper completionList, IType resolvedType)
		{
			if (!resolvedType.IsEnum ())
				return;
			string typeString = resolvedType.FullName; //  Document.CompilationUnit.ShortenTypeName (new DomReturnType (resolvedType), new AstLocation (Document.Editor.Caret.Line, Document.Editor.Caret.Column)).ToInvariantString ();
			Console.WriteLine ("type string:" + typeString);
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
			var invoke = GetInvocationBeforeCursor ();
			if (invoke == null)
				return null;
			
			this.currentType = Document.ParsedDocument.GetTypeDefinition (Editor.Caret.Line, Editor.Caret.Column);
			this.currentMember = Document.ParsedDocument.GetMember (Editor.Caret.Line, Editor.Caret.Column);
			
			ResolveResult resolveResult;
			switch (completionChar) {
			case '(': 
				var invocationExpression = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
				if (invocationExpression == null || invocationExpression.Item1 == null || invocationExpression.Item1.IsError)
					return null;
				resolveResult = invocationExpression.Item1;
				
				if (resolveResult is MethodGroupResolveResult)
					return new NRefactoryParameterDataProvider (this, resolveResult as MethodGroupResolveResult);
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
//				if (result.ExpressionContext is ExpressionContext.TypeExpressionContext) {
//					IReturnType returnType = resolveResult.ResolvedType ?? ((ExpressionContext.TypeExpressionContext)result.ExpressionContext).Type;
//					
//					IType type = resolver.SearchType (returnType);
//					if (type != null && returnType.GenericArguments != null)
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
				
			}
			return null;
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
