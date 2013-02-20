using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.VersionControl;
using MonoDevelop.VersionControl.Subversion;
using SharpSvn;
using SharpSvn.Security;
using SvnRevision = MonoDevelop.VersionControl.Subversion.SvnRevision;
using MonoDevelop.Ide;

namespace SubversionAddinWindows
{
	public class SvnSharpClient: SubversionVersionControl
	{
		static bool errorShown;
		static bool installError;
		static SvnClient client;
		
		static SvnSharpClient ()
		{
			try {
				client = new SvnClient ();
			} catch (Exception ex) {
				LoggingService.LogError ("SVN client could not be initialized", ex);
				installError = true;
			}
		}

		public override SubversionBackend CreateBackend ()
		{
			return new SvnSharpBackend ();
		}
			
		public override string GetPathUrl (FilePath path)
		{
			lock (client) {
				Uri u = client.GetUriFromWorkingCopy (path);
				return u != null ? u.ToString () : null;
			}
		}

		public override bool IsInstalled
		{
			get
			{
				if (!errorShown && installError) {
					errorShown = true;
					AlertButton db = new AlertButton ("Go to Download Page");
					AlertButton res = MessageService.AskQuestion ("The Subversion add-in could not be initialized", "This add-in requires the 'Microsoft Visual C++ 2005 Service Pack 1 Redistributable'. You may need to install it.", db, AlertButton.Ok);
					if (res == db) {
						DesktopService.ShowUrl ("http://www.microsoft.com/downloads/details.aspx?familyid=766a6af7-ec73-40ff-b072-9112bab119c2");
					}
				}
				return !installError;
			}
		}
	}

	public class SvnSharpBackend: SubversionBackend
	{
		SvnClient client;

		public SvnSharpBackend ()
		{
			Init ();
		}

		void Init ( )
		{
			client = new SvnClient ();
			client.Authentication.SslClientCertificateHandlers += new EventHandler<SharpSvn.Security.SvnSslClientCertificateEventArgs> (AuthenticationSslClientCertificateHandlers);
			client.Authentication.SslClientCertificatePasswordHandlers += new EventHandler<SvnSslClientCertificatePasswordEventArgs> (AuthenticationSslClientCertificatePasswordHandlers);
			client.Authentication.SslServerTrustHandlers += new EventHandler<SvnSslServerTrustEventArgs> (AuthenticationSslServerTrustHandlers);
			client.Authentication.UserNameHandlers += new EventHandler<SvnUserNameEventArgs> (AuthenticationUserNameHandlers);
			client.Authentication.UserNamePasswordHandlers += new EventHandler<SvnUserNamePasswordEventArgs> (AuthenticationUserNamePasswordHandlers);
		}

		void AuthenticationUserNamePasswordHandlers (object sender, SvnUserNamePasswordEventArgs e)
		{
			string user = e.UserName;
			string password;
			bool save;
			e.Cancel = !SimpleAuthenticationPrompt (e.Realm, e.MaySave, ref user, out password, out save);
			e.UserName = user;
			e.Password = password;
			e.Save = save;
		}

		void AuthenticationUserNameHandlers (object sender, SvnUserNameEventArgs e)
		{
			string name = e.UserName;
			bool save;
			e.Cancel = !UserNameAuthenticationPrompt (e.Realm, e.MaySave, ref name, out save);
			e.UserName = name;
			e.Save = save;
		}

		void AuthenticationSslServerTrustHandlers (object sender, SvnSslServerTrustEventArgs e)
		{
			SslFailure acceptedFailures;
			bool save;

			CertficateInfo certInfo = new CertficateInfo ();
			certInfo.AsciiCert = e.CertificateValue;
			certInfo.Fingerprint = e.Fingerprint;
			certInfo.HostName = e.CommonName;
			certInfo.IssuerName = e.Issuer;
			certInfo.ValidFrom = e.ValidFrom;
			certInfo.ValidUntil = e.ValidUntil;

			e.Cancel = !SslServerTrustAuthenticationPrompt (e.Realm, (SslFailure) (uint) e.Failures, e.MaySave, certInfo, out acceptedFailures, out save);

			e.AcceptedFailures = (SvnCertificateTrustFailures) (int) acceptedFailures;
			e.Save = save;
		}

		void AuthenticationSslClientCertificatePasswordHandlers (object sender, SvnSslClientCertificatePasswordEventArgs e)
		{
			string password;
			bool save;
			e.Cancel = !SslClientCertPwAuthenticationPrompt (e.Realm, e.MaySave, out password, out save);
			e.Password = password;
			e.Save = save;
		}

