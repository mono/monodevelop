
using System;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	
	
	internal partial class DeployFileListWidget : Gtk.Bin
	{
		ListStore store;
		
		PackageBuilder builder;
		DeployContext context;
		DeployFileCollection files;
		
		const int ColObject = 0;
		const int ColSourceFile = 1;
		const int ColTargetDir = 2;
		const int ColDestFile = 3;
		const int ColProject = 4;
		const int ColIncluded = 5;
		
		public DeployFileListWidget()
		{
			this.Build();
			
			store = new ListStore (typeof(object), typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool));
			fileList.Model = store;
			
			CellRendererToggle tog = new CellRendererToggle ();
			TreeViewColumn col = new TreeViewColumn ();
			tog.Toggled += OnToggled;
			col.Title = "";
			col.PackStart (tog, false);
			col.AddAttribute (tog, "active", ColIncluded);
			col.SortColumnId = ColIncluded;
			fileList.AppendColumn (col);
			
			CellRendererText crt = new CellRendererText ();
			col = new TreeViewColumn ();
			col.Title = "Solution/Project";
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ColProject);
			col.SortColumnId = ColProject;
			col.Resizable = true;
			fileList.AppendColumn (col);
			
			crt = new CellRendererText ();
			col = new TreeViewColumn ();
			col.Title = "Source File";
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ColSourceFile);
			col.SortColumnId = ColSourceFile;
			col.Resizable = true;
			fileList.AppendColumn (col);
			
			crt = new CellRendererText ();
			col = new TreeViewColumn ();
			col.Title = "Target Directory";
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ColTargetDir);
			col.SortColumnId = ColTargetDir;
			col.Resizable = true;
			fileList.AppendColumn (col);
			
			crt = new CellRendererText ();
			col = new TreeViewColumn ();
			col.Title = "Target Path";
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ColDestFile);
			col.SortColumnId = ColDestFile;
			col.Resizable = true;
			fileList.AppendColumn (col);
			
			store.SetSortColumnId (ColProject, SortType.Ascending);
		}
		
		public void Fill (PackageBuilder builder)
		{
			if (context != null)
				context.Dispose ();
			
			this.builder = builder;
			this.context = builder.CreateDeployContext ();
			
			files = builder.GetDeployFiles (context);
			
			store.Clear ();
			foreach (DeployFile file in files) {
				string desc = GetDirectoryName (file.TargetDirectoryID);
				store.AppendValues (file, file.DisplayName, desc, file.RelativeTargetPath, file.SourceCombineEntry.Name, builder.IsFileIncluded (file));
			}
		}
		
		string GetDirectoryName (string dirId)
		{
			foreach (DeployDirectoryInfo info in DeployService.GetDeployDirectoryInfo ())
				if (dirId == info.Id)
					return info.Description;
			return "";
		}		
		
		protected override void OnDestroyed ()
		{
			context.Dispose ();
			base.OnDestroyed ();
		}

		void OnToggled (object sender, Gtk.ToggledArgs args)
		{
			TreeIter iter;
			store.GetIterFromString (out iter, args.Path);
			DeployFile file = (DeployFile) store.GetValue (iter, ColObject);
			bool inc = !builder.IsFileIncluded (file);
			builder.SetFileIncluded (file, inc);
			store.SetValue (iter, ColIncluded, inc);
		}
	}
}
