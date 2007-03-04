//
// InstallDeployHandler.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.IO;
using System.Collections.Specialized;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Deployment.Extensions;

namespace MonoDevelop.Projects.Deployment
{
	
	public class InstallDeployHandler: IDeployHandler
	{
		public string Id {
			get { return "MonoDevelop.InstallDeploy"; } 
		}
		
		public string Description {
			get { return GettextCatalog.GetString ("Install"); } 
		}
		
		public string Icon {
			get { return "md-closed-folder"; }
		}
		
		public DeployTarget CreateTarget (CombineEntry entry)
		{
			return new InstallDeployTarget ();
		}
		
		public bool CanDeploy (CombineEntry entry) 
		{
			return entry is Project || entry is Combine;
		}
		
		public void Deploy (IProgressMonitor monitor, DeployTarget target)
		{
			if (target.CombineEntry is Project)
				DeployProject (monitor, target, (Project) target.CombineEntry);
			else {
				Combine c = (Combine) target.CombineEntry;
				foreach (Project p in c.GetAllProjects ())
					DeployProject (monitor, target, p);
			}
		}
		
		void DeployProject (IProgressMonitor monitor, DeployTarget target, Project p)
		{
			InstallDeployTarget dt = (InstallDeployTarget) target;
			DeployFileCollection deployFiles = p.GetDeployFiles ();
			StringDictionary dirs = GetDeployDirectories (dt.InstallPrefix, dt.ApplicationName);
			
			foreach (DeployFile df in deployFiles) {
				string targetPath = dirs [df.TargetDirectoryID];
				if (targetPath == null) {
					monitor.ReportWarning ("Could not copy file '" + df.RelativeTargetPath + "': Unknown target directory.");
					continue;
				}
				CopyFile (monitor, df.SourcePath, Path.Combine (targetPath, df.RelativeTargetPath));
			}
		}
		
		void CopyFile (IProgressMonitor monitor, string src, string dest)
		{
			dest = Runtime.FileService.GetFullPath (dest);
			monitor.Log.WriteLine (GettextCatalog.GetString ("Deploying file {0}.", dest));
			
			string targetDir = Path.GetDirectoryName (dest);
			if (!Directory.Exists (targetDir))
				Directory.CreateDirectory (targetDir);
			Runtime.FileService.CopyFile (src, dest);
		}
		
		StringDictionary GetDeployDirectories (string prefix, string appName)
		{
			if (appName == null) appName = "";
			StringDictionary directories = new StringDictionary ();
			directories [TargetDirectory.ProgramFiles] = Path.Combine (Path.Combine (prefix, "lib"), appName);
			directories [TargetDirectory.Binaries] =  Path.Combine (prefix, "bin");
			directories [TargetDirectory.Personal] =  Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), appName);
			directories [TargetDirectory.CommonApplicationData] =  Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.CommonApplicationData), appName);
			directories [TargetDirectory.ApplicationData] =  Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), appName);
			return directories;
		}
	}
	
}
