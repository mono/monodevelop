using System;

namespace Mono.Debugging.Client
{
	[Serializable]
	public enum TargetEventType
	{
		TargetReady,
		TargetStopped,
		TargetInterrupted,
		TargetHitBreakpoint,
		TargetSignaled,
		TargetExited,
		ExceptionThrown,
		UnhandledException,
		ThreadStarted,
		ThreadStopped
	}
}
