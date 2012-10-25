// 
// LocalCopyTests.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	
	[TestFixture]
	public class LocalCopyTests : TestBase
	{
		[Test]
		[Ignore ("We don't install the msbuild assemblies in the right place for this tests")]
		public void CheckLocalCopy ()
		{
			string solFile = Util.GetSampleProject ("vs-local-copy", "VSLocalCopyTest.sln");
			
			WorkspaceItem item = Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Assert.IsTrue (item is Solution);
			Solution sol = (Solution) item;
			
			AssertCleanBuild (sol, "Debug");
			AssertCleanBuild (sol, "Release");
			
			AssertOutputFiles (sol, "VSLocalCopyTest", "Debug", new string[] {
				"ClassLibrary1.dll",
				"ClassLibrary1.dll.mdb",
				"ClassLibrary2.dll",
				"ClassLibrary2.dll.mdb",
				"ClassLibrary4.dll",
				"ClassLibrary4.dll.mdb",
				"VSLocalCopyTest.exe",
				"VSLocalCopyTest.exe.mdb",
				"TextFile1.txt",
				"folder/baz.txt",
				"foo/bar.txt",
				"quux.txt",
				"VSLocalCopyTest.exe.config",
			});
			
			//FIXME: all of these should have mdb files in release mode.
			//See [Bug 431451] MD ignores DebugType pdbonly
			AssertOutputFiles (sol, "VSLocalCopyTest", "Release", new string[] {
				"ClassLibrary1.dll",
				"ClassLibrary2.dll",
				"ClassLibrary4.dll",
				"VSLocalCopyTest.exe",
				"TextFile1.txt",
				"folder/baz.txt",
				"foo/bar.txt",
				"quux.txt",
				"VSLocalCopyTest.exe.config",
			});
			
			AssertOutputFiles (sol, "ClassLibrary1", "Debug", new string[] {
				"ClassLibrary1.dll",
				"ClassLibrary1.dll.mdb",
				"ClassLibrary2.dll",
				"ClassLibrary2.dll.mdb",
				"TextFile1.txt",
				"TextFile2.txt",
				"foo/bar.txt",
			});
			
			AssertOutputFiles (sol, "ClassLibrary1", "Release", new string[] {
				"ClassLibrary1.dll",
				"ClassLibrary2.dll",
				"TextFile1.txt",
				"TextFile2.txt",
				"foo/bar.txt",
			});
			
			AssertOutputFiles (sol, "ClassLibrary2", "Debug", new string[] {
				"ClassLibrary2.dll",
				"ClassLibrary2.dll.mdb",
				"TextFile2.txt"
			});
			
			AssertOutputFiles (sol, "ClassLibrary2", "Release", new string[] {
				"ClassLibrary2.dll",
				"TextFile2.txt"
			});
			
			AssertOutputFiles (sol, "ClassLibrary3", "Debug", new string[] {
				"ClassLibrary3.dll",
				"ClassLibrary3.dll.mdb"
			});
			
			AssertOutputFiles (sol, "ClassLibrary3", "Release", new string[] {
				"ClassLibrary3.dll"
			});
			
			AssertOutputFiles (sol, "ClassLibrary4", "Debug", new string[] {
				"ClassLibrary4.dll",
				"ClassLibrary4.dll.mdb"
			});
			
			AssertOutputFiles (sol, "ClassLibrary4", "Release", new string[] {
				"ClassLibrary4.dll"
			});
			
			AssertOutputFiles (sol, "ClassLibrary5", "Debug", new string[] {
				"ClassLibrary5.dll",
				"ClassLibrary5.dll.mdb",
			});
			
			AssertOutputFiles (sol, "ClassLibrary5", "Release", new string[] {
				"ClassLibrary5.dll"
			});
		}
				
		static void AssertOutputFiles (Solution solution, string projectName, string configuration, string[] expectedFiles)
		{
			foreach (Project proj in solution.GetAllProjects ()) {
				if (proj.Name == projectName) {
					AssertOutputFiles (proj, configuration, expectedFiles);
					return;
				}
			}
			Assert.Fail ("Did not find project '{0}'", projectName);
		}
		
		static void AssertOutputFiles (Project project, string configuration, string[] expectedFiles)
		{
			string directory = Path.GetDirectoryName (project.GetOutputFileName ((SolutionConfigurationSelector)configuration));
			Assert.IsFalse (string.IsNullOrEmpty (directory), "Project '{0} has no output directory", project);
			List<string> files = new List<string> (Directory.GetFiles (directory, "*", SearchOption.AllDirectories));
			
			for (int i = 0; i < files.Count; i++)
				files[i] = files[i].Substring (directory.Length + 1);
			
			foreach (string expectedFile in expectedFiles)
				Assert.IsTrue (files.Remove (expectedFile), "Did not find file '{0}' in '{1}'",
				               expectedFile, directory);
			
			Assert.IsTrue (files.Count == 0, "There are unexpected files in the directory {0}: {1}", directory, Join (files));
		}
		
		static string Join (List<string> list)
		{
			if (list.Count < 1)
				return String.Empty;
			string [] arr = new string [list.Count + list.Count - 1];
			string comma = ", ";
			for (int i = 0; i < arr.Length; i++)
				arr[i] = (i % 2 == 0)? list [i / 2] : comma;
			return String.Concat (arr);
		}
		
		static void AssertCleanBuild (Solution sol, string configuration)
		{
			BuildResult cr = sol.Build (Util.GetMonitor (), configuration);
			Assert.IsNotNull (cr);
			Assert.AreEqual (0, cr.ErrorCount);
			Assert.AreEqual (0, cr.WarningCount);
		}
	}
}
