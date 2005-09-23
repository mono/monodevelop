// created on 07.08.2003 at 20:12

using MonoDevelop.Internal.Parser;

namespace VBBinding.Parser.SharpDevelopTree
{
	public class Parameter : AbstractParameter
	{
		public Parameter(string name, ReturnType type)
		{
			Name = name;
			returnType = type;
		}
	}
}
