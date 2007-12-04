using System.Collections;
using System.Text;
using System;
using System.Reflection;

namespace at.jku.ssw.Coco {



public class Parser {
	const int maxT = 42;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;
	const string errMsgFormat = "-- line {0} col {1}: {2}";  // 0=line, 1=column, 2=text

	static Token t;                   // last recognized token
	static Token la;                  // lookahead token
	static int errDist = minErrDist;

const int id = 0;
	const int str = 1;
	
	static bool genScanner;

/*-------------------------------------------------------------------------*/



	static void SynErr (int n) {
		if (errDist >= minErrDist) Errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public static void SemErr (string msg) {
		if (errDist >= minErrDist) Errors.Error(t.line, t.col, msg);
		errDist = 0;
	}
	
	static void Get () {
		for (;;) {
			t = la;
			la = Scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }  /* ML return changed to break */
				if (la.kind == 43) {
				Tab.SetDDT(la.val); 
				}

			la = t;
		}
	}
	
	static void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	static bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	static void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}
	
	static bool WeakSeparator (int n, int syFol, int repFol) {
		bool[] s = new bool[maxT+1];
		if (la.kind == n) { Get(); return true; }
		else if (StartOf(repFol)) return false;
		else {
			for (int i=0; i <= maxT; i++) {
				s[i] = set[syFol, i] || set[repFol, i] || set[0, i];
			}
			SynErr(n);
			while (!s[la.kind]) Get();
			return StartOf(syFol);
		}
	}
	
	static void Coco() {
		Symbol sym; Graph g; string gramName; 
		if (la.kind == 40) {
			UsingDecl(out ParserGen.usingPos);
		}
		Expect(6);
		//int gramLine = t.line;
		genScanner = true;
		bool ok = true;
		Tab.ignored = null;
		
		Expect(1);
		gramName = t.val;
		int beg = la.pos; 
		
		while (StartOf(1)) {
			Get();
		}
		Tab.semDeclPos = new Position(beg, la.pos-beg, 0); 
		while (StartOf(2)) {
			Declaration();
		}
		while (!(la.kind == 0 || la.kind == 7)) {SynErr(43); Get();}
		Expect(7);
		if (genScanner) DFA.MakeDeterministic();
		Graph.DeleteNodes();
		
		while (la.kind == 1) {
			Get();
			sym = Symbol.Find(t.val);
			bool undef = sym == null;
			if (undef) sym = new Symbol(Node.nt, t.val, t.line);
			else {
			  if (sym.typ == Node.nt) {
			    if (sym.graph != null) SemErr("name declared twice");
				 } else SemErr("this symbol kind not allowed on left side of production");
				 sym.line = t.line;
			}
			bool noAttrs = sym.attrPos == null;
			sym.attrPos = null;
			
			if (la.kind == 24) {
				AttrDecl(sym);
			}
			if (!undef)
			 if (noAttrs != (sym.attrPos == null))
			   SemErr("attribute mismatch between declaration and use of this symbol");
			
			if (la.kind == 38) {
				SemText(out sym.semPos);
			}
			ExpectWeak(8, 3);
			Expression(out g);
			sym.graph = g.l;
			Graph.Finish(g);
			
			ExpectWeak(9, 4);
		}
		Expect(10);
		Expect(1);
		if (gramName != t.val)
		 SemErr("name does not match grammar name");
		Tab.gramSy = Symbol.Find(gramName);
		if (Tab.gramSy == null)
		  SemErr("missing production for grammar name");
		else {
		  sym = Tab.gramSy;
		  if (sym.attrPos != null)
		    SemErr("grammar symbol must not have attributes");
		}
		Tab.noSym = new Symbol(Node.t, "???", 0); // noSym gets highest number
		Tab.SetupAnys();
		Tab.RenumberPragmas();
		if (Tab.ddt[2]) Node.PrintNodes();
		if (Errors.count == 0) {
		  Console.WriteLine("checking");
		  Tab.CompSymbolSets();
		  ok = ok && Tab.GrammarOk();
		  if (Tab.ddt[7]) Tab.XRef();
		  if (ok) {
		    Console.Write("parser");
		    ParserGen.WriteParser();
		    if (genScanner) {
		      Console.Write(" + scanner");
		      DFA.WriteScanner();
		      if (Tab.ddt[0]) DFA.PrintStates();
		    }
		    Console.WriteLine(" generated");
		    if (Tab.ddt[8]) ParserGen.WriteStatistics();
		  }
		}
		if (Tab.ddt[6]) Tab.PrintSymbolTable();
		
		Expect(9);
	}

