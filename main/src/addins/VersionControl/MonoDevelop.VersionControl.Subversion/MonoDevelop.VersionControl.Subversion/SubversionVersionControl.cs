using System;
using System.Collections.Generic;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.VersionControl.Subversion.Gui;
using System.Linq;

namespace MonoDevelop.VersionControl.Subversion
{
	public abstract class SubversionVersionControl : VersionControlSystem
	{
		public virtual string GetDirectoryDotSvn (FilePath path)
		{
			if (Directory.Exists (path.Combine (".svn")))
				return path;

			return String.Empty;
		}

		public override string Name
		{
			get { return "Subversion"; }
		}

		public abstract SubversionBackend CreateBackend ();

		public override Repository GetRepositoryReference (FilePath path, string id)
		{
			return new SubversionRepository (this, null, path);
		}

		protected override FilePath OnGetRepositoryPath (FilePath path, string id)
		{
			return GetDirectoryDotSvn (path);
		}

		protected override Repository OnCreateRepositoryInstance ()
		{
			return new SubversionRepository ();
		}
		
		public override IRepositoryEditor CreateRepositoryEditor (Repository repo)
		{
			return new UrlBasedRepositoryEditor ((SubversionRepository)repo);
		}

		internal protected virtual bool InstallDependencies ()
		{
			return true;
		}
	}

	public abstract class SubversionBackend
	{
		public abstract string GetTextBase (string sourcefile);

		internal static string GetDirectoryDotSvn (SubversionVersionControl vcs, FilePath path)
		{
			return vcs.GetDirectoryDotSvn (path);
		}

		public Revision[] GetHistory (Repository repo, FilePath sourcefile, Revision since)
		{
			SvnRevision startrev = SvnRevision.Working;
			SvnRevision sincerev = SvnRevision.First;
			if (since != null)
				sincerev = (SvnRevision) since;

			return Log (repo, sourcefile, startrev, sincerev).ToArray ();
		}

		public abstract IEnumerable<SvnRevision> Log (Repository repo, FilePath path, SvnRevision revisionStart, SvnRevision revisionEnd);

		/// <summary>
		/// Returns the text for a file at a particular revision. If the file is binary, 'null' is returned
		/// </summary>
		/// <returns>
		/// The text at revision or 'null' if the file is binary.
		/// </returns>
		/// <param name='repositoryPath'>
		/// Repository path.
		/// </param>
		/// <param name='revision'>
		/// Revision.
		/// </param>
		public abstract string GetTextAtRevision (string repositoryPath, Revision revision, string rootPath);

		internal protected virtual VersionControlOperation GetSupportedOperations (Repository repo, VersionInfo vinfo, VersionControlOperation defaultValue)
		{
			if (vinfo.IsVersioned && File.Exists (vinfo.LocalPath) && !Directory.Exists (vinfo.LocalPath) && vinfo.HasLocalChange (VersionStatus.ScheduledDelete))
				defaultValue |= VersionControlOperation.Add;
			return defaultValue;
		}

		public VersionInfo GetVersionInfo (Repository repo, FilePath localPath, bool getRemoteStatus)
		{
			// Check for directory before checking for file, since directory links may appear as files
			var isDir = Directory.Exists (localPath);
			try {
				if (isDir)
					return GetDirStatus (repo, localPath, getRemoteStatus);
				return GetFileStatus (repo, localPath, getRemoteStatus);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to query subversion status", e);
				return VersionInfo.CreateUnversioned (localPath, isDir);
			}
		}

		private VersionInfo GetFileStatus (Repository repo, FilePath sourcefile, bool getRemoteStatus)
		{
			SubversionRepository srepo = (SubversionRepository)repo;
			SubversionVersionControl vcs = (SubversionVersionControl)repo.VersionControlSystem;
			// If the directory is not versioned, there is no version info
			if (!Directory.Exists (GetDirectoryDotSvn (vcs, sourcefile.ParentDirectory)))
				return VersionInfo.CreateUnversioned (sourcefile, false);
			if (!sourcefile.IsChildPathOf (srepo.RootPath))
				return VersionInfo.CreateUnversioned (sourcefile, false);

			var statuses = new List<VersionInfo> (Status (repo, sourcefile, SvnRevision.Head, false, false, getRemoteStatus));

			if (statuses.Count == 0)
				return VersionInfo.CreateUnversioned (sourcefile, false);
			
			if (statuses.Count != 1)
				return VersionInfo.CreateUnversioned (sourcefile, false);
			
			VersionInfo ent = statuses [0];
			if (ent.IsDirectory)
				return VersionInfo.CreateUnversioned (sourcefile, false);
			
			return ent;
		}

		private VersionInfo GetDirStatus (Repository repo, FilePath localPath, bool getRemoteStatus)
		{
			SubversionVersionControl vcs = (SubversionVersionControl)repo.VersionControlSystem;
			// If the directory is not versioned, there is no version info
			if (!Directory.Exists (GetDirectoryDotSvn (vcs, localPath)))
				return VersionInfo.CreateUnversioned (localPath, true);
				
			foreach (VersionInfo ent in Status (repo, localPath, SvnRevision.Head, false, false, getRemoteStatus)) {
				if (ent.LocalPath.CanonicalPath == localPath.CanonicalPath)
					return ent;
			}
			return VersionInfo.CreateUnversioned (localPath, true);
		}

