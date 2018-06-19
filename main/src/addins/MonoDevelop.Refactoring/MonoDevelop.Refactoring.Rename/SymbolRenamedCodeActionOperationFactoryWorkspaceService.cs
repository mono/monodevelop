//
// SymbolRenamedCodeActionOperationFactoryWorkspaceService.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using MonoDevelop.Core;

namespace MonoDevelop.Refactoring.Rename
{
	[ExportWorkspaceService (typeof (ISymbolRenamedCodeActionOperationFactoryWorkspaceService), ServiceLayer.Host), Shared]
	sealed class SymbolRenamedCodeActionOperationFactoryWorkspaceService : ISymbolRenamedCodeActionOperationFactoryWorkspaceService
	{
		readonly IEnumerable<IRefactorNotifyService> refactorNotifyServices;

		[ImportingConstructor]
		public SymbolRenamedCodeActionOperationFactoryWorkspaceService (
			[ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices)
		{
			this.refactorNotifyServices = refactorNotifyServices;
		}

		public CodeActionOperation CreateSymbolRenamedOperation (ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution)
		{
			return new RenameSymbolOperation (
				refactorNotifyServices,
				symbol ?? throw new ArgumentNullException (nameof (symbol)),
				newName ?? throw new ArgumentNullException (nameof (newName)),
				startingSolution ?? throw new ArgumentNullException (nameof (startingSolution)),
				updatedSolution ?? throw new ArgumentNullException (nameof (updatedSolution)));
		}

		class RenameSymbolOperation : CodeActionOperation
		{
			readonly IEnumerable<IRefactorNotifyService> refactorNotifyServices;
			readonly ISymbol symbol;
			readonly string newName;
			readonly Solution startingSolution;
			readonly Solution updatedSolution;

			public RenameSymbolOperation (
				IEnumerable<IRefactorNotifyService> refactorNotifyServices,
				ISymbol symbol,
				string newName,
				Solution startingSolution,
				Solution updatedSolution)
			{
				this.refactorNotifyServices = refactorNotifyServices;
				this.symbol = symbol;
				this.newName = newName;
				this.startingSolution = startingSolution;
				this.updatedSolution = updatedSolution;
			}

			public override void Apply (Workspace workspace, CancellationToken cancellationToken = default (CancellationToken))
			{
				var updatedDocumentIds = updatedSolution.GetChanges (startingSolution).GetProjectChanges ().SelectMany (p => p.GetChangedDocuments ());

				foreach (var refactorNotifyService in refactorNotifyServices) {
					if (refactorNotifyService.TryOnBeforeGlobalSymbolRenamed (workspace, updatedDocumentIds, symbol, newName, false)) {
						refactorNotifyService.TryOnAfterGlobalSymbolRenamed (workspace, updatedDocumentIds, symbol, newName, false);
					}
				}
			}

			public override string Title => GettextCatalog.GetString ("Rename {0} to {1}", symbol.Name, newName);
		}
	}
}
