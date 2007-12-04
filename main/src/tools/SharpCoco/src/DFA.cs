// DFA.cs   Scaner automaton gnerated by Coco/R  H.Moessenboeck, Univ. of Linz
//----------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections;
using System.Text;

namespace at.jku.ssw.Coco {

//-----------------------------------------------------------------------------
//  State
//-----------------------------------------------------------------------------

public class State {				// state of finite automaton
	public static int lastNr;	// highest state number
	public int nr;						// state number
	public Action firstAction;// to first action of this state
	public Symbol endOf;			// recognized token if state is final
	public bool ctx;					// true if state is reached via contextTrans
	public State next;
	
	public State() {
		nr = ++lastNr;
	}
	
	public void AddAction(Action act) {
		Action lasta = null, a = firstAction;
		while (a != null && act.typ >= a.typ) {lasta = a; a = a.next;}
		// collecting classes at the beginning gives better performance
		act.next = a;
		if (a==firstAction) firstAction = act; else lasta.next = act;
	}
	
	public void DetachAction(Action act) {
		Action lasta = null, a = firstAction;
		while (a != null && a != act) {lasta = a; a = a.next;}
		if (a != null)
			if (a == firstAction) firstAction = a.next; else lasta.next = a.next;
	}
	
	public Action TheAction(char ch) {
		BitArray s;
		for (Action a = firstAction; a != null; a = a.next)
			if (a.typ == Node.chr && ch == a.sym) return a;
			else if (a.typ == Node.clas) {
				s = CharClass.Set(a.sym);
				if (s[ch]) return a;
			}
		return null;
	}
	
	public void MeltWith(State s) { // copy actions of s to state
		Action a;
		for (Action action = s.firstAction; action != null; action = action.next) {
			a = new Action(action.typ, action.sym, action.tc);
			a.AddTargets(action);
			AddAction(a);
		}
	}
	
}

//-----------------------------------------------------------------------------
//  Action
//-----------------------------------------------------------------------------

public class Action {			// action of finite automaton
	public int typ;					// type of action symbol: clas, chr
	public int sym;					// action symbol
	public int tc;					// transition code: normalTrans, contextTrans
	public Target target;		// states reached from this action
	public Action next;
	
	public Action(int typ, int sym, int tc) {
		this.typ = typ; this.sym = sym; this.tc = tc;
	}
	
	public void AddTarget(Target t) { // add t to the action.targets
		Target last = null;
		Target p = target;
		while (p != null && t.state.nr >= p.state.nr) {
			if (t.state == p.state) return;
			last = p; p = p.next;
		}
		t.next = p;
		if (p == target) target = t; else last.next = t;
	}

	public void AddTargets(Action a) { // add copy of a.targets to action.targets
		for (Target p = a.target; p != null; p = p.next) {
			Target t = new Target(p.state);
			AddTarget(t);
		}
		if (a.tc == Node.contextTrans) tc = Node.contextTrans;
	}
	
	public BitArray Symbols() {
		BitArray s;
		if (typ == Node.clas)
			s = (BitArray) CharClass.Set(sym).Clone();
		else {
			s = new BitArray(CharClass.charSetSize); s[sym] = true;
		}
		return s;
	}
	
	public void ShiftWith(BitArray s) {
		if (Sets.Elements(s) == 1) {
			typ = Node.chr; sym = Sets.First(s);
		} else {
			CharClass c = CharClass.Find(s);
			if (c == null) c = new CharClass("#", s); // class with dummy name
			typ = Node.clas; sym = c.n;
		}
	}
	
