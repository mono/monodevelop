/*using System;
using ICSharpCode.SharpRefactory.Parser.AST.VB;
using ICSharpCode.SharpRefactory.Parser.VB;

class MainClass
{
	public static void Main (string[] args) {
		
		string fileName = args[0];
		
		Console.WriteLine("Parsing source file {0}", fileName);
		IReader reader = new FileReader(fileName);
		Lexer lexer = new Lexer(reader);
		
//		while(true)
//		{
//			Token t = lexer.NextToken();
//			if(t.kind == Tokens.EOF) break;
//			
//			System.Console.WriteLine(t.val + "\t" + t.kind);
//		}
		
		Parser p = new Parser();
		p.Parse(lexer);
		if(p.Errors.count == 0) {
//			p.compilationUnit.AcceptVisitor(new DebugVisitor(), null);
		}
		
		System.Console.WriteLine("=======================");
		if (p.Errors.count == 1)
			Console.WriteLine("1 error dectected");
		else {
			Console.WriteLine("{0} errors dectected", p.Errors.count);
		}
		
		if(p.Errors.count != 0) {
			System.Console.WriteLine(p.Errors.ErrorOutput);
		}
		System.Console.WriteLine("=======================");
	}
}*/
