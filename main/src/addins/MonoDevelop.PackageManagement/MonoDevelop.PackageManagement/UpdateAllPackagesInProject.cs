// 
// UpdateAllPackagesInProject.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.PackageManagement;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class UpdateAllPackagesInProject : UpdatePackageActions
	{
		IPackageManagementProject project;
		
		public UpdateAllPackagesInProject(IPackageManagementProject project)
		{
			this.project = project;
			this.UpdateDependencies = true;
		}
		
		public override IEnumerable<UpdatePackageAction> CreateActions()
		{
			foreach (IPackage package in GetPackages()) {
				yield return CreateUpdatePackageAction(package);
			}
		}
		
		IEnumerable<IPackage> GetPackages()
		{
			return project.GetPackagesInReverseDependencyOrder();
		}
		
		UpdatePackageAction CreateUpdatePackageAction(IPackage package)
		{
			UpdatePackageAction action = CreateDefaultUpdatePackageAction(project);
			action.PackageId = package.Id;
			return action;
		}
	}
}
