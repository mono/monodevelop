//
// ReinstallAllPackagesInProjectHandler.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.PackageManagement.NodeBuilders;
using MonoDevelop.Projects;
using NuGet;

namespace MonoDevelop.PackageManagement.Commands
{
	public class ReinstallAllPackagesInProjectHandler : PackagesCommandHandler
	{
		protected override void Run ()
		{
			try {
				IPackageManagementProject project = PackageManagementServices.Solution.GetActiveProject ();
				List<ReinstallPackageAction> reinstallActions = CreateReinstallActions (project).ToList ();
				ProgressMonitorStatusMessage progressMessage = CreateProgressMessage (reinstallActions);
				PackageManagementServices.BackgroundPackageActionRunner.Run (progressMessage, reinstallActions);
			} catch (Exception ex) {
				ShowStatusBarError (ex);
			}
		}

		IEnumerable<ReinstallPackageAction> CreateReinstallActions (IPackageManagementProject project)
		{
			var packageReferenceFile = new ProjectPackageReferenceFile (project.DotNetProject);
			return packageReferenceFile.GetPackageReferences ()
				.Select (packageReference => CreateReinstallPackageAction (project, packageReference));
		}

		ReinstallPackageAction CreateReinstallPackageAction (IPackageManagementProject project, PackageReference packageReference)
		{
			ReinstallPackageAction action = project.CreateReinstallPackageAction ();
			action.PackageId = packageReference.Id;
			action.PackageVersion = packageReference.Version;

			return action;
		}

		ProgressMonitorStatusMessage CreateProgressMessage (IEnumerable<ReinstallPackageAction> actions)
		{
			if (actions.Count () == 1) {
				return ProgressMonitorStatusMessageFactory.CreateRetargetingSinglePackageMessage (actions.First ().PackageId);
			}
			return ProgressMonitorStatusMessageFactory.CreateRetargetingPackagesInProjectMessage (actions.Count ());
		}

		void ShowStatusBarError (Exception ex)
		{
			ProgressMonitorStatusMessage message = ProgressMonitorStatusMessageFactory.CreateRetargetingPackagesInProjectMessage ();
			PackageManagementServices.BackgroundPackageActionRunner.ShowError (message, ex);
		}

		protected override void Update (CommandInfo info)
		{
			info.Visible = SelectedDotNetProjectHasPackagesRequiringReinstall ();
		}

		bool SelectedDotNetProjectHasPackagesRequiringReinstall ()
		{
			DotNetProject project = GetSelectedDotNetProject ();
			if (project == null)
				return false;

			var packageReferenceFile = new ProjectPackageReferenceFile (project);
			return packageReferenceFile.AnyPackagesToBeReinstalled ();
		}
	}
}

