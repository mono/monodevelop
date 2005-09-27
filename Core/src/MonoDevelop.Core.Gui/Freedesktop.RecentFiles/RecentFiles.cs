/*
Copyright (c) 2004 John Luke

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

// implementation of the freedesktop.org Recent Files spec
// http://freedesktop.org/Standards/recent-file-spec/recent-file-spec-0.2.html

// Note: the file may not exist, apps should account for that themselves

// Lets keep things sorted as we go, and reverse before saving it.

namespace Freedesktop.RecentFiles
{
	// Currently using XmlSerializer to read/write the recent files list
	// other methods may be faster.
    public class RecentFiles
	{
		private static XmlSerializer serializer;

		// expose this so consumers can watch it with FileSystemWatcher for changes
		public static readonly string RecentFileStore = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".recently-used");

		static RecentFiles ()
		{
			serializer = new XmlSerializer (typeof (RecentFiles));
		}

		// required by serializer
		public RecentFiles ()
		{
		}

		// currently only emits on our changes
		// we should probably use FSW and send those events here
		// for now you have to do it on your own
		public event EventHandler Changed;

        [XmlElement ("RecentItem")]
        public RecentItem[] RecentItems;

		public void AddItem (RecentItem item)
		{
			if (RecentItems == null)
			{
				RecentItems = new RecentItem [] {item};
				Save ();
				return;
			}

			// check for already existing URI
			// if it does update timestamp and return unchanged;
			foreach (RecentItem ri in RecentItems)
			{
				if (ri.Uri == item.Uri)
				{
					ri.Timestamp = item.Timestamp;
					if (item.Groups != null)
						ri.AddGroups (item.Groups);
					Save ();
					return;
				}
			}

			while (RecentItems.Length > 499)
			{
				RemoveItem (OldestItem);
			}

			int length = RecentItems.Length;
			RecentItem[] newItems = new RecentItem[length + 1];
			RecentItems.CopyTo (newItems, 0);
			newItems[length] = item;
			RecentItems = newItems;
			Save ();
		}

		// ensure that that only max items are kept in group 
		public void AddWithLimit (RecentItem item, string group, int max)
		{
			if (max < 1)
				throw new ArgumentException ("max must be > 0");

			// we add it first in case the Uri is already there
			AddItem (item);

			// then we adjust for the limit
			RecentItem[] inGroup = GetItemsInGroup (group);
			if (inGroup.Length > max)
			{
				while (inGroup.Length > max) {
					RemoveItem (GetOldestItem (inGroup));
					inGroup = GetItemsInGroup (group);
				}
			}
		}

		public void Clear ()
		{
			RecentItems = null;
			Save ();
		}

		public void ClearGroup (string group)
		{
			if (RecentItems == null)
				return;

			ArrayList list = new ArrayList ();
			foreach (RecentItem ri in RecentItems)
			{
				if (Array.IndexOf (ri.Groups, group) == -1)
				{
					list.Add (ri);
				}
				else
				{
					ri.RemoveGroup (group);

					// it has other groups so dont delete it
					if (ri.Groups.Length > 0)
						list.Add (ri);
				}
			}

			RecentItem[] items = new RecentItem [list.Count];
			list.CopyTo (items, 0);
			RecentItems = items;
			Save ();
		}

		private void EmitChangedEvent ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);	
		}

		public static RecentFiles GetInstance ()
		{
			try
			{
				XmlTextReader reader = new XmlTextReader (RecentFileStore);
				RecentFiles rf = (RecentFiles) serializer.Deserialize (reader);
				reader.Close ();
				rf.Sort ();
				return rf;
			}
			catch (IOException e)
			{
				// FIXME: this is wrong, because if we later save it, we blow away what was there
				// somehow we should ask for the lock or wait for it...
				return new RecentFiles ();
			}
			catch
			{
				// FIXME: this is wrong, because if we later save it, we blow away what was there
				return new RecentFiles ();
			}
		}

		public RecentItem[] GetItemsInGroup (string group)
		{
			if (RecentItems == null)
				return null;

			ArrayList list = new ArrayList ();
			foreach (RecentItem ri in RecentItems)
			{
				if (ri.Groups == null)
					continue;

				if (Array.IndexOf (ri.Groups, group) != -1)
					list.Add (ri);
			}

			RecentItem[] items = new RecentItem [list.Count];
			list.CopyTo (items, 0);
			return items;
		}

		public RecentItem[] GetMostRecentInGroup (int count, string group)
		{
			if (count < 1)
				throw new ArgumentException ("count must be > 0");

			RecentItem[] inGroup = GetItemsInGroup (group);
			return GetMostRecent (count, inGroup);
		}

		public RecentItem[] GetMostRecent (int count)
		{
			if (count < 1)
				throw new ArgumentException ("count must be > 0");

			return GetMostRecent (count, RecentItems);
		}

		// return the last X items in newest to oldest order
		private RecentItem[] GetMostRecent (int count, RecentItem[] items)
		{
			if (count >= items.Length)
			{
				return items;
			}
			else
			{
				RecentItem[] countedItems = new RecentItem[count];
				// get the last count items
				Array.Copy (items, items.Length - count - 1, countedItems, 0, count);

				return countedItems;
			}
		}

		public static RecentItem GetOldestItem (RecentItem[] items)
		{
			return items[items.Length - 1];
		}

		public RecentItem OldestItem
		{
			get {
				return RecentItems[0];
			}
		}

		public void RemoveItem (RecentItem item)
		{
			if (RecentItems == null)
				return;

			ArrayList l = new ArrayList ();
			foreach (RecentItem ri in RecentItems)
			{
				if (ri == item)
				{
					// remove the whole thing
				}
				else if (ri.Uri == item.Uri)
				{
					if (ri.Groups != null)
					{
						// remove the groups
						if (item.Groups != null)
						{
							foreach (string g in item.Groups)
								ri.RemoveGroup (g);
						}
						l.Add (ri);
					}
				}
				else
				{
					// keep it
					l.Add (ri);
				}
			}

			RecentItem[] items = new RecentItem [l.Count];
			l.CopyTo (items, 0);
			RecentItems = items;
			Save ();
		}

		public void RemoveItem (Uri uri)
		{
			if (RecentItems == null)
				return;

			foreach (RecentItem ri in RecentItems)
			{
				if (ri.Uri == uri.ToString ())
					RemoveItem (ri);
			}
		}

		public void RenameItem (Uri oldUri, Uri newUri)
		{
			// FIXME: throw exception, cant rename non-existant item
			if (RecentItems == null)
				return;

			foreach (RecentItem ri in RecentItems)
			{
				if (ri.Uri == oldUri.ToString ())
				{
					ri.Uri = newUri.ToString ();
					ri.Timestamp = RecentItem.NewTimestamp;
					Save ();
					return;
				}
			}
		}

		// FIXME: only append new items, instead of re-writing
		// Save implies EmitChangedEvent (otherwise why would we save?)
		private void Save ()
		{
			// make sure we are in order
			this.Sort ();
			// but we need to write in oldest-to-newest order
			if (RecentItems != null)
				Array.Reverse (RecentItems);

			// if we specifically set Encoding UTF 8 here it writes the BOM
			// which confuses others (egg-recent-files) I guess
			try {
				XmlTextWriter writer = new XmlTextWriter (new StreamWriter (RecentFileStore));
				writer.Formatting = Formatting.Indented;
				serializer.Serialize (writer, this);
				writer.Close ();
			}
			catch {
				Console.WriteLine ("WARNING: cannot write to ~/.recently-used");
			}
			EmitChangedEvent ();

			// back to normal
			this.Sort ();
		}

		// this gives us the items in newest-to-oldest order
		public void Sort ()
		{
			if (RecentItems != null)
				Array.Sort (RecentItems);
		}

		public override string ToString ()
		{
			if (RecentItems == null)
				return "0 recent files";

			StringBuilder sb = new StringBuilder ();
			foreach (RecentItem ri in this.RecentItems)
			{
				sb.Append (ri.Uri);
				sb.Append (" ");
				sb.Append (ri.MimeType);
				sb.Append (" ");
				sb.Append (ri.Timestamp);
				sb.Append ("\n");
			}
			sb.Append (RecentItems.Length);
			sb.Append (" total recent files\n");
			return sb.ToString ();
		}
    }

    public class RecentItem : IComparable
	{
        [XmlElement ("URI")]
        public string Uri;

        [XmlElement ("Mime-Type")]
        public string MimeType;

        public int Timestamp;

        public string Private;

        [System.Xml.Serialization.XmlArrayItem(ElementName="Group",IsNullable=false)]
        public string[] Groups;

		// required by serialization
		public RecentItem ()
		{
		}

		public RecentItem (Uri uri, string mimetype) : this (uri, mimetype, null)
		{
		}

		public RecentItem (Uri uri, string mimetype, string group)
		{
			Uri = uri.ToString ();
			MimeType = mimetype;
			Timestamp = NewTimestamp;

			if (group != null)
			{
				this.Groups = new string[] {group};
			}
		}

		private void AddGroup (string group)
		{
			if (this.Groups == null)
			{
				Groups = new string[] {group};
				return;
			}

			// if it already has this group no need to add it
			foreach (string g in Groups)
			{
				if (g == group)
					return;
			}

			int length = this.Groups.Length;
			string[] groups = new string [length + 1];
			this.Groups.CopyTo (groups, 0);
			groups[length] = group;
			this.Groups = groups;
		}

		public void AddGroups (string[] groups)
		{
			if (this.Groups == null)
			{
				Groups = groups;
				return;
			}

			foreach (string s in groups)
				AddGroup (s);
		}

		// we want newer items first
		public int CompareTo (object item)
		{
			RecentItem other = item as RecentItem;
			if (other == null)
				throw new ArgumentException ("item is not of type " + typeof (RecentItem));

			if (this.Timestamp == other.Timestamp)
				return 0;
			else if (this.Timestamp < other.Timestamp)
				return 1; // older
			else
				return -1; // newer
		}

		public static int NewTimestamp
		{
			get {
				// from the unix epoch
				return ((int) (DateTime.UtcNow - new DateTime (1970, 1, 1, 0, 0, 0, 0)).TotalSeconds);
			}
		}

		public void RemoveGroup (string group)
		{
			if (Groups == null)
				return;

			ArrayList groups = new ArrayList ();
			if (Array.IndexOf (Groups, group) == -1)
			{
				return; // doesnt have it
			}
			else
			{
				foreach (string g in Groups)
				{
					if (g != group)
						groups.Add (g);
				}
			}

			string[] newGroups = new string [groups.Count];
			groups.CopyTo (newGroups, 0);
			this.Groups = newGroups;
		}

		// some apps might depend on this, even though they shouldn't
		public override string ToString ()
		{
			return this.Uri;
		}
    }
}

