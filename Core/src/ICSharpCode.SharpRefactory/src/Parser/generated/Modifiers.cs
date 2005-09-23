using System;

namespace ICSharpCode.SharpRefactory.Parser
{
	public class Modifiers
	{
		Modifier cur;
		Parser   parser;
		
		public Modifier Modifier {
			get {
				return cur;
			}
		}
		
		public Modifiers(Parser parser)
		{
			this.parser = parser;
			cur         = Modifier.None;
		}
		
		public bool isNone { get { return cur == Modifier.None; } }
		
		public void Add(Modifier m) 
		{
			if ((cur & m) == 0) {
				cur |= m;
			} else {
				parser.Error("modifier " + m + " already defined");
			}
		}
		
		public void Add(Modifiers m)
		{
			Add(m.cur);
		}
		
		// FIXME: probably need more flexible method
		public void Check (Modifier allowed)
		{
			Modifier wrong = cur & (allowed ^ Modifier.All);
			if (wrong != Modifier.None)
				parser.Error ("modifier(s) " + wrong + " not allowed here");

			if ((cur & (Modifier.Sealed | Modifier.Static)) == (Modifier.Sealed | Modifier.Static))
				parser.Error ("cannot be both static and sealed");
			if ((cur & (Modifier.Abstract | Modifier.Static)) == (Modifier.Abstract | Modifier.Static))
				parser.Error ("cannot be both static and abstract");
		}
	}
}
