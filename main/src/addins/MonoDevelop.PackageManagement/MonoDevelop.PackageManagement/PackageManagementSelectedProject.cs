// 
// PackageManagementSelectedProject.cs
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

namespace ICSharpCode.PackageManagement
{
	public class PackageManagementSelectedProject : IPackageManagementSelectedProject
	{
		public PackageManagementSelectedProject(IPackageManagementProject project)
			: this(project, selected: false)
		{
		}
		
		public PackageManagementSelectedProject(
			IPackageManagementProject project,
			bool selected)
			: this(project, selected, enabled: true)
		{
		}
		
		public PackageManagementSelectedProject (
			IPackageManagementProject project,
			bool selected,
			bool enabled)
			: this (project.Name, selected, enabled)
		{
			this.Project = project;
		}

		public PackageManagementSelectedProject (
			string name,
			bool selected,
			bool enabled)
		{
			this.Name = name;
			this.IsSelected = selected;
			this.IsEnabled = enabled;
		}
		
		public IPackageManagementProject Project { get; private set; }
		public string Name { get; private set; }
		
		public bool IsSelected { get; set; }
		public bool IsEnabled { get; set; }
	}
}
