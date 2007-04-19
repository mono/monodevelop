
using System;
using System.IO;
using MonoDevelop.Deployment;
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment.Linux
{
	internal class LinuxDeployExtension: DeployServiceExtension
	{
		public override DeployFileCollection GetProjectDeployFiles (DeployContext ctx, Project project)
		{
			DeployFileCollection col = base.GetProjectDeployFiles (ctx, project);
			
			if (ctx.Platform == "Linux") {
				DotNetProject netProject = project as DotNetProject;
				if (netProject != null) {
					LinuxDeployData data = LinuxDeployData.GetLinuxDeployData (netProject);
					DotNetProjectConfiguration conf = netProject.ActiveConfiguration as DotNetProjectConfiguration;
					if (conf != null && conf.CompileTarget == CompileTarget.Exe) {
						if (data.GenerateScript) {
							col.Add (GenerateLaunchScript (ctx, netProject, data, conf));
						}
					}
					if (conf != null && conf.CompileTarget == CompileTarget.Library) {
						if (data.GeneratePcFile) {
							col.Add (GeneratePcFile (ctx, netProject, data, conf));
						}
					}
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
				sw.WriteLine ("exec mono \"" + exe + "\"");
			}
			string outfile = data.ScriptName;
			if (string.IsNullOrEmpty (outfile))
				outfile = netProject.Name.ToLower ();
			DeployFile df = new DeployFile (file, outfile, TargetDirectory.Binaries);
			df.ContainsPathReferences = true;
			return df;
		}
		
		DeployFile GeneratePcFile (DeployContext ctx, DotNetProject netProject, LinuxDeployData data, DotNetProjectConfiguration conf)
		{
			string libs = "-r:@ProgramFiles@/" + Path.GetFileName (conf.CompiledOutputName);
			string requires = "";
			
			string file = ctx.CreateTempFile ();
			using (StreamWriter sw = new StreamWriter (file)) {
				sw.WriteLine ("Name: " + netProject.Name);
				sw.WriteLine ("Description: " + netProject.Name);
				sw.WriteLine ("Version: " + netProject.Version);
				sw.WriteLine ();
				sw.WriteLine ("Requires: " + requires);
				sw.WriteLine ("Libs: " + libs);
			}
			string outfile = netProject.Name.ToLower () + ".pc";
			DeployFile df = new DeployFile (file, outfile, LinuxTargetDirectory.PkgConfig);
			df.ContainsPathReferences = true;
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
