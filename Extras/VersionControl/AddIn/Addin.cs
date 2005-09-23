using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Gui;
using MonoDevelop.Core;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Gui.Pads.ProjectPad;
using MonoDevelop.Internal.Project;
using MonoDevelop.Commands;
using MonoDevelop.Services;

using VersionControl;

namespace VersionControlPlugin {

	public class VersionControlService  {
	
		public static ArrayList Providers = new ArrayList();
	
		static Gdk.Pixbuf overlay_normal = Gdk.Pixbuf.LoadFromResource("overlay_normal.png");
		static Gdk.Pixbuf overlay_modified = Gdk.Pixbuf.LoadFromResource("overlay_modified.png");
		static Gdk.Pixbuf overlay_conflicted = Gdk.Pixbuf.LoadFromResource("overlay_conflicted.png");
		static Gdk.Pixbuf overlay_added = Gdk.Pixbuf.LoadFromResource("overlay_added.png");

		public static Gdk.Pixbuf LoadIconForStatus(NodeStatus status) {
			if (status == NodeStatus.Unchanged)
				return overlay_normal;
			if (status == NodeStatus.Modified)
				return overlay_modified;
			if (status == NodeStatus.Conflicted)
				return overlay_conflicted;
			if (status == NodeStatus.ScheduledAdd)
				return overlay_added;
			return null;
		}
		
		static VersionControlService() {
			Providers.Add(new SubversionVersionControl());
		}
	}
	
	public class VersionControlNodeExtension : NodeBuilderExtension {
		Hashtable projectsWatched = new Hashtable();
		Hashtable fileStatus = new Hashtable();
	
		public override bool CanBuildNode (Type dataType) {
			//Console.Error.WriteLine(dataType);
			return typeof(ProjectFile).IsAssignableFrom (dataType)
				|| typeof(DotNetProject).IsAssignableFrom (dataType)
				|| typeof(ProjectFolder).IsAssignableFrom (dataType)
				|| typeof(Combine).IsAssignableFrom (dataType);
		}		
		
		public override void BuildNode (ITreeBuilder builder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon) {
			// Add status overlays
			
			if (!(dataObject is ProjectFile)) return;
			if (!builder.Options["ShowVersionControlOverlays"])
				return;
		
			ProjectFile file = (ProjectFile) dataObject;
			
			// TODO: Monitor changes not just to project files
			// but also to .svn directories to catch commits
			// and updates.
			
			WatchProject(file.Project);
			
			// When a file had a status and later has no status,
			// for whatever reason, it needs to be removed from the hashtable.
			fileStatus.Remove(file.FilePath);
			
			try {
				NodeStatus status = GetStatus(file.FilePath);
				if (status == NodeStatus.Unknown) return;
				
				fileStatus[file.FilePath] = status;
				
				Gdk.Pixbuf overlay = VersionControlService.LoadIconForStatus(status);
				
				double scale = (double)(2*icon.Width/3) / (double)overlay.Width;
				int w = (int)(overlay.Width*scale);
				int h = (int)(overlay.Height*scale);
				icon = icon.Copy();
				overlay.Composite(icon,
					icon.Width-w,  icon.Height-h,
					w, h,
					icon.Width-w, icon.Height-h,
					scale,scale, Gdk.InterpType.Bilinear, 255); 
			} catch (Exception e) {
			}
		}
		
		NodeStatus GetStatus(string filepath) {
			foreach (VersionControlSystem vc in VersionControlService.Providers) {
				if (vc.IsFileStatusAvailable(filepath)) {
					Node node = vc.GetFileStatus(filepath, false);
					return node.Status;
				}
			}
			return NodeStatus.Unknown;
		}
		
		void WatchProject(Project project) {
			if (projectsWatched.ContainsKey(project)) return;
			projectsWatched[project] = projectsWatched;
			project.FileChangedInProject += new ProjectFileEventHandler(Monitor);
		}
		
		void Monitor(object sender, ProjectFileEventArgs args) {
			// If the status of the file actually changed, then
			// update the project pad so the overlays are updated.
			
			string file = args.ProjectFile.FilePath;
			
			NodeStatus newstatus = GetStatus(file);
			if (newstatus == NodeStatus.Unknown && !fileStatus.ContainsKey(file))
				return; // had no status before, has no status now

			if (!fileStatus.ContainsKey(file)
				|| (fileStatus.ContainsKey(file) 
					&& (NodeStatus)fileStatus[file] != newstatus)) {
				// No status before and has status now, or
				// status changed.  Refresh the project pad.
				ITreeBuilder builder = Context.GetTreeBuilder(args.ProjectFile);
				if (builder != null)
					builder.UpdateAll();
			}
		}
		
