
using System;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Ide;
using System.Threading.Tasks;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GtkProjectServiceExtension: DotNetProjectExtension
	{
		[ItemProperty ("GtkDesignInfo", IsExternal = true, SkipEmpty = true)]
		GtkDesignInfo info;

		protected override bool SupportsObject (WorkspaceObject item)
		{
			return base.SupportsObject (item) && IdeApp.IsInitialized;
		}

		protected override void OnReadProject (ProgressMonitor monitor, MonoDevelop.Projects.Formats.MSBuild.MSBuildProject msproject)
		{
			base.OnReadProject (monitor, msproject);
			if (info != null)
				info.Project = Project;
		}

		public GtkDesignInfo DesignInfo {
			get {
				if (info == null)
					info = new GtkDesignInfo (Project);
				return info;
			}
			set {
				info = value;
			}
		}

		protected async override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			if (Project.References.Count == 0 || !GtkDesignInfo.HasDesignedObjects (Project))
				return await base.OnBuild (monitor, configuration, operationContext);

			Generator gen = new Generator ();
			if (!await gen.Run (monitor, Project, configuration)) {
				BuildResult gr = new BuildResult ();
				foreach (string s in gen.Messages)
					gr.AddError (DesignInfo.GuiBuilderProject.File, 0, 0, null, s);
				return gr;
			}
					
			BuildResult res = await base.OnBuild (monitor, configuration, operationContext);

			if (gen.Messages != null) {
				foreach (string s in gen.Messages)
					res.AddWarning (DesignInfo.GuiBuilderProject.File, 0, 0, null, s);
						
				if (gen.Messages.Length > 0)
					DesignInfo.ForceCodeGenerationOnBuild ();
			}
			
			if (res.Failed && !Platform.IsWindows && !Platform.IsMac) {
				// Some gtk# packages don't include the .pc file unless you install gtk-sharp-devel
				if (Project.AssemblyContext.GetPackage ("gtk-sharp-2.0") == null) {
					string msg = GettextCatalog.GetString (
						"ERROR: MonoDevelop could not find the Gtk# 2.0 development package. " +
						"Compilation of projects depending on Gtk# libraries will fail. " +
						"You may need to install development packages for gtk-sharp-2.0.");
					monitor.Log.WriteLine ();
					monitor.Log.WriteLine (BrandingService.BrandApplicationName (msg));
				}
			}
			
			return res;
		}
	}
	
	class Generator
	{
		public async Task<bool> Run (ProgressMonitor monitor, DotNetProject project, ConfigurationSelector configuration)
		{
			try {
				Stetic.CodeGenerationResult res = await GuiBuilderService.GenerateSteticCode (monitor, project, configuration);
				if (res != null)
					Messages = res.Warnings;
				return true;
			} catch (Exception ex) {
				Error = ex;
				LoggingService.LogError (ex.ToString ());
				Messages = new  [] { Error.Message };
				return false;
			}
		}
		public string[] Messages;
		public Exception Error;
	}
}
