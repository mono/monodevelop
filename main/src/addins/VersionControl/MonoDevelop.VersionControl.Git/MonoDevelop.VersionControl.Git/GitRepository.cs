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

using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

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
			StringReader sr = RunCommand ("log --name-status --date=iso " + localFile, true);
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
						message.AppendLine (line);
					}
					else
						break;
				}
				revs.Add (new GitRevision (this, rev, date, author, message.ToString (), paths.ToArray ()));
			}
			return revs.ToArray ();
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
//			Console.WriteLine (cmd);
			StringWriter outw = new StringWriter ();
			ProcessWrapper proc = Runtime.ProcessService.StartProcess ("git", "--no-pager " + cmd, path, outw, outw, null);
			proc.WaitForOutput ();
			if (monitor != null)
				monitor.Log.Write (outw.ToString ());
//			Console.WriteLine (outw.ToString ());
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
		
		VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, string fileName, bool getRemoteStatus, bool recursive)
		{
			string strRev = "";
			StringReader sr = RunCommand ("log -1 --pretty=raw", true);
			string cl = sr.ReadLine ();
			if (cl != null && cl.StartsWith ("commit "))
				strRev = cl.Substring (7);
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
			sr = RunCommand ("status --porcelain " + p, true);
			string line;
			while ((line = sr.ReadLine ()) != null) {
				char staged = line[0];
				string stat = line.Substring (0, 2);
				string file = line.Substring (3);
				int i = file.IndexOf ("->");
				if (i != -1)
					file = file.Substring (i + 3);
				FilePath statFile = path.Combine (file);
				if (statFile.ParentDirectory != localDirectory && (!statFile.IsChildPathOf (localDirectory) || !recursive))
					continue;
				VersionStatus status;
				if (stat == " M" || stat == "MM")
					status = VersionStatus.Versioned | VersionStatus.Modified;
				else if (staged == 'A')
					status = VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				else if (stat == " D")
					status = VersionStatus.Versioned | VersionStatus.ScheduledDelete;
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
			IEnumerable<string> output = ToList (RunCommand ("pull --rebase --ff --stat " + GetCurrentRemote () + " " + GetCurrentBranch (), true, monitor));
			foreach (string line in output) {
				int i = line.IndexOf ('|');
				if (i != -1 && line[0] == ' ') {
					FilePath file = path.Combine (line.Substring (1, i - 1).Trim ());
					if (File.Exists (file))
						FileService.NotifyFileChanged (file);
					else
						FileService.NotifyFileRemoved (file);
				}
			}
		}
		
		
		public override void Commit (ChangeSet changeSet, IProgressMonitor monitor)
		{
			string file = Path.GetTempFileName ();
			try {
				File.WriteAllText (file, changeSet.GlobalComment);
				StringBuilder sb = new StringBuilder ();
				foreach (ChangeSetItem it in changeSet.Items)
					sb.Append (" \"").Append (it.LocalPath).Append ('"');
				RunCommand ("commit -F " + file + sb, true, monitor);
			} finally {
				File.Delete (file);
			}
		}
		
		public override void Checkout (FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			RunCommand ("clone " + Url + " " + targetLocalPath, true, monitor);
		}
		
		
		public override void Revert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (FilePath it in localPaths)
				sb.Append (" \"").Append (it).Append ('"');
			RunCommand ("reset --" + sb, false, monitor);
			RunCommand ("checkout --" + sb, false, monitor);
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
			StringBuilder sb = new StringBuilder ();
			foreach (FilePath it in localPaths)
				sb.Append (" \"").Append (it).Append ('"');
			RunCommand ("add " + sb, true, monitor);
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
				StringReader sr = RunCommand ("diff \"" + baseLocalPath + "\"", true);
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
			List<string> remotes = new List<string> (GetNamedRemotes ());
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
		
		public IEnumerable<string> GetNamedRemotes ()
		{
			StringReader sr = RunCommand ("remote", true);
			string line;
			while ((line = sr.ReadLine ()) != null) {
				yield return line;
			}
		}
		
		public IEnumerable<string> GetBranches ()
		{
			StringReader sr = RunCommand ("branch", true);
			string line;
			while ((line = sr.ReadLine ()) != null) {
				if (line.StartsWith ("* "))
					yield return line.Substring (2).Trim ();
				else
					yield return line.Trim ();
			}
		}
		
		public IEnumerable<string> GetRemoteBranches (string remoteName)
		{
			StringReader sr = RunCommand ("branch -r", true);
			string line;
			while ((line = sr.ReadLine ()) != null) {
				if (line.StartsWith ("  " + remoteName + "/") || line.StartsWith ("* " + remoteName + "/")) {
					int i = line.IndexOf ('/');
					yield return line.Substring (i+1);
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
			StringReader sr = RunCommand ("diff " + branch + " --name-status", true);
			RunCommand ("checkout " + branch, true);
			
			foreach (string line in ToList (sr)) {
				char s = line [0];
				FilePath file = path.Combine (line.Substring (2));
				if (s == 'A')
					// File added to source branch not present to target branch.
					FileService.NotifyFileRemoved (file);
				else
					FileService.NotifyFileChanged (file);
			}
			if (BranchSelectionChanged != null)
				BranchSelectionChanged (this, EventArgs.Empty);
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
	}
	
	class GitRevision: Revision
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
			throw new System.NotImplementedException();
		}
	}
}

