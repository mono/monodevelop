using System;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Ide.TypeSystem
{
    internal interface IDynamicDocumentInfoProvider
    {
        event Action<DocumentInfo> Updated;
        DocumentInfo GetDynamicDocumentInfo(ProjectId projectId, string projectFilePath, string filePath);
        void RemoveDynamicDocumentInfo(ProjectId projectId, string projectFilePath, string filePath);
    }
}