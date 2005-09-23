/* ------------------------------------------------------------------------
 * Tab.cs
 * Symbol table management of Coco/R
 * by H.Moessenboeck, Univ. of Linz
 * ------------------------------------------------------------------------*/
using System;
using System.IO;
using System.Collections;

namespace at.jku.ssw.Coco {

public class Position {  // position of source code stretch (e.g. semantic action, resolver expressions)
	public int beg;      // start relative to the beginning of the file
	public int len;      // length of stretch
	public int col;      // column number of start position
	
	public Position(int beg, int len, int col) {
		this.beg = beg; this.len = len; this.col = col;
	}
}


//---------------------------------------------------------------------
// Symbols
//---------------------------------------------------------------------
	
public class Symbol : IComparable {
	public static ArrayList terminals = new ArrayList();
	public static ArrayList pragmas = new ArrayList();
	public static ArrayList nonterminals = new ArrayList();
	public static Hashtable tokenNames = null;  /* AW 2003-03-25 */
	
	public const int classToken    = 0;		// token kinds
	public const int litToken      = 1;
	public const int classLitToken = 2;
	
	public int      n;           // symbol number
	public int      typ;         // t, nt, pr, unknown, rslv /* ML 29_11_2002 slv added */ /* AW slv --> rslv */
	public string   name;        // symbol name
	public Node     graph;       // nt: to first node of syntax graph
	public int      tokenKind;   // t:  token kind (literal, class, ...)
	public bool     deletable;   // nt: true if nonterminal is deletable
	public bool     firstReady;  // nt: true if terminal start symbols have already been computed
	public BitArray first;       // nt: terminal start symbols
	public BitArray follow;      // nt: terminal followers
	public BitArray nts;         // nt: nonterminals whose followers have to be added to this sym
	public int      line;        // source text line number of item in this node
	public Position attrPos;     // nt: position of attributes in source text (or null)
	public Position semPos;      // pr: pos of semantic action in source text (or null)
	                             // nt: pos of local declarations in source text (or null)
	public override string ToString()
	{
		return String.Format("[Symbol:Name={0}, n={1}]", name, n);
	}
	public Symbol(int typ, string name, int line) {
		if (name.Length == 2 && name[0] == '"') {
			Parser.SemErr("empty token not allowed"); name = "???";
		}
		if (name.IndexOf(' ') >= 0) Parser.SemErr("tokens must not contain blanks");
		this.typ = typ; this.name = name; this.line = line;
		switch (typ) {
			case Node.t:  n = terminals.Count; terminals.Add(this); break;
			case Node.pr: pragmas.Add(this); break;
			case Node.nt: n = nonterminals.Count; nonterminals.Add(this); break;
		}
	}
	
	public static Symbol Find(string name) {
		foreach (Symbol s in terminals)
			if (s.name == name) return s;
		foreach (Symbol s in nonterminals)
			if (s.name == name) return s;
		return null;
	}
	
	public int CompareTo(object x) {
		return name.CompareTo(((Symbol)x).name);
	}
	
}


//---------------------------------------------------------------------
// Syntax graph (class Node, class Graph)
//---------------------------------------------------------------------

public class Node {
	public static ArrayList nodes = new ArrayList();
	public static string[] nTyp =
		{"    ", "t   ", "pr  ", "nt  ", "clas", "chr ", "wt  ", "any ", "eps ",  /* AW 03-01-14 nTyp[0]: " " --> "    " */
		 "sync", "sem ", "alt ", "iter", "opt ", "rslv"};
	
	// constants for node kinds
	public const int t    =  1;  // terminal symbol
	public const int pr   =  2;  // pragma
	public const int nt   =  3;  // nonterminal symbol
	public const int clas =  4;  // character class
	public const int chr  =  5;  // character
	public const int wt   =  6;  // weak terminal symbol
	public const int any  =  7;  // 
	public const int eps  =  8;  // empty
	public const int sync =  9;  // synchronization symbol
	public const int sem  = 10;  // semantic action: (. .)
	public const int alt  = 11;  // alternative: |
	public const int iter = 12;  // iteration: { }
	public const int opt  = 13;  // option: [ ]
	public const int rslv = 14;  // resolver expr  /* ML */ /* AW 03-01-13 renamed slv --> rslv */
	
	public const int normalTrans  = 0;		// transition codes
	public const int contextTrans = 1;
	
