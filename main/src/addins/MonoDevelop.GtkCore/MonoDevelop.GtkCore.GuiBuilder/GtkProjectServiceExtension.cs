
using System;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using System.Threading.Tasks;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GtkProjectServiceExtension: DotNetProjectExtension
	{
		protected override bool SupportsObject (WorkspaceObject item)
		{
			return base.SupportsObject (item) && IdeApp.IsInitialized;
		}

		protected override void OnExtensionChainCreated ()
		{
			base.OnExtensionChainCreated ();
			if (!GtkDesignInfo.HasDesignedObjects (Project))
				Dispose ();
		}

		protected async override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			GtkDesignInfo info = GtkDesignInfo.FromProject (Project);

			Generator gen = new Generator ();
			if (!await gen.Run (monitor, Project, configuration)) {
				BuildResult gr = new BuildResult ();
				foreach (string s in gen.Messages)
					gr.AddError (info.GuiBuilderProject.File, 0, 0, null, s);
				return gr;
			}
					
			BuildResult res = await base.OnBuild (monitor, configuration);

			if (gen.Messages != null) {
				foreach (string s in gen.Messages)
					res.AddWarning (info.GuiBuilderProject.File, 0, 0, null, s);
						
				if (gen.Messages.Length > 0)
					info.ForceCodeGenerationOnBuild ();
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
				if (res != null) {
					Messages = res.Warnings;
					return true;
				} else {
					Messages = new [] { GettextCatalog.GetString ("Code generation failed") };
					return false;
				}
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
