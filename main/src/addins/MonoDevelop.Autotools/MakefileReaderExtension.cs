
using System;
using System.IO;
using System.Threading;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Autotools
{
	public class MakefileReaderExtension: ProjectServiceExtension
	{
		public override bool IsSolutionItemFile (string fileName)
		{
			if (Path.GetFileNameWithoutExtension (fileName) == "Makefile")
				return true;
			else
				return base.IsSolutionItemFile (fileName);
		}
		
		protected override SolutionEntityItem LoadSolutionItem (IProgressMonitor monitor, string fileName)
		{
			if (Path.GetFileNameWithoutExtension (fileName) != "Makefile")
				return base.LoadSolutionItem (monitor, fileName);
			
			// Use Makefile.am instead of Makefile if it exists
			if (Path.GetFileName (fileName) == "Makefile") {
				string amFile = Path.Combine (Path.GetDirectoryName (fileName), "Makefile.am");
				if (File.Exists (amFile))
					fileName = amFile;
			}
			
			string projectFile = fileName + ".mdp";
			if (File.Exists (projectFile))
				return base.LoadSolutionItem (monitor, projectFile);

			MakefileProject project = new MakefileProject ();
			
			if (!DispatchService.IsGuiThread) {
				lock (project) {
					Gtk.Application.Invoke (delegate {
						ImportProject (project, monitor, fileName);
					});
					Monitor.Wait (project);
				}
			} else
				ImportProject (project, monitor, fileName);
			
			if (monitor.IsCancelRequested)
				return null;
				
			project.Save (projectFile, monitor);
			return base.LoadSolutionItem (monitor, projectFile);
		}
		
		void ImportProject (Project project, IProgressMonitor monitor, string fileName)
		{
			ImportMakefileDialog dialog = null;
			
			try {
				MakefileData data = new MakefileData ();
				data.OwnerProject = project;
				data.IntegrationEnabled = true;
				data.RelativeMakefileName = fileName;
				project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] = data;
				string name = Path.GetFileName (Path.GetDirectoryName (fileName));
				dialog = new ImportMakefileDialog (project, data, name);
				
				do {
					if (dialog.Run () == (int) Gtk.ResponseType.Ok) {
						if (dialog.Store ())
							return;
					} else {
						monitor.AsyncOperation.Cancel ();
						return;
					}
				}
				while (true);
			} finally {
				if (dialog != null)
					dialog.Destroy ();
				lock (project) {
					Monitor.PulseAll (project);
				}
			}
		}
	}
}
