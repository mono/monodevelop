
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
			try {
				return Runtime.AddInService.StartApplication ("IDE", args);
			} finally {
				Runtime.Shutdown ();
			}
		}
	}
}
