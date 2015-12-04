// 
// ProcessPackageOperationsAction.cs
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

using System.Collections.Generic;
using System.Linq;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public abstract class ProcessPackageOperationsAction : ProcessPackageAction
	{
		IPackageManagementEvents packageManagementEvents;

		public ProcessPackageOperationsAction(
			IPackageManagementProject project,
			IPackageManagementEvents packageManagementEvents)
			: base(project, packageManagementEvents)
		{
			this.packageManagementEvents = packageManagementEvents;
		}
		
		public IEnumerable<PackageOperation> Operations { get; set; }

		public override bool HasPackageScriptsToRun()
		{
			BeforeExecute();
			var files = new PackageFilesForOperations(Operations);
			return files.HasAnyPackageScripts();
		}
		
		protected override void BeforeExecute()
		{
			base.BeforeExecute();
			GetPackageOperationsIfMissing();
		}
		
		void GetPackageOperationsIfMissing()
		{
			if (Operations == null) {
				Operations = GetPackageOperations();
			}
		}
		
		protected virtual IEnumerable<PackageOperation> GetPackageOperations()
		{
			return null;
		}

		public IEnumerable<PackageOperation> GetInstallOperations ()
		{
			BeforeExecute ();
			return Operations.Where (operation => operation.Action == PackageAction.Install);
		}

		protected void OnParentPackageInstalled ()
		{
			packageManagementEvents.OnParentPackageInstalled (Package, Project, Operations);
		}
	}
}
