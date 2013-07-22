using System;
using System.IO;
using Antlr.Runtime;
using Antlr.Runtime.Misc;

namespace CssParser
{
	class Program
	{
		public static void Main(string[] args)
		{
			Stream inputStream = Console.OpenStandardInput();
			ANTLRInputStream input = new ANTLRInputStream(inputStream);
			CSS3Lexer lexer = new CSS3Lexer(input);
			CommonTokenStream tokens = new CommonTokenStream(lexer);
			CSS3Parser parser = new CSS3Parser(tokens);
			//parser.addSubExpr();
		}
	}

	
}