	public int      n;			// node number
	public int      typ;		// t, nt, wt, chr, clas, any, eps, sem, sync, alt, iter, opt, rslv
	public Node     next;		// to successor node
	public Node     down;		// alt: to next alternative
	public Node     sub;		// alt, iter, opt: to first node of substructure
	public bool     up;			// true: "next" leads to successor in enclosing structure
	public Symbol   sym;		// nt, t, wt: symbol represented by this node
	public int      val;		// chr:  ordinal character value
													// clas: index of character class
	public int      code;		// chr, clas: transition code
	public BitArray set;		// any, sync: the set represented by this node
	public Position pos;		// nt, t, wt: pos of actual attributes
													// sem:       pos of semantic action in source text
	public int      line;		// source text line number of item in this node
	public State    state;	// DFA state corresponding to this node
													// (only used in DFA.ConvertToStates)

	public Node(int typ, Symbol sym, int line) {
		this.typ = typ; this.sym = sym; this.line = line;
		n = nodes.Count;
		nodes.Add(this);
	}
	
	public Node(int typ, Node sub): this(typ, null, 0) {
		this.sub = sub;
	}
	
	public Node(int typ, int val, int line): this(typ, null, line) {
		this.val = val;
	}
	
	public static bool DelGraph(Node p) {
		return p == null || DelNode(p) && DelGraph(p.next);
	}
	
	public static bool DelAlt(Node p) {
		return p == null || DelNode(p) && (p.up || DelAlt(p.next));
	}
	
	public static bool DelNode(Node p) {
		if (p.typ == nt) return p.sym.deletable;
		else if (p.typ == alt) return DelAlt(p.sub) || p.down != null && DelAlt(p.down);
		else return p.typ == eps || p.typ == iter || p.typ == opt || p.typ == sem || p.typ == sync;
	}
	
	//----------------- for printing ----------------------
	
	static int Ptr(Node p, bool up) {
		if (p == null) return 0; 
		else if (up) return -p.n;
		else return p.n;
	}
	
	static string Pos(Position pos) {
		if (pos == null) return "     "; else return String.Format("{0,5}", pos.beg);
	}
	
	public static string Name(string name) {
		return (name + "           ").Substring(0, 12);
		/* isn't this better (less string allocations, easier to understand): *
		 * return (name.Length > 12) ? name.Substring(0,12) : name;           */
	}
	
	public static void PrintNodes() {
		Trace.WriteLine("Graph nodes:");
		Trace.WriteLine("----------------------------------------------------");
		Trace.WriteLine("   n type name          next  down   sub   pos  line");
		Trace.WriteLine("                               val  code");
		Trace.WriteLine("----------------------------------------------------");
		foreach (Node p in nodes) {
			Trace.Write("{0,4} {1} ", p.n, nTyp[p.typ]);
			if (p.sym != null)
				Trace.Write("{0,12} ", Name(p.sym.name));
			else if (p.typ == Node.clas) {
				CharClass c = (CharClass)CharClass.classes[p.val];
				Trace.Write("{0,12} ", Name(c.name));
			} else Trace.Write("             ");
			Trace.Write("{0,5} ", Ptr(p.next, p.up));
			switch (p.typ) {
				case t: case nt: case wt:
					Trace.Write("             {0,5}", Pos(p.pos)); break;
				case chr:
					Trace.Write("{0,5} {1,5}       ", p.val, p.code); break;
				case clas:
					Trace.Write("      {0,5}       ", p.code); break;
				case alt: case iter: case opt:
					Trace.Write("{0,5} {1,5}       ", Ptr(p.down, false), Ptr(p.sub, false)); break;
				case sem:
					Trace.Write("             {0,5}", Pos(p.pos)); break;
				case eps: case any: case sync:
					Trace.Write("                  "); break;
			}
			Trace.WriteLine("{0,5}", p.line);
		}
		Trace.WriteLine();
	}
	
}


public class Graph {
	static Node dummyNode = new Node(Node.eps, null, 0);
	
	public Node l;	// left end of graph = head
	public Node r;	// right end of graph = list of nodes to be linked to successor graph
	
	public Graph() {
		l = null; r = null;
	}
	
	public Graph(Node left, Node right) {
		l = left; r = right;
	}
	
	public Graph(Node p) {
		l = p; r = p;
	}

	public static void MakeFirstAlt(Graph g) {
		g.l = new Node(Node.alt, g.l); g.l.line = g.l.sub.line; /* AW 2002-03-07 make line available for error handling */
		g.l.next = g.r;
		g.r = g.l;
	}
	
	public static void MakeAlternative(Graph g1, Graph g2) {
		g2.l = new Node(Node.alt, g2.l); g2.l.line = g2.l.sub.line;
		Node p = g1.l; while (p.down != null) p = p.down;
		p.down = g2.l;
		p = g1.r; while (p.next != null) p = p.next;
		p.next = g2.r;
	}
	
