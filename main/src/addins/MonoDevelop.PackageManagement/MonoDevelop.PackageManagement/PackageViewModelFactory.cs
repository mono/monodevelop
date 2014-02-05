// 
// PackageViewModelFactory.cs
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
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class PackageViewModelFactory : IPackageViewModelFactory
	{
		public PackageViewModelFactory(IPackageViewModelFactory packageViewModelFactory)
			: this(
				packageViewModelFactory.Solution,
				packageViewModelFactory.PackageManagementEvents,
				packageViewModelFactory.PackageActionRunner)
		{
		}
		
		public PackageViewModelFactory(
			IPackageManagementSolution solution,
			IPackageManagementEvents packageManagementEvents,
			IPackageActionRunner actionRunner)
		{
			this.Solution = solution;
			this.SelectedProjects = new PackageManagementSelectedProjects(solution);
			this.PackageManagementEvents = packageManagementEvents;
			this.PackageActionRunner = actionRunner;
			this.Logger = new PackageManagementLogger(packageManagementEvents);
		}
		
		public virtual PackageViewModel CreatePackageViewModel(IPackageViewModelParent parent, IPackageFromRepository package)
		{
			return new PackageViewModel(
				parent,
				package,
				SelectedProjects,
				PackageManagementEvents,
				PackageActionRunner,
				Logger);
		}
		
		public IPackageManagementSolution Solution { get; private set; }
		public PackageManagementSelectedProjects SelectedProjects { get; protected set; }
		public IPackageManagementEvents PackageManagementEvents { get; private set; }
		public ILogger Logger { get; private set; }
		public IPackageActionRunner PackageActionRunner { get; private set; }
	}
}
