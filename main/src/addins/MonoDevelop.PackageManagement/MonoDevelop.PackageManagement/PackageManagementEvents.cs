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
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementEvents : IPackageManagementEvents
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
		
		public event EventHandler<PackageOperationMessageLoggedEventArgs> PackageOperationMessageLogged;
		
		public void OnPackageOperationMessageLogged (MessageLevel level, string message, params object[] args)
		{
			if (PackageOperationMessageLogged != null) {
				var eventArgs = new PackageOperationMessageLoggedEventArgs(level, message, args);
				PackageOperationMessageLogged(this, eventArgs);
			}
		}
		
		public event EventHandler<ResolveFileConflictEventArgs> ResolveFileConflict;
		
		public FileConflictAction OnResolveFileConflict(string message)
		{
			if (ResolveFileConflict != null) {
				var eventArgs = new ResolveFileConflictEventArgs(message);
				ResolveFileConflict(this, eventArgs);
				return eventArgs.Resolution;
			}
			return FileConflictAction.IgnoreAll;
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

		public event EventHandler<DotNetProjectImportEventArgs> ImportAdded;

		public void OnImportAdded (IDotNetProject project, string import)
		{
			ImportAdded?.Invoke (this, new DotNetProjectImportEventArgs (project, import));
		}

		public event EventHandler<DotNetProjectImportEventArgs> ImportRemoved;

		public void OnImportRemoved (IDotNetProject project, string import)
		{
			if (ImportRemoved != null) {
				ImportRemoved (this, new DotNetProjectImportEventArgs (project, import));
			}
		}

		public event EventHandler<PackageManagementEventArgs> PackageInstalled;

		public void OnPackageInstalled (IDotNetProject project, NuGet.ProjectManagement.PackageEventArgs e)
		{
			PackageInstalled?.Invoke (this, new PackageManagementEventArgs (project, e));
		}

		public event EventHandler<PackageManagementEventArgs> PackageUninstalling;

		public void OnPackageUninstalling (IDotNetProject project, NuGet.ProjectManagement.PackageEventArgs e)
		{
			PackageUninstalling?.Invoke (this, new PackageManagementEventArgs (project, e));
		}

		public event EventHandler<PackageManagementEventArgs> PackageUninstalled;

		public void OnPackageUninstalled (IDotNetProject project, NuGet.ProjectManagement.PackageEventArgs e)
		{
			PackageUninstalled?.Invoke (this, new PackageManagementEventArgs (project, e));
		}

		public event EventHandler<DotNetProjectEventArgs> NoUpdateFound;

		public void OnNoUpdateFound (IDotNetProject project)
		{
			NoUpdateFound?.Invoke (this, new DotNetProjectEventArgs (project));
		}
	}
}