	public void GetTargetStates(out BitArray targets, out Symbol endOf, out bool ctx) { 
		// compute the set of target states
		targets = new BitArray(DFA.maxStates); endOf = null;
		ctx = false;
		for (Target t = target; t != null; t = t.next) {
			int stateNr = t.state.nr;
			if (stateNr <= DFA.lastSimState) targets[stateNr] = true;
			else targets.Or(Melted.Set(stateNr));
			if (t.state.endOf != null)
				if (endOf == null || endOf == t.state.endOf)
					endOf = t.state.endOf;
				else {
					Console.WriteLine("Tokens {0} and {1} cannot be distinguished", endOf.name, t.state.endOf.name);
					Errors.count++;
				}
			if (t.state.ctx) {
				ctx = true;
				// The following check seems to be unnecessary. It reported an error
				// if a symbol + context was the prefix of another symbol, e.g.
				//   s1 = "a" "b" "c".
				//   s2 = "a" CONTEXT("b").
				// But this is ok.
				// if (t.state.endOf != null) {
				//   Console.WriteLine("Ambiguous context clause");
				//	 Errors.count++;
				// }
			}
		}
	}
	
}

//-----------------------------------------------------------------------------
//  Target
//-----------------------------------------------------------------------------

public class Target {				// set of states that are reached by an action
	public State state;				// target state
	public Target next;
	
	public Target (State s) {
		state = s;
	}
}

//-----------------------------------------------------------------------------
//  Melted
//-----------------------------------------------------------------------------

public class Melted {					// info about melted states
	public static Melted first;	// head of melted state list
	public BitArray set;				// set of old states
	public State state;					// new state
	public Melted next;
	
	public Melted(BitArray set, State state) {
		this.set = set; this.state = state;
		this.next = first; first = this;
	}

	public static BitArray Set(int nr) {
		Melted m = first;
		while (m != null) {
			if (m.state.nr == nr) return m.set; else m = m.next;
		}
		throw new Exception("-- compiler error in Melted.Set");
	}
	
	public static Melted StateWithSet(BitArray s) {
		for (Melted m = first; m != null; m = m.next)
			if (Sets.Equals(s, m.set)) return m;
		return null;
	}
	
}

//-----------------------------------------------------------------------------
//  Comment
//-----------------------------------------------------------------------------

public class Comment {					// info about comment syntax
	public static Comment first;	// list of comments
	public string start;
	public string stop;
	public bool nested;
	public Comment next;
	
	static string Str(Node p) {
		StringBuilder s = new StringBuilder();
		while (p != null) {
			if (p.typ == Node.chr) {
				s.Append((char)p.val);
			} else if (p.typ == Node.clas) {
				BitArray set = CharClass.Set(p.val);
				if (Sets.Elements(set) != 1) Parser.SemErr("character set contains more than 1 character");
				s.Append((char)Sets.First(set));
			} else Parser.SemErr("comment delimiters may not be structured");
			p = p.next;
		}
		if (s.Length == 0 || s.Length > 2) {
			Parser.SemErr("comment delimiters must be 1 or 2 characters long");
			s = new StringBuilder("?");
		}
		return s.ToString();
	}
	
	public Comment(Node from, Node to, bool nested) {
		start = Str(from);
		stop = Str(to);
		this.nested = nested;
		this.next = first; first = this;
	}
	
}

//-----------------------------------------------------------------------------
//  DFA
//-----------------------------------------------------------------------------

public class DFA {
	public static int maxStates;
	public const int  EOF = -1;
	public const char CR  = '\r';
	public const char LF  = '\n';
	
	public static State firstState;
	public static State lastState;		// last allocated state
	public static int lastSimState;		// last non melted state
	public static FileStream fram;		// scanner frame input
	public static StreamWriter gen;		// generated scanner file
	       static string srcDir;      // directory of attributed grammar file
	public static Symbol curSy;				// current token to be recognized (in FindTrans)
	public static Node curGraph;			// start of graph for current token (in FindTrans)
	public static bool dirtyDFA;			// DFA may become nondeterministic in MatchedDFA
	public static bool hasCtxMoves;		// DFA has context transitions
	
	//---------- Output primitives
	private static string Ch(char ch) {
		if (ch < ' ' || ch >= 127 || ch == '\'' || ch == '\\') return Convert.ToString((int)ch);
		else return String.Format("'{0}'", ch);
	}
	
	private static string ChCond(char ch) {
		return String.Format("ch == {0}", Ch(ch));
	}
	
