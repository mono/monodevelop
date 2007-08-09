using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

using System;
using System.Collections.Generic;
using System.IO;

namespace MonoDevelop.Prj2Make
{
	public class MSBuildProjectServiceExtension : ProjectServiceExtension
	{

		public override ICompilerResult Build (IProgressMonitor monitor, CombineEntry entry)
		{
			//xamlg any SilverLightPages
			DotNetProject project = entry as DotNetProject;
			if (project == null)
				return base.Build (monitor, entry);

			foreach (ProjectFile pf in project.ProjectFiles) {
				if (pf.BuildAction != BuildAction.EmbedAsResource)
					continue;

				//Check for SilverLightPage
				if (pf.ExtendedProperties ["MonoDevelop.MSBuildFileFormat.SilverlightPage"] != null)
					//FIXME: throw if error?
					GenerateXamlPartialClass (pf.Name, monitor);
			}

			return base.Build (monitor, entry);
		}

		string GenerateXamlPartialClass (string fname, IProgressMonitor monitor)
		{
			using (StringWriter sw = new StringWriter ()) {
				Console.WriteLine ("Generating partial classes for\n{0}$ {1} {2}", Path.GetDirectoryName (fname), "xamlg", fname);
				monitor.Log.WriteLine (GettextCatalog.GetString (
					"Generating partial classes for {0} with {1}", fname, "xamlg"));
				ProcessWrapper pw = null;
				try {
					pw = Runtime.ProcessService.StartProcess (
						"xamlg", String.Format ("\"{0}\"", fname),
						Path.GetDirectoryName (fname),
						sw, sw, null);
				} catch (System.ComponentModel.Win32Exception ex) {
					Console.WriteLine (GettextCatalog.GetString (
						"Error while trying to invoke '{0}' to generate partial classes for '{1}' :\n {2}", "xamlg", fname, ex.ToString ()));
					monitor.Log.WriteLine (GettextCatalog.GetString (
						"Error while trying to invoke '{0}' to generate partial classes for '{1}' :\n {2}", "xamlg", fname, ex.Message));

					return null;
				}

				//FIXME: Handle exceptions
				pw.WaitForOutput ();

				if (pw.ExitCode == 0) {
					return fname + ".g.cs";
				} else {
					//FIXME: Stop build on error?
					Console.WriteLine (GettextCatalog.GetString (
						"Unable to generate partial classes ({0}) for {1}. Ignoring. \nReason: \n{2}\n",
						"xamlg", fname, sw.ToString ()));
					monitor.Log.WriteLine (GettextCatalog.GetString (
						"Unable to generate partial classes ({0}) for {1}. Ignoring. \nReason: \n{2}\n",
						"xamlg", fname, sw.ToString ()));

					return null;
				}
			}

		}

	}
}
