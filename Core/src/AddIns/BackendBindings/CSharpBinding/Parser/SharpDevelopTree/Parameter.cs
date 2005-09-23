// created on 07.08.2003 at 20:12

using MonoDevelop.Internal.Parser;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Parameter : AbstractParameter
	{
		public Parameter(string name, ReturnType type)
		{
			this.name = name;
			returnType = type;
		}
	}
}