		public VersionInfo[] GetDirectoryVersionInfo (Repository repo, FilePath sourcepath, bool getRemoteStatus, bool recursive)
		{
			try {
				return Status (repo, sourcepath, SvnRevision.Head, recursive, true, getRemoteStatus).ToArray ();
			} catch (Exception e) {
				LoggingService.LogError ("Failed to get subversion directory status", e);
				return new VersionInfo [0];
			}
		}

		public abstract IEnumerable<VersionInfo> Status (Repository repo, FilePath path, SvnRevision revision, bool descendDirs, bool changedItemsOnly, bool remoteStatus);

		public abstract void Update (FilePath path, bool recurse, ProgressMonitor monitor);

		public abstract void Commit (FilePath[] paths, string message, ProgressMonitor monitor);

		public abstract void Mkdir (string[] paths, string message, ProgressMonitor monitor);

		public abstract void Checkout (string url, FilePath path, Revision rev, bool recurse, ProgressMonitor monitor);

		public abstract void Revert (FilePath[] paths, bool recurse, ProgressMonitor monitor);

		public abstract void RevertRevision (FilePath path, Revision revision, ProgressMonitor monitor);

		public abstract void RevertToRevision (FilePath path, Revision revision, ProgressMonitor monitor);

		public abstract void Add (FilePath path, bool recurse, ProgressMonitor monitor);

		public abstract void Delete (FilePath path, bool force, ProgressMonitor monitor);

		public abstract void Ignore (FilePath[] paths);

		public abstract void Unignore (FilePath[] paths);

		public IEnumerable<DirectoryEntry> List (FilePath path, bool recurse)
		{
			return List (path, recurse, SvnRevision.Head);
		}

		public IEnumerable<DirectoryEntry> ListUrl (string url, bool recurse)
		{
			return ListUrl (url, recurse, SvnRevision.Head);
		}

		public abstract IEnumerable<DirectoryEntry> List (FilePath path, bool recurse, SvnRevision rev);

		public abstract IEnumerable<DirectoryEntry> ListUrl (string url, bool recurse, SvnRevision rev);

		public void Move (FilePath srcPath, FilePath destPath, bool force, ProgressMonitor monitor)
		{
			Move (srcPath, destPath, SvnRevision.Head, force, monitor);
		}

		public abstract void Move (FilePath srcPath, FilePath destPath, SvnRevision rev, bool force, ProgressMonitor monitor);

		public abstract void Lock (ProgressMonitor monitor, string comment, bool stealLock, params FilePath[] paths);

		public abstract void Unlock (ProgressMonitor monitor, bool breakLock, params FilePath[] paths);

		public string GetUnifiedDiff (FilePath path, bool recursive, bool remoteDiff)
		{
			return GetUnifiedDiff (path,
				remoteDiff ? SvnRevision.Head : SvnRevision.Base,
				path,
				SvnRevision.Working,
				recursive);
		}

		public abstract string GetUnifiedDiff (FilePath path1, SvnRevision revision1, FilePath path2, SvnRevision revision2, bool recursive);
		
		public abstract string GetVersion ();

		static protected bool SimpleAuthenticationPrompt (string realm, bool may_save, ref string user_name, out string password, out bool save)
		{
			return UserPasswordDialog.Show (true, realm, may_save, ref user_name, out password, out save);
		}

		static protected bool UserNameAuthenticationPrompt (string realm, bool may_save, ref string user_name, out bool save)
		{
			string password;
			return UserPasswordDialog.Show (false, realm, may_save, ref user_name, out password, out save);
		}

		static protected bool SslServerTrustAuthenticationPrompt (string realm, SslFailure failures, bool may_save, CertficateInfo certInfo, out SslFailure accepted_failures, out bool save)
		{
			return SslServerTrustDialog.Show (realm, failures, may_save, certInfo, out accepted_failures, out save);
		}

		static protected bool SslClientCertAuthenticationPrompt (string realm, bool may_save, out string cert_file, out bool save)
		{
			return ClientCertificateDialog.Show (realm, may_save, out cert_file, out save);
		}

		static protected bool SslClientCertPwAuthenticationPrompt (string realm, bool may_save, out string password, out bool save)
		{
			return ClientCertificatePasswordDialog.Show (realm, may_save, out password, out save);
		}

		static protected void WorkingCopyFormatPrompt (bool isOld, Action action)
		{
			WorkingCopyFormatDialog.Show (isOld, action);
		}
		
		/// <summary>
		/// Get annotations for a versioned file.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> annotation for each line in file.
		/// </returns>
		public virtual Annotation[] GetAnnotations (Repository repo, FilePath file, SvnRevision revStart, SvnRevision revEnd)
		{
			return new Annotation[0];
		}

		public abstract bool HasNeedLock (FilePath file);
	}
	

	public class SubversionException : VersionControlException
	{
		public int ErrorCode {
			get { return (int)Data ["ErrorCode"]; }
		}
		
		public SubversionException (string message, int errorCode) : base (message)
		{
			Data ["ErrorCode"] = errorCode;
		}

		public SubversionException (string message) : this (message, 0)
		{
		}
	}
}
