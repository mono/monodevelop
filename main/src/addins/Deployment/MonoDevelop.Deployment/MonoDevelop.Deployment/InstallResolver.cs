//
// InstallResolver.cs
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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using System.Collections.Specialized;
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment
{
	class InstallResolver: IDirectoryResolver
	{
		string appName;
		
		public void Install (IProgressMonitor monitor, SolutionItem entry, string appName, string prefix, string configuration)
		{
			this.appName = appName;
			
			using (DeployContext ctx = new DeployContext (this, "Linux", prefix)) {
				InstallEntry (monitor, ctx, entry, configuration);
			}
		}
		
		void InstallEntry (IProgressMonitor monitor, DeployContext ctx, SolutionItem entry, string configuration)
		{
			foreach (DeployFile df in DeployService.GetDeployFiles (ctx, new SolutionItem[] { entry }, configuration)) {
				string targetPath = df.ResolvedTargetFile;
				if (targetPath == null) {
					monitor.ReportWarning ("Could not copy file '" + df.RelativeTargetPath + "': Unknown target directory.");
					continue;
				}
				
				CopyFile (monitor, df.SourcePath, df.ResolvedTargetFile, df.FileAttributes);
			}
			
			SolutionFolder c = entry as SolutionFolder;
			if (c != null) {
				monitor.BeginTask ("Installing solution '" + c.Name + "'", c.Items.Count);
				foreach (SolutionItem ce in c.Items) {
					InstallEntry (monitor, ctx, ce, configuration);
					monitor.Step (1);
				}
				monitor.EndTask ();
			}
		}
		
		void CopyFile (IProgressMonitor monitor, string src, string dest, DeployFileAttributes atts)
		{
			dest = FileService.GetFullPath (dest);
			monitor.Log.WriteLine (GettextCatalog.GetString ("Deploying file {0}.", dest));
			
			string targetDir = Path.GetDirectoryName (dest);
			if (!Directory.Exists (targetDir))
				Directory.CreateDirectory (targetDir);
			FileService.CopyFile (src, dest);
			
			Mono.Unix.Native.FilePermissions perms = Mono.Unix.Native.FilePermissions.DEFFILEMODE;
			if ((atts & DeployFileAttributes.Executable) != 0)
				perms |= Mono.Unix.Native.FilePermissions.S_IXGRP | Mono.Unix.Native.FilePermissions.S_IXUSR | Mono.Unix.Native.FilePermissions.S_IXOTH;
			if ((atts & DeployFileAttributes.ReadOnly) != 0)
				perms &= ~(Mono.Unix.Native.FilePermissions.S_IWGRP | Mono.Unix.Native.FilePermissions.S_IWOTH | Mono.Unix.Native.FilePermissions.S_IWUSR);
			if (perms != Mono.Unix.Native.FilePermissions.DEFFILEMODE)
				Mono.Unix.Native.Syscall.chmod (dest, perms);
		}
		
		public string GetDirectory (DeployContext ctx, string folderId)
		{
			switch (folderId) {
			case TargetDirectory.ProgramFiles:
				return Path.Combine (ctx.GetDirectory (TargetDirectory.ProgramFilesRoot), appName);
			case TargetDirectory.CommonApplicationData:
				return Path.Combine (ctx.GetDirectory (TargetDirectory.ProgramFilesRoot), appName);
			}

			if (ctx.Platform == "Linux" || ctx.Platform == "Unix") {
				string prefix = ctx.Prefix;
				if (prefix == null)
					prefix = "/usr/local";
				switch (folderId) {
				case TargetDirectory.ProgramFilesRoot:
					return Path.Combine (prefix, "lib");
				case TargetDirectory.CommonApplicationDataRoot:
					return Path.Combine (prefix, "share");
				case TargetDirectory.Binaries:
					return Path.Combine (prefix, "bin");
				case TargetDirectory.IncludeRoot:
					return Path.Combine (prefix, "include");
				case TargetDirectory.Include:
					return Path.Combine (ctx.GetDirectory (TargetDirectory.IncludeRoot), appName);
				}
			}
			else if (ctx.Platform == "Windows") {
				switch (folderId) {
				case TargetDirectory.ProgramFilesRoot:
					return Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles);
				case TargetDirectory.Binaries:
					return Path.Combine (ctx.GetDirectory (TargetDirectory.ProgramFilesRoot), appName);
				case TargetDirectory.CommonApplicationDataRoot:
					return Environment.GetFolderPath (Environment.SpecialFolder.CommonApplicationData);
				}
			}
			return null;
		}
	}
}
