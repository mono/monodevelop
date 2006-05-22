// created on 06.08.2003 at 12:37

using System;
using System.Diagnostics;

using MonoDevelop.Projects.Parser;
using ICSharpCode.NRefactory.Parser;
using ModifierFlags = ICSharpCode.NRefactory.Parser.AST.Modifier;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Class : AbstractClass
	{
		ICompilationUnit cu;
		
		public Class(CompilationUnit cu, ClassType t, ModifierFlags m, IRegion region, IRegion bodyRegion)
		{
			this.cu = cu;
			classType = t;
			this.region = region;
			this.bodyRegion = bodyRegion;
			modifiers = (ModifierEnum)m;
		}
		
		public void UpdateModifier()
		{
			if (classType == ClassType.Enum) {
				foreach (Field f in fields) {
					f.AddModifier(ModifierEnum.Public);
				}
				return;
			}
			if (classType != ClassType.Interface) {
				return;
			}
			foreach (Class c in innerClasses) {
				c.modifiers = c.modifiers | ModifierEnum.Public;
			}
			foreach (IMethod m in methods) {
				if (m is Constructor) {
					((Constructor)m).AddModifier(ModifierEnum.Public);
				} else if (m is Method) {
					((Method)m).AddModifier(ModifierEnum.Public);
				} else {
					Debug.Assert(false, "Unexpected type in method of interface. Can not set modifier to public!");
				}
			}
			foreach (Event e in events) {
				e.AddModifier(ModifierEnum.Public);
			}
			foreach (Field f in fields) {
				f.AddModifier(ModifierEnum.Public);
			}
			foreach (Indexer i in indexer) {
				i.AddModifier(ModifierEnum.Public);
			}
			foreach (Property p in properties) {
				p.AddModifier(ModifierEnum.Public);
			}
			
		}
		public override ICompilationUnit CompilationUnit {
			get {
				return cu;
			}
		}
	}
}
