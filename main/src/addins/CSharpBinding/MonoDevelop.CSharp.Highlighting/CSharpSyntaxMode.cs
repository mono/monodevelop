// 
// SyntaxMode.cs
//  
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor;
using System.Xml;
using MonoDevelop.Projects;
using MonoDevelop.CSharp.Project;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Tasks;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.IO;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.SourceEditor.QuickTasks;
using System.Threading;
using System.Diagnostics;


namespace MonoDevelop.CSharp.Highlighting
{
	static class StringHelper
	{
		public static bool IsAt (this string str, int idx, string pattern)
		{
			if (idx + pattern.Length > str.Length)
				return false;

			for (int i = 0; i < pattern.Length; i++)
				if (pattern [i] != str [idx + i])
					return false;
			return true;
		}
	}
	
	public class CSharpSyntaxMode : Mono.TextEditor.Highlighting.SyntaxMode, IQuickTaskProvider
	{
		Document guiDocument;
		SyntaxTree unit;
		CSharpUnresolvedFile parsedFile;
		ICompilation compilation;
		CSharpAstResolver resolver;
		CancellationTokenSource src = null;

		internal class StyledTreeSegment : TreeSegment
		{
			public string Style {
				get;
				private set;
			}
			
			public StyledTreeSegment (int offset, int length, string style) : base (offset, length)
			{
				Style = style;
			}
		}
		
		class HighlightingSegmentTree : SegmentTree<StyledTreeSegment>
		{
			public bool GetStyle (Chunk chunk, ref int endOffset, out string style)
			{
				var segment = GetSegmentsAt (chunk.Offset).FirstOrDefault (s => s.Offset == chunk.Offset);
				if (segment == null) {
					style = null;
					return false;
				}
				endOffset = segment.EndOffset;
				style = segment.Style;
				return true;
			}
			
			public void AddStyle (int startOffset, int endOffset, string style)
			{
				if (IsDirty)
					return;
				Add (new StyledTreeSegment (startOffset, endOffset - startOffset, style));
			}
		}
		
		HighlightingSegmentTree highlightedSegmentCache = new HighlightingSegmentTree ();
		
		public bool DisableConditionalHighlighting {
			get;
			set;
		}
		
		protected override void OnDocumentSet (EventArgs e)
		{
			if (guiDocument != null) {
				guiDocument.DocumentParsed -= HandleDocumentParsed;
				highlightedSegmentCache.RemoveListener (guiDocument.Editor.Document);
			}
			guiDocument = null;
			
			base.OnDocumentSet (e);
		}
		
		void HandleDocumentParsed (object sender, EventArgs e)
		{
			if (src != null)
				src.Cancel ();
			resolver = null;
			if (guiDocument != null && MonoDevelop.Core.PropertyService.Get ("EnableSemanticHighlighting", true)) {
				var parsedDocument = guiDocument.ParsedDocument;
				if (parsedDocument != null) {
					unit = parsedDocument.GetAst<SyntaxTree> ();
					parsedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;
					if (guiDocument.Project != null && guiDocument.IsCompileableInProject) {
						src = new CancellationTokenSource ();
						var cancellationToken = src.Token;
						System.Threading.Tasks.Task.Factory.StartNew (delegate {
							Thread.Sleep (100);
							compilation = guiDocument.Compilation;
							var newResolver = new CSharpAstResolver (compilation, unit, parsedFile);
							var visitor = new QuickTaskVisitor (newResolver, cancellationToken);
							try {
								unit.AcceptVisitor (visitor);
							} catch (Exception) {
								return;
							}
							if (!cancellationToken.IsCancellationRequested) {
								Gtk.Application.Invoke (delegate {
									if (cancellationToken.IsCancellationRequested)
										return;
									var editorData = guiDocument.Editor;
									if (editorData == null)
										return;
									resolver = newResolver;
									quickTasks = visitor.QuickTasks;
									OnTasksUpdated (EventArgs.Empty);
									var textEditor = editorData.Parent;
									if (textEditor != null) {
										var margin = textEditor.TextViewMargin;
										if (!parsedDocument.HasErrors) {
											highlightedSegmentCache.Clear ();
											margin.PurgeLayoutCache ();
											textEditor.QueueDraw ();
										}
									}
								});
							}
						}, cancellationToken);
					}
				}
			}
		}

		class QuickTaskVisitor : DepthFirstAstVisitor
		{
			internal List<QuickTask> QuickTasks = new List<QuickTask> ();
			readonly CSharpAstResolver resolver;
			readonly CancellationToken cancellationToken;

			public QuickTaskVisitor (ICSharpCode.NRefactory.CSharp.Resolver.CSharpAstResolver resolver, CancellationToken cancellationToken)
			{
				this.resolver = resolver;
			}
			
