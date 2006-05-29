// created on 06.08.2003 at 12:30

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Event : AbstractEvent
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Event (IClass declaringType, SR.EventInfo tinfo)
		{
		    this.declaringType = declaringType;
		
		    ModifierEnum mod = (ModifierEnum)0;
			modifiers = mod;
			
			this.FullyQualifiedName = tinfo.Name;
			returnType = new ReturnType(tinfo.EventHandlerType);
			this.region = Class.GetRegion();
			this.bodyRegion = Class.GetRegion();
		}
		
		public Event (IClass declaringType, EventInfo tinfo)
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
            if (tinfo.IsStatic)
                mod |= ModifierEnum.Static;
                
			modifiers = mod;
			
			this.FullyQualifiedName = tinfo.Name;
			returnType = new ReturnType(tinfo.Type);
			this.region = Class.GetRegion(tinfo.Location);
			this.bodyRegion = Class.GetRegion(tinfo.Location);
		}
	}
}
