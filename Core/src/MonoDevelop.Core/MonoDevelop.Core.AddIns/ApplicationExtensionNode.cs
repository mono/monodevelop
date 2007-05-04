
using System;
using Mono.Addins;

namespace MonoDevelop.Core.AddIns
{
	internal class ApplicationExtensionNode: ExtensionNode, IApplicationInfo
	{
		string typeName;
		string description;
		
		public string TypeName {
			get { return typeName; }
		}
		
		public string Description {
			get { return description; }
		}
		
		protected override void Read (NodeElement elem)
		{
			typeName = elem.GetAttribute ("type");
			if (typeName.Length == 0)
				typeName = elem.GetAttribute ("class");
			if (typeName.Length == 0)
				throw new InvalidOperationException ("Application type not provided");
			description = elem.GetAttribute ("description");
		}
		
		public object CreateInstance ()
		{
			return Activator.CreateInstance (base.Addin.GetType (typeName, true));
		}
	}
}
