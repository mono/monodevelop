//
// FindProjectReferenceUsagesHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.Refactoring
{
	sealed class FindProjectReferenceUsagesHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			var currentProject = IdeApp.ProjectOperations.CurrentSelectedProject;
			if (currentProject == null) {
				info.Enabled = false;
				return;
			}

			var pad = IdeApp.Workbench.GetPad<ProjectSolutionPad> ().Content as ProjectSolutionPad;
			var selectedNodes = pad.TreeView.GetSelectedNodes ();
			if (selectedNodes == null || selectedNodes.Length != 1) {
				info.Enabled = false;
				return;
			}
			info.Enabled = true;
		}

		protected async override void Run ()
		{
			var currentProject = IdeApp.ProjectOperations.CurrentSelectedProject;
			if (currentProject == null)
				return;
			var pad = IdeApp.Workbench.GetPad<ProjectSolutionPad> ().Content as ProjectSolutionPad;
			var selectedNodes = pad.TreeView.GetSelectedNodes ();
			if (selectedNodes == null || selectedNodes.Length != 1)
				return;
			var dataItem = selectedNodes[0].DataItem;

			var projectRef = dataItem as ProjectReference;
			if (projectRef != null) {
				await RefactoringService.FindReferenceUsagesAsync(projectRef);
			}
		}
	}
}