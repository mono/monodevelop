// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1965 $</version>
// </file>

using System;
using ICSharpCode.NRefactory.Parser.VB;

namespace ICSharpCode.NRefactory.PrettyPrinter
{
	/// <summary>
	/// Description of VBNetOutputFormatter.
	/// </summary>
	public sealed class VBNetOutputFormatter : AbstractOutputFormatter
	{
		public VBNetOutputFormatter(VBNetPrettyPrintOptions prettyPrintOptions) : base(prettyPrintOptions)
		{
		}
		
		public override void PrintToken(int token)
		{
			PrintText(Tokens.GetTokenString(token));
		}
		
		public override void PrintIdentifier(string identifier)
		{
			int token = Keywords.GetToken(identifier);
			if (token < 0 || Tokens.Unreserved[token]) {
				PrintText(identifier);
			} else {
				PrintText("[");
				PrintText(identifier);
				PrintText("]");
			}
		}
		
		public override void PrintComment(Comment comment, bool forceWriteInPreviousBlock)
		{
			switch (comment.CommentType) {
				case CommentType.Block:
					WriteLineInPreviousLine("'" + comment.CommentText.Replace("\n", "\n'"), forceWriteInPreviousBlock);
					break;
				case CommentType.Documentation:
					WriteLineInPreviousLine("'''" + comment.CommentText, forceWriteInPreviousBlock);
					break;
				default:
					WriteLineInPreviousLine("'" + comment.CommentText, forceWriteInPreviousBlock);
					break;
			}
		}
		
		public void PrintLineContinuation()
		{
			if (!LastCharacterIsWhiteSpace)
				Space();
			PrintText("_\r\n");
		}
	}
}