	private static void PutRange(BitArray s) {
		int[] lo = new int[32];
		int[] hi = new int[32];
		// fill lo and hi
		int max = CharClass.charSetSize;
		int top = -1;
		int i = 0;
		while (i < max) {
			if (s[i]) {
				top++; lo[top] = i; i++;
				while (i < max && s[i]) i++;
				hi[top] = i-1;
			} else i++;
		}
		// print ranges
		if (top == 1 && lo[0] == 0 && hi[1] == max-1 && hi[0]+2 == lo[1]) {
			BitArray s1 = new BitArray(max); s1[hi[0]+1] = true;
			gen.Write("!"); PutRange(s1);
		} else {
			gen.Write("(");
			for (i = 0; i <= top; i++) {
				if (hi[i] == lo[i]) gen.Write("ch == {0}", Ch((char)lo[i]));
				else if (lo[i] == 0) gen.Write("ch <= {0}", Ch((char)hi[i]));
				else if (hi[i] == max-1) gen.Write("ch >= {0}", Ch((char)lo[i]));
				else gen.Write("ch >= {0} && ch <= {1}", Ch((char)lo[i]), Ch((char)hi[i]));
				if (i < top) gen.Write(" || ");
			}
			gen.Write(")");
		}
	}
	
	//---------- String handling
	static char Hex2Char(string s) {
		int val = 0;
		for (int i = 0; i < s.Length; i++) {
			char ch = s[i];
			if ('0' <= ch && ch <= '9') val = 16 * val + (ch - '0');
			else if ('a' <= ch && ch <= 'f') val = 16 * val + (10 + ch - 'a');
			else if ('A' <= ch && ch <= 'Z') val = 16 * val + (10 + ch - 'A');
			else Parser.SemErr("bad escape sequence in string or character");
		}
		return (char)val;
	}
	
	static string Char2Hex(char ch) {
		StringWriter w = new StringWriter();
		w.Write("\\u{0:x4}", (int)ch);
		return w.ToString();
	}
		
	public static string Unescape (string s) {
		/* replaces escape sequences in s by their Unicode values. */
		StringBuilder buf = new StringBuilder();
		int i = 0;
		while (i < s.Length) {
			if (s[i] == '\\') {
				switch (s[i+1]) {
					case '\\': buf.Append('\\'); i += 2; break;
					case '\'': buf.Append('\''); i += 2; break;
					case '\"': buf.Append('\"'); i += 2; break;
					case 'r': buf.Append('\r'); i += 2; break;
					case 'n': buf.Append('\n'); i += 2; break;
					case 't': buf.Append('\t'); i += 2; break;
					case '0': buf.Append('\0'); i += 2; break;
					case 'a': buf.Append('\a'); i += 2; break;
					case 'b': buf.Append('\b'); i += 2; break;
					case 'f': buf.Append('\f'); i += 2; break;
					case 'v': buf.Append('\v'); i += 2; break;
					case 'u': case 'x':
						if (i + 6 <= s.Length) {
							buf.Append(Hex2Char(s.Substring(i+2, 4))); i += 6; break;
						} else {
							Parser.SemErr("bad escape sequence in string or character"); i = s.Length; break;
						}
					default: Parser.SemErr("bad escape sequence in string or character"); i += 2; break;
				}
			} else {
				buf.Append(s[i]);
				i++;
			}
		}
		return buf.ToString();
	}
	
	public static string Escape (string s) {
		StringBuilder buf = new StringBuilder();
		foreach (char ch in s) {
			if (ch == '\\') buf.Append("\\\\");
			else if (ch == '"') buf.Append("\\\"");
			else if (ch < ' ' || ch > '\u007f') buf.Append(Char2Hex(ch));
			else buf.Append(ch);
		}
		return buf.ToString();
	}
		
	//---------- State handling
	static State NewState() {
		State s = new State();
		if (firstState == null) firstState = s; else lastState.next = s;
		lastState = s;
		return s;
	}
	
	static void NewTransition(State from, State to, int typ, int sym, int tc) {
		if (to == firstState) Parser.SemErr("token must not start with an iteration");
		Target t = new Target(to);
		Action a = new Action(typ, sym, tc); a.target = t;
		from.AddAction(a);
	}
	
