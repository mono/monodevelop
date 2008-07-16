using System;

namespace Mono.Debugging.Client
{
	[Serializable]
	public enum TargetEventType
	{
		TargetRunning,
		TargetStopped,
		TargetInterrupted,
		TargetHitBreakpoint,
		TargetSignaled,
		TargetExited,
		ExceptionThrown,
		UnhandledException
	}
}
