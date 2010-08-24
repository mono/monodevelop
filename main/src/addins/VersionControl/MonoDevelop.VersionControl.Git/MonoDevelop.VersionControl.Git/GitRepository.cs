// 
// GitRepository.cs
//  
// Author:
//       Dale Ragan <dale.ragan@sinesignal.com>
// 
// Copyright (c) 2010 SineSignal, LLC
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

//#define DEBUG_GIT

using System;
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using MonoDevelop.Ide;
using GitSharp;
using GitSharp.Core.Transport;
using GitCore = GitSharp.Core;

namespace MonoDevelop.VersionControl.Git
{
	public class GitRepository: UrlBasedRepository
	{
		FilePath path;
		GitSharp.Repository repo;
		GitCore.Repository crepo;
		
		public static event EventHandler BranchSelectionChanged;
		
		public GitRepository ()
		{
			Method = "git";
		}
		
		public GitRepository (FilePath path, string url)
		{
			this.path = path;
			Url = url;
			repo = new GitSharp.Repository (path);
			crepo = GitCore.Repository.Open (path);
		}
		
		public FilePath RootPath {
			get { return path; }
		}
		
		public override void CopyConfigurationFrom (Repository other)
		{
			GitRepository r = (GitRepository) other;
			path = r.path;
			Url = r.Url;
			if (r.repo != null)
				repo = new GitSharp.Repository (path);
			if (r.crepo != null)
				crepo = GitCore.Repository.Open (path);
		}
		
		public override string LocationDescription {
			get {
				return Url ?? path;
			}
		}
		
		public override bool AllowLocking {
			get {
				return false;
			}
		}
		
		public override string GetBaseText (FilePath localFile)
		{
			// TODO verify it works
			return GetCommitContent (repo.CurrentBranch.CurrentCommit, localFile);
		}
		
		public override Revision[] GetHistory (FilePath localFile, Revision since)
		{
			List<Revision> revs = new List<Revision> ();
            foreach (var commit in repo.Get<AbstractTreeNode>(localFile.ToRelative (path)).GetHistory())
            {
				List<RevisionPath> paths = new List<RevisionPath> ();
				foreach (Change change in commit.Changes) {
					RevisionAction ra;
					switch (change.ChangeType) {
					case ChangeType.Added: ra = RevisionAction.Add; break;
					case ChangeType.Deleted: ra = RevisionAction.Delete; break;
					default: ra = RevisionAction.Modify; break;
					}
					RevisionPath p = new RevisionPath (path.Combine (change.Path), ra, null);
					paths.Add (p);
				}
				revs.Add (new GitRevision (this, commit.Hash, commit.CommitDate.LocalDateTime, commit.Author.Name, commit.Message, paths.ToArray ()));
            }
			return revs.ToArray ();
		}
		
		public override VersionInfo GetVersionInfo (FilePath localPath, bool getRemoteStatus)
		{
			if (Directory.Exists (localPath)) {
				GitRevision rev = new GitRevision (this, "");
				return new VersionInfo (localPath, "", true, VersionStatus.Versioned, rev, VersionStatus.Versioned, null);
			}
			else {
				VersionInfo[] infos = GetDirectoryVersionInfo (localPath.ParentDirectory, localPath.FileName, getRemoteStatus, false);
				if (infos.Length == 1)
					return infos[0];
				else
					return null;
			}
		}
		
		StringReader RunCommand (string cmd, bool checkExitCode)
		{
			return RunCommand (cmd, checkExitCode, null);
		}
		
		StringReader RunCommand (string cmd, bool checkExitCode, IProgressMonitor monitor)
		{
#if DEBUG_GIT
			Console.WriteLine ("> git " + cmd);
#endif
			var psi = new System.Diagnostics.ProcessStartInfo (GitVersionControl.GitExe, "--no-pager " + cmd) {
				WorkingDirectory = path,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = false,
				UseShellExecute = false,
			};
			if (PropertyService.IsWindows) {
				//msysgit needs this to read global config
				psi.EnvironmentVariables ["HOME"] = Environment.GetEnvironmentVariable("USERPROFILE");
			}

			StringWriter outw = new StringWriter ();
			ProcessWrapper proc = Runtime.ProcessService.StartProcess (psi, outw, outw, null);
			proc.WaitForOutput ();
			if (monitor != null)
				monitor.Log.Write (outw.ToString ());
#if DEBUG_GIT
			Console.WriteLine (outw.ToString ());
#endif
			if (checkExitCode && proc.ExitCode != 0)
				throw new InvalidOperationException ("Git operation failed");
			return new StringReader (outw.ToString ());
		}
		
