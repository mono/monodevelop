
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using System.Linq;

namespace MonoDevelop.VersionControl
{
	public class ChangeSet
	{
		string globalComment = string.Empty;
		List<ChangeSetItem> items = new List<ChangeSetItem> ();
		Repository repo;
		string basePath;
		
		internal protected ChangeSet (Repository repo, string basePath)
		{
			this.repo = repo;
			this.basePath = basePath;
		}
		
		public bool IsEmpty {
			get { return items.Count == 0; }
		}

		
		public int Count {
			get { return items.Count; }
		}
		
		public int CommentsCount {
			get { return items.Count (item => !string.IsNullOrEmpty (item.Comment)); }
		}
		
		public string GenerateGlobalComment (CommitMessageFormat format)
		{
			return GeneratePathComment (basePath, items, format, null);
		}
		
		public string GeneratePathComment (string path, IEnumerable<ChangeSetItem> items, 
			CommitMessageFormat messageFormat, MonoDevelop.Ide.Gui.UserInformation userInfo)
		{
			ChangeLogWriter writer = new ChangeLogWriter (path, userInfo);
			writer.MessageFormat = messageFormat;
			
			foreach (ChangeSetItem item in items) {
				writer.AddFile (item.Comment, item.LocalPath);
			}
			
			return writer.ToString ();
		}
		
		public string GlobalComment {
			get { return globalComment; }
			set { globalComment = value; }
		}
		
		public string BaseLocalPath {
			get { return basePath; }
		}
		
		public IEnumerable<ChangeSetItem> Items {
			get { return items; }
		}
		
		public Repository Repository {
			get { return repo; }
		}
		
		public bool ContainsFile (string fileName)
		{
			for (int n=0; n<items.Count; n++)
				if (items [n].LocalPath == fileName)
					return true;
			return false;
		}
		
		public ChangeSetItem AddFile (string file)
		{
			return AddFile (repo.GetVersionInfo (file, false));
		}
		
		public ChangeSetItem AddFile (VersionInfo fileVersionInfo)
		{
			ChangeSetItem item = new ChangeSetItem (fileVersionInfo);
			items.Add (item);
			return item;
		}
		
		public void AddFiles (VersionInfo[] fileVersionInfos)
		{
			foreach (VersionInfo vi in fileVersionInfos)
				AddFile (vi);
		}
		
		public ChangeSetItem GetFileItem (string file)
		{
			foreach (ChangeSetItem it in items)
				if (it.LocalPath == file)
					return it;
			return null;
		}

		public void RemoveFile (string file)
		{
			foreach (ChangeSetItem it in items) {
				if (it.LocalPath == file) {
					items.Remove (it);
					return;
				}
			}
		}
		
		public ChangeSet Clone ()
		{
			ChangeSet cs = (ChangeSet) MemberwiseClone ();
			cs.CopyFrom (this);
			return cs;
		}
		
		public virtual void CopyFrom (ChangeSet other)
		{
			globalComment = other.globalComment;
			repo = other.repo;
			basePath = other.basePath;
			items = new List<ChangeSetItem> ();
			foreach (ChangeSetItem cit in other.items)
				items.Add (cit.Clone ());
		}
	}
	
	public class ChangeSetItem
	{
		VersionInfo versionInfo;
		
		internal ChangeSetItem (VersionInfo versionInfo)
		{
			this.versionInfo = versionInfo;
		}
		
		public string Comment {
			get {
				string txt = VersionControlService.GetCommitComment (LocalPath);
				return txt != null ? txt : String.Empty;
			}
			set { VersionControlService.SetCommitComment (LocalPath, value, true); }
		}
		
		public string LocalPath {
			get { return versionInfo.LocalPath; }
		}
		
		public VersionStatus Status {
			get { return versionInfo.Status; }
		}
		
		public bool IsDirectory {
			get { return versionInfo.IsDirectory; }
		}
		
		public ChangeSetItem Clone ()
		{
			ChangeSetItem cs = (ChangeSetItem) MemberwiseClone ();
			cs.CopyFrom (this);
			return cs;
		}
		
		public virtual void CopyFrom (ChangeSetItem other)
		{
			versionInfo = other.versionInfo;
		}
	}
}