			protected override void VisitChildren (AstNode node)
			{
				if (cancellationToken.IsCancellationRequested)
					return;
				base.VisitChildren (node);
			}

			public override void VisitIdentifierExpression (IdentifierExpression identifierExpression)
			{
				base.VisitIdentifierExpression (identifierExpression);
				
				var result = resolver.Resolve (identifierExpression, cancellationToken);
				if (result.IsError) {
					QuickTasks.Add (new QuickTask (string.Format ("error CS0103: The name `{0}' does not exist in the current context", identifierExpression.Identifier), identifierExpression.StartLocation, Severity.Error));
				}
			}

			public override void VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression)
			{
				base.VisitMemberReferenceExpression (memberReferenceExpression);
				var result = resolver.Resolve (memberReferenceExpression, cancellationToken) as UnknownMemberResolveResult;
				if (result != null && result.TargetType.Kind != TypeKind.Unknown) {
					QuickTasks.Add (new QuickTask (string.Format ("error CS0117: `{0}' does not contain a definition for `{1}'", result.TargetType.FullName, memberReferenceExpression.MemberName), memberReferenceExpression.MemberNameToken.StartLocation, Severity.Error));
				}
			}
		}
		
		static CSharpSyntaxMode ()
		{
			MonoDevelop.Debugger.DebuggingService.DisableConditionalCompilation += (EventHandler<DocumentEventArgs>)DispatchService.GuiDispatch (new EventHandler<DocumentEventArgs> (OnDisableConditionalCompilation));
			IdeApp.Workspace.ActiveConfigurationChanged += delegate {
				foreach (var doc in IdeApp.Workbench.Documents) {
					TextEditorData data = doc.Editor;
					if (data == null)
						continue;
					// Force syntax mode reparse (required for #if directives)
					doc.Editor.Document.SyntaxMode = doc.Editor.Document.SyntaxMode;
					doc.ReparseDocument ();
				}
			};
		}
		
		static void OnDisableConditionalCompilation (object s, MonoDevelop.Ide.Gui.DocumentEventArgs e)
		{
			var mode = e.Document.Editor.Document.SyntaxMode as CSharpSyntaxMode;
			if (mode == null)
				return;
			mode.DisableConditionalHighlighting = true;
			e.Document.Editor.Document.CommitUpdateAll ();
		}
		
		Dictionary<string, string> contextualHighlightKeywords = new Dictionary<string, string> ();
		static readonly string[] ContextualHighlightKeywordList = new string[] {
			"value"
		};
		
		static readonly HashSet<string> ContextualDehighlightKeywordList = new HashSet<string> (new string[] {
			"get", "set", "add", "remove", "var", "global", "partial", 
			"where",
			"select",
			"group",
			"by",
			"into",
			"from",
			"ascending",
			"descending",
			"orderby",
			"let",
			"join",
			"on",
			"equals"
		});
		
		public CSharpSyntaxMode ()
		{
			var provider = new ResourceXmlProvider (typeof(IXmlProvider).Assembly, typeof(IXmlProvider).Assembly.GetManifestResourceNames ().First (s => s.Contains ("CSharpSyntaxMode")));
			using (XmlReader reader = provider.Open ()) {
				SyntaxMode baseMode = SyntaxMode.Read (reader);
				rules = new List<Rule> (baseMode.Rules);
				rules.Add (new Rule (baseMode) {
					Name = "PreProcessorComment"
				});
				keywords = new List<Keywords> (baseMode.Keywords);
				spans = new List<Span> (baseMode.Spans.Where (span => span.Begin.Pattern != "#")).ToArray ();
				matches = baseMode.Matches;
				prevMarker = baseMode.PrevMarker;
				SemanticRules = new List<SemanticRule> (baseMode.SemanticRules);
				keywordTable = baseMode.keywordTable;
				keywordTableIgnoreCase = baseMode.keywordTableIgnoreCase;
				properties = baseMode.Properties;
			}
			
			foreach (var word in ContextualHighlightKeywordList) {
				contextualHighlightKeywords[word] = keywordTable[word].Color;
				keywordTable.Remove (word);
			}
			
			AddSemanticRule ("Comment", new HighlightUrlSemanticRule ("comment"));
			AddSemanticRule ("XmlDocumentation", new HighlightUrlSemanticRule ("comment"));
			AddSemanticRule ("String", new HighlightUrlSemanticRule ("string"));
		}
		