		void AuthenticationSslClientCertificateHandlers (object sender, SvnSslClientCertificateEventArgs e)
		{
			string file;
			bool save;
			e.Cancel = !SslClientCertAuthenticationPrompt (e.Realm, e.MaySave, out file, out save);
			e.Save = save;
			e.CertificateFile = file;
		}

		public override void Add (FilePath path, bool recurse, IProgressMonitor monitor)
		{
			SvnAddArgs args = new SvnAddArgs ();
			BindMonitor (args, monitor);
			args.Depth = recurse ? SvnDepth.Infinity : SvnDepth.Empty;
			lock (client)
				client.Add (path, args);
		}

		public override void Checkout (string url, FilePath path, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			SvnCheckOutArgs args = new SvnCheckOutArgs ();
			BindMonitor (args, monitor);
			args.Depth = recurse ? SvnDepth.Infinity : SvnDepth.Empty;
			lock (client)
				client.CheckOut (new SvnUriTarget (url, GetRevision (rev)), path);
		}

		public override void Commit (FilePath[] paths, string message, IProgressMonitor monitor)
		{
			SvnCommitArgs args = new SvnCommitArgs ();
			BindMonitor (args, monitor);
			args.LogMessage = message;
			lock (client) 
				client.Commit (paths.ToStringArray (), args);
		}

		public override void Delete (FilePath path, bool force, IProgressMonitor monitor)
		{
			SvnDeleteArgs args = new SvnDeleteArgs ();
			BindMonitor (args, monitor);
			args.Force = force;
			lock (client) 
				client.Delete (path, args);
		}

		public override string GetTextAtRevision (string repositoryPath, Revision revision)
		{
			MemoryStream ms = new MemoryStream ();
			lock (client) 
				client.Write (new SvnUriTarget (repositoryPath, GetRevision (revision)), ms);
			ms.Position = 0;
			using (StreamReader sr = new StreamReader (ms)) {
				return sr.ReadToEnd ();
			}
		}