	public static void MakeSequence(Graph g1, Graph g2) {
		Node p = g1.r.next; g1.r.next = g2.l; // link head node
		while (p != null) {  // link substructure
			Node q = p.next; p.next = g2.l; p.up = true;
			p = q;
		}
		g1.r = g2.r;
	}
	
	public static void MakeIteration(Graph g) {
		g.l = new Node(Node.iter, g.l);
		Node p = g.r;
		g.r = g.l;
		while (p != null) {
			Node q = p.next; p.next = g.l; p.up = true;
			p = q;
		}
	}
	
	public static void MakeOption(Graph g) {
		g.l = new Node(Node.opt, g.l);
		g.l.next = g.r;
		g.r = g.l;
	}
	
	public static void Finish(Graph g) {
		Node p = g.r;
		while (p != null) {
			Node q = p.next; p.next = null; p = q;
		}
	}
	
  public static void SetContextTrans(Node p) { // set transition code in the graph rooted at p
    DFA.hasCtxMoves = true;
    while (p != null) {
      if (p.typ == Node.chr || p.typ == Node.clas) {
        p.code = Node.contextTrans;
      } else if (p.typ == Node.opt || p.typ == Node.iter) {
        SetContextTrans(p.sub);
      } else if (p.typ == Node.alt) {
        SetContextTrans(p.sub); SetContextTrans(p.down);
      }
      if (p.up) break;
      p = p.next;
    }
  }
	
	public static void DeleteNodes() {
		Node.nodes = new ArrayList();
		dummyNode = new Node(Node.eps, null, 0);
	}
	
	public static Graph StrToGraph(string str) {
		string s = DFA.Unescape(str.Substring(1, str.Length-2));
		if (s.IndexOf('\0') >= 0) Parser.SemErr("\\0 not allowed here. Used as eof character");
		if (s.Length == 0) Parser.SemErr("empty token not allowed");
		Graph g = new Graph();
		g.r = dummyNode;
		for (int i = 0; i < s.Length; i++) {
			Node p = new Node(Node.chr, (int)s[i], 0);
			g.r.next = p; g.r = p;
		}
		g.l = dummyNode.next; dummyNode.next = null;
		return g;
	}
	
}


//----------------------------------------------------------------
// Bit sets 
//----------------------------------------------------------------

public class Sets {
	
	public static int First(BitArray s) {
		int max = s.Count;
		for (int i=0; i<max; i++)
			if (s[i]) return i;
		return -1;
	}
	
	public static int Elements(BitArray s) {
		int max = s.Count;
		int n = 0;
		for (int i=0; i<max; i++)
			if (s[i]) n++;
		return n;
	}
	
	public static bool Equals(BitArray a, BitArray b) {
		int max = a.Count;
		for (int i=0; i<max; i++)
			if (a[i] != b[i]) return false;
		return true;
	}
	
	public static bool Includes(BitArray a, BitArray b) {	// a > b ?
		int max = a.Count;
		for (int i=0; i<max; i++)
			if (b[i] && ! a[i]) return false;
		return true;
	}
	
	public static bool Intersect(BitArray a, BitArray b) { // a * b != {}
		int max = a.Count;
		for (int i=0; i<max; i++)
			if (a[i] && b[i]) return true;
		return false;
	}
	
	public static void Subtract(BitArray a, BitArray b) { // a = a - b
		BitArray c = (BitArray) b.Clone();
		a.And(c.Not());
	}
	
	public static void PrintSet(BitArray s, int indent) {
		int col, len;
		col = indent;
		foreach (Symbol sym in Symbol.terminals) {
			if (s[sym.n]) {
				len = sym.name.Length;
				if (col + len >= 80) {
					Trace.WriteLine();
					for (col = 1; col < indent; col++) Trace.Write(" ");
				}
				Trace.Write("{0} ", sym.name);
				col += len + 1;
			}
		}
		if (col == indent) Trace.Write("-- empty set --");
		Trace.WriteLine();
	}
	
}


//---------------------------------------------------------------------
// Character class management
//---------------------------------------------------------------------

public class CharClass {
	public static ArrayList classes = new ArrayList();
	public static int dummyName = 'A';
	
	public const int charSetSize = 256;  // must be a multiple of 16
	
	public int n;       	// class number
	public string name;		// class name
	public BitArray set;	// set representing the class

	public CharClass(string name, BitArray s) {
		if (name == "#") name = "#" + (char)dummyName++;
		this.n = classes.Count; this.name = name; this.set = s;
		classes.Add(this);
	}
	
	public static CharClass Find(string name) {
		foreach (CharClass c in classes)
			if (c.name == name) return c;
		return null;
	}
	
	public static CharClass Find(BitArray s) {
		foreach (CharClass c in classes)
			if (Sets.Equals(s, c.set)) return c;
		return null;
	}
	
