
using System;
using SimpleApp;
using Mono.Addins;

namespace SystemInfoExtension
{
	public class SystemInfoWriter: IWriter
	{
		public string Title {
			get { return "Modules"; }
		}
		
		public string Write ()
		{
			string s = "System Info:";
			foreach (ModuleExtensionNode node in AddinManager.GetExtensionNodes ("/SystemInformation/Modules"))
				s += " " + node.Name;
			return s;
		}
	}
}
