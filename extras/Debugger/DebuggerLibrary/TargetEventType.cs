using System;

namespace DebuggerLibrary
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
		FrameChanged,
		Exception,
		UnhandledException
	}
}
