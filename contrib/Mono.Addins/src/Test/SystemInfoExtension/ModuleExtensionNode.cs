
using System;
using Mono.Addins;

namespace SystemInfoExtension
{
	public class ModuleExtensionNode: ExtensionNode
	{
		[NodeAttribute]
		string name;
		
		[NodeAttribute (Required=false)]
		string version;

		public string Name {
			get { return name; }
		}
		
		public string Version {
			get { return version; }
		}
	}
}
