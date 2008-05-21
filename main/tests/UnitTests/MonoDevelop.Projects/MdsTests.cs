// MdsTests.cs
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
using System.IO;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	[TestFixture()]
	public class MdsTests: TestBase
	{
		[Test()]
		public void LoadSaveBuildConsoleProject()
		{
			string solFile = Util.GetSampleProject ("csharp-console-mdp", "csharp-console-mdp.mds");
			
			WorkspaceItem item = Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Assert.IsTrue (item is Solution);
			
			Solution sol = (Solution) item;
			TestProjectsChecks.CheckBasicMdConsoleProject (sol);
			string projectFile = ((Project)sol.Items [0]).FileName;
			
			ICompilerResult cr = item.Build (Util.GetMonitor (), "Debug");
			Assert.IsNotNull (cr);
			Assert.AreEqual (0, cr.ErrorCount);
			Assert.AreEqual (0, cr.WarningCount);
			
			string solXml = Util.GetXmlFileInfoset (solFile);
			string projectXml = Util.GetXmlFileInfoset (projectFile);
			
			sol.Save (Util.GetMonitor ());
			
			Assert.AreEqual (solXml, Util.GetXmlFileInfoset (solFile), "Saved solution file");
			Assert.AreEqual (projectXml, Util.GetXmlFileInfoset (projectFile), "Saved project file");
		}
		
		[Test()]
		public void NestedSolutions()
		{
			string solFile = Util.GetSampleProject ("nested-solutions-mdp", "nested-solutions-mdp.mds");
			
			WorkspaceItem item = Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			Assert.IsTrue (item is Solution);
			
			Solution sol = (Solution) item;
			
			ICompilerResult cr = item.Build (Util.GetMonitor (), "Debug");
			Assert.IsNotNull (cr);
			Assert.AreEqual (0, cr.ErrorCount);
			Assert.AreEqual (0, cr.WarningCount);
			Assert.AreEqual (6, cr.BuildCount);

			string dir = Path.GetDirectoryName (solFile);
			string solXml = Util.GetXmlFileInfoset (solFile);
			string p1Xml = Util.GetXmlFileInfoset (dir, "console-project", "console-project.mdp");
			string s1Xml = Util.GetXmlFileInfoset (dir, "nested-solution1", "nested-solution1.mds");
			string s2Xml = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution2.mds");
			string plib1Xml = Util.GetXmlFileInfoset (dir, "nested-solution1", "library1", "library1.mdp");
			string plib2Xml = Util.GetXmlFileInfoset (dir, "nested-solution1", "library2", "library2.mdp");
			string p2Xml = Util.GetXmlFileInfoset (dir, "nested-solution2", "console-project2", "console-project2.mdp");
			string s3Xml = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "nested-solution3.mds");
			string plib3Xml = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "library3", "library3.mdp");
			string plib4Xml = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "library4", "library4.mdp");
			
			sol.Save (Util.GetMonitor ());
			
			string solXml2 = Util.GetXmlFileInfoset (solFile);
			string p1Xml2 = Util.GetXmlFileInfoset (dir, "console-project", "console-project.mdp");
			string s1Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution1", "nested-solution1.mds");
			string s2Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution2.mds");
			string plib1Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution1", "library1", "library1.mdp");
			string plib2Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution1", "library2", "library2.mdp");
			string p2Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution2", "console-project2", "console-project2.mdp");
			string s3Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "nested-solution3.mds");
			string plib3Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "library3", "library3.mdp");
			string plib4Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "library4", "library4.mdp");
			
			Assert.AreEqual (solXml, solXml2, "solXml");
			Assert.AreEqual (p1Xml, p1Xml2, "p1Xml");
			Assert.AreEqual (s1Xml, s1Xml2, "s1Xml");
			Assert.AreEqual (s2Xml, s2Xml2, "s2Xml");
			Assert.AreEqual (plib1Xml, plib1Xml2, "plib1Xml");
			Assert.AreEqual (plib2Xml, plib2Xml2, "plib2Xml");
			Assert.AreEqual (p2Xml, p2Xml2, "p2Xml");
			Assert.AreEqual (s3Xml, s3Xml2, "s3Xml");
			Assert.AreEqual (plib3Xml, plib3Xml2, "plib3Xml");
			Assert.AreEqual (plib4Xml, plib4Xml2, "plib4Xml");
		}
		
		[Test()]
		public void CreateNestedSolutions()
		{
			Solution sol = TestProjectsChecks.CreateProjectWithFolders ("nested-solutions-md1");
			sol.FileFormat = Services.ProjectService.FileFormats.GetFileFormat ("MD1");
			
			sol.Save (Util.GetMonitor ());
			
			string solFile = Util.GetSampleProjectPath ("nested-solutions-mdp", "nested-solutions-mdp.mds");
			string dir = Path.GetDirectoryName (solFile);
			string solXml = Util.GetXmlFileInfoset (solFile);
			string p1Xml = Util.GetXmlFileInfoset (dir, "console-project", "console-project.mdp");
			string s1Xml = Util.GetXmlFileInfoset (dir, "nested-solution1", "nested-solution1.mds");
			string s2Xml = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution2.mds");
			string plib1Xml = Util.GetXmlFileInfoset (dir, "nested-solution1", "library1", "library1.mdp");
			string plib2Xml = Util.GetXmlFileInfoset (dir, "nested-solution1", "library2", "library2.mdp");
			string p2Xml = Util.GetXmlFileInfoset (dir, "nested-solution2", "console-project2", "console-project2.mdp");
			string s3Xml = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "nested-solution3.mds");
			string plib3Xml = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "library3", "library3.mdp");
			string plib4Xml = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "library4", "library4.mdp");
			
			dir = Path.GetDirectoryName (sol.FileName);
			string solXml2 = Util.GetXmlFileInfoset (solFile);
			string p1Xml2 = Util.GetXmlFileInfoset (dir, "console-project", "console-project.mdp");
			string s1Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution1", "nested-solution1.mds");
			string s2Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution2.mds");
			string plib1Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution1", "library1", "library1.mdp");
			string plib2Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution1", "library2", "library2.mdp");
			string p2Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution2", "console-project2", "console-project2.mdp");
			string s3Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "nested-solution3.mds");
			string plib3Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "library3", "library3.mdp");
			string plib4Xml2 = Util.GetXmlFileInfoset (dir, "nested-solution2", "nested-solution3", "library4", "library4.mdp");
			
			Assert.AreEqual (solXml, solXml2, "solXml");
			Assert.AreEqual (p1Xml, p1Xml2, "p1Xml");
			Assert.AreEqual (s1Xml, s1Xml2, "s1Xml");
			Assert.AreEqual (s2Xml, s2Xml2, "s2Xml");
			Assert.AreEqual (plib1Xml, plib1Xml2, "plib1Xml");
			Assert.AreEqual (plib2Xml, plib2Xml2, "plib2Xml");
			Assert.AreEqual (p2Xml, p2Xml2, "p2Xml");
			Assert.AreEqual (s3Xml, s3Xml2, "s3Xml");
			Assert.AreEqual (plib3Xml, plib3Xml2, "plib3Xml");
			Assert.AreEqual (plib4Xml, plib4Xml2, "plib4Xml");
		}
		
		[Test]
		public void TestSaveWorkspace ()
		{
			// Saving a workspace must save all solutions and projects it contains
			
			string dir = Util.CreateTmpDir ("TestSaveWorkspace");
			Workspace ws = new Workspace ();
			ws.FileName = Path.Combine (dir, "workspace");
			
			Solution sol = new Solution ();
			sol.FileName = Path.Combine (dir, "thesolution");
			ws.Items.Add (sol);
			
			DotNetProject p = new DotNetProject ("C#");
			p.FileName = Path.Combine (dir, "theproject");
			sol.RootFolder.Items.Add (p);
			
			ws.Save (Util.GetMonitor ());
			
			Assert.IsTrue (File.Exists (ws.FileName));
			Assert.IsTrue (File.Exists (sol.FileName));
			Assert.IsTrue (File.Exists (p.FileName));
		}
		
		
		[Test]
		public void TestCreateLoadSaveConsoleProject ()
		{
			TestProjectsChecks.TestCreateLoadSaveConsoleProject ("MD1");
		}
		
		[Test]
		public void GenericProject ()
		{
			TestProjectsChecks.CheckGenericItemProject ("MD1");
		}
		
		[Test]
		public void TestLoadSaveSolutionFolders ()
		{
			TestProjectsChecks.TestLoadSaveSolutionFolders ("MD1");
		}
		
		[Test]
		public void TestLoadSaveResources ()
		{
			TestProjectsChecks.TestLoadSaveResources ("MD1");
		}
	}
}
