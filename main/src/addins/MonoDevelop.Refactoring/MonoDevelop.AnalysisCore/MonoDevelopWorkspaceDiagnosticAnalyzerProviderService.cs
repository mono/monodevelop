//
// MonoDevelopWorkspaceDiagnosticAnalyzerProviderService.cs
//
// Author:
//       therzok <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2017 (c) Marius Ungureanu
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
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.AnalysisCore
{
	[Export(typeof(IWorkspaceDiagnosticAnalyzerProviderService))]
	partial class MonoDevelopWorkspaceDiagnosticAnalyzerProviderService : IWorkspaceDiagnosticAnalyzerProviderService
	{
		static readonly AnalyzerAssemblyLoader analyzerAssemblyLoader = new AnalyzerAssemblyLoader ();
		readonly static string diagnosticAnalyzerAssembly = typeof (DiagnosticAnalyzerAttribute).Assembly.GetName ().Name;

		private TaskCompletionSource<OptionsTable> optionsCompletionSource = new TaskCompletionSource<OptionsTable> ();
		internal Task<OptionsTable> GetOptionsAsync () => optionsCompletionSource.Task;
		readonly Task<ImmutableArray<HostDiagnosticAnalyzerPackage>> hostDiagnosticAnalyzerInfoTask;

		const string extensionPath = "/MonoDevelop/Refactoring/AnalyzerAssemblies";
		string [] RuntimeEnabledAssemblies;
		public MonoDevelopWorkspaceDiagnosticAnalyzerProviderService ()
		{
			RefactoringEssentials.NRefactory6Host.GetLocalizedString = GettextCatalog.GetString;
			hostDiagnosticAnalyzerInfoTask = Task.Run (() => CreateHostDiagnosticAnalyzerPackages ());
		}

		void LoadAnalyzerAssemblies()
		{
			RuntimeEnabledAssemblies = AddinManager.GetExtensionNodes<AssemblyExtensionNode> (extensionPath).Select (b => b.FileName).ToArray ();
		}

		public IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader ()
		{
			return analyzerAssemblyLoader;
		}

		public IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages ()
		{
			return hostDiagnosticAnalyzerInfoTask.Result;
		}

		ImmutableArray<HostDiagnosticAnalyzerPackage> CreateHostDiagnosticAnalyzerPackages ()
		{
			LoadAnalyzerAssemblies ();
			var builder = ImmutableArray.CreateBuilder<HostDiagnosticAnalyzerPackage> ();
			var assemblies = ImmutableArray.CreateBuilder<string> ();
			var options = new OptionsTable ();
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
				try {
					var assemblyName = asm.GetName ().Name;
					if (Array.IndexOf (RuntimeEnabledAssemblies, assemblyName) == -1) {
						switch (assemblyName) {
						//blacklist
						case "FSharpBinding":
							continue;
						//addin assemblies that reference roslyn
						default:
							var refAsm = asm.GetReferencedAssemblies ();
							if (refAsm.Any (a => a.Name == diagnosticAnalyzerAssembly))
								break;
							continue;
						}
					}

					// Figure out a way to disable E&C analyzers.
					assemblies.Add (asm.Location);
					options.ProcessAssembly (asm);
				} catch (Exception e) {
					LoggingService.LogError ("Error while loading diagnostics in " + asm.FullName, e);
				}
			}
			optionsCompletionSource.SetResult (options);
			builder.Add (new HostDiagnosticAnalyzerPackage ("MonoDevelop", assemblies.AsImmutable ()));

			// Go through all providers
			return builder.AsImmutable ();
		}
	}
}
