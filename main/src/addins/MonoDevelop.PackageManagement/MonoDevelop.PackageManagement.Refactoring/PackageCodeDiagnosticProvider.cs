//
// PackageCodeDiagnosticProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.CodeIssues;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement.Refactoring
{
	sealed class PackageCodeDiagnosticProvider : CodeDiagnosticProvider
	{
		static readonly Dictionary<Project, AnalyzersFromAssembly> diagnosticCache = new Dictionary<Project, AnalyzersFromAssembly> ();

		static PackageCodeDiagnosticProvider ()
		{
			IdeApp.Workspace.SolutionUnloaded += delegate {
				diagnosticCache.Clear ();
			};
			IdeApp.Workspace.ActiveConfigurationChanged += delegate {
				diagnosticCache.Clear ();
			};
		}

		public async override Task<IEnumerable<CodeDiagnosticFixDescriptor>> GetCodeFixDescriptorsAsync (DocumentContext document, string language, CancellationToken cancellationToken = default(CancellationToken))
		{
			var diags = await GetProjectDiagnosticsAsync (document.Project, language, cancellationToken).ConfigureAwait (false);
			return diags.Fixes;
		}

		public async override Task<IEnumerable<CodeDiagnosticDescriptor>> GetCodeDiagnosticDescriptorsAsync (DocumentContext document, string language, CancellationToken cancellationToken = default(CancellationToken))
		{
			var diags = await GetProjectDiagnosticsAsync (document.Project, language, cancellationToken).ConfigureAwait (false);
			return diags.Analyzers;
		}

		public async override Task<IEnumerable<MonoDevelop.CodeActions.CodeRefactoringDescriptor>> GetCodeRefactoringDescriptorsAsync (DocumentContext document, string language, CancellationToken cancellationToken)
		{
			var diags = await GetProjectDiagnosticsAsync (document.Project, language, cancellationToken).ConfigureAwait (false);
			return diags.Refactorings;
		}

		static async Task<AnalyzersFromAssembly> GetProjectDiagnosticsAsync (Project project, string language, CancellationToken cancellationToken)
		{
			if (project == null)
				return AnalyzersFromAssembly.Empty;
			AnalyzersFromAssembly result;
			if (diagnosticCache.TryGetValue(project, out result)) 
				return result;

			result = new AnalyzersFromAssembly ();

			var dotNetProject = project as DotNetProject;
			if (dotNetProject != null) {
				var proxy = new DotNetProjectProxy (dotNetProject);
				if (proxy.HasPackages ()) {
					var packagesPath = await GetPackagesPath (proxy);
					foreach (var file in Directory.EnumerateFiles (packagesPath, "*.dll", SearchOption.AllDirectories)) {
						cancellationToken.ThrowIfCancellationRequested ();
						try {
							var asm = Assembly.LoadFrom (file);
							result.AddAssembly (asm);
						} catch (Exception) {
						}
					}
				}
			}
			diagnosticCache[project] = result;
			return result;
		}

		static Task<FilePath> GetPackagesPath (IDotNetProject project)
		{
			return Runtime.RunInMainThread (() => {
				var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
				var nugetProject = solutionManager.GetNuGetProject (project);
				return nugetProject.GetPackagesFolderPath (solutionManager);
			});
		}
	}
}