//
// RoslynService.cs
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
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Utilities;
using MonoDevelop.Core;
using MonoDevelop.Ide.RoslynServices.Options;

namespace MonoDevelop.Ide.RoslynServices
{
	class RoslynService
	{
		internal RoslynService ()
		{
			// Initialize Option Persisters
		}

		internal static IEnumerable<string> AllLanguages {
			get {
				yield return LanguageNames.CSharp;
				yield return LanguageNames.FSharp;
				yield return LanguageNames.VisualBasic;
			}
		}

		static int initialized;
		internal static void Initialize ()
		{
			if (Interlocked.CompareExchange (ref initialized, 1, 0) == 1)
				return;

			// Maybe we should crash here?
			FatalError.Handler = exception => LoggingService.LogInternalError ("Roslyn fatal exception", exception);
			FatalError.NonFatalHandler = exception => LoggingService.LogInternalError ("Roslyn non-fatal exception", exception);

			AttachLoggers ();

			// Initialize Roslyn foreground thread data.
			ForegroundThreadAffinitizedObject.CurrentForegroundThreadData = new ForegroundThreadData (
				Runtime.MainThread,
				Runtime.MainTaskScheduler,
				ForegroundThreadDataInfo.CreateDefault (ForegroundThreadDataKind.ForcedByPackageInitialize)
			);
		}

		static void AttachLoggers ()
		{
			var fullRoslynEnvValue = Environment.GetEnvironmentVariable ("MONODEVELOP_FULL_ROSLYN_LOG");
			var fullRoslynLogEnabled = bool.TryParse (fullRoslynEnvValue, out var value) && value;

			ILogger[] loggers;
			if (fullRoslynLogEnabled) {
				loggers = new ILogger[] {
					new RoslynLogger (),
					new RoslynFileLogger (),
					Logger.GetLogger ()
				};
			} else {
				loggers = new ILogger[] {
					new RoslynLogger (),
					Logger.GetLogger ()
				};
			}

			Logger.SetLogger (AggregateLogger.Create (loggers));
		}
	}
}
