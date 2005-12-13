// created on 06.08.2003 at 12:34

using MonoDevelop.Projects.Parser;
using ICSharpCode.SharpRefactory.Parser;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Indexer : AbstractIndexer
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Indexer (IClass declaringType, ReturnType type, ParameterCollection parameters, ModifierFlags m, IRegion region, IRegion bodyRegion)
		{
			returnType      = type;
			this.parameters = parameters;
			this.region     = region;
			this.bodyRegion = bodyRegion;
			this.declaringType = declaringType;
			modifiers = (ModifierEnum)m;
		}
	}
}