		public override string GetVersion ( )
		{
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

		IEnumerable<DirectoryEntry> List (SvnTarget target, bool recurse)
		{
			List<DirectoryEntry> list = new List<DirectoryEntry> ();
			SvnListArgs args = new SvnListArgs ();
			args.Depth = recurse ? SvnDepth.Infinity : SvnDepth.Children;
			lock (client) 
				client.List (target, args, delegate (object o, SvnListEventArgs a) {
				if (string.IsNullOrEmpty (a.Path))
					return;
				DirectoryEntry de = new DirectoryEntry ();
				de.CreatedRevision = ToBaseRevision (a.Entry.Revision).Rev;
				de.HasProps = a.Entry.HasProperties;
				de.IsDirectory = a.Entry.NodeKind == SvnNodeKind.Directory;
				de.LastAuthor = a.Entry.Author;
				de.Name = a.Path;
				de.Size = a.Entry.FileSize;
				de.Time = a.Entry.Time;
				list.Add (de);
			});
			return list;
		}

		public override IEnumerable<SvnRevision> Log (Repository repo, FilePath path, SvnRevision revisionStart, SvnRevision revisionEnd)
		{
			List<SvnRevision> list = new List<SvnRevision> ();
			SvnLogArgs args = new SvnLogArgs ();
			args.Range = new SvnRevisionRange (GetRevision (revisionStart), GetRevision (revisionEnd));
			lock (client) 
				client.Log (path, args, delegate (object o, SvnLogEventArgs a) {
				List<RevisionPath> paths = new List<RevisionPath> ();
				foreach (SvnChangeItem item in a.ChangedPaths) {
					paths.Add (new RevisionPath (item.Path, ConvertRevisionAction (item.Action), ""));
				}
				SvnRevision r = new SvnRevision (repo, (int) a.Revision, a.Time, a.Author, a.LogMessage, paths.ToArray ());
				list.Add (r);
			});
			return list;
		}

		private RevisionAction ConvertRevisionAction (SvnChangeAction svnChangeAction)
		{
			switch (svnChangeAction) {
				case SvnChangeAction.Add: return RevisionAction.Add;
				case SvnChangeAction.Delete: return RevisionAction.Delete;
				case SvnChangeAction.Modify: return RevisionAction.Modify;
				case SvnChangeAction.Replace: return RevisionAction.Replace;
			}
			return RevisionAction.Other;
		}

		public override void Mkdir (string[] paths, string message, IProgressMonitor monitor)
		{
			SvnCreateDirectoryArgs args = new SvnCreateDirectoryArgs ();
			args.CreateParents = true;
			BindMonitor (args, monitor);
			List<Uri> uris = new List<Uri> ();
			foreach (string path in paths)
				uris.Add (new Uri (path));
			args.LogMessage = message;
			lock (client) 
				client.RemoteCreateDirectories (uris, args);
		}

		public override void Move (FilePath srcPath, FilePath destPath, SvnRevision rev, bool force, IProgressMonitor monitor)
		{
			SvnMoveArgs args = new SvnMoveArgs ();
			BindMonitor (args, monitor);
			args.Force = force;
			lock (client) 
				client.Move (srcPath, destPath, args);
		}

		public override string GetUnifiedDiff (FilePath path1, SvnRevision revision1, FilePath path2, SvnRevision revision2, bool recursive)
		{
			SvnPathTarget t1 = new SvnPathTarget (path1, GetRevision (revision1));
			SvnPathTarget t2 = new SvnPathTarget (path2, GetRevision (revision2));
			SvnDiffArgs args = new SvnDiffArgs ();
			args.Depth = recursive ? SvnDepth.Infinity : SvnDepth.Children;
			MemoryStream ms = new MemoryStream ();
			lock (client) 
				client.Diff (t1, t2, args, ms);
			ms.Position = 0;
			using (StreamReader sr = new StreamReader (ms)) {
				return sr.ReadToEnd ();
			}
		}

		public override void Resolve (FilePath path, bool recurse, IProgressMonitor monitor)
		{
			SvnResolveArgs args = new SvnResolveArgs ();
			BindMonitor (args, monitor);
			args.Depth = recurse ? SvnDepth.Infinity : SvnDepth.Children;
			lock (client) 
				client.Resolve (path, SvnAccept.MineFull, args);
		}

		public override void Revert (FilePath[] paths, bool recurse, IProgressMonitor monitor)
		{
			SvnRevertArgs args = new SvnRevertArgs ();
			BindMonitor (args, monitor);
			args.Depth = recurse ? SvnDepth.Infinity : SvnDepth.Children;
			lock (client) 
				client.Revert (paths.ToStringArray (), args);
		}

		public override void RevertRevision (FilePath path, Revision revision, IProgressMonitor monitor)
		{
			SvnMergeArgs args = new SvnMergeArgs ();
			BindMonitor (args, monitor);
			Revision prev = ((SvnRevision) revision).GetPrevious ();
			SvnRevisionRange range = new SvnRevisionRange (GetRevision (revision), GetRevision (prev));
			lock (client) 
				client.Merge (path, new SvnPathTarget (path), range, args);
		}

		public override void RevertToRevision (FilePath path, Revision revision, IProgressMonitor monitor)
		{
			SvnMergeArgs args = new SvnMergeArgs ();
			BindMonitor (args, monitor);
			SvnRevisionRange range = new SvnRevisionRange (GetRevision (SvnRevision.Head), GetRevision (revision));
			lock (client) 
				client.Merge (path, new SvnPathTarget (path), range, args);
		}

		public override IEnumerable<VersionInfo> Status (Repository repo, FilePath path, SvnRevision revision, bool descendDirs, bool changedItemsOnly, bool remoteStatus)
		{
			List<VersionInfo> list = new List<VersionInfo> ();
			SvnStatusArgs args = new SvnStatusArgs ();
			args.Revision = GetRevision (revision);
			args.Depth = descendDirs ? SvnDepth.Infinity : SvnDepth.Children;
			args.RetrieveAllEntries = !changedItemsOnly;
			args.RetrieveRemoteStatus = remoteStatus;
			lock (client) 
				client.Status (path, args, delegate (object o, SvnStatusEventArgs a) {
				list.Add (CreateVersionInfo (repo, a));
			});
			return list;
		}

		VersionInfo CreateVersionInfo (Repository repo, SvnStatusEventArgs ent)
		{
			VersionStatus rs = VersionStatus.Unversioned;
			Revision rr = null;

			if (ent.IsRemoteUpdated) {
				rs = ConvertStatus (SvnSchedule.Normal, ent.RemoteContentStatus);
				rr = new SvnRevision (repo, (int) ent.RemoteUpdateRevision, ent.RemoteUpdateCommitTime,
									  ent.RemoteUpdateCommitAuthor, "(unavailable)", null);
			}

			SvnSchedule sched = ent.WorkingCopyInfo != null ? ent.WorkingCopyInfo.Schedule : SvnSchedule.Normal;
			VersionStatus status = ConvertStatus (sched, ent.LocalContentStatus);

			bool readOnly = File.Exists (ent.FullPath) && (File.GetAttributes (ent.FullPath) & FileAttributes.ReadOnly) != 0;

			if (ent.WorkingCopyInfo != null) {
				if (ent.RemoteLock != null || ent.WorkingCopyInfo.LockToken != null) {
					status |= VersionStatus.LockRequired;
					if (ent.WorkingCopyInfo.LockToken != null || (ent.RemoteLock != null && ent.RemoteLock.Token != null))
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
				newRev = new SvnRevision (repo, (int) ent.WorkingCopyInfo.Revision);

			VersionInfo ret = new VersionInfo (ent.FullPath, repoPath, ent.NodeKind == SvnNodeKind.Directory,
											   status, newRev,
											   rs, rr);
			return ret;
		}

		private VersionStatus ConvertStatus (SvnSchedule schedule, SvnStatus status)
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

		public override void Lock (IProgressMonitor monitor, string comment, bool stealLock, params FilePath[] paths)
		{
			SvnLockArgs args = new SvnLockArgs ();
			BindMonitor (args, monitor);
			args.Comment = comment;
			args.StealLock = stealLock;
			lock (client) 
				client.Lock (paths.ToStringArray (), args);
		}

		public override void Unlock (IProgressMonitor monitor, bool breakLock, params FilePath[] paths)
		{
			SvnUnlockArgs args = new SvnUnlockArgs ();
			BindMonitor (args, monitor);
			args.BreakLock = breakLock;
			lock (client) 
				client.Unlock (paths.ToStringArray (), args);
		}

		public override void Update (FilePath path, bool recurse, IProgressMonitor monitor)
		{
			SvnUpdateArgs args = new SvnUpdateArgs ();
			BindMonitor (args, monitor);
			args.Depth = recurse ? SvnDepth.Infinity : SvnDepth.Children;
			client.Update (path, args);
		}

		SharpSvn.SvnRevision GetRevision (Revision rev)
		{
			if (rev == null)
				return null;
			SvnRevision srev = (SvnRevision) rev;
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

		SvnRevision ToBaseRevision (SharpSvn.SvnRevision rev)
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

		void BindMonitor (SvnClientArgs args, IProgressMonitor monitor)
		{
			NotifData data = new NotifData ();

			args.Notify += delegate (object o, SvnNotifyEventArgs e) {
				Notify (e, data, monitor);
			};
			args.Cancel += delegate (object o, SvnCancelEventArgs a) {
				a.Cancel = monitor.IsCancelRequested;
			};
			args.SvnError += delegate (object o, SvnErrorEventArgs a) {
				monitor.ReportError (a.Exception.Message, a.Exception.RootCause);
			};
		}

		void Notify (SvnNotifyEventArgs e, NotifData notifData, IProgressMonitor monitor)
		{
			string actiondesc;
			string file = e.Path;
			bool skipEol = false;
			bool notifyChange = false;

			switch (e.Action) {
				case SvnNotifyAction.Skip:
					if (e.ContentState == SvnNotifyState.Missing) {
						actiondesc = string.Format (GettextCatalog.GetString ("Skipped missing target: '{0}'"), file);
					}
					else {
						actiondesc = string.Format (GettextCatalog.GetString ("Skipped '{0}'"), file);
					}
					break;
				case SvnNotifyAction.UpdateDelete:
					actiondesc = string.Format (GettextCatalog.GetString ("Deleted   '{0}'"), file);
					break;

				case SvnNotifyAction.UpdateAdd:
					if (e.ContentState == SvnNotifyState.Conflicted) {
						actiondesc = string.Format (GettextCatalog.GetString ("Conflict {0}"), file);
					}
					else {
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
					}
					else {
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
					}
					else {
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
					}
					else {
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
					actiondesc = e.Action.ToString () + " " + file;
					break;
			}

			if (monitor != null) {
				if (skipEol)
					monitor.Log.Write (actiondesc);
				else
					monitor.Log.WriteLine (actiondesc);
			}

			if (notifyChange && File.Exists (file))
				FileService.NotifyFileChanged (file);
		}
	}
}
