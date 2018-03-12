// 
// IPackageManagementEvents.cs
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

using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal interface IPackageManagementEvents
	{
		event EventHandler PackageOperationsStarting;
		event EventHandler PackageOperationsFinished;
		event EventHandler<ResolveFileConflictEventArgs> ResolveFileConflict;
		event EventHandler<PackageOperationExceptionEventArgs> PackageOperationError;
		event EventHandler<PackageOperationMessageLoggedEventArgs> PackageOperationMessageLogged;
		event EventHandler PackagesRestored;
		event EventHandler<FileEventArgs> FileChanged;
		event EventHandler<FileRemovingEventArgs> FileRemoving;
		event EventHandler UpdatedPackagesAvailable;
		event EventHandler<DotNetProjectReferenceEventArgs> ReferenceAdding;
		event EventHandler<DotNetProjectReferenceEventArgs> ReferenceRemoving;
		event EventHandler<DotNetProjectImportEventArgs> ImportAdded;
		event EventHandler<DotNetProjectImportEventArgs> ImportRemoved;
		event EventHandler<PackageManagementEventArgs> PackageInstalled;
		event EventHandler<PackageManagementEventArgs> PackageUninstalling;
		event EventHandler<PackageManagementEventArgs> PackageUninstalled;
		event EventHandler<DotNetProjectEventArgs> NoUpdateFound;

		void OnPackageOperationsStarting();
		void OnPackageOperationsFinished();
		void OnPackageOperationError(Exception ex);
		void OnPackageOperationMessageLogged (MessageLevel level, string message, params object[] args);
		FileConflictAction OnResolveFileConflict(string message);
		void OnPackagesRestored();
		void OnFileChanged(string path);
		void OnUpdatedPackagesAvailable ();
		bool OnFileRemoving (string path);
		void OnReferenceAdding (ProjectReference reference);
		void OnReferenceRemoving (ProjectReference reference);
		void OnImportAdded (IDotNetProject project, string import);
		void OnImportRemoved (IDotNetProject project, string import);
		void OnNoUpdateFound (IDotNetProject project);
	}
}
