
using System;
using Mono.Addins;
using Mono.Addins.Setup;
using MonoDevelop.Core;

namespace MonoDevelop.Core.AddIns
{
	class SetupApp: IApplication
	{
		public int Run (string[] arguments)
		{
			Console.WriteLine ("MonoDevelop Add-in Setup Utility");
			SetupTool tool = new SetupTool (AddinManager.Registry);
			return tool.Run (arguments, 0);
		}
	}
}
