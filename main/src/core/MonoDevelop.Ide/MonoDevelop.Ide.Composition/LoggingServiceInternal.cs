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
		}

		public void PostCounters ()
		{
		}

		public void PostEvent (string key, params object[] namesAndProperties)
		{
		}

		public void PostEvent (string key, IReadOnlyList<object> namesAndProperties)
		{
		}

		public void PostEvent (TelemetryEventType eventType, string eventName, TelemetryResult result = TelemetryResult.Success, params (string name, object property)[] namesAndProperties)
		{
		}

		public void PostEvent (TelemetryEventType eventType, string eventName, TelemetryResult result, IReadOnlyList<(string name, object property)> namesAndProperties)
		{
		}

		public void PostFault (string eventName, string description, Exception exceptionObject, string additionalErrorInfo, bool? isIncludedInWatsonSample)
		{
		}
		// This is new API, since our Mac source code is newer, we need to implement more stuff then Windows
		// remove #if MAC once upgrading NuGets on Windows
#if MAC
		public object CreateTelemetryOperationEventScope (string eventName, TelemetrySeverity severity, object [] correlations, IDictionary<string, object> startingProperties)
		{
			throw new NotImplementedException ();
		}

		public void EndTelemetryScope (object telemetryScope, TelemetryResult result, string summary = null)
		{
			throw new NotImplementedException ();
		}

		public object GetCorrelationFromTelemetryScope (object telemetryScope)
		{
			throw new NotImplementedException ();
		}

		public void PostFault (string eventName, string description, Exception exceptionObject, string additionalErrorInfo = null, bool? isIncludedInWatsonSample = null, object [] correlations = null)
		{
			throw new NotImplementedException ();
		}
#endif
	}
}
