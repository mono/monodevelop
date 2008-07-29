// created on 06.08.2003 at 12:37

using System.Diagnostics;
using MonoDevelop.Projects.Dom;
//using ICSharpCode.NRefactory.Parser;
//using Modifier = ICSharpCode.NRefactory.Ast.Modifiers;
//using ClassType = MonoDevelop.Projects.Parser.ClassType;

namespace VBBinding.Parser.SharpDevelopTree
{/*
	public class Class : DefaultClass
	{
		public Class(CompilationUnit cu, MonoDevelop.Projects.Parser.ClassType t, Modifier m, IRegion region): base (cu)
		{
			classType = t;
			this.region = region;
			modifiers = (ModifierEnum)m;
		}
		
		public void UpdateModifier()
		{
			if (classType == ClassType.Enum) {
				foreach (DefaultField f in Fields) {
					f.AddModifier(ModifierEnum.Public);
				}
				return;
			}
			if (classType != ClassType.Interface) {
				return;
			}
			foreach (Class c in InnerClasses) {
				c.modifiers = c.modifiers | ModifierEnum.Public;
			}
			foreach (IMethod m in Methods) {
				if (m is Constructor) {
					((Constructor)m).AddModifier(ModifierEnum.Public);
				} else if (m is DefaultMethod) {
					((DefaultMethod)m).AddModifier(ModifierEnum.Public);
				} else {
					Debug.Assert(false, "Unexpected type in method of interface. Can not set modifier to public!");
				}
			}
			foreach (DefaultEvent e in Events) {
				e.AddModifier(ModifierEnum.Public);
			}
			foreach (DefaultField f in Fields) {
				f.AddModifier(ModifierEnum.Public);
			}
			foreach (DefaultIndexer i in Indexer) {
				i.AddModifier(ModifierEnum.Public);
			}
			foreach (DefaultProperty p in Properties) {
				p.AddModifier(ModifierEnum.Public);
			}
			
		}
	}*/
}
