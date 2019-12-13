// Copyright (c) Microsoft
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace MonoDevelop.Ide.TypeSystem
{
	[Export]
	class DynamicFileManager
	{
		readonly ConcurrentDictionary<ProjectId, ProjectDynamicFileContext> projectContexts = new ConcurrentDictionary<ProjectId, ProjectDynamicFileContext> ();

		internal ILookup<string, Lazy<IDynamicDocumentInfoProvider>> DynamicFileProvidersByExtension { get; set; }

		[ImportMany]
		private IEnumerable<Lazy<IDynamicDocumentInfoProvider, FileExtensionsMetadata>> DynamicFileProviders
		{
			set
			{
				DynamicFileProvidersByExtension = value
					.Select(lazy => (lazyProvider: Wrap(lazy), extensions: lazy.Metadata.Extensions))
					.SelectMany(t => t.extensions.Select(extension => (t.lazyProvider, extension: "." + extension)))
					.ToLookup(t => t.extension, t => t.lazyProvider, StringComparer.OrdinalIgnoreCase);
			}
		}

		public ProjectInfo UpdateDynamicFiles (ProjectInfo projectInfo, IEnumerable<string> dynamicSourceFiles, Workspace workspace)
		{
			var projectContext = projectContexts.GetOrAdd (projectInfo.Id, _ => new ProjectDynamicFileContext (this, workspace));
			return projectContext.UpdateDynamicFiles (projectInfo, dynamicSourceFiles);
		}

		public void UnloadWorkspace (Workspace workspace)
		{
			foreach (var projectContextEntry in projectContexts.ToList ()) {
				if (projectContextEntry.Value.Workspace == workspace) {
					UnloadProject (projectContextEntry.Key);
				}
			}
		}

		public void UnloadProject (ProjectId projectId)
		{
			projectContexts.TryRemove (projectId, out _);
		}

		IEnumerable<IDynamicDocumentInfoProvider> GetDynamicFileProviders (string filePath)
		{
			return DynamicFileProvidersByExtension[Path.GetExtension(filePath) ?? string.Empty]
				.Select(lazy => lazy.Value);
		}

		private Lazy<IDynamicDocumentInfoProvider> Wrap (Lazy<IDynamicDocumentInfoProvider> lazyProvider)
		{
			return new Lazy<IDynamicDocumentInfoProvider> (() =>
			{
				var provider = lazyProvider.Value;
				provider.Updated += OnDynamicDocumentUpdated;
				return provider;
			});
		}

		private void OnDynamicDocumentUpdated (DocumentInfo document)
		{
			if (projectContexts.TryGetValue (document.Id.ProjectId, out var projectContext)) {
				var workspace = projectContext.Workspace;
				if (workspace.IsDocumentOpen (document.Id)) {
					return;
				}

				if (!workspace.CurrentSolution.ContainsDocument (document.Id)) {
					// By the time we get called back from Razor the project might
					// have been unloaded
					return;
				}

				workspace.OnDocumentReloaded (document);
			}
		}

		private class ProjectDynamicFileContext
		{
			private ImmutableHashSet<string> dynamicSourceFiles = ImmutableHashSet<string>.Empty;
			private DynamicFileManager dynamicFileManager;
			public Workspace Workspace { get; }

			public ProjectDynamicFileContext (DynamicFileManager dynamicFileManager, Workspace workspace)
			{
				this.dynamicFileManager = dynamicFileManager;
				this.Workspace = workspace;
			}

			public ProjectInfo UpdateDynamicFiles (ProjectInfo projectInfo, IEnumerable<string> currentDynamicSourceFiles)
			{
				var oldFiles = dynamicSourceFiles;
				var newFiles = currentDynamicSourceFiles.ToImmutableHashSet ();
				dynamicSourceFiles = newFiles;

				var removedFiles = oldFiles.Except (newFiles);

				foreach (var document in removedFiles) {
					foreach (var dynamicFileProvider in dynamicFileManager.GetDynamicFileProviders (document)) {
						dynamicFileProvider.RemoveDynamicDocumentInfo (
							projectInfo.Id,
							projectInfo.FilePath,
							document);
					}
				}

				List<DocumentInfo> documents = new List<DocumentInfo> ();

				foreach (var document in dynamicSourceFiles) {
					foreach (var dynamicFileProvider in dynamicFileManager.GetDynamicFileProviders (document)) {
						var dynamicFileInfo = dynamicFileProvider.GetDynamicDocumentInfo (
							projectInfo.Id,
							projectInfo.FilePath,
							document);

						bool alreadyAdded = projectInfo.Documents.Any (d => string.Equals (d.FilePath, document, StringComparison.OrdinalIgnoreCase));

						if (dynamicFileInfo != null && !alreadyAdded) {
							documents.Add (dynamicFileInfo);
						}
					}
				}

				projectInfo = projectInfo.WithDocuments(projectInfo.Documents.Concat(documents));
				return projectInfo;
			}
		}
	}
}