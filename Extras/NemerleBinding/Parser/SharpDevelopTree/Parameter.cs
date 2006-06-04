// created on 07.08.2003 at 20:12

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;
using NCC = Nemerle.Compiler;
using Nemerle.Compiler.Typedtree;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Parameter : AbstractParameter
	{
		public Parameter (IMember declaringMember, Fun_parm pinfo)
		{
			this.name = pinfo.Name;
			NCC.MType realType = (NCC.MType)pinfo.ty;
			if (realType is NCC.MType.Ref)
			{
			    NCC.MType.Ref rt = (NCC.MType.Ref)realType;
			    returnType = new ReturnType ((NCC.MType)rt.t);
			}
			else if (realType is NCC.MType.Out)
			{
			    NCC.MType.Out rt = (NCC.MType.Out)realType;
			    returnType = new ReturnType ((NCC.MType)rt.t);
			}
			else
			{
			    returnType = new ReturnType (realType);
			}
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
