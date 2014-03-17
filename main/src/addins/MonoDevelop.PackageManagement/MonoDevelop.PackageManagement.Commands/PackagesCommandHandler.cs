// 
// PackagesCommandHandler.cs
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
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement.Commands
{
	public abstract class PackagesCommandHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IsEnabled ()) {
				info.Bypass = false;
			} else {
				info.Enabled = false;
				info.Bypass = true;
			}
		}

		protected virtual bool IsEnabled ()
		{
			return IsDotNetProjectSelected () || IsDotNetSolutionSelected ();
		}

		protected bool IsDotNetProjectSelected ()
		{
			return GetSelectedDotNetProject () != null;
		}

		protected DotNetProject GetSelectedDotNetProject ()
		{
			return IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
		}

		protected bool SelectedDotNetProjectHasPackages ()
		{
			DotNetProject project = GetSelectedDotNetProject ();
			return (project != null) && project.HasPackages ();
		}

		protected bool IsDotNetSolutionSelected ()
		{
			return IdeApp.ProjectOperations.CurrentSelectedSolution != null;
		}

		protected bool SelectedDotNetSolutionHasPackages ()
		{
			Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution == null) {
				return false;
			}

			return solution.HasAnyProjectWithPackages ();
		}

		protected bool SelectedDotNetProjectOrSolutionHasPackages ()
		{
			if (IsDotNetProjectSelected ()) {
				return SelectedDotNetProjectHasPackages ();
			} else if (IsDotNetSolutionSelected ()) {
				return SelectedDotNetSolutionHasPackages ();
			}
			return false;
		}
	}
}
