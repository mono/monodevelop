// ParserGen.cs   Parser generator of Coco/R    H.Moessenboeck, Univ. of Linz
//----------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections;
using System.Text;

namespace at.jku.ssw.Coco {

public class ParserGen {

	const int maxTerm = 3;		// sets of size < maxTerm are enumerated
	const char CR  = '\r';
	const char LF  = '\n';
	const char TAB = '\t';
	const int EOF = -1;

	const int tErr = 0;			// error codes
	const int altErr = 1;
	const int syncErr = 2;
	
	public static Position usingPos; // "using" definitions from the attributed grammar
	
	static int errorNr;				// highest parser error number
	static Symbol curSy;			// symbol whose production is currently generated
	static FileStream fram;		// parser frame file
	static StreamWriter gen;	// generated parser source file
	static StringWriter err;	// generated parser error messages
	static string srcName;    // name of attributed grammar file
	static string srcDir;     // directory of attributed grammar file
	static ArrayList symSet = new ArrayList();
	
	static void Indent (int n) {
		for (int i = 1; i <= n; i++) gen.Write('\t');
	}
	
	/* AW: this replaces the method int Alternatives (Node p) */
	static bool UseSwitch (Node p) {
		if (p.typ != Node.alt) return false;
		int nAlts = 0;
		while (p != null) {
		  ++nAlts;
		  // must not optimize with switch-statement, if alt uses a resolver expression
		  if (p.sub.typ == Node.rslv) return false;  
		  p = p.down;
		}
		return nAlts > 5;
	}
	
	static void CopyFramePart (string stop) {
		char startCh = stop[0];
		int endOfStopString = stop.Length-1;
		int ch = fram.ReadByte();
		while (ch != EOF)
			if (ch == startCh) {
				int i = 0;
				do {
					if (i == endOfStopString) return; // stop[0..i] found
					ch = fram.ReadByte(); i++;
				} while (ch == stop[i]);
				// stop[0..i-1] found; continue with last read character
				gen.Write(stop.Substring(0, i));
			} else {
				gen.Write((char)ch); ch = fram.ReadByte();
			}
		Errors.Exception(" -- incomplete or corrupt parser frame file");
	}

	static void CopySourcePart (Position pos, int indent) {
		// Copy text described by pos from atg to gen
		int ch, nChars, i;
		if (pos != null) {
			Buffer.Pos = pos.beg; ch = Buffer.Read(); nChars = pos.len - 1;
// CHANGES BY M.KRUEGER	(#line pragma generation)
			gen.WriteLine();
			gen.WriteLine(String.Format("#line  {0} \"{1}\" ", Buffer.CountLines(pos.beg) + 1, Buffer.fileName));
// EOC
			Indent(indent);
			while (nChars >= 0) {
				while (ch == CR || ch == LF) {  // eol is either CR or CRLF or LF
					gen.WriteLine(); Indent(indent);
					if (ch == CR) { ch = Buffer.Read(); nChars--; }  // skip CR
					if (ch == LF) { ch = Buffer.Read(); nChars--; }  // skip LF
					for (i = 1; i <= pos.col && ch <= ' '; i++) { 
						// skip blanks at beginning of line
						ch = Buffer.Read(); nChars--;
					}
					if (i <= pos.col) pos.col = i - 1; // heading TABs => not enough blanks
					if (nChars < 0) goto done;
				}
				gen.Write((char)ch);
				ch = Buffer.Read(); nChars--;
			}
			done:
			if (indent > 0) gen.WriteLine();
		}
	}

	static void GenErrorMsg (int errTyp, Symbol sym) {
		errorNr++;
		err.Write("\t\t\tcase " + errorNr + ": s = \"");
		switch (errTyp) {
			case tErr: 
				if (sym.name[0] == '"') err.Write(DFA.Escape(sym.name) + " expected");
				else err.Write(sym.name + " expected"); 
				break;
			case altErr: err.Write("invalid " + sym.name); break;
			case syncErr: err.Write("this symbol not expected in " + sym.name); break;
		}
		err.WriteLine("\"; break;");
	}
	
	static int NewCondSet (BitArray s) {
		for (int i = 1; i < symSet.Count; i++) // skip symSet[0] (reserved for union of SYNC sets)
			if (Sets.Equals(s, (BitArray)symSet[i])) return i;
		symSet.Add(s.Clone());
		return symSet.Count - 1;
	}
	
