// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis;
using System.Composition;

namespace MonoDevelop.Ide.TypeSystem
{
//	[ExportWorkspaceServiceFactory(typeof(IProjectCacheHostService), ServiceLayer.Host)]
//	[Shared]
	internal partial class MonoDevelopProjectCacheHostServiceFactory : IWorkspaceServiceFactory
	{
		private const int ImplicitCacheTimeoutInMS = 10000;

		public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
		{
			// we support active document tracking only for visual studio workspace host.
			if (workspaceServices.Workspace is MonoDevelopWorkspace)
			{
				return GetMonoDevelopProjectCache(workspaceServices);
			}

			return GetMiscProjectCache(workspaceServices);
		}

		private static IWorkspaceService GetMiscProjectCache(HostWorkspaceServices workspaceServices)
		{
			var projectCacheService = new ProjectCacheService(workspaceServices.Workspace, ImplicitCacheTimeoutInMS);

			// Also clear the cache when the solution is cleared or removed.
			workspaceServices.Workspace.WorkspaceChanged += (s, e) =>
			{
				if (e.Kind == WorkspaceChangeKind.SolutionCleared || e.Kind == WorkspaceChangeKind.SolutionRemoved)
				{
					projectCacheService.ClearImplicitCache();
				}
			};

			return projectCacheService;
		}

		private static IWorkspaceService GetMonoDevelopProjectCache(HostWorkspaceServices workspaceServices)
		{
			var projectCacheService = new ProjectCacheService(workspaceServices.Workspace, ImplicitCacheTimeoutInMS);

			var documentTrackingService = workspaceServices.GetService<IMDDocumentTrackingService>();

			// Subscribe to events so that we can cache items from the active document's project
			var manager = new ActiveProjectCacheManager(documentTrackingService, projectCacheService);

			// TODO: Roslyn, when VS gets request from operating system that system virutal memory is low
			// CacheFlushRequested is invoked so caches are cleared to get some memory back...

			// Subscribe to requests to clear the cache
//			var workspaceCacheService = workspaceServices.GetService<IWorkspaceCacheService>();
//			if (workspaceCacheService != null)
//			{
//				workspaceCacheService.CacheFlushRequested += (s, e) => manager.Clear();
//			}

			// Also clear the cache when the solution is cleared or removed.
			workspaceServices.Workspace.WorkspaceChanged += (s, e) =>
			{
				if (e.Kind == WorkspaceChangeKind.SolutionCleared || e.Kind == WorkspaceChangeKind.SolutionRemoved)
				{
					manager.Clear();
				}
			};

			return projectCacheService;
		}

		private class ActiveProjectCacheManager
		{
			private readonly IMDDocumentTrackingService _documentTrackingService;
			private readonly ProjectCacheService _projectCacheService;
			private readonly object _guard = new object();

			private ProjectId _mostRecentActiveProjectId;
			private IDisposable _mostRecentCache;

			public ActiveProjectCacheManager(IMDDocumentTrackingService documentTrackingService, ProjectCacheService projectCacheService)
			{
				_documentTrackingService = documentTrackingService;
				_projectCacheService = projectCacheService;

				if (documentTrackingService != null)
				{
					documentTrackingService.ActiveDocumentChanged += UpdateCache;
					UpdateCache(null, documentTrackingService.GetActiveDocument());
				}
			}

			private void UpdateCache(object sender, DocumentId activeDocument)
			{
				lock (_guard)
				{
					if (activeDocument != null && activeDocument.ProjectId != _mostRecentActiveProjectId)
					{
						ClearMostRecentCache_NoLock();
						_mostRecentCache = _projectCacheService.EnableCaching(activeDocument.ProjectId);
						_mostRecentActiveProjectId = activeDocument.ProjectId;
					}
				}
			}

			public void Clear()
			{
				lock (_guard)
				{
					// clear most recent cache
					ClearMostRecentCache_NoLock();

					// clear implicit cache
					_projectCacheService.ClearImplicitCache();
				}
			}

			private void ClearMostRecentCache_NoLock()
			{
				if (_mostRecentCache != null)
				{
					_mostRecentCache.Dispose();
					_mostRecentCache = null;
				}

				_mostRecentActiveProjectId = null;
			}
		}
	}
}

