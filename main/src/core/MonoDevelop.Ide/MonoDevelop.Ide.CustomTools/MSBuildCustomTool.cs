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
	class MSBuildCustomTool : ISingleFileCustomTool
	{
		readonly string targetName;

		public MSBuildCustomTool (string targetName)
		{
			this.targetName = targetName;
		}

		public async Task Generate (ProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			var fileInfo = await GetProjectFileTimestamps (monitor, file.Project);
			await PerformGenerate (monitor, file, result);
			await SendFileChangeNotifications (monitor, file.Project, fileInfo);
		}

		async Task<List<FileInfo>> GetProjectFileTimestamps (ProgressMonitor monitor, Project project)
		{
			var infoList = new List<FileInfo> ();
			var projectFiles = await project.GetSourceFilesAsync (monitor, IdeApp.Workspace.ActiveConfiguration);

			foreach (var projectFile in projectFiles) {
				var info = new FileInfo (projectFile.FilePath);
				infoList.Add (info);
			}

			return infoList;
		}

		async Task SendFileChangeNotifications (ProgressMonitor monitor, Project project, List<FileInfo> beforeFileInfo)
		{
			var changedFiles = new List<FileInfo> ();
			var projectFiles = await project.GetSourceFilesAsync (monitor, IdeApp.Workspace.ActiveConfiguration);

			foreach (var projectFile in projectFiles) {
				var info = new FileInfo (projectFile.FilePath);

				var beforeFile = beforeFileInfo.FirstOrDefault (bf => bf.FullName == info.FullName);
				if (beforeFile != null) {
					if (beforeFile.LastWriteTime != info.LastWriteTime) {
						changedFiles.Add (info);
					}
				} else {
					changedFiles.Add (info);
				}
			}

			foreach (var changedFile in changedFiles) {
				FileService.NotifyFileChanged (changedFile.FullName);
			}
		}

		async Task PerformGenerate (ProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			var buildResult = await file.Project.RunTarget (monitor, targetName, IdeApp.Workspace.ActiveConfiguration);

			foreach (var err in buildResult.BuildResult.Errors) {
				result.Errors.Add (new CompilerError (err.FileName, err.Line, err.Column, err.ErrorNumber, err.ErrorText) {
					IsWarning = err.IsWarning
				});
			}
		}
	}
}

