using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.TypeSystem
{
    //
    // Summary:
    //     Provider for the Microsoft.CodeAnalysis.Host.DynamicFileInfo implementer of this
    //     service should be pure free-thread meaning it can't switch to UI thread underneath.
    //     otherwise, we can get into dead lock if we wait for the dynamic file info from
    //     UI thread
    internal interface IDynamicFileInfoProvider
    {
        //
        // Summary:
        //     indicate content of a file has updated. the event argument "string" should be
        //     same as "filepath" given to Microsoft.CodeAnalysis.Host.IDynamicFileInfoProvider.GetDynamicFileInfoAsync(Microsoft.CodeAnalysis.ProjectId,System.String,System.String,System.Threading.CancellationToken)
        event EventHandler<string> Updated;

        //
        // Summary:
        //     return Microsoft.CodeAnalysis.Host.DynamicFileInfo for the context given
        //
        // Parameters:
        //   projectId:
        //     Microsoft.CodeAnalysis.ProjectId this file belongs to
        //
        //   projectFilePath:
        //     full path to project file (ex, csproj)
        //
        //   filePath:
        //     full path to non source file (ex, cshtml)
        //
        // Returns:
        //     null if this provider can't handle the given file
        Task<DynamicFileInfo> GetDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken);
        //
        // Summary:
        //     let provider know certain file has been removed
        //
        // Parameters:
        //   projectId:
        //     Microsoft.CodeAnalysis.ProjectId this file belongs to
        //
        //   projectFilePath:
        //     full path to project file (ex, csproj)
        //
        //   filePath:
        //     full path to non source file (ex, cshtml)
        Task RemoveDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken);
    }
}