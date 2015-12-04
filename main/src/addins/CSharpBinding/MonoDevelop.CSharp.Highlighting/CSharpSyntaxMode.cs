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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.SourceEditor.QuickTasks;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.Refactoring;


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
	
	class CSharpSyntaxMode : SyntaxMode, IQuickTaskProvider, IDisposable
	{
		readonly Document guiDocument;

		CSharpAstResolver resolver;
		CancellationTokenSource src;

		public bool SemanticHighlightingEnabled {
			get {
				return true;
			}
		}

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
				var segment = GetSegmentsAt (chunk.Offset).FirstOrDefault ();
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
		
		Dictionary<DocumentLine, HighlightingVisitior> lineSegments = new Dictionary<DocumentLine, HighlightingVisitior> ();

		public bool DisableConditionalHighlighting {
			get;
			set;
		}

		public static IEnumerable<string> GetDefinedSymbols (MonoDevelop.Projects.Project project)
		{
			var workspace = IdeApp.Workspace;
			if (workspace == null || project == null)
				yield break;
			var configuration = project.GetConfiguration (workspace.ActiveConfiguration) as DotNetProjectConfiguration;
			if (configuration != null) {
				foreach (string s in configuration.GetDefineSymbols ())
					yield return s;
				// Workaround for mcs defined symbol
				if (configuration.TargetRuntime.RuntimeId == "Mono") 
					yield return "__MonoCS__";
			}
		}

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			if (src != null)
				src.Cancel ();
			resolver = null;
			if (guiDocument.IsProjectContextInUpdate) {
				return;
			}
			if (guiDocument != null && SemanticHighlightingEnabled) {
				var parsedDocument = guiDocument.ParsedDocument;
				if (parsedDocument != null) {
					if (guiDocument.Project != null && guiDocument.IsCompileableInProject) {
						src = new CancellationTokenSource ();
						var newResolverTask = guiDocument.GetSharedResolver ();
						var cancellationToken = src.Token;
						System.Threading.Tasks.Task.Factory.StartNew (delegate {
							if (newResolverTask == null)
								return;
							var newResolver = newResolverTask.Result;
							if (newResolver == null)
								return;
							var visitor = new QuickTaskVisitor (newResolver, cancellationToken);
							try {
								newResolver.RootNode.AcceptVisitor (visitor);
							} catch (Exception ex) {
								LoggingService.LogError ("Error while analyzing the file for the semantic highlighting.", ex);
								return;
							}
							if (!cancellationToken.IsCancellationRequested) {
								Gtk.Application.Invoke (delegate {
									if (cancellationToken.IsCancellationRequested)
										return;
									var editorData = guiDocument.Editor;
									if (editorData == null)
										return;
//									compilation = newResolver.Compilation;
									resolver = newResolver;
									quickTasks = visitor.QuickTasks;
									OnTasksUpdated (EventArgs.Empty);
									foreach (var kv in lineSegments) {
										try {
											kv.Value.tree.RemoveListener ();
										} catch (Exception) {
										}
									}
									lineSegments.Clear ();
									var textEditor = editorData.Parent;
									if (textEditor != null) {
										if (!parsedDocument.HasErrors) {
											var margin = textEditor.TextViewMargin;
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

		class HighlightingVisitior : SemanticHighlightingVisitor<string>
		{
			readonly int lineNumber;
			readonly int lineOffset;
			readonly int lineLength;
			internal HighlightingSegmentTree tree = new HighlightingSegmentTree ();

			public HighlightingVisitior (CSharpAstResolver resolver, CancellationToken cancellationToken, int lineNumber, int lineOffset, int lineLength)
			{
				if (resolver == null)
					throw new ArgumentNullException ("resolver");
				this.resolver = resolver;
				this.cancellationToken = cancellationToken;
				this.lineNumber = lineNumber;
				this.lineOffset = lineOffset;
				this.lineLength = lineLength;
				regionStart = new TextLocation (lineNumber, 1);
				regionEnd  = new TextLocation (lineNumber, lineLength);

				Setup ();
			}

			void Setup ()
			{
				defaultTextColor = "Plain Text";
				referenceTypeColor = "User Types";
				valueTypeColor = "User Types(Value types)";
				interfaceTypeColor = "User Types(Interfaces)";
				enumerationTypeColor = "User Types(Enums)";
				typeParameterTypeColor = "User Types(Type parameters)";
				delegateTypeColor = "User Types(Delegates)";

				methodCallColor = "User Method Usage";
				methodDeclarationColor = "User Method Declaration";

				eventDeclarationColor = "User Event Declaration";
				eventAccessColor = "User Event Usage";

				fieldDeclarationColor ="User Field Declaration";
				fieldAccessColor = "User Field Usage";

				propertyDeclarationColor = "User Property Declaration";
				propertyAccessColor = "User Property Usage";

				variableDeclarationColor = "User Variable Declaration";
				variableAccessColor = "User Variable Usage";

				parameterDeclarationColor = "User Parameter Declaration";
				parameterAccessColor = "User Parameter Usage";

				valueKeywordColor = "Keyword(Context)";
				externAliasKeywordColor = "Keyword(Namespace)";
				varKeywordTypeColor = "Keyword(Type)";

				parameterModifierColor = "Keyword(Parameter)";
				inactiveCodeColor = "Excluded Code";
				syntaxErrorColor = "Syntax Error";

				stringFormatItemColor = "String Format Items";
			}

			protected override void Colorize(TextLocation start, TextLocation end, string color)
			{
				int startOffset;
				if (start.Line == lineNumber) {
					startOffset = lineOffset + start.Column - 1;
				} else {
					if (start.Line > lineNumber)
						return;
					startOffset = lineOffset;
				}
				int endOffset;
				if (end.Line == lineNumber) {
					endOffset = lineOffset +end.Column - 1;
				} else {
					if (end.Line < lineNumber)
						return;
					endOffset = lineOffset + lineLength;
				}
				tree.AddStyle (startOffset, endOffset, color);
			}

			public override void VisitSimpleType (SimpleType simpleType)
			{
				var identifierToken = simpleType.IdentifierToken;
				VisitChildrenUntil(simpleType, identifierToken);
				var resolveResult = resolver.Resolve (simpleType, cancellationToken);
				if (resolveResult.Type.Namespace == "System") {
					switch (resolveResult.Type.Name) {
					case "nfloat":
					case "nint":
					case "nuint":
						Colorize(identifierToken, "Keyword(Type)");
						break;
					default:
						Colorize (identifierToken, resolveResult);
						break;
					}
				} else {
					Colorize (identifierToken, resolveResult);
				}
				VisitChildrenAfter(simpleType, identifierToken);
			}

			public override void VisitIdentifierExpression (IdentifierExpression identifierExpression)
			{
				var identifier = identifierExpression.IdentifierToken;
				VisitChildrenUntil(identifierExpression, identifier);
				if (isInAccessorContainingValueParameter && identifierExpression.Identifier == "value") {
					Colorize(identifier, valueKeywordColor);
				} else {
					var resolveResult = resolver.Resolve (identifierExpression, cancellationToken);
					Colorize (identifier, resolveResult);
				}
				VisitChildrenAfter(identifierExpression, identifier);
			}
		}

		class QuickTaskVisitor : DepthFirstAstVisitor
		{
			internal List<QuickTask> QuickTasks = new List<QuickTask> ();
			readonly CSharpAstResolver resolver;
			readonly CancellationToken cancellationToken;

			public QuickTaskVisitor (CSharpAstResolver resolver, CancellationToken cancellationToken)
			{
				this.resolver = resolver;
				this.cancellationToken = cancellationToken;
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
					QuickTasks.Add (new QuickTask (() => string.Format ("error CS0103: The name `{0}' does not exist in the current context", identifierExpression.Identifier), identifierExpression.StartLocation, Severity.Error));
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

			public override void VisitSimpleType (SimpleType simpleType)
			{
				base.VisitSimpleType (simpleType);
				var result = resolver.Resolve (simpleType, cancellationToken);
				if (result.IsError) {
					QuickTasks.Add (new QuickTask (string.Format ("error CS0246: The type or namespace name `{0}' could not be found. Are you missing an assembly reference?", simpleType.Identifier), simpleType.StartLocation, Severity.Error));
				}
			}

			public override void VisitMemberType (MemberType memberType)
			{
				base.VisitMemberType (memberType);
				var result = resolver.Resolve (memberType, cancellationToken);
				if (result.IsError) {
					QuickTasks.Add (new QuickTask (string.Format ("error CS0246: The type or namespace name `{0}' could not be found. Are you missing an assembly reference?", memberType.MemberName), memberType.StartLocation, Severity.Error));
				}
			}

			public override void VisitComment (ICSharpCode.NRefactory.CSharp.Comment comment)
			{
			}
		}
		
		static CSharpSyntaxMode ()
		{
			MonoDevelop.Debugger.DebuggingService.DisableConditionalCompilation += DispatchService.GuiDispatch (new EventHandler<DocumentEventArgs> (OnDisableConditionalCompilation));
			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.ActiveConfigurationChanged += delegate {
					foreach (var doc in IdeApp.Workbench.Documents) {
						TextEditorData data = doc.Editor;
						if (data == null)
							continue;
						// Force syntax mode reparse (required for #if directives)
						var editor = doc.Editor;
						if (editor != null) {
							if (data.Document.SyntaxMode is SyntaxMode) {
								((SyntaxMode)data.Document.SyntaxMode).UpdateDocumentHighlighting ();
								SyntaxModeService.WaitUpdate (data.Document);
							}
							editor.Parent.TextViewMargin.PurgeLayoutCache ();
							doc.ReparseDocument ();
							editor.Parent.QueueDraw ();
						}
					}
				};
			}
			CommentTag.SpecialCommentTagsChanged += (sender, e) => {
				UpdateCommentRule ();
				var actDoc = IdeApp.Workbench.ActiveDocument;
				if (actDoc != null && actDoc.Editor != null) {
					actDoc.UpdateParseDocument ();
					actDoc.Editor.Parent.TextViewMargin.PurgeLayoutCache ();
					actDoc.Editor.Parent.QueueDraw ();
				}
			};
		}
		
		static void OnDisableConditionalCompilation (object s, DocumentEventArgs e)
		{
			var mode = e.Document.Editor.Document.SyntaxMode as CSharpSyntaxMode;
			if (mode == null)
				return;
			mode.DisableConditionalHighlighting = true;
			e.Document.Editor.Document.CommitUpdateAll ();
		}
		
		static Dictionary<string, string> contextualHighlightKeywords;
		static readonly string[] ContextualKeywords = new string[] {
			"value"
/*			"async",
			"await",
			, //*
			"get", "set", "add", "remove",  //*
			"var", //*
			"global",
			"partial", //* 
			"where",  //*
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
			"equals"*/
		};

		#region Syntax mode rule cache
		static List<Rule> _rules;
		static List<Mono.TextEditor.Highlighting.Keywords> _keywords;
		static Span[] _spans;
		static Match[] _matches;
		static Marker[] _prevMarker;
		static List<SemanticRule> _SemanticRules;
		static Rule _commentRule;
		static Dictionary<string, Mono.TextEditor.Highlighting.Keywords> _keywordTable;
		static Dictionary<string, Mono.TextEditor.Highlighting.Keywords> _keywordTableIgnoreCase;
		static Dictionary<string, List<string>> _properties;
		#endregion

		static void UpdateCommentRule ()
		{
			if (_commentRule == null)
				return;
			var joinedTasks = string.Join ("", CommentTag.SpecialCommentTags.Select (t => t.Tag));
			_commentRule.SetDelimiter (new string ("&()<>{}[]~!%^*-+=|\\#/:;\"' ,\t.?".Where (c => joinedTasks.IndexOf (c) < 0).ToArray ()));
			_commentRule.Keywords = new[] {
				new Keywords {
					Color = "Comment Tag",
					Words = CommentTag.SpecialCommentTags.Select (t => t.Tag)
				}
			};
		}

		public CSharpSyntaxMode (Document document)
		{
			this.guiDocument = document;
			guiDocument.DocumentParsed += HandleDocumentParsed;
			if (guiDocument.ParsedDocument != null)
				HandleDocumentParsed (this, EventArgs.Empty);

			bool loadRules = _rules == null;

			if (loadRules) {
				var provider = new ResourceStreamProvider (typeof(ResourceStreamProvider).Assembly, typeof(ResourceStreamProvider).Assembly.GetManifestResourceNames ().First (s => s.Contains ("CSharpSyntaxMode")));
				using (var reader = provider.Open ()) {
					SyntaxMode baseMode = SyntaxMode.Read (reader);
					_rules = new List<Rule> (baseMode.Rules.Where (r => r.Name != "Comment"));
					_rules.Add (new Rule {
						Name = "PreProcessorComment"
					});

					_commentRule = new Rule {
						Name = "Comment",
						IgnoreCase = true
					};
					UpdateCommentRule ();

					_rules.Add (_commentRule);
					_keywords = new List<Keywords> (baseMode.Keywords);
					_spans = new List<Span> (baseMode.Spans.Where (span => span.Begin.Pattern != "#")).ToArray ();
					_matches = baseMode.Matches;
					_prevMarker = baseMode.PrevMarker;
					_SemanticRules = new List<SemanticRule> (baseMode.SemanticRules);
					_keywordTable = baseMode.keywordTable;
					_keywordTableIgnoreCase = baseMode.keywordTableIgnoreCase;
					_properties = baseMode.Properties;
				}

				contextualHighlightKeywords = new Dictionary<string, string> ();
				foreach (var word in ContextualKeywords) {
					if (_keywordTable.ContainsKey (word)) {
						contextualHighlightKeywords[word] = _keywordTable[word].Color;
					} else {
						Console.WriteLine ("missing keyword:"+word);
					}
				}

				foreach (var word in ContextualKeywords) {
					_keywordTable.Remove (word);
				}
			}

			rules = _rules;
			keywords = _keywords;
			spans = _spans;
			matches = _matches;
			prevMarker = _prevMarker;
			SemanticRules = _SemanticRules;
			keywordTable = _keywordTable;
			keywordTableIgnoreCase = _keywordTableIgnoreCase;
			properties = _properties;

			if (loadRules) {
				AddSemanticRule ("Comment", new HighlightUrlSemanticRule ("Comment(Line)"));
				AddSemanticRule ("XmlDocumentation", new HighlightUrlSemanticRule ("Comment(Doc)"));
				AddSemanticRule ("String", new HighlightUrlSemanticRule ("String"));
			}
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			if (src != null)
				src.Cancel ();
			guiDocument.DocumentParsed -= HandleDocumentParsed;
		}

		#endregion


//		public override SpanParser CreateSpanParser (DocumentLine line, CloneableStack<Span> spanStack)
//		{
//			return new CSharpSpanParser (this, spanStack ?? line.StartSpan.Clone ());
//		}
		
		public override ChunkParser CreateChunkParser (SpanParser spanParser, ColorScheme style, DocumentLine line)
		{
			return new CSharpChunkParser (this, spanParser, style, line);
		}

		protected class CSharpChunkParser : ChunkParser, IResolveVisitorNavigator
		{

			HashSet<string> tags = new HashSet<string> ();
			
			CSharpSyntaxMode csharpSyntaxMode;
			int lineNumber;
			public CSharpChunkParser (CSharpSyntaxMode csharpSyntaxMode, SpanParser spanParser, ColorScheme style, DocumentLine line) : base (csharpSyntaxMode, spanParser, style, line)
			{
				lineNumber = line.LineNumber;
				this.csharpSyntaxMode = csharpSyntaxMode;
				foreach (var tag in CommentTag.SpecialCommentTags) {
					tags.Add (tag.Tag);
				}

			}

			#region IResolveVisitorNavigator implementation
			ResolveVisitorNavigationMode IResolveVisitorNavigator.Scan(AstNode node)
			{
				if (node is SimpleType || node is MemberType
					|| node is IdentifierExpression || node is MemberReferenceExpression
					|| node is InvocationExpression) {
					return ResolveVisitorNavigationMode.Resolve;
				}
				return ResolveVisitorNavigationMode.Scan;
			}
			
			void IResolveVisitorNavigator.Resolved(AstNode node, ResolveResult result)
			{
			}
			
			void IResolveVisitorNavigator.ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
			}
			#endregion
			static int TokenLength (AstNode node)
			{
				Debug.Assert (node.StartLocation.Line == node.EndLocation.Line);
				return node.EndLocation.Column - node.StartLocation.Column;
			}

			protected override void AddRealChunk (Chunk chunk)
			{
				var document = csharpSyntaxMode.guiDocument;
				var parsedDocument = document != null ? document.ParsedDocument : null;
				if (parsedDocument != null && csharpSyntaxMode.SemanticHighlightingEnabled && csharpSyntaxMode.resolver != null) {
					int endLoc = -1;
					string semanticStyle = null;
					if (spanParser.CurSpan != null && (spanParser.CurSpan.Rule == "Comment" || spanParser.CurSpan.Rule == "PreProcessorComment")) {
						base.AddRealChunk (chunk);
						return;
					}

					try {
						HighlightingVisitior visitor;
						if (!csharpSyntaxMode.lineSegments.TryGetValue (line, out visitor)) {
							var resolver = csharpSyntaxMode.resolver;
							visitor = new HighlightingVisitior (resolver, default (CancellationToken), lineNumber, base.line.Offset, line.Length);
							visitor.tree.InstallListener (doc);
							resolver.RootNode.AcceptVisitor (visitor);
							csharpSyntaxMode.lineSegments[line] = visitor;
						}
						string style;
						if (visitor.tree.GetStyle (chunk, ref endLoc, out style)) {
							semanticStyle = style;
						}
					} catch (Exception e) {
						Console.WriteLine ("Error in semantic highlighting: " + e);
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
						return "Comment Tag";
				}
				return base.GetStyle (chunk);
			}
		}
		

		#region IQuickTaskProvider implementation
		public event EventHandler TasksUpdated;

		protected virtual void OnTasksUpdated (EventArgs e)
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
 
