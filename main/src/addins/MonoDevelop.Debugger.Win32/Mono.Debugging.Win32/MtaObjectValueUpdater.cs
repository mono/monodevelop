using Mono.Debugging.Backend;

namespace Mono.Debugging.Win32
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
