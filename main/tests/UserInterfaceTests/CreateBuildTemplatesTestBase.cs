//
// SimpleTest.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.IO;
using System.Threading;

using MonoDevelop.Core;

using NUnit.Framework;

namespace UserInterfaceTests
{
	public abstract class CreateBuildTemplatesTestBase: UITestBase
	{
		public readonly static Action EmptyAction = () => { };

		public void AssertExeHasOutput (string exe, string expectedOutput)
		{
			var sw = new StringWriter ();
			var p = ProcessUtils.StartProcess (new ProcessStartInfo (exe), sw, sw, CancellationToken.None);
			Assert.AreEqual (0, p.Result);
			string output = sw.ToString ();

			Assert.AreEqual (expectedOutput, output.Trim ());
		}

		public void CreateBuildProject (string projectName, string kind, string category, string categoryRoot, Action beforeBuild)
		{
			var solutionParentDirectory = Util.CreateTmpDir (projectName);
			try {
				var newProject = new NewProjectController ();
				newProject.Open ();

				Assert.IsTrue (newProject.SelectTemplateType (category, categoryRoot));
				Thread.Sleep (3000);
				Assert.IsTrue (newProject.SelectTemplate (kind));
				Thread.Sleep (3000);

				Assert.IsTrue (newProject.Next ());
				Thread.Sleep (3000);

				Assert.IsTrue (newProject.SetProjectName (projectName));
				Thread.Sleep (2000);

				Assert.IsTrue (newProject.SetSolutionName (projectName));
				Thread.Sleep (2000);

				Assert.IsTrue (newProject.SetSolutionLocation (solutionParentDirectory));
				Thread.Sleep (2000);

				Assert.IsTrue (newProject.CreateProjectInSolutionDirectory (false));
				Thread.Sleep (2000);

				Assert.IsTrue (newProject.UseGit (true, false));
				Thread.Sleep (2000);

				Assert.IsTrue (newProject.Next ());
				Thread.Sleep (2000);

				beforeBuild ();
				Thread.Sleep (1000);

				Assert.IsTrue (Ide.BuildSolution ());
			} finally {
				Directory.Delete (solutionParentDirectory, true);
				Ide.CloseAll ();
			}
		}
	}
}