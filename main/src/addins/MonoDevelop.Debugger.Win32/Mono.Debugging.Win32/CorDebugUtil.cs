using System;
using System.Runtime.InteropServices;
using Mono.Debugging.Client;

namespace Mono.Debugging.Win32
{
	public static class CorDebugUtil
	{
		public static T CallHandlingComExceptions<T> (Func<T> factory, string callName, T defaultValue = default(T))
		{
			try {
				return factory ();
			} catch (COMException e) {
				DebuggerLoggingService.LogMessage ("Exception in {0}: {1}", callName, e.Message);
				return defaultValue;
			}
		}
	}
}