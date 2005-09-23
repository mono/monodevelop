using System;
using System.Collections;
using System.IO;

using Gtk;

using VersionControl;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;

using Algorithm.Diff.Gtk;

namespace VersionControlPlugin {
	public class DiffView : BaseView {
		object left, right;
		Algorithm.Diff.Diff diff;
		HBox box = new HBox(true, 0);
		DiffWidget widget;
		ThreadNotify threadnotify;
		
		System.IO.FileSystemWatcher leftwatcher, rightwatcher;
		
		double pos = -1;
		
		public static bool Show(string filepath, bool test) {
			foreach (VersionControlSystem vc in VersionControlService.Providers) {
				if (vc.IsDiffAvailable(filepath)) {
					if (test) return true;
					DiffView d = new DiffView(
						Path.GetFileName(filepath),
						vc.GetPathToBaseText(filepath),
						filepath);
					MonoDevelop.Gui.WorkbenchSingleton.Workbench.ShowView(d, true);
					return true;
				}
			}
			return false;
		}
			
		static string[] split(string text) {
			if (text == "") return new string[0];
			return text.Split('\n', '\r');
		}
			
		public static void Show(string name, string lefttext, string righttext) {
			DiffView d = new DiffView(name, split(lefttext), split(righttext));
			MonoDevelop.Gui.WorkbenchSingleton.Workbench.ShowView(d, true);
		}
		
		public DiffView(string name, string left, string right) 
			: base(name + " Changes") {
			this.left = left;
			this.right = right;
			
			Refresh();
			
			threadnotify = new ThreadNotify(new ReadyEvent(Refresh));
			
			leftwatcher = new System.IO.FileSystemWatcher(Path.GetDirectoryName(left), Path.GetFileName(left));
			rightwatcher = new System.IO.FileSystemWatcher(Path.GetDirectoryName(right), Path.GetFileName(right));
			
			leftwatcher.Changed += new FileSystemEventHandler(filechanged);
			rightwatcher.Changed += new FileSystemEventHandler(filechanged);
			
			leftwatcher.EnableRaisingEvents = true;
			rightwatcher.EnableRaisingEvents = true;
		}
		
		public DiffView(string name, string[] left, string[] right) 
			: base(name + " Changes") {
			this.left = left;
			this.right = right;
			
			Refresh();
		}
		
		void filechanged(object src, FileSystemEventArgs args) {
			threadnotify.WakeupMain();			
		}
		
		private void Refresh() {
			box.Show();
			
			try {
				if (left is string)
					diff = new Algorithm.Diff.Diff((string)left, (string)right, false, true);
				else if (left is string[])
					diff = new Algorithm.Diff.Diff((string[])left, (string[])right, null, null);
			} catch (Exception e) {
				Console.Error.WriteLine(e.ToString());
				return;
			} 
			
			if (widget != null) {
				pos = widget.Position;
				box.Remove(widget);
				widget.Dispose();
			}
						
			DiffWidget.Options opts = new DiffWidget.Options();
			opts.Font = MonoDevelop.EditorBindings.Properties.TextEditorProperties.Font.ToString();
			opts.LeftName = "Repository";
			opts.RightName = "Working Copy";
			widget = new DiffWidget(diff, opts);
			
			box.Add(widget);
			box.ShowAll();
			
			widget.ExposeEvent += new ExposeEventHandler(OnExposed);
		}

		void OnExposed (object o, ExposeEventArgs args) {
			if (pos != -1)
				widget.Position = pos;
			pos = -1;
		}		
		
		protected override void SaveAs(string fileName) {
			if (!(left is string)) return;
		
			using (StreamWriter writer = new StreamWriter(fileName)) {
				Algorithm.Diff.UnifiedDiff.WriteUnifiedDiff(
					diff,
					writer,
					Path.GetFileName((string)right) + "    (repository)",
					Path.GetFileName((string)right) + "    (working copy)",
					3);
			}
		}
			
		public override Gtk.Widget Control { 
			get {
				return box;
			}
		}
	}
}
