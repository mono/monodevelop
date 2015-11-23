//
// FileChooserAction.cs
//
// Author:
//       therzok <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 therzok
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

namespace MonoDevelop.Components
{
	[Flags]
	public enum FileChooserAction
	{
		Open = 0x1,
		Save = 0x2,
		SelectFolder = 0x4,
		CreateFolder = 0x8,

		FolderFlags = CreateFolder | SelectFolder,
		FileFlags = Open | Save,
	}

	static class FileChooserActionExtensions
	{
		public static Gtk.FileChooserAction ToGtkAction(this FileChooserAction action)
		{
			if ((action & FileChooserAction.CreateFolder) != 0)
				return Gtk.FileChooserAction.CreateFolder;
			else if ((action & FileChooserAction.SelectFolder) != 0)
				return Gtk.FileChooserAction.SelectFolder;
			else if ((action & FileChooserAction.Open) != 0)
				return Gtk.FileChooserAction.Open;
			else if ((action & FileChooserAction.Save) != 0)
				return Gtk.FileChooserAction.Save;
			else
				throw new NotSupportedException ();

		}
	}
}

