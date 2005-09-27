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

using VersionControl;

namespace VersionControlPlugin {
	public class UpdateCommand {
		public static bool Update(string path, bool test) {
			foreach (VersionControlSystem vc in VersionControlService.Providers) {
				if (vc.CanUpdate(path)) {
					if (test) return true;
					new UpdateWorker(vc, path).Start();
					return true;
				}
			}
			return false;
		}

		private class UpdateWorker : Task {
			VersionControlSystem vc;
			string path;
						
			public UpdateWorker(VersionControlSystem vc, string path) {
				this.vc = vc;
				this.path = path;
			}
			
			protected override string GetDescription() {
				return "Updating " + path + "...";
			}
			
			protected override void Run() {
				vc.Update(path, true, new UpdateCallback(Callback));
			}
		
			protected override void Finished() {
			}
			
			private void Callback(string path, string action) {
				Log(action + "\t" + path);
			}
		}
		
	}

}
