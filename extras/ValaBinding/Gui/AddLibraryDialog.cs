//
// AddLibraryDialog.cs: A simple open file dialog to add libraries to the project (using vapi files)
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//


using System;
using System.IO;

using Mono.Addins;

namespace MonoDevelop.ValaBinding
{
	public partial class AddLibraryDialog : Gtk.Dialog
	{
		private string lib = string.Empty;
		
		public AddLibraryDialog()
		{
			this.Build();
			
			Gtk.FileFilter libs = new Gtk.FileFilter ();
			Gtk.FileFilter all = new Gtk.FileFilter ();
			
			libs.AddPattern ("*.vapi");
			libs.Name = "Libraries";
			
			all.AddPattern ("*.*");
			all.Name = "All Files";
			
			file_chooser_widget.AddFilter (libs);
			file_chooser_widget.AddFilter (all);
			file_chooser_widget.SetCurrentFolder ("/usr/share/vala/vapi");
		}
		
		private void OnOkButtonClick (object sender, EventArgs e)
		{
			lib = System.IO.Path.GetFileNameWithoutExtension (
				file_chooser_widget.Filename);
			
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
