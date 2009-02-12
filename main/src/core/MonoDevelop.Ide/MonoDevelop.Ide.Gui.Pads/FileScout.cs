// FileScout.cs
//
// Author:
//   John Luke  <john.luke@gmail.com>
//
// Copyright (c) 2004 John Luke
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

using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui.Pads
{
	internal class FileScout : Gtk.VPaned, IPadContent
	{
		PadFontChanger fontChanger;
		
		void IPadContent.Initialize (IPadWindow window)
		{
		}
		
		public string Id {
			get { return "MonoDevelop.Ide.Gui.Pads.FileScout"; }
		}
		
		public string DefaultPlacement {
			get { return "Left"; }
		}

		public Gtk.Widget Control {
			get {
				return this;
			}
		}
		
		public void RedrawContent()
		{
		}
		
		FileList filelister = new FileList ();
		FileBrowser fb = new FileBrowser ();

		public FileScout()
		{
			string path = IdeApp.ProjectOperations.ProjectsDefaultPath;

			if (Directory.Exists(path))
			{
				fb.CurrentDir = path;
			}

			fb.DirectoryChangedEvent += new DirectoryChangedEventHandler (OnDirChanged);
			filelister.RowActivated += new Gtk.RowActivatedHandler (FileSelected);
			IdeApp.Workspace.FirstWorkspaceItemOpened += OnCombineOpened;
			IdeApp.Workspace.LastWorkspaceItemClosed += OnCombineClosed;

			Gtk.ScrolledWindow listsw = new Gtk.ScrolledWindow ();
			listsw.Add (filelister);
			
			fontChanger = new PadFontChanger (listsw, delegate (Pango.FontDescription desc) {
				filelister.SetCustomFont (desc);
				fb.SetCustomFont (desc);
			}, delegate () {
				filelister.ColumnsAutosize ();
				fb.ColumnsAutosize ();
			});
			
			this.Pack1 (fb, true, true);
			this.Pack2 (listsw, true, true);

			fb.SelectFirst ();
			
			OnDirChanged (fb.CurrentDir);
			this.ShowAll ();
		}

		void OnDirChanged (string path) 
		{
			filelister.Clear ();

			bool ignoreHidden = !PropertyService.Get ("MonoDevelop.Core.Gui.FileScout.ShowHidden", false);
			fb.IgnoreHidden = ignoreHidden;

			foreach (string f in fb.Files)
			{
				try
				{
					if (System.IO.File.Exists(f))
					{
						if (!(System.IO.Path.GetFileName (f)).StartsWith ("."))
						{
							FileListItem it = new FileListItem (f);
							filelister.ItemAdded (it);
						}
						else
						{
							if (!ignoreHidden)
							{
								FileListItem it = new FileListItem (f);
								filelister.ItemAdded (it);
							
							}
						}
					}
				}
				catch (IOException) {} // Avoid crash on file existence check error
			}
		}

		void FileSelected (object sender, Gtk.RowActivatedArgs e)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;

			// we are not using SelectMultiple
			// nor can more than one be activated here
			if (filelister.Selection.GetSelected (out model, out iter))
			{
				FileListItem item = (FileListItem) filelister.Model.GetValue (iter, 3);

				//FIXME: use mimetypes not extensions
				// also change to Project tab when its a project
				if (Services.ProjectService.IsWorkspaceItemFile (item.FullName))
					IdeApp.Workspace.OpenWorkspaceItem (item.FullName);
				else
					IdeApp.Workbench.OpenDocument (item.FullName);
			}
		}

		void OnCombineOpened(object sender, WorkspaceItemEventArgs args)
		{
			try {
				Solution sol = args.Item as Solution;
				if (sol != null && sol.StartupItem != null)
					fb.CurrentDir = sol.StartupItem.BaseDirectory;
			} catch {
				fb.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			}
		}

		void OnCombineClosed(object sender, EventArgs args)
		{
			fb.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		}
		
		public override void Dispose ()
		{
			fontChanger.Dispose ();
			base.Dispose ();
		}

	}
}
