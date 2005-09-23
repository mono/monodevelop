// project created on 09.08.2003 at 10:16
using System;
using System.Collections.Specialized;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

using ICSharpCode.SharpRefactory.PrettyPrinter;
using ICSharpCode.SharpRefactory.Parser;

class MainClass
{
	static void PrintUsage ()
	{
		Console.WriteLine ("usage: test-parser.exe <dir>");
		Environment.Exit (0);
	}

	static void PrintFile (FileInfo file)
	{
		string fileName = file.FullName;
		//Console.WriteLine ("parsing {0}", fileName);
		Parser p = new Parser();
		p.Parse (new Lexer (new FileReader (fileName)));

		if (p.Errors.count == 0) {
			ErrorVisitor ev = new ErrorVisitor();
			ev.Visit(p.compilationUnit, null);
		}

		if (p.Errors.count == 0 && errorMode) {
			Console.WriteLine ("no errors in {0}", file.Name);
		} else if (p.Errors.count > 0 && !errorMode) {
			Console.WriteLine ("errors in {0}", file.Name);
			foreach (ErrorInfo error in p.Errors.ErrorInformation)
				Console.WriteLine (error.ToString ());
		}
	}

	static void PrintDir (DirectoryInfo dir)
	{
		if (dir.Name.StartsWith ("."))
			return;
		Console.WriteLine ("entering {0}", dir.Name);
		foreach (FileInfo fi in dir.GetFiles ("*.cs"))
			PrintFile (fi);

		foreach (DirectoryInfo di in dir.GetDirectories ())
			PrintDir (di);
	}

	static bool errorMode = false;

	public static void Main (string[] args)
	{
		if (args.Length == 0 || !Directory.Exists (args[0]))
			PrintUsage ();

		if (args.Length == 2 && args[1] == "-e")
			errorMode = true;

		PrintDir (new DirectoryInfo (args[0]));
	}
}

public class ErrorVisitor : AbstractASTVisitor
{
	public ErrorVisitor ()
	{
	}
}

