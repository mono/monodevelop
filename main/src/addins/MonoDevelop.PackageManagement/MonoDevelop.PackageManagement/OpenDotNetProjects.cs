// 
// OpenDotNetProjects.cs
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

using System;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.PackageManagement;

namespace ICSharpCode.PackageManagement
{
	public class OpenDotNetProjects
	{
		IPackageManagementProjectService projectService;
		
		public OpenDotNetProjects (IPackageManagementProjectService projectService)
		{
			this.projectService = projectService;
		}
		
		public IDotNetProject FindProject (string name)
		{
			foreach (IProject project in projectService.GetOpenProjects ()) {
				if (IsProjectNameMatch (project, name)) {
					return project as IDotNetProject;
				}
			}
			return null;
		}
		
		bool IsProjectNameMatch (IProject project, string name)
		{
			return IsMatchIgnoringCase (project.Name, name) ||
				(project.FileName == name);
		}
		
		bool IsMatchIgnoringCase (string a, string b)
		{
			return String.Equals (a, b, StringComparison.OrdinalIgnoreCase);
		}
	}
}
