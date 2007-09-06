//
// AddLibraryDialog.cs: A simple open file dialog to add libraries to the prject (*.a files)
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

using Mono.Addins;

namespace CBinding
{
	public partial class AddLibraryDialog : Gtk.Dialog
	{
		private string lib = string.Empty;
		
		public AddLibraryDialog()
		{
			this.Build();
			
			Gtk.FileFilter libs = new Gtk.FileFilter ();
			Gtk.FileFilter all = new Gtk.FileFilter ();
			
			libs.AddPattern ("*.a");
			libs.Name = "Library";
			
			all.AddPattern ("*.*");
			all.Name = "All Files";
			
			file_chooser_widget.AddFilter (libs);
			file_chooser_widget.AddFilter (all);
			file_chooser_widget.SetCurrentFolder ("/usr/lib");
		}
		
		private void OnOkButtonClick (object sender, EventArgs e)
		{
			lib = System.IO.Path.GetFileNameWithoutExtension (
				file_chooser_widget.Filename);
			
			if (lib.StartsWith ("lib"))
				lib = lib.Remove (0, 3);
			
			Destroy ();
		}
		
		private void OnCancelButtonClick (object sender, EventArgs e)
		{
			Destroy ();
		}
		
		public string Library {
			get { return lib; }
		}
	}
}
