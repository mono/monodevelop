//
// ProjectCodeDiagnosticProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.CodeIssues;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.CodeActions;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Refactoring;
using System.IO;

namespace MonoDevelop.CodeIssues
{
	class ProjectCodeDiagnosticProvider : CodeDiagnosticProvider
	{
		static Dictionary<DotNetProject, ProjectAnalyzers> cache = new Dictionary<DotNetProject, ProjectAnalyzers> ();

		class ProjectAnalyzers
		{
			List<AnalyzersFromAssembly> analyzers = new List<AnalyzersFromAssembly> ();

			public List<AnalyzersFromAssembly> Analyzers {
				get {
					return analyzers;
				}
			}

		}

		public ProjectCodeDiagnosticProvider ()
		{
			Ide.IdeApp.Workspace.SolutionLoaded += delegate {
				cache = new Dictionary<DotNetProject, ProjectAnalyzers> ();
			};
		}

		ProjectAnalyzers TryLoadCacheItem (DotNetProject netProject)
		{
			lock (cache) {
				if (cache.ContainsKey (netProject))
					return cache[netProject];
				var analyzers = new ProjectAnalyzers ();
				cache.Add (netProject, analyzers);

				var analyzerItems = netProject.Items.OfType<AnalyzerProjectItem> ();
				var shadowLoader = new ShadowCopyAnalyzerAssemblyLoader ();
				foreach (var item in analyzerItems) {
					if (File.Exists (item.FilePath)) {
						try {
							var assembly = shadowLoader.LoadCore (item.FilePath);
							var loader = new AnalyzersFromAssembly ();
							loader.AddAssembly (assembly, true);
							analyzers.Analyzers.Add (loader);
						} catch (Exception e) {
							LoggingService.LogWarning ("Can't load analyzers from " + item.FilePath, e);
						}
					}
				}
				return analyzers;
			}
		}

		internal static void ClearCache ()
		{
			cache = new Dictionary<DotNetProject, ProjectAnalyzers> ();
		}

		public override Task<IEnumerable<CodeDiagnosticDescriptor>> GetCodeDiagnosticDescriptorsAsync (DocumentContext document, string language, CancellationToken cancellationToken = default (CancellationToken))
		{
			var netProject = document.Project as DotNetProject;
			if (netProject == null)
				return TaskUtil.EmptyEnumerable<CodeDiagnosticDescriptor> ();

			var analyzers = TryLoadCacheItem (netProject);
			if (analyzers == null)
				return TaskUtil.EmptyEnumerable<CodeDiagnosticDescriptor> ();

			var result = new List<CodeDiagnosticDescriptor> ();
			foreach (var analyzer in analyzers.Analyzers) {
				result.AddRange (analyzer.Analyzers);
			}
			return Task.FromResult ((IEnumerable<CodeDiagnosticDescriptor>)result);
		}

		public override Task<IEnumerable<CodeDiagnosticFixDescriptor>> GetCodeFixDescriptorsAsync (DocumentContext document, string language, CancellationToken cancellationToken = default (CancellationToken))
		{
			var netProject = document.Project as DotNetProject;
			if (netProject == null)
				return TaskUtil.EmptyEnumerable<CodeDiagnosticFixDescriptor> ();
			var analyzers = TryLoadCacheItem (netProject);
			if (analyzers == null)
				return TaskUtil.EmptyEnumerable<CodeDiagnosticFixDescriptor> ();
			
			var result = new List<CodeDiagnosticFixDescriptor> ();
			foreach (var analyzer in analyzers.Analyzers) {
				result.AddRange (analyzer.Fixes);
			}
			return Task.FromResult ((IEnumerable<CodeDiagnosticFixDescriptor>)result);
		}

		public override Task<IEnumerable<CodeRefactoringDescriptor>> GetCodeRefactoringDescriptorsAsync (DocumentContext document, string language, CancellationToken cancellationToken = default (CancellationToken))
		{
			var netProject = document.Project as DotNetProject;
			if (netProject == null)
				return TaskUtil.EmptyEnumerable<CodeRefactoringDescriptor> ();
			var analyzers = TryLoadCacheItem (netProject);
			if (analyzers == null)
				return TaskUtil.EmptyEnumerable<CodeRefactoringDescriptor> ();
			
			var result = new List<CodeRefactoringDescriptor> ();
			foreach (var analyzer in analyzers.Analyzers) {
				result.AddRange (analyzer.Refactorings);
			}
			return Task.FromResult ((IEnumerable<CodeRefactoringDescriptor>)result);
		}
	}
}

