using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	public class ChangeSet
	{
		// Commits should be atomic and small. Therefore having a List instead
		// of a HashSet should be faster in most cases.
		readonly List<ChangeSetItem> items;
		readonly Repository repo;
		readonly FilePath basePath;
		Hashtable extendedProperties;

		internal protected ChangeSet (Repository repo, FilePath basePath) : this (repo, basePath, Enumerable.Empty<ChangeSetItem> ())
		{
		}

		ChangeSet (Repository repo, FilePath basePath, IEnumerable<ChangeSetItem> items)
		{
			this.repo = repo;
			this.items = items.ToList ();

			// Make sure the path has a trailing slash or the ChangeLogWriter's
			// call to GetDirectoryName will take us one extra directory up.
			string bp = basePath.ToString ();
			if (bp [bp.Length - 1] != System.IO.Path.DirectorySeparatorChar)
				basePath = bp + System.IO.Path.DirectorySeparatorChar;

			this.basePath = basePath;
			GlobalComment = "";
		}
		
		public IDictionary ExtendedProperties {
			get {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
			}
		}
		
		public string GlobalComment {
			get;
			set;
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
		
		public string GenerateGlobalComment (CommitMessageFormat format, MonoDevelop.Projects.AuthorInformation userInfo)
		{
			return GeneratePathComment (basePath.FullPath, items, format, userInfo);
		}
		
		public string GeneratePathComment (string path, IEnumerable<ChangeSetItem> items, 
			CommitMessageFormat messageFormat, MonoDevelop.Projects.AuthorInformation userInfo)
		{
			var writer = new ChangeLogWriter (path, userInfo);
			writer.MessageFormat = messageFormat;
			
			foreach (ChangeSetItem item in items) {
				writer.AddFile (item.Comment, item.LocalPath);
			}
			
			return writer.ToString ();
		}
		
		public FilePath BaseLocalPath {
			get { return basePath; }
		}
		
		public IReadOnlyList<ChangeSetItem> Items {
			get { return items; }
		}
		
		public Repository Repository {
			get { return repo; }
		}

		public bool ContainsFile (FilePath fileName)
		{
			return items.Any (item => item.LocalPath == fileName);
		}

		public ChangeSetItem AddFile (FilePath file)
		{
			return AddFile (repo.GetVersionInfo (file));
		}
		
		public ChangeSetItem AddFile (VersionInfo fileVersionInfo)
		{
			ChangeSetItem item = GetFileItem (fileVersionInfo.LocalPath);
			if (item != null)
				return item;

			item = new ChangeSetItem (fileVersionInfo);
			items.Add (item);
			return item;
		}
		
		public void AddFiles (VersionInfo[] fileVersionInfos)
		{
			foreach (VersionInfo vi in fileVersionInfos)
				AddFile (vi);
		}

		public ChangeSetItem GetFileItem (FilePath file)
		{
			return items.Find (it => it.LocalPath == file);
		}

		public void RemoveFile (FilePath file)
		{
			foreach (ChangeSetItem it in items) {
				if (it.LocalPath == file) {
					items.Remove (it);
					return;
				}
			}
		}
		
		public void RemoveItem (ChangeSetItem item)
		{
			items.Remove (item);
		}
		
		public ChangeSet Clone ()
		{
			return new ChangeSet (repo, basePath, items.Select (cit => cit.Clone ()));
		}
	}
	
	public sealed class ChangeSetItem
	{
		readonly VersionInfo versionInfo;
		
		internal ChangeSetItem (VersionInfo versionInfo)
		{
			this.versionInfo = versionInfo;
		}
		
		public string Comment {
			get { return VersionControlService.GetCommitComment (LocalPath) ?? String.Empty; }
			set { VersionControlService.SetCommitComment (LocalPath, value, true); }
		}
		
		public FilePath LocalPath {
			get { return versionInfo.LocalPath; }
		}
		
		public VersionStatus Status {
			get { return versionInfo.Status; }
		}
		
		public bool HasLocalChanges {
			get { return versionInfo.HasLocalChanges; }
		}
		
		public bool IsDirectory {
			get { return versionInfo.IsDirectory; }
		}
		
		public ChangeSetItem Clone ()
		{
			return new ChangeSetItem (versionInfo);
		}
	}
}