		public override void Dispose() {
			foreach (Project p in projectsWatched.Keys)
				p.FileChangedInProject -= new ProjectFileEventHandler(Monitor);
			projectsWatched.Clear();
		}
		
		
		public override Type CommandHandlerType {
			get { return typeof(AddinCommandHandler); }
		}
	}

	public enum Commands {
    	Update,
    	Diff,
    	Log,
    	Status
	}
	
	class AddinCommandHandler : NodeCommandHandler {
		[CommandHandler (Commands.Update)]
		protected void OnUpdate() {
			RunCommand(Commands.Update, false);
		}
		
		[CommandUpdateHandler (Commands.Update)]
		protected void UpdateUpdate(CommandInfo item) {
			TestCommand(Commands.Update, item);
		}
		
		[CommandHandler (Commands.Diff)]
		protected void OnDiff() {
			RunCommand(Commands.Diff, false);
		}
		
		[CommandUpdateHandler (Commands.Diff)]
		protected void UpdateDiff(CommandInfo item) {
			TestCommand(Commands.Diff, item);
		}
		
		[CommandHandler (Commands.Log)]
		protected void OnLog() {
			RunCommand(Commands.Log, false);
		}
		
		[CommandUpdateHandler (Commands.Log)]
		protected void UpdateLog(CommandInfo item) {
			TestCommand(Commands.Log, item);
		}
		
		[CommandHandler (Commands.Status)]
		protected void OnStatus() {
			RunCommand(Commands.Status, false);
		}
		
		[CommandUpdateHandler (Commands.Status)]
		protected void UpdateStatus(CommandInfo item) {
			TestCommand(Commands.Status, item);
		}
		
		private void TestCommand(Commands cmd, CommandInfo item) {
			item.Visible = RunCommand(cmd, true);
		}
		
		private bool RunCommand(Commands cmd, bool test) {
			string path;
			bool isDir;
			
			if (CurrentNode.DataItem is ProjectFile) {
				ProjectFile file = (ProjectFile)CurrentNode.DataItem;
				path = file.FilePath;
				isDir = false;
			} else if (CurrentNode.DataItem is DotNetProject) {
				DotNetProject project = (DotNetProject)CurrentNode.DataItem;
				path = project.BaseDirectory;
				isDir = true;
			} else if (CurrentNode.DataItem is ProjectFolder) {
				ProjectFolder f = ((ProjectFolder)CurrentNode.DataItem);
				path = f.Path;
				isDir = true;				
			} else if (CurrentNode.DataItem is Combine) {
				Combine c = ((Combine)CurrentNode.DataItem);
				path = c.BaseDirectory;
				isDir = true;				
			} else {
				Console.Error.WriteLine(CurrentNode.DataItem);
				return false;
			}
			
			switch (cmd) {
				case Commands.Update:
					return UpdateCommand.Update(path, test);
				case Commands.Diff:
					return DiffView.Show(path, test);
				case Commands.Log:
					return LogView.Show(path, isDir, null, test);
				case Commands.Status:
					return StatusView.Show(path, test);
			}
			return false;
		}
	}

	public abstract class BaseView : AbstractBaseViewContent, IViewContent {
		string name;
		public BaseView(string name) { this.name = name; }
		
		protected virtual void SaveAs(string fileName) {
		}

		void IViewContent.Load(string fileName) {
			throw new InvalidOperationException();
		}
		void IViewContent.Save() {
			throw new InvalidOperationException();
		}
		void IViewContent.Save(string fileName) {
			SaveAs(fileName);
		}
		
		string IViewContent.ContentName {
			get { return name; }
			set { }
		}
		
		bool IViewContent.HasProject {
			get { return false; }
			set { }
		}
		
		bool IViewContent.IsDirty {
			get { return false; }
			set { }
		}
		
		bool IViewContent.IsReadOnly {
			get { return true; }
		}

		bool IViewContent.IsUntitled {
			get { return false; }
		}

		bool IViewContent.IsViewOnly {
			get { return false; }
		}
		
		string IViewContent.PathRelativeToProject {
			get { return ""; }
		}
		
		MonoDevelop.Internal.Project.Project IViewContent.Project {
			get { return null; }
			set { }
		}
		
		string IViewContent.TabPageLabel {
			get { return name; }
		}
		
		string IViewContent.UntitledName {
			get { return ""; }
			set { }
		}
		
		event EventHandler IViewContent.BeforeSave { add { } remove { } }
		event EventHandler IViewContent.ContentChanged { add { } remove { } }
		event EventHandler IViewContent.ContentNameChanged { add { } remove { } }
		event EventHandler IViewContent.DirtyChanged { add { } remove { } }
	}
	

}
