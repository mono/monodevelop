using System;
using System.Reflection;
using System.Xml;

namespace Stetic
{
	[Serializable]
	public abstract class SignalDescriptor: ItemDescriptor
	{
		protected string name, label, description;
		protected string handlerTypeName;
		protected string handlerReturnTypeName;
		protected ParameterDescriptor[] handlerParameters;
		
		public SignalDescriptor (XmlElement elem, ItemGroup group, ClassDescriptor klass) : base (elem, group, klass)
		{
		}
		
		protected virtual void Load (XmlElement elem)
		{
			name = elem.GetAttribute ("name");
			label = elem.GetAttribute ("label");
			description = elem.GetAttribute ("description");
		}
		
		public override string Name {
			get { return name; }
		}

		public string Label {
			get { return label; }
		}

		public string Description {
			get { return description; }
		}
		
		public string HandlerTypeName {
			get { return handlerTypeName; }
		}
		
		public string HandlerReturnTypeName {
			get { return handlerReturnTypeName; }
		}

		public ParameterDescriptor[] HandlerParameters {
			get { return handlerParameters; }
		}
	}	

	[Serializable]
	public class ParameterDescriptor
	{
		string name, type;
		
		public ParameterDescriptor (string name, string type)
		{
			this.name = name;
			this.type = type;
		}
		
		public string Name {
			get { return name; }
		}
		
		public string TypeName {
			get { return type; }
		}
	}
}
