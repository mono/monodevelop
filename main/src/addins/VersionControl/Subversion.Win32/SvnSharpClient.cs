using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.VersionControl;
using MonoDevelop.VersionControl.Subversion;
using SharpSvn;
using SharpSvn.Security;
using SvnRevision = MonoDevelop.VersionControl.Subversion.SvnRevision;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Text;
using System.Timers;

namespace SubversionAddinWindows
{
	sealed class SvnSharpClient: SubversionVersionControl
	{
		static bool errorShown;
		static bool installError {
			get { return client.Value == null; }
		}
		static readonly internal Lazy<SvnClient> client;
		
		static SvnSharpClient ()
		{
			client = new Lazy<SvnClient> (CheckInstalled);
		}

		public override string Version {
			get {
				return SvnClient.Version.ToString ();
			}
		}

		static SvnClient CheckInstalled ()
		{
			try {
				return new SvnClient ();
			} catch (Exception ex) {
				LoggingService.LogError ("SVN client could not be initialized", ex);
			}
			return null;
		}

		public override SubversionBackend CreateBackend ()
		{
			return new SvnSharpBackend ();
		}

		public override bool IsInstalled
		{
			get
			{
				if (!errorShown && installError) {
					errorShown = true;
					var db = new AlertButton ("Go to Download Page");
					AlertButton res = MessageService.AskQuestion ("The Subversion add-in could not be initialized", "This add-in requires the 'Microsoft Visual C++ 2005 Service Pack 1 Redistributable'. You may need to install it.", db, AlertButton.Ok);
					if (res == db) {
						DesktopService.ShowUrl ("http://www.microsoft.com/downloads/details.aspx?familyid=766a6af7-ec73-40ff-b072-9112bab119c2");
					}
				}
				return !installError;
			}
		}

		public override string GetDirectoryDotSvn (FilePath path)
		{
			string wc_path;
			try {
				lock (client.Value)
					wc_path = client.Value.GetWorkingCopyRoot (path.FullPath);
				return wc_path;
			} catch (SvnException e) {
				switch (e.SvnErrorCode) {
				case SvnErrorCode.SVN_ERR_WC_NOT_WORKING_COPY:
				case SvnErrorCode.SVN_ERR_WC_NOT_FILE:
					return "";
				}
				throw;
			}
		}
	}

	sealed class SvnSharpBackend: SubversionBackend
	{
		static SvnClient client {
			get { return SvnSharpClient.client.Value; }
		}

		ProgressMonitor updateMonitor;
		NotifData notifyData;
		ProgressData progressData;

		public override string GetTextBase (string sourcefile)
		{
			var data = new MemoryStream ();
			try {
				// This outputs the contents of the base revision
				// of a file to a stream.
				lock (client)
					client.Write (new SvnPathTarget (sourcefile), data);
				return TextFile.ReadFile (sourcefile, data).Text;
			} catch (SvnIllegalTargetException e) {
				// This occurs when we don't have a base file for
				// the target file. We have no way of knowing if
				// a file has a base version therefore this will do.
				if (e.SvnErrorCode == SvnErrorCode.SVN_ERR_ILLEGAL_TARGET)
					return String.Empty;
				throw;
			}
		}

		public SvnSharpBackend ()
		{
			Init ();
		}

		void Init ()
		{
			client.Authentication.SslClientCertificateHandlers += AuthenticationSslClientCertificateHandlers;
			client.Authentication.SslClientCertificatePasswordHandlers += AuthenticationSslClientCertificatePasswordHandlers;
			client.Authentication.SslServerTrustHandlers += AuthenticationSslServerTrustHandlers;
			client.Authentication.UserNameHandlers += AuthenticationUserNameHandlers;
			client.Authentication.UserNamePasswordHandlers += AuthenticationUserNamePasswordHandlers;
			client.Notify += delegate (object o, SvnNotifyEventArgs e) {
				if (updateMonitor == null)
					return;

				Notify (e, notifyData, updateMonitor);
			};
			client.SvnError += delegate (object o, SvnErrorEventArgs a) {
				if (updateMonitor == null)
					return;

				updateMonitor.ReportError (a.Exception.Message, a.Exception.RootCause);
			};
			client.Progress += delegate (object sender, SvnProgressEventArgs e) {
				if (updateMonitor == null)
					return;

				ProgressWork (e, progressData, updateMonitor);
			};
			client.Cancel += delegate (object o, SvnCancelEventArgs a) {
				if (updateMonitor == null)
					return;

				a.Cancel = updateMonitor.CancellationToken.IsCancellationRequested;
			};
		}

