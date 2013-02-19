using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using System.Runtime.InteropServices;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.Subversion.Gui;

namespace MonoDevelop.VersionControl.Subversion
{
	public abstract class SubversionVersionControl : VersionControlSystem
	{
		internal static string GetTextBase(string sourcefile)
		{
			return Path.Combine (Path.GetDirectoryName(sourcefile), ".svn", "text-base", Path.GetFileName(sourcefile) + ".svn-base");
		}
		
		internal static string GetDirectoryDotSvn (string sourcepath)
		{
			return Path.Combine(sourcepath, ".svn");
		}

		internal static bool IsVersioned (FilePath sourcefile)
		{
			return File.Exists (GetTextBase (sourcefile)) || Directory.Exists (GetDirectoryDotSvn (sourcefile));
		}

		public override string Name
		{
			get { return "Subversion"; }
		}

		public abstract SubversionBackend CreateBackend ();
		
		public abstract string GetPathUrl (FilePath path);

		public override Repository GetRepositoryReference (FilePath path, string id)
		{
			try {
				if (!IsVersioned (path))
					return null;
				string url = GetPathUrl (path);
				return new SubversionRepository (this, url, path);
			} catch (Exception ex) {
				// No SVN
				LoggingService.LogError (ex.ToString ());
				return null;
			}
		}

		protected override Repository OnCreateRepositoryInstance ()
		{
			return new SubversionRepository ();
		}
		
		public override IRepositoryEditor CreateRepositoryEditor (Repository repo)
		{
			return new UrlBasedRepositoryEditor ((SubversionRepository)repo);
		}
	}

	public abstract class SubversionBackend
	{
		string GetTextBase (string sourcefile)
		{
			return SubversionVersionControl.GetTextBase (sourcefile);
		}
		
		string GetDirectoryDotSvn (string sourcepath)
		{
			return SubversionVersionControl.GetDirectoryDotSvn (sourcepath);
		}

		public string GetPathToBaseText (FilePath sourcefile) {
			return GetTextBase (sourcefile);
		}

