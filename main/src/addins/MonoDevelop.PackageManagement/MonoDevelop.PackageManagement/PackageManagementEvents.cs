// 
// PackageManagementEvents.cs
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
using NuGet;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;

namespace ICSharpCode.PackageManagement
{
	public class PackageManagementEvents : IPackageManagementEvents
	{
		public event EventHandler PackageOperationsStarting;
		
		public void OnPackageOperationsStarting()
		{
			if (PackageOperationsStarting != null) {
				PackageOperationsStarting(this, new EventArgs());
			}
		}

		public event EventHandler PackageOperationsFinished;

		public void OnPackageOperationsFinished()
		{
			if (PackageOperationsFinished != null) {
				PackageOperationsFinished(this, new EventArgs());
			}
		}
		
		public event EventHandler<PackageOperationExceptionEventArgs> PackageOperationError;
		
		public void OnPackageOperationError(Exception ex)
		{
			if (PackageOperationError != null) {
				PackageOperationError(this, new PackageOperationExceptionEventArgs(ex));
			}
		}
		
		public event EventHandler<AcceptLicensesEventArgs> AcceptLicenses;
		
		public bool OnAcceptLicenses(IEnumerable<IPackage> packages)
		{
			if (AcceptLicenses != null) {
				var eventArgs = new AcceptLicensesEventArgs(packages);
				AcceptLicenses(this, eventArgs);
				return eventArgs.IsAccepted;
			}
			return true;
		}
		
		public event EventHandler<ParentPackageOperationEventArgs> ParentPackageInstalled;
		
		public void OnParentPackageInstalled(IPackage package, IPackageManagementProject project)
		{
			if (ParentPackageInstalled != null) {
				ParentPackageInstalled(this, new ParentPackageOperationEventArgs(package, project));
			}
		}

		public void OnParentPackageInstalled (IPackage package, IPackageManagementProject project, IEnumerable<PackageOperation> operations)
		{
			if (ParentPackageInstalled != null) {
				ParentPackageInstalled (this, new ParentPackageOperationEventArgs(package, project, operations));
			}
		}

		public event EventHandler<ParentPackageOperationEventArgs> ParentPackageUninstalled;
		
		public void OnParentPackageUninstalled(IPackage package, IPackageManagementProject project)
		{
			if (ParentPackageUninstalled != null) {
				ParentPackageUninstalled(this, new ParentPackageOperationEventArgs(package, project));
			}
		}
		
		public event EventHandler<PackageOperationMessageLoggedEventArgs> PackageOperationMessageLogged;
		
		public void OnPackageOperationMessageLogged(MessageLevel level, string message, params object[] args)
		{
			if (PackageOperationMessageLogged != null) {
				var eventArgs = new PackageOperationMessageLoggedEventArgs(level, message, args);
				PackageOperationMessageLogged(this, eventArgs);
			}
		}
		
		public event EventHandler<SelectProjectsEventArgs> SelectProjects;
		
		public bool OnSelectProjects(IEnumerable<IPackageManagementSelectedProject> projects)
		{
			if (SelectProjects != null) {
				var eventArgs = new SelectProjectsEventArgs(projects);
				SelectProjects(this, eventArgs);
				return eventArgs.IsAccepted;
			}
			return true;
		}
		
		public event EventHandler<ResolveFileConflictEventArgs> ResolveFileConflict;
		
		public FileConflictResolution OnResolveFileConflict(string message)
		{
			if (ResolveFileConflict != null) {
				var eventArgs = new ResolveFileConflictEventArgs(message);
				ResolveFileConflict(this, eventArgs);
				return eventArgs.Resolution;
			}
			return FileConflictResolution.IgnoreAll;
		}
		
		public event EventHandler<ParentPackagesOperationEventArgs> ParentPackagesUpdated;
		
		public void OnParentPackagesUpdated(IEnumerable<IPackage> packages)
		{
			if (ParentPackagesUpdated != null) {
				ParentPackagesUpdated(this, new ParentPackagesOperationEventArgs(packages));
			}
		}

		public event EventHandler PackagesRestored;

		public void OnPackagesRestored()
		{
			if (PackagesRestored != null) {
				PackagesRestored(this, new EventArgs());
			}
		}

		public event EventHandler<FileEventArgs> FileChanged;

		public void OnFileChanged (string path)
		{
			if (FileChanged != null) {
				FileChanged (this, new FileEventArgs (new FilePath (path), false));
			}
		}

		public event EventHandler UpdatedPackagesAvailable;

		public void OnUpdatedPackagesAvailable ()
		{
			if (UpdatedPackagesAvailable != null) {
				UpdatedPackagesAvailable (this, new EventArgs ());
			}
		}

		public event EventHandler<FileRemovingEventArgs> FileRemoving;

		public bool OnFileRemoving (string path)
		{
			if (FileRemoving != null) {
				var eventArgs = new FileRemovingEventArgs (path);
				FileRemoving (this, eventArgs);
				return !eventArgs.IsCancelled;
			}
			return true;
		}

		public event EventHandler<PackageRestoredEventArgs> PackageRestored;

		public void OnPackageRestored (IPackage package)
		{
			if (PackageRestored != null) {
				PackageRestored (this, new PackageRestoredEventArgs (package));
			}
		}

		public event EventHandler<DotNetProjectReferenceEventArgs> ReferenceRemoving;

		public void OnReferenceRemoving (ProjectReference reference)
		{
			if (ReferenceRemoving != null) {
				ReferenceRemoving (this, new DotNetProjectReferenceEventArgs (reference));
			}
		}

		public event EventHandler<DotNetProjectReferenceEventArgs> ReferenceAdding;

		public void OnReferenceAdding (ProjectReference reference)
		{
			if (ReferenceAdding != null) {
				ReferenceAdding (this, new DotNetProjectReferenceEventArgs (reference));
			}
		}

		public event EventHandler<DotNetProjectImportEventArgs> ImportRemoved;

		public void OnImportRemoved (IDotNetProject project, string import)
		{
			if (ImportRemoved != null) {
				ImportRemoved (this, new DotNetProjectImportEventArgs (project, import));
			}
		}
	}
}