	static void UsingDecl(out Position pos) {
		Expect(40);
		int beg = t.pos; 
		while (StartOf(5)) {
			Get();
		}
		Expect(41);
		int end = t.pos; 
		while (la.kind == 40) {
			Get();
			while (StartOf(5)) {
				Get();
			}
			Expect(41);
			end = t.pos; 
		}
		pos = new Position(beg, end - beg + 1, 0); 
	}

	static void Declaration() {
		Graph g1, g2; bool nested = false; 
		switch (la.kind) {
		case 11: {
			Get();
			while (la.kind == 1) {
				SetDecl();
			}
			break;
		}
		case 12: {
			Get();
			while (la.kind == 1 || la.kind == 3 || la.kind == 5) {
				TokenDecl(Node.t);
			}
			break;
		}
		case 13: {
			Get();
			while (la.kind == 1 || la.kind == 3 || la.kind == 5) {
				TokenDecl(Node.pr);
			}
			break;
		}
		case 14: {
			Get();
			Expect(15);
			TokenExpr(out g1);
			Expect(16);
			TokenExpr(out g2);
			if (la.kind == 17) {
				Get();
				nested = true; 
			} else if (StartOf(6)) {
				nested = false; 
			} else SynErr(44);
			new Comment(g1.l, g2.l, nested); 
			break;
		}
		case 18: {
			Get();
			Set(out Tab.ignored);
			Tab.ignored[' '] = true; /* ' ' is always ignored */
			if (Tab.ignored[0]) SemErr("may not ignore \'\\0\'"); 
			break;
		}
		case 19: {
			Get();
			Symbol.tokenNames = new Hashtable(); 
			while (la.kind == 1 || la.kind == 3) {
				if (la.kind == 3) {
					Get();
				} else {
					Get();
				}
				string key = t.val; 
				Expect(8);
				Expect(1);
				string val = t.val; Symbol.tokenNames.Add(key, val); 
			}
			break;
		}
		default: SynErr(45); break;
		}
	}

	static void AttrDecl(Symbol sym) {
		Expect(24);
		int beg = la.pos; int col = la.col; 
		while (StartOf(7)) {
			if (StartOf(8)) {
				Get();
			} else {
				Get();
				SemErr("bad string in semantic action"); 
			}
		}
		Expect(25);
		sym.attrPos = new Position(beg, t.pos - beg, col); 
	}

	static void SemText(out Position pos) {
		Expect(38);
		int beg = la.pos; int col = la.col; 
		while (StartOf(9)) {
			if (StartOf(10)) {
				Get();
			} else if (la.kind == 4) {
				Get();
				SemErr("bad string in semantic action"); 
			} else {
				Get();
				SemErr("missing end of previous semantic action"); 
			}
		}
		Expect(39);
		pos = new Position(beg, t.pos - beg, col); 
	}

	static void Expression(out Graph g) {
		Graph g2; 
		Term(out g);
		bool first = true; 
		while (WeakSeparator(26,11,12) ) {
			Term(out g2);
			if (first) { Graph.MakeFirstAlt(g); first = false; }
			Graph.MakeAlternative(g, g2);
			
		}
	}

	static void SetDecl() {
		BitArray s; 
		Expect(1);
		string name = t.val;
		CharClass c = CharClass.Find(name);
		if (c != null) SemErr("name declared twice");
		
		Expect(8);
		Set(out s);
		if (Sets.Elements(s) == 0) SemErr("character set must not be empty");
		c = new CharClass(name, s);
		
		Expect(9);
	}

	static void TokenDecl(int typ) {
		string name; int kind; Symbol sym; Graph g; 
		Sym(out name, out kind);
		sym = Symbol.Find(name);
		if (sym != null) SemErr("name declared twice");
		else {
		  sym = new Symbol(typ, name, t.line);
		  sym.tokenKind = Symbol.classToken;
		}
		
		while (!(StartOf(13))) {SynErr(46); Get();}
		if (la.kind == 8) {
			Get();
			TokenExpr(out g);
			Expect(9);
			if (kind != id) SemErr("a literal must not be declared with a structure");
			Graph.Finish(g);
			DFA.ConvertToStates(g.l, sym);
			
		} else if (la.kind == 9) {
			Get();
			if (typ != Node.rslv) SemErr("resolver is only allowed in RESOLVERS section"); 
		} else if (StartOf(14)) {
			if (kind == id) genScanner = false;
			else DFA.MatchLiteral(sym);
			
		} else SynErr(47);
		if (la.kind == 38) {
			SemText(out sym.semPos);
			if (typ == Node.t) SemErr("semantic action not allowed here"); 
		} else if (StartOf(15)) {
			if (typ == Node.rslv) SemErr("resolvers must have a semantic action"); 
		} else SynErr(48);
	}

