//
// PathBasedProjectItem.cs
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
using MonoDevelop.Core;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects
{
	/// <summary>
	/// A base class for project items that represent a file or a folder
	/// </summary>
	public abstract class PathBasedProjectItem: ProjectItem
	{
		public FilePath Path { get; protected set; }

		public override string Include {
			get {
				return MSBuildProjectService.ToMSBuildPath (Project != null ? Project.ItemDirectory : null, Path);
			}
			protected set {
				Path = MSBuildProjectService.FromMSBuildPath (Project != null ? Project.ItemDirectory : null, value);
			}
		}

		internal protected override void Read (Project project, IMSBuildItemEvaluated buildItem)
		{
			base.Read (project, buildItem);
			Path = MSBuildProjectService.FromMSBuildPath (project.ItemDirectory, buildItem.Include);
		}

		internal protected override void Write (Project project, MSBuildItem buildItem)
		{
			if (string.IsNullOrEmpty (Include))
				Include = MSBuildProjectService.ToMSBuildPath (project.ItemDirectory, Path);
			base.Write (project, buildItem);
		}
	}
}

