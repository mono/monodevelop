// created on 06.08.2003 at 12:30

using MonoDevelop.Projects.Parser;
using Nemerle.Completion;
using SR = System.Reflection;
using NCC = Nemerle.Compiler;

using System.Xml;

namespace NemerleBinding.Parser.SharpDevelopTree
{
	public class Event : DefaultEvent
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		void LoadXml (Class declaring)
		{
			if (declaring.xmlHelp != null) {
				XmlNode node = declaring.xmlHelp.SelectSingleNode ("/Type/Members/Member[@MemberName='" + FullyQualifiedName + "']/Docs/summary");
				if (node != null) {
					Documentation = node.InnerXml;
				}
			}
		}
		
		public Event (Class declaringType, SR.EventInfo tinfo)
		{
		    this.declaringType = declaringType;
		
		    ModifierEnum mod = (ModifierEnum)0;
			modifiers = mod;
			
			this.FullyQualifiedName = tinfo.Name;
			returnType = new ReturnType(tinfo.EventHandlerType);
			this.region = Class.GetRegion();
			this.bodyRegion = Class.GetRegion();
			
			LoadXml (declaringType);
		}
		
		public Event (Class declaringType, NCC.IEvent tinfo)
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
			
			this.FullyQualifiedName = tinfo.Name;
			returnType = new ReturnType (tinfo.GetMemType ());
			this.region = Class.GetRegion (tinfo.Location);
            if (tinfo is NCC.MemberBuilder)
                this.bodyRegion = Class.GetRegion (((NCC.MemberBuilder)tinfo).BodyLocation);
            else
                this.bodyRegion = Class.GetRegion (tinfo.Location);
                
            LoadXml (declaringType);
		}
	}
}
