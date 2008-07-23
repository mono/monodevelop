
using System;
using System.IO;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GtkProjectServiceExtension: ProjectServiceExtension
	{
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem entry, string configuration)
		{
			DotNetProject project = entry as DotNetProject;
			if (project == null)
				return base.Build (monitor, entry, configuration);

			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			if (info.HasDesignedObjects) {
				// The code generator must run in the GUI thread since it needs to
				// access to Gtk classes
				Generator gen = new Generator ();
				lock (gen) {
					Gtk.Application.Invoke (delegate { gen.Run (monitor, project, configuration); });
					Monitor.Wait (gen);
				}
						
				BuildResult res = base.Build (monitor, entry, configuration);
						
				if (gen.Messages != null) {
					foreach (string s in gen.Messages)
						res.AddWarning (s);
							
					if (gen.Messages.Length > 0)
						info.ForceCodeGenerationOnBuild ();
				}
				return res;
			}
			
			return base.Build (monitor, entry, configuration);
		}
	}
	
	class Generator
	{
		public void Run (IProgressMonitor monitor, DotNetProject project, string configuration)
		{
			lock (this) {
				try {
					Stetic.CodeGenerationResult res = GuiBuilderService.GenerateSteticCode (monitor, project, configuration);
					if (res != null)
						Messages = res.Warnings;
				} catch (Exception ex) {
					Error = ex;
					LoggingService.LogError (ex.ToString ());
					Messages = new string [] { Error.Message };
				}
				Monitor.PulseAll (this);
			}
		}
		public string[] Messages;
		public Exception Error;
	}
}
