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
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Tasks;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp.ContextAction;
using MonoDevelop.CSharp.Resolver;

namespace MonoDevelop.CSharp.Highlighting
{
	static class StringHelper
	{
		public static bool IsAt (this string str, int idx, string pattern)
		{
			if (idx + pattern.Length > str.Length)
				return false;
			int i = idx;
			return pattern.All (ch => str[i++] == ch);
		}
	}		

	public class CSharpSyntaxMode : Mono.TextEditor.Highlighting.SyntaxMode
	{
		public bool DisableConditionalHighlighting {
			get;
			set;
		}
		
		static CSharpSyntaxMode ()
		{
			MonoDevelop.Debugger.DebuggingService.DisableConditionalCompilation += (EventHandler<DocumentEventArgs>)DispatchService.GuiDispatch (new EventHandler<DocumentEventArgs> (OnDisableConditionalCompilation));
			IdeApp.Workspace.ActiveConfigurationChanged += delegate {
				foreach (var doc in IdeApp.Workbench.Documents) {
					TextEditorData data = doc.Editor;
					if (data == null)
						continue;
					Mono.TextEditor.Document document = data.Document;
					document.Text = document.Text;
					doc.UpdateParseDocument ();
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
		
		public CSharpSyntaxMode ()
		{
			var provider = new ResourceXmlProvider (typeof(IXmlProvider).Assembly, typeof(IXmlProvider).Assembly.GetManifestResourceNames ().First (s => s.Contains ("CSharpSyntaxMode")));
			using (XmlReader reader = provider.Open ()) {
				SyntaxMode baseMode = SyntaxMode.Read (reader);
				this.rules = new List<Rule> (baseMode.Rules);
				this.keywords = new List<Keywords> (baseMode.Keywords);
				this.spans = new List<Span> (baseMode.Spans.Where (span => span.Begin.Pattern != "#")).ToArray ();
				this.matches = baseMode.Matches;
				this.prevMarker = baseMode.PrevMarker;
				this.SemanticRules = new List<SemanticRule> (baseMode.SemanticRules);
				this.keywordTable = baseMode.keywordTable;
				this.keywordTableIgnoreCase = baseMode.keywordTableIgnoreCase;
				this.properties = baseMode.Properties;
			}
			
			AddSemanticRule ("Comment", new HighlightUrlSemanticRule ("comment"));
			AddSemanticRule ("XmlDocumentation", new HighlightUrlSemanticRule ("comment"));
			AddSemanticRule ("String", new HighlightUrlSemanticRule ("string"));
		}
		
		public override SpanParser CreateSpanParser (Mono.TextEditor.Document doc, SyntaxMode mode, LineSegment line, CloneableStack<Span> spanStack)
		{
			return new CSharpSpanParser (doc, mode, spanStack ?? line.StartSpan.Clone ());
		}
		
		public override ChunkParser CreateChunkParser (SpanParser spanParser, Mono.TextEditor.Document doc, ColorSheme style, SyntaxMode mode, LineSegment line)
		{
			return new CSharpChunkParser (spanParser, doc, style, mode, line);
		}
		
		abstract class AbstractBlockSpan : Span
		{
			public bool IsValid {
				get;
				private set;
			}
			
			bool disabled;
			
			public bool Disabled {
				get { return this.disabled; }
				set { this.disabled = value; SetColor (); }
			}
			
			
			public AbstractBlockSpan (bool isValid)
			{
				this.IsValid = isValid;
				SetColor ();
				StopAtEol = false;
			}
			
			protected void SetColor ()
			{
				TagColor = "text.preprocessor";
				if (disabled || !IsValid) {
					Color = "comment.block";
					Rule = "String";
				} else {
					Color = "text";
					Rule = "<root>";
				}
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
		
		protected class CSharpChunkParser : ChunkParser
		{
			HashSet<string> tags = new HashSet<string> ();
			MonoDevelop.Ide.Gui.Document document;
			static HashSet<string> contextualKeywords = new HashSet<string> ();
//			NRefactoryResolver resolver;

			
			static CSharpChunkParser ()
			{
				contextualKeywords.Add ("get");
				contextualKeywords.Add ("set");
				contextualKeywords.Add ("value");
				
				contextualKeywords.Add ("add");
				contextualKeywords.Add ("remove");
				
				contextualKeywords.Add ("var");
				
				contextualKeywords.Add ("where");
				contextualKeywords.Add ("global");
				contextualKeywords.Add ("partial");
			}
			
			public CSharpChunkParser (SpanParser spanParser, Mono.TextEditor.Document doc, ColorSheme style, SyntaxMode mode, LineSegment line) : base (spanParser, doc, style, mode, line)
			{
				document = IdeApp.Workbench.GetDocument (doc.FileName);
				
				foreach (var tag in ProjectDomService.SpecialCommentTags) {
					tags.Add (tag.Tag);
				}
//				ICSharpCode.OldNRefactory.Ast.CompilationUnit unit = null;
//				if (document != null && document.ParsedDocument != null && MonoDevelop.Core.PropertyService.Get ("EnableSemanticHighlighting", false)) {
//					resolver = document.GetResolver ();
//					if (!document.ParsedDocument.TryGetTag (out unit)) {
//						try {
//							using (ICSharpCode.OldNRefactory.IParser parser = ICSharpCode.OldNRefactory.ParserFactory.CreateParser (ICSharpCode.OldNRefactory.SupportedLanguage.CSharp, document.Editor.Document.OpenTextReader ())) {
//								parser.Parse ();
//								unit = parser.CompilationUnit;
//								document.ParsedDocument.SetTag (unit);
//							}
//						} catch (Exception) {
//							resolver = null;
//							return;
//						}
//					}
//					resolver.SetupParsedCompilationUnit (unit);
//				}
			}
			
			string GetSemanticStyle (ParsedDocument parsedDocument, Chunk chunk, ref int endOffset)
			{
				var unit = parsedDocument.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit;
				if (unit == null)
					return null;
				
				var loc = doc.OffsetToLocation (chunk.Offset);
				if (contextualKeywords.Contains (wordbuilder.ToString ())) {
					var node = unit.GetNodeAt (loc.Line, loc.Column);
					if (node is Identifier) {
						switch (((Identifier)node).Name) {
						case "value":
							// highlight 'value' in property setters and event add/remove
							var n = node.Parent;
							while (n != null) {
								if (n is Accessor && n.Role != PropertyDeclaration.GetterRole)
									return null;
								n = n.Parent;
							}
							break;
						case "var": 
							if (node.Parent != null) {
								var vds = node.Parent.Parent as VariableDeclarationStatement;
								if (node.Parent.Parent is ForeachStatement && ((ForeachStatement)node.Parent.Parent).VariableType.StartLocation == node.StartLocation ||
									vds != null && node.StartLocation == vds.Type.StartLocation)
									return null;
							}
							break;
						}
					}
					if (node is CSharpTokenNode) 
						return null;
					endOffset = doc.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
					return spanParser.CurSpan != null ? spanParser.CurSpan.Color : "text";
				} else {
//					var type = unit.GetNodeAt<AstType> (loc.Line, loc.Column);
//					if (type is SimpleType) {
//						var st = (SimpleType)type;
//						if (st.IdentifierToken.Contains (loc.Line, loc.Column) && unit.GetNodeAt<UsingDeclaration> (loc.Line, loc.Column) == null) {
//							endOffset = doc.LocationToOffset (st.IdentifierToken.EndLocation.Line, st.IdentifierToken.EndLocation.Column);
//							return "keyword.semantic.type";
//						}
//						return null;
//					}
//					if (type is ICSharpCode.NRefactory.CSharp.MemberType) {
//						var mt = (ICSharpCode.NRefactory.CSharp.MemberType)type;
//						if (mt.MemberNameToken.Contains (loc.Line, loc.Column) && unit.GetNodeAt<UsingDeclaration> (loc.Line, loc.Column) == null) {
//							endOffset = doc.LocationToOffset (mt.MemberNameToken.EndLocation.Line, mt.MemberNameToken.EndLocation.Column);
//							return "keyword.semantic.type";
//						}
//						return null;
//					}
//					
//					var node = unit.GetNodeAt (loc.Line, loc.Column);
//					if (node is Identifier) {
//						if (node.Parent is TypeDeclaration && node.Role == TypeDeclaration.Roles.Identifier) {
//							endOffset = doc.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
//							return "keyword.semantic.type";
//						}
//						
//						if (node.Parent is VariableInitializer && node.Parent.Parent is FieldDeclaration || node.Parent is FixedVariableInitializer || node.Parent is EnumMemberDeclaration) {
//							endOffset = doc.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
//							return "keyword.semantic.field";
//						}
//					}
//					var identifierExpression = unit.GetNodeAt<IdentifierExpression> (loc.Line, loc.Column);
//					if (identifierExpression != null) {
//						var result = identifierExpression.ResolveExpression (document, resolver, loc);
//						if (result is MemberResolveResult) {
//							var member = ((MemberResolveResult)result).ResolvedMember;
//							if (member is IField) {
//								endOffset = doc.LocationToOffset (identifierExpression.EndLocation.Line, identifierExpression.EndLocation.Column);
//								return "keyword.semantic.field";
//							}
//							if (member == null && result.ResolvedType != null && !string.IsNullOrEmpty (result.ResolvedType.FullName)) {
//								endOffset = doc.LocationToOffset (identifierExpression.EndLocation.Line, identifierExpression.EndLocation.Column);
//								return "keyword.semantic.type";
//							}
//						}
//					}
//					
//					var memberReferenceExpression = unit.GetNodeAt<MemberReferenceExpression> (loc.Line, loc.Column);
//					if (memberReferenceExpression != null) {
//						if (!memberReferenceExpression.MemberNameToken.Contains (loc.Line, loc.Column)) 
//							return null;
//						
//						var result = memberReferenceExpression.ResolveExpression (document, resolver, loc);
//						if (result is MemberResolveResult) {
//							var member = ((MemberResolveResult)result).ResolvedMember;
//							if (member is IField) {
//								endOffset = doc.LocationToOffset (memberReferenceExpression.MemberNameToken.EndLocation.Line, memberReferenceExpression.MemberNameToken.EndLocation.Column);
//								return "keyword.semantic.field";
//							}
//							if (member == null && result.ResolvedType != null && !string.IsNullOrEmpty (result.ResolvedType.FullName)) {
//								endOffset = doc.LocationToOffset (memberReferenceExpression.MemberNameToken.EndLocation.Line, memberReferenceExpression.MemberNameToken.EndLocation.Column);
//								return "keyword.semantic.type";
//							}
//						}
//					}
				}
				return null;
			}
			
			protected override void AddRealChunk (Chunk chunk)
			{
				var parsedDocument = document != null ? document.ParsedDocument : null;
				if (parsedDocument != null && MonoDevelop.Core.PropertyService.Get ("EnableSemanticHighlighting", false)) {
					int endLoc = -1;
					string semanticStyle = GetSemanticStyle (parsedDocument, chunk, ref endLoc);
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
			class ConditinalExpressionEvaluator : ICSharpCode.OldNRefactory.Visitors.AbstractAstVisitor
			{
				HashSet<string> symbols = new HashSet<string> ();
				
				public ConditinalExpressionEvaluator (Mono.TextEditor.Document doc)
				{
					var project = IdeApp.ProjectOperations.CurrentSelectedProject;
					if (project != null) {
						var configuration = project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
						if (configuration != null) {
							var cparams = configuration.CompilationParameters as CSharpCompilerParameters;
							if (cparams != null) {
								string[] syms = cparams.DefineSymbols.Split (';', ',', ' ', '\t');
								foreach (string s in syms) {
									string ss = s.Trim ();
									if (ss.Length > 0 && !symbols.Contains (ss))
										symbols.Add (ss);
								}
							}
						} else {
							Console.WriteLine ("NO CONFIGURATION");
						}
					}
					
					var dom = ProjectDomService.GetProjectDom (project);
					var parsedDocument = ProjectDomService.GetParsedDocument (dom, doc.FileName);
/*					if (parsedDocument == null)
						parsedDocument = ProjectDomService.ParseFile (dom, doc.FileName ?? "a.cs", delegate { return doc.Text; });*/
					if (parsedDocument != null) {
						foreach (PreProcessorDefine define in parsedDocument.Defines) {
							symbols.Add (define.Define);
						}
					}
				}
				
				public override object VisitIdentifierExpression (ICSharpCode.OldNRefactory.Ast.IdentifierExpression identifierExpression, object data)
				{
					return symbols.Contains (identifierExpression.Identifier);
				}
				
				public override object VisitUnaryOperatorExpression (ICSharpCode.OldNRefactory.Ast.UnaryOperatorExpression unaryOperatorExpression, object data)
				{
					bool result = (bool)(unaryOperatorExpression.Expression.AcceptVisitor (this, data) ?? (object)false);
					if (unaryOperatorExpression.Op == ICSharpCode.OldNRefactory.Ast.UnaryOperatorType.Not)
						return !result;
					return result;
				}
				
				public override object VisitPrimitiveExpression (ICSharpCode.OldNRefactory.Ast.PrimitiveExpression primitiveExpression, object data)
				{
					return (bool)primitiveExpression.Value;
				}

				public override object VisitBinaryOperatorExpression (ICSharpCode.OldNRefactory.Ast.BinaryOperatorExpression binaryOperatorExpression, object data)
				{
					bool left  = (bool)(binaryOperatorExpression.Left.AcceptVisitor (this, data) ?? (object)false);
					bool right = (bool)(binaryOperatorExpression.Right.AcceptVisitor (this, data) ?? (object)false);
					
					switch (binaryOperatorExpression.Op) {
					case ICSharpCode.OldNRefactory.Ast.BinaryOperatorType.InEquality:
						return left != right;
					case ICSharpCode.OldNRefactory.Ast.BinaryOperatorType.Equality:
						return left == right;
					case ICSharpCode.OldNRefactory.Ast.BinaryOperatorType.LogicalOr:
						return left || right;
					case ICSharpCode.OldNRefactory.Ast.BinaryOperatorType.LogicalAnd:
						return left && right;
					}
					
					Console.WriteLine ("Unknown operator:" + binaryOperatorExpression.Op);
					return left;
				}
			}
			
			protected override void ScanSpan (ref int i)
			{
				if (CSharpSyntaxMode.DisableConditionalHighlighting) {
					base.ScanSpan (ref i);
					return;
				}
				int textOffset = i - StartOffset;
				if (CurText.IsAt (textOffset, "#else")) {
					if (!spanStack.Any (s => s is IfBlockSpan || s is ElseIfBlockSpan)) {
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
					while (spanStack.Count > 0 && !(CurSpan is IfBlockSpan || CurSpan is ElseIfBlockSpan)) {
						spanStack.Pop ();
					}
					IfBlockSpan ifBlock = CurSpan as IfBlockSpan;
					var elseIfBlock = CurSpan as ElseIfBlockSpan;
					var elseBlockSpan = new ElseBlockSpan (!previousResult);
					if (ifBlock != null) {
						elseBlockSpan.Disabled = ifBlock.Disabled;
					} else if (elseIfBlock != null) {
						elseBlockSpan.Disabled = elseIfBlock.Disabled;
					}
					FoundSpanBegin (elseBlockSpan, i, "#else".Length);
					i += "#else".Length;
					
					// put pre processor eol span on stack, so that '#elif' gets the correct highlight
					Span preprocessorSpan = CreatePreprocessorSpan ();
					FoundSpanBegin (preprocessorSpan, i, 0);
					return;
				}
				if (CurRule.Name == "<root>" && CurText.IsAt (textOffset, "#if")) {
					int length = CurText.Length - textOffset;
					string parameter = CurText.Substring (textOffset + 3, length - 3);
					ICSharpCode.OldNRefactory.Parser.CSharp.Lexer lexer = new ICSharpCode.OldNRefactory.Parser.CSharp.Lexer (new System.IO.StringReader (parameter));
					ICSharpCode.OldNRefactory.Ast.Expression expr = lexer.PPExpression ();
					bool result = false;
					if (expr != null && !expr.IsNull) {
						object o = expr.AcceptVisitor (new ConditinalExpressionEvaluator (doc), null);
						if (o is bool)
							result = (bool)o;
					}
					IfBlockSpan ifBlockSpan = new IfBlockSpan (result);
					
					foreach (Span span in spanStack) {
						if (span is AbstractBlockSpan) {
							var parentBlock = (AbstractBlockSpan)span;
							ifBlockSpan.Disabled = parentBlock.Disabled || !parentBlock.IsValid;
							break;
						}
					}
					
					FoundSpanBegin (ifBlockSpan, i, length);
					i += length - 1;
					return;
				}
				if (CurText.IsAt (textOffset, "#elif") && spanStack.Any (span => span is IfBlockSpan)) {
					LineSegment line = doc.GetLineByOffset (i);
					int length = line.Offset + line.EditableLength - i;
					string parameter = doc.GetTextAt (i + 5, length - 5);
					
					ICSharpCode.OldNRefactory.Parser.CSharp.Lexer lexer = new ICSharpCode.OldNRefactory.Parser.CSharp.Lexer (new System.IO.StringReader (parameter));
					ICSharpCode.OldNRefactory.Ast.Expression expr = lexer.PPExpression ();
				
					bool result = !expr.IsNull ? (bool)expr.AcceptVisitor (new ConditinalExpressionEvaluator (doc), null) : false;
					
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
					
					ElseIfBlockSpan elseIfBlockSpan = new ElseIfBlockSpan (result);
					if (containingIf != null)
						elseIfBlockSpan.Disabled = containingIf.Disabled;
					
					FoundSpanBegin (elseIfBlockSpan, i, 0);
					
					// put pre processor eol span on stack, so that '#elif' gets the correct highlight
					var preprocessorSpan = CreatePreprocessorSpan ();
					FoundSpanBegin (preprocessorSpan, i, 0);
					//i += length - 1;
					return;
				}
				if (CurRule.Name == "<root>" &&  CurText[textOffset] == '#') {
					var preprocessorSpan = CreatePreprocessorSpan ();
					FoundSpanBegin (preprocessorSpan, i, 1);
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
			
			protected override bool ScanSpanEnd (Mono.TextEditor.Highlighting.Span cur, ref int i)
			{
				if (cur is IfBlockSpan || cur is ElseIfBlockSpan || cur is ElseBlockSpan) {
					int textOffset = i - StartOffset;
					bool end = CurText.IsAt (textOffset, "#endif");
					if (end) {
						FoundSpanEnd (cur, i, 6); // put empty end tag in
						while (spanStack.Count > 0 && (spanStack.Peek () is IfBlockSpan || spanStack.Peek () is ElseIfBlockSpan || spanStack.Peek () is ElseBlockSpan)) {
							spanStack.Pop ();
							if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
								ruleStack.Pop ();
						}
					}
					return end;
				}
				return base.ScanSpanEnd (cur, ref i);
			}
			
	//		Span preprocessorSpan;
	//		Rule preprocessorRule;
			
			public CSharpSpanParser (Mono.TextEditor.Document doc, SyntaxMode mode, CloneableStack<Span> spanStack) : base (doc, mode, spanStack)
			{
//				foreach (Span span in mode.Spans) {
//					if (span.Rule == "text.preprocessor") {
//						preprocessorSpan = span;
//						preprocessorRule = GetRule (span);
//					}
//				}
			}
		}
	}
}
 