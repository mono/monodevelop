//
// PackageDetails.cs
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

namespace MonoDevelop.ValaBinding
{
	public partial class PackageDetails : Gtk.Dialog
	{
		Gtk.ListStore requiresStore = new Gtk.ListStore (typeof(string));
//		Gtk.ListStore libPathsStore = new Gtk.ListStore (typeof(string));
//		Gtk.ListStore libsStore = new Gtk.ListStore (typeof(string));
//		Gtk.ListStore cflagsStore = new Gtk.ListStore (typeof(string));
		
		public PackageDetails (ProjectPackage package)
		{
			this.Build();
					
			Gtk.CellRendererText textRenderer = new Gtk.CellRendererText ();
			
			requiresTreeView.Model = requiresStore;
			requiresTreeView.AppendColumn ("Requires", textRenderer, "text", 0);
			requiresTreeView.HeadersVisible = false;
			
//			libPathsTreeView.Model = libPathsStore;
//			libPathsTreeView.AppendColumn ("LibPaths", textRenderer, "text", 0);
//			libPathsTreeView.HeadersVisible = false;
//			
//			libsTreeView.Model = libsStore;
//			libsTreeView.AppendColumn ("Libs", textRenderer, "text", 0);
//			libsTreeView.HeadersVisible = false;
//			
//			cflagsTreeView.Model = cflagsStore;
//			cflagsTreeView.AppendColumn ("CFlags", textRenderer, "text", 0);
//			cflagsTreeView.HeadersVisible = false;
//			
			nameLabel.Text = package.Name;
			
			descriptionLabel.Text = package.Description;
			versionLabel.Text = package.Version;
			
			foreach (string req in package.Requires)
				requiresStore.AppendValues (req);
			
//			foreach (string libpath in package.LibPaths)
//				libPathsStore.AppendValues (libpath);
//			
//			foreach (string lib in package.Libs)
//				libsStore.AppendValues (lib);
//			
//			foreach (string cflag in package.CFlags)
//				cflagsStore.AppendValues (cflag);
		}

		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			Destroy ();
		}
	}
}
