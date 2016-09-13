//
// AddPlatformImplementationHandler.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Packaging.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Packaging
{
	class AddPlatformImplementationHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			info.Visible = project?.IsPortableLibrary == true;
		}

		protected override async void Run ()
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			if (project == null || !project.IsPortableLibrary)
				return;

			var viewModel = new AddPlatformImplementationViewModel (project);
			using (var dialog = new AddPlatformImplementationDialog (viewModel)) {
				if (dialog.ShowWithParent () == Xwt.Command.Ok) {
					using (ProgressMonitor monitor = CreateProgressMonitor ()) {
						await viewModel.CreateProjects (monitor);
					}
				}
			}
		}

		ProgressMonitor CreateProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Adding platform implementation..."),
				Stock.StatusSolutionOperation,
				false,
				false,
				false,
				null,
				false);
		}
	}
}
