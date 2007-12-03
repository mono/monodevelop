
using System;
using SimpleApp;
using Mono.Addins;

namespace SystemInfoExtension
{
	public class SystemInfoWriter: IWriter
	{
		public string Title {
			get { return AddinManager.CurrentLocalizer.GetString ("Modules"); }
		}
		
		public string Write ()
		{
			string s = "System Info:";
			foreach (ModuleExtensionNode node in AddinManager.GetExtensionNodes ("/SystemInformation/Modules"))
				s += " " + node.Name;
			return s;
		}
		
		public string Test (string test)
		{
			return test;
		}
	}
}
