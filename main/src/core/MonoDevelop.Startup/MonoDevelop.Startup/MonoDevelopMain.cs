
using System;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using Mono.Addins;
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