	static void GenCond (BitArray s, Node p) {
		if (p.typ == Node.rslv) CopySourcePart(p.pos, 0);
		else {
			GenCond(s);
			if (p.typ == Node.alt) {
				// for { ... | IF ... | ... } or [ ... | IF ... | ... ]
				// generate conditions: StartOf(...) || IF 
				Node q = p;
				while (q != null) {
					if (q.sub.typ == Node.rslv) {
						gen.Write(" || "); 
						CopySourcePart(q.sub.pos, 0);
					}
					q = q.down;
				}
			}
		}
	}
		
	static void GenCond (BitArray s) {
		int n = Sets.Elements(s);
		if (n == 0) gen.Write("false"); // should never happen
		else if (n <= maxTerm)
			foreach (Symbol sym in Symbol.terminals) {
				if (s[sym.n]) {
					gen.Write("la.kind == {0}", sym.n);
					--n;
					if (n > 0) gen.Write(" || ");
				}
			}
		else gen.Write("StartOf({0})", NewCondSet(s));
	}
	
	static void PutCaseLabels (BitArray s) {
		foreach (Symbol sym in Symbol.terminals)
			if (s[sym.n]) gen.Write("case {0}: ", sym.n);
	}
	
	static void GenCode (Node p, int indent, BitArray isChecked) {
		Node p2;
		BitArray s1, s2;
		while (p != null) {
			switch (p.typ) {
				case Node.nt: {
					Indent(indent);
					gen.Write(p.sym.name + "(");
					CopySourcePart(p.pos, 0);
					gen.WriteLine(");");
					break;
				}
				case Node.t: {
					Indent(indent);
					// M.Krueger: changed Get() to lexer.NextToken(); 
					if (isChecked[p.sym.n]) gen.WriteLine("lexer.NextToken();");
					else gen.WriteLine("Expect({0});", p.sym.n);
					break;
				}
				case Node.wt: {
					Indent(indent);
					s1 = Tab.Expected(p.next, curSy);
					s1.Or(Tab.allSyncSets);
					gen.WriteLine("ExpectWeak({0}, {1});", p.sym.n, NewCondSet(s1));
					break;
				}
				case Node.any: {
					Indent(indent);
					// M.Krueger: changed Get() to lexer.NextToken(); 
					gen.WriteLine("lexer.NextToken();");
					break;
				}
				case Node.eps: break; // nothing
				case Node.sem: {
					CopySourcePart(p.pos, indent);
					break;
				}
				case Node.sync: {
					Indent(indent);
					GenErrorMsg(syncErr, curSy);
					s1 = (BitArray)p.set.Clone();
					gen.Write("while (!("); GenCond(s1); gen.Write(")) {");
					// M.Krueger: changed Get() to lexer.NextToken(); 
					gen.Write("SynErr({0}); lexer.NextToken(); ", errorNr); gen.WriteLine("}");
					break;
				}
				case Node.alt: {
					s1 = Tab.First(p);
					bool equal = Sets.Equals(s1, isChecked);
					bool useSwitch = UseSwitch(p);
					if (useSwitch) { Indent(indent); gen.WriteLine("switch (la.kind) {"); }
					p2 = p;
					while (p2 != null) {
						s1 = Tab.Expected(p2.sub, curSy, 1);
						Indent(indent);
						if (useSwitch) { PutCaseLabels(s1); gen.WriteLine("{"); }
						else if (p2 == p) { 
							gen.Write("if ("); GenCond(s1, p2.sub); gen.WriteLine(") {"); 
						} else if (p2.down == null && equal) { gen.WriteLine("} else {");
						} else { 
							gen.Write("} else if (");  GenCond(s1, p2.sub); gen.WriteLine(") {"); 
						}
						s1.Or(isChecked);
						if (p2.sub.typ != Node.rslv) GenCode(p2.sub, indent + 1, s1);
						else GenCode(p2.sub.next, indent + 1, s1);
						if (useSwitch) {
							Indent(indent); gen.WriteLine("\tbreak;");
							Indent(indent); gen.WriteLine("}");
						}
						p2 = p2.down;
					}
					Indent(indent);
					if (equal) {
						gen.WriteLine("}");
					} else {
						GenErrorMsg(altErr, curSy);
						if (useSwitch) {
							gen.WriteLine("default: SynErr({0}); break;", errorNr);
							Indent(indent); gen.WriteLine("}");
						} else {
							gen.Write("} "); gen.WriteLine("else SynErr({0});", errorNr);
						}
					}
					break;
				}
				case Node.iter: {
					Indent(indent);
					p2 = p.sub;
					gen.Write("while (");
					if (p2.typ == Node.wt) {
						s1 = Tab.Expected(p2.next, curSy);
						s2 = Tab.Expected(p.next, curSy);
						gen.Write("WeakSeparator({0},{1},{2}) ", p2.sym.n, NewCondSet(s1), NewCondSet(s2));
						s1 = new BitArray(Symbol.terminals.Count);  // for inner structure
						if (p2.up || p2.next == null) p2 = null; else p2 = p2.next;
					} else {
						s1 = Tab.First(p2); 
						GenCond(s1, p2);
					}
					gen.WriteLine(") {");
					GenCode(p2, indent + 1, s1);
					Indent(indent);
					gen.WriteLine("}");
					break;
				}
				case Node.opt:
					if (p.sub.typ != Node.rslv) s1 = Tab.First(p.sub); 
					else s1 = Tab.First(p.sub.next);
					if (!Sets.Equals(isChecked, s1)) {
						Indent(indent);
						gen.Write("if ("); GenCond(s1, p.sub); gen.WriteLine(") {");
						if (p.sub.typ != Node.rslv) GenCode(p.sub, indent + 1, s1);
						else GenCode(p.sub.next, indent + 1, s1);
						Indent(indent); gen.WriteLine("}");
					} else GenCode(p.sub, indent, isChecked);
					break;
			}
			if (p.typ != Node.eps && p.typ != Node.sem && p.typ != Node.sync) 
				isChecked.SetAll(false);  // = new BitArray(Symbol.terminals.Count);
			if (p.up) break;
			p = p.next;
		}
	}
	