	public static BitArray Set(int i) {
		return ((CharClass)classes[i]).set;
	}
	
	static string Ch(int ch) {
		if (ch < ' ' || ch >= 127 || ch == '\'' || ch == '\\') return ch.ToString();
		else return String.Format("'{0}'", (char)ch);
	}
	
	static void WriteCharSet(BitArray s) {
			int i = 0, len = s.Count;
			while (i < len) {
				while (i < len && !s[i]) i++;
				if (i == len) break;
				int j = i;
				while (i < len && s[i]) i++;
				if (j < i-1) Trace.Write("{0}..{1} ", Ch(j), Ch(i-1)); 
				else Trace.Write("{0} ", Ch(j));
			}
	}
	
	public static void WriteClasses () {
		foreach (CharClass c in classes) {
			Trace.Write("{0,-10}: ", c.name);
			WriteCharSet(c.set);
			Trace.WriteLine();
		}
		Trace.WriteLine();
	}
}


//-----------------------------------------------------------
// Symbol table management routines
//-----------------------------------------------------------

public class Tab {
	public static Position semDeclPos;	// position of global semantic declarations
	public static BitArray ignored;			// characters ignored by the scanner
	public static bool[] ddt = new bool[10];	// debug and test switches
	public static Symbol gramSy;				// root nonterminal; filled by ATG
	public static Symbol eofSy;					// end of file symbol
	public static Symbol noSym;					// used in case of an error
	public static BitArray allSyncSets;	// union of all synchronisation sets
	public static string nsName;        // namespace for generated files
	
	static BitArray visited;						// mark list for graph traversals
	static Symbol curSy;								// current symbol in computation of sets
	
	//---------------------------------------------------------------------
	//  Symbol set computations
	//---------------------------------------------------------------------

	/* Computes the first set for the given Node. */
	static BitArray First0(Node p, BitArray mark) {
		BitArray fs = new BitArray(Symbol.terminals.Count);
		while (p != null && !mark[p.n]) {
			mark[p.n] = true;
			switch (p.typ) {
				case Node.nt: {
					if (p.sym.firstReady) fs.Or(p.sym.first);
					else fs.Or(First0(p.sym.graph, mark));
					break;
				}
				case Node.t: case Node.wt: {
					fs[p.sym.n] = true; break;
				}
				case Node.any: {
					fs.Or(p.set); break;
				}
				case Node.alt: {
					fs.Or(First0(p.sub, mark));
					fs.Or(First0(p.down, mark));
					break;
				}
				case Node.iter: case Node.opt: {
					fs.Or(First0(p.sub, mark));
					break;
				}
			}
			if (!Node.DelNode(p)) break;
			p = p.next;
		}
		return fs;
	}
	
	/// <returns>
	/// BitArray which contains the first tokens.
	/// </returns>
	public static BitArray First(Node p) {
		BitArray fs = First0(p, new BitArray(Node.nodes.Count));
		if (ddt[3]) {
			Trace.WriteLine(); 
			if (p != null) Trace.WriteLine("First: node = {0}", p.n);
			else Trace.WriteLine("First: node = null");
			Sets.PrintSet(fs, 0);
		}
		return fs;
	}

	
	static void CompFirstSets() {
		foreach (Symbol sym in Symbol.nonterminals) {
			sym.first = new BitArray(Symbol.terminals.Count);
			sym.firstReady = false;
		}
		foreach (Symbol sym in Symbol.nonterminals) {
			sym.first = First(sym.graph);
			sym.firstReady = true;
		}
	}
	
	static void CompFollow(Node p) {
		while (p != null && !visited[p.n]) {
			visited[p.n] = true;
			if (p.typ == Node.nt) {
				BitArray s = First(p.next);
				p.sym.follow.Or(s);
				if (Node.DelGraph(p.next))
					p.sym.nts[curSy.n] = true;
			} else if (p.typ == Node.opt || p.typ == Node.iter) {
				CompFollow(p.sub);
			} else if (p.typ == Node.alt) {
				CompFollow(p.sub); CompFollow(p.down);
			}
			p = p.next;
		}
	}
	
	static void Complete(Symbol sym) {
		if (!visited[sym.n]) {
			visited[sym.n] = true;
			foreach (Symbol s in Symbol.nonterminals) {
				if (sym.nts[s.n]) {
					Complete(s);
					sym.follow.Or(s.follow);
					if (sym == curSy) sym.nts[s.n] = false;
				}
			}
		}
	}
	
