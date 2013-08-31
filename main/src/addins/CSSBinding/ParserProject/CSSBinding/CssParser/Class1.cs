using System;
using System.IO;
using Antlr.Runtime;


namespace CssParser
{
	class Program
	{
		public static void Main(string[] args)
		{
			Stream inputStream = Console.OpenStandardInput();
			ANTLRInputStream input = new ANTLRInputStream(inputStream);
			CSSLexer lexer = new CSSLexer(input);
			CommonTokenStream tokens = new CommonTokenStream(lexer);
			CSSParser parser = new CSSParser(tokens);
			parser.styleSheet();

			Console.Read();
			
		}
	}

	public partial class CSSLexer
	{
		public override void ReportError(RecognitionException e)
		{
			base.ReportError(e);
			Console.WriteLine("Error in lexer at line " + e.Line + ":" + e.CharPositionInLine + e.Message);
		}

	}


	public partial class CSSParser
	{
		public override void ReportError(RecognitionException e)
		{
			base.ReportError(e);
			int x = e.CharPositionInLine;
			Console.WriteLine("Error in lexer at line " + e.Line + ":" + x);
		}

	}
}



