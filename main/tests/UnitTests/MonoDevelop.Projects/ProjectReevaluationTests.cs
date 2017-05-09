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

			sol.Dispose ();
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

			sol.Dispose ();
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

			sol.Dispose ();
		}

		[Test]
		public async Task ReevaluateExistingProjectReferencesAfterLoad ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject)sol.Items [0];

			int itemAdded = 0;
			int itemRemoved = 0;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;

			await p.ReevaluateProject (Util.GetMonitor ());

			var library1Reference = p.References.First (r => r.Include == @"..\library1\library1.csproj");
			var library2Reference = p.References.First (r => r.Include == @"..\library2\library2.csproj");

			var library1Item = p.Items.OfType<ProjectReference> ().First (r => r.Include == @"..\library1\library1.csproj");
			var library2Item = p.Items.OfType<ProjectReference> ().First (r => r.Include == @"..\library2\library2.csproj");

			Assert.AreEqual (0, itemAdded);
			Assert.AreEqual (0, itemRemoved);
			Assert.AreEqual (p, library1Item.OwnerProject);
			Assert.AreEqual (p, library2Item.OwnerProject);
			Assert.AreSame (library1Reference, library1Item);
			Assert.AreSame (library2Reference, library2Item);

			sol.Dispose ();
		}

		[Test]
		public async Task ReevaluateNewProjectReferencesAfterSave ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject)sol.Items [0];

			// Remove all existing project references and save.
			var existingProjectReferences = p.References.Where (r => r.ReferenceType == ReferenceType.Project).ToArray ();
			p.References.RemoveRange (existingProjectReferences);
			await p.SaveAsync (Util.GetMonitor ());

			// Reload solution.
			sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			p = (DotNetProject)sol.Items [0];

			Assert.IsFalse (p.References.Any (r => r.ReferenceType == ReferenceType.Project));

			// Add reference to library1.
			var library1Project = (DotNetProject)sol.Items.First (item => item.Name == "library1");
			var projectReference = ProjectReference.CreateProjectReference (library1Project);
			p.References.Add (projectReference);
			await p.SaveAsync (Util.GetMonitor ());

			int itemAdded = 0;
			int itemRemoved = 0;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;

			await p.ReevaluateProject (Util.GetMonitor ());

			var library1Reference = p.References.First (r => r.Include == @"..\library1\library1.csproj");

			var library1Item = p.Items.OfType<ProjectReference> ().First (r => r.Include == @"..\library1\library1.csproj");

			Assert.AreEqual (0, itemAdded);
			Assert.AreEqual (0, itemRemoved);
			Assert.AreEqual (p, library1Item.OwnerProject);
			Assert.AreSame (library1Reference, library1Item);
			Assert.AreEqual (library1Reference, library1Item);

			sol.Dispose ();
		}
	}
}