	static void CombineShifts() {
		State state;
		Action a, b, c;
		BitArray seta, setb;
		for (state = firstState; state != null; state = state.next) {
			for (a = state.firstAction; a != null; a = a.next) {
				b = a.next;
				while (b != null)
					if (a.target.state == b.target.state && a.tc == b.tc) {
						seta = a.Symbols(); setb = b.Symbols();
						seta.Or(setb);
						a.ShiftWith(seta);
						c = b; b = b.next; state.DetachAction(c);
					} else b = b.next;
			}
		}
	}
	
	static void FindUsedStates(State state, BitArray used) {
		if (used[state.nr]) return;
		used[state.nr] = true;
		for (Action a = state.firstAction; a != null; a = a.next)
			FindUsedStates(a.target.state, used);
	}
	
	static void DeleteRedundantStates() {
		State[] newState = new State[State.lastNr + 1];
		BitArray used = new BitArray(State.lastNr + 1);
		FindUsedStates(firstState, used);
		// combine equal final states
		for (State s1 = firstState.next; s1 != null; s1 = s1.next) // firstState cannot be final
			if (used[s1.nr] && s1.endOf != null && s1.firstAction == null && !s1.ctx)
				for (State s2 = s1.next; s2 != null; s2 = s2.next)
					if (used[s2.nr] && s1.endOf == s2.endOf && s2.firstAction == null & !s2.ctx) {
						used[s2.nr] = false; newState[s2.nr] = s1;
					}
		for (State state = firstState; state != null; state = state.next)
			if (used[state.nr])
				for (Action a = state.firstAction; a != null; a = a.next)
					if (!used[a.target.state.nr])
						a.target.state = newState[a.target.state.nr];
		// delete unused states
		lastState = firstState; State.lastNr = 0; // firstState has number 0
		for (State state = firstState.next; state != null; state = state.next)
			if (used[state.nr]) {state.nr = ++State.lastNr; lastState = state;}
			else lastState.next = state.next;
	}
	
	static State TheState(Node p) {
		State state;
		if (p == null) {state = NewState(); state.endOf = curSy; return state;}
		else return p.state;
	}
	
	static void Step(State from, Node p, BitArray stepped) {
		if (p == null) return;
		stepped[p.n] = true;
		switch (p.typ) {
			case Node.clas: case Node.chr: {
				NewTransition(from, TheState(p.next), p.typ, p.val, p.code);
				break;
			}
			case Node.alt: {
				Step(from, p.sub, stepped); Step(from, p.down, stepped);
				break;
			}
			case Node.iter: case Node.opt: {
				if (p.next != null && !stepped[p.next.n]) Step(from, p.next, stepped);
				Step(from, p.sub, stepped);
				break;
			}
		}
	}
	
	static void NumberNodes(Node p, State state) {
		/* Assigns a state n.state to every node n. There will be a transition from
		   n.state to n.next.state triggered by n.val. All nodes in an alternative
		   chain are represented by the same state.
		*/
		if (p == null) return;
		if (p.state != null) return; // already visited;
		if (state == null) state = NewState();
		p.state = state;
		if (Node.DelGraph(p)) state.endOf = curSy;
		switch (p.typ) {
			case Node.clas: case Node.chr: {
				NumberNodes(p.next, null);
				break;
			}
			case Node.opt: {
				NumberNodes(p.next, null); NumberNodes(p.sub, state);
				break;
			}
			case Node.iter: {
				NumberNodes(p.next, state); NumberNodes(p.sub, state);
				break;
			}
			case Node.alt: {
				NumberNodes(p.sub, state); NumberNodes(p.down, state);
				break;
			}
		}
	}
	
	static void FindTrans (Node p, bool start, BitArray marked) {
		if (p == null || marked[p.n]) return;
		marked[p.n] = true;
		if (start) Step(p.state, p, new BitArray(Node.nodes.Count)); // start of group of equally numbered nodes
		switch (p.typ) {
			case Node.clas: case Node.chr: {
				FindTrans(p.next, true, marked);
				break;
			}
			case Node.opt: {
				FindTrans(p.next, true, marked); FindTrans(p.sub, false, marked);
				break;
			}
			case Node.iter: {
				FindTrans(p.next, false, marked); FindTrans(p.sub, false, marked);
				break;
			}
			case Node.alt: {
				FindTrans(p.sub, false, marked); FindTrans(p.down, false, marked);
				break;
			}
		}
	}
	
