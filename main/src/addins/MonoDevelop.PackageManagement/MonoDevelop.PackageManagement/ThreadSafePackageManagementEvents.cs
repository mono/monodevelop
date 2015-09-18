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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using NuGet;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;

namespace ICSharpCode.PackageManagement
{
	public class ThreadSafePackageManagementEvents : IThreadSafePackageManagementEvents
	{
		static Action<MessageHandler> defaultGuiSyncDispatcher = DispatchService.GuiSyncDispatch;

		Action<MessageHandler> guiSyncDispatcher;
		IPackageManagementEvents unsafeEvents;

		public ThreadSafePackageManagementEvents (IPackageManagementEvents unsafeEvents)
			: this (unsafeEvents, defaultGuiSyncDispatcher)
		{
		}

		public ThreadSafePackageManagementEvents (
			IPackageManagementEvents unsafeEvents,
			Action<MessageHandler> guiSyncDispatcher)
		{
			this.unsafeEvents = unsafeEvents;
			this.guiSyncDispatcher = guiSyncDispatcher;
			
			RegisterEventHandlers();
		}

		void RegisterEventHandlers()
		{
			unsafeEvents.PackageOperationsStarting += RaisePackageOperationStartingEventIfHasSubscribers;
			unsafeEvents.PackageOperationsFinished += RaisePackageOperationFinishedEventIfHasSubscribers;
			unsafeEvents.PackageOperationError += RaisePackageOperationErrorEventIfHasSubscribers;
			unsafeEvents.ParentPackageInstalled += RaiseParentPackageInstalledEventIfHasSubscribers;
			unsafeEvents.ParentPackageUninstalled += RaiseParentPackageUninstalledEventIfHasSubscribers;
			unsafeEvents.ParentPackagesUpdated += RaiseParentPackagesUpdatedEventIfHasSubscribers;
			unsafeEvents.ResolveFileConflict += RaiseResolveFileConflictEventIfHasSubscribers;
		}
		
		public void Dispose()
		{
			UnregisterEventHandlers();
		}
		
		void UnregisterEventHandlers()
		{
			unsafeEvents.PackageOperationsStarting -= RaisePackageOperationStartingEventIfHasSubscribers;
			unsafeEvents.PackageOperationsFinished -= RaisePackageOperationFinishedEventIfHasSubscribers;
			unsafeEvents.PackageOperationError -= RaisePackageOperationErrorEventIfHasSubscribers;
			unsafeEvents.ParentPackageInstalled -= RaiseParentPackageInstalledEventIfHasSubscribers;
			unsafeEvents.ParentPackageUninstalled -= RaiseParentPackageUninstalledEventIfHasSubscribers;
			unsafeEvents.ParentPackagesUpdated -= RaiseParentPackagesUpdatedEventIfHasSubscribers;
			unsafeEvents.ResolveFileConflict -= RaiseResolveFileConflictEventIfHasSubscribers;
		}
		
		void RaisePackageOperationStartingEventIfHasSubscribers(object sender, EventArgs e)
		{
			if (PackageOperationsStarting != null) {
				guiSyncDispatcher (() => RaisePackageOperationStartingEvent (sender, e));
			}
		}
		
		void RaisePackageOperationStartingEvent(object sender, EventArgs e)
		{
			PackageOperationsStarting(sender, e);
		}
		
		public event EventHandler PackageOperationsStarting;

		void RaisePackageOperationFinishedEventIfHasSubscribers(object sender, EventArgs e)
		{
			if (PackageOperationsFinished != null) {
				guiSyncDispatcher (() => RaisePackageOperationFinishedEvent (sender, e));
			}
		}

		void RaisePackageOperationFinishedEvent(object sender, EventArgs e)
		{
			PackageOperationsFinished(sender, e);
		}

		public event EventHandler PackageOperationsFinished;

		void RaisePackageOperationErrorEventIfHasSubscribers(object sender, PackageOperationExceptionEventArgs e)
		{
			if (PackageOperationError != null) {
				guiSyncDispatcher (() => RaisePackageOperationErrorEvent(sender, e));
			}
		}
		
		void RaisePackageOperationErrorEvent(object sender, PackageOperationExceptionEventArgs e)
		{
			if (PackageOperationError != null) {
				guiSyncDispatcher (() => PackageOperationError(sender, e));
			}
		}
		
		public event EventHandler<PackageOperationExceptionEventArgs> PackageOperationError;
		
