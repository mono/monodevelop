using Antlr.Runtime;
using System;
namespace CssParser
{
	partial class CSS3Parser
	{
		public override void ReportError(RecognitionException e)
		{
			base.ReportError(e);
			Console.WriteLine("Error in parser at line " + e.Line + ":" + e.CharPositionInLine);
		}
	}
}