	/* ML 2002-09-07 Generates the class "Tokens"                           *
	 * which maps the token number to meaningfully named integer constants, *
	 * as specified in the NAMES section. */
	static void GenTokens() {
		if (Symbol.tokenNames != null && Symbol.tokenNames.Count > 0) {

			gen.WriteLine("public class Tokens {");

			foreach (DictionaryEntry entry in Symbol.tokenNames) {
				string token = entry.Key as string;
				string name = entry.Value as string;
				if (IsCSharpKW(name)) {
					Parser.SemErr(name + " is a C# keyword." + 
					              "Use another name for the token " + token);
					continue;
				}

				Symbol sym = Symbol.Find(token);
				if (sym != null && (sym.typ == Node.t || sym.typ == Node.wt))
					gen.WriteLine("\tpublic const int {0} = {1};", name, sym.n);
			}

			gen.WriteLine("}");			
		}
	}

	/* AW 03-01-20 to generate token name:            *
	 * a C# keyword must not be used as an identifier */
	static bool IsCSharpKW (string name) {
		return Array.BinarySearch(csKeywords, name) >= 0;
	}

	static string[] csKeywords = new string[] {
		"abstract",  "as",       "base",     "bool",       "break",     "byte",     
		"case",      "catch",    "char",     "checked",    "class",     "const",
		"continue",  "decimal",  "default",  "delegate",   "do",        "double",
		"else",      "enum",     "event",    "explicit",   "extern",    "false", 
		"finally",   "fixed",    "float",    "for",        "foreach",   "goto",
		"if",        "implicit", "in",       "int",        "interface", "internal", 
		"is",        "lock",     "long",     "namespace",  "new",       "null",    
		"object",    "operator", "out",      "override",   "params",    "private", 
		"protected", "public",   "readonly", "ref",        "return",    "sbyte", 
		"sealed",    "short",    "sizeof",   "stackalloc", "static",    "string", 
		"struct",    "switch",   "this",     "throw",      "true",      "try", 
		"typeof",    "uint",     "ulong",    "unchecked",  "unsafe",    "ushort", 
		"using",     "virtual",  "void",     "volatile",   "while"
	};

