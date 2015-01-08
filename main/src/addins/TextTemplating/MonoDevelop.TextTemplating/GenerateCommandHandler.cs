//
// GenerateCommand.cs
//
// Author:
//       Sergey Zhukov <>
//
// Copyright (c) 2015 Sergey Zhukov
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CustomTools;

namespace MonoDevelop.TextTemplating
{
	public enum Commands
	{
		Generate
	}

	public class GenerateCommandHandler : CommandHandler
	{
		protected override void Run ()
		{
			IWorkspaceObject wob = IdeApp.ProjectOperations.CurrentSelectedItem as IWorkspaceObject;

			Solution solution = wob as Solution;
			if (solution != null) {
				foreach(Project p in solution.GetAllProjects ())
					UpdateFilesInProject(p);
			} else {
				Project proj = wob as Project;

				if (proj != null) {
					UpdateFilesInProject (proj);
				}
			}

			base.Run ();

		}

		private void UpdateFilesInProject(Project project)
		{
			foreach (ProjectFile file in project.Files) {
				if (file.Generator == typeof(TextTemplatingFileGenerator).Name
					|| file.Generator == typeof(TextTemplatingFilePreprocessor).Name) {
					CustomToolService.Update (file, true);
				}
			}
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);
		}
	}
}

