// created on 06.08.2003 at 12:35

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;
using NCC = Nemerle.Compiler;
using Nemerle.Compiler.Typedtree;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Constructor : DefaultMethod, INemerleMethod
	{
		NCC.IMethod _member;
		public NCC.IMethod Member
		{
		    get { return _member; }
		}
		
		public Constructor (IClass declaringType, SR.MethodInfo tinfo)
		{
		    this.declaringType = declaringType;
		
		    ModifierEnum mod = (ModifierEnum)0;
            if (tinfo.IsPrivate)
                mod |= ModifierEnum.Private;
            if (tinfo.IsAssembly)
                mod |= ModifierEnum.Internal;
            if (tinfo.IsFamily)
                mod |= ModifierEnum.Protected;
            if (tinfo.IsPublic)
                mod |= ModifierEnum.Public;
            if (tinfo.IsAbstract)
                mod |= ModifierEnum.Abstract;
            if (tinfo.IsFinal)
                mod |= ModifierEnum.Sealed;
            if (tinfo.IsStatic)
                mod |= ModifierEnum.Static;
            if (tinfo.IsVirtual)
                mod |= ModifierEnum.Virtual;
                
			modifiers = mod;
			
			this.FullyQualifiedName = tinfo.Name;
			this.region = Class.GetRegion();
			this.bodyRegion = Class.GetRegion();
			this._member = null;
			    
			// Add parameters
			foreach (SR.ParameterInfo pinfo in tinfo.GetParameters())
			    parameters.Add(new Parameter(this, pinfo, null));
		}

		public Constructor (IClass declaringType, NCC.IMethod tinfo)
		{
		    this.declaringType = declaringType;
		
			ModifierEnum mod = (ModifierEnum)0;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Private) != 0)
                mod |= ModifierEnum.Private;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Internal) != 0)
                mod |= ModifierEnum.Internal;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Protected) != 0)
                mod |= ModifierEnum.Protected;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Public) != 0)
                mod |= ModifierEnum.Public;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Abstract) != 0)
                mod |= ModifierEnum.Abstract;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Sealed) != 0)
                mod |= ModifierEnum.Sealed;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Static) != 0)
                mod |= ModifierEnum.Static;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Override) != 0)
                mod |= ModifierEnum.Override;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Virtual) != 0)
                mod |= ModifierEnum.Virtual;
            if ((tinfo.Attributes & NCC.NemerleAttributes.New) != 0)
                mod |= ModifierEnum.New;
            if ((tinfo.Attributes & NCC.NemerleAttributes.Extern) != 0)
                mod |= ModifierEnum.Extern;
                
			modifiers = mod;
			this.FullyQualifiedName = "this";
			
			returnType = new ReturnType ((NCC.MType)tinfo.ReturnType);
			this.region = Class.GetRegion (tinfo.Location);
            if (tinfo is NCC.MemberBuilder)
                this.bodyRegion = Class.GetRegion (((NCC.MemberBuilder)tinfo).BodyLocation);
            else
                this.bodyRegion = Class.GetRegion (tinfo.Location);
			this._member = tinfo;
			    
			// Add parameters
			foreach (Fun_parm pinfo in tinfo.GetParameters ())
			    parameters.Add(new Parameter(this, pinfo, null));
		}
	}
}
