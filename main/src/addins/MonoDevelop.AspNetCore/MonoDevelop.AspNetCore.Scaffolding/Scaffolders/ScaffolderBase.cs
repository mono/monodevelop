//
// Scaffolder.cs
//
// Author:
//       jasonimison <jaimison@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	abstract class ScaffolderBase
	{
		const string DbContextTypeName = "System.Data.Entity.DbContext";
		const string EF7DbContextTypeName = "Microsoft.Data.Entity.DbContext";
		const string EFCDbContextTypeName = "Microsoft.EntityFrameworkCore.DbContext";

		public virtual string Name { get; }
		public virtual string CommandLineName { get; }

        public List<CommandLineArg> DefaultArgs { get; protected set; } = new List<CommandLineArg>();

		public virtual IEnumerable<ScaffolderField> Fields { get; }

		protected ComboField GetDbContextField (ScaffolderArgs args)
		{
			return new ComboField ("--dataContext", GettextCatalog.GetString("DbContext class to use:"), GetDbContextClassesAsync(args.Project, args.CancellationToken), isEditable: true, placeholderText: "Select or type DbContext class name");
		}

		protected ComboField GetModelField (ScaffolderArgs args)
		{
			return new ComboField ("--model", GettextCatalog.GetString("Model class to use:"), GetModelClassesAsync(args.Project, args.CancellationToken), isEditable: true);
		}

		async Task<IEnumerable<string>> GetDbContextClassesAsync (DotNetProject project, CancellationToken token)
		{
			var compilation = await IdeApp.TypeSystemService.GetCompilationAsync (project, token);
			if (compilation != null) {
				var dbContext = compilation.GetTypeByMetadataName (EFCDbContextTypeName)
							 ?? compilation.GetTypeByMetadataName (DbContextTypeName)
							 ?? compilation.GetTypeByMetadataName (EF7DbContextTypeName);

				if (dbContext != null) {
					var result = await SymbolFinder.FindDerivedClassesAsync (dbContext, IdeApp.TypeSystemService.Workspace.CurrentSolution);

					return result.Where (ModelVisitor.IncludeTypeInAddViewModelClassDropdown).Select (c => c.MetadataName).Distinct().OrderBy (x => x);
				}
			}

			return Enumerable.Empty<string> ();
		}

		async Task<IEnumerable<string>> GetModelClassesAsync (DotNetProject project, CancellationToken token)
		{
			var compilation = await IdeApp.TypeSystemService.GetCompilationAsync (project, token);
			if (compilation != null) {
				var modelTypes = ModelVisitor.FindModelTypes (compilation.Assembly);
				var dbContextTypes = await GetDbContextClassesAsync (project, token);
				return modelTypes.Select (t => t.MetadataName).Except(dbContextTypes).Distinct().OrderBy (x => x);
			}
			return Enumerable.Empty<string> ();
		}
	}
}
