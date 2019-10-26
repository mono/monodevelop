using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace MonoDevelop.Ide.TypeSystem
{
    class DynamicFileManager
    {
        private readonly MonoDevelopWorkspace monoDevelopWorkspace;
        private readonly ConcurrentDictionary<string, ProjectDynamicFileContext> projectContexts = new ConcurrentDictionary<string, ProjectDynamicFileContext>();

        internal ILookup<string, Lazy<IDynamicFileInfoProvider>> DynamicFileProvidersByExtension { get; set; }

        public DynamicFileManager(MonoDevelopWorkspace monoDevelopWorkspace)
        {
            this.monoDevelopWorkspace = monoDevelopWorkspace;
        }

        IEnumerable<IDynamicFileInfoProvider> GetDynamicFileProviders(string filePath)
        {
            return DynamicFileProvidersByExtension[Path.GetExtension(filePath) ?? string.Empty]
                .Select(lazy => lazy.Value);
        }

        public Task<ProjectInfo> UpdateDynamicFilesAsync(ProjectInfo projectInfo, IEnumerable<string> dynamicSourceFiles)
        {
            var projectContext = projectContexts.GetOrAdd(projectInfo.FilePath, _ => new ProjectDynamicFileContext(this));
            return projectContext.UpdateDynamicFilesAsync(projectInfo, dynamicSourceFiles);
        }

        private class ProjectDynamicFileContext
        {
            private ImmutableHashSet<string> dynamicSourceFiles = ImmutableHashSet<string>.Empty;
            private DynamicFileManager dynamicFileManager;

            public ProjectDynamicFileContext(DynamicFileManager dynamicFileManager)
            {
                this.dynamicFileManager = dynamicFileManager;
            }

            public async Task<ProjectInfo> UpdateDynamicFilesAsync(ProjectInfo projectInfo, IEnumerable<string> currentDynamicSourceFiles)
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
                        await dynamicFileProvider.RemoveDynamicFileInfoAsync(
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
                        var dynamicFileInfo = await dynamicFileProvider.GetDynamicFileInfoAsync(
                            projectInfo.Id,
                            projectInfo.FilePath,
                            document,
                            cancellationToken: default);

                        var docId = DocumentId.CreateNewId(projectInfo.Id, debugName: dynamicFileInfo.FilePath);

                        documents.Add(DocumentInfo.Create(
                                docId,
                                name: Path.GetFileName(document),
                                folders: null,
                                sourceCodeKind: dynamicFileInfo.SourceCodeKind,
                                loader: dynamicFileInfo.TextLoader,
                                filePath: dynamicFileInfo.FilePath,
                                isGenerated: true,
                                documentServiceProvider: dynamicFileInfo.DocumentServiceProvider);
                    }
                }

                projectInfo = projectInfo.WithAdditionalDocuments(documents);
                return projectInfo;
            }
        }
    }
}