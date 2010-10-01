// 
// GitVersionControl.cs
//  
// Author:
//       Dale Ragan <dale.ragan@sinesignal.com>
// 
// Copyright (c) 2010 SineSignal, LLC
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Core;
using System.IO;

namespace MonoDevelop.VersionControl.Git
{
	public abstract class GitVersionControl : VersionControlSystem
	{
		readonly string[] protocolsGit = {"git", "ssh", "http", "https", "ftp", "ftps", "rsync"};
		
		static string gitExe;
		const string msysGitX86 = @"C:\Program Files (x86)\Git\bin\git.exe";
		const string msysGit = @"C:\Program Files\Git\bin\git.exe";
		const string gitOsxInstaller = "/usr/local/git/bin/git";
		
		static GitVersionControl ()
		{
			string git = "git";
			if (PropertyService.IsWindows) {
				if (File.Exists (msysGit))
					git = msysGit;
				else if (File.Exists (msysGitX86))
					git = msysGitX86;
			} else if (PropertyService.IsMac) {
				if (File.Exists (gitOsxInstaller))
					git = gitOsxInstaller;
			}
			
			try {
				var psi = new System.Diagnostics.ProcessStartInfo (git, "--version") {
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				};
				
				StringWriter outw = new StringWriter ();
				var proc = Runtime.ProcessService.StartProcess (psi, outw, outw, null);
				proc.WaitForOutput ();
				if (proc.ExitCode == 0) {
					gitExe = git;
					return;
				}
			} catch (Exception) {
				LoggingService.LogWarning ("Could not find git. Git addin will be disabled");
				gitExe = null;
			}
		}
		
		internal static string GitExe {
			get { return gitExe; }
		}
		
		public override string Name {
			get { return "Git"; }
		}
		
		public override bool IsInstalled {
			get {
				return gitExe != null;
			}
		}
		
		public override Repository GetRepositoryReference (FilePath path, string id)
		{
			if (path.IsEmpty || path.ParentDirectory.IsEmpty || path.IsNull || path.ParentDirectory.IsNull)
				return null;
			if (System.IO.Directory.Exists (path.Combine (".git")))
				return new GitRepository (path, null);
			else
				return GetRepositoryReference (path.ParentDirectory, id);
		}
		
		protected override Repository OnCreateRepositoryInstance ()
		{
			return new GitRepository ();
		}
		
		public override Gtk.Widget CreateRepositoryEditor (Repository repo)
		{
			return new UrlBasedRepositoryEditor ((GitRepository)repo, protocolsGit);
		}
	}
}

