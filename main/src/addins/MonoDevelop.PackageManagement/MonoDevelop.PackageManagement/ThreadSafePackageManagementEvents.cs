// 
// ThreadSafePackageManagementEvents.cs
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
using System.Collections.Generic;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	//TODO use UI thread.
	public class ThreadSafePackageManagementEvents : IThreadSafePackageManagementEvents
	{
		IPackageManagementEvents unsafeEvents;
		
		public ThreadSafePackageManagementEvents(IPackageManagementEvents unsafeEvents)
		{
			this.unsafeEvents = unsafeEvents;
			
			RegisterEventHandlers();
		}
		
		void RegisterEventHandlers()
		{
			unsafeEvents.PackageOperationsStarting += RaisePackageOperationStartingEventIfHasSubscribers;
			unsafeEvents.PackageOperationError += RaisePackageOperationErrorEventIfHasSubscribers;
			unsafeEvents.ParentPackageInstalled += RaiseParentPackageInstalledEventIfHasSubscribers;
			unsafeEvents.ParentPackageUninstalled += RaiseParentPackageUninstalledEventIfHasSubscribers;
			unsafeEvents.ParentPackagesUpdated += RaiseParentPackagesUpdatedEventIfHasSubscribers;
		}
		
		public void Dispose()
		{
			UnregisterEventHandlers();
		}
		
		void UnregisterEventHandlers()
		{
			unsafeEvents.PackageOperationsStarting -= RaisePackageOperationStartingEventIfHasSubscribers;
			unsafeEvents.PackageOperationError -= RaisePackageOperationErrorEventIfHasSubscribers;
			unsafeEvents.ParentPackageInstalled -= RaiseParentPackageInstalledEventIfHasSubscribers;
			unsafeEvents.ParentPackageUninstalled -= RaiseParentPackageUninstalledEventIfHasSubscribers;
			unsafeEvents.ParentPackagesUpdated -= RaiseParentPackagesUpdatedEventIfHasSubscribers;
		}
		
		void RaisePackageOperationStartingEventIfHasSubscribers(object sender, EventArgs e)
		{
			if (PackageOperationsStarting != null) {
				RaisePackageOperationStartingEvent(sender, e);
			}
		}
		
		void RaisePackageOperationStartingEvent(object sender, EventArgs e)
		{
			PackageOperationsStarting(sender, e);
		}
		
		public event EventHandler PackageOperationsStarting;
		
		void RaisePackageOperationErrorEventIfHasSubscribers(object sender, PackageOperationExceptionEventArgs e)
		{
			if (PackageOperationError != null) {
				RaisePackageOperationErrorEvent(sender, e);
			}
		}
		
		void RaisePackageOperationErrorEvent(object sender, PackageOperationExceptionEventArgs e)
		{
			if (PackageOperationError != null) {
				PackageOperationError(sender, e);
			}
		}
		
		public event EventHandler<PackageOperationExceptionEventArgs> PackageOperationError;
		
		void RaiseParentPackageInstalledEventIfHasSubscribers(object sender, ParentPackageOperationEventArgs e)
		{
			if (ParentPackageInstalled != null) {
				RaiseParentPackageInstalledEvent(sender, e);
			}
		}
		
		void RaiseParentPackageInstalledEvent(object sender, ParentPackageOperationEventArgs e)
		{
			ParentPackageInstalled(sender, e);
		}
		
		public event EventHandler<ParentPackageOperationEventArgs> ParentPackageInstalled;
		
		void RaiseParentPackageUninstalledEventIfHasSubscribers(object sender, ParentPackageOperationEventArgs e)
		{
			if (ParentPackageUninstalled != null) {
				RaiseParentPackageUninstalledEvent(sender, e);
			}
		}
		
		void RaiseParentPackageUninstalledEvent(object sender, ParentPackageOperationEventArgs e)
		{
			ParentPackageUninstalled(sender, e);
		}
		
		public event EventHandler<ParentPackageOperationEventArgs> ParentPackageUninstalled;
		
		public event EventHandler<AcceptLicensesEventArgs> AcceptLicenses {
			add { unsafeEvents.AcceptLicenses += value; }
			remove { unsafeEvents.AcceptLicenses -= value; }
		}
		
		public event EventHandler<PackageOperationMessageLoggedEventArgs> PackageOperationMessageLogged {
			add { unsafeEvents.PackageOperationMessageLogged += value; }
			remove { unsafeEvents.PackageOperationMessageLogged -= value; }
		}
		
		public event EventHandler<SelectProjectsEventArgs> SelectProjects {
			add { unsafeEvents.SelectProjects += value; }
			remove { unsafeEvents.SelectProjects -= value; }
		}
		
		public void OnPackageOperationsStarting()
		{
			unsafeEvents.OnPackageOperationsStarting();
		}
		
		public void OnPackageOperationError(Exception ex)
		{
			unsafeEvents.OnPackageOperationError(ex);
		}
		
		public bool OnAcceptLicenses(IEnumerable<IPackage> packages)
		{
			return unsafeEvents.OnAcceptLicenses(packages);
		}
		
		public void OnParentPackageInstalled(IPackage package)
		{
			unsafeEvents.OnParentPackageInstalled(package);
		}
		
		public void OnParentPackageUninstalled(IPackage package)
		{
			unsafeEvents.OnParentPackageUninstalled(package);
		}
		
		public void OnPackageOperationMessageLogged(MessageLevel level, string message, params object[] args)
		{
			unsafeEvents.OnPackageOperationMessageLogged(level, message, args);
		}
		
		public bool OnSelectProjects(IEnumerable<IPackageManagementSelectedProject> selectedProjects)
		{
			return unsafeEvents.OnSelectProjects(selectedProjects);
		}
		
		public event EventHandler<ResolveFileConflictEventArgs> ResolveFileConflict {
			add { unsafeEvents.ResolveFileConflict += value; }
			remove { unsafeEvents.ResolveFileConflict -= value; }
		}
		
		public FileConflictResolution OnResolveFileConflict(string message)
		{
			return unsafeEvents.OnResolveFileConflict(message);
		}
		
		public event EventHandler<ParentPackagesOperationEventArgs> ParentPackagesUpdated;
		
		public void OnParentPackagesUpdated(IEnumerable<IPackage> packages)
		{
			unsafeEvents.OnParentPackagesUpdated(packages);
		}
		
		void RaiseParentPackagesUpdatedEventIfHasSubscribers(object sender, ParentPackagesOperationEventArgs e)
		{
			if (ParentPackagesUpdated != null) {
				RaiseParentPackagesUpdatedEvent(sender, e);
			}
		}
		
		void RaiseParentPackagesUpdatedEvent(object sender, ParentPackagesOperationEventArgs e)
		{
			ParentPackagesUpdated(sender, e);
		}
	}
}
