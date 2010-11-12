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

namespace MonoDevelop.VersionControl.Git
{
	public class GitRepository: UrlBasedRepository
	{
		FilePath path;
		
		public static event EventHandler BranchSelectionChanged;
		
		public GitRepository ()
		{
			Method = "git";
		}
		
		public GitRepository (FilePath path, string url)
		{
			this.path = path;
			Url = url;
		}
		
		public FilePath RootPath {
			get { return path; }
		}
		
		public override void CopyConfigurationFrom (Repository other)
		{
			GitRepository repo = (GitRepository) other;
			path = repo.path;
			Url = repo.Url;
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
			StringReader sr = RunCommand ("show \":" + localFile.ToRelative (path) + "\"", true);
			return sr.ReadToEnd ();
		}
		
		
		public override Revision[] GetHistory (FilePath localFile, Revision since)
		{
			List<Revision> revs = new List<Revision> ();
			StringReader sr = RunCommand ("log --name-status --date=iso " + ToCmdPath (localFile), true);
			string line;
			string rev;
			while ((rev = ReadWithPrefix (sr, "commit ")) != null) {
				string author = ReadWithPrefix  (sr, "Author: ");
				string dateStr = ReadWithPrefix (sr, "Date:   ");
				DateTime date;
				DateTime.TryParse (dateStr, out date);
				
				List<RevisionPath> paths = new List<RevisionPath> ();
				bool readingComment = true;
				StringBuilder message = new StringBuilder ();
				StringBuilder interline = new StringBuilder ();
				
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
				revs.Add (new GitRevision (this, rev, date, author, message.ToString ().Trim ('\n','\r'), paths.ToArray ()));
			}
			return revs.ToArray ();
		}
		
		bool IsEmptyLine (string txt)
		{
			return txt.Replace (" ","").Replace ("\t","").Length == 0;
		}
		
		string ReadWithPrefix (StringReader sr, string prefix)
		{
			do {
				string line = sr.ReadLine ();
				if (line == null)
					return null;
				if (line.StartsWith (prefix))
					return line.Substring (prefix.Length);
			} while (true);
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
			StringReader sr = RunCommand ("log -1 --format=format:%H", true);
			string strRev = sr.ReadLine ();
			sr.Close ();
			
			HashSet<FilePath> existingFiles = new HashSet<FilePath> ();
			if (fileName != null) {
				FilePath fp = localDirectory.Combine (fileName).CanonicalPath;
				if (File.Exists (fp))
					existingFiles.Add (fp);
			} else
				CollectFiles (existingFiles, localDirectory, recursive);
			
			GitRevision rev = new GitRevision (this, strRev);
			List<VersionInfo> versions = new List<VersionInfo> ();
			FilePath p = fileName != null ? localDirectory.Combine (fileName) : localDirectory;
			sr = RunCommand ("status -s " + ToCmdPath (p), true);
			string line;
			while ((line = sr.ReadLine ()) != null) {
				char staged = line[0];
				char nostaged = line[1];
				string file = line.Substring (3);
				FilePath srcFile = FilePath.Null;
				int i = file.IndexOf ("->");
				if (i != -1) {
					srcFile = path.Combine (file.Substring (0, i - 1));
					file = file.Substring (i + 3);
				}
				FilePath statFile = path.Combine (file);
				if (statFile.ParentDirectory != localDirectory && (!statFile.IsChildPathOf (localDirectory) || !recursive))
					continue;
				VersionStatus status;
				if (staged == 'M' || nostaged == 'M')
					status = VersionStatus.Versioned | VersionStatus.Modified;
				else if (staged == 'A')
					status = VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				else if (staged == 'D' || nostaged == 'D')
					status = VersionStatus.Versioned | VersionStatus.ScheduledDelete;
				else if (staged == 'U' || nostaged == 'U')
					status = VersionStatus.Versioned | VersionStatus.Conflicted;
				else if (staged == 'R') {
					// Renamed files are in reality files delete+added to a different location.
					existingFiles.Remove (srcFile.CanonicalPath);
					VersionInfo rvi = new VersionInfo (srcFile, "", false, VersionStatus.Versioned | VersionStatus.ScheduledDelete, rev, VersionStatus.Versioned, null);
					versions.Add (rvi);
					status = VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				}
				else
					status = VersionStatus.Unversioned;
				
				existingFiles.Remove (statFile.CanonicalPath);
				VersionInfo vi = new VersionInfo (statFile, "", false, status, rev, VersionStatus.Versioned, null);
				versions.Add (vi);
			}
			
			// Files for which git did not report an status are supposed to be tracked
			foreach (FilePath file in existingFiles) {
				VersionInfo vi = new VersionInfo (file, "", false, VersionStatus.Versioned, rev, VersionStatus.Versioned, null);
				versions.Add (vi);
			}
			
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
			string file = Path.GetTempFileName ();
			try {
				File.WriteAllText (file, changeSet.GlobalComment);
				string paths = ToCmdPathList (changeSet.Items.Select (it => it.LocalPath));
				RunCommand ("commit -F \"" + file + "\" " + paths, true, monitor);
			} finally {
				File.Delete (file);
			}
		}
		
