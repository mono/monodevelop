using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting.Channels;
using System.Collections;

using DL = DebuggerLibrary;

namespace DebuggerClient
{
	public delegate void TargetEventHandler (DL.TargetEventArgs args);
	public delegate void ProcessEventHandler(int process_id);
	public delegate void ThreadEventHandler(int thread_id);

	//FIXME: name
	public class Debugger
	{
		public DebuggerSession StartSession (string [] args)
		{
			if (args == null)
				throw new ArgumentNullException ("args");

			DebuggerController controller = new DebuggerController();
			return controller.StartSession (args);
		}
	}
}
