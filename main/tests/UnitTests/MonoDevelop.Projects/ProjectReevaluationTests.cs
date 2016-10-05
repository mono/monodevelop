//
// ProjectReevaluationTests.cs
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
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ProjectReevaluationTests: TestBase
	{
		[Test ()]
		public async Task ReevaluateLoadSave ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (Project)sol.Items [0];

			int itemAdded = 0;
			int itemRemoved = 0;
			int configAdded = 0;
			int configRemoved = 0;
			bool configsChanged = false;
			bool runConfigsChanged = false;
			bool capabilitiesChanged = false;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;
			p.ConfigurationAdded += (sender, e) => configAdded++;
			p.ConfigurationRemoved += (sender, e) => configRemoved++;
			p.ConfigurationsChanged += (sender, e) => configsChanged = true;
			p.RunConfigurationsChanged += (sender, e) => runConfigsChanged = true;
			p.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;

			await p.ReevaluateProject (Util.GetMonitor ());

			Assert.AreEqual (0, itemAdded);
			Assert.AreEqual (0, itemRemoved);
			Assert.AreEqual (0, configAdded);
			Assert.AreEqual (0, configRemoved);
			Assert.IsFalse (configsChanged);
			Assert.IsFalse (runConfigsChanged);
			Assert.IsFalse (capabilitiesChanged);

			string projectXml = File.ReadAllText (p.FileName);

			await sol.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName));
		}

		[Test ()]
		public async Task ReevaluateAddRemoveItem ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (Project)sol.Items [0];

			int itemAdded = 0;
			int itemRemoved = 0;
			int configAdded = 0;
			int configRemoved = 0;
			bool configsChanged = false;
			bool runConfigsChanged = false;
			bool capabilitiesChanged = false;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;
			p.ConfigurationAdded += (sender, e) => configAdded++;
			p.ConfigurationRemoved += (sender, e) => configRemoved++;
			p.ConfigurationsChanged += (sender, e) => configsChanged = true;
			p.RunConfigurationsChanged += (sender, e) => runConfigsChanged = true;
			p.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;

			var it = p.MSBuildProject.GetAllItems ().First (i => i.Include == "Program.cs");
			p.MSBuildProject.AddNewItem ("Compile", "foo.cs", it);
			p.MSBuildProject.AddNewItem ("Compile", "bar.cs");
			p.MSBuildProject.RemoveItem (it);

			await p.ReevaluateProject (Util.GetMonitor ());

			Assert.AreEqual (2, itemAdded);
			Assert.AreEqual (1, itemRemoved);
			Assert.AreEqual (0, configAdded);
			Assert.AreEqual (0, configRemoved);
			Assert.IsFalse (configsChanged);
			Assert.IsFalse (runConfigsChanged);
			Assert.IsFalse (capabilitiesChanged);

			Assert.AreEqual (new [] { @"Properties\AssemblyInfo.cs", "foo.cs", "bar.cs" }, p.Files.Select (f => f.Include).ToArray ());
			Assert.AreEqual (new [] { "Compile", "Compile", "Compile" }, p.Files.Select (f => f.BuildAction).ToArray ());

			await sol.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (File.ReadAllText (p.FileName), File.ReadAllText (p.FileName.ChangeName ("ConsoleProject-refresh-saved")));
		}

		[Test ()]
		public async Task ReevaluateModifyItem ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (Project)sol.Items [0];

			int itemAdded = 0;
			int itemRemoved = 0;
			int configAdded = 0;
			int configRemoved = 0;
			bool configsChanged = false;
			bool runConfigsChanged = false;
			bool capabilitiesChanged = false;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;
			p.ConfigurationAdded += (sender, e) => configAdded++;
			p.ConfigurationRemoved += (sender, e) => configRemoved++;
			p.ConfigurationsChanged += (sender, e) => configsChanged = true;
			p.RunConfigurationsChanged += (sender, e) => runConfigsChanged = true;
			p.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;

			var file = p.Files.FirstOrDefault (f => f.FilePath.FileName == "Program.cs");
			Assert.IsTrue (file.Visible);

			var it = p.MSBuildProject.GetAllItems ().First (i => i.Include == "Program.cs");
			it.Metadata.SetValue ("Visible", "False");
			it.Metadata.SetValue ("Foo", "Bar");

			await p.ReevaluateProject (Util.GetMonitor ());

			Assert.AreEqual (1, itemAdded);
			Assert.AreEqual (1, itemRemoved);
			Assert.AreEqual (0, configAdded);
			Assert.AreEqual (0, configRemoved);
			Assert.IsFalse (configsChanged);
			Assert.IsFalse (runConfigsChanged);
			Assert.IsFalse (capabilitiesChanged);

			Assert.AreEqual (new [] { @"Properties\AssemblyInfo.cs", "Program.cs" }, p.Files.Select (f => f.Include).ToArray ());

			file = p.Files.FirstOrDefault (f => f.FilePath.FileName == "Program.cs");
			Assert.IsFalse (file.Visible);

			await sol.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (File.ReadAllText (p.FileName), File.ReadAllText (p.FileName.ChangeName ("ConsoleProject-refresh-item-changed-saved")));
		}
	}
}
