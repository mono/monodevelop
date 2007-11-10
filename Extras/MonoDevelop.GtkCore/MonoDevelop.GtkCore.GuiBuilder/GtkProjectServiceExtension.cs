
using System;
using System.IO;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GtkProjectServiceExtension: ProjectServiceExtension
	{
		internal static bool GenerateSteticCode;
		
		public override ICompilerResult Build (IProgressMonitor monitor, CombineEntry entry)
		{
			if (GenerateSteticCode) {
				DotNetProject project = entry as DotNetProject;
				if (project != null) {
					GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
					if (info != null) {
						// The code generator must run in the GUI thread since it needs to
						// access to Gtk classes
						Generator gen = new Generator ();
						lock (gen) {
							Gtk.Application.Invoke (delegate { gen.Run (monitor, project); });
							Monitor.Wait (gen);
						}
						
						// Build the project
						ICompilerResult res = base.Build (monitor, entry);
						
						if (gen.Messages != null) {
							foreach (string s in gen.Messages)
								res.AddWarning (s);
								
							// If there are warnings, "touch" the stetic file, so the code will be regenerated.
							if (gen.Messages.Length > 0) {
								info.ForceCodeGenerationOnBuild ();
							}
						}
						return res;
					}
				}
			}
			
			return base.Build (monitor, entry);
		}
	}
	
	class Generator
	{
		public void Run (IProgressMonitor monitor, DotNetProject project)
		{
			lock (this) {
				try {
					Stetic.CodeGenerationResult res = GuiBuilderService.GenerateSteticCode (monitor, project);
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
