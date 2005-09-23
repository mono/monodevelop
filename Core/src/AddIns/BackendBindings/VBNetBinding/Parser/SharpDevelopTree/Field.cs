// created on 04.08.2003 at 18:06
using System;
using MonoDevelop.Internal.Parser;
using ICSharpCode.SharpRefactory.Parser.VB;

namespace VBBinding.Parser.SharpDevelopTree
{
	public class Field : AbstractField
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Field(ReturnType type, string fullyQualifiedName, Modifier m, IRegion region)
		{
			this.returnType = type;
			this.FullyQualifiedName = fullyQualifiedName;
			this.region = region;
			modifiers = (ModifierEnum)m;
//			Console.WriteLine("modifiers for field {0} are {1} were {2}", fullyQualifiedName, modifiers, m);
		}
		
		public void SetModifiers(ModifierEnum m)
		{
			modifiers = m;
		}
	}
}
