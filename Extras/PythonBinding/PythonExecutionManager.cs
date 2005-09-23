using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.CodeDom.Compiler;
using Gtk;

using MonoDevelop.Internal.Project;
using MonoDevelop.Gui;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;

namespace PythonBinding
{
	public class PythonExecutionManager
	{
		public void Execute (string filename, bool debug)
		{
			ProcessStartInfo psi = new ProcessStartInfo ("IronPythonConsole", filename);
			psi.WorkingDirectory = Path.GetDirectoryName (filename);
			psi.UseShellExecute = false;
		}
		
		public void Execute(IProject project, bool debug)
		{
			//PythonCompilerParameters parameters = (PythonCompilerParameters) project.ActiveConfiguration;
			//FileUtilityService fileUtilityService = (FileUtilityService) ServiceManager.GetService (typeof (FileUtilityService));
	
			string files = "";

			foreach (ProjectFile finfo in project.ProjectFiles) {
				if (finfo.Subtype != Subtype.Directory) {
					switch (finfo.BuildAction) {
						case BuildAction.Compile:
							files += String.Format ("{0} ", finfo.Name);
							break;
					}
				}
			}
			Console.WriteLine (files);

			string fullCommand = String.Format ("-e \"IronPythonConsole {0};read -p 'press any key to continue...' -n1\"", files);
			ProcessStartInfo psi = new ProcessStartInfo ("xterm", fullCommand);
			//psi.WorkingDirectory = Path.GetDirectoryName (exe);
			psi.UseShellExecute  = false;
			Process p = Process.Start (psi);
			p.WaitForExit ();
		}
	}
}
