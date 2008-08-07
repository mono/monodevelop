// FolderEntry.cs
//
// Author:
//   Ben Maurer  <bmaurer@users.sourceforge.net>
//   Todd Berman  <tberman@off.net>
//
// Copyright (c) 2004 Ben Maurer
// Copyright (c) 2004 Todd Berman
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
using Gtk;
using Gdk;

namespace MonoDevelop.Components {
	[System.ComponentModel.Category("MonoDevelop.Components")]
	[System.ComponentModel.ToolboxItem(true)]
	public class FolderEntry : BaseFileEntry
	{
		public FolderEntry () : base ("")
		{
		}
		
		public FolderEntry (string name) : base (name)
		{
		}
		
		protected override string ShowBrowseDialog (string name, string start_in)
		{
			FolderDialog fd = new FolderDialog (name);
			if (start_in != null)
				fd.SetFilename (start_in);
			
			int response = fd.Run ();
			
			if (response == (int) ResponseType.Ok) {
				fd.Hide ();
				return fd.Filename;
			}
			fd.Hide ();
			
			return null;
		}
	}
}
