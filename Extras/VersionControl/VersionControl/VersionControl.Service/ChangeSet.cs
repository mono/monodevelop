
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace VersionControl.Service
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
		
		public string GenerateGlobalComment (int maxColumns)
		{
			ArrayList comms = new ArrayList ();
			foreach (ChangeSetItem it in items) {
				bool found = false;
				string relPath = it.LocalPath.Substring (basePath.Length + 1);
				if (it.Comment.Length > 0) {
					foreach (object[] com in comms) {
						if (((string)com[0]) == it.Comment) {
							com[1] = ((string)com[1]) + ", " + relPath;
							found = true;
							break;
						}
					}
					if (!found)
						comms.Add (new object[] { it.Comment, relPath });
				}
			}
			StringBuilder message = new StringBuilder ();
			foreach (object[] com in comms) {
				string msg = (string) com[1] + ": " + (string) com[0];
				if (message.Length > 0)
					message.Append ('\n');
				message.Append ("* " + FormatText (msg, 0, 2, maxColumns));
			}
			return message.ToString ();
		}
		
		static string FormatText (string text, int initialLeftMargin, int leftMargin, int maxCols)
		{
			int n = 0;
			int margin = initialLeftMargin;
			
			if (text == "")
				return "";
			
			StringBuilder outs = new StringBuilder ();
			while (n < text.Length)
			{
				int col = margin;
				int lastWhite = -1;
				int sn = n;
				while ((col < maxCols || lastWhite==-1) && n < text.Length) {
					if (char.IsWhiteSpace (text[n]))
						lastWhite = n;
					if (text[n] == '\n') {
						lastWhite = n;
						n++;
						break;
					}
					col++;
					n++;
				}
				
				if (lastWhite == -1 || col < maxCols)
					lastWhite = n;
				else if (col >= maxCols)
					n = lastWhite + 1;
				
				if (outs.Length > 0) outs.Append ('\n');
				
				outs.Append (new String (' ', margin) + text.Substring (sn, lastWhite - sn));
				margin = leftMargin;
			}
			return outs.ToString ();
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
	}
	
	public class ChangeSetItem
	{
		VersionInfo versionInfo;
		string comment = string.Empty;
		
		internal ChangeSetItem (VersionInfo versionInfo)
		{
			this.versionInfo = versionInfo;
		}
		
		public string Comment {
			get { return comment; }
			set { comment = value; }
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
	}
}
