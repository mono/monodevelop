// created on 06.08.2003 at 12:37

using System;
using System.Diagnostics;
/*
using MonoDevelop.Projects.Dom;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using Modifier = ICSharpCode.NRefactory.Ast.Modifiers;
using ClassType = MonoDevelop.Projects.Parser.ClassType;
*/
namespace CSharpBinding.Parser.SharpDevelopTree
{/*
	public class Class : DefaultClass
	{
		public Class (DefaultCompilationUnit cu, MonoDevelop.Projects.Parser.ClassType t, Modifier m, IRegion region, IRegion bodyRegion): base (cu)
		{
			classType = t;
			this.region = region;
			this.bodyRegion = bodyRegion;
			modifiers = (ModifierEnum)m;
		}
		
		public void UpdateModifier()
		{
			if (classType == ClassType.Enum) {
				foreach (DefaultField f in fields) {
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
			foreach (DefaultEvent e in events) {
				e.AddModifier(ModifierEnum.Public);
			}
			foreach (DefaultField f in fields) {
				f.AddModifier(ModifierEnum.Public);
			}
			foreach (DefaultIndexer i in indexer) {
				i.AddModifier(ModifierEnum.Public);
			}
			foreach (DefaultProperty p in properties) {
				p.AddModifier(ModifierEnum.Public);
			}
			
		}
	}*/
}
