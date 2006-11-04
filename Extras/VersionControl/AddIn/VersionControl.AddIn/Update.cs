using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;

using VersionControl.Service;

namespace VersionControl.AddIn
{
	public class UpdateCommand
	{
		public static bool Update (Repository vc, string path, bool test)
		{
			if (vc.IsVersioned (path)) {
				if (test) return true;
				new UpdateWorker(vc, path).Start();
				return true;
			}
			return false;
		}

		private class UpdateWorker : Task {
			Repository vc;
			string path;
						
			public UpdateWorker(Repository vc, string path) {
				this.vc = vc;
				this.path = path;
			}
			
			protected override string GetDescription() {
				return "Updating " + path + "...";
			}
			
			protected override void Run() {
				vc.Update(path, true, GetProgressMonitor ());
			}
		}
		
	}

}
