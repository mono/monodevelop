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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using NuGet;
using MonoDevelop.Projects;

namespace ICSharpCode.PackageManagement
{
	public interface IPackageManagementEvents
	{
		event EventHandler PackageOperationsStarting;
		event EventHandler PackageOperationsFinished;
		event EventHandler<AcceptLicensesEventArgs> AcceptLicenses;
		event EventHandler<SelectProjectsEventArgs> SelectProjects;
		event EventHandler<ResolveFileConflictEventArgs> ResolveFileConflict;
		event EventHandler<PackageOperationExceptionEventArgs> PackageOperationError;
		event EventHandler<ParentPackageOperationEventArgs> ParentPackageInstalled;
		event EventHandler<ParentPackageOperationEventArgs> ParentPackageUninstalled;
		event EventHandler<ParentPackagesOperationEventArgs> ParentPackagesUpdated;
		event EventHandler<PackageOperationMessageLoggedEventArgs> PackageOperationMessageLogged;
		event EventHandler PackagesRestored;
		event EventHandler<FileEventArgs> FileChanged;
		event EventHandler<FileRemovingEventArgs> FileRemoving;
		event EventHandler UpdatedPackagesAvailable;
		event EventHandler<PackageRestoredEventArgs> PackageRestored;
		event EventHandler<DotNetProjectReferenceEventArgs> ReferenceAdding;
		event EventHandler<DotNetProjectReferenceEventArgs> ReferenceRemoving;
		event EventHandler<DotNetProjectImportEventArgs> ImportRemoved;

		void OnPackageOperationsStarting();
		void OnPackageOperationsFinished();
		void OnPackageOperationError(Exception ex);
		bool OnAcceptLicenses(IEnumerable<IPackage> packages);
		void OnParentPackageInstalled (IPackage package, IPackageManagementProject project, IEnumerable<PackageOperation> operations);
		void OnParentPackageUninstalled(IPackage package, IPackageManagementProject project);
		void OnParentPackagesUpdated(IEnumerable<IPackage> packages);
		void OnPackageOperationMessageLogged(MessageLevel level, string message, params object[] args);
		bool OnSelectProjects(IEnumerable<IPackageManagementSelectedProject> selectedProjects);
		FileConflictResolution OnResolveFileConflict(string message);
		void OnPackagesRestored();
		void OnFileChanged(string path);
		void OnUpdatedPackagesAvailable ();
		bool OnFileRemoving (string path);
		void OnPackageRestored (IPackage package);
		void OnReferenceAdding (ProjectReference reference);
		void OnReferenceRemoving (ProjectReference reference);
		void OnImportRemoved (IDotNetProject project, string import);

		[Obsolete]
		void OnParentPackageInstalled (IPackage package, IPackageManagementProject project);
	}
}
