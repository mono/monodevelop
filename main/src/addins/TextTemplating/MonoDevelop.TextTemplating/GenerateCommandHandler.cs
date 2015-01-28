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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.TextTemplating
{
	public enum Commands
	{
		Generate
	}

	class GenerateCommandHandler : CommandHandler
	{
		protected override void Run ()
		{
			var wob = IdeApp.ProjectOperations.CurrentSelectedItem;

			var pf = wob as ProjectFile;
			if (pf != null) {
				CustomToolService.Update (pf, true);
				return;
			}

			IEnumerable<ProjectFile> files;

			var solution = wob as Solution;
			if (solution != null) {
				files = solution.GetAllProjects ().SelectMany (GetFilesToUpdate);
			} else if (wob is Project) {
				files = GetFilesToUpdate ((Project)wob);
			} else {
				return;
			}

			CustomToolService.Update (files, true);
		}

		static IEnumerable<ProjectFile> GetFilesToUpdate (Project project)
		{
			return project.Files.Where (IsTemplate);
		}

		static bool IsTemplate (ProjectFile pf)
		{
			return pf.Generator == typeof(TextTemplatingFileGenerator).Name
			|| pf.Generator == typeof(TextTemplatingFilePreprocessor).Name;
		}

		protected override void Update (CommandInfo info)
		{
			var wob = IdeApp.ProjectOperations.CurrentSelectedItem;

			if (wob is Solution || wob is Project) {
				info.Text = GettextCatalog.GetString ("Process T4 Templates");
				info.Enabled = info.Visible = true;
				return;
			}

			var file = wob as ProjectFile;
			if (file != null && IsTemplate (file)) {
				info.Text = GettextCatalog.GetString ("Process T4 Template");
				info.Enabled = info.Visible = true;
				return;
			}

			info.Enabled = info.Visible = false;
		}
	}
}