	static void CompFollowSets() {
		foreach (Symbol sym in Symbol.nonterminals) {
			sym.follow = new BitArray(Symbol.terminals.Count);
			sym.nts = new BitArray(Symbol.nonterminals.Count);
		}
		visited = new BitArray(Node.nodes.Count);
		foreach (Symbol sym in Symbol.nonterminals) { // get direct successors of nonterminals
			curSy = sym;
			CompFollow(sym.graph);
		}
		foreach (Symbol sym in Symbol.nonterminals) { // add indirect successors to followers
			visited = new BitArray(Symbol.nonterminals.Count);
			curSy = sym;
			Complete(sym);
		}
	}
	
	static Node LeadingAny(Node p) {
		if (p == null) return null;
		Node a = null;
		if (p.typ == Node.any) a = p;
		else if (p.typ == Node.alt) {
			a = LeadingAny(p.sub);
			if (a == null) a = LeadingAny(p.down);
		}
		else if (p.typ == Node.opt || p.typ == Node.iter) a = LeadingAny(p.sub);
		else if (Node.DelNode(p) && !p.up) a = LeadingAny(p.next);
		return a;
	}
	
	static void FindAS(Node p) { // find ANY sets
		Node a;
		while (p != null) {
			if (p.typ == Node.opt || p.typ == Node.iter) {
				FindAS(p.sub);
				a = LeadingAny(p.sub);
				if (a != null) Sets.Subtract(a.set, First(p.next));
			} else if (p.typ == Node.alt) {
				BitArray s1 = new BitArray(Symbol.terminals.Count);
				Node q = p;
				while (q != null) {
					FindAS(q.sub);
					a = LeadingAny(q.sub);
					if (a != null)
						Sets.Subtract(a.set, First(q.down).Or(s1));
					else
						s1.Or(First(q.sub));
					q = q.down;
				}
			}
			if (p.up) break;
			p = p.next;
		}
	}
	
	static void CompAnySets() {
		foreach (Symbol sym in Symbol.nonterminals) FindAS(sym.graph);
	}
	
	public static BitArray Expected(Node p, Symbol curSy) {
		BitArray s = First(p);
		if (Node.DelGraph(p)) s.Or(curSy.follow);
		return s;
	}

	public static BitArray Expected(Node p, Symbol curSy, int outmost) {
		BitArray s = First(p);
		if (Node.DelGraph(p)) s.Or(curSy.follow);
		return s;
	}
	
	static void CompSync(Node p) {
		while (p != null && !visited[p.n]) {
			visited[p.n] = true;
			if (p.typ == Node.sync) {
				BitArray s = Expected(p.next, curSy);
				s[eofSy.n] = true;
				allSyncSets.Or(s);
				p.set = s;
			} else if (p.typ == Node.alt) {
				CompSync(p.sub); CompSync(p.down);
			} else if (p.typ == Node.opt || p.typ == Node.iter)
				CompSync(p.sub);
			p = p.next;
		}
	}
	
	static void CompSyncSets() {
		allSyncSets = new BitArray(Symbol.terminals.Count);
		allSyncSets[eofSy.n] = true;
		visited = new BitArray(Node.nodes.Count);
		foreach (Symbol sym in Symbol.nonterminals) {
			curSy = sym;
			CompSync(curSy.graph);
		}
	}
	
	public static void SetupAnys() {
		foreach (Node p in Node.nodes)
			if (p.typ == Node.any) {
				p.set = new BitArray(Symbol.terminals.Count, true);
				p.set[eofSy.n] = false;
			}
	}
	
	public static void CompDeletableSymbols() {
		bool changed;
		do {
			changed = false;
			foreach (Symbol sym in Symbol.nonterminals)
				if (!sym.deletable && sym.graph != null && Node.DelGraph(sym.graph)) {
					sym.deletable = true; changed = true;
				}
		} while (changed);
		foreach (Symbol sym in Symbol.nonterminals)
			if (sym.deletable) Console.WriteLine("  {0} deletable", sym.name);
	}
	
	public static void RenumberPragmas() {
		int n = Symbol.terminals.Count;
		foreach (Symbol sym in Symbol.pragmas) sym.n = n++;
	}

	public static void CompSymbolSets() {
		CompDeletableSymbols();
		CompFirstSets();
		CompFollowSets();
		CompAnySets();
		CompSyncSets();
		if (ddt[1]) {
			Trace.WriteLine();
			Trace.WriteLine("First & follow symbols:");
			Trace.WriteLine("----------------------"); Trace.WriteLine();
			foreach (Symbol sym in Symbol.nonterminals) {
				Trace.WriteLine(sym.name);
				Trace.Write("first:   "); Sets.PrintSet(sym.first, 10);
				Trace.Write("follow:  "); Sets.PrintSet(sym.follow, 10);
				Trace.WriteLine();
			}
		}
		if (ddt[4]) {
			Trace.WriteLine();
			Trace.WriteLine("ANY and SYNC sets:");
			Trace.WriteLine("-----------------");
			foreach (Node p in Node.nodes)
				if (p.typ == Node.any || p.typ == Node.sync) {
					Trace.Write("{0,4} {1,4}: ", p.n, Node.nTyp[p.typ]);
					Sets.PrintSet(p.set, 11);
				}
		}
	}
	