		static void AuthenticationUserNamePasswordHandlers (object sender, SvnUserNamePasswordEventArgs e)
		{
			string user = e.UserName;
			string password;
			bool save;
			e.Cancel = !SimpleAuthenticationPrompt (e.Realm, e.MaySave, ref user, out password, out save);
			e.UserName = user;
			e.Password = password;
			e.Save = save;
		}

		static void AuthenticationUserNameHandlers (object sender, SvnUserNameEventArgs e)
		{
			string name = e.UserName;
			bool save;
			e.Cancel = !UserNameAuthenticationPrompt (e.Realm, e.MaySave, ref name, out save);
			e.UserName = name;
			e.Save = save;
		}

		static void AuthenticationSslServerTrustHandlers (object sender, SvnSslServerTrustEventArgs e)
		{
			SslFailure acceptedFailures;
			bool save;

			var certInfo = new CertficateInfo {
				AsciiCert = e.CertificateValue,
				Fingerprint = e.Fingerprint,
				HostName = e.CommonName,
				IssuerName = e.Issuer,
				ValidFrom = e.ValidFrom,
				ValidUntil = e.ValidUntil,
			};

			e.Cancel = !SslServerTrustAuthenticationPrompt (e.Realm, (SslFailure) (uint) e.Failures, e.MaySave, certInfo, out acceptedFailures, out save);

			e.AcceptedFailures = (SvnCertificateTrustFailures) (int) acceptedFailures;
			e.Save = save;
		}

		static void AuthenticationSslClientCertificatePasswordHandlers (object sender, SvnSslClientCertificatePasswordEventArgs e)
		{
			string password;
			bool save;
			e.Cancel = !SslClientCertPwAuthenticationPrompt (e.Realm, e.MaySave, out password, out save);
			e.Password = password;
			e.Save = save;
		}

		static void AuthenticationSslClientCertificateHandlers (object sender, SvnSslClientCertificateEventArgs e)
		{
			string file;
			bool save;
			e.Cancel = !SslClientCertAuthenticationPrompt (e.Realm, e.MaySave, out file, out save);
			e.Save = save;
			e.CertificateFile = file;
		}

		public override void Add (FilePath path, bool recurse, ProgressMonitor monitor)
		{
			var args = new SvnAddArgs {
				Depth = recurse ? SvnDepth.Infinity : SvnDepth.Empty,
				Force = true,
			};
			BindMonitor (monitor);
			lock (client)
				client.Add (path, args);
		}

		public override void Checkout (string url, FilePath path, Revision rev, bool recurse, ProgressMonitor monitor)
		{
			var args = new SvnCheckOutArgs {
				Depth = recurse ? SvnDepth.Infinity : SvnDepth.Empty,
			};
			BindMonitor (monitor);
			lock (client) {
				try {
					client.CheckOut (new SvnUriTarget (url, GetRevision (rev)), path, args);
				} catch (SvnOperationCanceledException) {
					if (Directory.Exists (path.ParentDirectory))
						FileService.DeleteDirectory (path.ParentDirectory);
				}
			}
		}

		public override void Commit (FilePath[] paths, string message, ProgressMonitor monitor)
		{
			var args = new SvnCommitArgs {
				LogMessage = message,
			};
			BindMonitor (monitor);
			lock (client) 
				client.Commit (paths.ToStringArray (), args);
		}

		public override void Delete (FilePath path, bool force, ProgressMonitor monitor)
		{
			var args = new SvnDeleteArgs {
				Force = force,
			};
			BindMonitor (monitor);
			lock (client) 
				client.Delete (path, args);
		}

