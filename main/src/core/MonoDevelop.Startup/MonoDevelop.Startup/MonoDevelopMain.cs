
using System;
using MonoDevelop.Ide;

namespace MonoDevelop.Startup
{
	public class MonoDevelopMain
	{
		[STAThread]
		public static int Main (string[] args)
		{
			return IdeStartup.Main (args);
		}
	}
}