//
// RoslynFileLogger.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.RoslynServices
{
	class RoslynFileLogger : ILogger
	{
		readonly System.IO.TextWriter roslynLog;

		public RoslynFileLogger ()
		{
			roslynLog = LoggingService.CreateLogFile ("roslyn");
		}

		public bool IsEnabled (FunctionId functionId) => true;

		public void Log (FunctionId functionId, LogMessage logMessage)
		{
			roslynLog.WriteLine (string.Format ("[{0}] {1} - {2}", Thread.CurrentThread.ManagedThreadId, functionId.ToString (), logMessage.GetMessage ()));
		}

		public void LogBlockStart (FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
		{
			roslynLog.WriteLine (string.Format ("[{0}] Start({1}) : {2} - {3}", Thread.CurrentThread.ManagedThreadId, uniquePairId, functionId.ToString (), logMessage.GetMessage ()));
		}

		public void LogBlockEnd (FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
		{
			var functionString = functionId.ToString () + (cancellationToken.IsCancellationRequested ? " Canceled" : string.Empty);
			roslynLog.WriteLine (string.Format ("[{0}] End({1}) : [{2}ms] {3}", Thread.CurrentThread.ManagedThreadId, uniquePairId, delta, functionString));
		}
	}
}