		List<string> ToList (StringReader sr)
		{
			List<string> list = new List<string> ();
			string line;
			while ((line = sr.ReadLine ()) != null)
				list.Add (line);
			return list;
		}
		
		public override VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			return GetDirectoryVersionInfo (localDirectory, null, getRemoteStatus, recursive);
		}
		
		string ToGitPath (FilePath filePath)
		{
			return filePath.ToRelative (path).ToString ().Replace ('\\','/');
		}
		
		string ToCmdPath (FilePath filePath)
		{
			return "\"" + filePath.ToRelative (path) + "\"";
		}
		
		string ToCmdPathList (IEnumerable<FilePath> paths)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (FilePath it in paths)
				sb.Append (' ').Append (ToCmdPath (it));
			return sb.ToString ();
		}
		
		VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, string fileName, bool getRemoteStatus, bool recursive)
		{
			Console.WriteLine ("pp GetDirectoryVersionInfo:");
			DateTime t = DateTime.Now;
			
			HashSet<FilePath> existingFiles = new HashSet<FilePath> ();
			if (fileName != null) {
				FilePath fp = localDirectory.Combine (fileName).CanonicalPath;
				if (File.Exists (fp))
					existingFiles.Add (fp);
			} else
				CollectFiles (existingFiles, localDirectory, recursive);
			
			GitRevision rev = new GitRevision (this, repo.CurrentBranch.CurrentCommit.Hash);
			List<VersionInfo> versions = new List<VersionInfo> ();
			FilePath p = fileName != null ? localDirectory.Combine (fileName) : localDirectory;
			p = p.CanonicalPath;
			
			RepositoryStatus status = repo.Status;
	
			Action<IEnumerable<string>,VersionStatus> AddFiles = delegate (IEnumerable<string> files, VersionStatus fstatus) {
				foreach (string file in files) {
					FilePath statFile = path.Combine (file);
					if (statFile.ParentDirectory != localDirectory && (!statFile.IsChildPathOf (localDirectory) || !recursive))
						continue;
					existingFiles.Remove (statFile.CanonicalPath);
					VersionInfo vi = new VersionInfo (statFile, "", false, fstatus, rev, VersionStatus.Versioned, null);
					versions.Add (vi);
				}
			};
			
			AddFiles (status.Added, VersionStatus.Versioned | VersionStatus.ScheduledAdd);
			AddFiles (status.Modified, VersionStatus.Versioned | VersionStatus.Modified);
			AddFiles (status.Removed, VersionStatus.Versioned | VersionStatus.ScheduledDelete);
			AddFiles (status.MergeConflict, VersionStatus.Versioned | VersionStatus.Conflicted);
			AddFiles (status.Untracked, VersionStatus.Unversioned);
				
/*				else if (staged == 'R') {
					// Renamed files are in reality files delete+added to a different location.
					existingFiles.Remove (srcFile.CanonicalPath);
					VersionInfo rvi = new VersionInfo (srcFile, "", false, VersionStatus.Versioned | VersionStatus.ScheduledDelete, rev, VersionStatus.Versioned, null);
					versions.Add (rvi);
					status = VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				}
				else*/
				
			// Files for which git did not report an status are supposed to be tracked
			foreach (FilePath file in existingFiles) {
				VersionInfo vi = new VersionInfo (file, "", false, VersionStatus.Versioned, rev, VersionStatus.Versioned, null);
				versions.Add (vi);
			}
			
			Console.WriteLine ("pp GetDirectoryVersionInfo -- : " + (DateTime.Now - t).TotalMilliseconds);
			return versions.ToArray ();
		}
		
		void CollectFiles (HashSet<FilePath> files, FilePath dir, bool recursive)
		{
			foreach (string file in Directory.GetFiles (dir))
				files.Add (new FilePath (file).CanonicalPath);
			if (recursive) {
				foreach (string sub in Directory.GetDirectories (dir))
					CollectFiles (files, sub, true);
			}
		}
		
		
		public override Repository Publish (string serverPath, FilePath localPath, FilePath[] files, string message, IProgressMonitor monitor)
		{
			throw new System.NotImplementedException();
		}
		
		public override void Update (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			// TODO needs ssh
			
			List<string> statusList = null;
			
			try {
				// Fetch remote commits
				RunCommand ("fetch " + GetCurrentRemote (), true, monitor);
				
				// Get a list of files that are different in the target branch
				statusList = ToList (RunCommand ("diff " + GetCurrentRemote () + "/" + GetCurrentBranch () + " --name-status", true));
				
				// Save local changes
				RunCommand ("stash save " + GetStashName ("_tmp_"), true);
				
				// Apply changes
				StringReader sr = RunCommand ("rebase " + GetCurrentRemote () + " " + GetCurrentBranch (), false, monitor);
				string conflictFile = null;
				do {
					conflictFile = GetConflictFiles (sr).FirstOrDefault ();
					if (conflictFile != null) {
						ConflictResult res = ResolveConflict (conflictFile);
						if (res == ConflictResult.Abort) {
							sr = RunCommand ("rebase --abort", false, monitor);
							break;
						}
						else if (res == ConflictResult.Skip)
							sr = RunCommand ("rebase --skip", false, monitor);
						else {
							RunCommand ("add " + ToCmdPath (conflictFile), false, monitor);
							sr = RunCommand ("rebase --continue", false, monitor);
						}
					}
				} while (conflictFile != null);
				
			} finally {
				// Restore local changes
				string sid = GetStashId ("_tmp_");
				if (sid != null)
					RunCommand ("stash pop " + sid, false);
			}
			
			// Notify changes
			if (statusList != null)
				NotifyFileChanges (statusList);
		}
		
		public void Merge (string branch, IProgressMonitor monitor)
		{
			List<string> statusList = null;
			
			try {
				// Get a list of files that are different in the target branch
				statusList = ToList (RunCommand ("diff " + branch + " --name-status", true));
				
				// Save local changes
				RunCommand ("stash save " + GetStashName ("_tmp_"), true);
				
				// Apply changes
				StringReader sr = RunCommand ("merge " + branch, false, monitor);
				
				var conflicts = GetConflictFiles (sr);
				if (conflicts.Count != 0) {
					foreach (string conflictFile in conflicts) {
						ConflictResult res = ResolveConflict (conflictFile);
						if (res == ConflictResult.Abort) {
							sr = RunCommand ("reset --merge", false, monitor);
							return;
						}
						else if (res == ConflictResult.Skip) {
							RunCommand ("reset " + ToCmdPath (conflictFile), false, monitor);
							RunCommand ("reset " + ToCmdPath (conflictFile), false, monitor);
							RunCommand ("checkout " + ToCmdPath (conflictFile), false, monitor);
						} else {
							// Do nothing. The change will be committed with the -a option
						}
					}
					string msg = "Merge branch '" + branch + "'";
					RunCommand ("commit -a -m \"" + msg + "\"", true, monitor);
				}
				
			} finally {
				// Restore local changes
				string sid = GetStashId ("_tmp_");
				if (sid != null)
					RunCommand ("stash pop " + sid, false);
			}
			
			// Notify changes
			if (statusList != null)
				NotifyFileChanges (statusList);
		}
		
		List<string> GetConflictFiles (StringReader reader)
		{
			List<string> list = new List<string> ();
			foreach (string line in ToList (reader)) {
				if (line.StartsWith ("CONFLICT ")) {
					string s = "Merge conflict in ";
					int i = line.IndexOf (s) + s.Length;
					list.Add (path.Combine (line.Substring (i)));
				}
			}
			return list;
		}
		
		ConflictResult ResolveConflict (string file)
		{
			ConflictResult res = ConflictResult.Abort;
			DispatchService.GuiSyncDispatch (delegate {
				var dlg = new ConflictResolutionDialog ();
				try {
					dlg.Load (file);
					var dres = (Gtk.ResponseType) MessageService.RunCustomDialog (dlg);
					dlg.Hide ();
					switch (dres) {
					case Gtk.ResponseType.Cancel: res = ConflictResult.Abort; break;
					case Gtk.ResponseType.Close: res = ConflictResult.Skip; break;
					case Gtk.ResponseType.Ok: res = ConflictResult.Continue; dlg.Save (file); break;
					}
				} finally {
					dlg.Destroy ();
				}
			});
			return res;
		}
		
		
		public override void Commit (ChangeSet changeSet, IProgressMonitor monitor)
		{
			repo.Commit (changeSet.GlobalComment, changeSet.Items.Select (it => it.LocalPath.ToString ()).ToArray ());
		}
		
		public void GetUserInfo (out string name, out string email)
		{
			GitSharp.Config config = repo.Config;
			name = config ["user.name"];
			email = config ["user.email"];
		}
		
		public void SetUserInfo (string name, string email)
		{
			GitSharp.Config config = repo.Config;
			repo.Config ["user.name"] = name;
			repo.Config ["user.email"] = email;
			config.Persist ();
		}
		
		public override void Checkout (FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			// TODO verify it works
			GitSharp.Git.Clone (Url, targetLocalPath);
		}
		
		public override void Revert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			// TODO verify it works
			repo.Index.Unstage (localPaths.ToStringArray ());
			
			// The checkout command may fail if a file is not tracked anymore after
			// the reset, so the checkouts have to be run one by one.
			foreach (FilePath p in localPaths)
				repo.Index.Checkout (p);
			
			foreach (FilePath p in localPaths)
				FileService.NotifyFileChanged (p);
		}
		
		public override void RevertRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			throw new System.NotImplementedException();
		}
		
		
		public override void RevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			throw new System.NotImplementedException();
		}
		
		
		public override void Add (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			repo.Index.Add (localPaths.ToStringArray ());
		}
		
		public override string GetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			// TODO verify it works
			Commit c = repo.Get<Commit> (revision.ToString ());
			if (c != null)
				return GetCommitContent (c, repositoryPath);
			else
				return string.Empty;
		}
		
		public override DiffInfo[] PathDiff (FilePath baseLocalPath, FilePath[] localPaths, bool remoteDiff)
		{
			if (localPaths != null) {
				return new DiffInfo [0];
			}
			else {
				List<DiffInfo> diffs = new List<DiffInfo> ();
				VersionInfo[] vinfos = GetDirectoryVersionInfo (baseLocalPath, null, false, true);
				foreach (VersionInfo vi in vinfos) {
					if ((vi.Status & VersionStatus.ScheduledAdd) != 0) {
						string ctxt = File.ReadAllText (vi.LocalPath);
						diffs.Add (new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff ("", ctxt)));
					}
					else if ((vi.Status & VersionStatus.ScheduledDelete) != 0) {
						string ctxt = GetCommitContent (repo.CurrentBranch.CurrentCommit, vi.LocalPath);
						diffs.Add (new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff (ctxt, "")));
					}
					else if ((vi.Status & VersionStatus.Modified) != 0) {
						string ctxt1 = GetCommitContent (repo.CurrentBranch.CurrentCommit, vi.LocalPath);
						string ctxt2 = File.ReadAllText (vi.LocalPath);
						diffs.Add (new DiffInfo (baseLocalPath, vi.LocalPath, GenerateDiff (ctxt1, ctxt2)));
					}
				}
				return diffs.ToArray ();
			}
		}
		
		string GetCommitContent (GitSharp.Commit c, FilePath file)
		{
			GitSharp.Leaf leaf = c.Tree [ToGitPath (file)] as GitSharp.Leaf;
			if (leaf != null)
				return leaf.Data;
			else
				return string.Empty;
		}
		
		string GenerateDiff (string s1, string s2)
		{
			var text1 = new GitSharp.Core.Diff.RawText (Encoding.UTF8.GetBytes (s1));
			var text2 = new GitSharp.Core.Diff.RawText (Encoding.UTF8.GetBytes (s2));
            var edits = new GitSharp.Core.Diff.MyersDiff(text1, text2).getEdits();
			var formatter = new GitSharp.Core.Diff.DiffFormatter ();
			MemoryStream s = new MemoryStream ();
			formatter.FormatEdits (s, text1, text2, edits);
			return Encoding.UTF8.GetString (s.ToArray ());
		}
		
		DiffInfo[] GetUnifiedDiffInfo (string diffContent, FilePath basePath, FilePath[] localPaths)
		{
			basePath = basePath.FullPath;
			List<DiffInfo> list = new List<DiffInfo> ();
			using (StringReader sr = new StringReader (diffContent)) {
				string line;
				StringBuilder content = new StringBuilder ();
				string fileName = null;
				
				while ((line = sr.ReadLine ()) != null) {
					if (line.StartsWith ("+++ ") || line.StartsWith ("--- ")) {
						string newFile = path.Combine (line.Substring (6));
						if (fileName != null && fileName != newFile) {
							list.Add (new DiffInfo (basePath, fileName, content.ToString ().Trim ('\n')));
							content = new StringBuilder ();
						}
						fileName = newFile;
					}
					else if (!line.StartsWith ("diff") && !line.StartsWith ("index")) {
						content.Append (line).Append ('\n');
					}
				}
				if (fileName != null) {
					list.Add (new DiffInfo (basePath, fileName, content.ToString ().Trim ('\n')));
				}
			}
			return list.ToArray ();
		}
		
		public string GetCurrentRemote ()
		{
			List<string> remotes = new List<string> (GetRemotes ().Select (r => r.Name));
			if (remotes.Count == 0)
				throw new InvalidOperationException ("There are no remote repositories defined");
			
			if (remotes.Contains ("origin"))
				return "origin";
			else
				return remotes [0];
		}
		
		public void Push (IProgressMonitor monitor, string remote, string remoteBranch)
		{
			// TODO needs ssh
			RunCommand ("push " + remote + " HEAD:" + remoteBranch, true, monitor);
			monitor.ReportSuccess ("Repository successfully pushed");
		}
		
		public void CreateBranch (string name, string trackSource)
		{
			GitSharp.Branch b = GitSharp.Branch.Create (repo, name);
			b.UpstreamSource = trackSource;
			repo.Config.Persist ();
		}
		
		public void SetBranchTrackSource (string name, string trackSource)
		{
			GitSharp.Branch b;
			if (repo.Branches.TryGetValue (name, out b)) {
				b.UpstreamSource = trackSource;
				repo.Config.Persist ();
			}
		}
		
		public void RemoveBranch (string name)
		{
			GitSharp.Branch b = repo.Get<GitSharp.Branch> (name);
			if (b != null)
				b.Delete ();
		}
		
		public void RenameBranch (string name, string newName)
		{
			GitSharp.Branch b = repo.Get<GitSharp.Branch> (name);
			if (b != null)
				b.Rename (newName);
		}

		public IEnumerable<RemoteSource> GetRemotes ()
		{
			foreach (Remote rem in repo.Remotes)
				yield return new RemoteSource (rem);
		}
		
		public void RenameRemote (string name, string newName)
		{
			Remote rem = repo.Remotes.FirstOrDefault (r => r.Name == name);
			if (rem != null) {
				rem.Name = newName;
				rem.Update ();
				repo.Config.Persist ();
			}
		}
		
		public void AddRemote (RemoteSource remote, bool importTags)
		{
<<<<<<< HEAD
			RunCommand ("remote add " + (importTags ? "--tags " : "--no-tags ") + remote.Name + " " + remote.FetchUrl, true);
			if (!string.IsNullOrEmpty (remote.PushUrl) && remote.PushUrl != remote.FetchUrl)
				RunCommand ("remote set-url --push " + remote.Name + " " + remote.PushUrl, true);
=======
			if (string.IsNullOrEmpty (remote.Name))
				throw new InvalidOperationException ("Name not set");
			Remote rem = repo.Remotes.CreateRemote (remote.Name);
			remote.RepoRemote = rem;
			UpdateRemote (remote);
>>>>>>> Initial implementation of git support using GitSharp.
		}
		
		public void UpdateRemote (RemoteSource remote)
		{
			if (string.IsNullOrEmpty (remote.FetchUrl))
				throw new InvalidOperationException ("Fetch url can't be empty");

			if (remote.RepoRemote == null)
				throw new InvalidOperationException ("Remote not created");
			
			remote.Update ();
			repo.Config.Persist ();
		}
		
		public void RemoveRemote (string name)
		{
			Remote rem = repo.Remotes.FirstOrDefault (r => r.Name == name);
			if (rem != null) {
				repo.Remotes.Remove (rem);
				repo.Config.Persist ();
			}
		}
		
		public IEnumerable<Branch> GetBranches ()
		{
			foreach (var b in repo.Branches) {
				Branch br = new Branch ();
				Console.WriteLine ("pp: {0} - {1} - {2}", b.Key, b.Value.Name, b.Value.Fullname);
				br.Name = b.Value.Name;
				br.Tracking = b.Value.UpstreamSource;
				yield return br;
			}
		}
		
		public IEnumerable<string> GetTags ()
		{
			return repo.Tags.Keys;
		}
		
		public IEnumerable<string> GetRemoteBranches (string remoteName)
		{
			foreach (var b in repo.RemoteBranches) {
				if (b.Key.StartsWith (remoteName + "/"))
					yield return b.Key.Substring (remoteName.Length + 1);
			}
		}
		
		public string GetCurrentBranch ()
		{
			return repo.CurrentBranch.Name;
		}
		
		public void SwitchToBranch (string branch)
		{
			// TODO Stash!
			
			// Remove the stash for this branch, if exists
			string currentBranch = GetCurrentBranch ();
			string sid = GetStashId (currentBranch);
			if (sid != null)
				RunCommand ("stash drop " + sid, true);
			
			// Get a list of files that are different in the target branch
			var statusList = ToList (RunCommand ("diff " + branch + " --name-status", true));
			
			// Create a new stash for the branch. This allows switching branches
			// without losing local changes
			RunCommand ("stash save " + GetStashName (currentBranch), true);
			
			// Switch to the target branch
			try {
				RunCommand ("checkout " + branch, true);
			} catch {
				// If something goes wrong, restore the work tree status
				RunCommand ("stash pop", true);
				throw;
			}

			// Restore the branch stash
			
			sid = GetStashId (branch);
			if (sid != null)
				RunCommand ("stash pop " + sid, true);
			
			// Notify file changes
			
			NotifyFileChanges (statusList);
			
			if (BranchSelectionChanged != null)
				BranchSelectionChanged (this, EventArgs.Empty);
		}
		
		void NotifyFileChanges (List<string> statusList)
		{
			foreach (string line in statusList) {
				char s = line [0];
				FilePath file = path.Combine (line.Substring (2));
				if (s == 'A')
					// File added to source branch not present to target branch.
					FileService.NotifyFileRemoved (file);
				else
					FileService.NotifyFileChanged (file);
			}
		}
		
		string GetStashName (string branchName)
		{
			return "__MD_" + branchName;
		}
		
		string GetStashId (string branchName)
		{
			string sn = GetStashName (branchName);
			foreach (string ss in ToList (RunCommand ("stash list", true))) {
				if (ss.IndexOf (sn) != -1) {
					int i = ss.IndexOf (':');
					return ss.Substring (0, i);
				}
			}
			return null;
		}
		
		public ChangeSet GetPushChangeSet (string remote, string branch)
		{
			ChangeSet cset = CreateChangeSet (path);
			GitSharp.Commit c1 = repo.Get<Commit> (remote + "/" + branch);
			GitSharp.Commit c2 = repo.Get<Commit> (GetCurrentBranch ());
			
			foreach (GitSharp.Change change in GitSharp.Commit.CompareCommits (c1, c2)) {
				VersionStatus status;
				switch (change.ChangeType) {
				case GitSharp.ChangeType.Added: status = VersionStatus.ScheduledAdd; break;
				case GitSharp.ChangeType.Deleted: status = VersionStatus.ScheduledDelete; break;
				default: status = VersionStatus.Modified; break;
				}
				VersionInfo vi = new VersionInfo (FromGitPath (change.Path), "", false, status | VersionStatus.Versioned, null, VersionStatus.Versioned, null);
				cset.AddFile (vi);
			}
			return cset;
		}
		
		FilePath FromGitPath (string filePath)
		{
			filePath = filePath.Replace ('/', Path.DirectorySeparatorChar);
			return path.Combine (filePath);
		}
		
		public DiffInfo[] GetPushDiff (string remote, string branch)
		{
			GitSharp.Commit c1 = repo.Get<Commit> (remote + "/" + branch);
			GitSharp.Commit c2 = repo.Get<Commit> (GetCurrentBranch ());
			
			List<DiffInfo> diffs = new List<DiffInfo> ();
			foreach (GitSharp.Change change in GitSharp.Commit.CompareCommits (c1, c2)) {
				string diff;
				switch (change.ChangeType) {
				case GitSharp.ChangeType.Added:
					diff = GenerateDiff ("", GetCommitContent (c2, change.Path));
					break;
				case GitSharp.ChangeType.Deleted:
					diff = GenerateDiff (GetCommitContent (c1, change.Path), "");
					break;
				default:
					diff = GenerateDiff (GetCommitContent (c1, change.Path), GetCommitContent (c2, change.Path));
					break;
				}
				DiffInfo di = new DiffInfo (path, FromGitPath (change.Path), diff);
				diffs.Add (di);
			}
			return diffs.ToArray ();
		}
		
		public override void MoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			// TODO move command
			if (!IsVersioned (localSrcPath)) {
				base.MoveFile (localSrcPath, localDestPath, force, monitor);
				return;
			}
			RunCommand ("mv " + ToCmdPath (localSrcPath) + " " + ToCmdPath (localDestPath), true, monitor);
		}
		
		public override void MoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			// TODO move command
			try {
				RunCommand ("mv " + ToCmdPath (localSrcPath) + " " + ToCmdPath (localDestPath), true, monitor);
			} catch {
				// If the move can't be done using git, do a regular move
				base.MoveDirectory (localSrcPath, localDestPath, force, monitor);
			}
		}
		
		public override bool CanGetAnnotations (FilePath localPath)
		{
			return true;
		}
		
		public override Annotation[] GetAnnotations (FilePath repositoryPath)
		{
			Commit[] lineCommits = repo.CurrentBranch.CurrentCommit.Blame (ToGitPath (repositoryPath));
			Annotation[] lines = new Annotation [lineCommits.Length];
			for (int n=0; n<lines.Length; n++) {
				Commit c = lineCommits [n];
				lines[n] = new Annotation (c.Hash, c.Author.Name + "<" + c.Author.EmailAddress + ">", c.CommitDate.DateTime);
			}
			return lines;
			
			/*
			List<Annotation> alist = new List<Annotation> ();
			StringReader sr = RunCommand ("blame -p " + ToCmdPath (repositoryPath), true);
			
			string author = null;
			string mail = null;
			string date = null;
			string tz = null;
				
			
			string line;
			while ((line = sr.ReadLine ()) != null) {
				string[] header = line.Split (' ');
				string rev = header[0];
				int lcount = int.Parse (header[3]);
				
				line = sr.ReadLine ();
				while (line != null && line.Length > 0 && line[0] != '\t') {
					int i = line.IndexOf (' ');
					string val;
					string field;
					if (i != -1) {
						val = line.Substring (i + 1);
						field = line.Substring (0, i);
					} else {
						val = null;
						field = line;
					}
					switch (field) {
					case "author": author = val; break;
					case "author-mail": mail = val; break;
					case "author-time": date = val; break;
					case "author-tz": tz = val; break;
					}
					line = sr.ReadLine ();
				}
				
				// Convert from git date format
				double secs = double.Parse (date);
				DateTime t = new DateTime (1970, 1, 1) + TimeSpan.FromSeconds (secs);
				string st = t.ToString ("yyyy-MM-ddTHH:mm:ss") + tz.Substring (0, 3) + ":" + tz.Substring (3);
				DateTime sdate = DateTime.Parse (st);
				
				string sauthor = author;
				if (!string.IsNullOrEmpty (mail))
					sauthor += " " + mail;
				Annotation a = new Annotation (rev, sauthor, sdate);
				while (lcount-- > 0) {
					alist.Add (a);
					if (lcount > 0) {
						sr.ReadLine (); // Next header
						sr.ReadLine (); // Next line content
					}
				}
			}
			return alist.ToArray ();
			*/
		}
		
		internal GitRevision GetPreviousRevisionFor (GitRevision revision)
		{
			StringReader sr = RunCommand ("log -1 --name-status --date=iso " + revision + "^", true);
			string rev = ReadWithPrefix (sr, "commit ");
			string author = ReadWithPrefix  (sr, "Author: ");
			string dateStr = ReadWithPrefix (sr, "Date:   ");
			DateTime date;
			DateTime.TryParse (dateStr, out date);
			
			List<RevisionPath> paths = new List<RevisionPath> ();
			bool readingComment = true;
			StringBuilder message = new StringBuilder ();
			StringBuilder interline = new StringBuilder ();
			
			string line;
			while ((line = sr.ReadLine ()) != null) {
				if (line.Length > 2 && ("ADM".IndexOf (line[0]) != -1) && line [1] == '\t') {
					readingComment = false;
					string file = line.Substring (2);
					RevisionAction ra;
					switch (line[0]) {
					case 'A': ra = RevisionAction.Add; break;
					case 'D': ra = RevisionAction.Delete; break;
					default: ra = RevisionAction.Modify; break;
					}
					RevisionPath p = new RevisionPath (path.Combine (file), ra, null);
					paths.Add (p);
				}
				else if (readingComment) {
					if (IsEmptyLine (line))
						interline.AppendLine (line);
					else {
						message.Append (interline);
						message.AppendLine (line);
						interline = new StringBuilder ();
					}
				}
				else
					break;
			}
			
			return new GitRevision (this, rev, date, author, message.ToString ().Trim ('\n','\r'), paths.ToArray ());
		}
	}
	
	public class GitRevision: Revision
	{
		string rev;
		
		public GitRevision (Repository repo, string rev)
			: base (repo)
		{
			this.rev = rev;
		}
					
		public GitRevision (Repository repo, string rev, DateTime time, string author, string message, RevisionPath[] changedFiles)
			: base (repo, time, author, message, changedFiles)
		{
			this.rev = rev;
		}
		
		public override string ToString ()
		{
			return rev;
		}
		
		public override Revision GetPrevious ()
		{
			return ((GitRepository) this.Repository).GetPreviousRevisionFor (this);
		}
	}
	
	public class Branch
	{
		public string Name { get; internal set; }
		public string Tracking { get; internal set; }
	}
	
	public class RemoteSource
	{
		internal Remote RepoRemote;
		
		public RemoteSource ()
		{
		}
		
		internal RemoteSource (Remote rem)
		{
			RepoRemote = rem;
			Name = rem.Name;
			FetchUrl = rem.URIs.Select (u => u.ToString ()).FirstOrDefault ();
			PushUrl = rem.PushURIs.Select (u => u.ToString ()).FirstOrDefault ();
			if (string.IsNullOrEmpty (PushUrl))
				PushUrl = FetchUrl;
		}
		
		internal void Update ()
		{
			RepoRemote.Name = Name;
			
			var list = new List<URIish> (RepoRemote.URIs);
			list.ForEach (u => RepoRemote.RemoveURI (u));
			
			list = new List<URIish> (RepoRemote.PushURIs);
			list.ForEach (u => RepoRemote.RemovePushURI (u));
			
			RepoRemote.AddURI (new URIish (FetchUrl));
			if (!string.IsNullOrEmpty (PushUrl) && PushUrl != FetchUrl)
				RepoRemote.AddPushURI (new URIish (PushUrl));
			RepoRemote.Update ();
		}
		
		public string Name { get; internal set; }
		public string FetchUrl { get; internal set; }
		public string PushUrl { get; internal set; }
	}
}