		void EnsureGuiDocument ()
		{
			if (guiDocument != null)
				return;
			try {
				if (File.Exists (Document.FileName))
					guiDocument = IdeApp.Workbench.GetDocument (Document.FileName);
			} catch (Exception) {
				guiDocument = null;
			}
			if (guiDocument != null) {

				guiDocument.Closed += delegate {
					if (src != null)
						src.Cancel ();
				};
				guiDocument.DocumentParsed += HandleDocumentParsed;
				highlightedSegmentCache = new HighlightingSegmentTree ();
				highlightedSegmentCache.InstallListener (guiDocument.Editor.Document);
				if (guiDocument.ParsedDocument != null)
					HandleDocumentParsed (this, EventArgs.Empty);
			}
		}
		
		public override SpanParser CreateSpanParser (DocumentLine line, CloneableStack<Span> spanStack)
		{
			EnsureGuiDocument ();
			return new CSharpSpanParser (this, spanStack ?? line.StartSpan.Clone ());
		}
		
		public override ChunkParser CreateChunkParser (SpanParser spanParser, ColorScheme style, DocumentLine line)
		{
			EnsureGuiDocument ();
			return new CSharpChunkParser (this, spanParser, style, line);
		}
		
		abstract class AbstractBlockSpan : Span
		{
			public bool IsValid {
				get;
				private set;
			}
			
			bool disabled;
			
			public bool Disabled {
				get { return disabled; }
				set { disabled = value; SetColor (); }
			}
			
			
			public AbstractBlockSpan (bool isValid)
			{
				IsValid = isValid;
				SetColor ();
				StopAtEol = false;
			}
			
			protected void SetColor ()
			{
				TagColor = "text.preprocessor";
				if (disabled || !IsValid) {
					Color = "comment.block";
					Rule = "PreProcessorComment";
				} else {
					Color = "text";
					Rule = "<root>";
				}
			}
		}

		class DefineSpan : Span
		{
			string define;

			public string Define { 
				get { 
					return define;
				}
			}

			public DefineSpan (string define)
			{
				this.define = define;
				StopAtEol = false;
				Color = "text";
				Rule = "<root>";
			}
		}

		class IfBlockSpan : AbstractBlockSpan
		{
			public IfBlockSpan (bool isValid) : base (isValid)
			{
			}
			
			public override string ToString ()
			{
				return string.Format("[IfBlockSpan: IsValid={0}, Disabled={3}, Color={1}, Rule={2}]", IsValid, Color, Rule, Disabled);
			}
		}
		
		class ElseIfBlockSpan : AbstractBlockSpan
		{
			public ElseIfBlockSpan (bool isValid) : base (isValid)
			{
				base.Begin = new Regex ("#elif");
			}
			
			public override string ToString ()
			{
				return string.Format("[ElseIfBlockSpan: IsValid={0}, Disabled={3}, Color={1}, Rule={2}]", IsValid, Color, Rule, Disabled);
			}
		}
		
		class ElseBlockSpan : AbstractBlockSpan
		{
			public ElseBlockSpan (bool isValid) : base (isValid)
			{
				base.Begin = new Regex ("#else");
			}
			
			public override string ToString ()
			{
				return string.Format("[ElseBlockSpan: IsValid={0}, Disabled={3}, Color={1}, Rule={2}]", IsValid, Color, Rule, Disabled);
			}
		}
		
		protected class CSharpChunkParser : ChunkParser, IResolveVisitorNavigator
		{
			HashSet<string> tags = new HashSet<string> ();
			/*
			sealed class SemanticResolveVisitorNavigator : IResolveVisitorNavigator
			{
				readonly Dictionary<AstNode, ResolveVisitorNavigationMode> dict = new Dictionary<AstNode, ResolveVisitorNavigationMode> ();
				
				public void AddNode (AstNode node)
				{
					dict [node] = ResolveVisitorNavigationMode.Resolve;
					for (var ancestor = node.Parent; ancestor != null && !dict.ContainsKey(ancestor); ancestor = ancestor.Parent) {
						dict.Add (ancestor, ResolveVisitorNavigationMode.Scan);
					}
				}
				
				public void ProcessConversion (Expression expression, ResolveResult result, Conversion conversion, IType targetType)
				{
					
				}
				
				public void Resolved (AstNode node, ResolveResult result)
				{

				}
				
				public ResolveVisitorNavigationMode Scan (AstNode node)
				{
					if (node is Expression || node is AstType)
						return ResolveVisitorNavigationMode.Resolve;
					return ResolveVisitorNavigationMode.Scan;
				}
				
				public void Reset ()
				{
					dict.Clear ();
				}
			}*/
			CSharpSyntaxMode csharpSyntaxMode;
			
			public CSharpChunkParser (CSharpSyntaxMode csharpSyntaxMode, SpanParser spanParser, ColorScheme style, DocumentLine line) : base (csharpSyntaxMode, spanParser, style, line)
			{
				this.csharpSyntaxMode = csharpSyntaxMode;
				foreach (var tag in CommentTag.SpecialCommentTags) {
					tags.Add (tag.Tag);
				}

			}