	static void TokenExpr(out Graph g) {
		Graph g2; 
		TokenTerm(out g);
		bool first = true; 
		while (WeakSeparator(26,16,17) ) {
			TokenTerm(out g2);
			if (first) { Graph.MakeFirstAlt(g); first = false; }
			Graph.MakeAlternative(g, g2);
			
		}
	}

	static void Set(out BitArray s) {
		BitArray s2; 
		SimSet(out s);
		while (la.kind == 20 || la.kind == 21) {
			if (la.kind == 20) {
				Get();
				SimSet(out s2);
				s.Or(s2); 
			} else {
				Get();
				SimSet(out s2);
				Sets.Subtract(s, s2); 
			}
		}
	}

	static void SimSet(out BitArray s) {
		int n1, n2; 
		s = new BitArray(CharClass.charSetSize); 
		if (la.kind == 1) {
			Get();
			CharClass c = CharClass.Find(t.val);
			if (c == null) SemErr("undefined name"); else s.Or(c.set);
			
		} else if (la.kind == 3) {
			Get();
			string name = t.val;
			name = DFA.Unescape(name.Substring(1, name.Length-2));
			foreach (char ch in name) s[ch] = true;
			
		} else if (la.kind == 5) {
			Char(out n1);
			s[n1] = true; 
			if (la.kind == 22) {
				Get();
				Char(out n2);
				for (int i = n1; i <= n2; i++) s[i] = true; 
			}
		} else if (la.kind == 23) {
			Get();
			s = new BitArray(CharClass.charSetSize, true);
			s[0] = false;
			
		} else SynErr(49);
	}

	static void Char(out int n) {
		Expect(5);
		string name = t.val;
		name = DFA.Unescape(name.Substring(1, name.Length-2));
		int max = CharClass.charSetSize;
		if (name.Length != 1 || name[0] > max-1) SemErr("unacceptable character value");
		n = name[0] % max;
		
	}

	static void Sym(out string name, out int kind) {
		name = "???"; kind = id; 
		if (la.kind == 1) {
			Get();
			kind = id; name = t.val; 
		} else if (la.kind == 3 || la.kind == 5) {
			if (la.kind == 3) {
				Get();
				name = t.val; 
			} else {
				Get();
				name = "\"" + t.val.Substring(1, t.val.Length-2) + "\""; 
			}
			kind = str; 
		} else SynErr(50);
	}

	static void Term(out Graph g) {
		Graph g2; Position pos; Node rslv = null; 
		g = null;
		
		if (StartOf(18)) {
			if (la.kind == 35) {
				rslv = new Node(Node.rslv, null, la.line); 
				ResolveExpr(out pos);
				rslv.pos = pos;
				g = new Graph(rslv);
				
			}
			Factor(out g2);
			if (rslv != null) Graph.MakeSequence(g, g2);
			else g = g2;
			
			while (StartOf(19)) {
				Factor(out g2);
				Graph.MakeSequence(g, g2); 
			}
		} else if (StartOf(20)) {
			g = new Graph(new Node(Node.eps, null, 0)); 
		} else SynErr(51);
	}

	static void ResolveExpr(out Position pos) {
		Expect(35);
		Expect(28);
		int beg = la.pos; int col = la.col; 
		if (la.kind == 8 || la.kind == 36) {
			if (la.kind == 8) {
				Get();
			} else {
				Get();
			}
			CondPart();
		} else if (la.kind == 28) {
			Get();
			CondPart();
			Expect(29);
		} else if (StartOf(21)) {
			Get();
			CondPart();
		} else SynErr(52);
		pos = new Position(beg, t.pos - beg, col); 
	}

