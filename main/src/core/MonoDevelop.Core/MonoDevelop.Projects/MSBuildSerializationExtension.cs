//
// MSBuildSerializationExtension.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	class MSBuildSerializationExtension: WorkspaceObjectReader
	{
		public override bool CanRead (FilePath file, Type expectedType)
		{
			foreach (var f in MSBuildFileFormat.GetSupportedFormats ()) {
				if (f.CanReadFile (file, expectedType))
					return true;
			}
			return false;
		}

		public override Task<SolutionItem> LoadSolutionItem (ProgressMonitor monitor, SolutionLoadContext ctx, string fileName, MSBuildFileFormat expectedFormat, string typeGuid, string itemGuid)
		{
			return Task.Run (() => {
				foreach (var f in MSBuildFileFormat.GetSupportedFormats ()) {
					if (f.CanReadFile (fileName, typeof(SolutionItem)))
						return MSBuildProjectService.LoadItem (monitor, fileName, f, typeGuid, itemGuid, ctx);
				}
				throw new NotSupportedException ();
			});
		}

		public override Task<WorkspaceItem> LoadWorkspaceItem (ProgressMonitor monitor, string fileName)
		{
			return Task.Run (async () => {
				foreach (var f in MSBuildFileFormat.GetSupportedFormats ()) {
					if (f.CanReadFile (fileName, typeof (WorkspaceItem)))
						return (WorkspaceItem)await f.ReadFile (fileName, typeof (WorkspaceItem), monitor).ConfigureAwait (false);
				}
				throw new NotSupportedException ();
			});
		}
	}
}