			#region IResolveVisitorNavigator implementation
			ResolveVisitorNavigationMode IResolveVisitorNavigator.Scan(AstNode node)
			{
/*				if (node.StartLocation.Line <= lineNumber && node.EndLocation.Line >= lineNumber) {*/
					if (node is SimpleType || node is MemberType
					    || node is IdentifierExpression || node is MemberReferenceExpression
					    || node is InvocationExpression)
					{
						return ResolveVisitorNavigationMode.Resolve;
					} else {
						return ResolveVisitorNavigationMode.Scan;
					}
/*				} else {
					return ResolveVisitorNavigationMode.Skip;
				}*/
			}
			
			void IResolveVisitorNavigator.Resolved(AstNode node, ResolveResult result)
			{
			}
			
			void IResolveVisitorNavigator.ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
			}
			#endregion
			string GetSemanticStyle (ParsedDocument parsedDocument, Chunk chunk, ref int endOffset)
			{
				string style;
				bool found = csharpSyntaxMode.highlightedSegmentCache.GetStyle (chunk, ref endOffset, out style);
				if (!found && !csharpSyntaxMode.highlightedSegmentCache.IsDirty) {
					style = GetSemanticStyleFromAst (parsedDocument, chunk, ref endOffset);
					csharpSyntaxMode.highlightedSegmentCache.AddStyle (chunk.Offset, style == null ? chunk.EndOffset : endOffset, style);
				}
				return style;
			}

			static int TokenLength (AstNode node)
			{
				Debug.Assert (node.StartLocation.Line == node.EndLocation.Line);
				return node.EndLocation.Column - node.StartLocation.Column;
			}