		public override string GetTextAtRevision (string repositoryPath, Revision revision, string rootPath)
		{
			var ms = new MemoryStream ();
			SvnUriTarget target;
			lock (client)
				target = client.GetUriFromWorkingCopy (rootPath);
			// Redo path link.
			repositoryPath = repositoryPath.TrimStart (new [] { '/' });
			foreach (var segment in target.Uri.Segments) {
				if (repositoryPath.StartsWith (segment, StringComparison.Ordinal))
					repositoryPath = repositoryPath.Remove (0, segment.Length);
			}

			lock (client) {
				// repositoryPath already contains the relative URL path.
				try {
					client.Write (new SvnUriTarget (target.Uri.AbsoluteUri + repositoryPath, GetRevision (revision)), ms);
				} catch (SvnFileSystemException e) {
					// File got added/deleted at some point.
					if (e.SvnErrorCode == SvnErrorCode.SVN_ERR_FS_NOT_FOUND)
						return "";
					throw;
				} catch (SvnClientNodeKindException e) {
					// We're trying on a directory.
					if (e.SvnErrorCode == SvnErrorCode.SVN_ERR_CLIENT_IS_DIRECTORY)
						return "";
					throw;
				}
			}
			return TextFile.ReadFile (repositoryPath, ms).Text;
		}

		public override string GetVersion ()
		{
			lock (client)
				return SvnClient.Version.ToString ();
		}

		public override IEnumerable<DirectoryEntry> ListUrl (string url, bool recurse, SvnRevision rev)
		{
			return List (new SvnUriTarget (url, GetRevision (rev)), recurse);
		}

		public override IEnumerable<DirectoryEntry> List (FilePath path, bool recurse, SvnRevision rev)
		{
			return List (new SvnPathTarget (path, GetRevision (rev)), recurse);
		}

		static IEnumerable<DirectoryEntry> List (SvnTarget target, bool recurse)
		{
			var list = new List<DirectoryEntry> ();
			var args = new SvnListArgs {
				Depth = recurse ? SvnDepth.Infinity : SvnDepth.Children,
			};
			lock (client) 
				client.List (target, args, delegate (object o, SvnListEventArgs a) {
					if (string.IsNullOrEmpty (a.Path))
						return;
					list.Add (new DirectoryEntry {
						CreatedRevision = ToBaseRevision (a.Entry.Revision).Rev,
						HasProps = a.Entry.HasProperties,
						IsDirectory = a.Entry.NodeKind == SvnNodeKind.Directory,
						LastAuthor = a.Entry.Author,
						Name = a.Path,
						Size = a.Entry.FileSize,
						Time = a.Entry.Time,
					});
				});
			return list;
		}

		public override IEnumerable<SvnRevision> Log (Repository repo, FilePath path, SvnRevision revisionStart, SvnRevision revisionEnd)
		{
			var list = new List<SvnRevision> ();
			var args = new SvnLogArgs {
				Range = new SvnRevisionRange (GetRevision (revisionStart), GetRevision (revisionEnd)),
			};
			lock (client)
				client.Log (path, args, (o, a) =>
					list.Add (new SvnRevision (repo, (int)a.Revision, a.Time, a.Author, a.LogMessage,
						a.ChangedPaths.Select (item => new RevisionPath (item.Path, ConvertRevisionAction (item.Action), "")).ToArray ())));
			return list;
		}

		static RevisionAction ConvertRevisionAction (SvnChangeAction svnChangeAction)
		{
			switch (svnChangeAction) {
				case SvnChangeAction.Add: return RevisionAction.Add;
				case SvnChangeAction.Delete: return RevisionAction.Delete;
				case SvnChangeAction.Modify: return RevisionAction.Modify;
				case SvnChangeAction.Replace: return RevisionAction.Replace;
			}
			return RevisionAction.Other;
		}

		public override void Mkdir (string[] paths, string message, ProgressMonitor monitor)
		{
			var args = new SvnCreateDirectoryArgs {
				CreateParents = true,
				LogMessage = message,
			};
			BindMonitor (monitor);
			var uris = paths.Select (p => new Uri (p)).ToArray ();
			lock (client) 
				client.RemoteCreateDirectories (uris, args);
		}

		public override void Move (FilePath srcPath, FilePath destPath, SvnRevision rev, bool force, ProgressMonitor monitor)
		{
			var args = new SvnMoveArgs {
				Force = force,
			};
			BindMonitor (monitor);
			lock (client) 
				client.Move (srcPath, destPath, args);
		}

		public override string GetUnifiedDiff (FilePath path1, SvnRevision revision1, FilePath path2, SvnRevision revision2, bool recursive)
		{
			var t1 = new SvnPathTarget (path1, GetRevision (revision1));
			var t2 = new SvnPathTarget (path2, GetRevision (revision2));
			var args = new SvnDiffArgs {
				Depth = recursive ? SvnDepth.Infinity : SvnDepth.Children,
			};
			using (var ms = new MemoryStream ()) {
				lock (client)
					client.Diff (t1, t2, args, ms);
				ms.Position = 0;
				using (var sr = new StreamReader (ms)) {
					return sr.ReadToEnd ();
				}
			}
		}

