
using System;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	
	
	public partial class DeployFileListWidget : Gtk.Bin
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
		
		public DeployFileListWidget()
		{
			this.Build();
			
			store = new ListStore (typeof(object), typeof(string), typeof(string), typeof(string), typeof(string));
			fileList.Model = store;
			
			CellRendererText crt = new CellRendererText ();
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = "Solution/Project";
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ColProject);
			col.SortColumnId = 0;
			fileList.AppendColumn (col);
			
			crt = new CellRendererText ();
			col = new TreeViewColumn ();
			col.Title = "Source File";
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ColSourceFile);
			col.SortColumnId = 1;
			fileList.AppendColumn (col);
			
			crt = new CellRendererText ();
			col = new TreeViewColumn ();
			col.Title = "Target Directory";
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ColTargetDir);
			col.SortColumnId = 2;
			fileList.AppendColumn (col);
			
			crt = new CellRendererText ();
			col = new TreeViewColumn ();
			col.Title = "Target Path";
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ColDestFile);
			col.SortColumnId = 3;
			fileList.AppendColumn (col);
			
			store.SetSortColumnId (0, SortType.Ascending);
		}
		
		public void Fill (PackageBuilder builder)
		{
			if (context != null)
				context.Dispose ();
			
			this.builder = builder;
			this.context = builder.CreateDeployContext ();
			
			files = DeployService.GetDeployFiles (context, builder.GetAllEntries ());
			
			store.Clear ();
			foreach (DeployFile file in files) {
				string desc = GetDirectoryName (file.TargetDirectoryID);
				store.AppendValues (file, file.DisplayName, desc, file.RelativeTargetPath, file.SourceCombineEntry.Name);
			}
		}
		
		string GetDirectoryName (string dirId)
		{
			foreach (DeployDirectoryInfo info in DeployService.GetDeployDirectoryInfo ())
				if (dirId == info.Id)
					return info.Description;
			return "";
		}		
		
		public override void Dispose ()
		{
			context.Dispose ();
			base.Dispose ();
		}

	}
}
