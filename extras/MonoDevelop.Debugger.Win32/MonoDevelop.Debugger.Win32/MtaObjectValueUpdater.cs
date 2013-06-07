using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Win32
{
	class MtaObjectValueUpdater : IObjectValueUpdater
	{
		IObjectValueUpdater source;

		public MtaObjectValueUpdater (IObjectValueUpdater s)
		{
			source = s;
		}

		public void RegisterUpdateCallbacks (UpdateCallback[] callbacks)
		{
			MtaThread.Run (() => source.RegisterUpdateCallbacks (callbacks));
		}
	}
}
