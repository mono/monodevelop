using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoDevelop;
using System.IO;

namespace MacCrashLogger
{
	class MainClass
	{
		static void Main (string [] args)
		{
			string error;
			if (!OptionsParser.TryParse (args, out error)) {
				Console.WriteLine (error);
				return;
			}
			
			NSApplication.Init ();
			NSApplication.Main (args);
		}
	}
}	

