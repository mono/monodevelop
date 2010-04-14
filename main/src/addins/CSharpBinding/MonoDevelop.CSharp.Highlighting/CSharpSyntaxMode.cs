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

namespace MonoDevelop.CSharp.Highlighting
{
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
					TextEditorData data = doc.TextEditorData;
					if (data == null)
						continue;
					Mono.TextEditor.Document document = data.Document;
					document.UpdateHighlighting ();
					document.CommitUpdateAll ();
				}
			};
		}
		
		static void OnDisableConditionalCompilation (object s, MonoDevelop.Ide.Gui.DocumentEventArgs e)
		{
			CSharpSyntaxMode mode = e.Document.TextEditorData.Document.SyntaxMode as CSharpSyntaxMode;
			if (mode == null)
				return;
			mode.DisableConditionalHighlighting = true;
			e.Document.TextEditorData.Document.CommitUpdateAll ();
		}
		
		public CSharpSyntaxMode ()
		{
			ResourceXmlProvider provider = new ResourceXmlProvider (typeof(IXmlProvider).Assembly, typeof(IXmlProvider).Assembly.GetManifestResourceNames ().First (s => s.Contains ("CSharpSyntaxMode")));
			using (XmlReader reader = provider.Open ()) {
				SyntaxMode baseMode = SyntaxMode.Read (reader);
				this.rules = new List<Rule> (baseMode.Rules);
				this.keywords = new List<Keywords> (baseMode.Keywords);
				this.spans = new List<Span> (baseMode.Spans.Where (span => span.Begin.Pattern != "#")).ToArray ();
				this.matches = baseMode.Matches;
				this.prevMarker = baseMode.PrevMarker;
				this.SemanticRules = new List<SemanticRule> (baseMode.SemanticRules);
				this.table = baseMode.Table;
				this.properties = baseMode.Properties;
			}
			
			AddSemanticRule ("Comment", new HighlightUrlSemanticRule ("comment"));
			AddSemanticRule ("XmlDocumentation", new HighlightUrlSemanticRule ("comment"));
			AddSemanticRule ("String", new HighlightUrlSemanticRule ("string"));
			AddSemanticRule (new HighlightCSharpSemanticRule ());
		}
		
		public override SpanParser CreateSpanParser (Mono.TextEditor.Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack)
		{
			return new CSharpSpanParser (doc, mode, line, spanStack);
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
				if (disabled) {
					TagColor = Color = "comment.block";
					Rule = "String";
					return;
				}
				
				TagColor = "text.preprocessor";
				if (!IsValid) {
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
		
		protected class CSharpSpanParser : SpanParser
		{
			CSharpSyntaxMode CSharpSyntaxMode {
				get {
					return (CSharpSyntaxMode)mode;
				}
			}
			class ConditinalExpressionEvaluator : ICSharpCode.NRefactory.Visitors.AbstractAstVisitor
			{
				HashSet<string> symbols = new HashSet<string> ();
				
				public ConditinalExpressionEvaluator (Mono.TextEditor.Document doc)
				{
					var project = IdeApp.ProjectOperations.CurrentSelectedProject;
					if (project != null) {
						DotNetProjectConfiguration configuration = project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
						if (configuration != null) {
							CSharpCompilerParameters cparams = configuration.CompilationParameters as CSharpCompilerParameters;
							if (cparams != null) {
								string[] syms = cparams.DefineSymbols.Split (';', ',');
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
					
					ProjectDom dom = ProjectDomService.GetProjectDom (project);
					ParsedDocument parsedDocument = ProjectDomService.GetParsedDocument (dom, doc.FileName);
/*					if (parsedDocument == null)
						parsedDocument = ProjectDomService.ParseFile (dom, doc.FileName ?? "a.cs", delegate { return doc.Text; });*/
					if (parsedDocument != null) {
						foreach (PreProcessorDefine define in parsedDocument.Defines) {
							symbols.Add (define.Define);
						}
					}
				}
				
				public override object VisitIdentifierExpression (ICSharpCode.NRefactory.Ast.IdentifierExpression identifierExpression, object data)
				{
					return symbols.Contains (identifierExpression.Identifier);
				}
				
				public override object VisitUnaryOperatorExpression (ICSharpCode.NRefactory.Ast.UnaryOperatorExpression unaryOperatorExpression, object data)
				{
					bool result = (bool)(unaryOperatorExpression.Expression.AcceptVisitor (this, data) ?? (object)false);
					if (unaryOperatorExpression.Op == ICSharpCode.NRefactory.Ast.UnaryOperatorType.Not)
						return !result;
					return result;
				}
				
				public override object VisitPrimitiveExpression (ICSharpCode.NRefactory.Ast.PrimitiveExpression primitiveExpression, object data)
				{
					return (bool)primitiveExpression.Value;
				}

				public override object VisitBinaryOperatorExpression (ICSharpCode.NRefactory.Ast.BinaryOperatorExpression binaryOperatorExpression, object data)
				{
					bool left  = (bool)(binaryOperatorExpression.Left.AcceptVisitor (this, data) ?? (object)false);
					bool right = (bool)(binaryOperatorExpression.Right.AcceptVisitor (this, data) ?? (object)false);
					
					switch (binaryOperatorExpression.Op) {
					case ICSharpCode.NRefactory.Ast.BinaryOperatorType.InEquality:
						return left != right;
					case ICSharpCode.NRefactory.Ast.BinaryOperatorType.Equality:
						return left == right;
					case ICSharpCode.NRefactory.Ast.BinaryOperatorType.LogicalOr:
						return left || right;
					case ICSharpCode.NRefactory.Ast.BinaryOperatorType.LogicalAnd:
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
				if (i + 5 < doc.Length && doc.GetTextAt (i, 5) == "#else") {
					LineSegment line = doc.GetLineByOffset (i);
					
					bool previousResult = false;
					foreach (Span span in spanStack.ToArray ().Reverse ()) {
						if (span is IfBlockSpan) {
							previousResult = ((IfBlockSpan)span).IsValid;
						}
						if (span is ElseIfBlockSpan) {
							previousResult |= ((ElseIfBlockSpan)span).IsValid;
						}
					}
					
					
					int length = line.Offset + line.EditableLength - i;
					while (spanStack.Count > 0 && !(CurSpan is IfBlockSpan)) {
						spanStack.Pop ();
					}
					IfBlockSpan ifBlock = (IfBlockSpan)CurSpan;
					ElseBlockSpan elseBlockSpan = new ElseBlockSpan (!previousResult);
					if (ifBlock != null) 
						elseBlockSpan.Disabled = ifBlock.Disabled;
					OnFoundSpanBegin (elseBlockSpan, i, 0);
					
					spanStack.Push (elseBlockSpan);
					ruleStack.Push (GetRule (elseBlockSpan));
					
					// put pre processor eol span on stack, so that '#else' gets the correct highlight
					OnFoundSpanBegin (elseBlockSpan, i, 5);
					spanStack.Push (elseBlockSpan);
					ruleStack.Push (GetRule (elseBlockSpan));
					i += length - 1;
					return;
				}
				if (CurRule.Name == "<root>" && i + 3 < doc.Length && doc.GetTextAt (i, 3) == "#if") {
					LineSegment line = doc.GetLineByOffset (i);
					int length = line.Offset + line.EditableLength - i;
					string parameter = doc.GetTextAt (i + 3, length - 3);
					ICSharpCode.NRefactory.Parser.CSharp.Lexer lexer = new ICSharpCode.NRefactory.Parser.CSharp.Lexer (new System.IO.StringReader (parameter));
					ICSharpCode.NRefactory.Ast.Expression expr = lexer.PPExpression ();
					bool result = false;
					if (expr != null && !expr.IsNull) {
						object o = expr.AcceptVisitor (new ConditinalExpressionEvaluator (doc), null);
						if (o is bool)
							result = (bool)o;
					}
					IfBlockSpan ifBlockSpan = new IfBlockSpan (result);
					
					foreach (Span span in spanStack.ToArray ()) {
						if (span is AbstractBlockSpan) {
							AbstractBlockSpan parentBlock = (AbstractBlockSpan)span;
							ifBlockSpan.Disabled = parentBlock.Disabled || !parentBlock.IsValid;
							break;
						}
					}
					
					OnFoundSpanBegin (ifBlockSpan, i, length);
					i += length - 1;
					spanStack.Push (ifBlockSpan);
					ruleStack.Push (GetRule (ifBlockSpan));
					return;
				}
				if (i + 5 < doc.Length && doc.GetTextAt (i, 5) == "#elif" && spanStack.Any (span => span is IfBlockSpan)) {
					LineSegment line = doc.GetLineByOffset (i);
					int length = line.Offset + line.EditableLength - i;
					string parameter = doc.GetTextAt (i + 5, length - 5);
					
					ICSharpCode.NRefactory.Parser.CSharp.Lexer lexer = new ICSharpCode.NRefactory.Parser.CSharp.Lexer (new System.IO.StringReader (parameter));
					ICSharpCode.NRefactory.Ast.Expression expr = lexer.PPExpression ();
				
					bool result = !expr.IsNull ? (bool)expr.AcceptVisitor (new ConditinalExpressionEvaluator (doc), null) : false;
					
					IfBlockSpan containingIf = null;
					if (result) {
						bool previousResult = false;
						foreach (Span span in spanStack.ToArray ().Reverse ()) {
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
					
					OnFoundSpanBegin (elseIfBlockSpan, i, 0);
					
					spanStack.Push (elseIfBlockSpan);
					ruleStack.Push (GetRule (elseIfBlockSpan));
					
					// put pre processor eol span on stack, so that '#elif' gets the correct highlight
					OnFoundSpanBegin (elseIfBlockSpan, i, 1);
					spanStack.Push (elseIfBlockSpan);
					ruleStack.Push (GetRule (elseIfBlockSpan));
					//i += length - 1;
					return;
				}
				if (CurRule.Name == "<root>" &&  doc.GetCharAt (i) == '#') {
					Span preprocessorSpan = new Span ();
					
					preprocessorSpan.TagColor = "text.preprocessor";
					preprocessorSpan.Color = "text.preprocessor";
					preprocessorSpan.Rule = "String";
					preprocessorSpan.StopAtEol = true;
					OnFoundSpanBegin (preprocessorSpan, i, 1);
					spanStack.Push (preprocessorSpan);
					ruleStack.Push (GetRule (preprocessorSpan));
				}

				base.ScanSpan (ref i);
			}
			
			protected override bool ScanSpanEnd (Mono.TextEditor.Highlighting.Span cur, int i)
			{
				if (cur is IfBlockSpan || cur is ElseIfBlockSpan || cur is ElseBlockSpan) {
					bool end = i + 6 <= doc.Length && doc.GetTextAt (i, 6) == "#endif";
					if (end) {
						OnFoundSpanEnd (cur, i, 6); // put empty end tag in
						while (spanStack.Count > 0 && !(spanStack.Peek () is IfBlockSpan)) {
							spanStack.Pop ();
							if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
								ruleStack.Pop ();
						}
						if (spanStack.Count > 0)
							spanStack.Pop ();
						if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
							ruleStack.Pop ();
					/*	// put pre processor eol span on stack, so that '#endif' gets the correct highlight
						foreach (Span span in mode.Spans) {
							if (span.Rule == "text.preprocessor") {
								OnFoundSpanBegin (span, i, 6);
								spanStack.Push (span);
								ruleStack.Push (GetRule (span));
								break;
							}
						}*/
					}
					return end;
				}
				return base.ScanSpanEnd (cur, i);
			}
			
	//		Span preprocessorSpan;
	//		Rule preprocessorRule;
			
			public CSharpSpanParser (Mono.TextEditor.Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack) : base (doc, mode, line, spanStack)
			{
		/*		foreach (Span span in mode.Spans) {
					if (span.Rule == "text.preprocessor") {
						preprocessorSpan = span;
						preprocessorRule = GetRule (span);
					}
				}*/
			}
		}
	}
}
 