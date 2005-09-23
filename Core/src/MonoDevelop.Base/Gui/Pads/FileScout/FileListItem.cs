using System;
using System.IO;

using MonoDevelop.Gui.Utils;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;

namespace MonoDevelop.Gui.Pads
{
	internal class FileListItem
	{
		string fullname;
		string text;
		string size;
		string lastModified;
		Gdk.Pixbuf icon;
			
		public string FullName {
			get {
				return fullname;
			} 
			set {
				fullname = System.IO.Path.GetFullPath(value);
				text = System.IO.Path.GetFileName(fullname);
			}
		}
			
		public string Text {
			get {
				return text;
			}
		}
			
		public string Size {
			get {
				return size;
			}
			set {
				size = value;
			}
		}
			
		public string LastModified {
			get {
				return lastModified;
			}
			set {
				lastModified = value;
			}
		}

		public Gdk.Pixbuf Icon {
			get {
				return icon;
			}
			set {
				icon = value;
			}
		}
			
		public FileListItem(string fullname, string size, string lastModified) 
		{
			this.size = size;
			this.lastModified = lastModified;
			//FIXME: This is because //home/blah is not the same as /home/blah according to Icon.LookupSync, if we get weird behaviours, lets look at this again, see if we still need it.
			FullName = fullname.Substring (1);
			icon = FileIconLoader.GetPixbufForFile (FullName, 24);
		}

		public FileListItem (string name)
		{
			FileInfo fi = new FileInfo (name);
			this.size = Math.Round ((double) fi.Length / 1024).ToString () + " KB";
			this.lastModified = fi.LastWriteTime.ToString ();
			FullName = System.IO.Path.GetFullPath (name); 
			icon = FileIconLoader.GetPixbufForFile (FullName, 24);
		}
	}
}

