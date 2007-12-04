// created on 04.08.2003 at 18:06

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;
using NCC = Nemerle.Compiler;

using System.Xml;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Local : DefaultField
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Local (Class declaringType, NCC.LocalValue tinfo)
		{
		    this.declaringType = declaringType;
		
		    ModifierEnum mod = ModifierEnum.Public;
            
            if (!tinfo.IsMutable)
                mod |= ModifierEnum.Readonly;
                
			modifiers = mod;
			
			this.FullyQualifiedName = tinfo.Name;
			returnType = new ReturnType (tinfo.Type.Fix ());
			this.region = Class.GetRegion ();
	   }
	}
}
