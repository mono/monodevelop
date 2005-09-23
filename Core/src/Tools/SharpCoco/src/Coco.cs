/*-------------------------------------------------------------------------
  Trace output options
  0 | A: prints the states of the scanner automaton
  1 | F: prints the First and Follow sets of all nonterminals
  2 | G: prints the syntax graph of the productions
  3 | I: traces the computation of the First sets
  4 | J: prints the sets associated with ANYs and synchronisation sets
  6 | S: prints the symbol table (terminals, nonterminals, pragmas)
  7 | X: prints a cross reference list of all syntax symbols
  8 | P: prints statistics about the Coco run
  
  Trace output can be switched on by the pragma
    $ { digit | letter }
  in the attributed grammar or as a command-line option
  -------------------------------------------------------------------------*/

using System;
using System.IO;

namespace at.jku.ssw.Coco {

public class Coco {
	
	public static void Main (string[] arg) {
		Console.WriteLine("Coco/R (Aug 4, 2003)");
		string ATGName = null; 
		for (int i = 0; i < arg.Length; i++) {
			if (arg[i] == "-nonamespace") Tab.nsName = null;
			else if (arg[i] == "-namespace") Tab.nsName = arg[++i];
			else if (arg[i] == "-trace") Tab.SetDDT(arg[++i]); 
			else ATGName = arg[i];
		}
		if (arg.Length > 0 && ATGName != null) {
			int pos = ATGName.LastIndexOf('/');
			if (pos < 0) pos = ATGName.LastIndexOf('\\');
			string file = ATGName;
			string dir = ATGName.Substring(0, pos+1);
			
			Scanner.Init(file);
			Trace.Init(dir);
			Tab.Init(); 
			DFA.Init(dir); 
			ParserGen.Init(file, dir);

			Parser.Parse();

			Trace.Close();
			Console.WriteLine();
			Console.WriteLine("{0} errors detected", Errors.count);
		} else {
			Console.WriteLine("Usage: Coco {{Option}} Grammar.ATG {{Option}}{0}" +
			                  "Options:{0}" +
			                  "  -nonamespace{0}" +
			                  "  -namespace <packageName>{0}" +
			                  "  -trace   <traceString>{0}" +
			                  "Valid characters in the trace string:{0}" +
			                  "  A  trace automaton{0}" +
			                  "  F  list first/follow sets{0}" +
			                  "  G  print syntax graph{0}" +
			                  "  I  trace computation of first sets{0}" +
			                  "  P  print statistics{0}" +
			                  "  S  list symbol table{0}" +
			                  "  X  list cross reference table{0}" +
			                  "Scanner.frame and Parser.frame files needed in ATG directory{0}" +
                        "or in a directory referenced by the environment variable CRFRAMES.",
			                  Environment.NewLine);
		}
	}
} // end Coco

} // end namespace
