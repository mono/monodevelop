// 
// Scope.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Ide.FindInFiles
{

	abstract partial class Scope
	{
		class WholeProjectScope : Scope
		{
			readonly Project project;

			public WholeProjectScope (Project project)
			{
				if (project == null)
					throw new ArgumentNullException ("project");

				this.project = project;
			}

			public override async Task<IReadOnlyList<FileProvider>> GetFilesAsync (FindInFilesModel filterOptions, CancellationToken cancellationToken)
			{
				var results = new List<FileProvider> ();
				if (IdeApp.Workspace.IsOpen) {
					var alreadyVisited = new HashSet<string> ();
					var conf = project.DefaultConfiguration?.Selector;
					foreach (ProjectFile file in await project.GetSourceFilesAsync (conf)) {
						if ((file.Flags & ProjectItemFlags.Hidden) == ProjectItemFlags.Hidden)
							continue;
						if (!filterOptions.IncludeCodeBehind && file.Subtype == Subtype.Designer)
							continue;
						if (!filterOptions.NameMatches (file.Name))
							continue;
						if (!IdeServices.DesktopService.GetFileIsText (file.Name))
							continue;
						if (!alreadyVisited.Add (file.FilePath.FullPath))
							continue;
						results.Add (new FileProvider (file.Name, project));
					}
				}
				return results;
			}

			public override string GetDescription (FindInFilesModel model)
			{
				if (!model.InReplaceMode)
					return GettextCatalog.GetString ("Looking for '{0}' in project '{1}'", model.FindPattern, project.Name);
				return GettextCatalog.GetString ("Replacing '{0}' in project '{1}'", model.FindPattern, project.Name);
			}
		}
	}
}