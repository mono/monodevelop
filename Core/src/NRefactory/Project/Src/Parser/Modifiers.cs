// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 915 $</version>
// </file>

using ICSharpCode.NRefactory.Parser.AST;
using System.Drawing;

namespace ICSharpCode.NRefactory.Parser
{
	internal class Modifiers
	{
		Modifier cur;
		Point location = new Point(-1, -1);
		
		public Modifier Modifier {
			get {
				return cur;
			}
		}
		
		public Point GetDeclarationLocation(Point keywordLocation)
		{
			if(location.X == -1 && location.Y == -1) {
				return keywordLocation;
			}
			return location;
		}
		
		public Point Location {
			get {
				return location;
			}
			set {
				location = value;
			}
		}
		
		public bool isNone { get { return cur == Modifier.None; } }
		
		public bool Contains(Modifier m)
		{
			return ((cur & m) != 0);
		}
		
		public void Add(Modifier m, Point tokenLocation) 
		{
			if(location.X == -1 && location.Y == -1) {
				location = tokenLocation;
			}
			
			if ((cur & m) == 0) {
				cur |= m;
			} else {
//				parser.Error("modifier " + m + " already defined");
			}
		}
		
		public void Add(Modifiers m)
		{
			Add(m.cur, m.Location);
		}
		
		public void Check(Modifier allowed)
		{
			Modifier wrong = cur & ~allowed;
			if (wrong != Modifier.None) {
//				parser.Error("modifier(s) " + wrong + " not allowed here");
			}
		}
	}
}