	//---------------------------------------------------------------------
	//  Grammar checks
	//---------------------------------------------------------------------
	
	public static bool GrammarOk() {
		bool ok = NtsComplete() 
			&& AllNtReached() 
			&& NoCircularProductions()
			&& AllNtToTerm();
    if (ok) CheckLL1();
    return ok;
	}

	//--------------- check for circular productions ----------------------
	
	class CNode {	// node of list for finding circular productions
		public Symbol left, right;
	
		public CNode (Symbol l, Symbol r) {
			left = l; right = r;
		}
	}

	static void GetSingles(Node p, ArrayList singles) {
		if (p == null) return;  // end of graph
		if (p.typ == Node.nt) {
			if (p.up || Node.DelGraph(p.next)) singles.Add(p.sym);
		} else if (p.typ == Node.alt || p.typ == Node.iter || p.typ == Node.opt) {
			if (p.up || Node.DelGraph(p.next)) {
				GetSingles(p.sub, singles);
				if (p.typ == Node.alt) GetSingles(p.down, singles);
			}
		}
		if (!p.up && Node.DelNode(p)) GetSingles(p.next, singles);
	}
	
	public static bool NoCircularProductions() {
		bool ok, changed, onLeftSide, onRightSide;
		ArrayList list = new ArrayList();
		foreach (Symbol sym in Symbol.nonterminals) {
			ArrayList singles = new ArrayList();
			GetSingles(sym.graph, singles); // get nonterminals s such that sym-->s
			foreach (Symbol s in singles) list.Add(new CNode(sym, s));
		}
		do {
			changed = false;
			for (int i = 0; i < list.Count; i++) {
				CNode n = (CNode)list[i];
				onLeftSide = false; onRightSide = false;
				foreach (CNode m in list) {
					if (n.left == m.right) onRightSide = true;
					if (n.right == m.left) onLeftSide = true;
				}
				if (!onLeftSide || !onRightSide) {
					list.Remove(n); i--; changed = true;
				}
			}
		} while(changed);
		ok = true;
		foreach (CNode n in list) {
			ok = false; Errors.count++;
			Console.WriteLine("  {0} --> {1}", n.left.name, n.right.name);
		}
		return ok;
	}
	
	//--------------- check for LL(1) errors ----------------------
	
	static void LL1Error(int cond, Symbol sym) {
		Console.Write("  LL1 warning in {0}: ", curSy.name);
		if (sym != null) Console.Write("{0} is ", sym.name);
		switch (cond) {
			case 1: Console.WriteLine(" start of several alternatives"); break;
			case 2: Console.WriteLine(" start & successor of deletable structure"); break;
			case 3: Console.WriteLine(" an ANY node that matches no symbol"); break;
		}
	}
	
	static void CheckOverlap(BitArray s1, BitArray s2, int cond) {
		foreach (Symbol sym in Symbol.terminals) {
			if (s1[sym.n] && s2[sym.n]) LL1Error(cond, sym);
		}
	}
	
	static void CheckAlts(Node p) {
		BitArray s1, s2;
		while (p != null) {
			if (p.typ == Node.alt) {
				Node q = p;
				s1 = new BitArray(Symbol.terminals.Count);
				while (q != null) { // for all alternatives
					s2 = Expected(q.sub, curSy);
					CheckOverlap(s1, s2, 1);
					s1.Or(s2);
					CheckAlts(q.sub);
					q = q.down;
				}
			} else if (p.typ == Node.opt || p.typ == Node.iter) {
				s1 = Expected(p.sub, curSy);
				s2 = Expected(p.next, curSy);
				CheckOverlap(s1, s2, 2);
				CheckAlts(p.sub);
			} else if (p.typ == Node.any) {
				if (Sets.Elements(p.set) == 0) LL1Error(3, null);
				// e.g. {ANY} ANY or [ANY] ANY
			}
			if (p.up) break;
			p = p.next;
		}
	}

	static void RSlvError(string msg) {
		Console.WriteLine(msg);
	}


