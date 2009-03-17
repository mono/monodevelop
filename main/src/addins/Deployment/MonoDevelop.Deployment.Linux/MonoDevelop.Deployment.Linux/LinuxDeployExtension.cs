
using System;
using System.IO;
using MonoDevelop.Deployment;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment.Linux
{
	internal class LinuxDeployExtension: DeployServiceExtension
	{
		public override DeployFileCollection GetProjectDeployFiles (DeployContext ctx, Project project, string config)
		{
			DeployFileCollection col = base.GetProjectDeployFiles (ctx, project, config);
			
			LinuxDeployData data = LinuxDeployData.GetLinuxDeployData (project);
			
			if (ctx.Platform == "Linux") {
				DotNetProject netProject = project as DotNetProject;
				if (netProject != null) {
					DotNetProjectConfiguration conf = netProject.GetConfiguration (config) as DotNetProjectConfiguration;
					if (conf != null) {
						if (conf.CompileTarget == CompileTarget.Exe || conf.CompileTarget == CompileTarget.WinExe) {
							if (data.GenerateScript) {
								col.Add (GenerateLaunchScript (ctx, netProject, data, conf));
							}
						}
						if (conf.CompileTarget == CompileTarget.Library || conf.CompiledOutputName.EndsWith (".dll")) {
							if (data.GeneratePcFile) {
								col.Add (GeneratePcFile (ctx, netProject, data, conf));
							}
						}
					}
				}
			}
			
			// If the project is deploying an app.desktop file, rename it to the name of the project.
			foreach (DeployFile file in col) {
				if (Path.GetFileName (file.RelativeTargetPath) == "app.desktop") {
					string dir = Path.GetDirectoryName (file.RelativeTargetPath);
					file.RelativeTargetPath = Path.Combine (dir, data.PackageName + ".desktop");
				}
			}
			
			return col;
		}
		
		DeployFile GenerateLaunchScript (DeployContext ctx, DotNetProject netProject, LinuxDeployData data, DotNetProjectConfiguration conf)
		{
			string file = ctx.CreateTempFile ();
			string exe = "@ProgramFiles@/" + Path.GetFileName (conf.CompiledOutputName);
			
			using (StreamWriter sw = new StreamWriter (file)) {
				sw.WriteLine ("#!/bin/sh");
				sw.WriteLine ();
				sw.WriteLine ("exec mono \"" + exe + "\" \"$@\"");
			}
			string outfile = data.ScriptName;
			if (string.IsNullOrEmpty (outfile))
				outfile = netProject.Name.ToLower ();
			DeployFile df = new DeployFile (netProject, file, outfile, TargetDirectory.Binaries);
			df.ContainsPathReferences = true;
			df.DisplayName = GettextCatalog.GetString ("Launch script for {0}", netProject.Name);
			df.FileAttributes = DeployFileAttributes.Executable;
			return df;
		}
		
		DeployFile GeneratePcFile (DeployContext ctx, DotNetProject netProject, LinuxDeployData data, DotNetProjectConfiguration conf)
		{
			string libs = "-r:@ProgramFiles@/" + Path.GetFileName (conf.CompiledOutputName);
			string requires = "";
			string version = netProject.Version;
			if (string.IsNullOrEmpty (version) && netProject.ParentSolution != null)
				version = netProject.ParentSolution.Version;
			    
			string file = ctx.CreateTempFile ();
			using (StreamWriter sw = new StreamWriter (file)) {
				sw.WriteLine ("Name: " + netProject.Name);
				sw.WriteLine ("Description: " + (String.IsNullOrEmpty(netProject.Description) ? netProject.Name : netProject.Description));
				sw.WriteLine ("Version: " + version);
				sw.WriteLine ();
				sw.WriteLine ("Requires: " + requires);
				sw.WriteLine ("Libs: " + libs);
			}
			string outfile = netProject.Name.ToLower () + ".pc";
			DeployFile df = new DeployFile (netProject, file, outfile, LinuxTargetDirectory.PkgConfig);
			df.ContainsPathReferences = true;
			df.DisplayName = GettextCatalog.GetString ("pkg-config file for {0}", netProject.Name);
			return df;
		}
		
		public override string ResolveDirectory (DeployContext context, string folderId)
		{
			if (context.Platform == "Linux") {
				if (folderId == LinuxTargetDirectory.PkgConfig)
					return Path.Combine (context.GetDirectory (TargetDirectory.ProgramFilesRoot), "pkgconfig");
				if (folderId == LinuxTargetDirectory.DesktopApplications)
					return Path.Combine (context.GetDirectory (TargetDirectory.CommonApplicationDataRoot), "applications");
			}
			return base.ResolveDirectory (context, folderId);
		}

	}
}