		public Revision[] GetHistory (Repository repo, FilePath sourcefile, Revision since)
		{
			List<Revision> revs = new List<Revision>();
			
			SvnRevision startrev = SvnRevision.Working;
			SvnRevision sincerev = SvnRevision.First;
			if (since != null)
				sincerev = (SvnRevision) since;
			
			foreach (SvnRevision rev in Log (repo, sourcefile, startrev, sincerev))
				revs.Add (rev);
			
			return revs.ToArray ();
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
		public abstract string GetTextAtRevision (string repositoryPath, Revision revision);
		
		internal protected virtual VersionControlOperation GetSupportedOperations (Repository repo, VersionInfo vinfo, VersionControlOperation defaultValue)
		{
			if (vinfo.IsVersioned && File.Exists (vinfo.LocalPath) && !Directory.Exists (vinfo.LocalPath) && vinfo.HasLocalChange (VersionStatus.ScheduledDelete))
				defaultValue |= VersionControlOperation.Add;
			return defaultValue;
		}

		public VersionInfo GetVersionInfo (Repository repo, FilePath localPath, bool getRemoteStatus)
		{
			// Check for directory before checking for file, since directory links may appear as files
			if (Directory.Exists (GetDirectoryDotSvn (localPath)) || Directory.Exists (localPath))
				return GetDirStatus (repo, localPath, getRemoteStatus);
			else if (File.Exists (GetTextBase(localPath)) || File.Exists (localPath))
				return GetFileStatus (repo, localPath, getRemoteStatus);
			else
				return VersionInfo.CreateUnversioned (localPath, false);
		}

		private VersionInfo GetFileStatus (Repository repo, FilePath sourcefile, bool getRemoteStatus)
		{
			SubversionRepository srepo = (SubversionRepository) repo;
			
			// If the directory is not versioned, there is no version info
			if (!Directory.Exists (GetDirectoryDotSvn (sourcefile.ParentDirectory)))
				return VersionInfo.CreateUnversioned (sourcefile, false);
			if (!sourcefile.IsChildPathOf (srepo.RootPath))
				return VersionInfo.CreateUnversioned (sourcefile, false);
			
			List<VersionInfo> statuses = new List<VersionInfo> ();
			statuses.AddRange (Status (repo, sourcefile, SvnRevision.Head, false, false, getRemoteStatus));

			if (statuses.Count == 0)
				return VersionInfo.CreateUnversioned (sourcefile, false);
			
			if (statuses.Count != 1)
				return VersionInfo.CreateUnversioned (sourcefile, false);
			
			VersionInfo ent = (VersionInfo) statuses[0];
			if (ent.IsDirectory)
				return VersionInfo.CreateUnversioned (sourcefile, false);
			
			return ent;
		}

		private VersionInfo GetDirStatus (Repository repo, FilePath localPath, bool getRemoteStatus)
		{
			// If the directory is not versioned, there is no version info
			if (!Directory.Exists (GetDirectoryDotSvn (localPath)))
				return VersionInfo.CreateUnversioned (localPath, true);
				
			foreach (VersionInfo ent in Status (repo, localPath, SvnRevision.Head, false, false, getRemoteStatus)) {
				if (ent.LocalPath.CanonicalPath == localPath.CanonicalPath)
					return ent;
			}
			return VersionInfo.CreateUnversioned (localPath, true);
		}

		public VersionInfo[] GetDirectoryVersionInfo (Repository repo, FilePath sourcepath, bool getRemoteStatus, bool recursive)
		{
			List<VersionInfo> list = new List<VersionInfo> ();
			list.AddRange (Status (repo, sourcepath, SvnRevision.Head, recursive, true, getRemoteStatus));
			return list.ToArray ();
		}

		public virtual IEnumerable<VersionInfo> Status (Repository repo, FilePath path, SvnRevision revision)
		{
			return Status (repo, path, revision, false, false, false);
		}

		public abstract IEnumerable<VersionInfo> Status (Repository repo, FilePath path, SvnRevision revision, bool descendDirs, bool changedItemsOnly, bool remoteStatus);

		public abstract void Update (FilePath path, bool recurse, IProgressMonitor monitor);

		public abstract void Commit (FilePath[] paths, string message, IProgressMonitor monitor);

		public abstract void Mkdir (string[] paths, string message, IProgressMonitor monitor);

		public abstract void Checkout (string url, FilePath path, Revision rev, bool recurse, IProgressMonitor monitor);

		public abstract void Revert (FilePath[] paths, bool recurse, IProgressMonitor monitor);

		public virtual void Resolve (FilePath[] paths, bool recurse, IProgressMonitor monitor)
		{
			foreach (string path in paths)
				Resolve (path, recurse, monitor);
		}

		public abstract void Resolve (FilePath path, bool recurse, IProgressMonitor monitor);

		public abstract void RevertRevision (FilePath path, Revision revision, IProgressMonitor monitor);

		public abstract void RevertToRevision (FilePath path, Revision revision, IProgressMonitor monitor);

		public abstract void Add (FilePath path, bool recurse, IProgressMonitor monitor);

		public abstract void Delete (FilePath path, bool force, IProgressMonitor monitor);

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

		public void Move (FilePath srcPath, FilePath destPath, bool force, IProgressMonitor monitor)
		{
			Move (srcPath, destPath, SvnRevision.Head, force, monitor);
		}

		public abstract void Move (FilePath srcPath, FilePath destPath, SvnRevision rev, bool force, IProgressMonitor monitor);

		public abstract void Lock (IProgressMonitor monitor, string comment, bool stealLock, params FilePath[] paths);

		public abstract void Unlock (IProgressMonitor monitor, bool breakLock, params FilePath[] paths);

		public string GetUnifiedDiff (FilePath path, bool recursive, bool remoteDiff)
		{
			if (remoteDiff)
				return GetUnifiedDiff (path, SvnRevision.Head, path, SvnRevision.Working, recursive);
			else
				return GetUnifiedDiff (path, SvnRevision.Base, path, SvnRevision.Working, recursive);
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
	}
	

	public class SubversionException : ApplicationException {
		public SubversionException(string message) : base(message) { }
	}
}
