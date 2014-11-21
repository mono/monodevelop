//
// TestablePackageManagementEventsMonitor.cs
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
using MonoDevelop.Ide;
using MonoDevelop.Core;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class TestablePackageManagementEventsMonitor : PackageManagementEventsMonitor
	{
		public TestablePackageManagementEventsMonitor (
			IProgressMonitor progressMonitor,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
			: base (progressMonitor, packageManagementEvents, progressProvider)
		{
		}

		public List<FilePath> FilesChanged = new List<FilePath> ();

		protected override void NotifyFilesChanged (FilePath[] files)
		{
			FilesChanged.AddRange (files);
		}

		protected override void GuiSyncDispatch (MessageHandler handler)
		{
			handler.Invoke ();
		}

		protected override void ShowPackageConsole (IProgressMonitor progressMonitor)
		{
			IsPackageConsoleShown = true;
			ProgressMonitorPassedToShowPackageConsole = progressMonitor;
		}

		public bool IsPackageConsoleShown;
		public IProgressMonitor ProgressMonitorPassedToShowPackageConsole;

		protected override void ReconnectAssemblyReferences (IPackageManagementProject project)
		{
			ProjectsPassedToReconnectAssemblyReferences.Add (project);
			IsTypeSystemRefreshed = true;
		}

		public List<IPackageManagementProject> ProjectsPassedToReconnectAssemblyReferences = new List<IPackageManagementProject> ();
		public bool IsTypeSystemRefreshed;
	}
}

