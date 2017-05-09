// 
// PackageManagementLogger.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using NuGet.Common;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementLogger : ILogger
	{
		IPackageManagementEvents packageManagementEvents;
		
		public PackageManagementLogger(IPackageManagementEvents packageManagementEvents)
		{
			this.packageManagementEvents = packageManagementEvents;
		}
		
		public void Log(MessageLevel level, string message, params object[] args)
		{
			packageManagementEvents.OnPackageOperationMessageLogged(level, message, args);
		}

		public void LogDebug (string data)
		{
			Log (MessageLevel.Debug, data);
		}

		public void LogError (string data)
		{
			Log (MessageLevel.Error, data);
		}

		public void LogInformation (string data)
		{
			Log (MessageLevel.Info, data);
		}

		public void LogVerbose (string data)
		{
			Log (MessageLevel.Debug, data);
		}

		public void LogWarning (string data)
		{
			Log (MessageLevel.Warning, data);
		}

		public void LogMinimal (string data)
		{
			LogInformation (data);
		}

		public void LogInformationSummary (string data)
		{
			LogDebug (data);
		}

		public void LogErrorSummary (string data)
		{
			LogDebug (data);
		}
	}
}
