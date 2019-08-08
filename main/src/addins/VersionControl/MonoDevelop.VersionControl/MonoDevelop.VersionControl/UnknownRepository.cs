
using System;
using System.Collections;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl
{
	public class UnknownRepository: Repository, IExtendedDataItem
	{
		Hashtable properties;
		
		public IDictionary ExtendedProperties {
			get {
				if (properties == null) properties = new Hashtable ();
				return properties;
			}
		}

		public override Task<string> GetBaseTextAsync (FilePath localFile, CancellationToken cancellationToken) => Task.FromResult ((string)null);

		protected override Task<string> OnGetTextAtRevisionAsync (FilePath repositoryPath, Revision revision, CancellationToken cancellationToken) => Task.FromResult ((string)null);

		protected override Task<Revision []> OnGetHistoryAsync (FilePath localFile, Revision since, CancellationToken cancellationToken) => Task.FromResult ((Revision[])null);

		protected override Task<IReadOnlyList<VersionInfo>> OnGetVersionInfoAsync (IEnumerable<FilePath> paths, bool getRemoteStatus, CancellationToken cancellationToken)
		{
			var result = new List<VersionInfo> ();
			foreach (var p in paths)
				result.Add (VersionInfo.CreateUnversioned (p, System.IO.Directory.Exists (p)));
			return Task.FromResult ((IReadOnlyList<VersionInfo>)result);
		}

		protected override Task<VersionInfo []> OnGetDirectoryVersionInfoAsync (FilePath localDirectory, bool getRemoteStatus, bool recursive, CancellationToken cancellationToken) => Task.FromResult(new VersionInfo [0]);

		protected override Task<Repository> OnPublishAsync (string serverPath, FilePath localPath, FilePath [] files, string message, ProgressMonitor monitor) => Task.FromResult ((Repository)null);

		protected override Task OnUpdateAsync (FilePath[] localPaths, bool recurse, ProgressMonitor monitor) => Task.CompletedTask;

		protected override Task OnCommitAsync (ChangeSet changeSet, ProgressMonitor monitor) => Task.CompletedTask;

		protected override Task OnCheckoutAsync (FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor) => Task.CompletedTask;

		protected override Task OnRevertAsync (FilePath [] localPaths, bool recurse, ProgressMonitor monitor) => Task.CompletedTask;

		protected override Task OnRevertRevisionAsync (FilePath localPath, Revision revision, ProgressMonitor monitor) => Task.CompletedTask;

		protected override Task OnRevertToRevisionAsync (FilePath localPath, Revision revision, ProgressMonitor monitor) => Task.CompletedTask;

		protected override Task OnAddAsync (FilePath[] localPaths, bool recurse, ProgressMonitor monitor) => Task.CompletedTask;

		protected override Task OnMoveFileAsync (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor) => Task.CompletedTask;

		protected override Task OnMoveDirectoryAsync (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor) => Task.CompletedTask;

		protected override Task OnDeleteFilesAsync (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal) => Task.CompletedTask;

		protected override Task OnDeleteDirectoriesAsync (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal) => Task.CompletedTask;

		protected override Task OnIgnoreAsync (FilePath[] localPath, CancellationToken cancellationToken) => Task.CompletedTask;

		protected override Task OnUnignoreAsync (FilePath [] localPath, CancellationToken cancellationToken) => Task.CompletedTask;
		
		public override Task<Annotation []> GetAnnotationsAsync (FilePath repositoryPath, Revision since, CancellationToken cancellationToken) => Task.FromResult (new Annotation [0]);

		protected override Task<RevisionPath []> OnGetRevisionChangesAsync (Revision revision, CancellationToken cancellationToken = default) => Task.FromResult (new RevisionPath [0]);
	}
}