	static void CheckResolver(Node p) {
		while (p != null) {
			// check subnodes of current node p
			if ((p.typ == Node.alt || p.typ == Node.iter || p.typ == Node.opt) &&
			    !p.sub.up)
				if (p.sub.typ == Node.rslv) CheckResolver(p.sub.next);
				else CheckResolver(p.sub);
			
			// check current node p
			switch (p.typ) {
				case Node.alt:
					BitArray uncovered = new BitArray(Symbol.terminals.Count);  // first symbols of alternatives without a resolver (not "covered" by a resolver)
					ArrayList coveredList = new ArrayList();  // list of follow symbols of each resolver (these are "covered" by the resolver)
					ArrayList rslvList = new ArrayList();

					// build set of uncovered first symbols & check for misplaced resolvers 
					// (= not at the first n-1 of n conflicting alternatives)
					Node q = p;
					while (q != null) {
						BitArray curCovered;
						if (q.sub.typ == Node.rslv) {
						  // get followers of resolver (these are "covered" by it)
							if (q.sub.next == null) curCovered = curSy.follow;
							else curCovered = First0(q.sub.next, new BitArray(Node.nodes.Count));
							coveredList.Add(curCovered);
							rslvList.Add(q.sub);
							// resolver must "cover" all but the last occurrence of a conflicting symbol
							if (Sets.Intersect(uncovered, curCovered))
								RSlvError("Misplaced resolver at line " + q.sub.line + " will never be evaluated. " +
								          "Place resolver at previous conflicting alternative.");
						} else uncovered.Or(First0(q.sub, new BitArray(Node.nodes.Count)));
						q = q.down;
					}

					// check for obsolete resolvers 
					// (= alternatives starting with resolvers, when there is no conflict)
					BitArray[] covered = (BitArray[]) coveredList.ToArray(typeof(BitArray));
					Node[] rslvs = (Node[]) rslvList.ToArray(typeof(Node));
					for (int i = 0; i < rslvs.Length; ++i) {
						if (!Sets.Intersect(uncovered, covered[i])) 
							RSlvError("Obsolete resolver at line " + rslvs[i].line + ". " +
							          "Neither of the start symbols of the alternative occurs without a resolver.");
						/*
						if (!Sets.Includes(uncovered, covered[i]))
							RSlvError("At least one of the symbols after the resolver at line " + rslvs[i].line + 
							          " does not appear without a resolver. Remove the last resolver covering this symbol.");
						*/
						/*
						if (Sets.Equals(, covered[i]))
							RSlvError("Resolver at line " + rslvArr[i].line + " covers more symbols than necessary.\n" +
							          "Place resolvers only in front of conflicting symbols.");
						*/
					}
					break;
				case Node.iter: case Node.opt:
					if (p.sub.typ == Node.rslv) {
						BitArray fs = First0(p.sub.next, new BitArray(Node.nodes.Count));
						BitArray fsNext;
						if (p.next == null) fsNext = curSy.follow;
						else fsNext = First0(p.next, new BitArray(Node.nodes.Count));
						if (!Sets.Intersect(fs, fsNext)) 
							RSlvError("Obsolete resolver expression (IF ...) at line " + p.sub.line);
					}
					break;
				case Node.rslv:
					RSlvError("Unexpected Resolver in line " + p.line + ". Will cause parsing error.");
					break;
			}
			if (p.up) break; 
			p = p.next;
		}
	}

	public static void CheckLL1() {
		foreach (Symbol sym in Symbol.nonterminals) {
			curSy = sym;
			CheckAlts(curSy.graph);
			CheckResolver(curSy.graph);
		}
	}
	
	//------------- check if every nts has a production --------------------
	
	public static bool NtsComplete() {
		bool complete = true;
		foreach (Symbol sym in Symbol.nonterminals) {
			if (sym.graph == null) {
				complete = false; Errors.count++;
				Console.WriteLine("  No production for {0}", sym.name);
			}
		}
		return complete;
	}
	
	//-------------- check if every nts can be reached  -----------------
	
	static void MarkReachedNts(Node p) {
		while (p != null) {
			if (p.typ == Node.nt && !visited[p.sym.n]) { // new nt reached
				visited[p.sym.n] = true;
				MarkReachedNts(p.sym.graph);
			} else if (p.typ == Node.alt || p.typ == Node.iter || p.typ == Node.opt) {
				MarkReachedNts(p.sub);
				if (p.typ == Node.alt) MarkReachedNts(p.down);
			}
			if (p.up) break;
			p = p.next;
		}
	}
	
	public static bool AllNtReached() {
		bool ok = true;
		visited = new BitArray(Symbol.nonterminals.Count);
		visited[gramSy.n] = true;
		MarkReachedNts(gramSy.graph);
		foreach (Symbol sym in Symbol.nonterminals) {
			if (!visited[sym.n]) {
				ok = false; Errors.count++;
				Console.WriteLine("  {0} cannot be reached", sym.name);
			}
		}
		return ok;
	}
	
	//--------- check if every nts can be derived to terminals  ------------
	