	static void Factor(out Graph g) {
		string name; int kind; Position pos; bool weak = false; 
		g = null;
		
		switch (la.kind) {
		case 1: case 3: case 5: case 27: {
			if (la.kind == 27) {
				Get();
				weak = true; 
			}
			Sym(out name, out kind);
			Symbol sym = Symbol.Find(name);
			bool undef = sym == null;
			if (undef) {
			  if (kind == id)
			    sym = new Symbol(Node.nt, name, 0);  // forward nt
			  else if (genScanner) { 
			    sym = new Symbol(Node.t, name, t.line);
			    DFA.MatchLiteral(sym);
			  } else {  // undefined string in production
			    SemErr("undefined string in production");
			    sym = Tab.eofSy;  // dummy
			  }
			}
			int typ = sym.typ;
			if (typ != Node.t && typ != Node.nt && typ != Node.rslv) /* ML */
			  SemErr("this symbol kind is not allowed in a production");
			if (weak)
			  if (typ == Node.t) typ = Node.wt;
			  else SemErr("only terminals may be weak");
			Node p = new Node(typ, sym, t.line);
			g = new Graph(p);
			
			if (la.kind == 24) {
				Attribs(p);
				if (kind != id) SemErr("a literal must not have attributes"); 
			}
			if (undef)
			 sym.attrPos = p.pos;  // dummy
			else if ((p.pos == null) != (sym.attrPos == null))
			  SemErr("attribute mismatch between declaration and use of this symbol");
			
			break;
		}
		case 28: {
			Get();
			Expression(out g);
			Expect(29);
			break;
		}
		case 30: {
			Get();
			Expression(out g);
			Expect(31);
			Graph.MakeOption(g); 
			break;
		}
		case 32: {
			Get();
			Expression(out g);
			Expect(33);
			Graph.MakeIteration(g); 
			break;
		}
		case 38: {
			SemText(out pos);
			Node p = new Node(Node.sem, null, 0);
			p.pos = pos;
			g = new Graph(p);
			
			break;
		}
		case 23: {
			Get();
			Node p = new Node(Node.any, null, 0);  // p.set is set in Tab.SetupAnys
			g = new Graph(p);
			
			break;
		}
		case 34: {
			Get();
			Node p = new Node(Node.sync, null, 0);
			g = new Graph(p);
			
			break;
		}
		default: SynErr(53); break;
		}
	}

	static void Attribs(Node p) {
		Expect(24);
		int beg = la.pos; int col = la.col; 
		while (StartOf(7)) {
			if (StartOf(8)) {
				Get();
			} else {
				Get();
				SemErr("bad string in attributes"); 
			}
		}
		Expect(25);
		p.pos = new Position(beg, t.pos - beg, col); 
	}

	static void CondPart() {
		while (StartOf(22)) {
			if (la.kind == 28) {
				Get();
				CondPart();
			} else {
				Get();
			}
		}
		Expect(29);
	}

	static void TokenTerm(out Graph g) {
		Graph g2; 
		TokenFactor(out g);
		while (StartOf(16)) {
			TokenFactor(out g2);
			Graph.MakeSequence(g, g2); 
		}
		if (la.kind == 37) {
			Get();
			Expect(28);
			TokenExpr(out g2);
			Graph.SetContextTrans(g2.l); Graph.MakeSequence(g, g2); 
			Expect(29);
		}
	}

	static void TokenFactor(out Graph g) {
		string name; int kind; 
		g = new Graph(); 
		if (la.kind == 1 || la.kind == 3 || la.kind == 5) {
			Sym(out name, out kind);
			if (kind == id) {
			 CharClass c = CharClass.Find(name);
			 if (c == null) {
			   SemErr("undefined name");
			   c = new CharClass(name, new BitArray(CharClass.charSetSize));
			 }
			 Node p = new Node(Node.clas, null, 0); p.val = c.n;
			 g = new Graph(p);
			} else g = Graph.StrToGraph(name);  // str
			
		} else if (la.kind == 28) {
			Get();
			TokenExpr(out g);
			Expect(29);
		} else if (la.kind == 30) {
			Get();
			TokenExpr(out g);
			Expect(31);
			Graph.MakeOption(g); 
		} else if (la.kind == 32) {
			Get();
			TokenExpr(out g);
			Expect(33);
			Graph.MakeIteration(g); 
		} else SynErr(54);
	}



	public static void Parse() {
		Errors.SynErr = new ErrorCodeProc(SynErr);
		la = new Token();
		la.val = "";		
		Get();
		Coco();

	}