	public static void ConvertToStates(Node p, Symbol sym) {
		curGraph = p; curSy = sym;
		if (Node.DelGraph(curGraph)) Parser.SemErr("token might be empty");
		NumberNodes(curGraph, firstState);
		FindTrans(curGraph, true, new BitArray(Node.nodes.Count));
	}
	
	static Symbol MatchedDFA(string s, Symbol sym) {
		int i, len = s.Length;
		bool weakMatch = false;
		// s has no quotes
		State state = firstState;
		for (i = 0; i < len; i++) { // try to match s against existing DFA
			Action a = state.TheAction(s[i]);
			if (a == null) break;
			if (a.typ == Node.clas) weakMatch = true;
			state = a.target.state;
		}
		// don't execute the following block if s was totally consumed and the DFA is in a final state
		if (weakMatch && (i != len || state.endOf == null)) {
			state = firstState; i = 0;
			dirtyDFA = true;
		}
		for (; i < len; i++) { // make new DFA for s[i..len-1]
			State to = NewState();
			NewTransition(state, to, Node.chr, s[i], Node.normalTrans);
			state = to;
		}
		Symbol matchedSym = state.endOf;
		if (state.endOf == null) state.endOf = sym;
		return matchedSym;
	}
	
	public static void MatchLiteral(Symbol sym) { // store string either as token or as literal
		string name = Unescape(sym.name.Substring(1, sym.name.Length-2));
		if (name.IndexOf('\0') >= 0) Parser.SemErr("\\0 not allowed here. Used as eof character");
	  Symbol matchedSym = MatchedDFA(name, sym);
	  if (matchedSym == null)
	    sym.tokenKind = Symbol.classToken;
	  else {
	    matchedSym.tokenKind = Symbol.classLitToken;
	    sym.tokenKind = Symbol.litToken;
	  }
	}
	
	static void SplitActions(State state, Action a, Action b) {
		Action c; BitArray seta, setb, setc;
		seta = a.Symbols(); setb = b.Symbols();
		if (Sets.Equals(seta, setb)) {
			a.AddTargets(b);
			state.DetachAction(b);
		} else if (Sets.Includes(seta, setb)) {
			setc = (BitArray)seta.Clone(); Sets.Subtract(setc, setb);
			b.AddTargets(a);
			a.ShiftWith(setc);
		} else if (Sets.Includes(setb, seta)) {
			setc = (BitArray)setb.Clone(); Sets.Subtract(setc, seta);
			a.AddTargets(b);
			b.ShiftWith(setc);
		} else {
			setc = (BitArray)seta.Clone(); setc.And(setb);
			Sets.Subtract(seta, setc);
			Sets.Subtract(setb, setc);
			a.ShiftWith(seta);
			b.ShiftWith(setb);
			c = new Action(0, 0, Node.normalTrans);  // typ and sym are set in ShiftWith
			c.AddTargets(a);
			c.AddTargets(b);
			c.ShiftWith(setc);
			state.AddAction(c);
		}
	}
	
	private static bool Overlap(Action a, Action b) {
		BitArray seta, setb;
		if (a.typ == Node.chr)
			if (b.typ == Node.chr) return a.sym == b.sym;
			else {setb = CharClass.Set(b.sym); return setb[a.sym];}
		else {
			seta = CharClass.Set(a.sym);
			if (b.typ ==Node.chr) return seta[b.sym];
			else {setb = CharClass.Set(b.sym); return Sets.Intersect(seta, setb);}
		}
	}
	
	static bool MakeUnique(State state) { // return true if actions were split
		bool changed = false;
		for (Action a = state.firstAction; a != null; a = a.next)
			for (Action b = a.next; b != null; b = b.next)
				if (Overlap(a, b)) {SplitActions(state, a, b); changed = true;}
		return changed;
	}
	
