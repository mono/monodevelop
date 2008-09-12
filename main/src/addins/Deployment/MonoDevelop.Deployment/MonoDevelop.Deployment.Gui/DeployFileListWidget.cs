
using System;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment
{
	
	
	[System.ComponentModel.Category("MonoDevelop.Deployment")]
	[System.ComponentModel.ToolboxItem(true)]
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
			
			store.Clear ();
			
			string[] configs = builder.GetSupportedConfigurations ();
			
			string currentActive = comboConfigs.Active != -1 ? comboConfigs.ActiveText : null;
			int i = Array.IndexOf (configs, currentActive);
			if (i == -1) i = 0;
			
			((Gtk.ListStore)comboConfigs.Model).Clear ();
			foreach (string conf in configs)
				comboConfigs.AppendText (conf);
			
			if (configs.Length <= 1) {
				labelFiles.Text = GettextCatalog.GetString ("The following files will be included in the package:");
				comboConfigs.Visible = false;
				if (configs.Length == 0)
					return;
			}
			else if (configs.Length > 0) {
				comboConfigs.Visible = true;
				labelFiles.Text = GettextCatalog.GetString ("The following files will be included in the package for the configuration:");
			}
			
			comboConfigs.Active = i;
		}
		
		void FillFiles ()
		{
			store.Clear ();
			if (comboConfigs.Active == -1)
				return;

			files = builder.GetDeployFiles (context, comboConfigs.ActiveText);
			
			foreach (DeployFile file in files) {
				string desc = GetDirectoryName (file.TargetDirectoryID);
				store.AppendValues (file, file.DisplayName, desc, file.RelativeTargetPath, file.SourceSolutionItem.Name, builder.IsFileIncluded (file));
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

		protected virtual void OnComboConfigsChanged (object sender, System.EventArgs e)
		{
			FillFiles ();
		}
	}
}
