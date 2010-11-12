// BaseFuseFileCopyHandler.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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

using System;
using System.IO;
using mun = Mono.Unix.Native;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Deployment.Targets
{
	
	public abstract class BaseFuseFileCopyHandler : LocalFileCopyHandler
	{
		public override string Name {
			get { throw new NotImplementedException ("Inheriting classes must override this.");  }
		}
		
		public override string Id {
			get { throw new NotImplementedException ("Inheriting classes must override this."); }
		}
		
		public override void CopyFiles (IProgressMonitor monitor, IFileReplacePolicy replacePolicy, FileCopyConfiguration copyConfig, DeployFileCollection deployFiles, DeployContext context)
		{
			DirectoryInfo tempDir = null;
			try {
				tempDir = CreateTempDir ();
			} catch (Exception e) {
				monitor.ReportError (GettextCatalog.GetString ("Could not create temporary directory."), e);
				return;
			}
			
			try {
				MountTempDirectory (monitor, copyConfig, tempDir.FullName);
			} catch (Exception e) {
				monitor.ReportError (GettextCatalog.GetString ("Could not mount FUSE filesystem."), e);
				RemoveTempDirIfEmpty (tempDir);
				return;
			}
			
			try {
				base.InternalCopyFiles (monitor, replacePolicy, copyConfig, deployFiles, context, tempDir.FullName);
			} finally {
				//unmount the filesystem
				try {
					string escapedDir = tempDir.FullName.Replace ("\"", "\\\"");
					string cmd, args;
					
					if (PropertyService.IsMac) {
						cmd = "umount";
						args = string.Format ("\"{0}\"", escapedDir);
					} else {
						cmd = "fusermount";
						args = string.Format ("-u \"{0}\"", escapedDir);
					}
					RunFuseCommand (monitor, cmd, args);
				} catch (Exception e) {
					LoggingService.LogError (GettextCatalog.GetString ("Could not unmount FUSE filesystem."), e);
					monitor.ReportError (GettextCatalog.GetString ("Could not unmount FUSE filesystem."), e);
				}
				RemoveTempDirIfEmpty (tempDir);
			}
		}
		
		void RemoveTempDirIfEmpty (DirectoryInfo tempDir)
		{
			FileSystemInfo[] infos = tempDir.GetFileSystemInfos ();
			if (infos != null && infos.Length == 0)
				tempDir.Delete ();
		}
		
		public abstract void MountTempDirectory (IProgressMonitor monitor, FileCopyConfiguration copyConfig, string tempPath);
		
		protected void RunFuseCommand (IProgressMonitor monitor, string command, string args)
		{
			LoggingService.LogInfo ("Running FUSE command: {0} {1}", command, args);
			var log = new StringWriter ();
			var psi = new System.Diagnostics.ProcessStartInfo (command, args) {
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
			};
			using (var opMon = new AggregatedOperationMonitor (monitor)) {
				using (var pWrapper = MonoDevelop.Core.Runtime.ProcessService.StartProcess (psi, log, log, null)) {
					opMon.AddOperation (pWrapper);
					pWrapper.WaitForOutput ();
					if (pWrapper.ExitCode != 0)
						throw new Exception (log.ToString ());
				}
			}
		}

		static string ConstructTempDirName ()
		{
			return System.IO.Path.Combine (System.IO.Path.GetTempPath (), "md-deploy-" + Environment.UserName + DateTime.Now.Ticks);
		}
		
		DirectoryInfo CreateTempDir ()
		{
			string tempPath = ConstructTempDirName ();
			if (Directory.Exists (tempPath) || File.Exists (tempPath)) {
				tempPath = ConstructTempDirName ();
				if (Directory.Exists (tempPath) || File.Exists (tempPath))
					throw new Exception ("Temp directory " + ConstructTempDirName () + " already exists");
			}

			DirectoryInfo dInfo = Directory.CreateDirectory (tempPath);
			if (dInfo == null || !dInfo.Exists)
				throw new Exception ("Directory could not be created.");
			
			//make read/write-able by owner only, for security
			//System.Security.AccessControl.DirectorySecurity not supported on Mono, so use Mono.Posix
			int err = mun.Syscall.chmod (tempPath, mun.FilePermissions.S_IRUSR | mun.FilePermissions.S_IWUSR | mun.FilePermissions.S_IXUSR);
			if (err != 0)
				throw new Exception (string.Format ("Could not change access rights for temporary directory: error code {0}.", ((mun.Errno) err).ToString ()));
			                   
			return dInfo;
		}
		
		public override FileCopyConfiguration CreateConfiguration ()
		{
			throw new NotImplementedException ("Inheriting classes must override this.");
		}

	}
}
