// created on 06.08.2003 at 12:36

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Property : AbstractProperty
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		internal Method Getter;
		internal Method Setter;
		
		public Property (IClass declaringType, SR.PropertyInfo tinfo)
		{
		   	this.declaringType = declaringType;
		
		    ModifierEnum mod = (ModifierEnum)0;
 			modifiers = mod;
			
			this.FullyQualifiedName = tinfo.Name;
			returnType = new ReturnType(tinfo.PropertyType);
			this.region = Class.GetRegion();
			this.bodyRegion = Class.GetRegion();
		}
		
		public Property (IClass declaringType, PropertyInfo tinfo)
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
			
			this.FullyQualifiedName = tinfo.Name;
			returnType = new ReturnType(tinfo.Type);
			this.region = Class.GetRegion(tinfo.Location);
			this.bodyRegion = Class.GetRegion(tinfo.Location);
			
			if (tinfo.Getter != null)
		    {
			    this.Getter = new Method(declaringType, tinfo.Getter);
			    getterRegion = Class.GetRegion(tinfo.Getter.Location);
			}
			if (tinfo.Setter != null)
			{
			    this.Setter = new Method(declaringType, tinfo.Setter);
			    setterRegion = Class.GetRegion(tinfo.Setter.Location);
			}
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
