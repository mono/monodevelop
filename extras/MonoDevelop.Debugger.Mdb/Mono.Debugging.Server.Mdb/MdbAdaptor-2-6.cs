/*

WARNING WARNING WARNING 
  
This class is compiled by the debugger add-in at runtime. It is done in
this way because features implemented here depend on the installed MDB version.
   
*/

using System;
using Mono.Debugger;

namespace DebuggerServer
{
	public class MdbAdaptor_2_6: MdbAdaptor
	{
		public override void SetupXsp (DebuggerConfiguration config)
		{
			config.SetupXSP ();
			Console.WriteLine ("XSP configured");
		}
	}
}
