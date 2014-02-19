// 
// PackageViewModelOperationLogger.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
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

using System;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class PackageViewModelOperationLogger : ILogger
	{
		ILogger logger;
		IPackage package;
		
		public PackageViewModelOperationLogger(ILogger logger, IPackage package)
		{
			this.logger = logger;
			this.package = package;
			
			GetMessageFormats();
		}

		void GetMessageFormats()
		{
			AddingPackageMessageFormat = "Installing...{0}";
			RemovingPackageMessageFormat = "Uninstalling...{0}";
			ManagingPackageMessageFormat = "Managing...{0}";
		}
		
		public string AddingPackageMessageFormat { get; set; }
		public string RemovingPackageMessageFormat { get; set; }
		public string ManagingPackageMessageFormat { get; set; }
		
		public void Log(MessageLevel level, string message, params object[] args)
		{
			logger.Log(level, message, args);
		}
		
		public void LogInformation(string message)
		{
			Log(MessageLevel.Info, message);
		}
		
		public void LogAfterPackageOperationCompletes()
		{
			LogEndMarkerLine();
			LogEmptyLine();
		}
		
		void LogEndMarkerLine()
		{
			string message = new String('=', 30);
			LogInformation(message);
		}

		void LogEmptyLine()
		{
			LogInformation(String.Empty);
		}
		
		public void LogAddingPackage()
		{
			string message = GetFormattedStartPackageOperationMessage(AddingPackageMessageFormat);
			LogInformation(message);
		}
		
		string GetFormattedStartPackageOperationMessage(string format)
		{
			string message = String.Format(format, package.ToString());
			return GetStartPackageOperationMessage(message);
		}
		
		string GetStartPackageOperationMessage(string message)
		{
			return String.Format("------- {0} -------", message);
		}
		
		public void LogRemovingPackage()
		{
			string message =  GetFormattedStartPackageOperationMessage(RemovingPackageMessageFormat);
			LogInformation(message);
		}
		
		public void LogError(Exception ex)
		{
			LogInformation(ex.ToString());
		}
		
		public void LogManagingPackage()
		{
			string message =  GetFormattedStartPackageOperationMessage(ManagingPackageMessageFormat);
			LogInformation(message);
		}
		
		public FileConflictResolution ResolveFileConflict(string message)
		{
			return logger.ResolveFileConflict(message);
		}
	}
}
