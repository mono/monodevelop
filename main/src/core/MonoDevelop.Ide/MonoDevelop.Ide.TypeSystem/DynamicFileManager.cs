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
        readonly ConcurrentDictionary<ProjectId, ProjectDynamicFileContext> projectContexts = new ConcurrentDictionary<ProjectId, ProjectDynamicFileContext>();

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

        public ProjectInfo UpdateDynamicFiles(ProjectInfo projectInfo, IEnumerable<string> dynamicSourceFiles, Workspace workspace)
        {
            var projectContext = projectContexts.GetOrAdd(projectInfo.Id, _ => new ProjectDynamicFileContext(this, workspace));
            return projectContext.UpdateDynamicFiles(projectInfo, dynamicSourceFiles);
        }

        public void UnloadWorkspace(Workspace workspace)
        {
            foreach (var projectContextEntry in projectContexts.ToList())
            {
                if (projectContextEntry.Value.Workspace == workspace)
                {
                    UnloadProject(projectContextEntry.Key);
                }
            }
        }

        public void UnloadProject(ProjectId projectId)
        {
            projectContexts.TryRemove(projectId, out _);
        }

        IEnumerable<IDynamicDocumentInfoProvider> GetDynamicFileProviders(string filePath)
        {
            return DynamicFileProvidersByExtension[Path.GetExtension(filePath) ?? string.Empty]
                .Select(lazy => lazy.Value);
        }

        private Lazy<IDynamicDocumentInfoProvider> Wrap(Lazy<IDynamicDocumentInfoProvider> lazyProvider)
        {
            return new Lazy<IDynamicDocumentInfoProvider>(() =>
            {
                var provider = lazyProvider.Value;
                provider.Updated += OnDynamicDocumentUpdated;
                return provider;
            });
        }

        private void OnDynamicDocumentUpdated(DocumentInfo document)
        {
            if (projectContexts.TryGetValue(document.Id.ProjectId, out var projectContext))
            {
                if (projectContext.Workspace.IsDocumentOpen(document.Id))
                {
                    return;
                }

                projectContext.Workspace.OnDocumentReloaded(document);
            }
        }

        private class ProjectDynamicFileContext
        {
            private ImmutableHashSet<string> dynamicSourceFiles = ImmutableHashSet<string>.Empty;
            private DynamicFileManager dynamicFileManager;
            public Workspace Workspace { get; }

            public ProjectDynamicFileContext(DynamicFileManager dynamicFileManager, Workspace workspace)
            {
                this.dynamicFileManager = dynamicFileManager;
                this.Workspace = workspace;
            }

            public ProjectInfo UpdateDynamicFiles(ProjectInfo projectInfo, IEnumerable<string> currentDynamicSourceFiles)
            {
                var oldFiles = dynamicSourceFiles;
                var newFiles = currentDynamicSourceFiles.ToImmutableHashSet();
                dynamicSourceFiles = newFiles;

                var addedFiles = newFiles.Except(oldFiles);
                var removedFiles = oldFiles.Except(newFiles);

                foreach (var document in removedFiles)
                {
                    foreach (var dynamicFileProvider in dynamicFileManager.GetDynamicFileProviders(document))
                    {
                        dynamicFileProvider.RemoveDynamicDocumentInfo(
                            projectInfo.Id,
                            projectInfo.FilePath,
                            document,
                            cancellationToken: default);
                    }
                }

                List<DocumentInfo> documents = new List<DocumentInfo>();

                foreach (var document in addedFiles)
                {
                    foreach (var dynamicFileProvider in dynamicFileManager.GetDynamicFileProviders(document))
                    {
                        var dynamicFileInfo = dynamicFileProvider.GetDynamicDocumentInfo(
                            projectInfo.Id,
                            projectInfo.FilePath,
                            document,
                            cancellationToken: default);

                        if (dynamicFileInfo != null)
                        {
                            documents.Add(dynamicFileInfo);
                        }
                    }
                }

                projectInfo = projectInfo.WithAdditionalDocuments(documents);
                return projectInfo;
            }
        }
    }
}