		public override void Revert (FilePath[] paths, bool recurse, ProgressMonitor monitor)
		{
			var args = new SvnRevertArgs {
				Depth = recurse ? SvnDepth.Infinity : SvnDepth.Children,
			};
			BindMonitor (monitor);
			lock (client) 
				client.Revert (paths.ToStringArray (), args);
		}

		public override void RevertRevision (FilePath path, Revision revision, ProgressMonitor monitor)
		{
			var args = new SvnMergeArgs ();
			BindMonitor (monitor);
			Revision prev = ((SvnRevision) revision).GetPrevious ();
			var range = new SvnRevisionRange (GetRevision (revision), GetRevision (prev));
			lock (client) 
				client.Merge (path, new SvnPathTarget (path), range, args);
		}

		public override void RevertToRevision (FilePath path, Revision revision, ProgressMonitor monitor)
		{
			var args = new SvnMergeArgs ();
			BindMonitor (monitor);
			var range = new SvnRevisionRange (GetRevision (SvnRevision.Head), GetRevision (revision));
			lock (client) 
				client.Merge (path, new SvnPathTarget (path), range, args);
		}

		public override IEnumerable<VersionInfo> Status (Repository repo, FilePath path, SvnRevision revision, bool descendDirs, bool changedItemsOnly, bool remoteStatus)
		{
			var list = new List<VersionInfo> ();
			var args = new SvnStatusArgs {
				Revision = GetRevision (revision),
				Depth = descendDirs ? SvnDepth.Infinity : SvnDepth.Children,
				RetrieveAllEntries = !changedItemsOnly,
				RetrieveRemoteStatus = remoteStatus,
			};
			lock (client) {
				try {
					client.Status (path, args, (o, a) => list.Add (CreateVersionInfo (repo, a)));
				} catch (SvnInvalidNodeKindException e) {
					if (e.SvnErrorCode == SvnErrorCode.SVN_ERR_WC_NOT_WORKING_COPY)
						list.Add (VersionInfo.CreateUnversioned (e.File, true));
					else if (e.SvnErrorCode == SvnErrorCode.SVN_ERR_WC_NOT_FILE)
						list.Add (VersionInfo.CreateUnversioned (e.File, false));
					else
						throw;
				} catch (SvnWorkingCopyPathNotFoundException e) {
					list.Add (VersionInfo.CreateUnversioned (e.File, Directory.Exists (e.File)));
				}
			}
			return list;
		}

		static VersionInfo CreateVersionInfo (Repository repo, SvnStatusEventArgs ent)
		{
			VersionStatus rs = VersionStatus.Unversioned;
			Revision rr = null;

			// TODO: Fix remote status for Win32 Svn.
			if (ent.IsRemoteUpdated) {
				rs = ConvertStatus (SvnSchedule.Normal, ent.RemoteContentStatus);
				rr = new SvnRevision (repo, (int) ent.RemoteUpdateRevision, ent.RemoteUpdateCommitTime,
									  ent.RemoteUpdateCommitAuthor, "(unavailable)", null);
			}

			VersionStatus status = ConvertStatus (SvnSchedule.Normal, ent.LocalContentStatus);

			bool readOnly = File.Exists (ent.FullPath) && (File.GetAttributes (ent.FullPath) & FileAttributes.ReadOnly) != 0;

			if (ent.WorkingCopyInfo != null) {
				if (ent.RemoteLock != null || ent.LocalLock != null) {
					status |= VersionStatus.LockRequired;
					if (ent.LocalLock != null || (ent.RemoteLock != null && ent.RemoteLock.Token != null))
						status |= VersionStatus.LockOwned;
					else
						status |= VersionStatus.Locked;
				}
				else if (readOnly)
					status |= VersionStatus.LockRequired;
			}

			string repoPath = ent.Uri != null ? ent.Uri.ToString () : null;
			SvnRevision newRev = null;
			if (ent.WorkingCopyInfo != null)
				newRev = new SvnRevision (repo, (int) ent.Revision);

			return new VersionInfo (ent.FullPath, repoPath, ent.NodeKind == SvnNodeKind.Directory,
				status, newRev,
				rs, rr);
		}