	static bool IsTerm(Node p, BitArray mark) { // true if graph can be derived to terminals
		while (p != null) {
			if (p.typ == Node.nt && !mark[p.sym.n]) return false;
			if (p.typ == Node.alt && !IsTerm(p.sub, mark) 
			&& (p.down == null || !IsTerm(p.down, mark))) return false;
			if (p.up) break;
			p = p.next;
		}
		return true;
	}
	
	public static bool AllNtToTerm() {
		bool changed, ok = true;
		BitArray mark = new BitArray(Symbol.nonterminals.Count);
		// a nonterminal is marked if it can be derived to terminal symbols
		do {
			changed = false;
			foreach (Symbol sym in Symbol.nonterminals)
				if (!mark[sym.n] && IsTerm(sym.graph, mark)) {
					mark[sym.n] = true; changed = true;
				}
		} while (changed);
		foreach (Symbol sym in Symbol.nonterminals)
			if (!mark[sym.n]) {
				ok = false; Errors.count++;
				Console.WriteLine("  {0} cannot be derived to terminals", sym.name);
			}
		return ok;
	}
	
	/*---------------------------------------------------------------------
	  Utility functions
	---------------------------------------------------------------------*/
	
	static int Num(Node p) {
		if (p == null) return 0; else return p.n;
	}
	
	static void PrintSym(Symbol sym) {
		Trace.Write("{0,3} {1,-14} {2}", sym.n, Node.Name(sym.name), Node.nTyp[sym.typ]);
		if (sym.attrPos==null) Trace.Write(" false "); else Trace.Write(" true  ");
		if (sym.typ == Node.nt) {
			Trace.Write("{0,5}", Num(sym.graph));
			if (sym.deletable) Trace.Write(" true  "); else Trace.Write(" false ");
		} else
			Trace.Write("            ");
		Trace.WriteLine("{0,5}", sym.line);
	}

	public static void PrintSymbolTable() {
		Trace.WriteLine("Symbol Table:");
		Trace.WriteLine("------------"); Trace.WriteLine();
		Trace.WriteLine(" nr name          typ  hasAt graph  del   line");
		foreach (Symbol sym in Symbol.terminals) PrintSym(sym);
		foreach (Symbol sym in Symbol.pragmas) PrintSym(sym);
		foreach (Symbol sym in Symbol.nonterminals) PrintSym(sym);
		Trace.WriteLine();
	}
	
	public static void XRef() {
		SortedList tab = new SortedList();
		// collect lines where symbols have been defined
		foreach (Symbol sym in Symbol.nonterminals) {
			ArrayList list = (ArrayList)tab[sym];
			if (list == null) {list = new ArrayList(); tab[sym] = list;}
			list.Add(- sym.line);
		}
		// collect lines where symbols have been referenced
		foreach (Node n in Node.nodes) {
			if (n.typ == Node.t || n.typ == Node.wt || n.typ == Node.nt) {
				ArrayList list = (ArrayList)tab[n.sym];
				if (list == null) {list = new ArrayList(); tab[n.sym] = list;}
				list.Add(n.line);
			}
		}
		// print cross reference list
		Trace.WriteLine();
		Trace.WriteLine("Cross reference list:");
		Trace.WriteLine("--------------------"); Trace.WriteLine();
		foreach (Symbol sym in tab.Keys) {
			Trace.Write("  {0,-12}", Node.Name(sym.name));
			ArrayList list = (ArrayList)tab[sym];
			int col = 14;
			foreach (int line in list) {
				if (col + 5 > 80) {
					Trace.WriteLine();
					for (col = 1; col <= 14; col++) Trace.Write(" ");
				}
				Trace.Write("{0,5}", line); col += 5;
			}
			Trace.WriteLine();
		}
		Trace.WriteLine(); Trace.WriteLine();
	}
	
	public static void SetDDT(string s) {
		s = s.ToUpper();
		foreach (char ch in s) {
			if ('0' <= ch && ch <= '9') ddt[ch - '0'] = true;
			else switch (ch) {
				case 'A' : ddt[0] = true; break; // trace automaton
				case 'F' : ddt[1] = true; break; // list first/follow sets
				case 'G' : ddt[2] = true; break; // print syntax graph
				case 'I' : ddt[3] = true; break; // trace computation of first sets
				case 'J' : ddt[4] = true; break; // print ANY and SYNC sets
				case 'P' : ddt[8] = true; break; // print statistics
				case 'S' : ddt[6] = true; break; // list symbol table
				case 'X' : ddt[7] = true; break; // list cross reference table
				default : break;
			}
		}
	}

	public static void Init () {
		eofSy = new Symbol(Node.t, "EOF", 0);
	}
	
} // end Tab

} // end namespace