		void RaiseParentPackageInstalledEventIfHasSubscribers(object sender, ParentPackageOperationEventArgs e)
		{
			if (ParentPackageInstalled != null) {
				guiSyncDispatcher (() => RaiseParentPackageInstalledEvent(sender, e));
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
				guiSyncDispatcher (() => RaiseParentPackageUninstalledEvent(sender, e));
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
		
		public void OnPackageOperationsFinished()
		{
			unsafeEvents.OnPackageOperationsFinished();
		}

		public void OnPackageOperationError(Exception ex)
		{
			unsafeEvents.OnPackageOperationError(ex);
		}
		
		public bool OnAcceptLicenses(IEnumerable<IPackage> packages)
		{
			return unsafeEvents.OnAcceptLicenses(packages);
		}
		
		public void OnParentPackageInstalled(IPackage package, IPackageManagementProject project)
		{
			OnParentPackageInstalled (package, project, new PackageOperation [0]);
		}

		public void OnParentPackageInstalled (IPackage package, IPackageManagementProject project, IEnumerable<PackageOperation> operations)
		{
			unsafeEvents.OnParentPackageInstalled (package, project, operations);
		}

		public void OnParentPackageUninstalled(IPackage package, IPackageManagementProject project)
		{
			unsafeEvents.OnParentPackageUninstalled(package, project);
		}
		
		public void OnPackageOperationMessageLogged(MessageLevel level, string message, params object[] args)
		{
			unsafeEvents.OnPackageOperationMessageLogged(level, message, args);
		}
		
		public bool OnSelectProjects(IEnumerable<IPackageManagementSelectedProject> selectedProjects)
		{
			return unsafeEvents.OnSelectProjects(selectedProjects);
		}
		
		public event EventHandler<ResolveFileConflictEventArgs> ResolveFileConflict;
		
		public FileConflictResolution OnResolveFileConflict(string message)
		{
			return unsafeEvents.OnResolveFileConflict(message);
		}

		void RaiseResolveFileConflictEventIfHasSubscribers (object sender, ResolveFileConflictEventArgs e)
		{
			if (ResolveFileConflict != null) {
				guiSyncDispatcher (() => ResolveFileConflict (sender, e));
			}
		}
		
		public event EventHandler<ParentPackagesOperationEventArgs> ParentPackagesUpdated;
		
		public void OnParentPackagesUpdated(IEnumerable<IPackage> packages)
		{
			unsafeEvents.OnParentPackagesUpdated(packages);
		}
		
		void RaiseParentPackagesUpdatedEventIfHasSubscribers(object sender, ParentPackagesOperationEventArgs e)
		{
			if (ParentPackagesUpdated != null) {
				guiSyncDispatcher (() => RaiseParentPackagesUpdatedEvent(sender, e));
			}
		}
		
		void RaiseParentPackagesUpdatedEvent(object sender, ParentPackagesOperationEventArgs e)
		{
			ParentPackagesUpdated(sender, e);
		}

		public event EventHandler PackagesRestored {
			add { unsafeEvents.PackagesRestored += value; }
			remove { unsafeEvents.PackagesRestored -= value; }
		}

		public void OnPackagesRestored()
		{
			unsafeEvents.OnPackagesRestored ();
		}

		public event EventHandler<FileEventArgs> FileChanged {
			add { unsafeEvents.FileChanged += value; }
			remove { unsafeEvents.FileChanged -= value; }
		}

		public void OnFileChanged (string path)
		{
			unsafeEvents.OnFileChanged (path);
		}

		public event EventHandler UpdatedPackagesAvailable {
			add { unsafeEvents.UpdatedPackagesAvailable += value; }
			remove { unsafeEvents.UpdatedPackagesAvailable -= value; }
		}

		public void OnUpdatedPackagesAvailable ()
		{
			unsafeEvents.OnUpdatedPackagesAvailable ();
		}

		public event EventHandler<FileRemovingEventArgs> FileRemoving {
			add { unsafeEvents.FileRemoving += value; }
			remove { unsafeEvents.FileRemoving -= value; }
		}

		public bool OnFileRemoving (string path)
		{
			return unsafeEvents.OnFileRemoving (path);
		}

		public event EventHandler<PackageRestoredEventArgs> PackageRestored {
			add { unsafeEvents.PackageRestored += value; }
			remove { unsafeEvents.PackageRestored -= value; }
		}

		public void OnPackageRestored (IPackage package)
		{
			unsafeEvents.OnPackageRestored (package);
		}

		public event EventHandler<DotNetProjectReferenceEventArgs> ReferenceAdding {
			add { unsafeEvents.ReferenceAdding += value; }
			remove { unsafeEvents.ReferenceAdding -= value; }
		}

		public event EventHandler<DotNetProjectReferenceEventArgs> ReferenceRemoving {
			add { unsafeEvents.ReferenceRemoving += value; }
			remove { unsafeEvents.ReferenceRemoving -= value; }
		}

		public void OnReferenceAdding (ProjectReference reference)
		{
			unsafeEvents.OnReferenceAdding (reference);
		}

		public void OnReferenceRemoving (ProjectReference reference)
		{
			unsafeEvents.OnReferenceRemoving (reference);
		}

		public event EventHandler<DotNetProjectImportEventArgs> ImportRemoved {
			add { unsafeEvents.ImportRemoved += value; }
			remove { unsafeEvents.ImportRemoved -= value; }
		}

		public void OnImportRemoved (IDotNetProject project, string import)
		{
			unsafeEvents.OnImportRemoved (project, import);
		}
	}
}