		static VersionStatus ConvertStatus (SvnSchedule schedule, SvnStatus status)
		{
			switch (schedule) {
				case SvnSchedule.Add: return VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				case SvnSchedule.Delete: return VersionStatus.Versioned | VersionStatus.ScheduledDelete;
				case SvnSchedule.Replace: return VersionStatus.Versioned | VersionStatus.ScheduledReplace;
			}

			switch (status) {
				case SvnStatus.None: return VersionStatus.Versioned;
				case SvnStatus.Normal: return VersionStatus.Versioned;
				case SvnStatus.NotVersioned: return VersionStatus.Unversioned;
				case SvnStatus.Modified: return VersionStatus.Versioned | VersionStatus.Modified;
				case SvnStatus.Merged: return VersionStatus.Versioned | VersionStatus.Modified;
				case SvnStatus.Conflicted: return VersionStatus.Versioned | VersionStatus.Conflicted;
				case SvnStatus.Ignored: return VersionStatus.Unversioned | VersionStatus.Ignored;
				case SvnStatus.Obstructed: return VersionStatus.Versioned;
				case SvnStatus.Added: return VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				case SvnStatus.Deleted: return VersionStatus.Versioned | VersionStatus.ScheduledDelete;
				case SvnStatus.Replaced: return VersionStatus.Versioned | VersionStatus.ScheduledReplace;
			}

			return VersionStatus.Unversioned;
		}

		public override void Lock (ProgressMonitor monitor, string comment, bool stealLock, params FilePath[] paths)
		{
			var args = new SvnLockArgs {
				Comment = comment,
				StealLock = stealLock,
			};
			BindMonitor (monitor);
			lock (client) 
				client.Lock (paths.ToStringArray (), args);
		}

		public override void Unlock (ProgressMonitor monitor, bool breakLock, params FilePath[] paths)
		{
			var args = new SvnUnlockArgs {
				BreakLock = breakLock,
			};
			BindMonitor (monitor);
			lock (client) 
				client.Unlock (paths.ToStringArray (), args);
		}

		public override void Update (FilePath path, bool recurse, ProgressMonitor monitor)
		{
			var args = new SvnUpdateArgs {
				Depth = recurse ? SvnDepth.Infinity : SvnDepth.Children,
			};
			BindMonitor (monitor);
			lock (client)
				client.Update (path, args);
		}

		public override void Ignore (FilePath[] paths)
		{
			string result;
			lock (client) {
				foreach (var path in paths) {
					if (client.GetProperty (new SvnPathTarget (path.ParentDirectory), SvnPropertyNames.SvnIgnore, out result)) {
						client.SetProperty (path.ParentDirectory, SvnPropertyNames.SvnIgnore, result + path.FileName);
					}
				}
			}
		}

		public override void Unignore (FilePath[] paths)
		{
			string result;
			lock (client) {
				foreach (var path in paths) {
					if (client.GetProperty (new SvnPathTarget (path.ParentDirectory), SvnPropertyNames.SvnIgnore, out result)) {
						int index = result.IndexOf (path.FileName + Environment.NewLine, StringComparison.Ordinal);
						result = (index < 0) ? result : result.Remove (index, path.FileName.Length+Environment.NewLine.Length);
						client.SetProperty (path.ParentDirectory, SvnPropertyNames.SvnIgnore, result);
					}
				}
			}
		}

		public override Annotation[] GetAnnotations (Repository repo, FilePath file, SvnRevision revStart, SvnRevision revEnd)
		{
			if (file == FilePath.Null)
				throw new ArgumentNullException ();

			var target = new SvnPathTarget (file, SharpSvn.SvnRevision.Base);
			int numAnnotations = 0;
			using (var data = new MemoryStream ()) {
				lock (client)
					client.Write (target, data);

				using (var reader = new StreamReader (data)) {
					reader.BaseStream.Seek (0, SeekOrigin.Begin);
					while (reader.ReadLine () != null)
						numAnnotations++;
				}
			}

			System.Collections.ObjectModel.Collection<SvnBlameEventArgs> list;
			var args = new SvnBlameArgs {
				Start = GetRevision (revStart),
				End = GetRevision (revEnd),
			};

			bool success;
			lock (client) {
				success = client.GetBlame (target, args, out list);
			}
			if (success) {
				var annotations = new Annotation [numAnnotations];
				foreach (var annotation in list) {
					if (annotation.LineNumber < annotations.Length)
						annotations [(int)annotation.LineNumber] = new Annotation (annotation.Revision.ToString (),
																					annotation.Author, annotation.Time);
				}
				return annotations;
			}
			return new Annotation[0];
		}

