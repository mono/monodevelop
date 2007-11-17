// project created on 16/07/2006 at 13:33
using System;
using Mono.Addins;
using Mono.Addins.Setup;

namespace mautil
{
	class MainClass
	{
		public static int Main(string[] args)
		{
			if (args.Length == 0 || args [0] == "--help" || args [0] == "help") {
				Console.WriteLine ("Mono.Addins Setup Utility");
				Console.WriteLine ("Usage: mautil [options] <command> [arguments]");
				Console.WriteLine ();
				Console.WriteLine ("Options:");
				Console.WriteLine ("  --registry (-reg) Specify add-in registry path");
				Console.WriteLine ("  -v                Verbose output");
			}
			
			int ppos = 0;
			
			bool verbose = false;
			foreach (string a in args)
				if (a == "-v")
					verbose = true;
			
			string path = null;
			bool toolParam = true;
			
			while (toolParam && ppos < args.Length)
			{
				if (args [ppos] == "-reg" || args [ppos] == "--registry") {
					if (ppos + 1 >= args.Length) {
						Console.WriteLine ("Registry path not provided.");
						return 1;
					}
					path = args [ppos + 1];
					ppos += 2;
				}
				else if (args [ppos] == "-v") {
					verbose = true;
					ppos++;
				} else
					toolParam = false;
			}
			
			AddinRegistry reg = path != null ? new AddinRegistry (path) : AddinRegistry.GetGlobalRegistry ();
			try {
				SetupTool setupTool = new SetupTool (reg);
				setupTool.VerboseOutput = verbose;
				return setupTool.Run (args, ppos);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return -1;
			}
			finally {
				reg.Dispose ();
			}
		}
	}
}