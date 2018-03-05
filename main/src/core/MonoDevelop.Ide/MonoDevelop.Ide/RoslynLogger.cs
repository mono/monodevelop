using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using MonoDevelop.Core;

namespace MonoDevelop.Ide
{
	class RoslynLogger : ILogger
	{
		public bool IsEnabled (FunctionId functionId)
		{
			// ? Maybe log more than these exceptions? http://source.roslyn.io/#Microsoft.CodeAnalysis.Workspaces/Log/FunctionId.cs,8
			switch (functionId) {
			case FunctionId.BKTree_ExceptionInCacheRead:
			case FunctionId.StorageDatabase_Exceptions:
			case FunctionId.SymbolTreeInfo_ExceptionInCacheRead:
			case FunctionId.Extension_Exception:
				return true;
			}
			return false;
		}

		public void Log (FunctionId functionId, LogMessage logMessage)
		{
			LoggingService.LogError ("Roslyn error: {0} {1}", functionId.ToString(), logMessage?.GetMessage ());
		}

		public void LogBlockEnd (FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
		{
			// Fixme at some point
			LoggingService.LogError ("Roslyn error: {0} {1}", functionId.ToString (), logMessage?.GetMessage ());
		}

		public void LogBlockStart (FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
		{
			// Fixme at some point
			LoggingService.LogError ("Roslyn error: {0} {1}", functionId.ToString (), logMessage?.GetMessage ());
		}
	}
}