			string GetSemanticStyleFromAst (ParsedDocument parsedDocument, Chunk chunk, ref int endOffset)
			{
				var unit = csharpSyntaxMode.unit;
				if (unit == null || csharpSyntaxMode.resolver == null)
					return null;
				
				var loc = doc.OffsetToLocation (chunk.Offset);
				var node = unit.GetNodeAt (loc, n => n is Identifier || n is AstType || n is CSharpTokenNode);
				var word = wordbuilder.ToString ();
				string color;
				while (node != null && !(node is Statement || node is EntityDeclaration)) {
					if (node is CSharpTokenNode || node is ICSharpCode.NRefactory.CSharp.Comment || node is PreProcessorDirective)
						break;
					if (node is SimpleType) {
						var st = (SimpleType)node;
						
						var result = csharpSyntaxMode.resolver.Resolve (st);
						if (result.IsError && csharpSyntaxMode.guiDocument.Project != null) {
							endOffset = chunk.Offset + TokenLength (st.IdentifierToken);
							return "keyword.semantic.error";
						}
						if (result is TypeResolveResult && st.IdentifierToken.Contains (loc) && unit.GetNodeAt<UsingDeclaration> (loc) == null) {
							endOffset = chunk.Offset + TokenLength (st.IdentifierToken);
							return "keyword.semantic.type";
						}
						return null;
					}
					if (node is ICSharpCode.NRefactory.CSharp.MemberType) {
						var mt = (ICSharpCode.NRefactory.CSharp.MemberType)node;
						
						var result = csharpSyntaxMode.resolver.Resolve (mt);
						if (result.IsError && csharpSyntaxMode.guiDocument.Project != null) {
							endOffset = chunk.Offset + TokenLength (mt.MemberNameToken);
							return "keyword.semantic.error";
						}
						if (result is TypeResolveResult && mt.MemberNameToken.Contains (loc) && unit.GetNodeAt<UsingDeclaration> (loc) == null) {
							endOffset = chunk.Offset + TokenLength (mt.MemberNameToken);
							return "keyword.semantic.type";
						}
						return null;
					}
					
					if (node is Identifier) {
						if (node.Parent is TypeDeclaration && node.Role == Roles.Identifier) {
							endOffset = chunk.Offset + TokenLength ((Identifier)node);
							return "keyword.semantic.type";
						}

						if (node.Parent is PropertyDeclaration) {
							endOffset = chunk.Offset + TokenLength ((Identifier)node);
							return "keyword.semantic.property";
						}

						if (node.Parent is VariableInitializer && node.Parent.Parent is FieldDeclaration) {
							var field = node.Parent.Parent as FieldDeclaration;
							if (field.Modifiers.HasFlag (Modifiers.Const) || field.Modifiers.HasFlag (Modifiers.Static | Modifiers.Readonly))
								return null;
							endOffset = chunk.Offset + TokenLength ((Identifier)node);
							return "keyword.semantic.field";
						}
						if (node.Parent is FixedVariableInitializer /*|| node.Parent is EnumMemberDeclaration*/) {
							endOffset = chunk.Offset + TokenLength ((Identifier)node);
							return "keyword.semantic.field";
						}
					}
					
					var id = node as IdentifierExpression;
					if (id != null) {
						var result = csharpSyntaxMode.resolver.Resolve (id);
						if (result.IsError && csharpSyntaxMode.guiDocument.Project != null) {
							endOffset = chunk.Offset + TokenLength (id.IdentifierToken);
							return "keyword.semantic.error";
						}
						
						if (result is MemberResolveResult) {
							var member = ((MemberResolveResult)result).Member;
							if (member is IField) {
								var field = member as IField;
								if (field.IsConst || field.IsStatic && field.IsReadOnly)
									return null;
								endOffset = chunk.Offset + TokenLength (id.IdentifierToken);
								return "keyword.semantic.field";
							}
							if (member is IProperty) {
								endOffset = chunk.Offset + TokenLength (id.IdentifierToken);
								return "keyword.semantic.property";
							}
						}
						if (result is TypeResolveResult) {
							if (!result.IsError && csharpSyntaxMode.guiDocument.Project != null) {
								endOffset = chunk.Offset + id.Identifier.Length;
								return "keyword.semantic.type";
							}
						}
					}
					
					var memberReferenceExpression = node as MemberReferenceExpression;
					if (memberReferenceExpression != null) {
						if (!memberReferenceExpression.MemberNameToken.Contains (loc)) 
							return null;
						
						var result = csharpSyntaxMode.resolver.Resolve (memberReferenceExpression);
						if (result.IsError && csharpSyntaxMode.guiDocument.Project != null) {
							endOffset = chunk.Offset + TokenLength (memberReferenceExpression.MemberNameToken);
							return "keyword.semantic.error";
						}
						
						if (result is MemberResolveResult) {
							var member = ((MemberResolveResult)result).Member;
							if (member is IField && !member.IsStatic && !((IField)member).IsConst) {
								endOffset = chunk.Offset + TokenLength (memberReferenceExpression.MemberNameToken);
								return "keyword.semantic.field";
							}
						}
						if (result is TypeResolveResult) {
							if (!result.IsError && csharpSyntaxMode.guiDocument.Project != null) {
								endOffset = chunk.Offset + TokenLength (memberReferenceExpression.MemberNameToken);
								return "keyword.semantic.type";
							}
						}
					}
					var pointerReferenceExpression = node as PointerReferenceExpression;
					if (pointerReferenceExpression != null) {
						if (!pointerReferenceExpression.MemberNameToken.Contains (loc)) 
							return null;
						
						var result = csharpSyntaxMode.resolver.Resolve (pointerReferenceExpression);
						if (result.IsError && csharpSyntaxMode.guiDocument.Project != null) {
							endOffset = chunk.Offset + TokenLength (pointerReferenceExpression.MemberNameToken);
							return "keyword.semantic.error";
						}
						
						if (result is MemberResolveResult) {
							var member = ((MemberResolveResult)result).Member;
							if (member is IField && !member.IsStatic && !((IField)member).IsConst) {
								endOffset = chunk.Offset + TokenLength (pointerReferenceExpression.MemberNameToken);
								return "keyword.semantic.field";
							}
						}
						if (result is TypeResolveResult) {
							if (!result.IsError && csharpSyntaxMode.guiDocument.Project != null) {
								endOffset = chunk.Offset + TokenLength (pointerReferenceExpression.MemberNameToken);
								return "keyword.semantic.type";
							}
						}
					}
					node = node.Parent;
				}

				if (csharpSyntaxMode.contextualHighlightKeywords.TryGetValue (word, out color)) {
					if (node == null)
						return null;
					switch (word) {
					case "value":
						// highlight 'value' in property setters and event add/remove
						var n = node.Parent;
						while (n != null) {
							if (n is Accessor && n.Role == PropertyDeclaration.SetterRole) {
								endOffset = chunk.Offset + "value".Length;
								return color;
							}
							n = n.Parent;
						}
						return null;
					}
					endOffset = chunk.Offset + word.Length;
					if (node is CSharpTokenNode)
						return color;
					return spanParser.CurSpan != null ? spanParser.CurSpan.Color : "text";
				}

				if (ContextualDehighlightKeywordList.Contains (word)) {
					if (node == null)
						return null;
					if (node is Identifier) {
						switch (((Identifier)node).Name) {
						case "var": 
							if (node.Parent != null) {
								var vds = node.Parent.Parent as VariableDeclarationStatement;
								if (node.Parent.Parent is ForeachStatement && ((ForeachStatement)node.Parent.Parent).VariableType.StartLocation == node.StartLocation ||
									vds != null && node.StartLocation == vds.Type.StartLocation)
									return null;
							}
							endOffset = chunk.Offset + "var".Length;
							return spanParser.CurSpan != null ? spanParser.CurSpan.Color : "text";
						}
					} else if (node is CSharpTokenNode)
						return color;
					endOffset = chunk.Offset + word.Length;
					return spanParser.CurSpan != null ? spanParser.CurSpan.Color : "text";
				}

				return null;
			}
			
