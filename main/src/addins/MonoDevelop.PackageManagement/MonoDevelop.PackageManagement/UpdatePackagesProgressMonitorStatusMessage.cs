//
// UpdatePackagesProgressStatusMessage.cs
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
using ICSharpCode.PackageManagement;

namespace MonoDevelop.PackageManagement
{
	public class UpdatePackagesProgressMonitorStatusMessage : ProgressMonitorStatusMessage
	{
		UpdatedPackagesMonitor monitor;
		string packagesUpToDateMessage;
		string packagesUpToDateWarningMessage;

		public UpdatePackagesProgressMonitorStatusMessage (
			IPackageManagementProject project,
			string packagesUpToDateMessage,
			string packagesUpToDateWarningMessage,
			ProgressMonitorStatusMessage message)
			: this (
				new IPackageManagementProject [] { project },
				packagesUpToDateMessage,
				packagesUpToDateWarningMessage,
				message)
		{
		}

		public UpdatePackagesProgressMonitorStatusMessage (
			IEnumerable<IPackageManagementProject> projects,
			string packagesUpToDateMessage,
			string packagesUpToDateWarningMessage,
			ProgressMonitorStatusMessage message)
			: base (message.Status, message.Success, message.Error, message.Warning)
		{
			this.packagesUpToDateMessage = packagesUpToDateMessage;
			this.packagesUpToDateWarningMessage = packagesUpToDateWarningMessage;
			monitor = new UpdatedPackagesMonitor (projects);
		}

		protected override string GetSuccessMessage ()
		{
			Dispose ();

			if (AnyPackagesUpdated ()) {
				return base.GetSuccessMessage ();
			}

			return packagesUpToDateMessage;
		}

		protected override string GetErrorMessage ()
		{
			Dispose ();
			return base.GetErrorMessage ();
		}

		protected override string GetWarningMessage ()
		{
			Dispose ();

			if (AnyPackagesUpdated ()) {
				return base.GetWarningMessage ();
			}

			return packagesUpToDateWarningMessage;
		}

		bool AnyPackagesUpdated ()
		{
			return monitor.AnyPackagesUpdated ();
		}

		void Dispose ()
		{
			monitor.Dispose ();
		}
	}
}

