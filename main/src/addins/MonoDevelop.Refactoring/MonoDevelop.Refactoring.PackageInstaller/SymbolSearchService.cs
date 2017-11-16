//
// PackageInstallerService.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace MonoDevelop.Refactoring.PackageInstaller
{
	[ExportWorkspaceService (typeof (ISymbolSearchService), ServiceLayer.Host), Shared]
	class SymbolSearchService : ISymbolSearchService
	{

		async Task<IList<PackageWithAssemblyResult>> ISymbolSearchService.FindPackagesWithAssemblyAsync (string source, string assemblyName, CancellationToken cancellationToken)
		{
			if (PackageInstallerServiceFactory.PackageServices == null)
				return ImmutableArray<PackageWithAssemblyResult>.Empty;
			var result = new List<PackageWithAssemblyResult> ();
			foreach (var tuple in await PackageInstallerServiceFactory.PackageServices.FindPackagesWithAssemblyAsync (source, assemblyName, cancellationToken)) {
				result.Add (new PackageWithAssemblyResult (tuple.PackageName, tuple.Version, tuple.Rank));
			}
			return result.ToImmutableArray ();
		}

		async Task<IList<PackageWithTypeResult>> ISymbolSearchService.FindPackagesWithTypeAsync (string source, string name, int arity, CancellationToken cancellationToken)
		{
			if (PackageInstallerServiceFactory.PackageServices == null)
				return ImmutableArray<PackageWithTypeResult>.Empty;

			var result = new List<PackageWithTypeResult> ();
			foreach (var tuple in await PackageInstallerServiceFactory.PackageServices.FindPackagesWithTypeAsync (source, name, arity, cancellationToken)) {
				result.Add (new PackageWithTypeResult (tuple.PackageName, tuple.TypeName, tuple.Version, tuple.Rank, tuple.ContainingNamespaceNames));
			}
			return result.ToImmutableArray ();
		}

		async Task<IList<ReferenceAssemblyWithTypeResult>> ISymbolSearchService.FindReferenceAssembliesWithTypeAsync (string name, int arity, CancellationToken cancellationToken)
		{
			if (PackageInstallerServiceFactory.PackageServices == null)
				return ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty;
			var result = new List<ReferenceAssemblyWithTypeResult> ();
			foreach (var tuple in await PackageInstallerServiceFactory.PackageServices.FindReferenceAssembliesWithTypeAsync (name, arity, cancellationToken)) {
				result.Add (new ReferenceAssemblyWithTypeResult (tuple.AssemblyName, tuple.TypeName, tuple.ContainingNamespaceNames));
			}
			return result.ToImmutableArray ();
		}
	}
}