	static void MeltStates(State state) {
		bool changed, ctx;
		BitArray targets;
		Symbol endOf;
		for (Action action = state.firstAction; action != null; action = action.next) {
			if (action.target.next != null) {
				action.GetTargetStates(out targets, out endOf, out ctx);
				Melted melt = Melted.StateWithSet(targets);
				if (melt == null) {
					State s = NewState(); s.endOf = endOf; s.ctx = ctx;
					for (Target targ = action.target; targ != null; targ = targ.next)
						s.MeltWith(targ.state);
					do {changed = MakeUnique(s);} while (changed);
					melt = new Melted(targets, s);
				}
				action.target.next = null;
				action.target.state = melt.state;
			}
		}
	}
	
	static void FindCtxStates() {
		for (State state = firstState; state != null; state = state.next)
			for (Action a = state.firstAction; a != null; a = a.next)
				if (a.tc == Node.contextTrans) a.target.state.ctx = true;
	}
	
	public static void MakeDeterministic() {
		State state;
		bool changed;
		lastSimState = lastState.nr;
		maxStates = 2 * lastSimState; // heuristic for set size in Melted.set
		FindCtxStates();
		for (state = firstState; state != null; state = state.next)
			do {changed = MakeUnique(state);} while (changed);
		for (state = firstState; state != null; state = state.next)
			MeltStates(state);
		DeleteRedundantStates();
		CombineShifts();
	}
	
	public static void PrintStates() {
		Trace.WriteLine("\n---------- states ----------");
		for (State state = firstState; state != null; state = state.next) {
			bool first = true;
			if (state.endOf == null) Trace.Write("               ");
			else Trace.Write("E({0,12})", Node.Name(state.endOf.name));
			Trace.Write("{0,3}:", state.nr);
			if (state.firstAction == null) Trace.WriteLine();
			for (Action action = state.firstAction; action != null; action = action.next) {
				if (first) {Trace.Write(" "); first = false;} else Trace.Write("                    ");
				if (action.typ == Node.clas) Trace.Write(((CharClass)CharClass.classes[action.sym]).name);
				else Trace.Write("{0, 3}", Ch((char)action.sym));
				for (Target targ = action.target; targ != null; targ = targ.next)
					Trace.Write(" {0, 3}", targ.state.nr);
				if (action.tc == Node.contextTrans) Trace.WriteLine(" context"); else Trace.WriteLine();
			}
		}
		Trace.WriteLine("\n---------- character classes ----------");
		CharClass.WriteClasses();
	}
	
	static void GenComBody(Comment com) {
		gen.WriteLine(  "\t\t\tfor(;;) {");
		gen.Write    (  "\t\t\t\tif ({0}) ", ChCond(com.stop[0])); gen.WriteLine("{");
		if (com.stop.Length == 1) {
			gen.WriteLine("\t\t\t\t\tlevel--;");
			gen.WriteLine("\t\t\t\t\tif (level == 0) { oldEols = line - line0; NextCh(); return true; }");
			gen.WriteLine("\t\t\t\t\tNextCh();");
		} else {
			gen.WriteLine("\t\t\t\t\tNextCh();");
			gen.WriteLine("\t\t\t\t\tif ({0}) {{", ChCond(com.stop[1]));
			gen.WriteLine("\t\t\t\t\t\tlevel--;");
			gen.WriteLine("\t\t\t\t\t\tif (level == 0) { oldEols = line - line0; NextCh(); return true; }");
			gen.WriteLine("\t\t\t\t\t\tNextCh();");
			gen.WriteLine("\t\t\t\t\t}");
		}
		if (com.nested) {
			gen.Write    ("\t\t\t\t}"); gen.Write(" else if ({0}) ", ChCond(com.start[0])); gen.WriteLine("{");
			if (com.start.Length == 1)
				gen.WriteLine("\t\t\t\t\tlevel++; NextCh();");
			else {
				gen.WriteLine("\t\t\t\t\tNextCh();");
				gen.Write    ("\t\t\t\t\tif ({0}) ", ChCond(com.start[1])); gen.WriteLine("{");
				gen.WriteLine("\t\t\t\t\t\tlevel++; NextCh();");
				gen.WriteLine("\t\t\t\t\t}");
			}
		}
		gen.WriteLine(    "\t\t\t\t} else if (ch == EOF) return false;");
		gen.WriteLine(    "\t\t\t\telse NextCh();");
		gen.WriteLine(    "\t\t\t}");
	}
	
