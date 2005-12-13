// created on 06.08.2003 at 12:36

using MonoDevelop.Projects.Parser;
using ICSharpCode.SharpRefactory.Parser;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Property : AbstractProperty
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Property (IClass declaringType, string fullyQualifiedName, ReturnType type, ModifierFlags m, IRegion region, IRegion bodyRegion)
		{
			this.FullyQualifiedName = fullyQualifiedName;
			returnType = type;
			this.region = region;
			this.bodyRegion = bodyRegion;
			this.declaringType = declaringType;
			modifiers = (ModifierEnum)m;
		}
		
		public new IRegion GetterRegion {
			get { return getterRegion; }
			set { getterRegion = value; }
		}

		public new IRegion SetterRegion {
			get { return setterRegion; }
			set { setterRegion = value; }
		}
	}
}