		static SharpSvn.SvnRevision GetRevision (Revision rev)
		{
			if (rev == null)
				return null;
			var srev = (SvnRevision) rev;
			if (srev == SvnRevision.Base)
				return new SharpSvn.SvnRevision (SvnRevisionType.Base);
			if (srev == SvnRevision.Committed)
				return new SharpSvn.SvnRevision (SvnRevisionType.Committed);
			if (srev == SvnRevision.Head)
				return new SharpSvn.SvnRevision (SvnRevisionType.Head);
			if (srev == SvnRevision.Previous)
				return new SharpSvn.SvnRevision (SvnRevisionType.Previous);
			if (srev == SvnRevision.Working)
				return new SharpSvn.SvnRevision (SvnRevisionType.Working);
			return new SharpSvn.SvnRevision (srev.Rev);
		}

		static SvnRevision ToBaseRevision (SharpSvn.SvnRevision rev)
		{
			if (rev.RevisionType == SvnRevisionType.Base)
				return SvnRevision.Base;
			if (rev.RevisionType == SvnRevisionType.Committed)
				return SvnRevision.Committed;
			if (rev.RevisionType == SvnRevisionType.Head)
				return SvnRevision.Head;
			if (rev.RevisionType == SvnRevisionType.Previous)
				return SvnRevision.Previous;
			if (rev.RevisionType == SvnRevisionType.Working)
				return SvnRevision.Working;
			if (rev.RevisionType == SvnRevisionType.Number)
				return new SvnRevision (null, (int) rev.Revision);
			throw new SubversionException ("Unknown revision type: " + rev.RevisionType);
		}

		class NotifData
		{
			public bool SendingData;
		}

		class ProgressData
		{
			// It's big enough. You don't see repos with more than 5Gb.
			public long Remainder;
			public long SavedProgress;
			public long KBytes;
			public Timer LogTimer = new Timer ();
			public int Seconds;
		}

		void BindMonitor (ProgressMonitor monitor)
		{
			notifyData = new NotifData ();
			progressData = new ProgressData ();

			updateMonitor = monitor;
		}

		static string BytesToSize (long kbytes)
		{
			if (kbytes < 1024)
				return String.Format ("{0} KBytes", kbytes);
			return String.Format ("{0:0.00} MBytes", kbytes / 1024.0);
		}

		static void ProgressWork (SvnProgressEventArgs e, ProgressData data, ProgressMonitor monitor)
		{
			if (monitor == null)
				return;

			long currentProgress = e.Progress;
			if (currentProgress <= data.KBytes) {
				if (data.SavedProgress < data.KBytes) {
					data.SavedProgress += data.KBytes;
				}
				return;
			}

			long totalProgress = e.TotalProgress;
			if (totalProgress != -1 && currentProgress >= totalProgress) {
				data.LogTimer.Dispose ();
				return;
			}

			data.Remainder += currentProgress % 1024;
			if (data.Remainder >= 1024) {
				data.SavedProgress += data.Remainder / 1024;
				data.Remainder = data.Remainder % 1024;
			}

			data.KBytes = data.SavedProgress + currentProgress / 1024;
			if (data.LogTimer.Enabled)
				return;

			data.LogTimer.Interval = 1000;
			data.LogTimer.Elapsed += delegate {
				data.Seconds += 1;
				monitor.Log.WriteLine ("Transferred {0} in {1} seconds.", BytesToSize (data.KBytes), data.Seconds);
			};
			data.LogTimer.Start ();
		}

