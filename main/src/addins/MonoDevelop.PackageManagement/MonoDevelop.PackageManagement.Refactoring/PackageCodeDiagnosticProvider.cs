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
using MonoDevelop.PackageManagement;
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
using System.Security.Cryptography;
using System.Text;
using MonoDevelop.Core;

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
		static MD5 md5 = MD5.Create (); 

		public static string GetMD5 (byte[] data)
		{
			var result = new StringBuilder();
			foreach (var b in md5.ComputeHash (data)) {
				result.Append(b.ToString("X2"));
			}
			return result.ToString();
		}

		internal static ConfigurationProperty<string> loadAnalyzers = ConfigurationProperty.Create ("LoadAnalyzers", "");
		internal static ConfigurationProperty<string> knownAnalyzers = ConfigurationProperty.Create ("KnownAnalyzers", "");

		async static Task<AnalyzersFromAssembly> GetProjectDiagnosticsAsync (Project project, string language, CancellationToken cancellationToken)
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
				var analyzingDomain = AppDomain.CreateDomain ("analyzerDomain");
				if (proxy.HasPackages ()) {
					var packagesPath = new SolutionPackageRepositoryPath (proxy, PackageManagementServices.Options);
					foreach (var file in Directory.EnumerateFiles (packagesPath.PackageRepositoryPath, "*.dll", SearchOption.AllDirectories)) {
						cancellationToken.ThrowIfCancellationRequested ();
						try {
							var assembly = File.ReadAllBytes (file);
							var analyzers = new AnalyzersFromAssembly ();
							analyzers.AddAssembly (analyzingDomain.Load (assembly));
							if (analyzers.Fixes.Count + analyzers.Analyzers.Count + analyzers.Refactorings.Count == 0)
								continue;

							var md5 = GetMD5 (assembly);
							if (!knownAnalyzers.Value.Contains (md5)) {
								knownAnalyzers.Value += md5 + "," + Path.GetFileName (file) + ",";
								await Runtime.RunInMainThread (delegate {
									var button = MessageService.AskQuestion (GettextCatalog.GetString ("The assembly {0} contains analyzers. Load them ?", Path.GetFileName (file)), AlertButton.Yes, AlertButton.No);
									if (button == AlertButton.Yes) {
										loadAnalyzers.Value += md5 + ",";
										AddAssembly (result, file, md5);
									}
								});
							} else {
								if (loadAnalyzers.Value.Contains (md5))
									AddAssembly (result, file, md5);
							}
						} catch (Exception) {
						}
					}
					AppDomain.Unload (analyzingDomain); 
				}
			}
			diagnosticCache[project] = result;
			return result;
		}

		static void AddAssembly (AnalyzersFromAssembly result, string file, string md5)
		{
			var asm = Assembly.LoadFrom (file);
			result.AddAssembly (asm);
		}
	}
}