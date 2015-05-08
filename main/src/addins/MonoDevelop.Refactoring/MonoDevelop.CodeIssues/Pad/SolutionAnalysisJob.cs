//
// SolutionAnalysisJob.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Simon Lindgren
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
using MonoDevelop.Projects;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Ide;

namespace MonoDevelop.CodeIssues
{
	public class SolutionAnalysisJob : SimpleAnalysisJob
	{
		public SolutionAnalysisJob (Solution solution) : base(GetApplicableFiles(solution))
		{
		}

		static IList<ProjectFile> GetApplicableFiles (Solution solution)
		{
			var configurationSelector = IdeApp.Workspace.ActiveConfiguration;
			var config = solution.GetConfiguration (configurationSelector);
			return solution.GetAllProjects ()
				.Where (config.BuildEnabledForItem)
				.SelectMany (p => p.Files)
				.Where (f => f.BuildAction == BuildAction.Compile)
				.Distinct (new FilePathComparer())
				.ToList ();
		}

		class FilePathComparer : IEqualityComparer<ProjectFile>
		{
			#region IEqualityComparer implementation

			public bool Equals (ProjectFile x, ProjectFile y)
			{
				return x == y ||
					(x != null &&
					 y != null &&
					 x.Name == y.Name);
			}

			public int GetHashCode (ProjectFile obj)
			{
				return obj == null ? -1 : obj.Name.GetHashCode ();
			}

			#endregion
		}
	}
}

