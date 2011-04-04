// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 4482 $</version>
// </file>

using ICSharpCode.OldNRefactory.Ast;

namespace ICSharpCode.OldNRefactory.Parser
{
	internal class ModifierList
	{
		Modifiers cur;
		Location location = new Location(-1, -1);
		
		public Modifiers Modifier {
			get {
				return cur;
			}
			set {
				cur = value;
			}
		}
		
		public Location GetDeclarationLocation(Location keywordLocation)
		{
			if(location.IsEmpty) {
				return keywordLocation;
			}
			return location;
		}
		
//		public Location Location {
//			get {
//				return location;
//			}
//			set {
//				location = value;
//			}
//		}
		
		public bool isNone { get { return cur == Modifiers.None; } }
		
		public bool Contains(Modifiers m)
		{
			return ((cur & m) != 0);
		}
		
		public void Add(Modifiers m, Location tokenLocation) 
		{
			if(location.IsEmpty) {
				location = tokenLocation;
			}
			if (m == Modifiers.Internal && (cur & Modifiers.Protected) != 0) {
				cur = Modifiers.ProtectedAndInternal;
				return;
			}
			if ((cur & m) == 0) {
				cur |= m;
			} else {
//				parser.Error("modifier " + m + " already defined");
			}
		}
		
//		public void Add(Modifiers m)
//		{
//			Add(m.cur, m.Location);
//		}
		
		public void Check(Modifiers allowed)
		{
			Modifiers wrong = cur & ~allowed;
			if (wrong != Modifiers.None) {
//				parser.Error("modifier(s) " + wrong + " not allowed here");
			}
		}
	}
}
