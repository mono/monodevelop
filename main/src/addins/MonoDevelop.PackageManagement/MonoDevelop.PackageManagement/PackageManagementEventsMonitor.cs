//
// PackageManagementEventsMonitor.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class PackageManagementEventsMonitor : IDisposable
	{
		IProgressMonitor progressMonitor;
		IPackageManagementEvents packageManagementEvents;
		IProgressProvider progressProvider;
		FileConflictResolution lastFileConflictResolution;
		IFileConflictResolver fileConflictResolver = new FileConflictResolver ();
		string currentProgressOperation;
		List<FileEventArgs> fileChangedEvents = new List<FileEventArgs> ();

		public PackageManagementEventsMonitor (
			IProgressMonitor progressMonitor,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
		{
			this.progressMonitor = progressMonitor;
			this.packageManagementEvents = packageManagementEvents;
			this.progressProvider = progressProvider;

			packageManagementEvents.PackageOperationMessageLogged += PackageOperationMessageLogged;
			packageManagementEvents.ResolveFileConflict += ResolveFileConflict;
			packageManagementEvents.AcceptLicenses += AcceptLicenses;
			packageManagementEvents.FileChanged += FileChanged;
			progressProvider.ProgressAvailable += ProgressAvailable;
		}
			
		public void Dispose ()
		{
			progressProvider.ProgressAvailable -= ProgressAvailable;
			packageManagementEvents.FileChanged -= FileChanged;
			packageManagementEvents.AcceptLicenses -= AcceptLicenses;
			packageManagementEvents.ResolveFileConflict -= ResolveFileConflict;
			packageManagementEvents.PackageOperationMessageLogged -= PackageOperationMessageLogged;

			NotifyFilesChanged ();
		}

		void ResolveFileConflict(object sender, ResolveFileConflictEventArgs e)
		{
			if (UserPreviouslySelectedOverwriteAllOrIgnoreAll ()) {
				e.Resolution = lastFileConflictResolution;
			} else {
				DispatchService.GuiSyncDispatch (() => {
					e.Resolution = fileConflictResolver.ResolveFileConflict (e.Message);
				});
				lastFileConflictResolution = e.Resolution;
			}
		}

		bool UserPreviouslySelectedOverwriteAllOrIgnoreAll()
		{
			return
				(lastFileConflictResolution == FileConflictResolution.IgnoreAll) ||
				(lastFileConflictResolution == FileConflictResolution.OverwriteAll);
		}
		
		void PackageOperationMessageLogged (object sender, PackageOperationMessageLoggedEventArgs e)
		{
			if (e.Message.Level == MessageLevel.Warning) {
				ReportWarning (e.Message.ToString ());
			} else {
				LogMessage (e.Message.ToString ());
			}
		}

		void ReportWarning (string message)
		{
			progressMonitor.ReportWarning (message);
			LogMessage (message);

			HasWarnings = true;
		}

		void LogMessage (string message)
		{
			progressMonitor.Log.WriteLine (message);
		}

		public bool HasWarnings { get; private set; }

		public void ReportResult (ProgressMonitorStatusMessage progressMessage)
		{
			if (HasWarnings) {
				progressMonitor.ReportSuccess (progressMessage.Warning);
			} else {
				progressMonitor.ReportSuccess (progressMessage.Success);
			}
		}

		void AcceptLicenses (object sender, AcceptLicensesEventArgs e)
		{
			foreach (IPackage package in e.Packages) {
				ReportLicenseAgreementWarning (package);
			}
			e.IsAccepted = true;
		}

		void ReportLicenseAgreementWarning (IPackage package)
		{
			string message = GettextCatalog.GetString (
				"The {0} package has a license agreement which is available at {1}{2}" +
				"Please review this license agreement and remove the package if you do not accept the agreement.{2}" +
				"Check the package for additional dependencies which may also have license agreements.{2}" +
				"Using this package and any dependencies constitutes your acceptance of these license agreements.",
				package.Id,
				package.LicenseUrl,
				Environment.NewLine);

			ReportWarning (message);
		}

		void ProgressAvailable (object sender, ProgressEventArgs e)
		{
			if (currentProgressOperation == e.Operation)
				return;

			currentProgressOperation = e.Operation;
			progressMonitor.Log.WriteLine (e.Operation);
		}

		void FileChanged (object sender, FileEventArgs e)
		{
			fileChangedEvents.Add (e);
		}

		void NotifyFilesChanged ()
		{
			DispatchService.GuiSyncDispatch (() => {
				FilePath[] files = fileChangedEvents
					.SelectMany (fileChangedEvent => fileChangedEvent.ToArray ())
					.Select (fileInfo => fileInfo.FileName)
					.ToArray ();

				FileService.NotifyFilesChanged (files);
			});
		}
	}
}

