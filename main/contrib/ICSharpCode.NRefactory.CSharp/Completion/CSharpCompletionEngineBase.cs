// 
// CSharpCompletionEngineBase.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Completion
{
	/// <summary>
	/// Acts as a common base between code completion and parameter completion.
	/// </summary>
	public class CSharpCompletionEngineBase
	{
		protected IDocument document;
		protected int offset;
		protected TextLocation location;
		
		protected ITypeDefinition currentType;
		protected IMember currentMember;
		
		#region Input properties
		public ITypeResolveContext ctx { get; set; }
		public CompilationUnit Unit { get; set; }
		public CSharpParsedFile CSharpParsedFile { get; set; }
		public IProjectContent ProjectContent { get; set; }
		#endregion
		
		protected void SetOffset (int offset)
		{
			this.offset = offset;
			this.location = document.GetLocation (offset);
			
			this.currentType = CSharpParsedFile.GetInnermostTypeDefinition (location);
			this.currentMember = CSharpParsedFile.GetMember (location);	
		}
		
		#region Context helper methods
		protected bool IsInsideComment ()
		{
			return IsInsideComment (offset);
		}
		
		protected bool IsInsideComment (int offset)
		{
			var loc = document.GetLocation (offset);
			return Unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Comment> (loc.Line, loc.Column) != null;
		}
		
		protected bool IsInsideDocComment ()
		{
			var loc = document.GetLocation (offset);
			var cmt = Unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Comment> (loc.Line, loc.Column - 1);
			return cmt != null && cmt.CommentType == CommentType.Documentation;
		}
		
		protected bool IsInsideString ()
		{
			return IsInsideString (offset);
		}
		
		protected bool IsInsideString (int offset)
		{
			var loc = document.GetLocation (offset);
			var expr = Unit.GetNodeAt<PrimitiveExpression> (loc.Line, loc.Column);
			return expr != null && expr.Value is string;
		}
		#endregion
		
		#region Basic parsing/resolving functions
		protected void AppendMissingClosingBrackets (StringBuilder wrapper, string memberText, bool appendSemicolon)
		{
			var bracketStack = new Stack<char> ();
			
			bool isInString = false, isInChar = false;
			bool isInLineComment = false, isInBlockComment = false;
			
			for (int pos = 0; pos < memberText.Length; pos++) {
				char ch = memberText [pos];
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
					} else if (!isInString && !isInChar && pos + 1 < memberText.Length) {
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
					didAppendSemicolon = false;
					lastBracket = ')';
					break;
				case '[':
					wrapper.Append (']');
					didAppendSemicolon = false;
					lastBracket = ']';
					break;
				case '<':
					wrapper.Append ('>');
					didAppendSemicolon = false;
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
				wrapper.Append ("class GenAttr {}");
			} else {
				if (!didAppendSemicolon)
					wrapper.Append (';');
			}
		}

		protected CompilationUnit ParseStub (string continuation, bool appendSemicolon = true)
		{
			var mt = GetMemberTextToCaret ();
			if (mt == null)
				return null;
			
			string memberText = mt.Item1;
			bool wrapInClass = mt.Item2;
			
			var wrapper = new StringBuilder ();
			if (wrapInClass) {
				wrapper.Append ("class Stub {");
				wrapper.AppendLine ();
			}
			
			wrapper.Append (memberText);
			wrapper.Append (continuation);
			AppendMissingClosingBrackets (wrapper, memberText, appendSemicolon);
			
			if (wrapInClass)
				wrapper.Append ('}');
			
			TextLocation memberLocation;
			if (currentMember != null) {
				memberLocation = currentMember.Region.Begin;
			} else if (currentType != null) {
				memberLocation = currentType.Region.Begin;
			} else {
				memberLocation = new TextLocation (1, 1);
			}
			
			using (var stream = new System.IO.StringReader (wrapper.ToString ())) {
				var parser = new CSharpParser ();
				return parser.Parse (stream, wrapInClass ? memberLocation.Line - 2 : 0);
			}
		}
		
		protected Tuple<string, bool> GetMemberTextToCaret ()
		{
			int startOffset;
			if (currentMember != null) {
				startOffset = document.GetOffset (currentMember.Region.BeginLine, currentMember.Region.BeginColumn);
			} else if (currentType != null) {
				startOffset = document.GetOffset (currentType.Region.BeginLine, currentType.Region.BeginColumn);
			} else {
				startOffset = 0;
			}
			return Tuple.Create (document.GetText (startOffset, offset - startOffset), startOffset != 0);
		}

		protected Tuple<CSharpParsedFile, AstNode, CompilationUnit> GetInvocationBeforeCursor (bool afterBracket)
		{
			CompilationUnit baseUnit;
			if (currentMember == null) {
				baseUnit = ParseStub ("", false);
				var section = baseUnit.GetNodeAt<AttributeSection> (location.Line, location.Column - 2);
				var attr = section != null ? section.Attributes.LastOrDefault () : null;
				if (attr != null) {
					// insert target type into compilation unit, to respect the 
					attr.Remove ();
					var node = Unit.GetNodeAt (location) ?? Unit;
					node.AddChild (attr, AttributeSection.AttributeRole);
					return Tuple.Create (CSharpParsedFile, (AstNode)attr, Unit);
				}
			}
			
			if (currentMember == null && currentType == null) {
				return null;
			}
			baseUnit = ParseStub (afterBracket ? "" : "x");
			
			var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			var mref = baseUnit.GetNodeAt (location, n => n is InvocationExpression || n is ObjectCreateExpression); 
			AstNode expr;
			if (mref is InvocationExpression) {
				expr = ((InvocationExpression)mref).Target;
			} else if (mref is ObjectCreateExpression) {
				expr = mref;
			} else {
				return null;
			}
			
			var member = Unit.GetNodeAt<AttributedNode> (memberLocation);
			var member2 = baseUnit.GetNodeAt<AttributedNode> (memberLocation);
			member2.Remove ();
			member.ReplaceWith (member2);
			var tsvisitor = new TypeSystemConvertVisitor (ProjectContent, CSharpParsedFile.FileName);
			Unit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, (AstNode)expr, Unit);

			/*
			
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
			
			var tsvisitor = new TypeSystemConvertVisitor (ProjectContext, Document.FileName);
			Unit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, expr, Unit);*/
		}
		
		protected Tuple<ResolveResult, CSharpResolver> ResolveExpression (CSharpParsedFile file, AstNode expr, CompilationUnit unit)
		{
			if (expr == null)
				return null;
			AstNode resolveNode;
			if (expr is Expression || expr is AstType) {
				resolveNode = expr;
			} else if (expr is VariableDeclarationStatement) {
				resolveNode = ((VariableDeclarationStatement)expr).Type;
			} else {
				resolveNode = expr;
			}
			
			var csResolver = new CSharpResolver (ctx, System.Threading.CancellationToken.None);
			var navigator = new NodeListResolveVisitorNavigator (new[] { resolveNode });
			var visitor = new ResolveVisitor (csResolver, file, navigator);
			visitor.Scan (unit);
//			Print (unit);
			var state = visitor.GetResolverStateBefore (resolveNode);
			var result = visitor.GetResolveResult (resolveNode);
			return Tuple.Create (result, state);
		}
		
		protected static void Print (AstNode node)
		{
			var v = new CSharpOutputVisitor (Console.Out, new CSharpFormattingOptions ());
			node.AcceptVisitor (v, null);
		}
		
		#endregion
	}
}