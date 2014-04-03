// 
// UpdatePackageAction.cs
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
	public class UpdatePackageAction : ProcessPackageOperationsAction, IUpdatePackageSettings
	{
		public UpdatePackageAction(
			IPackageManagementProject project,
			IPackageManagementEvents packageManagementEvents)
			: base(project, packageManagementEvents)
		{
			UpdateDependencies = true;
			UpdateIfPackageDoesNotExistInProject = true;
		}
		
		public bool UpdateDependencies { get; set; }
		public bool UpdateIfPackageDoesNotExistInProject { get; set; }
		
		protected override IEnumerable<PackageOperation> GetPackageOperations()
		{
			var installAction = Project.CreateInstallPackageAction();
			installAction.AllowPrereleaseVersions = AllowPrereleaseVersions;
			installAction.IgnoreDependencies = !UpdateDependencies;
			return Project.GetInstallPackageOperations(Package, installAction);
		}
		
		protected override void ExecuteCore()
		{
			if (ShouldUpdatePackage()) {
				Project.UpdatePackage(Package, this);
				OnParentPackageInstalled();
			}
		}
		
		bool ShouldUpdatePackage()
		{
			if (!UpdateIfPackageDoesNotExistInProject) {
				return PackageIdExistsInProject();
			}
			return true;
		}

		protected override string StartingMessageFormat {
			get { return "Updating {0}..."; }
		}

		protected override bool ShouldLogEmptyLineForFinishedAction ()
		{
			return ShouldUpdatePackage ();
		}

		protected override bool ShouldLogStartingMessage ()
		{
			return ShouldUpdatePackage ();
		}
	}
}