		static void Notify (SvnNotifyEventArgs e, NotifData notifData, ProgressMonitor monitor)
		{
			string actiondesc;
			string file = e.Path;
			bool skipEol = false;
			bool notifyChange = false;

			switch (e.Action) {
			case SvnNotifyAction.Skip:
				if (e.ContentState == SvnNotifyState.Missing) {
					actiondesc = string.Format (GettextCatalog.GetString ("Skipped missing target: '{0}'"), file);
				} else {
					actiondesc = string.Format (GettextCatalog.GetString ("Skipped '{0}'"), file);
				}
				break;
			case SvnNotifyAction.UpdateDelete:
				actiondesc = string.Format (GettextCatalog.GetString ("Deleted   '{0}'"), file);
				break;

			case SvnNotifyAction.UpdateAdd:
				if (e.ContentState == SvnNotifyState.Conflicted) {
					actiondesc = string.Format (GettextCatalog.GetString ("Conflict {0}"), file);
				} else {
					actiondesc = string.Format (GettextCatalog.GetString ("Added   {0}"), file);
				}
				break;
			case SvnNotifyAction.Restore:
				actiondesc = string.Format (GettextCatalog.GetString ("Restored '{0}'"), file);
				break;
			case SvnNotifyAction.Revert:
				actiondesc = string.Format (GettextCatalog.GetString ("Reverted '{0}'"), file);
				break;
			case SvnNotifyAction.RevertFailed:
				actiondesc = string.Format (GettextCatalog.GetString ("Failed to revert '{0}' -- try updating instead."), file);
				break;
			case SvnNotifyAction.Resolved:
				actiondesc = string.Format (GettextCatalog.GetString ("Resolved conflict state of '{0}'"), file);
				break;
			case SvnNotifyAction.Add:
				if (e.MimeTypeIsBinary) {
					actiondesc = string.Format (GettextCatalog.GetString ("Add (bin) '{0}'"), file);
				} else {
					actiondesc = string.Format (GettextCatalog.GetString ("Add       '{0}'"), file);
				}
				break;
			case SvnNotifyAction.Delete:
				actiondesc = string.Format (GettextCatalog.GetString ("Delete    '{0}'"), file);
				break;

			case SvnNotifyAction.UpdateUpdate:
				actiondesc = string.Format (GettextCatalog.GetString ("Update '{0}'"), file);
				notifyChange = true;
				break;
			case SvnNotifyAction.UpdateExternal:
				actiondesc = string.Format (GettextCatalog.GetString ("Fetching external item into '{0}'"), file);
				break;
			case SvnNotifyAction.UpdateCompleted:  // TODO
				actiondesc = GettextCatalog.GetString ("Finished");
				break;
			case SvnNotifyAction.StatusExternal:
				actiondesc = string.Format (GettextCatalog.GetString ("Performing status on external item at '{0}'"), file);
				break;
			case SvnNotifyAction.StatusCompleted:
				actiondesc = string.Format (GettextCatalog.GetString ("Status against revision: '{0}'"), e.Revision);
				break;

			case SvnNotifyAction.CommitDeleted:
				actiondesc = string.Format (GettextCatalog.GetString ("Deleting       {0}"), file);
				break;
			case SvnNotifyAction.CommitModified:
				actiondesc = string.Format (GettextCatalog.GetString ("Sending        {0}"), file);
				notifyChange = true;
				break;
			case SvnNotifyAction.CommitAdded:
				if (e.MimeTypeIsBinary) {
					actiondesc = string.Format (GettextCatalog.GetString ("Adding  (bin)  '{0}'"), file);
				} else {
					actiondesc = string.Format (GettextCatalog.GetString ("Adding         '{0}'"), file);
				}
				break;
			case SvnNotifyAction.CommitReplaced:
				actiondesc = string.Format (GettextCatalog.GetString ("Replacing      {0}"), file);
				notifyChange = true;
				break;
			case SvnNotifyAction.CommitSendData:
				if (!notifData.SendingData) {
					notifData.SendingData = true;
					actiondesc = GettextCatalog.GetString ("Transmitting file data");
				} else {
					actiondesc = ".";
					skipEol = true;
				}
				break;

			case SvnNotifyAction.LockLocked:
				actiondesc = string.Format (GettextCatalog.GetString ("'{0}' locked by user '{1}'."), file, e.Lock.Owner);
				break;
			case SvnNotifyAction.LockUnlocked:
				actiondesc = string.Format (GettextCatalog.GetString ("'{0}' unlocked."), file);
				break;
			default:
				actiondesc = e.Action + " " + file;
				break;
			}

			if (monitor != null) {
				if (skipEol)
					monitor.Log.Write (actiondesc);
				else
					monitor.Log.WriteLine (actiondesc);
			}

			if (notifyChange && File.Exists (file))
				FileService.NotifyFileChanged (file, true);
		}

		public override bool HasNeedLock (FilePath file)
		{
			string tmp;
			lock (client)
				return client.GetProperty (new SvnPathTarget (file), SvnPropertyNames.SvnNeedsLock, out tmp);
		}
	}
}
