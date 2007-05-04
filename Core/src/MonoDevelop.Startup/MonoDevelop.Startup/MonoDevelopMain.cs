
using System;

using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Startup
{
	public class SharpDevelopMain
	{
		public static int Main (string[] args)
		{
			Runtime.Initialize (true);
			
		//	AddinManager.CheckAssemblyLoadConflicts = true;
			try {
				return Runtime.ApplicationService.StartApplication ("IDE", args);
			} finally {
				Runtime.Shutdown ();
			}
		}
	}
}
