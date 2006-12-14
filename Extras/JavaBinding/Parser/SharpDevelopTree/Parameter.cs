// created on 07.08.2003 at 20:12

using MonoDevelop.Projects.Parser;

namespace JavaBinding.Parser.SharpDevelopTree
{
	public class Parameter : DefaultParameter
	{
		public Parameter(string name, ReturnType type)
		{
			this.name = name;
			returnType = type;
		}
	}
}
