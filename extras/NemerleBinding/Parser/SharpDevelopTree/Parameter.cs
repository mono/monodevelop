// created on 07.08.2003 at 20:12

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;
using NCC = Nemerle.Compiler;
using Nemerle.Compiler.Typedtree;

using System.Xml;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Parameter : DefaultParameter
	{
	    void LoadXml(XmlNode methodNode)
	    {
            if (methodNode != null) {
				XmlNode paramDocu = methodNode.SelectSingleNode("Docs/param[@name='" + name + "']");
				if (paramDocu != null) {
					documentation = paramDocu.InnerXml;
				}
			}	       
	    }
	   
		public Parameter (IMember declaringMember, Fun_parm pinfo, XmlNode docNode)
		{
			this.name = pinfo.Name;
			NCC.MType realType = (NCC.MType)pinfo.ty;
			if (realType is NCC.MType.Ref)
			{
			    NCC.MType.Ref rt = (NCC.MType.Ref)realType;
			    returnType = new ReturnType ((NCC.MType)rt.t.Fix ());
			    modifier |= ParameterModifier.Ref;
			}
			else if (realType is NCC.MType.Out)
			{
			    NCC.MType.Out rt = (NCC.MType.Out)realType;
			    returnType = new ReturnType ((NCC.MType)rt.t.Fix ());
			    modifier |= ParameterModifier.Out;
			}
			else
			{
			    returnType = new ReturnType (realType);
			}
			this.declaringMember = declaringMember;
			
			try { LoadXml (docNode); } catch { }
		}
		
		public Parameter (IMember declaringMember, SR.ParameterInfo pinfo, XmlNode docNode)
		{
		    this.name = pinfo.Name;
		    returnType = new ReturnType(pinfo.ParameterType);
		    this.declaringMember = declaringMember;
		    
		    try { LoadXml (docNode); } catch { }
		}
	}
}