	static void GenCodePragmas() {
		foreach (Symbol sym in Symbol.pragmas) {
			gen.WriteLine("\t\t\t\tif (la.kind == {0}) {{", sym.n);
			CopySourcePart(sym.semPos, 4);
			gen.WriteLine("\t\t\t\t}");
		}
	}

	static void GenProductions() {
		foreach (Symbol sym in Symbol.nonterminals) {
			curSy = sym;
			gen.Write("\tvoid {0}(", sym.name);
			CopySourcePart(sym.attrPos, 0);
			gen.WriteLine(") {");
			CopySourcePart(sym.semPos, 2);
			GenCode(sym.graph, 2, new BitArray(Symbol.terminals.Count));
			gen.WriteLine("\t}"); gen.WriteLine();
		}
	}
	
	static void InitSets() {
		for (int i = 0; i < symSet.Count; i++) {
			BitArray s = (BitArray)symSet[i];
			gen.Write("\t{"); 
			int j = 0;
			foreach (Symbol sym in Symbol.terminals) {
				if (s[sym.n]) gen.Write("T,"); else gen.Write("x,");
				++j;
				if (j%4 == 0) gen.Write(" ");
			}
			if (i == symSet.Count-1) gen.WriteLine("x}"); else gen.WriteLine("x},");
		}
	}
	
	public static void WriteParser () {
		FileStream s;
		symSet.Add(Tab.allSyncSets);
		string fr = srcDir + "Parser.frame";
		if (!File.Exists(fr)) {
			string frameDir = Environment.GetEnvironmentVariable("crframes");
			if (frameDir != null) fr = frameDir.Trim() + "\\Parser.frame";
			if (!File.Exists(fr)) Errors.Exception("-- Cannot find Parser.frame");
		}
		try {
			fram = new FileStream(fr, FileMode.Open, FileAccess.Read, FileShare.Read);
		} catch (IOException) {
			Errors.Exception("-- Cannot open Parser.frame.");
		}
		try {
			string fn = srcDir + "Parser.cs";
			if (File.Exists(fn)) File.Copy(fn, fn.Replace(".cs", ".old.cs"), true);
			s = new FileStream(fn, FileMode.Create);
			gen = new StreamWriter(s);
		} catch (IOException) {
			Errors.Exception("-- Cannot generate parser file");
		}
		err = new StringWriter();
		foreach (Symbol sym in Symbol.terminals) GenErrorMsg(tErr, sym);
		if (usingPos != null) CopySourcePart(usingPos, 0);
		gen.WriteLine();
		CopyFramePart("-->namespace");
		/* AW open namespace, if it exists */
		if (Tab.nsName != null && Tab.nsName.Length > 0) {
			gen.Write("namespace ");
			gen.Write(Tab.nsName);
			gen.Write(" {");
		}
		CopyFramePart("-->tokens"); GenTokens(); /* ML 2002/09/07 write the tokenkinds */
		CopyFramePart("-->constants");
		gen.WriteLine("\tconst int maxT = {0};", Symbol.terminals.Count-1);
		CopyFramePart("-->declarations"); CopySourcePart(Tab.semDeclPos, 0);
		CopyFramePart("-->pragmas"); GenCodePragmas();
		CopyFramePart("-->productions"); GenProductions();
		CopyFramePart("-->parseRoot"); gen.WriteLine("\t\t{0}();", Tab.gramSy.name);
		CopyFramePart("-->errors"); gen.Write(err.ToString());
		CopyFramePart("-->initialization"); InitSets();
		CopyFramePart("$$$");
		/* AW 2002-12-20 close namespace, if it exists */
		if (Tab.nsName != null && Tab.nsName.Length > 0) gen.Write("}");
		gen.Close();
	}
	
	public static void WriteStatistics () {
		Trace.WriteLine();
		Trace.WriteLine("{0} terminals", Symbol.terminals.Count);
		Trace.WriteLine("{0} symbols", Symbol.terminals.Count + Symbol.pragmas.Count +
		                               Symbol.nonterminals.Count);
		Trace.WriteLine("{0} nodes", Node.nodes.Count);
		Trace.WriteLine("{0} sets", symSet.Count);
	}

	public static void Init (string file, string dir) {
		srcName = file;
		srcDir = dir;	
		errorNr = -1;
		usingPos = null;
	}

} // end ParserGen

} // end namespace
