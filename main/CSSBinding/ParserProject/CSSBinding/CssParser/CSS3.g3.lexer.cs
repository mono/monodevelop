using Antlr.Runtime;
using System;

namespace CssParser
{
	partial class CSS3Lexer
	{
		public override void ReportError(RecognitionException e)
		{
			base.ReportError(e);
			Console.WriteLine("Error in lexer at line " + e.Line + ":" + e.CharPositionInLine);
		}
	}
}
