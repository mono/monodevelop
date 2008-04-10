// FileListItem.cs
//
// Author:
//   John Luke  <john.luke@gmail.com>
//
// Copyright (c) 2007 John Luke
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
//
//

using System;
using System.IO;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Pads
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
			icon = IdeApp.Services.PlatformService.GetPixbufForFile (FullName, Gtk.IconSize.Menu);
		}

		public FileListItem (string name)
		{
			FileInfo fi = new FileInfo (name);
			this.size = Math.Round ((double) fi.Length / 1024).ToString () + " KB";
			this.lastModified = fi.LastWriteTime.ToString ();
			FullName = System.IO.Path.GetFullPath (name); 
			icon = IdeApp.Services.PlatformService.GetPixbufForFile (FullName, Gtk.IconSize.Menu);
		}
	}
}

