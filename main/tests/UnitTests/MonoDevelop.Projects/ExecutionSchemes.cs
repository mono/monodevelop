//
// ExecutionSchemes.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTests;
using System.Linq;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ExecutionSchemes: TestBase
	{
		[Test]
		public async Task SaveSchemes ()
		{
			string solFile = Util.GetSampleProject ("execution-schemes", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];

			var es = new ProjectExecutionScheme ("Test1");
			es.Properties.SetValue ("SomeValue","Foo");
			p.ExecutionSchemes.Add (es);

			es = new ProjectExecutionScheme ("Test2");
			es.Properties.SetValue ("SomeValue", "Bar");
			p.ExecutionSchemes.Add (es);

			Assert.AreEqual (2, p.GetExecutionSchemes (p.DefaultConfiguration.Selector).Count ());

			await sol.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.schemes-added"));
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task LoadSchemes ()
		{
			string projFile = Util.GetSampleProject ("execution-schemes", "ConsoleProject", "ConsoleProject.schemes-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			Assert.AreEqual (2, p.ExecutionSchemes.Count);

			Assert.IsInstanceOf<ProjectExecutionScheme> (p.ExecutionSchemes [0]);
			var es = (ProjectExecutionScheme)p.ExecutionSchemes [0];
			Assert.AreEqual (es.Name, "Test1");
			Assert.AreEqual (es.Properties.GetValue ("SomeValue"), "Foo");

			Assert.IsInstanceOf<ProjectExecutionScheme> (p.ExecutionSchemes [1]);
			es = (ProjectExecutionScheme)p.ExecutionSchemes [1];
			Assert.AreEqual (es.Name, "Test2");
			Assert.AreEqual (es.Properties.GetValue ("SomeValue"), "Bar");
		}

		[Test]
		public async Task UpdateSchemes ()
		{
			string solFile = Util.GetSampleProject ("execution-schemes", "ConsoleProject", "ConsoleProject.schemes-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			Assert.AreEqual (2, p.ExecutionSchemes.Count);

			Assert.IsInstanceOf<ProjectExecutionScheme> (p.ExecutionSchemes [1]);
			var es = (ProjectExecutionScheme)p.ExecutionSchemes [1];
			es.Properties.SetValue ("SomeValue", "Time");
			es.Properties.SetValue ("SomeValue2", "Time2");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.schemes-modified"));
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task RemoveSchemes ()
		{
			string solFile = Util.GetSampleProject ("execution-schemes", "ConsoleProject", "ConsoleProject.schemes-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			Assert.AreEqual (2, p.ExecutionSchemes.Count);

			p.ExecutionSchemes.Clear ();

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject"));
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task SaveSchemesToUserProject ()
		{
			string solFile = Util.GetSampleProject ("execution-schemes", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];

			var es = new ProjectExecutionScheme ("Test1");
			es.Properties.SetValue ("SomeValue", "Foo");
			p.ExecutionSchemes.Add (es);

			es = new ProjectExecutionScheme ("Test2");
			es.StoreInUserFile = true;
			es.Properties.SetValue ("SomeValue", "Bar");
			p.ExecutionSchemes.Add (es);

			Assert.AreEqual (2, p.GetExecutionSchemes (p.DefaultConfiguration.Selector).Count ());

			await sol.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.schemes-user-added"));
			Assert.AreEqual (newProjectXml, projectXml);

			Assert.IsTrue (File.Exists (p.FileName + ".user"));

			projectXml = File.ReadAllText (p.FileName + ".user");
			newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.schemes-user-added") + ".user");
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task LoadSchemesFromUserProject ()
		{
			string projFile = Util.GetSampleProject ("execution-schemes", "ConsoleProject", "ConsoleProject.schemes-user-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			Assert.AreEqual (2, p.ExecutionSchemes.Count);

			Assert.IsInstanceOf<ProjectExecutionScheme> (p.ExecutionSchemes [0]);
			var es = (ProjectExecutionScheme)p.ExecutionSchemes [0];
			Assert.IsFalse (es.StoreInUserFile);
			Assert.AreEqual (es.Name, "Test1");
			Assert.AreEqual (es.Properties.GetValue ("SomeValue"), "Foo");

			Assert.IsInstanceOf<ProjectExecutionScheme> (p.ExecutionSchemes [1]);
			es = (ProjectExecutionScheme)p.ExecutionSchemes [1];
			Assert.IsTrue (es.StoreInUserFile);
			Assert.AreEqual (es.Name, "Test2");
			Assert.AreEqual (es.Properties.GetValue ("SomeValue"), "Bar");
		}

		[Test]
		public async Task SwitchUserSchemes ()
		{
			string projFile = Util.GetSampleProject ("execution-schemes", "ConsoleProject", "ConsoleProject.schemes-user-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			Assert.AreEqual (2, p.ExecutionSchemes.Count);

			Assert.IsInstanceOf<ProjectExecutionScheme> (p.ExecutionSchemes [0]);
			var es = (ProjectExecutionScheme)p.ExecutionSchemes [0];
			es.StoreInUserFile = true;

			Assert.IsInstanceOf<ProjectExecutionScheme> (p.ExecutionSchemes [1]);
			es = (ProjectExecutionScheme)p.ExecutionSchemes [1];
			es.StoreInUserFile = false;

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.schemes-user-switched"));
			Assert.AreEqual (newProjectXml, projectXml);

			Assert.IsTrue (File.Exists (p.FileName + ".user"));

			projectXml = File.ReadAllText (p.FileName + ".user");
			newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.schemes-user-switched") + ".user");
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task RemoveUserSchemes ()
		{
			string solFile = Util.GetSampleProject ("execution-schemes", "ConsoleProject", "ConsoleProject.schemes-user-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			Assert.AreEqual (2, p.ExecutionSchemes.Count);
			Assert.IsTrue (File.Exists (p.FileName + ".user"));

			p.ExecutionSchemes.Clear ();

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject"));
			Assert.AreEqual (newProjectXml, projectXml);

			Assert.IsFalse (File.Exists (p.FileName + ".user"));
		}
	}
}

