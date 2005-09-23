using System;
using System.IO;

using ICSharpCode.SharpRefactory.Parser;
using CSharpBinding.Parser;

// print out parsing errors
// from a dir recursively
// or a single file
class ParserTest
{
	static Parser p = new Parser ();
	static Lexer lexer;
	static int counter = 0;
	static int errors = 0;

	static int Main (string[] args)
	{
		if (args.Length == 1 && Directory.Exists (args[0]))
			Parse (new DirectoryInfo (args[0]));
		else if (args.Length == 1 && File.Exists (args[0]))
			Parse (new FileInfo (args[0]));
		else
			return PrintUsage ();

		Console.WriteLine ("{0} out of {1} failed to parse correctly", errors, counter);
		return 0;
	}

	static int PrintUsage ()
	{
		Console.WriteLine ("usage: parse.exe <dir>");
		return 0;
	}

	static void Parse (FileInfo file)
	{
		if (file.Exists) {
			lexer = new Lexer (new FileReader (file.FullName));
			p.Parse (lexer);
			CSharpVisitor v = new CSharpVisitor ();
			v.Visit (p.compilationUnit, null);
			v.Cu.ErrorsDuringCompile = p.Errors.count > 0;
			if (v.Cu.ErrorsDuringCompile) {
				Console.WriteLine ("errors in parsing " + file.FullName);
				errors ++;
			}
			else {
				counter ++;
			}

			foreach (ErrorInfo error in p.Errors.ErrorInformation)
				Console.WriteLine (error.ToString ());
		}
	}

	static void Parse (DirectoryInfo dir)
	{
		foreach (FileInfo f in dir.GetFiles ("*.cs"))
			Parse (f);

		foreach (DirectoryInfo di in dir.GetDirectories ())
			Parse (di);
	}
}

