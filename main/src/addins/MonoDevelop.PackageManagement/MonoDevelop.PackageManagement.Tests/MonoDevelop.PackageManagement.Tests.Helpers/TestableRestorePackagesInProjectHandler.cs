//
// TestableRestorePackagesInProjectHandler.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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

using MonoDevelop.Components.Commands;
using MonoDevelop.PackageManagement.Commands;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class TestableRestorePackagesInProjectHandler : RestorePackagesInProjectHandler
	{
		CommandInfo info = new CommandInfo ();
		Project project;
		Solution solution;

		public bool Enabled {
			get { return info.Enabled; }
		}

		public void RunUpdate (Solution solution, Project project)
		{
			this.solution = solution;
			this.project = project;

			base.Update (info);
		}

		protected override Project CurrentSelectedProject => project;
		protected override Solution CurrentSelectedSolution => solution;
	}
}
