// created on 07.08.2003 at 20:12

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Parameter : AbstractParameter
	{
		public Parameter (IMember declaringMember, ParameterInfo pinfo)
		{
			this.name = pinfo.Name;
			returnType = new ReturnType(pinfo.Type);
			this.declaringMember = declaringMember;
		}
		
		public Parameter (IMember declaringMember, ConstructedTypeInfo tinfo)
		{
			this.name = "";
			returnType = new ReturnType(tinfo);
			this.declaringMember = declaringMember;
		}
		
		public Parameter (IMember declaringMember, SR.ParameterInfo pinfo)
		{
		    this.name = pinfo.Name;
		    returnType = new ReturnType(pinfo.ParameterType);
		    this.declaringMember = declaringMember;
		}
	}
}
