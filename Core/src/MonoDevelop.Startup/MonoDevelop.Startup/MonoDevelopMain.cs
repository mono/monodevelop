
using System;

using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Startup
{
	public class SharpDevelopMain
	{
		public static int Main (string[] args)
		{
			Runtime.Initialize ();
			Runtime.AddInService.CheckAssemblyLoadConflicts = true;
			return Runtime.AddInService.StartApplication ("IDE", args);
		}
	}
}
