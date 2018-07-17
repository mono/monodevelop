//
// MonoDevelopErrorReportingService.cs
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
using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.RoslynServices
{
	[ExportWorkspaceServiceFactory (typeof (IErrorReportingService), ServiceLayer.Host), Shared]
	sealed class MonoDevelopErrorReportingServiceFactory : IWorkspaceServiceFactory
	{
		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return new MonoDevelopErrorReportingService (workspaceServices.GetRequiredService<IInfoBarService> ());
		}

		sealed class MonoDevelopErrorReportingService : IErrorReportingService
		{
			readonly IInfoBarService _infoBarService;

			public MonoDevelopErrorReportingService (IInfoBarService infoBarService)
			{
				_infoBarService = infoBarService;
			}

			public void ShowErrorInfoInActiveView (string message, params InfoBarUI [] items) =>
				_infoBarService.ShowInfoBarInActiveView (message, items);

			public void ShowGlobalErrorInfo (string message, params InfoBarUI [] items) =>
				_infoBarService.ShowInfoBarInGlobalView (message, items);

			// These are usually analyzers which would crash the process.
			public void ShowDetailedErrorInfo (Exception exception)
			{
				LoggingService.LogError("Roslyn reported an exception to the user", exception);
				
				var logFile = (string)typeof (LoggingService).InvokeMember ("logFile", BindingFlags.GetField | BindingFlags.Static | BindingFlags.NonPublic, null, null, null);

				// If the output is redirected, open the log file, otherwise do not do anything.
				if (logFile != null)
					DesktopService.OpenFile (logFile);
			}
		}
	}
}
