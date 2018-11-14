using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Utilities;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (ILoggingServiceInternal))]
	class LoggingServiceInternal : ILoggingServiceInternal
	{
		public void AdjustCounter (string key, string name, int delta = 1)
		{
			throw new NotImplementedException ();
		}

		public void PostCounters ()
		{
			throw new NotImplementedException ();
		}

		public void PostEvent (string key, params object[] namesAndProperties)
		{
			throw new NotImplementedException ();
		}

		public void PostEvent (string key, IReadOnlyList<object> namesAndProperties)
		{
			throw new NotImplementedException ();
		}

		public void PostEvent (TelemetryEventType eventType, string eventName, TelemetryResult result = TelemetryResult.Success, params (string name, object property)[] namesAndProperties)
		{
			throw new NotImplementedException ();
		}

		public void PostEvent (TelemetryEventType eventType, string eventName, TelemetryResult result, IReadOnlyList<(string name, object property)> namesAndProperties)
		{
			throw new NotImplementedException ();
		}

		public void PostFault (string eventName, string description, Exception exceptionObject, string additionalErrorInfo, bool? isIncludedInWatsonSample)
		{
			throw new NotImplementedException ();
		}
	}
}
