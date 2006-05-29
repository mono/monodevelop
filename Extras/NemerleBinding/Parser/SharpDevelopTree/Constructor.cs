// created on 06.08.2003 at 12:35

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Constructor : AbstractMethod, INemerleMethod
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		MethodInfo _member;
		public MethodInfo Member
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
			    parameters.Add(new Parameter(this, pinfo));
		}

		public Constructor (IClass declaringType, MethodInfo tinfo)
		{
		    this.declaringType = declaringType;
		
		    ModifierEnum mod = (ModifierEnum)0;
            if (tinfo.IsPrivate)
                mod |= ModifierEnum.Private;
            if (tinfo.IsInternal)
                mod |= ModifierEnum.Internal;
            if (tinfo.IsProtected)
                mod |= ModifierEnum.Protected;
            if (tinfo.IsPublic)
                mod |= ModifierEnum.Public;
            if (tinfo.IsAbstract)
                mod |= ModifierEnum.Abstract;
            if (tinfo.IsFinal)
                mod |= ModifierEnum.Sealed;
            if (tinfo.IsStatic)
                mod |= ModifierEnum.Static;
            if (tinfo.IsOverride)
                mod |= ModifierEnum.Override;
            if (tinfo.IsVirtual)
                mod |= ModifierEnum.Virtual;
            if (tinfo.IsNew)
                mod |= ModifierEnum.New;
            if (tinfo.IsExtern)
                mod |= ModifierEnum.Extern;
                
			modifiers = mod;
			
			this.FullyQualifiedName = "this";
			returnType = new ReturnType(tinfo.Type);
			this.region = Class.GetRegion(tinfo.Location);
			this.bodyRegion = Class.GetRegion(tinfo.Location);
			this._member = tinfo;
			    
			// Add parameters
			foreach (ParameterInfo pinfo in tinfo.Parameters)
			    parameters.Add(new Parameter(this, pinfo));
		}
	}
}