	static void GenComment(Comment com, int i) {
		gen.Write    ("\n\tstatic bool Comment{0}() ", i); gen.WriteLine("{");
		gen.WriteLine("\t\tint level = 1, line0 = line, lineStart0 = lineStart;");
		if (com.start.Length == 1) {
			gen.WriteLine("\t\tNextCh();");
			GenComBody(com);
		} else {
			gen.WriteLine("\t\tNextCh();");
			gen.Write    ("\t\tif ({0}) ", ChCond(com.start[1])); gen.WriteLine("{");
			gen.WriteLine("\t\t\tNextCh();");
			GenComBody(com);
			gen.WriteLine("\t\t} else {");
			gen.WriteLine("\t\t\tif (ch==EOL) {line--; lineStart = lineStart0;}");
			gen.WriteLine("\t\t\tpos = pos - 2; Buffer.Pos = pos+1; NextCh();");
			gen.WriteLine("\t\t}");
			gen.WriteLine("\t\treturn false;");
		}
		gen.WriteLine("\t}");
	}
	
	static void CopyFramePart(string stop) {
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
		Errors.Exception(" -- incomplete or corrupt scanner frame file");
	}
	
	static void GenLiterals () {
		foreach (Symbol sym in Symbol.terminals) {
			if (sym.tokenKind == Symbol.litToken) {
				// sym.name stores literals with quotes, e.g. "\"Literal\"",
				// therefore do NOT place any quotes around {0} after the case
				// or you'll get: case ""Literal"": t.kind = ..., which causes an error
				gen.WriteLine("\t\t\tcase {0}: t.kind = {1}; break;", sym.name, sym.n);
			}
		}
		gen.WriteLine("\t\t\tdefault: break;");
	}
	
	static void WriteState(State state) {
		Symbol endOf = state.endOf;
		gen.WriteLine("\t\t\tcase {0}:", state.nr);
		bool ctxEnd = state.ctx;
		for (Action action = state.firstAction; action != null; action = action.next) {
			if (action == state.firstAction) gen.Write("\t\t\t\tif (");
			else gen.Write("\t\t\t\telse if (");
			if (action.typ == Node.chr) gen.Write(ChCond((char)action.sym));
			else PutRange(CharClass.Set(action.sym));
			gen.Write(") {");
			if (action.tc == Node.contextTrans) {
				gen.Write("apx++; "); ctxEnd = false;
			} else if (state.ctx)
				gen.Write("apx = 0; ");
			gen.Write("buf.Append(ch); NextCh(); goto case {0};", action.target.state.nr);
			gen.WriteLine("}");
		}
		if (state.firstAction == null)
			gen.Write("\t\t\t\t{");
		else
			gen.Write("\t\t\t\telse {");
		if (ctxEnd) { // final context state: cut appendix
			gen.WriteLine();
			gen.WriteLine("\t\t\t\t\tbuf.Length = buf.Length - apx;");
			gen.WriteLine("\t\t\t\t\tpos = pos - apx - 1; line = t.line;");
			gen.WriteLine("\t\t\t\t\tBuffer.Pos = pos+1; NextCh();");
			gen.Write(  	"\t\t\t\t\t");
		}
		if (endOf == null) {
			gen.WriteLine("t.kind = noSym; goto done;}");
		} else {
			gen.Write("t.kind = {0}; ", endOf.n);
			if (endOf.tokenKind == Symbol.classLitToken) {
				gen.WriteLine("t.val = buf.ToString(); CheckLiteral(); return t;}");
			} else {
				gen.WriteLine("goto done;}");
			}
		}
	}
	
	static void FillStartTab(int[] startTab) {
		startTab[0] = State.lastNr + 1; // eof
		for (Action action = firstState.firstAction; action != null; action = action.next) {
			int targetState = action.target.state.nr;
			if (action.typ == Node.chr) startTab[action.sym] = targetState;
			else {
				BitArray s = CharClass.Set(action.sym);
				for (int i = 0; i < s.Count; i++)
					if (s[i]) startTab[i] = targetState;
			}
		}
	}
	
