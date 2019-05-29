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

using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement.Commands
{
	internal abstract class PackagesCommandHandler : CommandHandler
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
			return CurrentSelectedProject as DotNetProject;
		}

		protected bool SelectedDotNetProjectHasPackages ()
		{
			DotNetProject project = GetSelectedDotNetProject ();
			return (project != null) && project.HasPackages ();
		}

		protected bool IsDotNetSolutionSelected ()
		{
			return CurrentSelectedSolution != null;
		}

		protected bool SelectedDotNetSolutionHasPackages ()
		{
			Solution solution = CurrentSelectedSolution;
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

		protected Solution GetSelectedSolution ()
		{
			DotNetProject project = GetSelectedDotNetProject ();
			if (project != null) {
				return project.ParentSolution;
			}
			return CurrentSelectedSolution;
		}

		protected bool CanUpdatePackagesForSelectedDotNetProject ()
		{
			DotNetProject project = GetSelectedDotNetProject ();
			return project?.CanUpdatePackages () == true;
		}

		bool CanUpdatePackagesForSelectedDotNetSolution ()
		{
			Solution solution = CurrentSelectedSolution;
			return solution?.CanUpdatePackages () == true;
		}

		protected bool CanUpdatePackagesForSelectedDotNetProjectOrSolution ()
		{
			if (IsDotNetProjectSelected ()) {
				return CanUpdatePackagesForSelectedDotNetProject ();
			} else if (IsDotNetSolutionSelected ()) {
				return CanUpdatePackagesForSelectedDotNetSolution ();
			}
			return false;
		}

		/// <summary>
		/// Used by unit tests to avoid having to initialize the IDE workspace.
		/// </summary>
		protected virtual Solution CurrentSelectedSolution {
			get { return IdeApp.ProjectOperations.CurrentSelectedSolution; }
		}

		/// <summary>
		/// Used by unit tests to avoid having to initialize the IDE workspace.
		/// </summary>
		protected virtual Project CurrentSelectedProject {
			get { return IdeApp.ProjectOperations.CurrentSelectedProject; }
		}
	}
}
