// PackagingTests.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Deployment.Targets;
using MonoDevelop.Autotools;

namespace MonoDevelop.Deployment
{
	[TestFixture]
	public class PackagingTests: TestBase
	{
		string tarDir = null;
		int packId = 0;

#region Binaries package
		[Test]
		public void ConsoleProjectBins ()
		{
			string[] binFiles = new string[] {
				"ConsoleProject.exe",
				"ConsoleProject.exe.mdb",
				"consoleproject"
			};
			RunTestBinariesPackage ("console-project/ConsoleProject.sln", binFiles);
		}
		
		[Test]
		public void ConsoleProjectWithLibsMdpBins ()
		{
			string[] binFiles = new string[] {
				"console-with-libs-mdp.exe", 
				"console-with-libs-mdp.exe.mdb", 
				"console-with-libs-mdp",
				"library1.dll", 
				"library1.dll.mdb", 
				"library1.pc", 
				"library2.dll", 
				"library2.pc", 
				"library2.dll.mdb"
			};
			RunTestBinariesPackage ("console-with-libs-mdp/console-with-libs-mdp.mds", binFiles);
		}
#endregion
		
#region Sources package
		
		[Test]
		public void ConsoleProjectSources ()
		{
			RunTestSourcesPackage ("console-project/ConsoleProject.sln");
		}
		
		[Test]
		public void ConsoleProjectWithLibsMdpSources ()
		{
			RunTestSourcesPackage ("console-with-libs-mdp/console-with-libs-mdp.mds");
		}
		
		[Test]
		public void NestedSolutionsMdpSources ()
		{
			RunTestSourcesPackage ("nested-solutions-mdp/nested-solutions-mdp.mds");
		}
#endregion
		
#region Tarball package
		
		// Console project
		
		string[] filesConsoleProjectTarball = new string[] {
			"bin/consoleproject", 
			"lib/consoleproject/ConsoleProject.exe", 
			"lib/consoleproject/ConsoleProject.exe.mdb"
		};
		
		[Test]
		public void ConsoleProjectTarball ()
		{
			RunTestTarballPackage ("console-project/ConsoleProject.sln", false, "Debug", filesConsoleProjectTarball);
		}
		
		[Test]
		public void ConsoleProjectTarballAutotools ()
		{
			RunTestTarballPackage ("console-project/ConsoleProject.sln", true, "Debug", filesConsoleProjectTarball);
		}
		
		
		// ConsoleProjectWithLibsMdpTarball
		
		string[] filesConsoleProjectWithLibsMdpTarball = new string[] {
			"bin/console-with-libs-mdp", 
			"lib/console-with-libs-mdp/console-with-libs-mdp.exe", 
			"lib/console-with-libs-mdp/console-with-libs-mdp.exe.mdb", 
			"lib/console-with-libs-mdp/library1.dll",
			"lib/console-with-libs-mdp/library1.dll.mdb",
			"lib/console-with-libs-mdp/library2.dll",
			"lib/console-with-libs-mdp/library2.dll.mdb",
			"lib/pkgconfig/library1.pc",
			"lib/pkgconfig/library2.pc"
		};
	
		[Test]
		public void ConsoleProjectWithLibsMdpTarball ()
		{
			RunTestTarballPackage ("console-with-libs-mdp/console-with-libs-mdp.mds", false, "Debug", filesConsoleProjectWithLibsMdpTarball);
		}
	
		[Test]
		public void ConsoleProjectWithLibsMdpTarballAutotools ()
		{
			RunTestTarballPackage ("console-with-libs-mdp/console-with-libs-mdp.mds", true, "Debug", filesConsoleProjectWithLibsMdpTarball);
		}
		
		string[] filesNestedSolutionsMdpTarball = new string[] {
			"bin/console-project", 
			"bin/console-project2", 
			"lib/nested-solutions-mdp/console-project.exe", 
			"lib/nested-solutions-mdp/console-project.exe.mdb",
			"lib/nested-solutions-mdp/console-project2.exe", 
			"lib/nested-solutions-mdp/console-project2.exe.mdb",
			"lib/nested-solutions-mdp/library1.dll", 
			"lib/nested-solutions-mdp/library1.dll.mdb", 
			"lib/nested-solutions-mdp/library2.dll", 
			"lib/nested-solutions-mdp/library2.dll.mdb", 
			"lib/nested-solutions-mdp/library3.dll", 
			"lib/nested-solutions-mdp/library3.dll.mdb", 
			"lib/nested-solutions-mdp/library4.dll", 
			"lib/nested-solutions-mdp/library4.dll.mdb", 
			"lib/pkgconfig/library1.pc", 
			"lib/pkgconfig/library2.pc", 
			"lib/pkgconfig/library3.pc", 
			"lib/pkgconfig/library4.pc", 
		};
		
		[Test]
		public void NestedSolutionsMdpTarball ()
		{
			RunTestTarballPackage ("nested-solutions-mdp/nested-solutions-mdp.mds", false, "Debug", filesNestedSolutionsMdpTarball);
		}
		
		[Test]
		public void NestedSolutionsMdpTarballAutotools ()
		{
			RunTestTarballPackage ("nested-solutions-mdp/nested-solutions-mdp.mds", true, "Debug", filesNestedSolutionsMdpTarball);
		}

#endregion
		
#region Support methods
		
		void RunTestBinariesPackage (string solFile, string[] expectedFiles)
		{
			solFile = Util.GetSampleProject (solFile);
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor(), solFile);
			