		public void GetUserInfo (out string name, out string email)
		{
			name = ToList (RunCommand ("config --get user.name", false)).FirstOrDefault ();
			email = ToList (RunCommand ("config --get user.email", false)).FirstOrDefault ();
		}
		
		public void SetUserInfo (string name, string email)
		{
			RunCommand ("config user.name \"" + name + "\"", false);
			RunCommand ("config user.email \"" + email + "\"", false);
		}
		
		public override void Checkout (FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			RunCommand ("clone " + Url + " " + targetLocalPath, true, monitor);
		}
		
		
		public override void Revert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			RunCommand ("reset --" + ToCmdPathList (localPaths), false, monitor);
			
			// Reset again. If a file is in conflict, the first reset will only
			// reset the conflict state, but it won't unstage the file
			RunCommand ("reset --" + ToCmdPathList (localPaths), false, monitor);
			
			// The checkout command may fail if a file is not tracked anymore after
			// the reset, so the checkouts have to be run one by one.
			foreach (FilePath p in localPaths)
				RunCommand ("checkout -- " + ToCmdPath (p), false, monitor);
			
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
			RunCommand ("add " + ToCmdPathList (localPaths), true, monitor);
		}
		
		public override string GetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			StringReader sr = RunCommand ("show \"" + revision.ToString () + ":" + repositoryPath.ToRelative (path) + "\"", true);
			return sr.ReadToEnd ();
		}
		
		public override DiffInfo[] PathDiff (FilePath baseLocalPath, FilePath[] localPaths, bool remoteDiff)
		{
			if (localPaths != null) {
				return new DiffInfo [0];
			}
			else {
				StringReader sr = RunCommand ("diff " + ToCmdPath (baseLocalPath), true);
				return GetUnifiedDiffInfo (sr.ReadToEnd (), baseLocalPath, null);
			}
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
			RunCommand ("push " + remote + " HEAD:" + remoteBranch, true, monitor);
			monitor.ReportSuccess ("Repository successfully pushed");
		}
		
		public void CreateBranch (string name, string trackSource)
		{
			if (string.IsNullOrEmpty (trackSource))
				RunCommand ("branch " + name, true);
			else
				RunCommand ("branch --track " + name + " " + trackSource, true);
		}
		
		public void SetBranchTrackSource (string name, string trackSource)
		{
			if (string.IsNullOrEmpty (trackSource))
				RunCommand ("branch --no-track -f " + name, true);
			else
				RunCommand ("branch --track -f " + name + " " + trackSource, true);
		}
		
		public void RemoveBranch (string name)
		{
			RunCommand ("branch -d " + name, true);
		}
		
		public void RenameBranch (string name, string newName)
		{
			RunCommand ("branch -m " + name + " " + newName, true);
		}
		
		public IEnumerable<RemoteSource> GetRemotes ()
		{
			StringReader sr = RunCommand ("remote -v", true);
			string line;
			List<RemoteSource> sources = new List<RemoteSource> ();
			while ((line = sr.ReadLine ()) != null) {
				int i = line.IndexOf ('\t');
				if (i == -1)
					continue;
				string name = line.Substring (0, i);
				RemoteSource remote = sources.FirstOrDefault (r => r.Name == name);
				if (remote == null) {
					remote = new RemoteSource ();
					remote.Name = name;
					sources.Add (remote);
				}
				int j = line.IndexOf (" (fetch)");
				if (j != -1) {
					remote.FetchUrl = line.Substring (i, line.Length - i - 8).Trim ();
				} else {
					j = line.IndexOf (" (push)");
					remote.PushUrl = line.Substring (i, line.Length - i - 7).Trim ();
				}
			}
			return sources;
		}
		
		public void RenameRemote (string name, string newName)
		{
			RunCommand ("remote rename " + name + " " + newName, true);
		}
		
		public void AddRemote (RemoteSource remote, bool importTags)
		{
			RunCommand ("remote add " + (importTags ? "--tags " : "--no-tags ") + remote.Name + " " + remote.FetchUrl, true);
			if (!string.IsNullOrEmpty (remote.PushUrl) && remote.PushUrl != remote.FetchUrl)
				RunCommand ("remote set-url --push " + remote.Name + " " + remote.PushUrl, true);
		}
		
		public void UpdateRemote (RemoteSource remote)
		{
			if (string.IsNullOrEmpty (remote.FetchUrl))
				throw new InvalidOperationException ("Fetch url can't be empty");
			
			RunCommand ("remote set-url " + remote.Name + " " + remote.FetchUrl, true);
			
			if (!string.IsNullOrEmpty (remote.PushUrl))
				RunCommand ("remote set-url --push " + remote.Name + " " + remote.PushUrl, true);
			else
				RunCommand ("remote set-url --push --delete " + remote.Name + " " + remote.PushUrl, true);
		}
		
		public void RemoveRemote (string name)
		{
			RunCommand ("remote rm " + name, true);
		}
		
		public IEnumerable<Branch> GetBranches ()
		{
			StringReader sr = RunCommand ("branch -vv", true);
			List<Branch> list = new List<Branch> ();
			string line;
			while ((line = sr.ReadLine ()) != null) {
				if (line.Length < 2)
					continue;
				line = line.Substring (2);
				int i = line.IndexOf (' ');
				if (i == -1)
					continue;
				
				Branch b = new Branch ();
				b.Name = line.Substring (0, i);
				
				i = line.IndexOf ('[', i);
				if (i != -1) {
					int j = line.IndexOf (']', ++i);
					b.Tracking = line.Substring (i, j - i);
				}
				list.Add (b);
			}
			return list;
		}
		
		public IEnumerable<string> GetTags ()
		{
			StringReader sr = RunCommand ("tag", true);
			return ToList (sr);
		}
		
		public IEnumerable<string> GetRemoteBranches (string remoteName)
		{
			StringReader sr = RunCommand ("branch -r", true);
			string line;
			while ((line = sr.ReadLine ()) != null) {
				if (line.StartsWith ("  " + remoteName + "/") || line.StartsWith ("* " + remoteName + "/")) {
					int i = line.IndexOf ('/');
					int j = line.IndexOf (" -> ");
					if (j == -1) j = line.Length;
					i++;
					yield return line.Substring (i, j - i);
				}
			}
		}
		
		public string GetCurrentBranch ()
		{
			StringReader sr = RunCommand ("branch", true);
			string line;
			while ((line = sr.ReadLine ()) != null) {
				if (line.StartsWith ("* "))
					return line.Substring (2).Trim ();
			}
			return null;
		}
		
		public void SwitchToBranch (string branch)
		{
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
			StringReader sr = RunCommand ("diff --name-status " + remote + "/" + branch + " " + GetCurrentBranch (), true);
			foreach (string change in ToList (sr)) {
				FilePath file = path.Combine (change.Substring (2));
				VersionStatus status;
				switch (change [0]) {
				case 'A': status = VersionStatus.ScheduledAdd; break;
				case 'D': status = VersionStatus.ScheduledDelete; break;
				default: status = VersionStatus.Modified; break;
				}
				VersionInfo vi = new VersionInfo (file, "", false, status | VersionStatus.Versioned, null, VersionStatus.Versioned, null);
				cset.AddFile (vi);
			}
			return cset;
		}
		
		public DiffInfo[] GetPushDiff (string remote, string branch)
		{
			StringReader sr = RunCommand ("diff " + remote + "/" + branch + " " + GetCurrentBranch (), true);
			return GetUnifiedDiffInfo (sr.ReadToEnd (), path, null);
		}
		
		public override void MoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			if (!IsVersioned (localSrcPath)) {
				base.MoveFile (localSrcPath, localDestPath, force, monitor);
				return;
			}
			RunCommand ("mv " + ToCmdPath (localSrcPath) + " " + ToCmdPath (localDestPath), true, monitor);
		}
		
		public override void MoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
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
		public string Name { get; internal set; }
		public string FetchUrl { get; internal set; }
		public string PushUrl { get; internal set; }
	}
}

