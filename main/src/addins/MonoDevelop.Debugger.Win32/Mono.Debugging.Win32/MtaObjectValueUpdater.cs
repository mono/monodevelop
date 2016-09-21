using Mono.Debugging.Backend;

namespace MonoDevelop.Debugger.Win32
{
	class MtaObjectValueUpdater : IObjectValueUpdater
	{
		readonly IObjectValueUpdater source;

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
