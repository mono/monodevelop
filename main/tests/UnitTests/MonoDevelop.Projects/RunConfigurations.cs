//
// RunConfigurations.cs
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
	public class RunConfigurations: TestBase
	{
		[Test]
		public async Task SaveRunConfigurations ()
		{
			string solFile = Util.GetSampleProject ("run-configurations", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];

			var es = new ProjectRunConfiguration ("Test1");
			es.StoreInUserFile = false;
			es.Properties.SetValue ("SomeValue","Foo");
			p.RunConfigurations.Add (es);

			es = new ProjectRunConfiguration ("Test2");
			es.StoreInUserFile = false;
			es.Properties.SetValue ("SomeValue", "Bar");
			p.RunConfigurations.Add (es);

			Assert.AreEqual (3, p.GetRunConfigurations ().Count ());

			await sol.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.configs-added"));
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task LoadRunConfigurations ()
		{
			string projFile = Util.GetSampleProject ("run-configurations", "ConsoleProject", "ConsoleProject.configs-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			Assert.AreEqual (3, p.RunConfigurations.Count);

			var es = p.RunConfigurations [1];
			Assert.AreEqual (es.Name, "Test1");
			Assert.AreEqual (es.Properties.GetValue ("SomeValue"), "Foo");

			es = p.RunConfigurations [2];
			Assert.AreEqual (es.Name, "Test2");
			Assert.AreEqual (es.Properties.GetValue ("SomeValue"), "Bar");
		}

		[Test]
		public async Task UpdateRunConfigurations ()
		{
			string solFile = Util.GetSampleProject ("run-configurations", "ConsoleProject", "ConsoleProject.configs-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			Assert.AreEqual (3, p.RunConfigurations.Count);

			var es = p.RunConfigurations [2];
			es.Properties.SetValue ("SomeValue", "Time");
			es.Properties.SetValue ("SomeValue2", "Time2");

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.configs-modified"));
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task RemoveRunConfigurations ()
		{
			string solFile = Util.GetSampleProject ("run-configurations", "ConsoleProject", "ConsoleProject.configs-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			Assert.AreEqual (3, p.RunConfigurations.Count);

			p.RunConfigurations.Clear ();

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject"));
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task SaveRunConfigurationsToUserProject ()
		{
			string solFile = Util.GetSampleProject ("run-configurations", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];

			var es = new ProjectRunConfiguration ("Test1");
			es.StoreInUserFile = false;
			es.Properties.SetValue ("SomeValue", "Foo");
			p.RunConfigurations.Add (es);

			es = new ProjectRunConfiguration ("Test2");
			es.Properties.SetValue ("SomeValue", "Bar");
			p.RunConfigurations.Add (es);

			Assert.AreEqual (3, p.GetRunConfigurations ().Count ());

			await sol.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.configs-user-added"));
			Assert.AreEqual (newProjectXml, projectXml);

			Assert.IsTrue (File.Exists (p.FileName + ".user"));

			projectXml = File.ReadAllText (p.FileName + ".user");
			newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.configs-user-added") + ".user");
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task LoadRunConfigurationsFromUserProject ()
		{
			string projFile = Util.GetSampleProject ("run-configurations", "ConsoleProject", "ConsoleProject.configs-user-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			Assert.AreEqual (3, p.RunConfigurations.Count);

			var es = p.RunConfigurations [1];
			Assert.IsFalse (es.StoreInUserFile);
			Assert.AreEqual (es.Name, "Test1");
			Assert.AreEqual (es.Properties.GetValue ("SomeValue"), "Foo");

			es = p.RunConfigurations [2];
			Assert.IsTrue (es.StoreInUserFile);
			Assert.AreEqual (es.Name, "Test2");
			Assert.AreEqual (es.Properties.GetValue ("SomeValue"), "Bar");
		}

		[Test]
		public async Task SwitchUserRunConfigurations ()
		{
			string projFile = Util.GetSampleProject ("run-configurations", "ConsoleProject", "ConsoleProject.configs-user-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			Assert.AreEqual (3, p.RunConfigurations.Count);

			var es = p.RunConfigurations [1];
			es.StoreInUserFile = true;

			es = p.RunConfigurations [2];
			es.StoreInUserFile = false;

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.configs-user-switched"));
			Assert.AreEqual (newProjectXml, projectXml);

			Assert.IsTrue (File.Exists (p.FileName + ".user"));

			projectXml = File.ReadAllText (p.FileName + ".user");
			newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.configs-user-switched") + ".user");
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task RemoveUserRunConfigurations ()
		{
			string solFile = Util.GetSampleProject ("run-configurations", "ConsoleProject", "ConsoleProject.configs-user-added.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			Assert.AreEqual (3, p.RunConfigurations.Count);
			Assert.IsTrue (File.Exists (p.FileName + ".user"));

			p.RunConfigurations.Clear ();

			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			string newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject"));
			Assert.AreEqual (newProjectXml, projectXml);

			Assert.IsFalse (File.Exists (p.FileName + ".user"));
		}

		[Test]
		public async Task ModifyDefaultRunConfiguration ()
		{
			string solFile = Util.GetSampleProject ("run-configurations", "ConsoleProject", "ConsoleProject.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			Assert.AreEqual (1, p.RunConfigurations.Count);

			var es = p.RunConfigurations [0];
			Assert.AreEqual ("Default", es.Name);
			es.Properties.SetValue ("SomeValue", "Time");

			string projectXml = File.ReadAllText (p.FileName);

			await p.SaveAsync (Util.GetMonitor ());

			string newProjectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (newProjectXml, projectXml);

			Assert.IsTrue (File.Exists (p.FileName + ".user"));

			projectXml = File.ReadAllText (p.FileName + ".user");
			newProjectXml = File.ReadAllText (p.FileName.ChangeName ("ConsoleProject.default-modified") + ".user");
			Assert.AreEqual (newProjectXml, projectXml);
		}

		[Test]
		public async Task ResetDefaultRunConfiguration ()
		{
			string solFile = Util.GetSampleProject ("run-configurations", "ConsoleProject", "ConsoleProject.default-modified.csproj");
			DotNetProject p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			Assert.AreEqual (1, p.RunConfigurations.Count);

			var es = p.RunConfigurations [0];
			Assert.AreEqual ("Default", es.Name);
			Assert.AreEqual ("Time", es.Properties.GetValue ("SomeValue"));
			es.Properties.RemoveProperty ("SomeValue");

			string projectXml = File.ReadAllText (p.FileName);

			await p.SaveAsync (Util.GetMonitor ());

			string newProjectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (newProjectXml, projectXml);

			Assert.IsFalse (File.Exists (p.FileName + ".user"));
		}
	}
}