			BinariesZipPackageBuilder pb = new BinariesZipPackageBuilder ();
			pb.SetSolutionItem (sol.RootFolder, sol.GetAllSolutionItems<SolutionItem> ());
			
			pb.TargetFile = GetTempTarFile ("binzip");
			pb.Platform = "Linux";
			pb.Configuration = "Debug";
			
			if (!DeployService.BuildPackage (Util.GetMonitor (), pb))
				Assert.Fail ("Package generation failed");
			
			CheckTarContents (pb.TargetFile, expectedFiles, true);
		}
		
		void RunTestSourcesPackage (string solFile)
		{
			solFile = Util.GetSampleProject (solFile);
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor(), solFile);
			
			SourcesZipPackageBuilder pb = new SourcesZipPackageBuilder ();
			pb.FileFormat = sol.FileFormat;
			pb.SetSolutionItem (sol.RootFolder, sol.GetAllSolutionItems<SolutionItem> ());
			
			pb.TargetFile = GetTempTarFile ("sourceszip");
			
			if (!DeployService.BuildPackage (Util.GetMonitor (), pb))
				Assert.Fail ("Package generation failed");
			
			List<string> files = new List<string> ();
			foreach (string f in sol.GetItemFiles (true)) {
				files.Add (sol.GetRelativeChildPath (f));
			}
			CheckTarContents (pb.TargetFile, files.ToArray (), true);
		}
		
		void RunTestTarballPackage (string solFile, bool autotools, string config, string[] expectedFiles)
		{
			solFile = Util.GetSampleProject (solFile);
			Solution sol = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor(), solFile);
			
			TarballDeployTarget pb = new TarballDeployTarget ();
			pb.SetSolutionItem (sol.RootFolder, sol.GetAllSolutionItems<SolutionItem> ());
			pb.TargetDir = Util.CreateTmpDir ("tarball-target-dir");
			pb.DefaultConfiguration = config;
			pb.GenerateFiles = true;
			pb.GenerateAutotools = autotools;
			
			if (!DeployService.BuildPackage (Util.GetMonitor (), pb))
				Assert.Fail ("Package generation failed");
			
			string tarfile = Directory.GetFiles (pb.TargetDir) [0];
			
			Untar (tarfile, null);
			
			string[] dirs = Directory.GetDirectories (pb.TargetDir);
			Assert.AreEqual (1, dirs.Length);
			
			string tarDir = dirs [0];
			string prefix = Path.Combine (pb.TargetDir, "install");
			
			if (!Exec (Path.Combine (tarDir, "configure"), "--prefix=" + prefix, tarDir))
				Assert.Fail ("Configure script failed");
			
			if (!Exec ("make", "all", tarDir))
				Assert.Fail ("Build failed");
			
			if (!Exec ("make", "install", tarDir))
				Assert.Fail ("Install failed");
			
			CheckDirContents (prefix, expectedFiles);
		}
		
		
		string GetTempTarFile (string hint)
		{
			if (tarDir == null)
				tarDir = Util.CreateTmpDir ("packages");
			return Path.Combine (tarDir, hint + "-" + (packId++) + ".tar.gz");
		}
		
		void CheckTarContents (string file, string[] expectedFiles, bool checkInSubdir)
		{
			Assert.IsTrue (File.Exists (file), "tar file not found");
			
			string dir = Util.CreateTmpDir ("tar-contents");
			Untar (file, dir);
			
			if (checkInSubdir) {
				string[] dirs = Directory.GetDirectories (dir);
				Assert.IsTrue (dirs.Length == 1, "No unique subdir found in tar");
				Assert.IsTrue (Directory.GetFiles(dir).Length == 0, "No unique subdir found in tar");
				dir = Path.Combine (dir, dirs[0]);
			}
			CheckDirContents (dir, expectedFiles);
		}
		
		void CheckDirContents (string dir, string[] expectedFiles)
		{
			List<string> validFiles = new List<string> ();
			foreach (string f in expectedFiles) {
				string tf = Path.Combine (dir, f);
				Assert.IsTrue (File.Exists (tf), "Content check: file not found: " + Path.GetFullPath (tf));
				validFiles.Add (Path.GetFullPath (tf));
			}
			CheckValidFiles (dir, validFiles);
		}
		
		void CheckValidFiles (string dir, List<string> validFiles)
		{
			foreach (string f in Directory.GetFiles (dir))
				Assert.IsFalse (!validFiles.Contains (Path.GetFullPath(f)), "Content check: extra file: " + Path.GetFullPath(f));
			foreach (string d in Directory.GetDirectories (dir))
				CheckValidFiles (d, validFiles);
		}
		
		void Untar (string file, string dir)
		{
			if (dir == null)
				dir = Path.GetDirectoryName (file);
			Exec ("tar", "xvfz " + file + " -C " + dir, null);
		}
		
		bool Exec (string file, string args, string workDir)
		{
			Process proc = new Process ();
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;
			proc.StartInfo.FileName = file;
			proc.StartInfo.Arguments = args;
			if (workDir != null)
				proc.StartInfo.WorkingDirectory = workDir;
			proc.Start ();
			string sout = proc.StandardOutput.ReadToEnd ();
			sout += proc.StandardError.ReadToEnd ();
			proc.WaitForExit ();
			if (proc.ExitCode == 0)
				return true;
			else {
				Console.WriteLine (sout);
				return false;
			}
		}
	}
#endregion
}
