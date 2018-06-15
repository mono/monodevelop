//
// MonoDevelopProjectCacheHostServiceFactory.cs
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
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.RoslynServices
{
	[ExportWorkspaceServiceFactory (typeof (IProjectCacheHostService), ServiceLayer.Host)]
	[Shared]
	class MonoDevelopProjectCacheHostServiceFactory : IWorkspaceServiceFactory
	{
		// Same as VSWin.
		const int ImplicitCacheTimeoutInMS = 10000;

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			// we support active document tracking only for visual studio workspace host.
			if (workspaceServices.Workspace is MonoDevelopWorkspace) {
				// We will finish setting this up in VisualStudioWorkspaceImpl.DeferredInitializationState
				var projectCacheService = new ProjectCacheService (workspaceServices.Workspace, ImplicitCacheTimeoutInMS);
				var documentTrackingService = workspaceServices.GetService<IDocumentTrackingService> ();

				// Subscribe to events so that we can cache items from the active document's project
				var manager = new ActiveProjectCacheManager (documentTrackingService, projectCacheService);

				// Subscribe to requests to clear the cache
				var workspaceCacheService = workspaceServices.GetService<IWorkspaceCacheService> ();
				if (workspaceCacheService != null) {
					workspaceCacheService.CacheFlushRequested += (s, e) => manager.Clear ();
				}

				// Also clear the cache when the solution is cleared or removed.
				workspaceServices.Workspace.WorkspaceChanged += (s, e) => {
					if (e.Kind == WorkspaceChangeKind.SolutionCleared || e.Kind == WorkspaceChangeKind.SolutionRemoved) {
						manager.Clear ();
					}
				};
				return projectCacheService;
			}

			// TODO: Handle miscellaneous files workspace later on.
			return new ProjectCacheService (workspaceServices.Workspace);
		}

		class ActiveProjectCacheManager
		{
			readonly IDocumentTrackingService _documentTrackingService;
			readonly ProjectCacheService _projectCacheService;
			readonly object lockObject = new object ();

			ProjectId _mostRecentActiveProjectId;
			IDisposable _mostRecentCache;

			public ActiveProjectCacheManager (IDocumentTrackingService documentTrackingService, ProjectCacheService projectCacheService)
			{
				_documentTrackingService = documentTrackingService;
				_projectCacheService = projectCacheService;

				if (documentTrackingService != null) {
					documentTrackingService.ActiveDocumentChanged += UpdateCache;
					UpdateCache (null, documentTrackingService.GetActiveDocument ());
				}
			}

			void UpdateCache (object sender, DocumentId activeDocument)
			{
				lock (lockObject) {
					if (activeDocument != null && activeDocument.ProjectId != _mostRecentActiveProjectId) {
						ClearMostRecentCache_NoLock ();
						_mostRecentCache = _projectCacheService.EnableCaching (activeDocument.ProjectId);
						_mostRecentActiveProjectId = activeDocument.ProjectId;
					}
				}
			}

			public void Clear ()
			{
				lock (lockObject) {
					// clear most recent cache
					ClearMostRecentCache_NoLock ();

					// clear implicit cache
					_projectCacheService.ClearImplicitCache ();
				}
			}

			void ClearMostRecentCache_NoLock ()
			{
				if (_mostRecentCache != null) {
					_mostRecentCache.Dispose ();
					_mostRecentCache = null;
				}

				_mostRecentActiveProjectId = null;
			}
		}
	}
}