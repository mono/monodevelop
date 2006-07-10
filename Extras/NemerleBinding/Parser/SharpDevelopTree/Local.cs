// created on 04.08.2003 at 18:06

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;
using NCC = Nemerle.Compiler;

using System.Xml;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Local : AbstractField
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Local (Class declaringType, NCC.LocalValueCompletionPossibility tinfo)
		{
		    this.declaringType = declaringType;
		
		    ModifierEnum mod = ModifierEnum.Public;
            
            if (!tinfo.Value.IsMutable)
                mod |= ModifierEnum.Readonly;
                
			modifiers = mod;
			
			this.FullyQualifiedName = tinfo.Value.Name;
			returnType = new ReturnType (tinfo.Value.Type.Fix ());
			this.region = Class.GetRegion ();
	   }
	}
}
