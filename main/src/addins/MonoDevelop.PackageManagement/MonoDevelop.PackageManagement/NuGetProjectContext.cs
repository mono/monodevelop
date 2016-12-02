//
// NuGetProjectContext.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Xml.Linq;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class NuGetProjectContext : INuGetProjectContext
	{
		IPackageManagementEvents packageManagementEvents;
		IDEExecutionContext executionContext;

		public NuGetProjectContext ()
		{
			packageManagementEvents = PackageManagementServices.PackageManagementEvents;
			var commonOperations = new MonoDevelopCommonOperations ();
			executionContext = new IDEExecutionContext (commonOperations);
		}

		public ExecutionContext ExecutionContext {
			get { return executionContext; }
		}

		public NuGetActionType ActionType { get; set; }
		public XDocument OriginalPackagesConfig { get; set; }
		public PackageExtractionContext PackageExtractionContext { get; set; }
		public TelemetryServiceHelper TelemetryService { get; set; }

		public ISourceControlManagerProvider SourceControlManagerProvider {
			get { return null; }
		}

		public void Log (MessageLevel level, string message, params object [] args)
		{
			packageManagementEvents.OnPackageOperationMessageLogged (level, message, args);
		}

		public void ReportError (string message)
		{
			packageManagementEvents.OnPackageOperationMessageLogged (MessageLevel.Error, message);
		}

		public FileConflictAction? FileConflictResolution { get; set; }

		public FileConflictAction ResolveFileConflict (string message)
		{
			if (FileConflictResolution.HasValue && FileConflictResolution != FileConflictAction.PromptUser)
				return FileConflictResolution.Value;

			return packageManagementEvents.OnResolveFileConflict (message);
		}
	}
}

