// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1080 $</version>
// </file>

using System.Text;
using System.Collections;
using System.Diagnostics;

using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.CSharp;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.PrettyPrinter
{
	public sealed class CSharpOutputFormatter : AbstractOutputFormatter
	{
		PrettyPrintOptions prettyPrintOptions;
		
		bool          emitSemicolon  = true;
		
		public bool EmitSemicolon {
			get {
				return emitSemicolon;
			}
			set {
				emitSemicolon = value;
			}
		}
		
		public CSharpOutputFormatter(PrettyPrintOptions prettyPrintOptions) : base(prettyPrintOptions)
		{
			this.prettyPrintOptions = prettyPrintOptions;
		}
		
		public override void PrintToken(int token)
		{
			if (token == Tokens.Semicolon && !EmitSemicolon) {
				return;
			}
			PrintText(Tokens.GetTokenString(token));
		}
		
		Stack braceStack = new Stack();
		
		public void BeginBrace(BraceStyle style)
		{
			switch (style) {
				case BraceStyle.EndOfLine:
					Space();
					PrintToken(Tokens.OpenCurlyBrace);
					NewLine();
					++IndentationLevel;
					break;
				case BraceStyle.NextLine:
					NewLine();
					Indent();
					PrintToken(Tokens.OpenCurlyBrace);
					NewLine();
					++IndentationLevel;
					break;
				case BraceStyle.NextLineShifted:
					NewLine();
					++IndentationLevel;
					Indent();
					PrintToken(Tokens.OpenCurlyBrace);
					NewLine();
					break;
				case BraceStyle.NextLineShifted2:
					NewLine();
					++IndentationLevel;
					Indent();
					PrintToken(Tokens.OpenCurlyBrace);
					NewLine();
					++IndentationLevel;
					break;
			}
			braceStack.Push(style);
		}
		
		public void EndBrace()
		{
			BraceStyle style = (BraceStyle)braceStack.Pop();
			switch (style) {
				case BraceStyle.EndOfLine:
				case BraceStyle.NextLine:
					--IndentationLevel;
					Indent();
					PrintToken(Tokens.CloseCurlyBrace);
					NewLine();
					break;
				case BraceStyle.NextLineShifted:
					Indent();
					PrintToken(Tokens.CloseCurlyBrace);
					NewLine();
					--IndentationLevel;
					break;
				case BraceStyle.NextLineShifted2:
					--IndentationLevel;
					Indent();
					PrintToken(Tokens.CloseCurlyBrace);
					NewLine();
					--IndentationLevel;
					break;
			}
		}
		
		public override void PrintIdentifier(string identifier)
		{
			if (Keywords.GetToken(identifier) >= 0)
				PrintText("@");
			PrintText(identifier);
		}
		
		public override void PrintComment(Comment comment, bool forceWriteInPreviousBlock)
		{
			switch (comment.CommentType) {
				case CommentType.Block:
					if (forceWriteInPreviousBlock) {
						WriteInPreviousLine("/*" + comment.CommentText + "*/", forceWriteInPreviousBlock);
					} else {
						PrintText("/*");
						PrintText(comment.CommentText);
						PrintText("*/");
					}
					break;
				case CommentType.Documentation:
					WriteInPreviousLine("///" + comment.CommentText, forceWriteInPreviousBlock);
					break;
				default:
					WriteInPreviousLine("//" + comment.CommentText, forceWriteInPreviousBlock);
					break;
			}
		}
	}
}
