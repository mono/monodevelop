//
// MSBuildCustomTool.cs
//
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc.
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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.CodeDom.Compiler;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Ide.CustomTools
{
	class MSBuildCustomTool : SingleProjectFileCustomTool
	{
		readonly string targetName;

		public MSBuildCustomTool (string targetName)
		{
			this.targetName = targetName;
		}

		public override async Task Generate (ProgressMonitor monitor, Project project, ProjectFile file, SingleFileCustomToolResult result)
		{
			if (project == null) {
				return;
			}

			var buildResult = await project.PerformGeneratorAsync (monitor, IdeApp.Workspace.ActiveConfiguration, this.targetName);

			foreach (var err in buildResult.BuildResult.Errors) {
				result.Errors.Add (new CompilerError (err.FileName, err.Line, err.Column, err.ErrorNumber, err.ErrorText) {
					IsWarning = err.IsWarning
				});
			}
		}
	}
}

