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

using System.Linq;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace MonoDevelop.Ide.FindInFiles
{
	enum SearchScope
	{
		WholeWorkspace,
		CurrentProject,
		AllOpenFiles,
		Directories,
		CurrentDocument,
		Selection
	}

	abstract partial class Scope
	{
		public virtual PathMode PathMode {
			get {
				var workspace = IdeApp.Workspace;
				var solutions = workspace != null ? workspace.GetAllSolutions () : null;

				if (solutions != null && solutions.Count () == 1)
					return PathMode.Relative;

				return PathMode.Absolute;
			}
		}

		protected static readonly Task<IReadOnlyList<FileProvider>> EmptyFileProviderTask = Task.FromResult<IReadOnlyList<FileProvider>> (new FileProvider [0]);

		public abstract Task<IReadOnlyList<FileProvider>> GetFilesAsync (FindInFilesModel model, CancellationToken cancellationToken = default);

		public abstract string GetDescription (FindInFilesModel model);

		public virtual bool ValidateSearchOptions (FindInFilesModel filterOptions) => true;

		internal static Scope Create (FindInFilesModel model)
		{
			switch (model.SearchScope) {
			case SearchScope.CurrentDocument:
				return new DocumentScope ();
			case SearchScope.Selection:
				return new SelectionScope ();
			case SearchScope.WholeWorkspace:
				return new WholeSolutionScope ();
			case SearchScope.CurrentProject:
				var currentSelectedProject = IdeApp.ProjectOperations.CurrentSelectedProject;
				if (currentSelectedProject != null) {
					return new WholeProjectScope (currentSelectedProject);
				}
				return null;
			case SearchScope.AllOpenFiles:
				return new AllOpenFilesScope ();
			case SearchScope.Directories:
				if (!System.IO.Directory.Exists (model.FindInFilesPath))
					return null;

				return new DirectoryScope (model.FindInFilesPath, model.RecurseSubdirectories);
			default:
				throw new ApplicationException ("Unknown scope:" + model.SearchScope);
			}
		}
	}
}
