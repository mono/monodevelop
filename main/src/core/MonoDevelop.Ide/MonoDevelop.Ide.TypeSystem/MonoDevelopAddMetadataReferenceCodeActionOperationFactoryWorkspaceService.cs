//
// MonoDevelopAddMetadataReferenceCodeActionOperationFactoryWorkspaceService.cs
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
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceService (typeof (IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService), ServiceLayer.Host), Shared]
	sealed class MonoDevelopAddMetadataReferenceCodeActionOperationFactoryWorkspaceService : IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService
	{
		public CodeActionOperation CreateAddMetadataReferenceOperation (ProjectId projectId, AssemblyIdentity assemblyIdentity)
		{
			if (projectId == null)
				throw new ArgumentNullException (nameof (projectId));
			if (assemblyIdentity == null)
				throw new ArgumentNullException (nameof (assemblyIdentity));
			return new AddMetadataReferenceOperation (projectId, assemblyIdentity);
		}

		class AddMetadataReferenceOperation : CodeActionOperation
		{
			readonly AssemblyIdentity assemblyIdentity;
			readonly ProjectId projectId;

			public AddMetadataReferenceOperation (ProjectId projectId, AssemblyIdentity assemblyIdentity)
			{
				this.projectId = projectId;
				this.assemblyIdentity = assemblyIdentity;
			}

			public override void Apply (Workspace workspace, CancellationToken cancellationToken = default (CancellationToken))
			{
				var mdWorkspace = workspace as MonoDevelopWorkspace;
				if (mdWorkspace == null)
					return; // no md workspace -> not a common file/ignore.
				var mdProject = mdWorkspace.GetMonoProject (projectId) as MonoDevelop.Projects.DotNetProject;
				if (mdProject == null) {
					LoggingService.LogWarning ("Can't find project  " + projectId + " to add reference " + assemblyIdentity.GetDisplayName ());
					return;
				}
				var newReference = MonoDevelop.Projects.ProjectReference.CreateAssemblyReference (assemblyIdentity.GetDisplayName ());
				foreach (var r in mdProject.References) {
					if (r.ReferenceType == newReference.ReferenceType && r.Reference == newReference.Reference) {
						LoggingService.LogWarning ("Warning duplicate reference is added " + newReference.Reference);
						return;
					}
				}

				mdProject.References.Add (newReference);
				IdeApp.ProjectOperations.SaveAsync (mdProject);
			}

			public override string Title => string.Format (GettextCatalog.GetString("Add a reference to '{0}'"), assemblyIdentity.GetDisplayName ());
		}
	}
}
