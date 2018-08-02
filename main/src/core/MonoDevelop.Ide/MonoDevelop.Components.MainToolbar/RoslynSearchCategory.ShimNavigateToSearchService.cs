//
// RoslynSearchCategory.ShimNavigateToSearchService.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.NavigateTo;

namespace MonoDevelop.Components.MainToolbar
{
	[Obsolete ("https://github.com/dotnet/roslyn/issues/28343")]
	class ShimNavigateToSearchService : INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate
	{
		private readonly INavigateToSearchService _navigateToSearchService;

		public ShimNavigateToSearchService (INavigateToSearchService navigateToSearchService)
		{
			_navigateToSearchService = navigateToSearchService;
		}

		public IImmutableSet<string> KindsProvided => ImmutableHashSet.Create<string> (StringComparer.Ordinal);

		public bool CanFilter => false;

		public Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync (Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
			=> _navigateToSearchService.SearchDocumentAsync (document, searchPattern, cancellationToken);

		public Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync (Project project, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
			=> _navigateToSearchService.SearchProjectAsync (project, searchPattern, cancellationToken);
	}
}