			protected override void AddRealChunk (Chunk chunk)
			{
				var document = csharpSyntaxMode.guiDocument;
				var parsedDocument = document != null ? document.ParsedDocument : null;
				if (parsedDocument != null && MonoDevelop.Core.PropertyService.Get ("EnableSemanticHighlighting", true)) {
					int endLoc = -1;
					string semanticStyle = null;
					if (spanParser.CurSpan == null || spanParser.CurSpan is DefineSpan) {
						try {
							semanticStyle = GetSemanticStyle (parsedDocument, chunk, ref endLoc);
						} catch (Exception e) {
							Console.WriteLine ("Error in semantic highlighting: " + e);
						}
					}
					if (semanticStyle != null) {
						if (endLoc < chunk.EndOffset) {
							base.AddRealChunk (new Chunk (chunk.Offset, endLoc - chunk.Offset, semanticStyle));
							base.AddRealChunk (new Chunk (endLoc, chunk.EndOffset - endLoc, chunk.Style));
							return;
						}
						chunk.Style = semanticStyle;
					}
				}
				
				base.AddRealChunk (chunk);
			}
			
			protected override string GetStyle (Chunk chunk)
			{
				if (spanParser.CurRule.Name == "Comment") {
					if (tags.Contains (doc.GetTextAt (chunk))) 
						return "comment.keyword.todo";
				}
				return base.GetStyle (chunk);
			}
		}
		
		protected class CSharpSpanParser : SpanParser
		{
			CSharpSyntaxMode CSharpSyntaxMode {
				get {
					return (CSharpSyntaxMode)mode;
				}
			}
			class ConditinalExpressionEvaluator : DepthFirstAstVisitor<object, object>
			{
				HashSet<string> symbols;

				MonoDevelop.Projects.Project GetProject (Mono.TextEditor.TextDocument doc)
				{
					// There is no reference between document & higher level infrastructure,
					// therefore it's a bit tricky to find the right project.
					
					MonoDevelop.Projects.Project project = null;
					var view = doc.Annotation<MonoDevelop.SourceEditor.SourceEditorView> ();
					if (view != null)
						project = view.Project;
					
					if (project == null) {
						var ideDocument = IdeApp.Workbench.GetDocument (doc.FileName);
						if (ideDocument != null)
							project = ideDocument.Project;
					}
					
					if (project == null)
						project = IdeApp.Workspace.GetProjectContainingFile (doc.FileName);
					
					return project;
				}

				public ConditinalExpressionEvaluator (Mono.TextEditor.TextDocument doc, IEnumerable<string> symbols)
				{
					this.symbols = new HashSet<string> (symbols);
					var project = GetProject (doc);
					
					if (project == null) {
						var ideDocument = IdeApp.Workbench.GetDocument (doc.FileName);
						if (ideDocument != null)
							project = ideDocument.Project;
					}
					
					if (project == null)
						project = IdeApp.Workspace.GetProjectContainingFile (doc.FileName);
					
					if (project != null) {
						var configuration = project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
						if (configuration != null) {
							var cparams = configuration.CompilationParameters as CSharpCompilerParameters;
							if (cparams != null) {
								string[] syms = cparams.DefineSymbols.Split (';', ',', ' ', '\t');
								foreach (string s in syms) {
									string ss = s.Trim ();
									if (ss.Length > 0 && !symbols.Contains (ss))
										this.symbols.Add (ss);
								}
							}
							// Workaround for mcs defined symbol
							if (configuration.TargetRuntime.RuntimeId == "Mono") 
								this.symbols.Add ("__MonoCS__");
						} else {
							Console.WriteLine ("NO CONFIGURATION");
						}
					}
/*					var parsedDocument = TypeSystemService.ParseFile (document.ProjectContent, doc.FileName, doc.MimeType, doc.Text);
					if (parsedDocument == null)
						parsedDocument = TypeSystemService.ParseFile (dom, doc.FileName ?? "a.cs", delegate { return doc.Text; });
					if (parsedDocument != null) {
						foreach (PreProcessorDefine define in parsedDocument.Defines) {
							symbols.Add (define.Define);
						}
						
					}*/
				}
				
				public override object VisitIdentifierExpression (IdentifierExpression identifierExpression, object data)
				{
					return symbols.Contains (identifierExpression.Identifier);
				}
				
				public override object VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression, object data)
				{
					bool result = (bool)(unaryOperatorExpression.Expression.AcceptVisitor (this, data) ?? (object)false);
					if (unaryOperatorExpression.Operator ==  UnaryOperatorType.Not)
						return !result;
					return result;
				}
				
				public override object VisitPrimitiveExpression (PrimitiveExpression primitiveExpression, object data)
				{
					if (primitiveExpression.Value is bool)
						return (bool)primitiveExpression.Value;
					return false;
				}

				public override object VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, object data)
				{
					bool left = (bool)(binaryOperatorExpression.Left.AcceptVisitor (this, data) ?? (object)false);
					bool right = (bool)(binaryOperatorExpression.Right.AcceptVisitor (this, data) ?? (object)false);
					switch (binaryOperatorExpression.Operator) {
					case BinaryOperatorType.InEquality:
						return left != right;
					case BinaryOperatorType.Equality:
						return left == right;
					case BinaryOperatorType.ConditionalOr:
						return left || right;
					case BinaryOperatorType.ConditionalAnd:
						return left && right;
					}
					
					Console.WriteLine ("Unknown operator:" + binaryOperatorExpression.Operator);
					return left;
				}

				public override object VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression, object data)
				{
					return parenthesizedExpression.Expression.AcceptVisitor (this, data);
				}
			}
			
			void ScanPreProcessorElse (ref int i)
			{
				if (!spanStack.Any (s => s is IfBlockSpan || s is ElseIfBlockSpan)) {
					base.ScanSpan (ref i);
					return;
				}
				bool previousResult = false;
				foreach (Span span in spanStack) {
					if (span is IfBlockSpan) {
						previousResult = ((IfBlockSpan)span).IsValid;
					}
					if (span is ElseIfBlockSpan) {
						previousResult |= ((ElseIfBlockSpan)span).IsValid;
					}
				}
				//					LineSegment line = doc.GetLineByOffset (i);
				//					int length = line.Offset + line.EditableLength - i;
				while (spanStack.Count > 0 && !(CurSpan is IfBlockSpan || CurSpan is ElseIfBlockSpan)) {
					spanStack.Pop ();
				}
				var ifBlock = CurSpan as IfBlockSpan;
				var elseIfBlock = CurSpan as ElseIfBlockSpan;
				var elseBlockSpan = new ElseBlockSpan (!previousResult);
				if (ifBlock != null) {
					elseBlockSpan.Disabled = ifBlock.Disabled;
				} else if (elseIfBlock != null) {
					elseBlockSpan.Disabled = elseIfBlock.Disabled;
				}
				FoundSpanBegin (elseBlockSpan, i, "#else".Length);
				i += "#else".Length;
					
				// put pre processor eol span on stack, so that '#elif' gets the correct highlight
				Span preprocessorSpan = CreatePreprocessorSpan ();
				FoundSpanBegin (preprocessorSpan, i, 0);
			}
			IEnumerable<string> Defines {
				get {
					if (SpanStack == null)
						yield break;
					foreach (var span in SpanStack) {
						if (span is DefineSpan) {
							var define = ((DefineSpan)span).Define;
							if (define != null)
								yield return define;
						}
					}
				}
			}
			void ScanPreProcessorIf (int textOffset, ref int i)
			{
				int length = CurText.Length - textOffset;
				string parameter = CurText.Substring (textOffset + 3, length - 3);
				AstNode expr = new CSharpParser ().ParseExpression (parameter);
				bool result = false;
				if (expr != null && !expr.IsNull) {
					object o = expr.AcceptVisitor (new ConditinalExpressionEvaluator (doc, Defines), null);
					if (o is bool)
						result = (bool)o;
				}
					
				foreach (Span span in spanStack) {
					if (span is IfBlockSpan) {
						result &= ((IfBlockSpan)span).IsValid;
					}
					if (span is ElseIfBlockSpan) {
						result &= ((ElseIfBlockSpan)span).IsValid;
					}
				}
					
				var ifBlockSpan = new IfBlockSpan (result);
					
				foreach (Span span in spanStack) {
					if (span is AbstractBlockSpan) {
						var parentBlock = (AbstractBlockSpan)span;
						ifBlockSpan.Disabled = parentBlock.Disabled || !parentBlock.IsValid;
						break;
					}
				}
					
				FoundSpanBegin (ifBlockSpan, i, length);
				i += length - 1;
			}

			void ScanPreProcessorElseIf (ref int i)
			{
				DocumentLine line = doc.GetLineByOffset (i);
				int length = line.Offset + line.Length - i;
				string parameter = doc.GetTextAt (i + 5, length - 5);
				AstNode expr= new CSharpParser ().ParseExpression (parameter);
				bool result;
				if (expr != null && !expr.IsNull) {
					var visitResult = expr.AcceptVisitor (new ConditinalExpressionEvaluator (doc, Defines), null);
					result = visitResult != null ? (bool)visitResult : false;
				} else {
					result = false;
				}
					
				IfBlockSpan containingIf = null;
				if (result) {
					bool previousResult = false;
					foreach (Span span in spanStack) {
						if (span is IfBlockSpan) {
							containingIf = (IfBlockSpan)span;
							previousResult = ((IfBlockSpan)span).IsValid;
							break;
						}
						if (span is ElseIfBlockSpan) {
							previousResult |= ((ElseIfBlockSpan)span).IsValid;
						}
					}
						
					result = !previousResult;
				}
					
				var elseIfBlockSpan = new ElseIfBlockSpan (result);
				if (containingIf != null)
					elseIfBlockSpan.Disabled = containingIf.Disabled;
					
				FoundSpanBegin (elseIfBlockSpan, i, 0);
					
				// put pre processor eol span on stack, so that '#elif' gets the correct highlight
				var preprocessorSpan = CreatePreprocessorSpan ();
				FoundSpanBegin (preprocessorSpan, i, 0);
			}

			protected override void ScanSpan (ref int i)
			{
				if (CSharpSyntaxMode.DisableConditionalHighlighting) {
					base.ScanSpan (ref i);
					return;
				}
				int textOffset = i - StartOffset;

				if (textOffset < CurText.Length && CurRule.Name != "Comment" && CurRule.Name != "String" && CurText [textOffset] == '#' && IsFirstNonWsChar (textOffset)) {

					if (CurText.IsAt (textOffset, "#define")) {
						int length = CurText.Length - textOffset;
						string parameter = CurText.Substring (textOffset + "#define".Length, length - "#define".Length).Trim ();

						var defineSpan = new DefineSpan (parameter);
						FoundSpanBegin (defineSpan, i, 0);
					}
	
					if (CurText.IsAt (textOffset, "#else")) {
						ScanPreProcessorElse (ref i);
						return;
					}
	
					if (CurText.IsAt (textOffset, "#if")) {
						ScanPreProcessorIf (textOffset, ref i);
						return;
					}
	
					if (CurText.IsAt (textOffset, "#elif") && spanStack != null && spanStack.Any (span => span is IfBlockSpan)) {
						ScanPreProcessorElseIf (ref i);
						return;
					}
	
					var preprocessorSpan = CreatePreprocessorSpan ();
					FoundSpanBegin (preprocessorSpan, i, 1);
					return;
				}

				base.ScanSpan (ref i);
			}
			
			public static Span CreatePreprocessorSpan ()
			{
				var result = new Span ();
				result.TagColor = "text.preprocessor";
				result.Color = "text.preprocessor";
				result.Rule = "String";
				result.StopAtEol = true;
				return result;
			}
			
			void PopCurrentIfBlock ()
			{
				while (spanStack.Count > 0 && (spanStack.Peek () is IfBlockSpan || spanStack.Peek () is ElseIfBlockSpan || spanStack.Peek () is ElseBlockSpan)) {
					var poppedSpan = PopSpan ();
					if (poppedSpan is IfBlockSpan)
						break;
				}
			}
			
			protected override bool ScanSpanEnd (Mono.TextEditor.Highlighting.Span cur, ref int i)
			{
				if (cur is IfBlockSpan || cur is ElseIfBlockSpan || cur is ElseBlockSpan) {
					int textOffset = i - StartOffset;
					bool end = CurText.IsAt (textOffset, "#endif");
					if (end) {
						FoundSpanEnd (cur, i, 6); // put empty end tag in
						
						// if we're in a complex span stack pop it up to the if block
						if (spanStack.Count > 0) {
							var prev = spanStack.Peek ();
							
							if ((cur is ElseIfBlockSpan || cur is ElseBlockSpan) && (prev is ElseIfBlockSpan || prev is IfBlockSpan))
								PopCurrentIfBlock ();
						}
					}
					return end;
				}
				return base.ScanSpanEnd (cur, ref i);
			}
			
	//		Span preprocessorSpan;
	//		Rule preprocessorRule;
			
			public CSharpSpanParser (CSharpSyntaxMode mode, CloneableStack<Span> spanStack) : base (mode, spanStack)
			{
//				foreach (Span span in mode.Spans) {
//					if (span.Rule == "text.preprocessor") {
//						preprocessorSpan = span;
//						preprocessorRule = GetRule (span);
//					}
//				}
			}
		}

		#region IQuickTaskProvider implementation
		public event EventHandler TasksUpdated;

		protected virtual void OnTasksUpdated (System.EventArgs e)
		{
			var handler = TasksUpdated;
			if (handler != null)
				handler (this, e);
		}

		List<QuickTask> quickTasks;
		public IEnumerable<QuickTask> QuickTasks {
			get {
				return quickTasks;
			}
		}
		#endregion
	}
}
 