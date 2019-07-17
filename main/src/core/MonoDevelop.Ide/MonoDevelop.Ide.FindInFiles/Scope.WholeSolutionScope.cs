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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Ide.FindInFiles
{

	abstract partial class Scope
	{
		sealed class WholeSolutionScope : Scope
		{
			public override Task<IReadOnlyList<FileProvider>> GetFilesAsync (FindInFilesModel filterOptions, CancellationToken cancellationToken = default)
			{
				if (!IdeApp.Workspace.IsOpen) {
					return EmptyFileProviderTask;
				}

				var alreadyVisited = new HashSet<string> ();
				var results = new List<FileProvider> ();

				var options = new ParallelOptions ();
				options.MaxDegreeOfParallelism = 4;

				foreach (var solution in IdeApp.Workspace.GetAllSolutions ()) {
					Parallel.ForEach (solution.GetAllItems<SolutionFolderItem> (),
						options,
						() => new List<FileProvider> (),
						(item, loop, providers) => {
							if (item is SolutionFolder folder) {
								foreach (var file in folder.Files.Where (f => filterOptions.IsFileNameMatching (f.FileName) && File.Exists (f.FileName))) {
									if (!IdeServices.DesktopService.GetFileIsText (file.FileName))
										continue;
									lock (alreadyVisited) {
										if (alreadyVisited.Contains (file.FileName))
											continue;
										alreadyVisited.Add (file.FileName);
									}
									providers.Add (new FileProvider (file.FileName));
								}
							}

							if (item is Project project) {
								var conf = project.DefaultConfiguration?.Selector;
								foreach (var file in project.GetSourceFilesAsync (conf).Result) {
									if ((file.Flags & ProjectItemFlags.Hidden) == ProjectItemFlags.Hidden)
										continue;
									if (!filterOptions.IncludeCodeBehind && file.Subtype == Subtype.Designer)
										continue;
									if (!filterOptions.IsFileNameMatching (file.Name))
										continue;
									if (!IdeServices.DesktopService.GetFileIsText (file.Name))
										continue;
									if (!alreadyVisited.Add (file.Name))
										continue;
									providers.Add (new FileProvider (file.Name, project));
								}
							}
							return providers;
						}, (providers) => {
							lock (results) {
								results.AddRange (providers);
							}
						});
				}

				return Task.FromResult ((IReadOnlyList<FileProvider>)results);
			}

			public override string GetDescription (FindInFilesModel model)
			{
				return model.InReplaceMode
					? GettextCatalog.GetString ("Replacing '{0}' in all projects", model.FindPattern)
					: GettextCatalog.GetString ("Looking for '{0}' in all projects", model.FindPattern);
			}
		}
	}
}