	static void SynErr (int line, int col, int n) {
		Errors.count++; 
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "number expected"; break;
			case 3: s = "string expected"; break;
			case 4: s = "badString expected"; break;
			case 5: s = "char expected"; break;
			case 6: s = "\"COMPILER\" expected"; break;
			case 7: s = "\"PRODUCTIONS\" expected"; break;
			case 8: s = "\"=\" expected"; break;
			case 9: s = "\".\" expected"; break;
			case 10: s = "\"END\" expected"; break;
			case 11: s = "\"CHARACTERS\" expected"; break;
			case 12: s = "\"TOKENS\" expected"; break;
			case 13: s = "\"PRAGMAS\" expected"; break;
			case 14: s = "\"COMMENTS\" expected"; break;
			case 15: s = "\"FROM\" expected"; break;
			case 16: s = "\"TO\" expected"; break;
			case 17: s = "\"NESTED\" expected"; break;
			case 18: s = "\"IGNORE\" expected"; break;
			case 19: s = "\"TOKENNAMES\" expected"; break;
			case 20: s = "\"+\" expected"; break;
			case 21: s = "\"-\" expected"; break;
			case 22: s = "\"..\" expected"; break;
			case 23: s = "\"ANY\" expected"; break;
			case 24: s = "\"<\" expected"; break;
			case 25: s = "\">\" expected"; break;
			case 26: s = "\"|\" expected"; break;
			case 27: s = "\"WEAK\" expected"; break;
			case 28: s = "\"(\" expected"; break;
			case 29: s = "\")\" expected"; break;
			case 30: s = "\"[\" expected"; break;
			case 31: s = "\"]\" expected"; break;
			case 32: s = "\"{\" expected"; break;
			case 33: s = "\"}\" expected"; break;
			case 34: s = "\"SYNC\" expected"; break;
			case 35: s = "\"IF\" expected"; break;
			case 36: s = "\"!=\" expected"; break;
			case 37: s = "\"CONTEXT\" expected"; break;
			case 38: s = "\"(.\" expected"; break;
			case 39: s = "\".)\" expected"; break;
			case 40: s = "\"using\" expected"; break;
			case 41: s = "\";\" expected"; break;
			case 42: s = "??? expected"; break;
			case 43: s = "this symbol not expected in Coco"; break;
			case 44: s = "invalid Declaration"; break;
			case 45: s = "invalid Declaration"; break;
			case 46: s = "this symbol not expected in TokenDecl"; break;
			case 47: s = "invalid TokenDecl"; break;
			case 48: s = "invalid TokenDecl"; break;
			case 49: s = "invalid SimSet"; break;
			case 50: s = "invalid Sym"; break;
			case 51: s = "invalid Term"; break;
			case 52: s = "invalid ResolveExpr"; break;
			case 53: s = "invalid Factor"; break;
			case 54: s = "invalid TokenFactor"; break;

			default: s = "error " + n; break;
		}
		Console.WriteLine(errMsgFormat, line, col, s);
	}

	static bool[,] set = {
	{T,T,x,T, x,T,x,T, T,T,x,T, T,T,T,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x},
	{x,T,T,T, T,T,T,x, T,T,T,x, x,x,x,T, T,T,x,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x},
	{x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{T,T,x,T, x,T,x,T, T,T,x,T, T,T,T,x, x,x,T,T, x,x,x,T, x,x,T,T, T,x,T,x, T,x,T,T, x,x,T,x, x,x,x,x},
	{T,T,x,T, x,T,x,T, T,T,T,T, T,T,T,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,x},
	{x,x,x,x, x,x,x,T, x,x,x,T, T,T,T,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x},
	{x,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x},
	{x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x, T,T,T,x},
	{x,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x,x, T,T,T,x},
	{x,T,x,T, x,T,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, x,x,T,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x},
	{T,T,x,T, x,T,x,T, T,T,x,T, T,T,T,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x},
	{x,T,x,T, x,T,x,T, x,x,x,T, T,T,T,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x},
	{x,T,x,T, x,T,x,T, x,x,x,T, T,T,T,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,T, x,T,x,T, T,T,T,x, T,T,T,T, x,x,x,x, x,x,x,x, x,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x},
	{x,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, T,x,T,x, T,x,T,T, x,x,T,x, x,x,x,x},
	{x,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, T,x,T,x, T,x,T,x, x,x,T,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,T,x,T, x,T,x,x, x,x,x,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, x,T,T,T, T,T,T,x},
	{x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,x}

	};
} // end Parser

}
