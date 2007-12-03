
using System;
using Mono.Addins;

namespace SystemInfoExtension
{
	public class ModuleExtensionNode: ExtensionNode
	{
		// Not using NodeAttribute here to avoid caching localized strings
		string name;
		
		[NodeAttribute (Required=false)]
		protected string version;

		public string Name {
			get { return Addin.Localizer.GetString (name); }
		}
		
		public string Version {
			get { return version; }
		}
		
		public override string ToString ()
		{
			return name;
		}
		
		protected override void Read (NodeElement elem)
		{
			base.Read (elem);
			name = elem.GetAttribute ("name");
		}

	}
}