	public static void WriteScanner() {
		int i, j;
		int[] startTab = new int[CharClass.charSetSize];
		string dir = System.Environment.CurrentDirectory;
		string fr = Path.Combine (dir, "Scanner.frame");
		if (!File.Exists(fr)) {
			string frameDir = Environment.GetEnvironmentVariable("crframes");
			if (frameDir != null) fr = Path.Combine (frameDir.Trim(), "Scanner.frame");
			if (!File.Exists(fr)) Errors.Exception("-- Cannot find Scanner.frame");
		}
		try {
			fram = new FileStream(fr, FileMode.Open, FileAccess.Read, FileShare.Read);
		} catch (FileNotFoundException) {
			Errors.Exception("-- Cannot open Scanner.frame.");
		}
		try {
			string fn = dir + "\\Scanner.cs";
			if (File.Exists(fn)) File.Copy(fn, fn+".old", true);
			FileStream s = new FileStream(fn, FileMode.Create);
			gen = new StreamWriter(s);
		} catch (IOException) {
			Errors.Exception("-- Cannot generate scanner file.");
		}
		if (dirtyDFA) MakeDeterministic();
		FillStartTab(startTab);

		CopyFramePart("-->namespace");
		/* AW add namespace, if it exists */
		if (Tab.nsName != null && Tab.nsName.Length > 0) {
			gen.Write("namespace ");
			gen.Write(Tab.nsName);
			gen.Write(" {");
		}
		CopyFramePart("-->constants");
		gen.WriteLine("\tconst int maxT = {0};", Symbol.terminals.Count - 1);
		CopyFramePart("-->declarations");
		gen.WriteLine("\tconst int noSym = {0};", Tab.noSym.n);
		gen.WriteLine("\tstatic short[] start = {");
		for (i = 0; i < CharClass.charSetSize / 16; i++) {
			gen.Write("\t");
			for (j = 0; j < 16; j++)
				gen.Write("{0,3},", startTab[16*i+j]);
			gen.WriteLine();
		}
		gen.WriteLine("\t  0};");
		CopyFramePart("-->initialization");
		gen.WriteLine("\t\tignore = new BitArray({0});", CharClass.charSetSize);
		gen.Write("\t\t");
		if (Tab.ignored == null) gen.Write("ignore[' '] = true;");
		else {
			j = 0;
			for (i = 0; i < Tab.ignored.Count; i++)
				if (Tab.ignored[i]) {
					gen.Write("ignore[{0}] = true; ", i);
					if (++j % 4 == 0) { gen.WriteLine(); gen.Write("\t\t"); }
				}
		} 
		CopyFramePart("-->comment");
		Comment com = Comment.first; i = 0;
		while (com != null) {
			GenComment(com, i);
			com = com.next; i++;
		}
		CopyFramePart("-->literals"); GenLiterals();
		CopyFramePart("-->scan1");
		if (Comment.first!=null) {
			gen.Write("\t\tif (");
			com = Comment.first; i = 0;
			while (com != null) {
				gen.Write(ChCond(com.start[0]));
				gen.Write(" && Comment{0}()", i);
				if (com.next != null) gen.Write(" ||");
				com = com.next; i++;
			}
			gen.Write(") return NextToken();");
		}
		if (hasCtxMoves) gen.WriteLine("\t\tint apx = 0;");
		CopyFramePart("-->scan2");
		for (State state = firstState.next; state != null; state = state.next)
			WriteState(state);
		gen.Write("\t\t\tcase "+(State.lastNr+1)+": {t.kind = 0; goto done;}");
		CopyFramePart("$$$");
		/* AW 12-20-02 close namespace, if it exists */
		if (Tab.nsName != null && Tab.nsName.Length > 0) gen.Write("}");
		gen.Close();
	}
	
	public static void Init (string dir) {
		srcDir = dir;
		firstState = null; lastState = null; State.lastNr = -1;
		firstState = NewState();
		Melted.first = null; Comment.first = null;
		dirtyDFA = false;
		hasCtxMoves = false;
	}
	
} // end DFA

} // end namespace
