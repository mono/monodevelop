// created on 06.08.2003 at 12:34

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Indexer : AbstractIndexer
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Indexer (IClass declaringType, SR.PropertyInfo tinfo)
		{
		    this.declaringType = declaringType;
		
		    ModifierEnum mod = (ModifierEnum)0;
			modifiers = mod;
			
			this.FullyQualifiedName = tinfo.Name;
			returnType = new ReturnType(tinfo.PropertyType);
			this.region = Class.GetRegion();
			this.bodyRegion = Class.GetRegion();
			    
			// Add parameters
			foreach (SR.ParameterInfo pinfo in tinfo.GetIndexParameters())
			    parameters.Add(new Parameter(this, pinfo));
		}
		
		public Indexer (IClass declaringType, PropertyInfo tinfo)
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
			    getterRegion = Class.GetRegion(tinfo.Getter.Location);
			if (tinfo.Setter != null)
			    setterRegion = Class.GetRegion(tinfo.Setter.Location);
			    
			// Add parameters
			foreach (ConstructedTypeInfo pinfo in tinfo.IndexerParameters)
			    parameters.Add(new Parameter(this, pinfo));
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
